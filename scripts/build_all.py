# -*- coding: utf-8 -*-
"""
Chill 项目统一构建脚本

用法:
  python scripts/build_all.py [命令] [选项]

命令:
  mod             构建 ChillPatcher BepInEx 插件 (mods/chillPatcher/src/)
  player          构建 OmniMixPlayer 播放器 (playerbuild/)
  fh6-asset       打包 FH6 桥接 Flutter 资源
  fh6             构建 FH6 桥接原生 mod (release/FH6OmniBridge)
  all             构建全部 (mod + player + fh6-asset)

选项:
  --full          完整构建 (clean + restore NuGet + 构建原生插件)
  --skip-flutter  跳过快闪 GUI 构建
  --dry-run       只显示构建计划，不实际执行
  --verbose       显示详细构建输出

示例:
  # 快速构建 BepInEx 插件
  python scripts/build_all.py mod

  # 完整构建 OmniMixPlayer（含原生插件）
  python scripts/build_all.py player --full

  # 构建全部
  python scripts/build_all.py all --full
"""

import argparse
import json
import os
import re
import shutil
import subprocess
import sys
from datetime import datetime
from pathlib import Path

# ── 根目录 ──
ROOT = Path(__file__).resolve().parent.parent

# ════════════════════════════════════════════
#  工具链配置 — 按需修改这里的路径
# ════════════════════════════════════════════

# Visual Studio 安装路径（用于 MSVC 编译）
VS_INSTALL_DIR = Path("D:/Program Files/Microsoft Visual Studio/18/Community")

# Mingw-w64 安装路径（为 Go cgo 提供 gcc）
# 下载: https://github.com/niXman/mingw-builds-binaries/releases
# 推荐: Mingw64ツールチェーン x86_64-14.2.0-release-win32-seh-ucrt-rt_v12-rev0.7z
# 解压到 D:/mingw64/ 即可
MINGW_DIR = Path("D:/mingw64")

# CMake 安装路径（可选，留空则自动发现）
CMAKE_DIR = Path("")

# Go 安装路径（可选，留空则自动发现）
GO_DIR = Path("")

# Node.js 安装路径（可选，留空则自动发现）
NODE_DIR = Path("")

# ════════════════════════════════════════════
#  工具链自动发现 — 一般不需要修改
# ════════════════════════════════════════════

def _setup_toolchain():
    """自动将工具链目录加入 PATH。"""
    paths_to_add = []

    def _add_to_path(dir_path: Path):
        s = str(dir_path)
        if s not in os.environ.get("PATH", ""):
            paths_to_add.append(s)

    # ── Go ──
    go_candidates = [GO_DIR / "bin"] if GO_DIR else []
    go_candidates += [
        Path("C:/Program Files/Go/bin"),
        Path("D:/Program Files/Go/bin"),
        Path("C:/Go/bin"),
    ]
    for p in go_candidates:
        if p.joinpath("go.exe").exists():
            _add_to_path(p)
            break

    # ── VS LLVM/Clang (Go cgo 需要 CC 环境变量) ──
    llvm_bin = VS_INSTALL_DIR / "VC" / "Tools" / "Llvm" / "bin"
    if llvm_bin.exists() and llvm_bin.joinpath("clang.exe").exists():
        _add_to_path(llvm_bin)
        # clang -fuse-ld=lld: 用 LLVM lld 替代 MSVC link.exe，兼容 Go cgo 的 GNU ld 脚本
        os.environ["CC"] = "clang -fuse-ld=lld"

    # ── Mingw-w64 gcc（备选，如果 clang 不可用）──
    if "CC" not in os.environ:
        mingw_bin = MINGW_DIR / "bin"
        if mingw_bin.joinpath("gcc.exe").exists():
            _add_to_path(mingw_bin)
            os.environ["CC"] = "gcc"

    # ── CMake ──
    cmake_candidates = [CMAKE_DIR / "bin"] if CMAKE_DIR else []
    cmake_candidates += [
        VS_INSTALL_DIR / "Common7" / "IDE" / "CommonExtensions" / "Microsoft" / "CMake" / "CMake" / "bin",
        Path("C:/Program Files/CMake/bin"),
        Path("C:/Program Files (x86)/CMake/bin"),
    ]
    for p in cmake_candidates:
        if p.joinpath("cmake.exe").exists():
            _add_to_path(p)
            break

    # ── Node.js / npm ──
    node_candidates = [NODE_DIR] if NODE_DIR else []
    node_candidates += [
        Path("C:/Program Files/nodejs"),
        Path("D:/Program Files/nodejs"),
    ]
    for p in node_candidates:
        if p.joinpath("node.exe").exists():
            _add_to_path(p)
            break

    if paths_to_add:
        os.environ["PATH"] = ";".join(paths_to_add) + ";" + os.environ.get("PATH", "")


_setup_toolchain()

