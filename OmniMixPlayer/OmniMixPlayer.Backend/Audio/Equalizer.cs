using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace OmniMixPlayer.Backend.Audio
{
    public enum EqualizerFilterType
    {
        Peaking,
        LowShelf,
        HighShelf,
        LowPass,
        HighPass
    }

    public class EqualizerPoint
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public float Frequency { get; set; } = 1000f;
        public float GainDb { get; set; } = 0f;
        public float Q { get; set; } = 1.0f;
        public EqualizerFilterType Type { get; set; } = EqualizerFilterType.Peaking;
    }

    public class EqualizerState
    {
        public bool Enabled { get; set; } = false;
        public float GlobalGainDb { get; set; } = 0f;
        public bool SoftClipEnabled { get; set; } = true;
        public List<EqualizerPoint> Points { get; set; } = new List<EqualizerPoint>();
    }

    public struct ChannelState
    {
        public float S1;
        public float S2;
    }

    public class BiquadFilter
    {
        public string PointId { get; }
        public float Frequency { get; }
        public float GainDb { get; }
        public float Q { get; }
        public EqualizerFilterType Type { get; }

        public float B0, B1, B2, A1, A2;
        public ChannelState[] States;
        public float LastSampleRate { get; private set; }

        public BiquadFilter(string pointId, float frequency, float gainDb, float q, EqualizerFilterType type, int channels)
        {
            PointId = pointId;
            Frequency = Math.Clamp(frequency, 20f, 22000f);
            GainDb = Math.Clamp(gainDb, -24f, 24f);
            Q = Math.Clamp(q, 0.1f, 20f);
            Type = type;
            States = new ChannelState[channels];
        }

        public void EnsureChannels(int channels)
        {
            if (States == null || States.Length != channels)
            {
                var newStates = new ChannelState[channels];
                if (States != null)
                {
                    int copyCount = Math.Min(States.Length, channels);
                    Array.Copy(States, newStates, copyCount);
                }
                States = newStates;
            }
        }

        public void CalculateCoefficients(float sampleRate)
        {
            LastSampleRate = sampleRate;
            float w0 = 2f * MathF.PI * Frequency / sampleRate;
            float sinW0 = MathF.Sin(w0);
            float cosW0 = MathF.Cos(w0);
            float alpha = sinW0 / (2f * Q);

            float a0 = 1f;

            switch (Type)
            {
                case EqualizerFilterType.Peaking:
                    {
                        float A = MathF.Pow(10f, GainDb / 40f);
                        a0 = 1f + alpha / A;
                        B0 = (1f + alpha * A) / a0;
                        B1 = (-2f * cosW0) / a0;
                        B2 = (1f - alpha * A) / a0;
                        A1 = (-2f * cosW0) / a0;
                        A2 = (1f - alpha / A) / a0;
                    }
                    break;

                case EqualizerFilterType.LowShelf:
                    {
                        float A = MathF.Pow(10f, GainDb / 40f);
                        float sqrtA = MathF.Sqrt(A);
                        float twoSqrtAAlpha = 2f * sqrtA * alpha;

                        a0 = (A + 1f) + (A - 1f) * cosW0 + twoSqrtAAlpha;
                        B0 = (A * ((A + 1f) - (A - 1f) * cosW0 + twoSqrtAAlpha)) / a0;
                        B1 = (2f * A * ((A - 1f) - (A + 1f) * cosW0)) / a0;
                        B2 = (A * ((A + 1f) - (A - 1f) * cosW0 - twoSqrtAAlpha)) / a0;
                        A1 = (-2f * ((A - 1f) + (A + 1f) * cosW0)) / a0;
                        A2 = ((A + 1f) + (A - 1f) * cosW0 - twoSqrtAAlpha) / a0;
                    }
                    break;

                case EqualizerFilterType.HighShelf:
                    {
                        float A = MathF.Pow(10f, GainDb / 40f);
                        float sqrtA = MathF.Sqrt(A);
                        float twoSqrtAAlpha = 2f * sqrtA * alpha;

                        a0 = (A + 1f) - (A - 1f) * cosW0 + twoSqrtAAlpha;
                        B0 = (A * ((A + 1f) + (A - 1f) * cosW0 + twoSqrtAAlpha)) / a0;
                        B1 = (-2f * A * ((A - 1f) + (A + 1f) * cosW0)) / a0;
                        B2 = (A * ((A + 1f) + (A - 1f) * cosW0 - twoSqrtAAlpha)) / a0;
                        A1 = (2f * ((A - 1f) - (A + 1f) * cosW0)) / a0;
                        A2 = ((A + 1f) - (A - 1f) * cosW0 - twoSqrtAAlpha) / a0;
                    }
                    break;

                case EqualizerFilterType.LowPass:
                    {
                        a0 = 1f + alpha;
                        B0 = ((1f - cosW0) / 2f) / a0;
                        B1 = (1f - cosW0) / a0;
                        B2 = ((1f - cosW0) / 2f) / a0;
                        A1 = (-2f * cosW0) / a0;
                        A2 = (1f - alpha) / a0;
                    }
                    break;

                case EqualizerFilterType.HighPass:
                    {
                        a0 = 1f + alpha;
                        B0 = ((1f + cosW0) / 2f) / a0;
                        B1 = -(1f + cosW0) / a0;
                        B2 = ((1f + cosW0) / 2f) / a0;
                        A1 = (-2f * cosW0) / a0;
                        A2 = (1f - alpha) / a0;
                    }
                    break;
            }
        }
    }

    public class Equalizer
    {
        private volatile BiquadFilter[] _activeFilters = Array.Empty<BiquadFilter>();
        private readonly object _stateLock = new object();

        public bool Enabled { get; private set; }
        public float GlobalGainDb { get; private set; }
        public bool SoftClipEnabled { get; private set; } = true;
        public EqualizerState CurrentState { get; private set; } = new EqualizerState();

        public void UpdateState(EqualizerState state)
        {
            if (state == null) return;

            lock (_stateLock)
            {
                Enabled = state.Enabled;
                GlobalGainDb = Math.Clamp(state.GlobalGainDb, -24f, 24f);
                SoftClipEnabled = state.SoftClipEnabled;
                CurrentState = state;

                // Build new active filters
                var newFilters = new List<BiquadFilter>();
                var oldFiltersMap = new Dictionary<string, BiquadFilter>();

                // Keep references to old filters if parameters are similar to prevent click glitches
                foreach (var oldF in _activeFilters)
                {
                    if (oldF.PointId != null)
                        oldFiltersMap[oldF.PointId] = oldF;
                }

                foreach (var pt in state.Points)
                {
                    if (pt == null) continue;

                    // If a point with the same ID, frequency, Q and type exists, we reuse it to keep the history state
                    if (oldFiltersMap.TryGetValue(pt.Id, out var oldFilter) &&
                        MathF.Abs(oldFilter.Frequency - pt.Frequency) < 0.01f &&
                        MathF.Abs(oldFilter.Q - pt.Q) < 0.01f &&
                        MathF.Abs(oldFilter.GainDb - pt.GainDb) < 0.01f &&
                        oldFilter.Type == pt.Type)
                    {
                        newFilters.Add(oldFilter);
                    }
                    else
                    {
                        // Create a new biquad filter. It will calculate coefficients on the fly in the process loop.
                        // We default to 2 channels (stereo), but the process loop will dynamically adjust if needed.
                        var filter = new BiquadFilter(pt.Id, pt.Frequency, pt.GainDb, pt.Q, pt.Type, 2);

                        // If only gain or frequency slightly changed, we can copy the old history state to minimize audio clicks!
                        if (oldFilter != null)
                        {
                            filter.States = oldFilter.States;
                        }

                        newFilters.Add(filter);
                    }
                }

                // Swap array atomically
                _activeFilters = newFilters.ToArray();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Process(float[] buffer, int frameCount, int channels, int sampleRate)
        {
            if (!Enabled) return;

            float multiplier = MathF.Pow(10f, GlobalGainDb / 20f);

            var filters = _activeFilters;
            if (filters == null || filters.Length == 0)
            {
                if (MathF.Abs(multiplier - 1.0f) > 0.0001f)
                {
                    int sampleCount = frameCount * channels;
                    for (int i = 0; i < sampleCount; i++)
                    {
                        buffer[i] *= multiplier;
                    }
                }
                return;
            }

            // Ensure filter coefficients are updated for current stream parameters
            for (int j = 0; j < filters.Length; j++)
            {
                var filter = filters[j];
                filter.EnsureChannels(channels);
                if (MathF.Abs(filter.LastSampleRate - sampleRate) > 0.1f)
                {
                    filter.CalculateCoefficients(sampleRate);
                }
            }

            int filterCount = filters.Length;

            // Highly optimized sample processing loop
            for (int i = 0; i < frameCount; i++)
            {
                int baseIndex = i * channels;
                for (int ch = 0; ch < channels; ch++)
                {
                    float x = buffer[baseIndex + ch];

                    for (int f = 0; f < filterCount; f++)
                    {
                        var filter = filters[f];

                        // Transposed Direct Form II Difference Equation
                        float y = x * filter.B0 + filter.States[ch].S1;
                        filter.States[ch].S1 = x * filter.B1 - y * filter.A1 + filter.States[ch].S2;
                        filter.States[ch].S2 = x * filter.B2 - y * filter.A2;

                        x = y;
                    }

                    buffer[baseIndex + ch] = SoftClipEnabled ? SoftClip(x * multiplier) : (x * multiplier);
                }
            }
        }

        /// <summary>
        /// Smooth saturation near full-scale. Transparent for |x| <= 0.85 and
        /// asymptotically approaches ±1 above, preventing hard clipping distortion
        /// when EQ boost + global gain push the signal past [-1, 1].
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float SoftClip(float x)
        {
            const float knee = 0.85f;
            float a = MathF.Abs(x);
            if (a <= knee) return x;
            float over = (a - knee) / (1f - knee);
            return MathF.CopySign(knee + (1f - knee) * over / (1f + over), x);
        }
    }
}
