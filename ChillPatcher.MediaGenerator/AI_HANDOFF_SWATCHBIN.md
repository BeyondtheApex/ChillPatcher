## 背景

ChillPatcher.MediaGenerator 是一个 C# CLI 工具，自动生成 FH6 电台 Mod 的 media overlay。已完成:

- FSB5 Bank 解析 + 静音 PCM16 生成 (BankGenerator.cs)
- RadioInfo XML 修改——劫持 Streamer Mode (RadioInfoModifier.cs)
- Anthem.zip 纹理替换——**待你完成**

项目位置: `G:\Csharp\Chill\ChillPatcher.MediaGenerator\`
接口签名: `AnthemHandler.Process(ConfigModel config, string outputDir)`

## 关键发现

### ZIP 是部分覆盖 (partial overlay)

- 原始 Anthem.zip: 1696 个文件
- fh6-universal-radio 修改版: 1551 个文件 (删除了无关的，只保留需要的)
- 游戏可能以 merge/overlay 模式加载 ZIP

### Streamer_Mode.swatchbin 替换策略

修改版直接用了 `Horizon_Pulse.swatchbin` (20,524 bytes) 覆盖了 `Streamer_Mode.swatchbin` (52,160 bytes)。
两者 `burG` 头部相同，只有尺寸字段不同。**最简单方案: 复制某个现有电台的 swatchbin。**

### SwatchBin 格式 (从对比多个文件得出)

```
Offset  | 内容
0x00    | 62 75 72 47 = "burG" magic
0x04    | 01 01 00 00 (常量)
0x08    | header/pitch 大小 (如 0x8C)
0x0C    | 文件总大小 (如 0x502C = 20524)
0x10    | chunk 数量 (通常是 1)
0x14    | "BCXT" chunk (纹理元数据 + DXT/BC 压缩像素)
可选    | "HCXT" chunk (多分辨率纹理时出现)
```

### 需要替换的 ZIP 内路径

任意,但是支持使用路径和参数控制替换目标和输入
HiRes 版对应相同路径 (像素 4x)。

## 你的任务

### 1. 逆向 SwatchBin 格式

解析 BCXT/HCXT 内部结构: width/height/pitch/format/mipmaps
对比多个 swatchbin 验证理解 (Horizon_Pulse, Horizon_BassArena 等)

### 2. 实现 AnthemHandler.Process()

解压 ZIP -> 替换目标 swatchbin -> 重新打包
同时处理普通版和 HiRes 版
简单方案: 复制现有 Logo; 高级方案: PNG -> swatchbin 转换

### 3. config.json 扩展建议

```json
{
  "anthemZip": {
    "enabled": true,
    "copyLogoFrom": "HUD/RadioLogos/Horizon_Pulse.swatchbin",
    "targetSwatchBin": "HUD/RadioLogos/Streamer_Mode.swatchbin",
    "mode": "partial"
  }
}
```

### 4. 知道G:\Csharp\Chill\game\radio\media\UI\ 替换了什么

和游戏E:\SteamLibrary\steamapps\common\ForzaHorizon6原文件比较
获取一个列表,我需要知道这个ui包替换了哪些ui

## 资源路径

- 原始 Anthem: `E:\SteamLibrary\steamapps\common\ForzaHorizon6\media\UI\Textures\`
- 修改参考: `G:\Csharp\Chill\NativePlugins\fh6-universal-radio\dist\media.generated\UI\Textures\`
- SwatchBin 相关都在 Anthem.zip 内 `HUD/RadioLogos/` 和 `Icons/` 下
- 可对比 Horizon_Pulse.swatchbin (20KB) 和 Streamer_Mode.swatchbin (52KB) 来理解格式差异
