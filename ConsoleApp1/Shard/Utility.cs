using System;

namespace Shard
{
    public static class EasingExtensions
    {
        // -----------------------------
        // Sine
        // -----------------------------
        public static float EaseInSine(this float t)
        {
            return 1f - (float)Math.Cos((t * Math.PI) / 2f);
        }

        public static float EaseOutSine(this float t)
        {
            return (float)Math.Sin((t * Math.PI) / 2f);
        }

        public static float EaseInOutSine(this float t)
        {
            return -(float)(Math.Cos(Math.PI * t) - 1f) / 2f;
        }

        // -----------------------------
        // Quad
        // -----------------------------
        public static float EaseInQuad(this float t)
        {
            return t * t;
        }

        public static float EaseOutQuad(this float t)
        {
            return 1f - (1f - t) * (1f - t);
        }

        public static float EaseInOutQuad(this float t)
        {
            return t < 0.5f ? 2f * t * t : 1f - (float)Math.Pow(-2f * t + 2f, 2) / 2f;
        }

        // -----------------------------
        // Cubic
        // -----------------------------
        public static float EaseInCubic(this float t)
        {
            return t * t * t;
        }

        public static float EaseOutCubic(this float t)
        {
            return 1f - (float)Math.Pow(1f - t, 3);
        }

        public static float EaseInOutCubic(this float t)
        {
            return t < 0.5f ? 4f * t * t * t : 1f - (float)Math.Pow(-2f * t + 2f, 3) / 2f;
        }

        // -----------------------------
        // Quart
        // -----------------------------
        public static float EaseInQuart(this float t)
        {
            return t * t * t * t;
        }

        public static float EaseOutQuart(this float t)
        {
            return 1f - (float)Math.Pow(1f - t, 4);
        }

        public static float EaseInOutQuart(this float t)
        {
            return t < 0.5f ? 8f * t * t * t * t : 1f - (float)Math.Pow(-2f * t + 2f, 4) / 2f;
        }

        // -----------------------------
        // Quint
        // -----------------------------
        public static float EaseInQuint(this float t)
        {
            return t * t * t * t * t;
        }

        public static float EaseOutQuint(this float t)
        {
            return 1f - (float)Math.Pow(1f - t, 5);
        }

        public static float EaseInOutQuint(this float t)
        {
            return t < 0.5f ? 16f * t * t * t * t * t : 1f - (float)Math.Pow(-2f * t + 2f, 5) / 2f;
        }

        // -----------------------------
        // Expo
        // -----------------------------
        public static float EaseInExpo(this float t)
        {
            return t == 0f ? 0f : (float)Math.Pow(2, 10 * t - 10);
        }

        public static float EaseOutExpo(this float t)
        {
            return t == 1f ? 1f : 1f - (float)Math.Pow(2, -10 * t);
        }

        public static float EaseInOutExpo(this float t)
        {
            if (t == 0f)
                return 0f;
            if (t == 1f)
                return 1f;
            return t < 0.5f ?
                (float)Math.Pow(2, 20 * t - 10) / 2f :
                (2 - (float)Math.Pow(2, -20 * t + 10)) / 2f;
        }

        // -----------------------------
        // Circ
        // -----------------------------
        public static float EaseInCirc(this float t)
        {
            return 1f - (float)Math.Sqrt(1f - t * t);
        }

        public static float EaseOutCirc(this float t)
        {
            return (float)Math.Sqrt(1f - Math.Pow(t - 1, 2));
        }

        public static float EaseInOutCirc(this float t)
        {
            return t < 0.5f ?
                (1f - (float)Math.Sqrt(1f - Math.Pow(2 * t, 2))) / 2f :
                ((float)Math.Sqrt(1f - Math.Pow(-2 * t + 2, 2)) + 1f) / 2f;
        }

        // -----------------------------
        // Back
        // -----------------------------
        // Standard constant values for back easing
        public static float EaseInBack(this float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1;
            return c3 * t * t * t - c1 * t * t;
        }

        public static float EaseOutBack(this float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1;
            return 1f + c3 * (float)Math.Pow(t - 1, 3) + c1 * (float)Math.Pow(t - 1, 2);
        }

        public static float EaseInOutBack(this float t)
        {
            const float c1 = 1.70158f;
            const float c2 = c1 * 1.525f;
            return t < 0.5f
                ? (float)(Math.Pow(2 * t, 2) * ((c2 + 1) * 2 * t - c2)) / 2f
                : (float)(Math.Pow(2 * t - 2, 2) * ((c2 + 1) * (2 * t - 2) + c2) + 2) / 2f;
        }

        // -----------------------------
        // Elastic
        // -----------------------------
        public static float EaseInElastic(this float t)
        {
            const float c4 = (2 * (float)Math.PI) / 3;
            if (t == 0f)
                return 0f;
            if (t == 1f)
                return 1f;
            return -(float)Math.Pow(2, 10 * t - 10) * (float)Math.Sin((t * 10 - 10.75f) * c4);
        }

        public static float EaseOutElastic(this float t)
        {
            const float c4 = (2 * (float)Math.PI) / 3;
            if (t == 0f)
                return 0f;
            if (t == 1f)
                return 1f;
            return (float)Math.Pow(2, -10 * t) * (float)Math.Sin((t * 10 - 0.75f) * c4) + 1;
        }

        public static float EaseInOutElastic(this float t)
        {
            const float c5 = (2 * (float)Math.PI) / 4.5f;
            if (t == 0f)
                return 0f;
            if (t == 1f)
                return 1f;
            return t < 0.5f
                ? -(float)(Math.Pow(2, 20 * t - 10) * Math.Sin((20 * t - 11.125f) * c5)) / 2f
                : (float)(Math.Pow(2, -20 * t + 10) * Math.Sin((20 * t - 11.125f) * c5)) / 2f + 1;
        }

        // -----------------------------
        // Bounce
        // -----------------------------
        public static float EaseOutBounce(this float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;
            if (t < 1f / d1)
            {
                return n1 * t * t;
            }
            else if (t < 2f / d1)
            {
                t -= 1.5f / d1;
                return n1 * t * t + 0.75f;
            }
            else if (t < 2.5f / d1)
            {
                t -= 2.25f / d1;
                return n1 * t * t + 0.9375f;
            }
            else
            {
                t -= 2.625f / d1;
                return n1 * t * t + 0.984375f;
            }
        }

        public static float EaseInBounce(this float t)
        {
            return 1f - EaseOutBounce(1f - t);
        }

        public static float EaseInOutBounce(this float t)
        {
            return t < 0.5f
                ? (1f - EaseOutBounce(1f - 2 * t)) / 2f
                : (1f + EaseOutBounce(2 * t - 1)) / 2f;
        }
    }
}