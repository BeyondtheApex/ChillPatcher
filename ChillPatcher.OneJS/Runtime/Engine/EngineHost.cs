using System;
using System.Reflection;
using OneJS.Utils;
using Puerts;
using UnityEngine;

namespace OneJS {
    public interface IEngineHost {
        event Action onReload;
        event Action onDispose;
    }

    /// <summary>
    /// Used to provide host objects and host functions to the JS side under `onejs` global variable
    /// </summary>
    public class EngineHost : IEngineHost, IDisposable {
        // public readonly Interop interop;
        public event Action onReload;
        public event Action onDispose;
        public event Action<Exception> onError;

        // public delegate void JSCallback(object v);

        readonly ScriptEngine _engine;

        public EngineHost(ScriptEngine engine) {
            // interop = new(engine);
            _engine = engine;
            engine.OnReload += DoReload;
            engine.OnDispose += Dispose;
            engine.OnError += Error;
        }

        public void DoReload() {
            onReload?.Invoke();
        }

        public void Dispose() {
            onDispose?.Invoke();
            _engine.OnDispose -= Dispose;
            onDispose = null;

            _engine.OnReload -= DoReload;
            onReload = null;
            
            _engine.OnError -= Error;
            onError = null;
        }

        public void Error(Exception ex) {
            onError?.Invoke(ex);
        }

#if PUERTS_DISABLE_IL2CPP_OPTIMIZATION || (!PUERTS_IL2CPP_OPTIMIZATION && (UNITY_WEBGL || UNITY_IPHONE)) || !ENABLE_IL2CPP

        /// <summary>
        /// Use this method to subscribe to an event on an object regardless of JS engine.
        ///
        /// TODO: remove usage of Puerts.GenericDelegate
        /// </summary>
        /// <param name="eventSource">The object containing the event</param>
        /// <param name="eventName">The name of the event</param>
        /// <param name="handler">A C# delegate or a JS function</param>
        /// <returns>A function to unsubscribe event</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public Action subscribe(object eventSource, string eventName, GenericDelegate handler) {
            if (eventSource is null) {
                throw new ArgumentNullException(nameof(eventSource), "[SubscribeEvent] Event source is null.");
            } else if (eventSource is JSObject) {
                throw new NotSupportedException("[SubscribeEvent] Cannot subscribe event on JS value.");
            }

            var eventInfo = eventSource.GetType().GetEvent(eventName, BindingFlags.Public | BindingFlags.Instance);
            if (eventInfo is null) {
                throw new ArgumentException(
                    $"[SubscribeEvent] Cannot find event \"{eventName}\" on type \"{eventSource.GetType()}\".",
                    nameof(eventName));
            }

            var handlerDelegate = GenericDelegateWrapper.Wrap(_engine.JsEnv, eventInfo, handler);
            var isOnReloadEvent = eventSource == this && eventName == nameof(onReload);
            var isOnDisposeEvent = eventSource == this && eventName == nameof(onDispose);

            eventInfo.AddEventHandler(eventSource, handlerDelegate);

            if (!isOnReloadEvent) {
                onReload += unsubscribe;
                onDispose += unsubscribe;
            }
            return () => {
                unsubscribe();

                if (!isOnReloadEvent) {
                    onReload -= unsubscribe;
                }
                if (!isOnDisposeEvent) {
                    onDispose -= unsubscribe;
                }
            };

            void unsubscribe() {
                eventInfo.RemoveEventHandler(eventSource, handlerDelegate);
            }
        }

        public Action subscribe(string eventName, GenericDelegate handler) => subscribe(this, eventName, handler);
#endif
    }
}