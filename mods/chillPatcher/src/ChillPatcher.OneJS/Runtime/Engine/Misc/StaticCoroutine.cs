using System.Collections;
using UnityEngine;

namespace OneJS {
    public class StaticCoroutine {
        private class CoroutineHolder : MonoBehaviour {
        }

        private static CoroutineHolder _holder;

        private static CoroutineHolder Holder {
            get {
                if (_holder == null) {
                    GameObject obj = new GameObject("StaticCoroutineHolder");
                    _holder = obj.AddComponent<CoroutineHolder>();
                    Object.DontDestroyOnLoad(obj);
                }
                return _holder;
            }
        }

        public static Coroutine Start(IEnumerator coroutine) {
            return Holder.StartCoroutine(coroutine);
        }

        public static void Stop(Coroutine coroutine) {
            if (coroutine != null)
                Holder.StopCoroutine(coroutine);
        }
    }
}