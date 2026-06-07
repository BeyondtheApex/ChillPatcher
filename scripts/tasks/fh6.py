# -*- coding: utf-8 -*-
"""
FH6 桥接构建任务
- fh6-asset: 打包为 Flutter 资源 zip
- fh6-mod: 构建原生 mod 到 release/FH6OmniBridge
"""
import shutil

from build_config import (
    FH6_DIR, FH6_BIN, FH6_STAGE, FH6_ZIP, FH6_FLUTTER_ASSETS,
    OMNI_PCM_DLL, NATIVE_PLUGINS_DIR,
)
from .base import TaskNode
from .common import copy_file, info, run_cmd, clean_cmake_cache, package_zip


def create_fh6_asset_tasks(full: bool = False) -> TaskNode:
    """创建 FH6 资源打包任务树 (cmd: fh6-asset)。"""
    root = TaskNode("FH6 Bridge Asset", "FH6 桥接资源打包 → Flutter assets/")

    if full:
        root.create_leaf("Build OmniPcmShared", "编译 OmniPcmShared",
                         run_fn=_build_omni_pcm)
        root.create_leaf("Build FH6 Bridge", "编译 FH6 桥接 version.dll",
                         run_fn=_build_fh6_bridge)
    else:
        root.create_leaf("Native builds", "跳过 (使用 --full 重新编译)",
                         run_fn=lambda: 0)

    root.create_leaf("Package ZIP", "打包 FH6OmniBridge.zip → Flutter assets",
                     run_fn=_package)
    return root


def create_fh6_mod_tasks(full: bool = False) -> TaskNode:
    """创建 FH6 原生 mod 构建任务树 (cmd: fh6)。"""
    root = TaskNode("FH6 Mod", "FH6 原生 Mod - 输出到 release/FH6OmniBridge/")

    root.create_leaf("Clean", "清理 release/FH6OmniBridge/", run_fn=_clean_mod)

    if full:
        root.create_leaf("Build OmniPcmShared", "编译 OmniPcmShared",
                         run_fn=_build_omni_pcm)
        root.create_leaf("Build FH6 Bridge", "编译 FH6 桥接 version.dll",
                         run_fn=_build_fh6_bridge)
    else:
        root.create_leaf("Native builds", "跳过 (使用 --full 重新编译)",
                         run_fn=lambda: 0)

    root.create_leaf("Assemble", "组装 release/FH6OmniBridge/", run_fn=_assemble_mod)
    return root


# ── 内部执行函数 ──

def _build_omni_pcm() -> int:
    src = NATIVE_PLUGINS_DIR / "OmniPcmShared"
    clean_cmake_cache(src)
    return run_cmd(["build.bat"], cwd=src)


def _build_fh6_bridge() -> int:
    clean_cmake_cache(FH6_DIR)
    return run_cmd(["build.bat"], cwd=FH6_DIR)


def _package() -> bool:
    if not FH6_BIN.exists():
        info(f"  ERROR: Missing {FH6_BIN}")
        return False

    if FH6_STAGE.exists():
        shutil.rmtree(FH6_STAGE)
    FH6_STAGE.mkdir(parents=True, exist_ok=True)

    copy_file(FH6_BIN, FH6_STAGE)
    if OMNI_PCM_DLL.exists():
        copy_file(OMNI_PCM_DLL, FH6_STAGE)
    readme = FH6_DIR / "README.md"
    if readme.exists():
        copy_file(readme, FH6_STAGE)

    return package_zip(FH6_STAGE, FH6_ZIP, FH6_FLUTTER_ASSETS)


def _clean_mod() -> bool:
    if FH6_STAGE.exists():
        shutil.rmtree(FH6_STAGE)
    FH6_STAGE.mkdir(parents=True)
    info("release/FH6OmniBridge cleaned")
    return True


def _assemble_mod() -> bool:
    if FH6_BIN.exists():
        copy_file(FH6_BIN, FH6_STAGE)
    else:
        info(f"  WARNING: Missing {FH6_BIN}")
    if OMNI_PCM_DLL.exists():
        copy_file(OMNI_PCM_DLL, FH6_STAGE)
    readme = FH6_DIR / "README.md"
    if readme.exists():
        copy_file(readme, FH6_STAGE)
    info("  FH6 mod files assembled")
    return True
