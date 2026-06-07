# -*- coding: utf-8 -*-
"""
共享工具函数 — 所有 task 模块共用
"""
import json
import os
import re
import shutil
import subprocess
import sys
import threading
from datetime import datetime
from pathlib import Path
from .base import TaskStatus

# ── 日志文件 (线程本地, runner 在执行每个任务前设置) ──
_log_file: Path | None = None
_log_lock = threading.Lock()


def set_log_file(path: Path | None):
    global _log_file
    with _log_lock:
        _log_file = path


def _write_log(line: str):
    global _log_file
    with _log_lock:
        if _log_file:
            try:
                _log_file.parent.mkdir(parents=True, exist_ok=True)
                with open(_log_file, "a", encoding="utf-8") as f:
                    f.write(line + "\n")
            except Exception:
                pass


# ════════════════════════════════════════════
#  终端输出
# ════════════════════════════════════════════

def info(msg: str):
    line = f"  {msg}"
    print(line, flush=True)
    _write_log(line)


def step(label: str, msg: str):
    line = f"\n[{label}] {msg}"
    print(line, flush=True)
    _write_log(line)


# ════════════════════════════════════════════
#  命令执行
# ════════════════════════════════════════════

def run_cmd(cmd: list[str], cwd: Path | None = None,
            env: dict[str, str] | None = None,
            verbose: bool = False) -> int:
    """运行命令，捕获输出到 GUI 和日志文件，返回退出码。"""
    cmd_str = " ".join(str(c) for c in cmd)
    header = f"    > {cmd_str}"
    print(header, flush=True)
    _write_log(header)

    # 判断是否需要 shell=True (.bat/.cmd 脚本; 纯 exe 用 shell=False 更可靠)
    first = cmd[0].lower() if cmd else ""
    use_shell = first.endswith(".bat") or first.endswith(".cmd")

    if not use_shell:
        # 用 shutil.which 解析完整路径, 避免 CreateProcess PATH 查找问题
        resolved = shutil.which(cmd[0])
        if resolved:
            cmd = [resolved] + cmd[1:]
        else:
            err = f"    FAILED: command not found: {cmd[0]}"
            print(err, flush=True)
            _write_log(err)
            return -1

    popen_cmd = cmd_str if use_shell else cmd

    try:
        proc = subprocess.Popen(
            popen_cmd, cwd=cwd, shell=use_shell, env=env,
            stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
            text=True, encoding="utf-8", errors="replace",
        )
        for line in proc.stdout:
            line = line.rstrip("\n\r")
            if line.strip():
                print(f"      {line}", flush=True)
                _write_log(f"      {line}")
        proc.wait()
        code = proc.returncode
    except Exception as e:
        err = f"    FAILED: {e}"
        print(err, flush=True)
        _write_log(err)
        return -1

    if code != 0:
        fail = f"    FAILED (exit={code})"
        print(fail, flush=True)
        _write_log(fail)
    else:
        ok = f"    OK (exit=0)"
        print(ok, flush=True)
        _write_log(ok)
    return code


# ════════════════════════════════════════════
#  文件操作
# ════════════════════════════════════════════

def _rmtree_ignore_locked(path: Path):
    """删除目录树，跳过被锁定的文件。"""
    if not path.exists():
        return
    for root_str, dirs, files in os.walk(str(path), topdown=False):
        root = Path(root_str)
        for name in files:
            try:
                (root / name).unlink()
            except PermissionError:
                pass
        for name in dirs:
            try:
                (root / name).rmdir()
            except OSError:
                pass
    try:
        path.rmdir()
    except OSError:
        pass


def copy_file(src: Path, dst_dir: Path):
    """复制文件，跳过被锁定的。"""
    try:
        shutil.copy2(src, dst_dir)
    except PermissionError:
        info(f"  WARNING: Locked, skipped: {src.name}")


def copy_dir_contents(src: Path, dst: Path):
    """复制目录内容（跳过锁定文件）。"""
    for item in src.iterdir():
        if item.is_dir():
            dst_sub = dst / item.name
            dst_sub.mkdir(parents=True, exist_ok=True)
            for f in item.rglob("*"):
                if f.is_dir():
                    continue
                rel = f.relative_to(item)
                target = dst_sub / rel
                target.parent.mkdir(parents=True, exist_ok=True)
                copy_file(f, target.parent)
        else:
            copy_file(item, dst)


def copy_dir_except(src: Path, dst: Path, skip: set[str] | None = None):
    """复制目录内容，跳过指定名称的子目录（如 node_modules）。"""
    if skip is None:
        skip = set()
    if not src.exists():
        return
    dst.mkdir(parents=True, exist_ok=True)
    for item in src.iterdir():
        if item.name in skip:
            continue
        if item.is_dir():
            dst_sub = dst / item.name
            copy_dir_except(item, dst_sub, skip)
        else:
            copy_file(item, dst)


