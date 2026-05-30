using System;
using System.Threading;
using Newtonsoft.Json;
using Puerts;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// JS API 返回值转换辅助类。
    /// PuerTS 不会自动将 C# Dictionary/Array 转为 JS 原生对象/数组，
    /// 这里提供统一的转换方法，保证 JS 端拿到的都是原生类型。
    /// </summary>
    internal static class JSApiHelper
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Include,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        // 复用 byte[] 缓冲区，避免每帧 GC（频谱/波形每帧调用）
        [ThreadStatic] private static byte[] _floatBuffer;
        [ThreadStatic] private static int _floatBufferSize;

        /// <summary>
        /// 将任意对象序列化为 JSON 字符串，JS 端通过 JSON.parse() 获得原生对象/数组。
        /// </summary>
        public static string ToJson(object obj)
        {
            if (obj == null) return "null";
            return JsonConvert.SerializeObject(obj, JsonSettings);
        }

        /// <summary>
        /// 将 float[] 转为 Puerts.ArrayBuffer，JS 端通过 new Float32Array(buf) 获得原生类型数组。
        /// 内部复用 byte[] 缓冲区，适合每帧调用（频谱/波形可视化）。
        /// </summary>
        public static ArrayBuffer ToFloat32Buffer(float[] data)
        {
            if (data == null) return null;
            var byteLen = data.Length * sizeof(float);
            if (_floatBuffer == null || _floatBufferSize < byteLen)
            {
                _floatBuffer = new byte[byteLen];
                _floatBufferSize = byteLen;
            }
            Buffer.BlockCopy(data, 0, _floatBuffer, 0, byteLen);
            return new ArrayBuffer(_floatBuffer, byteLen);
        }
    }
}