# ── BepInEx Mod 路径 ──
MOD_DIR = ROOT / "mods" / "chillPatcher" / "src"
MOD_RELEASE = ROOT / "release" / "ChillPatcher"
MOD_SDK_PROJ = MOD_DIR / "ChillPatcher.SDK" / "ChillPatcher.SDK.csproj"
MOD_MAIN_PROJ = MOD_DIR / "ChillPatcher.csproj"
MOD_ONEJS_PROJ = MOD_DIR / "ChillPatcher.OneJS" / "ChillPatcher.OneJS.csproj"
MOD_UI_DIRS = [
    MOD_DIR / "ui" / "default",
    MOD_DIR / "ui" / "window-manager",
]

# ── OmniMixPlayer 路径 ──
PLAYER_DIR = ROOT / "OmniMixPlayer"
PLAYER_BUILD = ROOT / "playerbuild"
PLAYER_SDK_PROJ = PLAYER_DIR / "OmniMixPlayer.SDK" / "OmniMixPlayer.SDK.csproj"
PLAYER_BACKEND_PROJ = PLAYER_DIR / "OmniMixPlayer.Backend" / "OmniMixPlayer.Backend.csproj"
PLAYER_BACKEND_BUILD = PLAYER_DIR / "bin" / "Backend"
PLAYER_BACKEND_PUBLISH = PLAYER_DIR / "bin" / "BackendPublish"
PLAYER_MODULES_BUILD = PLAYER_DIR / "bin" / "Modules"
PLAYER_FLUTTER_DIR = PLAYER_DIR / "gui_flutter"
PLAYER_FLUTTER_BUILD = PLAYER_DIR / "gui_flutter" / "build" / "windows" / "x64" / "runner" / "Release"

# ── MediaGenerator ──
MEDIA_GEN_PROJ = ROOT / "ChillPatcher.MediaGenerator" / "ChillPatcher.MediaGenerator.csproj"
MEDIA_GEN_PUBLISH = ROOT / "ChillPatcher.MediaGenerator" / "bin" / "publish"

# ── 模块映射 (source_name, module_id) ──
PLAYER_MODULE_MAP = [
    ("LocalFolder", "com.chillpatcher.localfolder"),
    ("Netease", "com.chillpatcher.netease"),
    ("Bilibili", "com.chillpatcher.bilibili"),
    ("QQMusic", "com.chillpatcher.qqmusic"),
    ("Spotify", "com.chillpatcher.spotify"),
]

# ── 原生插件 ──
NATIVE_PLUGINS_DIR = ROOT / "NativePlugins"
NATIVE_PROJECTS_ALWAYS = [
    "AudioDecoder",
    "FlacDecoder",
    "OmniPcmShared",
    "SpotifyLibrespotBridge",
    "EsbuildBridge",
    "SmtcBridge",
]
NATIVE_PROJECTS_FULL_ONLY = [
    "netease_bridge",
    "qqmusic_bridge",
]

# ── FH6 桥接 ──
FH6_DIR = ROOT / "mods" / "ForzaHorizon6OmniBridge"
FH6_BIN = FH6_DIR / "bin" / "version.dll"
FH6_STAGE = ROOT / "release" / "FH6OmniBridge"
FH6_ZIP = ROOT / "release" / "FH6OmniBridge.zip"
FH6_FLUTTER_ASSETS = PLAYER_FLUTTER_DIR / "assets" / "FH6OmniBridge.zip"

# ── ChillPatcher Mod 打包 ──
MOD_RELEASE_ZIP = ROOT / "release" / "ChillPatcher.zip"
MOD_FLUTTER_ASSET = PLAYER_FLUTTER_DIR / "assets" / "ChillPatcher.zip"

OMNI_PCM_DLL = ROOT / "bin" / "native" / "x64" / "OmniPcmShared.dll"


# ════════════════════════════════════════════
#  工具函数
# ════════════════════════════════════════════

def info(msg: str):
    print(f"  {msg}")


def step(label: str, msg: str):
    print(f"\n[{label}] {msg}")


def run(cmd: list[str], cwd: Path | None = None, verbose: bool = False) -> int:
    """运行命令，实时打印输出，返回退出码。"""
    cmd_str = " ".join(str(c) for c in cmd)
    print(f"    > {cmd_str}")
    try:
        result = subprocess.run(cmd, cwd=cwd, shell=True, check=False)
    except Exception as e:
        print(f"    FAILED: {e}")
        return -1
    if result.returncode != 0:
        print(f"    FAILED (exit={result.returncode})")
    return result.returncode


def check_exists(path: Path, desc: str = "") -> bool:
    if not path.exists():
        info(f"  WARNING: {desc or path.name} not found: {path}")
        return False
    return True


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


def _copy_with(src: Path, dst_dir: Path):
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
                _copy_with(f, target.parent)
        else:
            _copy_with(item, dst)


# ════════════════════════════════════════════
#  原生插件构建
# ════════════════════════════════════════════

def _clean_cmake_cache(src: Path):
    """清理 CMake 缓存（路径变更后旧 cache 会报错）。"""
    cmake_build = src / "build"
    if cmake_build.exists():
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


