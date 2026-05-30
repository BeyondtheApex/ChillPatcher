#if PUERTS_DISABLE_IL2CPP_OPTIMIZATION || (!PUERTS_IL2CPP_OPTIMIZATION && (UNITY_WEBGL || UNITY_IPHONE)) || !ENABLE_IL2CPP

using System;
using System.Linq;
using System.Reflection;
using Puerts;

namespace OneJS.Utils {
    public class GenericDelegateWrapper {
        JsEnv _jsEnv;
        EventInfo _eventInfo;
        GenericDelegate _handler;

        Delegate _del;

        public static Delegate Wrap(JsEnv jsEnv, EventInfo eventInfo, GenericDelegate handler) =>
            new GenericDelegateWrapper(jsEnv, eventInfo, handler).GetWrapped();

        /// <summary>
        /// https://nondisplayable.ca/2017/03/31/using-reflection-to-bind-lambda-to-event-handler.html
        /// </summary>
        public GenericDelegateWrapper(JsEnv jsEnv, EventInfo eventInfo, GenericDelegate handler) {
            _jsEnv = jsEnv;
            _eventInfo = eventInfo;
            _handler = handler;

            var handlerType = _eventInfo.EventHandlerType;
            MethodInfo invoke = handlerType.GetMethod("Invoke");

            if (invoke.ReturnType != typeof(void)) {
                throw new ArgumentException("[GenericDelegateWrapper] Only support event delegate that return nothing.");
            }
            
            ParameterInfo[] pars = invoke.GetParameters();
            var paramTypes = pars.Select(p => p.ParameterType).ToArray();
            var methodInfo =
                OneJS.Compat.NetFxCompat.GetMethodWithArity(typeof(GenericDelegateWrapper), nameof(GetAction), paramTypes.Length, Array.Empty<Type>())
                ?? throw new ArgumentException("[GenericDelegateWrapper] Only support handler with up to 4 parameters.",
                    nameof(handler));

            if (paramTypes.Length > 0) {
                methodInfo = methodInfo.MakeGenericMethod(paramTypes);
            }

            var h = (Delegate)methodInfo.Invoke(this, Array.Empty<object>());
            _del = Delegate.CreateDelegate(handlerType, h, "Invoke");
        }

        public Delegate GetWrapped() {
            return _del;
        }

        public Action GetAction() {
            return () => _handler.Action();
        }

        public Action<A> GetAction<A>() {
            return (a) => {
                // var aa = JsValue.FromObject(_engine, a);
                _handler.Action(a);
            };
        }

        public Action<A, B> GetAction<A, B>() {
            return (a, b) => {
                // var aa = JsValue.FromObject(_engine, a);
                // var bb = JsValue.FromObject(_engine, b);
                _handler.Action(a, b);
            };
        }

        public Action<A, B, C> GetAction<A, B, C>() {
            return (a, b, c) => {
                // var aa = JsValue.FromObject(_engine, a);
                // var bb = JsValue.FromObject(_engine, b);
                // var cc = JsValue.FromObject(_engine, c);
                _handler.Action(a, b, c);
            };
        }

        public Action<A, B, C, D> GetAction<A, B, C, D>() {
            return (a, b, c, d) => {
                // var aa = JsValue.FromObject(_engine, a);
                // var bb = JsValue.FromObject(_engine, b);
                // var cc = JsValue.FromObject(_engine, c);
                // var dd = JsValue.FromObject(_engine, d);
                _handler.Action(a, b, c, d);
            };
        }
    }
}
#endif