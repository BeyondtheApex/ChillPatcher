using System;
using System.Threading;
using BepInEx.Logging;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace ChillPatcher
{
    /// <summary>
    /// Injects a custom update into Unity's PlayerLoop since MonoBehaviour.Update
    /// doesn't work for dynamically created GameObjects in this Wallpaper Engine environment.
    /// </summary>
    public static class PlayerLoopInjector
    {
        private static ManualLogSource _log;
        private static bool _installed;
        private static bool _firstTickLogged;

        private struct ChillPatcherUpdate { }

        public static void Install(ManualLogSource log)
        {
            if (_installed) return;
            _log = log;
            _installed = true;

            // Inject immediately
            DoInject("immediate");

            // Also re-inject after 3 seconds in case the game resets the PlayerLoop
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(3000);
                log.LogInfo("[PlayerLoop] Delayed re-inject starting...");
                // Must run on main thread for SetPlayerLoop - use a flag to do it next opportunity
                // Since we have no main thread access, just do it directly (Unity 2021+ allows this)
                try
                {
                    DoInject("delayed-3s");
                }
                catch (Exception ex)
                {
                    log.LogError($"[PlayerLoop] Delayed inject failed: {ex}");
                }
            });
        }

        private static void DoInject(string label)
        {
            var currentLoop = PlayerLoop.GetCurrentPlayerLoop();
            var subSystems = currentLoop.subSystemList;

            if (subSystems == null || subSystems.Length == 0)
            {
                _log?.LogWarning($"[PlayerLoop:{label}] No subsystems found!");
                return;
            }

            // Log current subsystems
            _log?.LogInfo($"[PlayerLoop:{label}] Current subsystems ({subSystems.Length}):");
            for (int i = 0; i < subSystems.Length; i++)
            {
                var sub = subSystems[i];
                int childCount = sub.subSystemList?.Length ?? 0;
                _log?.LogInfo($"[PlayerLoop:{label}]   [{i}] {sub.type?.Name ?? "null"} ({childCount} children)");
            }

            // Try to inject into multiple phases for maximum chance of running
            bool injectedAny = false;
            Type[] targetPhases = new[] { typeof(PostLateUpdate), typeof(PreLateUpdate), typeof(Update), typeof(EarlyUpdate) };

            foreach (var phaseType in targetPhases)
            {
                for (int i = 0; i < subSystems.Length; i++)
                {
                    if (subSystems[i].type == phaseType)
                    {
                        var system = subSystems[i];
                        var existing = system.subSystemList ?? Array.Empty<PlayerLoopSystem>();

                        // Check if already injected
                        bool alreadyExists = false;
                        foreach (var s in existing)
                        {
                            if (s.type == typeof(ChillPatcherUpdate))
                            {
                                alreadyExists = true;
                                break;
                            }
                        }
                        if (alreadyExists)
                        {
                            _log?.LogInfo($"[PlayerLoop:{label}] Already in {phaseType.Name}");
                            continue;
                        }

                        var newSubs = new PlayerLoopSystem[existing.Length + 1];
                        // Insert at beginning for higher priority
                        newSubs[0] = new PlayerLoopSystem
                        {
                            type = typeof(ChillPatcherUpdate),
                            updateDelegate = OnUpdate
                        };
                        Array.Copy(existing, 0, newSubs, 1, existing.Length);
                        system.subSystemList = newSubs;
                        subSystems[i] = system;
                        _log?.LogInfo($"[PlayerLoop:{label}] Injected into {phaseType.Name}");
                        injectedAny = true;
                        break; // Only inject into first matching phase
                    }
                }
                if (injectedAny) break;
            }

            if (!injectedAny)
            {
                // Absolute fallback: add to root
                var newRoot = new PlayerLoopSystem[subSystems.Length + 1];
                Array.Copy(subSystems, newRoot, subSystems.Length);
                newRoot[subSystems.Length] = new PlayerLoopSystem
                {
                    type = typeof(ChillPatcherUpdate),
                    updateDelegate = OnUpdate
                };
                currentLoop.subSystemList = newRoot;
                _log?.LogInfo($"[PlayerLoop:{label}] Injected at root level");
            }

            PlayerLoop.SetPlayerLoop(currentLoop);
            _log?.LogInfo($"[PlayerLoop:{label}] SetPlayerLoop done");
        }

        private static void OnUpdate()
        {
            if (!_firstTickLogged)
            {
                _firstTickLogged = true;
                _log?.LogInfo("[PlayerLoop] First tick running!");
            }

            OneJSBridge.Tick();
        }
    }
}
