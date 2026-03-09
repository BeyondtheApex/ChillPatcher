using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;  // 添加此行用于 Marshal

namespace ChillPatcher.Rime
{
    /// <summary>
    /// Rime引擎封装类
    /// </summary>
    public class RimeEngine : IDisposable
    {
        private ulong _sessionId;
        private bool _initialized;
        private bool _disposed;

        public ulong SessionId => _sessionId;
        public bool IsInitialized => _initialized;

        /// <summary>
        /// 检查 build 目录是否有被锁定的文件
        /// </summary>
        private bool IsBuildDirectoryLocked(string userDataDir)
        {
            string buildDir = Path.Combine(userDataDir, "build");
            if (!Directory.Exists(buildDir))
                return false;

            try
            {
                foreach (string file in Directory.GetFiles(buildDir))
                {
                    try
                    {
                        // 尝试以独占方式打开，检测文件是否被锁定
                        using (new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                        { }
                    }
                    catch
                    {
                        // 尝试移除只读属性后再删除
                        try
                        {
                            var attrs = File.GetAttributes(file);
                            if ((attrs & FileAttributes.ReadOnly) != 0)
                                File.SetAttributes(file, attrs & ~FileAttributes.ReadOnly);
                            File.Delete(file);
                        }
                        catch
                        {
                            Plugin.Logger.LogWarning($"[Rime] 文件被锁定: {Path.GetFileName(file)}");
                            return true;
                        }
                    }
                }
            }
            catch
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 寻找一个 build 目录未被锁定的可用 userDataDir。
        /// 如果原始目录被锁，顺延到 user_1, user_2, ...，并把配置文件拷贝过去。
        /// 同时尽力清理以前残留的旧目录。
        /// </summary>
        private string ResolveAvailableUserDataDir(string userDataDir)
        {
            // 先尝试清理旧的顺延目录（重启后锁释放了就可以删掉）
            CleanupOldUserDirs(userDataDir);

            // 检查原始目录
            if (!IsBuildDirectoryLocked(userDataDir))
                return userDataDir;

            Plugin.Logger.LogWarning($"[Rime] {userDataDir} 的 build 目录被锁定，寻找可用目录...");

            string baseDir = Path.GetDirectoryName(userDataDir);
            string baseName = Path.GetFileName(userDataDir);

            for (int i = 1; i <= 100; i++)
            {
                string candidate = Path.Combine(baseDir, $"{baseName}_{i}");
                if (!IsBuildDirectoryLocked(candidate))
                {
                    Directory.CreateDirectory(candidate);
                    // 拷贝配置文件（yaml、txt等，不拷贝 build 和 userdb 子目录）
                    CopyUserConfigs(userDataDir, candidate);
                    Plugin.Logger.LogInfo($"[Rime] 使用替代目录: {candidate}");
                    return candidate;
                }
            }

            // 不太可能到这里，但保底
            Plugin.Logger.LogError("[Rime] 无法找到可用的 user 目录，尝试使用原始目录");
            return userDataDir;
        }

        /// <summary>
        /// 将原始 user 目录的配置文件拷贝到新目录（仅拷贝根目录下的文件）
        /// </summary>
        private void CopyUserConfigs(string srcDir, string dstDir)
        {
            try
            {
                foreach (string file in Directory.GetFiles(srcDir))
                {
                    string destFile = Path.Combine(dstDir, Path.GetFileName(file));
                    if (!File.Exists(destFile))
                    {
                        try
                        {
                            File.Copy(file, destFile, false);
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[Rime] 拷贝配置文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 尝试清理之前顺延产生的旧 user_N 目录（重启后文件锁释放即可删除）
        /// </summary>
        private void CleanupOldUserDirs(string userDataDir)
        {
            string baseDir = Path.GetDirectoryName(userDataDir);
            string baseName = Path.GetFileName(userDataDir);

            try
            {
                foreach (string dir in Directory.GetDirectories(baseDir, $"{baseName}_*"))
                {
                    // 确保是我们生成的数字后缀目录
                    string suffix = Path.GetFileName(dir).Substring(baseName.Length + 1);
                    if (!int.TryParse(suffix, out _))
                        continue;

                    try
                    {
                        Directory.Delete(dir, true);
                        Plugin.Logger.LogInfo($"[Rime] 已清理旧目录: {Path.GetFileName(dir)}");
                    }
                    catch
                    {
                        // 删不掉就算了，可能还在被占用
                    }
                }
            }
            catch { }
        }

        public void Initialize(string sharedDataDir, string userDataDir, string appName = "rime.chill")
        {
            if (_initialized)
                throw new InvalidOperationException("Rime引擎已经初始化");

            Directory.CreateDirectory(sharedDataDir);
            Directory.CreateDirectory(userDataDir);

            // 检测 build 目录锁定，必要时切换到可用的 user 目录
            userDataDir = ResolveAvailableUserDataDir(userDataDir);

            // 创建日志目录
            string logDir = Path.Combine(userDataDir, "logs");
            Directory.CreateDirectory(logDir);
            
            // 创建 OpenCC 数据目录（在 shared 目录下）
            string openccDir = Path.Combine(sharedDataDir, "opencc");
            Directory.CreateDirectory(openccDir);

            var traits = RimeApi.RimeTraits.Create();
            traits.SharedDataDir = sharedDataDir;
            traits.UserDataDir = userDataDir;
            traits.AppName = appName;
            traits.DistributionName = "ChillPatcher";
            traits.DistributionCodeName = "chill";
            traits.DistributionVersion = "1.0.0";
            traits.LogDir = logDir;           // 设置日志目录
            traits.MinLogLevel = 2;           // 0=INFO, 1=WARNING, 2=ERROR, 3=FATAL
            traits.PrebuiltDataDir = sharedDataDir;  // OpenCC 从 shared 目录下查找

            RimeApi.RimeSetup(ref traits);
            
            // 初始化部署器,加载部署模块
            Plugin.Logger.LogInfo("[Rime] 初始化部署器...");
            RimeApi.RimeDeployerInitialize(ref traits);
            
            // 执行部署,构建用户数据
            Plugin.Logger.LogInfo("[Rime] 开始部署工作空间...");
            if (RimeApi.RimeDeployWorkspace())
            {
                Plugin.Logger.LogInfo("[Rime] 工作空间部署成功");
            }
            else
            {
                Plugin.Logger.LogWarning("[Rime] 工作空间部署失败或已是最新");
            }
            
            // 初始化Rime引擎
            Plugin.Logger.LogInfo("[Rime] 初始化引擎...");
            RimeApi.RimeInitialize(ref traits);
            RimeApi.RimeJoinMaintenanceThread();

            _sessionId = RimeApi.RimeCreateSession();
            if (_sessionId == 0)
                throw new Exception("创建Rime Session失败");

            // 检查状态（使用手动内存管理）
            IntPtr statusBuffer = IntPtr.Zero;
            try
            {
                int bufferSize = 256;
                statusBuffer = Marshal.AllocHGlobal(bufferSize);
                
                // 清零内存
                unsafe
                {
                    byte* p = (byte*)statusBuffer;
                    for (int i = 0; i < bufferSize; i++) p[i] = 0;
                }
                
                // 写入 DataSize
                int structSize = Marshal.SizeOf<RimeApi.RimeStatus>();
                Marshal.WriteInt32(statusBuffer, structSize - sizeof(int));
                
                if (RimeApi.RimeGetStatus(_sessionId, statusBuffer))
                {
                    var status = Marshal.PtrToStructure<RimeApi.RimeStatus>(statusBuffer);
                    
                    string schemaId = status.SchemaId != IntPtr.Zero ? Marshal.PtrToStringAnsi(status.SchemaId) : "null";
                    string schemaName = status.SchemaName != IntPtr.Zero ? Marshal.PtrToStringAnsi(status.SchemaName) : "null";
                    Plugin.Logger.LogInfo($"[Rime] 初始化状态检查成功 - Schema: {schemaId}/{schemaName}, ASCII: {status.IsAsciiMode}, Composing: {status.IsComposing}");
                    
                    // 释放 Rime 内部分配的内存
                    if (status.SchemaId != IntPtr.Zero || status.SchemaName != IntPtr.Zero)
                    {
                        RimeApi.RimeFreeStatus(statusBuffer);
                    }
                }
                else
                {
                    Plugin.Logger.LogWarning("[Rime] 初始化状态检查失败");
                }
            }
            finally
            {
                if (statusBuffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(statusBuffer);
                }
            }

            _initialized = true;
            Plugin.Logger.LogInfo($"[Rime] 引擎初始化成功, SessionID={_sessionId}");
            Plugin.Logger.LogInfo($"[Rime] 日志目录: {logDir}");
        }

        public bool ProcessKey(int keyCode, int modifiers = 0)
        {
            if (!_initialized) return false;
            
            try
            {
                return RimeApi.RimeProcessKey(_sessionId, keyCode, modifiers);
            }
            catch (AccessViolationException ex)
            {
                Plugin.Logger.LogError($"[Rime] ProcessKey内存访问异常: {ex.Message}");
                _initialized = false; // 标记为未初始化,触发重启
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[Rime] ProcessKey异常(已隔离): {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        public RimeContextInfo GetContext()
        {
            if (!_initialized) return null;

            IntPtr buffer = IntPtr.Zero;
            try
            {
                // 手动分配堆内存（512字节，足够容纳结构体并留出安全边界）
                int bufferSize = 512;
                buffer = Marshal.AllocHGlobal(bufferSize);
                
                // 清零内存
                unsafe
                {
                    byte* p = (byte*)buffer;
                    for (int i = 0; i < bufferSize; i++) p[i] = 0;
                }
                
                // 写入 DataSize（Rime API 需要这个字段来识别结构体版本）
                int structSize = Marshal.SizeOf<RimeApi.RimeContext>();
                Marshal.WriteInt32(buffer, structSize - sizeof(int));
                
                // 调用 Rime API，传递指针而非 ref struct
                if (!RimeApi.RimeGetContext(_sessionId, buffer))
                {
                    return null; // 静默失败,避免日志刷屏
                }

                // 从内存读取结构体
                var context = Marshal.PtrToStructure<RimeApi.RimeContext>(buffer);

                try
                {
                    var info = new RimeContextInfo
                    {
                        // 手动 marshal IntPtr → string
                        Preedit = context.Composition.Preedit != IntPtr.Zero 
                            ? Marshal.PtrToStringAnsi(context.Composition.Preedit) ?? string.Empty 
                            : string.Empty,
                        CursorPos = context.Composition.CursorPos,
                        HighlightedIndex = context.Menu.HighlightedCandidateIndex,
                        Candidates = new List<CandidateInfo>()
                    };

                    // 边界检查（防御性编程，防止内存越界）
                    int numCandidates = context.Menu.NumCandidates;
                    
                    if (numCandidates < 0)
                    {
                        Plugin.Logger.LogWarning($"[Rime] 负数候选词: {numCandidates}，已重置为 0");
                        numCandidates = 0;
                    }
                    else if (numCandidates > 10)  // 10 覆盖所有常见配置（默认 5，最大 9）
                    {
                        Plugin.Logger.LogWarning($"[Rime] 候选词过多: {numCandidates}，截断为 10");
                        numCandidates = 10;
                    }
                    
                    // 指针有效性检查
                    if (numCandidates > 0 && context.Menu.Candidates == IntPtr.Zero)
                    {
                        Plugin.Logger.LogWarning($"[Rime] 候选词指针为空但数量为 {numCandidates}，已重置为 0");
                        numCandidates = 0;
                    }

                    var candidates = RimeApi.GetCandidates(
                        context.Menu.Candidates,
                        numCandidates);  // 使用检查后的安全值

                    foreach (var candidate in candidates)
                    {
                        info.Candidates.Add(new CandidateInfo
                        {
                            Text = RimeApi.GetCandidateText(candidate),
                            Comment = RimeApi.GetCandidateComment(candidate)
                        });
                    }

                    return info;
                }
                finally
                {
                    // 释放 Rime 内部分配的内存（preedit、candidates等）
                    RimeApi.RimeFreeContext(buffer);
                }
            }
            catch (AccessViolationException ex)
            {
                Plugin.Logger.LogError($"[Rime] GetContext内存访问异常: {ex.Message}");
                _initialized = false; // 标记为未初始化,触发重启
                return null;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[Rime] GetContext异常(已隔离): {ex.GetType().Name}: {ex.Message}");
                return null;
            }
            finally
            {
                // 释放我们手动分配的堆内存
                if (buffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }

        public void SetOption(string option, bool value)
        {
            if (!_initialized) return;
            RimeApi.RimeSetOption(_sessionId, option, value);
        }

        public bool GetOption(string option)
        {
            if (!_initialized) return false;
            return RimeApi.RimeGetOption(_sessionId, option);
        }

        public void ToggleAsciiMode()
        {
            if (!_initialized) return;
            bool current = GetOption("ascii_mode");
            SetOption("ascii_mode", !current);
            Plugin.Logger.LogInfo($"[Rime] ASCII 模式: {!current}");
        }

        public string GetCommit()
        {
            if (!_initialized) return null;

            IntPtr buffer = IntPtr.Zero;
            try
            {
                // 手动分配堆内存
                int bufferSize = 256;
                buffer = Marshal.AllocHGlobal(bufferSize);
                
                // 清零内存
                unsafe
                {
                    byte* p = (byte*)buffer;
                    for (int i = 0; i < bufferSize; i++) p[i] = 0;
                }
                
                // 写入 DataSize
                int structSize = Marshal.SizeOf<RimeApi.RimeCommit>();
                Marshal.WriteInt32(buffer, structSize - sizeof(int));
                
                if (!RimeApi.RimeGetCommit(_sessionId, buffer))
                    return null;

                // 从内存读取结构体
                var commit = Marshal.PtrToStructure<RimeApi.RimeCommit>(buffer);

                try
                {
                    // 手动 marshal IntPtr → string
                    if (commit.Text == IntPtr.Zero)
                        return null;
                    
                    return Marshal.PtrToStringAnsi(commit.Text);
                }
                finally
                {
                    // 释放 Rime 内部分配的内存
                    RimeApi.RimeFreeCommit(buffer);
                }
            }
            catch (AccessViolationException ex)
            {
                Plugin.Logger.LogError($"[Rime] GetCommit内存访问异常: {ex.Message}");
                _initialized = false;
                return null;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[Rime] GetCommit异常(已隔离): {ex.GetType().Name}: {ex.Message}");
                return null;
            }
            finally
            {
                // 释放我们手动分配的堆内存
                if (buffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }

        public void ClearComposition()
        {
            if (!_initialized) return;
            
            try
            {
                RimeApi.RimeClearComposition(_sessionId);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[Rime] ClearComposition异常(已隔离): {ex.Message}");
            }
        }

        public bool SelectCandidate(int index)
        {
            if (!_initialized) return false;
            return RimeApi.RimeSelectCandidateOnCurrentPage(_sessionId, (nuint)index);
        }

        /// <summary>
        /// 重新部署并重启会话(F6热重载配置)
        /// </summary>
        public void Redeploy()
        {
            if (!_initialized)
            {
                Plugin.Logger.LogWarning("[Rime] 引擎未初始化,无法重新部署");
                return;
            }

            Plugin.Logger.LogInfo("[Rime] 开始重新部署...");

            // 1. 销毁当前会话
            if (_sessionId != 0)
            {
                RimeApi.RimeDestroySession(_sessionId);
                _sessionId = 0;
            }

            // 2. 重新部署(重新编译schema)
            if (RimeApi.RimeDeployWorkspace())
            {
                Plugin.Logger.LogInfo("[Rime] 重新部署成功");
            }
            else
            {
                Plugin.Logger.LogWarning("[Rime] 重新部署失败");
            }

            // 3. 重新创建会话
            _sessionId = RimeApi.RimeCreateSession();
            if (_sessionId == 0)
            {
                Plugin.Logger.LogError("[Rime] 重新创建Session失败");
                return;
            }

            // 4. 重新选择schema
            if (!RimeApi.RimeSelectSchema(_sessionId, "luna_pinyin"))
            {
                Plugin.Logger.LogWarning("[Rime] 重新选择schema失败");
            }

            // 5. 重新设置选项
            RimeApi.RimeSetOption(_sessionId, "ascii_mode", false);

            Plugin.Logger.LogInfo("[Rime] 重新部署完成,会话已重启");
        }

        public void Dispose()
        {
            if (_disposed) return;

            if (_initialized)
            {
                if (_sessionId != 0)
                {
                    RimeApi.RimeDestroySession(_sessionId);
                    _sessionId = 0;
                }

                RimeApi.RimeFinalize();
                _initialized = false;
                Plugin.Logger.LogInfo("[Rime] 引擎已释放");
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// 上下文信息
    /// </summary>
    public class RimeContextInfo
    {
        public string Preedit { get; set; }
        public int CursorPos { get; set; }
        public int HighlightedIndex { get; set; }
        public List<CandidateInfo> Candidates { get; set; }

        /// <summary>
        /// 生成在preedit区域显示的文本
        /// 格式: "preedit [候选词1₁ 候选词2₂ 候选词3₃]"
        /// 选中的候选用上标,未选中用下标
        /// </summary>
        public string GetPreeditWithCandidates()
        {
            if (string.IsNullOrEmpty(Preedit))
                return string.Empty;

            if (Candidates == null || Candidates.Count == 0)
                return Preedit;

            var result = Preedit + " [";

            for (int i = 0; i < Math.Min(Candidates.Count, 9); i++)
            {
                if (i > 0) result += " "; // 候选词之间添加空格

                var candidate = Candidates[i];
                result += candidate.Text;
                
                // 选中的用上标,未选中用下标
                bool isHighlighted = (i == HighlightedIndex);
                result += isHighlighted ? GetSuperscriptNumber(i + 1) : GetSubscriptNumber(i + 1);

                if (!string.IsNullOrEmpty(candidate.Comment))
                    result += $"({candidate.Comment})";
            }

            result += "]";
            return result;
        }

        private static string GetSuperscriptNumber(int num)
        {
            return num switch
            {
                1 => "¹",
                2 => "²",
                3 => "³",
                4 => "⁴",
                5 => "⁵",
                6 => "⁶",
                7 => "⁷",
                8 => "⁸",
                9 => "⁹",
                _ => num.ToString()
            };
        }

        private static string GetSubscriptNumber(int num)
        {
            return num switch
            {
                1 => "₁",
                2 => "₂",
                3 => "₃",
                4 => "₄",
                5 => "₅",
                6 => "₆",
                7 => "₇",
                8 => "₈",
                9 => "₉",
                _ => num.ToString()
            };
        }
    }

    /// <summary>
    /// 候选词信息
    /// </summary>
    public class CandidateInfo
    {
        public string Text { get; set; }
        public string Comment { get; set; }
    }
}