def build_native_plugins(projects: list[str], verbose: bool = False):
    """逐个构建原生插件。"""
    for proj in projects:
        src = NATIVE_PLUGINS_DIR / proj
        build_script = src / "build.bat"
        if not build_script.exists():
            info(f"SKIP {proj}: no build.bat")
            continue
        step("native", f"Building {proj}...")
        _clean_cmake_cache(src)

        # netease_bridge 和 qqmusic_bridge 的 build.bat 有 pause，传 --no-pause 避免卡住
        args = ["build.bat"]
        if proj in ("netease_bridge", "qqmusic_bridge"):
            args.append("--no-pause")
        code = run(args, cwd=src, verbose=verbose)
        if code != 0:
            info(f"  WARNING: {proj} build failed (exit={code})")


# ════════════════════════════════════════════
#  C# 项目构建
# ════════════════════════════════════════════

def dotnet_restore(proj: Path, verbose: bool = False) -> int:
    info(f"Restoring {proj.name}...")
    return run(["dotnet", "restore", str(proj)], verbose=verbose)


def dotnet_build(proj: Path, config: str = "Release", verbose: bool = False) -> int:
    info(f"Building {proj.name} ({config})...")
    return run(["dotnet", "build", str(proj), "-c", config, "--no-restore"], verbose=verbose)

def dotnet_publish(proj: Path, output: Path, config: str = "Release", verbose: bool = False) -> int:
    """发布 .NET 项目（Self-Contained + Trimmed）。"""
    info(f"Publishing {proj.name} to {output.name}...")
    if output.exists():
        shutil.rmtree(output)
    return run([
        "dotnet", "publish", str(proj),
        "-c", config,
        "--no-restore",
        "-o", str(output),
        "--self-contained",
    ], verbose=verbose)


# ════════════════════════════════════════════
#  UI (esbuild) 构建
# ════════════════════════════════════════════

def build_ui(verbose: bool = False) -> bool:
    """构建 BepInEx 插件的 Preact UI (esbuild)。"""
    # 检查 npm 是否可用
    code = run(["where", "npm"], verbose=False)
    if code != 0:
        info("npm not found! Skipping esbuild bundling.")
        return False

    all_ok = True
    for ui_dir in MOD_UI_DIRS:
        name = ui_dir.name
        if not ui_dir.exists():
            info(f"SKIP {name}: directory not found")
            continue
        step("ui", f"Bundling {name} with esbuild...")
        # npm install if needed
        if not (ui_dir / "node_modules").exists():
            info(f"  Installing npm dependencies for {name}...")
            code = run(["npm", "install"], cwd=ui_dir, verbose=verbose)
            if code != 0:
                info(f"  ERROR: npm install failed for {name}")
                all_ok = False
                continue
        code = run(["npm", "run", "build"], cwd=ui_dir, verbose=verbose)
        if code != 0:
            info(f"  ERROR: esbuild build failed for {name}")
            all_ok = False
    return all_ok


# ════════════════════════════════════════════
#  构建 BepInEx Mod
# ════════════════════════════════════════════

def cmd_mod(full: bool = False, verbose: bool = False):
    """构建 ChillPatcher BepInEx 插件。"""
    print("=" * 50)
    print("Building ChillPatcher BepInEx Mod")
    print("=" * 50)
    if full:
        print("  [full mode: clean + restore + native plugins]")

    mod_bin = MOD_DIR / "bin"
    mod_obj = MOD_DIR / "obj"

    # ── Clean ──
    step("0/0", "Cleaning...")
    if MOD_RELEASE.exists():
        shutil.rmtree(MOD_RELEASE)
    if full:
        if mod_bin.exists():
            shutil.rmtree(mod_bin)
        if mod_obj.exists():
            shutil.rmtree(mod_obj)

    MOD_RELEASE.mkdir(parents=True, exist_ok=True)
    (MOD_RELEASE / "native" / "x64").mkdir(parents=True, exist_ok=True)
    (MOD_RELEASE / "SDK").mkdir(exist_ok=True)
    info("release/ChillPatcher cleaned")

    # ── Restore all (full mode) ──
    if full:
        step("1/8", "Restoring NuGet packages...")
        for proj in [MOD_SDK_PROJ, MOD_MAIN_PROJ, MOD_ONEJS_PROJ]:
            if proj.exists():
                if dotnet_restore(proj, verbose) != 0:
                    sys.exit(1)

    # ── SDK ──
    step("2/8", "Building ChillPatcher.SDK...")
    if dotnet_build(MOD_SDK_PROJ, verbose=verbose) != 0:
        sys.exit(1)

    # ── Main Plugin ──
    step("3/8", "Building ChillPatcher (Main Plugin)...")
    if dotnet_build(MOD_MAIN_PROJ, verbose=verbose) != 0:
        sys.exit(1)

    # ── OneJS ──
    step("4/8", "Building ChillPatcher.OneJS...")
    if dotnet_build(MOD_ONEJS_PROJ, verbose=verbose) != 0:
        sys.exit(1)

    # ── UI (esbuild) ──
    step("5/8", "Building UI (Preact + esbuild)...")
    build_ui(verbose)

    # ── Native Plugins (full mode only) ──
    if full:
        step("6/8", "Building Native Plugins...")
        build_native_plugins(NATIVE_PROJECTS_ALWAYS + NATIVE_PROJECTS_FULL_ONLY, verbose)
    else:
        info("Native plugins: SKIPPED (use --full to build)")

    # ── Assemble ──
    step("7/8", "Assembling release...")
    assemble_mod(verbose)

    # ── Package zip for Flutter assets ──
    _package_mod_zip()

    # ── Done ──
    print()
    print("=" * 50)
    print("Mod Build Complete!")
    print("=" * 50)
    print(f"  Output: {MOD_RELEASE}")
    print()


