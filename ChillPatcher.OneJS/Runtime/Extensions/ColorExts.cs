using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace OneJS.Extensions {
    public static class ColorExts {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToVector3(this Color color) {
            return new Vector3(color.r, color.g, color.b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ToFloat3(this Color color) {
            return new float3(color.r, color.g, color.b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 ToFloat4(this Color color) {
            return new float4(color.r, color.g, color.b, color.a);
        }

        public static Color ToColor(this float4 f4) {
            return new Color(f4.x, f4.y, f4.z, f4.w);
        }

        public static Color ToColor(this float3 f3) {
            return new Color(f3.x, f3.y, f3.z, 1f);
        }

        public static Color32 ToColor32(this float3 f3) {
            return (Color32)new Color(f3.x, f3.y, f3.z, 1f);
        }

        public static Color32 ToColor32(this float4 f4) {
            return (Color32)f4.ToColor();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ToFloat3(this Color32 color) {
            // return new float4(color.r / 255f, color.g / 255f, color.b / 255f, color.a / 255f);
            return ((Color)color).ToFloat3();
        }

        public static Color ToColor(this Color32 color) {
            return ((Color)color);
        }

        public static Color32 ToColor32(this Color color) {
            return (Color32)color;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 ToFloat4(this Color32 color) {
            // return new float4(color.r / 255f, color.g / 255f, color.b / 255f, color.a / 255f);
            return ((Color)color).ToFloat4();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 ToInt4(this Color32 color) {
            return new int4(color.r, color.g, color.b, color.a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color32 ToColor32(this int4 color) {
            return new Color32((byte)color.x, (byte)color.y, (byte)color.z, (byte)color.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt(this Color32 color) {
            return (uint)((color.a << 24) | (color.r << 16) | (color.g << 8) | (color.b << 0));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color32 ToColor32(this uint color) {
            return new Color32((byte)(color >> 16), (byte)(color >> 8), (byte)(color >> 0), (byte)(color >> 24));
        }
    }
}