def check_exists(path: Path, desc: str = "") -> bool:
    if not path.exists():
        info(f"  WARNING: {desc or path.name} not found: {path}")
        return False
    return True


# ════════════════════════════════════════════
#  .NET 构建
# ════════════════════════════════════════════

def dotnet_restore(proj: Path) -> int:
    info(f"Restoring {proj.name}...")
    return run_cmd(["dotnet", "restore", str(proj)])


def dotnet_build(proj: Path, config: str = "Release") -> int:
    info(f"Building {proj.name} ({config})...")
    return run_cmd(["dotnet", "build", str(proj), "-c", config])


def dotnet_publish(proj: Path, output: Path, config: str = "Release",
                   single_file: bool = False) -> int:
    """发布 .NET 项目。"""
    info(f"Publishing {proj.name} to {output.name}...")
    if output.exists():
        shutil.rmtree(output)
    cmd = [
        "dotnet", "publish", str(proj),
        "-c", config,
        "-o", str(output),
        "--self-contained",
    ]
    if single_file:
        cmd += [
            "-p:PublishSingleFile=true",
            "-p:PublishTrimmed=false",
            "-p:IncludeNativeLibrariesForSelfExtract=true",
        ]
    return run_cmd(cmd)


# ════════════════════════════════════════════
#  原生插件构建
# ════════════════════════════════════════════

def clean_cmake_cache(src: Path):
    """清理 CMake 缓存（路径变更后旧 cache 会报错）。"""
    cmake_build = src / "build"
    if not cmake_build.exists():
        return
    stale = False
    for cache in cmake_build.rglob("CMakeCache.txt"):
        try:
            text = cache.read_text(encoding="utf-8", errors="ignore")
            for line in text.splitlines():
                if line.startswith("CMAKE_HOME_DIRECTORY"):
                    cached_src = line.split("=", 1)[-1].strip().replace("\\", "/")
                    if src.as_posix() not in cached_src:
                        stale = True
                    break
        except Exception:
            pass
    if stale:
        info(f"  Stale CMake cache detected, clearing build dir...")
        shutil.rmtree(cmake_build, ignore_errors=True)


# ════════════════════════════════════════════
#  版本信息
# ════════════════════════════════════════════

def read_version_info(mod_dir: Path, flutter_dir: Path,
                      fh6_dir: Path, fh6_file: Path) -> dict:
    """读取所有版本信息。"""
    flutter_ver = "0.0.0"
    pubspec = flutter_dir / "pubspec.yaml"
    if pubspec.exists():
        m = re.search(r'^version:\s*(\S+)', pubspec.read_text(encoding="utf-8"), re.M)
        if m:
            flutter_ver = m.group(1)

    cs_ver = "0.0.0"
    cs_file = mod_dir / "MyPluginInfo.cs"
    if cs_file.exists():
        m = re.search(r'PLUGIN_VERSION\s*=\s*"([^"]+)"',
                      cs_file.read_text(encoding="utf-8"))
        if m:
            cs_ver = m.group(1)

    fh6_ver = "0.0.0"
    if fh6_file.exists():
        m = re.search(r'FH6_BRIDGE_VERSION\s+"([^"]+)"',
                      fh6_file.read_text(encoding="utf-8"))
        if m:
            fh6_ver = m.group(1)

    return {
        "flutter_version": flutter_ver,
        "mod_version": cs_ver,
        "fh6_bridge_version": fh6_ver,
        "build_time": datetime.now().isoformat(),
    }


def write_version_json(data: dict, *paths: Path):
    """写入 version_info.json 到多个位置。"""
    text = json.dumps(data, indent=2)
    for p in paths:
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(text, encoding="utf-8")
        info(f"  version_info.json -> {p}")


def package_zip(src_dir: Path, zip_path: Path,
                flutter_asset: Path | None = None) -> bool:
    """将目录打包为 zip，可选复制到 Flutter assets。"""
    if not src_dir.exists():
        info(f"  WARNING: {src_dir} not found, skipping zip")
        return False
    if zip_path.exists():
        zip_path.unlink()
    shutil.make_archive(
        str(zip_path.with_suffix("")),  # 去掉 .zip 后缀
        "zip",
        src_dir,
    )
    if flutter_asset:
        flutter_asset.parent.mkdir(parents=True, exist_ok=True)
        copy_file(zip_path, flutter_asset.parent)
        info(f"  {zip_path.name} -> {flutter_asset}")
    return True
