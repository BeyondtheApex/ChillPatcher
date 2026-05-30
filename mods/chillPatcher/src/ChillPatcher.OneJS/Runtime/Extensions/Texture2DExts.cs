using Unity.Collections;
using UnityEngine;

namespace OneJS.Extensions {
    public static class Texture2DExts {
        public static NativeArray<Color32> GetRawDataColor32(this Texture2D texture) {
            var nativeArray = texture.GetRawTextureData<Color32>();
            return nativeArray;
        }
    }
}