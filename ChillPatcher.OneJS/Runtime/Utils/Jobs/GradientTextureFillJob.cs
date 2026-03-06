using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace OneJS.Utils {
    [BurstCompile]
    public struct GradientTextureFillJob : IJobParallelFor {
        public NativeArray<Color32> colors;
        public int width;
        public int height;
        public Color32 topRightColor;

        // Convenience static method for easy calling from JS for example
        public static void Run(NativeArray<Color32> colors, int width, int height, Color32 topRightColor) {
            var job = new GradientTextureFillJob {
                colors = colors,
                width = width,
                height = height,
                topRightColor = topRightColor
            };
            job.Schedule(colors.Length, 64).Complete();
        }

        public void Execute(int index) {
            int x = index % width;
            int y = index / height;
            float fx = (float)x / (float)width;
            float fy = (float)y / (float)height;

            Color32 leftColor = Color32.Lerp(Color.black, Color.white, fy);
            Color32 rightColor = Color32.Lerp(Color.black, topRightColor, fy);
            Color32 pixelColor = Color32.Lerp(leftColor, rightColor, fx);

            colors[index] = pixelColor;
        }
    }
}