def assemble_mod(verbose: bool = False):
    """组装 BepInEx mod 的发布目录。"""
    mod_bin = MOD_DIR / "bin"

    # 主插件 DLL + config（SDK 由主插件引用自动带出，单独放 SDK/ 目录）
    for f in mod_bin.glob("ChillPatcher.*"):
        if f.suffix in (".dll", ".config") and "SDK" not in f.stem:
            _copy_with(f, MOD_RELEASE)

    # SDK（仅放在 SDK/ 子目录，仅 dll）
    sdk_bin = MOD_DIR / "bin" / "SDK"
    if sdk_bin.exists():
        for f in sdk_bin.glob("*.dll"):
            _copy_with(f, MOD_RELEASE / "SDK")

    # 原生插件 DLL（从根 bin/native/x64 复制，排除后端模块桥接）
    native_src = ROOT / "bin" / "native" / "x64"
    native_dst = MOD_RELEASE / "native" / "x64"
    native_exclude = {"ChillNetease.dll", "ChillQQMusic.dll"}
    if native_src.exists():
        native_dst.mkdir(parents=True, exist_ok=True)
        for f in native_src.glob("*.dll"):
            if f.name not in native_exclude:
                _copy_with(f, native_dst)
        info("  Native DLLs copied")

    # VC++ 运行时 DLL（放到 native/x64/，CoreDependencyLoader 从那里加载）
    lib_dir = MOD_DIR / "lib"
    if lib_dir.exists():
        for f in lib_dir.glob("*.dll"):
            _copy_with(f, native_dst)
        info("  VC++ runtime DLLs copied")

    # puerts.dll（OneJS V8 引擎，放到 native/x64/）
    puerts_src = MOD_DIR / "ChillPatcher.OneJS" / "native" / "x64" / "puerts.dll"
    if puerts_src.exists():
        _copy_with(puerts_src, native_dst)
        info("  puerts.dll copied")

    # RIME 输入法引擎
    rime_dir = NATIVE_PLUGINS_DIR / "rime"
    rime_dll = rime_dir / "librime" / "build" / "bin" / "Release" / "rime.dll"
    if rime_dll.exists():
        _copy_with(rime_dll, MOD_RELEASE)
        info("  RIME library copied")

    # RIME 词库数据
    rime_data = MOD_RELEASE / "rime-data"
    rime_shared = rime_data / "shared"
    rime_opencc = rime_shared / "opencc"
    rime_shared.mkdir(parents=True, exist_ok=True)
    rime_opencc.mkdir(parents=True, exist_ok=True)
    (rime_data / "user").mkdir(exist_ok=True)

    # prelude
    prelude = rime_dir / "rime-schemas" / "prelude"
    for f in ["symbols.yaml", "punctuation.yaml", "key_bindings.yaml"]:
        src = prelude / f
        if src.exists():
            _copy_with(src, rime_shared)

    # default config
    default_cfg = rime_dir / "RimeDefaultConfig"
    for f in ["default.yaml", "luna_pinyin.custom.yaml"]:
        src = default_cfg / f
        if src.exists():
            _copy_with(src, rime_shared)

    # essay
    essay = rime_dir / "rime-schemas" / "essay" / "essay.txt"
    if essay.exists():
        _copy_with(essay, rime_shared)

    # luna_pinyin schemas
    lp = rime_dir / "rime-schemas" / "luna-pinyin"
    for f in ["luna_pinyin.schema.yaml", "luna_pinyin.dict.yaml", "pinyin.yaml"]:
        src = lp / f
        if src.exists():
            _copy_with(src, rime_shared)

    # stroke
    st = rime_dir / "rime-schemas" / "stroke"
    for f in ["stroke.schema.yaml", "stroke.dict.yaml"]:
        src = st / f
        if src.exists():
            _copy_with(src, rime_shared)

    # double_pinyin
    dp = rime_dir / "rime-schemas" / "double-pinyin"
    for f in ["double_pinyin.schema.yaml", "double_pinyin_abc.schema.yaml",
              "double_pinyin_flypy.schema.yaml", "double_pinyin_mspy.schema.yaml"]:
        src = dp / f
        if src.exists():
            _copy_with(src, rime_shared)

    # OpenCC
    opencc_src = rime_dir / "librime" / "share" / "opencc"
    if opencc_src.exists():
        for f in opencc_src.glob("*.json"):
            _copy_with(f, rime_opencc)
        for f in opencc_src.glob("*.ocd2"):
            _copy_with(f, rime_opencc)

    info("Mod files assembled")


