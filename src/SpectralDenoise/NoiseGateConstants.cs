using System;

namespace SpectralDenoise
{
    internal static class NoiseGateConstants
    {
        // Default threshold in dB below which the gate closes
        public const float DefaultThresholdDb = -45f;

        // Attack time constraints in milliseconds
        public const float MinAttackMs = 0.1f;
        public const float MaxAttackMs = 1000f;

        // Release time constraints in milliseconds
        public const float MinReleaseMs = 1f;
        public const float MaxReleaseMs = 5000f;

        // Minimum time constant in seconds to prevent instability
        public const float MinTimeSeconds = 0.001f;
    }
}
