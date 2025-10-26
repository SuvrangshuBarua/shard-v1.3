using System;
using System.Numerics;

namespace Shard.Shard
{
    internal enum TEXTURE_WRAPPING_SPACE
    {
        LOCAL, // 1D - Texture wrapping is affected by the segment's length
        WORLD, // 1D - Texture wrapping is not affected by the segment's length
        SPRITESHEET // 2D - Cut out a section of the segment's spritesheet
    }

    internal class DoomSegment : GameObject
    {
        public DoomSegment(float x, float y, float width, float rotz, string texturePath, bool isTransparent)
        {
            Width = width;
            TexturePath = texturePath;
            IsTransparent = isTransparent;

            // Default texture wrapping is in world space with textures being repeated every 100 units, this is designed for walls
            TextureWrappingSpace = TEXTURE_WRAPPING_SPACE.WORLD;
            TextureWrappingValue = 100.0f;

            Transform.X = x;
            Transform.Y = y;
            Transform.Rotz = rotz;

            calculateCachedEndPoints();

            // Initialize segment as white
            r = 255;
            g = 255;
            b = 255;
            a = 255;
        }

        public DoomSegment(float x, float y, float width, float rotz, string texturePath, bool isTransparent, TEXTURE_WRAPPING_SPACE textureWrappingSpace, float textureWrappingValue)
        {
            Width = width;
            TexturePath = texturePath;
            IsTransparent = isTransparent;
            TextureWrappingSpace = textureWrappingSpace;
            TextureWrappingValue = textureWrappingValue;

            Transform.X = x;
            Transform.Y = y;
            Transform.Rotz = rotz;

            calculateCachedEndPoints();

            // Initialize segment as white
            r = 255;
            g = 255;
            b = 255;
            a = 255;
        }

        protected float width;

        protected Vector2 cachedStartPoint;
        protected Vector2 cachedEndPoint;
        private int r, g, b, a;
        private bool isLengthDirty = true;
        private float length;
        private float height = 1.0f;
        private float screenOffsetY = 0.0f;
        private bool isTransparent;
        private string texturePath;
        private Vector3 normal;
        private float textureU = 1.0f;
        private float textureV = 1.0f;
        private TEXTURE_WRAPPING_SPACE textureWrappingSpace;
        private float textureWrappingValue = 1.0f;

        // These are in texture space (i.e. pixels) and necessary when "cutting out" a section of the segment's texture, i.e. when using spritesheets
        private int textureCutoutWidth = 100;

        private int textureCutoutHeight = 100;
        private int textureCutoutOffsetX = 0;
        private int textureCutoutOffsetY = 0;

        public int R { get => r; set => r = value; }
        public int G { get => g; set => g = value; }
        public int B { get => b; set => b = value; }
        public int A { get => a; set => a = value; }
        public float Width { get => width; set => width = value; }

        public Vector2 StartPoint
        {
            get
            {
                return cachedStartPoint;
            }
        }

        public Vector2 EndPoint
        {
            get
            {
                return cachedEndPoint;
            }
        }

        public float Length { get => calculateLength(); }
        public float Height { get => height; set => height = value; }
        public float ScreenOffsetY { get => screenOffsetY; set => screenOffsetY = value; }
        public bool IsTransparent { get => isTransparent; set => isTransparent = value; }
        public string TexturePath { get => texturePath; set => texturePath = value; }
        public Vector3 Normal { get => normal; set => normal = value; }
        public float TextureU { get => textureU; set => textureU = value; }
        public float TextureV { get => textureV; set => textureV = value; }
        public TEXTURE_WRAPPING_SPACE TextureWrappingSpace { get => textureWrappingSpace; set => textureWrappingSpace = value; }
        public float TextureWrappingValue { get => textureWrappingValue; set => textureWrappingValue = value; }
        public int TextureCutoutWidth { get => textureCutoutWidth; set => textureCutoutWidth = value; }
        public int TextureCutoutHeight { get => textureCutoutHeight; set => textureCutoutHeight = value; }
        public int TextureCutoutOffsetX { get => textureCutoutOffsetX; set => textureCutoutOffsetX = value; }
        public int TextureCutoutOffsetY { get => textureCutoutOffsetY; set => textureCutoutOffsetY = value; }

        private float calculateLength()
        {
            if (!isLengthDirty)
                return length;

            length = Vector2.Distance(cachedStartPoint, cachedEndPoint);
            isLengthDirty = false;
            return length;
        }

        public bool getIntersectionWithRay(Vector2 o, Vector2 d, out Vector2 intersection, out float u)
        {
            intersection = Vector2.Zero;
            u = 0f;

            var v1 = o - cachedStartPoint;
            var v2 = cachedEndPoint - cachedStartPoint;
            var v3 = new Vector2(-d.Y, d.X);

            var dot = Vector2.Dot(v2, v3);
            if (Math.Abs(dot) < 0.000001)
            {
                return false;
            }

            var t1 = (v2.X * v1.Y - v2.Y * v1.X) / dot;
            var t2 = Vector2.Dot(v1, v3) / dot;

            if (t1 >= 0.0 && (t2 >= 0.0 && t2 <= 1.0))
            {
                u = t2;
                intersection = o + t1 * d;
                return true;
            }

            return false;
        }

        public Vector2 getMidpoint()
        {
            return new Vector2(Transform.X, Transform.Y);
        }

        internal void calculateCachedEndPoints()
        {
            // Creates and applies a "rotation matrix" manually
            float angle = Transform.Rotz * 0.01745329252f; // This constant is the result of PI / 180, or conversion from degrees to radians
            float ca = MathF.Cos(angle);
            float sa = MathF.Sin(angle);
            float hw = width / 2;
            float hwca = hw * ca;
            float hwsa = hw * sa;

            cachedStartPoint = new Vector2(Transform.X + -hwca, Transform.Y + hwsa);
            cachedEndPoint = new Vector2(Transform.X + hwca, Transform.Y + -hwsa);

            isLengthDirty = true;
        }

        // Returns (float u, float yStart, float height)
        public (float, float, float) getTextureCoordinate(float u, float textureWidth, float textureHeight)
        {
            // The texture is repeated from startPoint n (= textureWrappingMethod) times
            if (TextureWrappingSpace == TEXTURE_WRAPPING_SPACE.LOCAL)
            {
                float t = u * textureWrappingValue;
                return (t - MathF.Floor(t), 0, 1); // Perform "modulo" on float to ensure repeating texture
            }

            // The texture is repeated from startPoint every n (= textureWrappingValue) units in world space
            if (TextureWrappingSpace == TEXTURE_WRAPPING_SPACE.WORLD)
            {
                float t = u / MathF.Max(0.001f, textureWrappingValue / MathF.Max(0.001f, Length));
                return (t - MathF.Floor(t), 0, 1); // Perform "modulo" on float to ensure repeating texture
            }

            if (TextureWrappingSpace == TEXTURE_WRAPPING_SPACE.SPRITESHEET)
            {
                float t = (1 - u) * textureCutoutWidth / textureWidth + textureCutoutOffsetX / textureWidth;
                return (t - MathF.Floor(t), textureCutoutOffsetY / textureHeight, textureCutoutHeight / textureHeight);
            }

            // Return u itself as a baseline (this should never happen though)
            return (u, 0, 1);
        }

        public override void killMe()
        {
            base.killMe();
            DoomRenderer.getInstance().RemoveDoomSegment(this);
        }
    }
}