def _package_mod_zip():
    """打包 ChillPatcher mod 为 zip 并复制到 Flutter assets。"""
    step("pack", "Packaging ChillPatcher.zip...")
    if MOD_RELEASE.exists():
        if MOD_RELEASE_ZIP.exists():
            MOD_RELEASE_ZIP.unlink()
        shutil.make_archive(
            str(MOD_RELEASE_ZIP.with_suffix("")),
            "zip",
            MOD_RELEASE,
        )
        # 复制到 Flutter assets
        MOD_FLUTTER_ASSET.parent.mkdir(parents=True, exist_ok=True)
        _copy_with(MOD_RELEASE_ZIP, MOD_FLUTTER_ASSET.parent)
        info(f"  ChillPatcher.zip -> {MOD_FLUTTER_ASSET}")

        # 同步写入 version_info.json 到 Flutter assets
        cs_ver = "0.0.0"
        cs_file = MOD_DIR / "MyPluginInfo.cs"
        if cs_file.exists():
            m = re.search(r'PLUGIN_VERSION\s*=\s*"([^"]+)"', cs_file.read_text(encoding="utf-8"))
            if m:
                cs_ver = m.group(1)
        ver_data = {
            "mod_version": cs_ver,
            "build_time": datetime.now().isoformat(),
        }
        asset_ver = MOD_FLUTTER_ASSET.parent / "version_info.json"
        asset_ver.write_text(json.dumps(ver_data, indent=2), encoding="utf-8")
        info(f"  version_info.json -> {asset_ver}")
    else:
        info("  WARNING: MOD_RELEASE not found, skipping zip")


# ════════════════════════════════════════════
#  构建 OmniMixPlayer
# ════════════════════════════════════════════

def cmd_player(full: bool = False, skip_flutter: bool = False, verbose: bool = False):
    """构建 OmniMixPlayer。"""
    print("=" * 50)
    print("Building OmniMixPlayer")
    print("=" * 50)
    if full:
        print("  [full mode: clean + restore]")
    if skip_flutter:
        print("  [skipping Flutter]")

    # ── Clean ──
    step("0", "Cleaning playerbuild...")
    _rmtree_ignore_locked(PLAYER_BUILD)
    (PLAYER_BUILD / "modules").mkdir(parents=True)
    (PLAYER_BUILD / "native" / "x64").mkdir(parents=True)
    info("playerbuild cleaned")

    # ── Native builds ──
    step("native", "Building native plugins...")
    build_native_plugins(NATIVE_PROJECTS_ALWAYS, verbose)

    # ── C# builds ──
    step("1/8", "Building OmniMixPlayer.SDK...")
    if full:
        if dotnet_restore(PLAYER_SDK_PROJ, verbose) != 0:
            sys.exit(1)
    if dotnet_build(PLAYER_SDK_PROJ, verbose=verbose) != 0:
        sys.exit(1)

    step("2/8", "Publishing OmniMixPlayer.Backend (Single-File)...")
    if full:
        info("Restoring with RuntimeIdentifier win-x64...")
        code = run(["dotnet", "restore", str(PLAYER_BACKEND_PROJ), "--runtime", "win-x64"], verbose=verbose)
        if code != 0:
            sys.exit(1)

    if PLAYER_BACKEND_PUBLISH.exists():
        shutil.rmtree(PLAYER_BACKEND_PUBLISH)
    code = run([
        "dotnet", "publish", str(PLAYER_BACKEND_PROJ),
        "-c", "Release", "--no-restore",
        "-o", str(PLAYER_BACKEND_PUBLISH),
        "--self-contained",
        "-p:PublishSingleFile=true",
        "-p:PublishTrimmed=false",
        "-p:IncludeNativeLibrariesForSelfExtract=true",
    ], verbose=verbose)
    if code != 0:
        info("  FAILED: Backend publish failed")
        sys.exit(1)

    # Modules
    for i, (src_name, _) in enumerate(PLAYER_MODULE_MAP, start=3):
        step(f"{i}/8", f"Building module: {src_name}...")
        module_proj = PLAYER_DIR / "modules" / src_name / f"ChillPatcher.Module.{src_name}.csproj"
        if not module_proj.exists():
            info(f"  SKIP: project file not found")
            continue
        if full:
            if dotnet_restore(module_proj, verbose) != 0:
                sys.exit(1)
        if dotnet_build(module_proj, verbose=verbose) != 0:
            sys.exit(1)

    # ── MediaGenerator ──
    step("9/9", "Publishing MediaGenerator (single-file)...")
    if MEDIA_GEN_PUBLISH.exists():
        shutil.rmtree(MEDIA_GEN_PUBLISH)
    code = run([
        "dotnet", "publish", str(MEDIA_GEN_PROJ),
        "-c", "Release",
        "-o", str(MEDIA_GEN_PUBLISH),
        "--self-contained",
        "-p:PublishSingleFile=true",
        "-p:PublishTrimmed=false",
        "-p:IncludeNativeLibrariesForSelfExtract=true",
    ], verbose=verbose)
    if code != 0:
        info("  WARNING: MediaGenerator publish failed")

    # ── Assemble ──
    step("10/10", "Assembling playerbuild...")
    assemble_player(full, verbose)

    # ── FH6 asset ──
    step("fh6", "Packaging FH6 bridge assets...")
    if full:
        build_fh6_asset_internal(verbose)
    else:
        fh6_asset_package_only(verbose)

    # ── Flutter ──
    if skip_flutter:
        info("Flutter GUI: SKIPPED")
    else:
        step("flutter", "Building Flutter GUI...")
        code = run(["flutter", "build", "windows", "--release"], cwd=PLAYER_FLUTTER_DIR, verbose=verbose)
        if code != 0:
            info("  WARNING: Flutter build failed")
        else:
            copy_flutter(verbose)

    # ── Version info ──
    write_version_info()

    # ── Done ──
    print()
    print("=" * 50)
    print("Player Build Complete!")
    print("=" * 50)
    print(f"  Output: {PLAYER_BUILD}")
    print()


