using System;
using NestopiSystem.Steam;

namespace ChillPatcher.Patches
{
    /// <summary>
    /// Steam 连接状态追踪器
    /// 在静默启动模式下，独立追踪真实的Steam连接状态
    /// （与游戏认为的 IsInitialized 区分开来）
    /// </summary>
    public static class SteamConnectionState
    {
        public enum State
        {
            /// <summary>尚未尝试初始化</summary>
            Unknown,
            /// <summary>初始化失败，等待Steam启动</summary>
            Pending,
            /// <summary>已成功连接到Steam</summary>
            Connected,
            /// <summary>硬件/DLL故障，无法连接</summary>
            Failed
        }

        /// <summary>当前Steam连接状态</summary>
        public static State CurrentState { get; private set; } = State.Unknown;

        /// <summary>Steam是否真正已初始化（区别于游戏认为的状态）</summary>
        public static bool IsSteamActuallyInitialized => CurrentState == State.Connected;

        /// <summary>SteamManager实例引用（用于后续重连时更新内部状态）</summary>
        internal static SteamManager ManagerInstance { get; set; }

        /// <summary>玩家Steam ID（连接后填充）</summary>
        public static string SteamUserId { get; private set; }

        /// <summary>玩家Steam昵称（连接后填充）</summary>
        public static string PersonaName { get; private set; }

        /// <summary>玩家是否拥有游戏（连接后填充）</summary>
        public static bool IsGameOwned { get; private set; }

        /// <summary>Steam成功连接时触发（可用于同步成就等操作）</summary>
        public static event Action OnSteamConnected;

        /// <summary>标记为等待状态（Steam未运行，继续尝试）</summary>
        public static void MarkPending()
        {
            CurrentState = State.Pending;
            SteamUserId = null;
            PersonaName = null;
            IsGameOwned = false;
            Plugin.Logger.LogInfo("[SteamState] 状态 → Pending（等待Steam启动）");
        }

        /// <summary>标记为硬件故障（DLL缺失等无法恢复的错误）</summary>
        public static void MarkFailed(string reason)
        {
            CurrentState = State.Failed;
            Plugin.Logger.LogError($"[SteamState] 状态 → Failed: {reason}");
        }

        /// <summary>标记为已连接，并刷新玩家信息</summary>
        public static void MarkConnected()
        {
            CurrentState = State.Connected;

            try
            {
                var steamId = Steamworks.SteamUser.GetSteamID();
                SteamUserId = steamId.IsValid() ? steamId.ToString() : null;
                PersonaName = Steamworks.SteamFriends.GetPersonaName();
                IsGameOwned = Steamworks.SteamApps.BIsSubscribed();

                Plugin.Logger.LogInfo(
                    $"[SteamState] 状态 → Connected: {PersonaName} ({SteamUserId})" +
                    $"，拥有游戏: {IsGameOwned}");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[SteamState] 获取玩家信息时出错: {ex.Message}");
            }

            OnSteamConnected?.Invoke();
        }

        /// <summary>重置状态（调试/测试用）</summary>
        public static void Reset()
        {
            CurrentState = State.Unknown;
            SteamUserId = null;
            PersonaName = null;
            IsGameOwned = false;
        }
    }
}
