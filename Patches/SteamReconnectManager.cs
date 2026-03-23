using System;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace ChillPatcher.Patches
{
    /// <summary>
    /// Steam 重连管理器（壁纸引擎模式专用）
    ///
    /// 设计原则：
    ///   - ThreadPool 定时器：仅调用 SteamAPI.IsSteamRunning()，极轻量
    ///   - 检测到 Steam 后只设置 volatile bool 旗标，不做任何 Steam API 操作
    ///   - 主线程 Update() 读取旗标，执行 SteamAPI.Init() 及后续操作
    ///   - 成就同步请求通过 AchievementSyncManager 旗标，同样在主线程执行
    /// </summary>
    public static class SteamReconnectManager
    {
        private const int InitialDelayMs = 5000;   // 首次检查延迟 5 秒
        private const int CheckIntervalMs = 15000; // 之后每 15 秒检查一次
        private static bool _initialized = false;
        private static bool _hasConnected = false;
        private static float _nextPollTime = -1f;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            if (SteamConnectionState.IsSteamActuallyInitialized)
            {
                _hasConnected = true;
                Plugin.Logger.LogInfo("[SteamReconnect] Steam 已在启动时连接，重连监视器空闲");
                return;
            }

            _nextPollTime = Time.unscaledTime + InitialDelayMs / 1000f;

            Plugin.Logger.LogInfo("[SteamReconnect] 重连监视器已启动，等待 Steam 就绪…");
        }

        /// <summary>
        /// 主线程定时轮询 Steam 状态
        /// </summary>
        private static bool PollSteam()
        {
            if (!PluginConfig.EnableWallpaperEngineMode.Value) return false;

            try
            {
                if (SteamAPI.IsSteamRunning())
                    return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[SteamReconnect] 轮询 Steam 状态时出错: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// 主线程每帧检查是否到达下一次轮询时间
        /// </summary>
        public static void Tick()
        {
            if (!_initialized) return;
            if (!PluginConfig.EnableWallpaperEngineMode.Value) return;
            if (_nextPollTime < 0f) return;
            if (Time.unscaledTime < _nextPollTime) return;

            _nextPollTime = Time.unscaledTime + CheckIntervalMs / 1000f;

            bool isSteamRunning = PollSteam();

            if (_hasConnected)
            {
                if (!isSteamRunning)
                    HandleSteamStopped();

                return;
            }

            if (!isSteamRunning)
                return;

            Plugin.Logger.LogInfo("[SteamReconnect] 检测到 Steam 已启动，尝试连接…");
            AttemptReconnect();
        }

        /// <summary>
        /// 在主线程执行真正的 Steam 初始化
        /// </summary>
        private static void AttemptReconnect()
        {
            try
            {
                string initError;
                bool success = SteamAPI.InitEx(out initError) == ESteamAPIInitResult.k_ESteamAPIInitResult_OK;
                if (!success)
                {
                    Plugin.Logger.LogWarning(
                        $"[SteamReconnect] SteamAPI.InitEx() 未成功，等待下次重试。原因: {initError}");
                    return; // 下次 Timer 继续轮询
                }

                // 更新 SteamManager 内部的 isInitialized 字段
                if (SteamConnectionState.ManagerInstance != null)
                {
                    var field = AccessTools.Field(
                        typeof(NestopiSystem.Steam.SteamManager), "isInitialized");
                    field.SetValue(SteamConnectionState.ManagerInstance, true);
                }

                SteamUserStats.RequestUserStats(SteamUser.GetSteamID());

                _hasConnected = true;
                _nextPollTime = Time.unscaledTime + CheckIntervalMs / 1000f;

                // 填充玩家信息、触发 OnSteamConnected 事件
                SteamConnectionState.MarkConnected();

                // 在线 ID 与存档 ID 相同才同步（设旗标，由 Update() 异步执行）
                if (PluginConfig.EnableAchievementCache.Value)
                {
                    string realId = SteamConnectionState.SteamUserId;
                    string configId = PluginConfig.OfflineUserId.Value;

                    if (!string.IsNullOrEmpty(realId) && realId == configId)
                    {
                        Plugin.Logger.LogInfo(
                            $"[SteamReconnect] 在线ID ({realId}) 与存档ID一致，触发成就同步");
                        AchievementSyncManager.ManualSync(); // 非阻塞旗标
                    }
                    else
                    {
                        Plugin.Logger.LogInfo(
                            $"[SteamReconnect] 在线ID ({realId}) 与存档ID ({configId}) 不一致，跳过成就同步");
                    }
                }

                Plugin.Logger.LogInfo("[SteamReconnect] Steam 重连成功！");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[SteamReconnect] 连接尝试失败: {ex.Message}");
                // 不设 _hasConnected，下次 Timer 继续尝试
            }
        }

        private static void HandleSteamStopped()
        {
            try
            {
                SteamAPI.Shutdown();
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[SteamReconnect] Steam 关闭后的 Shutdown 处理失败: {ex.Message}");
            }

            if (SteamConnectionState.ManagerInstance != null)
            {
                try
                {
                    var field = AccessTools.Field(
                        typeof(NestopiSystem.Steam.SteamManager), "isInitialized");
                    field.SetValue(SteamConnectionState.ManagerInstance, false);
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogWarning($"[SteamReconnect] 重置 SteamManager 状态失败: {ex.Message}");
                }
            }

            _hasConnected = false;
            SteamConnectionState.MarkPending();
            Plugin.Logger.LogWarning("[SteamReconnect] 检测到 Steam 已关闭，已切回等待重连状态");
        }
    }
}