def assemble_player(full: bool = False, verbose: bool = False):
    """组装 playerbuild 目录。"""
    # Backend（从 publish 输出复制，已包含全部运行时）
    info("Backend...")
    if PLAYER_BACKEND_PUBLISH.exists():
        copy_dir_contents(PLAYER_BACKEND_PUBLISH, PLAYER_BUILD)
        _rmtree_ignore_locked(PLAYER_BUILD / "modules")
        info("  Backend published")
    else:
        info("  WARNING: Backend publish output not found")

    # Native decoders: only the two audio decoders (other DLLs belong to modules / other projects)
    native_src = ROOT / "bin" / "native" / "x64"
    if native_src.exists():
        native_dst = PLAYER_BUILD / "native" / "x64"
        native_dst.mkdir(parents=True, exist_ok=True)
        for dll in ["ChillAudioDecoder.dll", "ChillFlacDecoder.dll"]:
            src = native_src / dll
            if src.exists():
                _copy_with(src, native_dst)
        info("  Native decoders copied from bin/native/x64")

    # MediaGenerator（单文件发布，只复制 exe + 配置）
    info("MediaGenerator...")
    if MEDIA_GEN_PUBLISH.exists():
        for f in MEDIA_GEN_PUBLISH.glob("chill-gen-media.exe"):
            _copy_with(f, PLAYER_BUILD)
        for f in MEDIA_GEN_PUBLISH.glob("*.pdb"):
            _copy_with(f, PLAYER_BUILD)
        for cfg in ["config.json"]:
            src = MEDIA_GEN_PUBLISH / cfg
            if src.exists():
                _copy_with(src, PLAYER_BUILD)
        info("  MediaGenerator copied")

    # Modules
    info("Modules...")
    for src_name, module_id in PLAYER_MODULE_MAP:
        src_dir = PLAYER_MODULES_BUILD / src_name
        dst_dir = PLAYER_BUILD / "modules" / module_id
        if not src_dir.exists():
            info(f"  WARNING: Module output not found: {src_name}")
            continue
        info(f"  {src_name} -> modules/{module_id}/")
        dst_dir.mkdir(parents=True, exist_ok=True)
        for ext in ("*.dll", "*.json", "*.png"):
            for f in src_dir.glob(ext):
                _copy_with(f, dst_dir)
        # Native from build output
        for src_native in [src_dir / "native" / "x64"]:
            if src_native.exists():
                dst_native = dst_dir / "native" / "x64"
                dst_native.mkdir(parents=True, exist_ok=True)
                for f in src_native.iterdir():
                    if f.suffix in (".dll", ".exe"):
                        _copy_with(f, dst_native)
        # Native from source tree
        src_module = PLAYER_DIR / "modules" / src_name
        for src_native in [src_module / "native" / "x64"]:
            if src_native.exists():
                dst_native = dst_dir / "native" / "x64"
                dst_native.mkdir(parents=True, exist_ok=True)
                for f in src_native.iterdir():
                    if f.suffix in (".dll", ".exe"):
                        _copy_with(f, dst_native)

    # 清理构建输出的冗余文件
    _cleanup_playerbuild()


def _cleanup_playerbuild():
    """清理 playerbuild 中运行时不需要的文件。"""
    # runtimes/ - 有 .NET 运行时就不需要平台特定 DLL
    _rmtree_ignore_locked(PLAYER_BUILD / "runtimes")
    # *.pdb - 调试符号
    for f in PLAYER_BUILD.rglob("*.pdb"):
        try:
            f.unlink()
        except:
            pass
    # *.xml - 文档 XML
    for f in PLAYER_BUILD.rglob("*.xml"):
        try:
            f.unlink()
        except:
            pass
    # *.deps.json - 依赖清单（运行时不需要）
    for f in PLAYER_BUILD.rglob("*.deps.json"):
        try:
            f.unlink()
        except:
            pass
    # *.staticwebassets.* - ASP.NET 静态资源清单
    for f in PLAYER_BUILD.rglob("*.staticwebassets.*"):
        try:
            f.unlink()
        except:
            pass
    info("  Unnecessary files cleaned")


def copy_flutter(verbose: bool = False):
    """复制 Flutter 构建输出到 playerbuild。"""
    if not PLAYER_FLUTTER_BUILD.exists():
        info(f"  WARNING: Flutter build output not found")
        return
    for item in PLAYER_FLUTTER_BUILD.iterdir():
        dst = PLAYER_BUILD / item.name
        if item.is_dir():
            shutil.copytree(item, dst, dirs_exist_ok=True)
        else:
            _copy_with(item, PLAYER_BUILD)
    info("  Flutter GUI copied")


