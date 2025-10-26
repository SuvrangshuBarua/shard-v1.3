namespace Shard.Shard
{
    internal enum AnimationType
    {
        ONCE,
        LOOP
    }

    // Specifies coordinates for a cutout of the animation's spritesheet for this keyframe
    internal struct SpritesheetAnimationKeyframe
    {
        public int offsetX;
        public int offsetY;
        public int height;
        public int width;
        public float screenOffsetY;
    }

    internal struct SpritesheetAnimation
    {
        public SpritesheetAnimation(string name, string texturePath, float duration, AnimationType type)
        {
            this.name = name;
            this.texturePath = texturePath;
            this.duration = duration;
            this.type = type;
        }

        public string name;
        public string texturePath;
        public SpritesheetAnimationKeyframe[] keyFrames;
        public float duration;
        public AnimationType type;
    }

    internal class AnimatedBillboard : DoomBillboard
    {
        protected SpritesheetAnimation? currentAnimation;
        protected long currentAnimationStartTime;

        public AnimatedBillboard(float x, float y, float width, float height, string texturePath, bool isTransparent) : base(x, y, width, height, texturePath, isTransparent, TEXTURE_WRAPPING_SPACE.SPRITESHEET, 1.0f)
        {
        }

        protected void playAnimation(SpritesheetAnimation animation)
        {
            TexturePath = animation.texturePath;
            currentAnimationStartTime = Bootstrap.getCurrentMillis();
            currentAnimation = animation;
        }

        public override void update()
        {
            base.update();

            tickAnimation();
        }

        private void tickAnimation()
        {
            if (!currentAnimation.HasValue)
                return;

            long millis = currentAnimation.Value.type == AnimationType.LOOP ? ((Bootstrap.getCurrentMillis() - currentAnimationStartTime) % (long)(currentAnimation.Value.duration * 1000)) : (Bootstrap.getCurrentMillis() - currentAnimationStartTime);

            float timeSinceAnimationStart = (millis) / 1000f;
            float t = millis / (currentAnimation.Value.duration * 1000);
            int frameIndex = (int)(t * (currentAnimation.Value.keyFrames.Length));
            if (frameIndex >= currentAnimation.Value.keyFrames.Length)
            {
                if (currentAnimation.Value.type == AnimationType.ONCE)
                {
                    currentAnimation = null;
                    return;
                }
            }
            SpritesheetAnimationKeyframe frame = currentAnimation.Value.keyFrames[frameIndex];
            TextureCutoutWidth = frame.width;
            TextureCutoutHeight = frame.height;
            TextureCutoutOffsetX = frame.offsetX;
            TextureCutoutOffsetY = frame.offsetY;
            ScreenOffsetY = frame.screenOffsetY;
        }
    }
}