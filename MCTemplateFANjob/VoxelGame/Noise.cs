using System;

namespace VoxelGame
{
    public static class Noise
    {
        // integer hash noise - returns -1..1
        static float NoiseInt(int x, int y)
        {
            int n = x + y * 57;
            n = (n << 13) ^ n;
            int nn = (n * (n * n * 15731 + 789221) + 1376312589);
            nn = (nn & 0x7fffffff);
            return 1.0f - (nn / 1073741824.0f);
        }

        static float Interpolate(float a, float b, float t)
        {
            float ft = t * (float)Math.PI;
            float f = (1f - (float)Math.Cos(ft)) * 0.5f;
            return a * (1f - f) + b * f;
        }

        static float SmoothNoise(float x, float y)
        {
            int ix = (int)Math.Floor(x);
            int iy = (int)Math.Floor(y);

            float xf = x - ix;
            float yf = y - iy;

            float v1 = NoiseInt(ix, iy);
            float v2 = NoiseInt(ix + 1, iy);
            float v3 = NoiseInt(ix, iy + 1);
            float v4 = NoiseInt(ix + 1, iy + 1);

            float i1 = Interpolate(v1, v2, xf);
            float i2 = Interpolate(v3, v4, xf);
            return Interpolate(i1, i2, yf);
        }

        public static float FractalNoise2D(float x, float y, int octaves)
        {
            float total = 0f;
            float frequency = 1f;
            float amplitude = 1f;
            float max = 0f;

            for (int i = 0; i < octaves; i++)
            {
                total += SmoothNoise(x * frequency, y * frequency) * amplitude;
                max += amplitude;
                amplitude *= 0.5f;
                frequency *= 2f;
            }

            return total / max; // roughly in -1..1
        }
    }
}
