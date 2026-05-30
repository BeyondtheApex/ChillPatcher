using System;
using System.Linq;
using System.Reflection;
using OneJS.Compat;
using UnityEngine;
using UnityEngine.UIElements;

namespace OneJS.Extensions {
    public static class VisualElementExts {
        public static VisualElement Q(this VisualElement e, Type type, string name = null, params string[] classes) {
            return Query(e, type, name, classes).First();
        }
        
        public static UQueryBuilder<VisualElement> Query(this VisualElement e, Type type, string name = null, params string[] classes) {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (type == null || !typeof(VisualElement).IsAssignableFrom(type))
                throw new ArgumentException("Type must be a subclass of VisualElement", nameof(type));

            var queryMethod = NetFxCompat.GetMethodWithArity(typeof(UQueryExtensions), "Query", 1, new Type[] { typeof(VisualElement), typeof(string), typeof(string[]) }).MakeGenericMethod(type);

            var result = queryMethod.Invoke(null, new object[] { e, name, classes });

            var ofTypeMethod = NetFxCompat.GetMethodWithArity(result.GetType(), "OfType", 1, new Type[] { typeof(string), typeof(string[]) }).MakeGenericMethod(typeof(VisualElement));

            return (UQueryBuilder<VisualElement>)ofTypeMethod.Invoke(result, new object[] { name, classes });
        }

        public static void Register(this CallbackEventHandler cbeh, Type eventType,
            EventCallback<EventBase> handler, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) {
            var flags = BindingFlags.Public | BindingFlags.Instance;
            var mi = cbeh.GetType().GetMethods(flags)
                .Where(m => m.Name == "RegisterCallback" && m.GetGenericArguments().Length == 1).First();
            mi = mi.MakeGenericMethod(eventType);
            mi.Invoke(cbeh, new object[] { handler, useTrickleDown });
        }

        public static void Unregister(this CallbackEventHandler cbeh, Type eventType,
            EventCallback<EventBase> handler, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) {
            var flags = BindingFlags.Public | BindingFlags.Instance;
            var mi = cbeh.GetType().GetMethods(flags)
                .Where(m => m.Name == "UnregisterCallback" && m.GetGenericArguments().Length == 1).First();
            mi = mi.MakeGenericMethod(eventType);
            mi.Invoke(cbeh, new object[] { handler, useTrickleDown });
        }

        public static void ForceUpdate(this VisualElement view) {
            view.schedule.Execute(() => {
                var fakeOldRect = Rect.zero;
                var fakeNewRect = view.layout;

                using var evt = GeometryChangedEvent.GetPooled(fakeOldRect, fakeNewRect);
                evt.target = view.contentContainer;
                view.contentContainer.SendEvent(evt);
            });
        }
    }
}