using System;
using System.Numerics;

namespace Shard.Shard
{
    internal class DoomBillboard : DoomSegment
    {
        public DoomBillboard(float x, float y, float width, float height, string texturePath, bool isTransparent) : base(x, y, width, 0, texturePath, isTransparent)
        {
            this.Height = height;

            // Billboards have a default texture wrapping space of LOCAL, with the texture being stretched out once across the billboard
            TextureWrappingSpace = TEXTURE_WRAPPING_SPACE.LOCAL;
            TextureWrappingValue = 1.0f;
        }

        public DoomBillboard(float x, float y, float width, float height, float screenOffsetY, string texturePath, bool isTransparent) : base(x, y, width, 0, texturePath, isTransparent)
        {
            this.Height = height;
            this.ScreenOffsetY = screenOffsetY;

            // Billboards have a default texture wrapping space of LOCAL, with the texture being stretched out once across the billboard
            TextureWrappingSpace = TEXTURE_WRAPPING_SPACE.LOCAL;
            TextureWrappingValue = 1.0f;
        }

        public DoomBillboard(float x, float y, float width, float height, string texturePath, bool isTransparent, TEXTURE_WRAPPING_SPACE textureWrappingMethod, float textureWrappingValue) : base(x, y, width, 0, texturePath, isTransparent, textureWrappingMethod, textureWrappingValue)
        {
            this.Height = height;
        }

        // This method differs from RotateTowardsPlane as the input variables are the same for each billboard and can thus be provided precomputed from the renderer, speeding up FPS
        public DoomSegment SetBillboardFacing(float rotzInRadians, float rotzInDegrees, float rotCos, float rotSin)
        {
            Transform.Rotz = rotzInDegrees;
            float hw = width * 0.5f;
            float hwca = hw * rotCos; // HalfWidthCosAngle
            float hwsa = hw * rotSin; // HaldWidthSinAngle

            cachedStartPoint = new Vector2(Transform.X + -hwca, Transform.Y + hwsa);
            cachedEndPoint = new Vector2(Transform.X + hwca, Transform.Y + -hwsa);

            return this;
        }

        public DoomSegment RotateTowardsPlane(Vector2 camForward)
        {
            // Set the Billboard's rotation in the up direction to face the camera's plane
            Transform.Rotz = MathF.Atan2(camForward.X, camForward.Y) * 57.295779513f; // This constant is the result of 180 / PI, or conversion from radians to degrees
            calculateCachedEndPoints();

            return this;
        }

        public override void killMe()
        {
            base.killMe();
            DoomRenderer.getInstance().RemoveDoomBillboard(this);
        }
    }
}