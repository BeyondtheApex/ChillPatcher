import os

# 1. 定义主人指定的 6 个源文件夹
TARGET_SOURCES = [
    "ChillPatcher.Module.Bilibili",
    "ChillPatcher.Module.LocalFolder",
    "ChillPatcher.Module.Netease",
    "ChillPatcher.Module.QQMusic",
    "ChillPatcher.Module.Spotify",
    "ChillPatcher.SDK"
]

# 2. 定义目标文件夹
DEST_DIR = "OmniMixPlayer"

def get_cs_files_from_folders(folders):
    """获取指定文件夹列表下所有的 .cs 文件名（不含路径）"""
    cs_files = set()
    for folder in folders:
        if os.path.exists(folder):
            for root, _, files in os.walk(folder):
                for file in files:
                    if file.endswith('.cs'):
                        cs_files.add(file)
        else:
            print(f"⚠️ 警告：未找到源文件夹 {folder}")
    return cs_files

def get_cs_files_from_dest(dest):
    """获取目标文件夹下所有的 .cs 文件名（不含路径）"""
    cs_files = set()
    if os.path.exists(dest):
        for root, _, files in os.walk(dest):
            for file in files:
                if file.endswith('.cs'):
                    cs_files.add(file)
    else:
        print(f"❌ 错误：未找到目标文件夹 {dest}")
    return cs_files

def main():
    print("🔍 开始比对 .cs 文件...")
    
    # 收集源文件和目标文件
    source_files = get_cs_files_from_folders(TARGET_SOURCES)
    dest_files = get_cs_files_from_dest(DEST_DIR)
    
    print(f"📊 指定的源文件夹中共有 {len(source_files)} 个 .cs 文件")
    print(f"📊 目标文件夹 {DEST_DIR} 中共有 {len(dest_files)} 个 .cs 文件")
    print("-" * 50)

    # 找出在源文件夹中存在，但目标文件夹中缺失的文件
    missing_files = source_files - dest_files

    if not missing_files:
        print("✅ 恭喜主人！所有指定模块的 .cs 文件已成功复制到 OmniMixPlayer 中！")
    else:
        print(f"❌ 发现有 {len(missing_files)} 个 .cs 文件未复制成功：")
        for file in sorted(missing_files):
            print(f"   - {file}")

if __name__ == "__main__":
    main()