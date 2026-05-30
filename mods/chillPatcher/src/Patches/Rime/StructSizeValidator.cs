using System;
using System.Runtime.InteropServices;

namespace ChillPatcher.Rime
{
    /// <summary>
    /// 验证 C# 结构体大小与 C++ 定义的对齐
    /// </summary>
    public static class StructSizeValidator
    {
        public static void ValidateStructSizes()
        {
            // 计算结构体实际大小
            int compositionSize = Marshal.SizeOf<RimeApi.RimeComposition>();
            int candidateSize = Marshal.SizeOf<RimeApi.RimeCandidate>();
            int menuSize = Marshal.SizeOf<RimeApi.RimeMenu>();
            int commitSize = Marshal.SizeOf<RimeApi.RimeCommit>();
            int contextSize = Marshal.SizeOf<RimeApi.RimeContext>();
            int statusSize = Marshal.SizeOf<RimeApi.RimeStatus>();

            Plugin.Logger.LogInfo("=== Rime 结构体大小验证 ===");
            
            // RimeComposition
            // C++: int(4) + int(4) + int(4) + int(4) + char*(8) = 24 字节 (x64)
            Plugin.Logger.LogInfo($"RimeComposition: {compositionSize} 字节 (预期: 24)");
            Plugin.Logger.LogInfo($"  - Length (int): offset 0, size 4");
            Plugin.Logger.LogInfo($"  - CursorPos (int): offset 4, size 4");
            Plugin.Logger.LogInfo($"  - SelStart (int): offset 8, size 4");
            Plugin.Logger.LogInfo($"  - SelEnd (int): offset 12, size 4");
            Plugin.Logger.LogInfo($"  - Preedit (IntPtr): offset 16, size 8");
            
            // RimeCandidate
            // C++: char*(8) + char*(8) + void*(8) = 24 字节
            Plugin.Logger.LogInfo($"RimeCandidate: {candidateSize} 字节 (预期: 24)");
            Plugin.Logger.LogInfo($"  - Text (IntPtr/string): offset 0, size 8");
            Plugin.Logger.LogInfo($"  - Comment (IntPtr/string): offset 8, size 8");
            Plugin.Logger.LogInfo($"  - Reserved (IntPtr): offset 16, size 8");
            
            // RimeMenu
            // C++: int(4) + int(4) + Bool/int(4) + int(4) + int(4) + padding(4) + RimeCandidate*(8) + char*(8) = 40 字节
            Plugin.Logger.LogInfo($"RimeMenu: {menuSize} 字节 (预期: 40)");
            Plugin.Logger.LogInfo($"  - PageSize (int): offset 0, size 4");
            Plugin.Logger.LogInfo($"  - PageNo (int): offset 4, size 4");
            Plugin.Logger.LogInfo($"  - IsLastPage (int): offset 8, size 4");
            Plugin.Logger.LogInfo($"  - HighlightedCandidateIndex (int): offset 12, size 4");
            Plugin.Logger.LogInfo($"  - NumCandidates (int): offset 16, size 4");
            Plugin.Logger.LogInfo($"  - [padding]: offset 20, size 4 (对齐到8字节边界)");
            Plugin.Logger.LogInfo($"  - Candidates (IntPtr): offset 24, size 8");
            Plugin.Logger.LogInfo($"  - SelectKeys (IntPtr/string): offset 32, size 8");
            
            // RimeCommit
            // C++: int(4) + padding(4) + char*(8) = 16 字节
            Plugin.Logger.LogInfo($"RimeCommit: {commitSize} 字节 (预期: 16)");
            Plugin.Logger.LogInfo($"  - DataSize (int): offset 0, size 4");
            Plugin.Logger.LogInfo($"  - [padding]: offset 4, size 4 (对齐到8字节边界)");
            Plugin.Logger.LogInfo($"  - Text (IntPtr): offset 8, size 8");
            
            // RimeContext
            // C++: int(4) + padding(4) + RimeComposition(24) + RimeMenu(40) + char*(8) + char**(8) = 88 字节
            Plugin.Logger.LogInfo($"RimeContext: {contextSize} 字节 (预期: 88)");
            Plugin.Logger.LogInfo($"  - DataSize (int): offset 0, size 4");
            Plugin.Logger.LogInfo($"  - [padding]: offset 4, size 4");
            Plugin.Logger.LogInfo($"  - Composition (struct): offset 8, size 24");
            Plugin.Logger.LogInfo($"  - Menu (struct): offset 32, size 40");
            Plugin.Logger.LogInfo($"  - CommitTextPreview (IntPtr): offset 72, size 8");
            Plugin.Logger.LogInfo($"  - SelectLabels (IntPtr): offset 80, size 8");
            
            // RimeStatus
            // C++: int(4) + padding(4) + char*(8) + char*(8) + 7*Bool/int(28) + padding(4) = 56 字节
            Plugin.Logger.LogInfo($"RimeStatus: {statusSize} 字节 (预期: 56)");
            Plugin.Logger.LogInfo($"  - DataSize (int): offset 0, size 4");
            Plugin.Logger.LogInfo($"  - [padding]: offset 4, size 4");
            Plugin.Logger.LogInfo($"  - SchemaId (IntPtr): offset 8, size 8");
            Plugin.Logger.LogInfo($"  - SchemaName (IntPtr): offset 16, size 8");
            Plugin.Logger.LogInfo($"  - IsDisabled (int): offset 24, size 4");
            Plugin.Logger.LogInfo($"  - IsComposing (int): offset 28, size 4");
            Plugin.Logger.LogInfo($"  - IsAsciiMode (int): offset 32, size 4");
            Plugin.Logger.LogInfo($"  - IsFullShape (int): offset 36, size 4");
            Plugin.Logger.LogInfo($"  - IsSimplified (int): offset 40, size 4");
            Plugin.Logger.LogInfo($"  - IsTraditional (int): offset 44, size 4");
            Plugin.Logger.LogInfo($"  - IsAsciiPunct (int): offset 48, size 4");
            
            Plugin.Logger.LogInfo("=== 缓冲区余量检查 ===");
            Plugin.Logger.LogInfo($"GetContext 缓冲区: 512 字节, 实际需要: {contextSize} 字节, 余量: {512 - contextSize} 字节");
            Plugin.Logger.LogInfo($"GetCommit 缓冲区: 256 字节, 实际需要: {commitSize} 字节, 余量: {256 - commitSize} 字节");
            Plugin.Logger.LogInfo($"GetStatus 缓冲区: 256 字节, 实际需要: {statusSize} 字节, 余量: {256 - statusSize} 字节");
            
            // 验证对齐
            if (contextSize > 512)
            {
                Plugin.Logger.LogError($"[严重] GetContext 缓冲区不足！需要 {contextSize} 字节，当前只有 512 字节");
            }
            else if (contextSize > 400)
            {
                Plugin.Logger.LogWarning($"[警告] GetContext 缓冲区余量较少，建议增大到 1024 字节");
            }
            else
            {
                Plugin.Logger.LogInfo($"[正常] GetContext 缓冲区余量充足 ({512 - contextSize} 字节)");
            }
            
            if (commitSize != 16 || contextSize != 88 || statusSize != 56)
            {
                Plugin.Logger.LogWarning("[警告] 结构体大小与预期不符，可能存在对齐问题！");
                Plugin.Logger.LogWarning("请检查 StructLayout 设置和字段类型");
            }
            else
            {
                Plugin.Logger.LogInfo("[正常] 所有结构体大小符合预期");
            }
        }
        
        /// <summary>
        /// 输出结构体字段偏移量（用于调试）
        /// </summary>
        public static void DumpFieldOffsets()
        {
            Plugin.Logger.LogInfo("=== RimeContext 字段偏移量 ===");
            Plugin.Logger.LogInfo($"DataSize: {Marshal.OffsetOf<RimeApi.RimeContext>("DataSize")}");
            Plugin.Logger.LogInfo($"Composition: {Marshal.OffsetOf<RimeApi.RimeContext>("Composition")}");
            Plugin.Logger.LogInfo($"Menu: {Marshal.OffsetOf<RimeApi.RimeContext>("Menu")}");
            Plugin.Logger.LogInfo($"CommitTextPreview: {Marshal.OffsetOf<RimeApi.RimeContext>("CommitTextPreview")}");
            Plugin.Logger.LogInfo($"SelectLabels: {Marshal.OffsetOf<RimeApi.RimeContext>("SelectLabels")}");
        }
    }
}
