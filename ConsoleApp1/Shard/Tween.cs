using Shard.Shard;
using System;
using System.Collections;
using System.Numerics;

namespace Shard
{
    internal class Tween
    {
        private IEnumerator coroutine;
        private System.Func<float, float> easingFunction = (x) => x; // Default is linear
        private Action onComplete = null;

        public static Tween MoveTo(DoomSegment segment, Vector2 to, float duration)
        {
            Vector2 from = new Vector2(segment.Transform.X, segment.Transform.Y);
            Tween tween = new Tween();
            tween.coroutine = Bootstrap.getRunningGame().StartCoroutine(tweenFloat(tween, 0, 1, duration, (value) =>
            {
                if (segment == null)
                {
                    Bootstrap.getRunningGame().StopCoroutine(tween.coroutine);
                    return;
                }

                Vector2 p = Vector2.Lerp(from, to, value);

                segment.Transform.X = p.X;
                segment.Transform.Y = p.Y;
                segment.calculateCachedEndPoints();
            }));
            return tween;
        }

        public static Tween Value(float from, float to, float duration, Action<float> onUpdate)
        {
            Tween tween = new Tween();
            tween.coroutine = Bootstrap.getRunningGame().StartCoroutine(tweenFloat(tween, from, to, duration, onUpdate));
            return tween;
        }

        // The value returned in onUpdate is already eased
        private static IEnumerator tweenFloat(Tween owner, float from, float to, float duration, Action<float> onUpdate)
        {
            long startMillis = Bootstrap.getCurrentMillis();
            long durationMillis = Bootstrap.getCurrentMillis() + (long)(duration * 1000) - startMillis;

            onUpdate(from);

            while (true)
            {
                long currentMillis = Bootstrap.getCurrentMillis() - startMillis;

                float t = (float)currentMillis / (float)durationMillis;

                if (t >= 1.0f)
                {
                    onUpdate(to);

                    if (owner.onComplete != null)
                    {
                        owner.onComplete();
                    }
                    break;
                }

                float range = to - from;

                float value = range * owner.easingFunction(t) + from;

                onUpdate(value);

                yield return null;
            }
        }

        // On Complete
        internal Tween setOnComplete(Action onComplete)
        {
            this.onComplete = onComplete;
            return this;
        }

        // -----------------------------
        // Sine
        // -----------------------------
        internal Tween setEaseInSine()
        { easingFunction = (x) => x.EaseInSine(); return this; }

        internal Tween setEaseOutSine()
        { easingFunction = (x) => x.EaseOutSine(); return this; }

        internal Tween setEaseInOutSine()
        { easingFunction = (x) => x.EaseInOutSine(); return this; }

        // -----------------------------
        // Quad
        // -----------------------------
        internal Tween setEaseInQuad()
        { easingFunction = (x) => x.EaseInQuad(); return this; }

        internal Tween setEaseOutQuad()
        { easingFunction = (x) => x.EaseOutQuad(); return this; }

        internal Tween setEaseInOutQuad()
        { easingFunction = (x) => x.EaseInOutQuad(); return this; }

        // -----------------------------
        // Cubic
        // -----------------------------
        internal Tween setEaseInCubic()
        { easingFunction = (x) => x.EaseInCubic(); return this; }

        internal Tween setEaseOutCubic()
        { easingFunction = (x) => x.EaseOutCubic(); return this; }

        internal Tween setEaseInOutCubic()
        { easingFunction = (x) => x.EaseInOutCubic(); return this; }

        // -----------------------------
        // Quart
        // -----------------------------
        internal Tween setEaseInQuart()
        { easingFunction = (x) => x.EaseInQuart(); return this; }

        internal Tween setEaseOutQuart()
        { easingFunction = (x) => x.EaseOutQuart(); return this; }

        internal Tween setEaseInOutQuart()
        { easingFunction = (x) => x.EaseInOutQuart(); return this; }

        // -----------------------------
        // Quint
        // -----------------------------
        internal Tween setEaseInQuint()
        { easingFunction = (x) => x.EaseInQuint(); return this; }

        internal Tween setEaseOutQuint()
        { easingFunction = (x) => x.EaseOutQuint(); return this; }

        internal Tween setEaseInOutQuint()
        { easingFunction = (x) => x.EaseInOutQuint(); return this; }

        // -----------------------------
        // Expo
        // -----------------------------
        internal Tween setEaseInExpo()
        { easingFunction = (x) => x.EaseInExpo(); return this; }

        internal Tween setEaseOutExpo()
        { easingFunction = (x) => x.EaseOutExpo(); return this; }

        internal Tween setEaseInOutExpo()
        { easingFunction = (x) => x.EaseInOutExpo(); return this; }

        // -----------------------------
        // Circ
        // -----------------------------
        internal Tween setEaseInCirc()
        { easingFunction = (x) => x.EaseInCirc(); return this; }

        internal Tween setEaseOutCirc()
        { easingFunction = (x) => x.EaseOutCirc(); return this; }

        internal Tween setEaseInOutCirc()
        { easingFunction = (x) => x.EaseInOutCirc(); return this; }

        // -----------------------------
        // Back
        // -----------------------------
        internal Tween setEaseInBack()
        { easingFunction = (x) => x.EaseInBack(); return this; }

        internal Tween setEaseOutBack()
        { easingFunction = (x) => x.EaseOutBack(); return this; }

        internal Tween setEaseInOutBack()
        { easingFunction = (x) => x.EaseInOutBack(); return this; }

        // -----------------------------
        // Elastic
        // -----------------------------
        internal Tween setEaseInElastic()
        { easingFunction = (x) => x.EaseInElastic(); return this; }

        internal Tween setEaseOutElastic()
        { easingFunction = (x) => x.EaseOutElastic(); return this; }

        internal Tween setEaseInOutElastic()
        { easingFunction = (x) => x.EaseInOutElastic(); return this; }

        // -----------------------------
        // Bounce
        // -----------------------------
        internal Tween setEaseInBounce()
        { easingFunction = (x) => x.EaseInBounce(); return this; }

        internal Tween setEaseOutBounce()
        { easingFunction = (x) => x.EaseOutBounce(); return this; }

        internal Tween setEaseInOutBounce()
        { easingFunction = (x) => x.EaseInOutBounce(); return this; }
    }
}