def write_version_info():
    """写入版本信息到 playerbuild 和 Flutter assets。"""
    # Flutter 版本
    pubspec = PLAYER_FLUTTER_DIR / "pubspec.yaml"
    flutter_ver = "0.0.0"
    if pubspec.exists():
        m = re.search(r'^version:\s*(\S+)', pubspec.read_text(encoding="utf-8"), re.M)
        if m:
            flutter_ver = m.group(1)

    # C# mod 版本
    cs_ver = "0.0.0"
    cs_file = MOD_DIR / "MyPluginInfo.cs"
    if cs_file.exists():
        m = re.search(r'PLUGIN_VERSION\s*=\s*"([^"]+)"', cs_file.read_text(encoding="utf-8"))
        if m:
            cs_ver = m.group(1)

    # FH6 bridge 版本
    fh6_ver = "0.0.0"
    fh6_file = FH6_DIR / "src" / "bridge.cpp"
    if fh6_file.exists():
        m = re.search(r'FH6_BRIDGE_VERSION\s+"([^"]+)"', fh6_file.read_text(encoding="utf-8"))
        if m:
            fh6_ver = m.group(1)

    version_data = {
        "flutter_version": flutter_ver,
        "mod_version": cs_ver,
        "fh6_bridge_version": fh6_ver,
        "build_time": datetime.now().isoformat(),
    }

    # 写入 playerbuild（运行时读取）
    dst = PLAYER_BUILD / "version_info.json"
    dst.write_text(json.dumps(version_data, indent=2), encoding="utf-8")

    # 写入 Flutter assets（打包到 app 内供部署时读取）
    asset_dst = PLAYER_FLUTTER_DIR / "assets" / "version_info.json"
    asset_dst.write_text(json.dumps(version_data, indent=2), encoding="utf-8")

    info(f"  Version info: mod={cs_ver} fh6_bridge={fh6_ver} flutter={flutter_ver}")


# ════════════════════════════════════════════
#  FH6 桥接资源打包
# ════════════════════════════════════════════

def cmd_fh6_asset(full: bool = False, verbose: bool = False):
    """打包 FH6 桥接 Flutter 资源。"""
    print("=" * 50)
    print("FH6 Omni Bridge Asset Packager")
    print("=" * 50)

    if full:
        # 构建原生插件
        step("1/3", "Building OmniPcmShared...")
        code = run(["build.bat"], cwd=NATIVE_PLUGINS_DIR / "OmniPcmShared", verbose=verbose)
        if code != 0:
            sys.exit(1)

        step("2/3", "Building Forza Horizon 6 Omni Bridge...")
        code = run(["build.bat"], cwd=FH6_DIR, verbose=verbose)
        if code != 0:
            sys.exit(1)
    else:
        step("1/3", "Native build skipped (use --full to rebuild)")

    fh6_asset_package_only(verbose)


def fh6_asset_package_only(verbose: bool = False):
    """仅打包（不构建）FH6 资源。"""
    step("3/3", "Packaging Flutter asset...")

    if not FH6_BIN.exists():
        info(f"  ERROR: Missing {FH6_BIN}")
        info("  Build FH6 bridge first: python scripts/build_all.py fh6-asset --full")
        return

    if not OMNI_PCM_DLL.exists():
        info(f"  WARNING: Missing {OMNI_PCM_DLL}")
        info("  Build OmniPcmShared first")

    # 打包
    if FH6_STAGE.exists():
        shutil.rmtree(FH6_STAGE)
    FH6_STAGE.mkdir(parents=True, exist_ok=True)

    _copy_with(FH6_BIN, FH6_STAGE)
    if OMNI_PCM_DLL.exists():
        _copy_with(OMNI_PCM_DLL, FH6_STAGE)
    readme = FH6_DIR / "README.md"
    if readme.exists():
        _copy_with(readme, FH6_STAGE)

    # 创建 zip
    if FH6_ZIP.exists():
        FH6_ZIP.unlink()
    FH6_FLUTTER_ASSETS.parent.mkdir(parents=True, exist_ok=True)
    shutil.make_archive(
        str(FH6_ZIP.with_suffix("")),  # 去掉 .zip 后缀
        "zip",
        FH6_STAGE,
    )

    # 复制到 Flutter assets
    _copy_with(FH6_ZIP, FH6_FLUTTER_ASSETS.parent)
    info(f"  Done: {FH6_FLUTTER_ASSETS}")


# ════════════════════════════════════════════
#  FH6 原生 Mod 构建
# ════════════════════════════════════════════

