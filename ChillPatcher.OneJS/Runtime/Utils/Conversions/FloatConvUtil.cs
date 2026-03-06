using Puerts;
using Unity.Mathematics;

namespace OneJS.Utils {
    public class FloatConvUtil {
        public static float[] CreateFloatBuffer(JSObject obj) {
            var length = obj.Get<int>("length");
            var buffer = new float[length];
            for (var i = 0; i < length; i++) {
                buffer[i] = obj.Get<float>(i + "");
            }
            return buffer;
        }
        
        /// <summary>
        /// Useful for working around boxing issue and double-to-float conversion issue during JS-C# interop
        /// </summary>
        public static void SetFloatValue(System.Array arr, float val, int index) {
            float[] floatArr = arr as float[];
            if (floatArr != null)
                floatArr[index] = val;
        }

        /// <summary>
        /// Useful for working around boxing issue during JS-C# interop
        /// </summary>
        public static void SetFloat2Value(System.Array arr, float2 val, int index) {
            var floatArr = arr as float2[];
            if (floatArr != null)
                floatArr[index] = val;
        }

        /// <summary>
        /// Useful for working around boxing issue during JS-C# interop
        /// </summary>
        public static void SetFloat3Value(System.Array arr, float3 val, int index) {
            var floatArr = arr as float3[];
            if (floatArr != null)
                floatArr[index] = val;
        }

        /// <summary>
        /// Useful for working around boxing issue during JS-C# interop
        /// </summary>
        public static void SetFloat4Value(System.Array arr, float4 val, int index) {
            var floatArr = arr as float4[];
            if (floatArr != null)
                floatArr[index] = val;
        }
    }
}