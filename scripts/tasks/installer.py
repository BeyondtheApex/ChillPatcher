# -*- coding: utf-8 -*-
"""
构建任务: 打包 Inno Setup 安装程序

输出: release/OmniMixPlayer_V{x.y.z}_installer.exe

ISCC 路径查找顺序:
  1. 环境变量 INNOSETUP (如 INNOSETUP=C:\Program Files\Inno Setup 7)
  2. 常见安装路径自动探测

要求:
  - playerbuild/ 已完成 (所有文件就位)
  - playerbuild/VC_redist.x64.exe (可选, 缺失则自动下载)
"""

import os
from pathlib import Path

from tasks.base import TaskNode, TaskStatus
from tasks.common import run_cmd, info

# ── 常量 ──
ISS_FILE = Path(__file__).resolve().parent.parent / "installer" / "OmniMixPlayer.iss"
VC_REDIST_URL = "https://aka.ms/vs/17/release/vc_redist.x64.exe"

import build_config as C


def _find_iscc() -> Path | None:
    """查找 ISCC.exe: 环境变量 → 常见路径。"""
    env = os.environ.get("INNOSETUP", "")
    for candidate in [
        Path(env) / "ISCC.exe" if env else None,
        Path("C:/Program Files/Inno Setup 7/ISCC.exe"),
        Path("C:/Program Files (x86)/Inno Setup 7/ISCC.exe"),
        Path("C:/Program Files/Inno Setup 6/ISCC.exe"),
        Path("C:/Program Files (x86)/Inno Setup 6/ISCC.exe"),
    ]:
        if candidate and candidate.is_file():
            return candidate
    return None


def installer_node() -> TaskNode:
    """返回安装程序构建任务节点。"""
    node = TaskNode("📦 Installer (Inno Setup)", "生成 Windows 安装程序 .exe")

    node.create_leaf("检查 Inno Setup 7", "", run_fn=_check_iscc)
    node.create_leaf("检查 VC_redist.x64.exe", "缺失则从 Microsoft 下载",
                     run_fn=_ensure_vc_redist)
    node.create_leaf("更新 .iss 版本号", f"写入 -> {ISS_FILE}", run_fn=_update_iss_version)
    node.create_leaf("编译安装程序", f"ISCC -> release/", run_fn=_compile_iss)

    return node


# ══════════════════════════════════════════════
#  各步骤实现
# ══════════════════════════════════════════════

def _check_iscc() -> TaskStatus:
    iscc = _find_iscc()
    if iscc:
        info(f"  [OK] {iscc}")
        return TaskStatus.SUCCESS
    info(f"  [ERROR] Inno Setup 未找到!")
    info(f"  安装: https://jrsoftware.org/isinfo.php")
    info(f"  或设置环境变量 INNOSETUP=安装目录")
    return TaskStatus.FAILED


def _ensure_vc_redist() -> TaskStatus:
    vc_file = C.PLAYER_BUILD / "VC_redist.x64.exe"
    if vc_file.is_file():
        info(f"  [OK] {vc_file}")
        return TaskStatus.SUCCESS

    info(f"  [WARN] 未找到, 从 Microsoft 下载...")
    info(f"  URL: {VC_REDIST_URL}")
    import urllib.request
    try:
        urllib.request.urlretrieve(VC_REDIST_URL, str(vc_file))
        if vc_file.is_file():
            info(f"  [OK] 下载完成 ({vc_file.stat().st_size / 1024 / 1024:.1f} MB)")
            return TaskStatus.SUCCESS
    except Exception as e:
        info(f"  [ERROR] 下载失败: {e}")
        info(f"  请手动下载放到: {vc_file}")
    return TaskStatus.FAILED


def _update_iss_version() -> TaskStatus:
    # 从 playerbuild/version_info.json 读取版本
    version = "1.0.0"
    ver_file = C.PLAYER_BUILD / "version_info.json"
    if ver_file.is_file():
        import json
        data = json.loads(ver_file.read_text(encoding="utf-8"))
        version = data.get("flutter_version", "1.0.0")
    version = version.split("+")[0]  # 去掉 build 号 (如 3.0+1 → 3.0)

    if not ISS_FILE.is_file():
        info(f"  [ERROR] .iss 文件不存在: {ISS_FILE}")
        return TaskStatus.FAILED

    content = ISS_FILE.read_text(encoding="utf-8")

    import re
    # 版本号
    content = re.sub(
        r'#define MyAppVersion ".*?"',
        f'#define MyAppVersion "{version}"',
        content,
    )
    # 输出文件名
    content = re.sub(
        r'OutputBaseFilename=OmniMixPlayer_V.*?_installer',
        f'OutputBaseFilename=OmniMixPlayer_V{version}_installer',
        content,
    )
    # 源文件路径 (本地 / CI 通用)
    content = re.sub(
        r'#define SourceDir ".*?"',
        f'#define SourceDir "{C.PLAYER_BUILD.as_posix()}"',
        content,
    )
    # 输出目录
    release_dir = (C.PLAYER_BUILD.parent / "release").as_posix()
    content = re.sub(
        r'OutputDir=.*',
        f'OutputDir={release_dir}',
        content,
    )

    ISS_FILE.write_text(content, encoding="utf-8")
    info(f"  [OK] 版本={version}  SourceDir={C.PLAYER_BUILD}")
    return TaskStatus.SUCCESS


def _compile_iss() -> TaskStatus:
    if not ISS_FILE.is_file():
        info(f"  [ERROR] .iss 文件不存在: {ISS_FILE}")
        return TaskStatus.FAILED

    iscc = _find_iscc()
    if not iscc:
        info(f"  [ERROR] ISCC.exe 未找到")
        return TaskStatus.FAILED

    info(f"  正在编译安装程序 (可能需要几分钟)...")
    return run_cmd([str(iscc), str(ISS_FILE)])