def cmd_fh6_mod(full: bool = False, verbose: bool = False):
    """构建 FH6 桥接原生 mod 到 release/FH6OmniBridge。"""
    print("=" * 50)
    print("Building FH6 Omni Bridge Mod")
    print("=" * 50)

    fh6_release = FH6_STAGE  # release/FH6OmniBridge

    # Clean
    step("0/3", "Cleaning...")
    if fh6_release.exists():
        shutil.rmtree(fh6_release)
    fh6_release.mkdir(parents=True)
    info("release/FH6OmniBridge cleaned")

    if full:
        step("1/3", "Building OmniPcmShared...")
        _clean_cmake_cache(NATIVE_PLUGINS_DIR / "OmniPcmShared")
        code = run(["build.bat"], cwd=NATIVE_PLUGINS_DIR / "OmniPcmShared", verbose=verbose)
        if code != 0:
            info("  WARNING: OmniPcmShared build failed")

        step("2/3", "Building Forza Horizon 6 Omni Bridge...")
        _clean_cmake_cache(FH6_DIR)
        code = run(["build.bat"], cwd=FH6_DIR, verbose=verbose)
        if code != 0:
            info("  WARNING: FH6 bridge build failed")
    else:
        info("Native builds: SKIPPED (use --full to rebuild)")

    step("3/3", "Assembling release...")
    if FH6_BIN.exists():
        _copy_with(FH6_BIN, fh6_release)
    else:
        info(f"  WARNING: Missing {FH6_BIN}")
    if OMNI_PCM_DLL.exists():
        _copy_with(OMNI_PCM_DLL, fh6_release)
    readme = FH6_DIR / "README.md"
    if readme.exists():
        _copy_with(readme, fh6_release)

    info("  FH6 mod files assembled")
    print()
    print("=" * 50)
    print("FH6 Mod Build Complete!")
    print("=" * 50)
    print(f"  Output: {fh6_release}")
    print()


def build_fh6_asset_internal(verbose: bool = False):
    """player 内部调用的 FH6 资源打包。"""
    # 构建 OmniPcmShared
    code = run(["build.bat"], cwd=NATIVE_PLUGINS_DIR / "OmniPcmShared", verbose=verbose)
    if code != 0:
        info("  WARNING: OmniPcmShared build failed")

    # 构建 FH6 bridge
    code = run(["build.bat"], cwd=FH6_DIR, verbose=verbose)
    if code != 0:
        info("  WARNING: FH6 bridge build failed")

    # 打包
    if FH6_STAGE.exists():
        shutil.rmtree(FH6_STAGE)
    FH6_STAGE.mkdir(parents=True, exist_ok=True)

    if FH6_BIN.exists():
        _copy_with(FH6_BIN, FH6_STAGE)
    else:
        info("  WARNING: FH6 version.dll not found")
    if OMNI_PCM_DLL.exists():
        _copy_with(OMNI_PCM_DLL, FH6_STAGE)

    if FH6_ZIP.exists():
        FH6_ZIP.unlink()
    FH6_FLUTTER_ASSETS.parent.mkdir(parents=True, exist_ok=True)
    shutil.make_archive(str(FH6_ZIP.with_suffix("")), "zip", FH6_STAGE)
    _copy_with(FH6_ZIP, FH6_FLUTTER_ASSETS.parent)
    info(f"  FH6 asset: {FH6_FLUTTER_ASSETS}")


# ════════════════════════════════════════════
#  Main
# ════════════════════════════════════════════

def main():
    parser = argparse.ArgumentParser(
        description="Chill 项目统一构建脚本",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
示例:
  python scripts/build_all.py mod
  python scripts/build_all.py player --full
  python scripts/build_all.py player --skip-flutter
  python scripts/build_all.py all --full
  python scripts/build_all.py fh6-asset --full
  python scripts/build_all.py fh6 --full
""",
    )
    parser.add_argument(
        "command",
        nargs="?",
        default="all",
        choices=["mod", "player", "fh6-asset", "fh6", "all"],
        help="构建目标",
    )
    parser.add_argument("--full", action="store_true", help="完整构建 (clean + restore + 原生插件)")
    parser.add_argument("--skip-flutter", action="store_true", help="跳过快闪 GUI 构建")
    parser.add_argument("--dry-run", action="store_true", help="只显示构建计划")
    parser.add_argument("--verbose", action="store_true", help="显示详细构建输出")

    args = parser.parse_args()

    if args.dry_run:
        print("=" * 50)
        print("DRY RUN - Build Plan")
        print("=" * 50)
        targets = []
        if args.command in ("mod", "all"):
            targets.append("  [mod]   ChillPatcher BepInEx 插件")
        if args.command in ("player", "all"):
            targets.append("  [player] OmniMixPlayer 播放器")
        if args.command in ("fh6-asset", "all"):
            targets.append("  [fh6-asset]  FH6 桥接资源打包")
        if args.command == "fh6":
            targets.append("  [fh6]   FH6 桥接原生 mod")
        for t in targets:
            print(t)
        print(f"\n  Options: full={args.full} skip-flutter={args.skip_flutter}")
        print()
        return

    if args.command in ("mod", "all"):
        cmd_mod(full=args.full, verbose=args.verbose)

    if args.command in ("player", "all"):
        cmd_player(full=args.full, skip_flutter=args.skip_flutter, verbose=args.verbose)

    if args.command == "fh6-asset":
        cmd_fh6_asset(full=args.full, verbose=args.verbose)

    if args.command == "fh6":
        cmd_fh6_mod(full=args.full, verbose=args.verbose)


if __name__ == "__main__":
    main()
