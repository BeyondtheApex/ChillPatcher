using System;
using System.Runtime.CompilerServices;

namespace OmniMixPlayer.Backend.Audio
{
    public class VolumeNode
    {
        private float _volume = 1.0f;

        public float Volume
        {
            get => _volume;
            set => _volume = Math.Clamp(value, 0f, 1f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Process(float[] buffer, int frameCount, int channels)
        {
            float vol = _volume;
            // Early return if volume is at maximum (1.0) to avoid unnecessary multiplications
            if (MathF.Abs(vol - 1.0f) <= 0.0001f)
                return;

            int sampleCount = frameCount * channels;
            for (int i = 0; i < sampleCount; i++)
            {
                buffer[i] *= vol;
            }
        }
    }
}
