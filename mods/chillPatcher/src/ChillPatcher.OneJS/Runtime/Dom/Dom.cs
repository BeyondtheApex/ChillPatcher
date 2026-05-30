using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Puerts;
using UnityEngine;
using UnityEngine.UIElements;

namespace OneJS.Dom {
    public class RegisteredCallbackHolder {
        public EventCallback<EventBase> callback;
        public object jsValue;
        public bool useCapture;
    }

    public class Dom {
        #region Statics
        static Dictionary<string, Type> _allUIElementEventTypes = new();

        static Dom() {
            InitAllUIElementEvents();
        }

        static void InitAllUIElementEvents() {
            AddEventsFromAssembly(typeof(VisualElement).Assembly);
        }

        public static void AddEventsFromAssemblies(Assembly[] assemblies) {
            foreach (var assembly in assemblies)
                AddEventsFromAssembly(assembly);
        }

        public static void AddEventsFromAssembly(Assembly assembly) {
            var eventTypes = assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(EventBase)));
            foreach (var type in eventTypes)
                AddEventType(type);
        }

        public static void AddEventsFromTypes(Type[] types) {
            var eventTypes = types.Where(type => type.IsSubclassOf(typeof(EventBase)));
            foreach (var type in eventTypes)
                AddEventType(type);
        }

        static void AddEventType(Type type) {
            var typeNameLower = type.Name.ToLower();
            _allUIElementEventTypes[typeNameLower] = type;
            if (type.Name.EndsWith("Event"))
                _allUIElementEventTypes[typeNameLower[..^5]] = type;
        }

        static Type FindUIElementEventType(string name) {
            if (_allUIElementEventTypes.TryGetValue(name, out var type)) {
                return type;
            }
            return null;
        }
        #endregion

        public IDocument document => _document;

        public VisualElement ve => _ve;

        public Dom[] childNodes => _childNodes.ToArray();

        public Dom firstChild => _childNodes.Count > 0 ? childNodes[0] : null;

        public Dom parentNode { get { return _parentNode; } }

        public Dom nextSibling { get { return _nextSibling; } }

        public int nodeType => _nodeType;

        /// <summary>
        /// ECMA Compliant id property, stored in the VE.name
        /// </summary>
        public string Id { get { return _ve.name; } set { _ve.name = value; } }

        public string key { get { return _key; } set { _key = value; } }

        public DomStyle style => _style;

        public object value { get { return _value; } }

        public bool @checked { get { return _checked; } }

        public object data {
            get { return _data; }
            set {
                _data = value;
                if (_ve is TextElement) {
                    (_ve as TextElement).text = value.ToString();
                }
            }
        }

        public string innerHTML { get { return _innerHTML; } }

        public Vector2 layoutSize => _ve.layout.size;

        public object _children { get { return __children; } set { __children = value; } }

        // NOTE: Using `JsValue` here because `EventCallback<EventBase>` will lead to massive slowdown on Linux.
        // [props.ts] `dom._listeners[name + useCapture] = value;`
        public Dictionary<string, EventCallback<EventBase>> _listeners => __listeners;

        static (string, string)[] _replacePairsForClassNames = new[] {
            (".", "_d_"), ("/", "_s_"), (":", "_c_"), ("%", "_p_"), ("#", "_n_"),
            ("[", "_lb_"), ("]", "_rb_"), ("(", "_lp_"), (")", "_rp_"),
            (",", "_cm_"),
            ("&", "_amp_"), (">", "_gt_"), ("<", "_lt_"), ("*", "_ast_"),
            ("'", "_sq_"),
        };

        IDocument _document;
        VisualElement _ve;
        DomStyle _style;
        string _key;
        Dom _parentNode;
        Dom _nextSibling;
        int _nodeType;
        object _value;
        bool _checked;
        object _data;
        string _innerHTML;
        List<Dom> _childNodes = new List<Dom>();
        object __children;
        Dictionary<string, EventCallback<EventBase>> __listeners = new Dictionary<string, EventCallback<EventBase>>();

        Dictionary<string, List<RegisteredCallbackHolder>> _registeredCallbacks =
            new Dictionary<string, List<RegisteredCallbackHolder>>();

        static Dictionary<string, RegisterCallbackDelegate> _eventCache =
            new Dictionary<string, RegisterCallbackDelegate>();

        TickBasedCallTracker _tickBasedCallTracker;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init() {
            _eventCache.Clear();
        }

        public static void RegisterCallback<T>(VisualElement ve, EventCallback<T> callback,
            TrickleDown trickleDown = TrickleDown.NoTrickleDown)
            where T : EventBase<T>, new() {
            ve.RegisterCallback(callback, trickleDown);
        }

        public delegate void RegisterCallbackDelegate(VisualElement ve, EventCallback<EventBase> callback,
            TrickleDown trickleDown = TrickleDown.NoTrickleDown);

        // Not Used
        //public Dom(string tagName) {
        //    _ve = new VisualElement();
        //}

        public Dom(VisualElement ve, IDocument document) {
            _ve = ve;
            _document = document;
            _style = new DomStyle(this);
        }

        // public void CallListener(string name, EventBase evt) {
        //     // var func = __listeners[name].As<FunctionInstance>();
        //     var tick = _document.scriptEngine.Tick;
        //     _tickBasedCallTracker.count = _tickBasedCallTracker.tick == tick ? _tickBasedCallTracker.count + 1 : 0;
        //     _tickBasedCallTracker.tick = tick;
        //     if (_tickBasedCallTracker.count > 1000) {
        //         Debug.LogError(
        //             $"Possible infinite loop detected. Event Listener(s) on {_ve.GetType().Name} called more than 1000 times in one frame.");
        //         return;
        //     }
        //     var jintEngine = _document.scriptEngine.JintEngine;
        //     var thisDom = JsValue.FromObject(jintEngine, this);
        //     jintEngine.Call(__listeners[name], thisDom, new[] { JsValue.FromObject(jintEngine, evt) });
        // }

        public string className {
            get {
                return string.Join(" ", _ve.GetClasses().Where(c => !c.StartsWith("unity-")).ToArray());
            }
            set {
                this.setAttribute("class", value);
            }
        }

        public void SetBackgroundColor(Color color) {
            _ve.style.backgroundColor = color;
        }

        public void clearChildren() {
            _ve.Clear();
        }

        public void _addToListeners(string name, EventCallback<EventBase> callback) {
            __listeners[name] = callback;
        }

        public EventCallback<EventBase> _getFromListeners(string name) {
            if (__listeners.TryGetValue(name, out var callback)) {
                return callback;
            }
            return null;
        }

        public void _callListener(string name, EventBase evt) {
            if (__listeners.TryGetValue(name, out var callback)) {
                callback(evt);
            }
        }

        public void addEventListener(string name, EventCallback<EventBase> callback, bool useCapture = false) {
            var nameLower = name.ToLower();
            var isValueChanged = nameLower == "valuechanged" || nameLower == "change";
            if (!isValueChanged && _eventCache.ContainsKey(nameLower)) {
                _eventCache[nameLower](_ve, callback, useCapture ? TrickleDown.TrickleDown : TrickleDown.NoTrickleDown);
                // Debug.Log("Registered " + name + " on " + _ve.name);
            } else {
                Type eventType = null;
                if (isValueChanged) {
                    var notifyInterface = _ve.GetType().GetInterfaces().Where(i => i.Name == "INotifyValueChanged`1")
                        .FirstOrDefault();
                    if (notifyInterface != null) {
                        var valType = notifyInterface.GenericTypeArguments[0];
                        eventType = typeof(VisualElement).Assembly.GetType($"UnityEngine.UIElements.ChangeEvent`1");
                        eventType = eventType.MakeGenericType(valType);
                    }
                } else {
                    eventType = FindUIElementEventType(nameLower);
                }

                if (eventType != null) {
                    var mi = this.GetType().GetMethod("RegisterCallback");
                    mi = mi.MakeGenericMethod(eventType);
                    // mi.Invoke(null, new object[] { _ve, callback });
                    var del = (RegisterCallbackDelegate)Delegate.CreateDelegate(typeof(RegisterCallbackDelegate), mi);
                    if (!isValueChanged && !_eventCache.ContainsKey(nameLower))
                        _eventCache.Add(nameLower, del);
                    del(_ve, callback, useCapture ? TrickleDown.TrickleDown : TrickleDown.NoTrickleDown);
                    // Debug.Log("Registered " + name + " on " + _ve.name);
                }
            }

            var callbackHolder = new RegisteredCallbackHolder() { callback = callback, useCapture = useCapture };
            if (!_registeredCallbacks.TryGetValue(nameLower, out List<RegisteredCallbackHolder> callbackList)) {
                callbackList = new List<RegisteredCallbackHolder>();
                _registeredCallbacks[nameLower] = callbackList;
            }
            callbackList.Add(callbackHolder);

            // Debug.Log($"{name} {(DateTime.Now - t).TotalMilliseconds}ms");
        }

        /// <summary>
        /// Name, callback, and useCapture must match exactly to remove the listener.
        /// </summary>
        public void removeEventListener(string name, EventCallback<EventBase> callback, bool useCapture = false) {
            var nameLower = name.ToLower();
            if (!_registeredCallbacks.ContainsKey(nameLower))
                return;
            var callbackHolders = _registeredCallbacks[nameLower];
            var eventType = FindUIElementEventType(nameLower);
            if (eventType != null) {
                var flags = BindingFlags.Public | BindingFlags.Instance;
                var mi = _ve.GetType().GetMethods(flags)
                    .Where(m => m.Name == "UnregisterCallback" && m.GetGenericArguments().Length == 1).First();
                mi = mi.MakeGenericMethod(eventType);
                for (var i = 0; i < callbackHolders.Count; i++) {
                    if (callbackHolders[i].callback == callback && callbackHolders[i].useCapture == useCapture) {
                        mi.Invoke(_ve,
                            new object[] { callbackHolders[i].callback, useCapture ? TrickleDown.TrickleDown : TrickleDown.NoTrickleDown });
                        callbackHolders.RemoveAt(i);
                        i--;
                    }
                }
                if (callbackHolders.Count == 0) {
                    _registeredCallbacks.Remove(nameLower);
                }
            }
        }

        public void appendChild(Dom node) {
            if (node == null)
                return;
            try {
                this._ve.Add(node.ve);
            } catch (Exception e) {
                Debug.LogError(e.Message);
                throw new Exception("Invalid Dom appendChild");
            }
            node._parentNode = this;
            if (_childNodes.Count > 0) {
                _childNodes[_childNodes.Count - 1]._nextSibling = node;
            }
            _childNodes.Add(node);
            TryAddCacheDom(node);
        }

        public void removeChild(Dom child) {
            if (child == null || !this._ve.Contains(child.ve))
                return;
            using (var evt = TransitionCancelEvent.GetPooled()) {
                evt.target = child.ve;
                child.ve.SendEvent(evt);
            }
            this._ve.Remove(child.ve);
            var index = _childNodes.IndexOf(child);
            if (index > 0) {
                var prev = _childNodes[index - 1];
                prev._nextSibling = child._nextSibling;
            }
            _childNodes.Remove(child);
            child._parentNode = null;
            TryRemoveCacheDom(child);
        }

        public void insertBefore(Dom a, Dom b) {
            if (a == null)
                return;
            if (b == null || b.ve == null || _ve.IndexOf(b.ve) == -1) {
                appendChild(a);
                return;
            }
            if (a == b) {
                return;
            }
            var index = _ve.IndexOf(b.ve);
            _ve.Insert(index, a.ve);
            _childNodes.Insert(index, a);
            a._nextSibling = b;
            a._parentNode = this;
            if (index > 0) {
                _childNodes[index - 1]._nextSibling = a;
            }
            TryAddCacheDom(a);
        }

        public void insertAfter(Dom a, Dom b) {
            if (a == null)
                return;
            if (b == null || b.ve == null || _ve.IndexOf(b.ve) == -1) {
                appendChild(a);
                return;
            }
            if (a == b) {
                return;
            }
            var index = _ve.IndexOf(b.ve);
            var newIndex = index + 1;
            _ve.Insert(newIndex, a.ve);
            _childNodes.Insert(newIndex, a);
            a._parentNode = this;
            a._nextSibling = b._nextSibling;
            b._nextSibling = a;
            TryAddCacheDom(a);
        }

        public void setAttribute(string name, object val) {
            if (name == "class" || name == "classname" || name == "className") {
                var unityClassnames = _ve.GetClasses().Where(c => c.StartsWith("unity-")).ToArray();
                _ve.ClearClassList();
                var unprocessedClassStr = ProcessClassStr(val.ToString(), this);
                // var unprocessedClassStr = val.ToString();
                var parts = (unprocessedClassStr).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var unityClassname in unityClassnames) {
                    _ve.AddToClassList(unityClassname);
                }
                foreach (var part in parts) {
                    _ve.AddToClassList(part);
                }
            } else if (name == "id" || name == "name") {
                _ve.name = val.ToString();
            } else if (name == "disabled") {
                _ve.SetEnabled(!Convert.ToBoolean(val));
            } else {
                name = name.Replace("-", "");
                var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
                var ei = _ve.GetType().GetEvent(name, flags);
                if (ei != null) {
                    ei.AddMethod.Invoke(_ve, new object[] { val });
                    return;
                }
                var pi = _ve.GetType().GetProperty(name, flags);
                if (pi != null) {
                    var genericArgs = pi.PropertyType.GetGenericArguments();
                    if (pi.PropertyType.IsEnum) {
                        val = Convert.ToInt32(val);
                    } else if (val is JSObject jsObj && jsObj.Get<int>("length") > 0) {
                        var length = jsObj.Get<int>("length");
                        var objAry = new object[length];
                        for (var i = 0; i < length; i++) {
                            objAry[i] = jsObj.Get<object>(i.ToString());
                        }
                        if (pi.PropertyType.IsArray) {
                            Array destinationArray = Array.CreateInstance(pi.PropertyType.GetElementType(), length);
                            Array.Copy(objAry, destinationArray, length);
                            val = destinationArray;
                        } else if ((genericArgs.Length > 0)) {
                            var listType = typeof(List<>).MakeGenericType(genericArgs);
                            if (pi.PropertyType == listType) {
                                Array destinationArray = Array.CreateInstance(genericArgs[0], length);
                                Array.Copy(objAry, destinationArray, length);
                                var list = (IList)Activator.CreateInstance(listType, destinationArray);
                                val = list;
                            }
                        }
                    } else if (pi.PropertyType == typeof(Single) && val.GetType() == typeof(double)) {
                        val = Convert.ToSingle(val);
                    } else if (pi.PropertyType == typeof(Int32) && val.GetType() == typeof(double)) {
                        val = Convert.ToInt32(val);
                    } else if (pi.PropertyType == typeof(char) && val.GetType() == typeof(string)) {
                        val = val.ToString()[0];
                    }
                    pi.SetValue(_ve, val);
                }
            }
        }

        public void removeAttribute(string name) {
            if (name == "class" || name == "className") {
                _ve.ClearClassList();
            } else if (name == "id") {
                _ve.name = null;
            } else if (name == "disabled") {
                _ve.SetEnabled(true);
            } else {
                var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
                var pi = _ve.GetType().GetProperty(name, flags);
                if (pi != null) {
                    pi.SetValue(_ve, null);
                }
            }
        }

        public bool contains(Dom child) {
            return _ve.Contains(child.ve);
        }

        public void focus() {
            _ve.Focus();
        }

        public override string ToString() {
            return $"dom: {this._ve.GetType().Name} {this.key} ({this._ve.GetHashCode()})";
        }

        /// <summary>
        /// BFS for first predicate matching Dom, including this one.
        /// </summary>
        /// <param name="predicate">Search criteria</param>
        /// <returns>Matching Dom or null</returns>
        public Dom First(Func<Dom, bool> predicate) {
            Queue<Dom> q = new();
            q.Enqueue(this);
            while (q.Count > 0) {
                var cnt = q.Count;
                for (int i = 0; i < cnt; i++) {
                    var cur = q.Dequeue();
                    if (predicate(cur)) {
                        return cur;
                    }
                    if (cur._childNodes != null) {
                        for (int ci = 0; ci < cur._childNodes.Count; ci++) {
                            q.Enqueue(cur._childNodes[ci]);
                        }
                    }
                }
            }
            return null;
        }

        public string ProcessClassStr(string classStr, Dom dom) {
            var names = classStr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            names = names.Select(s => s[0] >= 48 && s[0] <= 57 ? "_" + s : s).ToArray(); // ^\d => _\d

            var output = String.Join(" ", names);
            return TransformUsingPairs(output, _replacePairsForClassNames);
        }

        void TryAddCacheDom(Dom dom) {
            if (_document != null) {
                _document.AddCachingDom(dom);
            }
        }

        void TryRemoveCacheDom(Dom dom) {
            if (_document != null) {
                _document.RemoveCachingDom(dom);
            }
        }

        /// <summary>
        /// Basically a more performant version of chaining string.Replace
        /// </summary>
        string TransformUsingPairs(string input, (string, string)[] pairs) {
            var sb = new StringBuilder(input);
            var indices = new List<(int, int, string)>();

            foreach (var pair in pairs) {
                int index = 0;
                while ((index = input.IndexOf(pair.Item1, index)) != -1) {
                    indices.Add((index, pair.Item1.Length, pair.Item2));
                    index += pair.Item1.Length;
                }
            }

            indices.Sort((a, b) => b.Item1.CompareTo(a.Item1));

            foreach (var tuple in indices) {
                sb.Remove(tuple.Item1, tuple.Item2);
                sb.Insert(tuple.Item1, tuple.Item3);
            }

            return sb.ToString();
        }

        struct TickBasedCallTracker {
            public int tick;
            public int count;
        }
    }
}
