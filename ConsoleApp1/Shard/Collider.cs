/*
*
*   The abstract collider class that is the base of all collisions.   Specific variations should
*       extend from this or one of its children.
*   @author Michael Heron
*   @version 1.0
*
*/

using Shard.Shard;
using System.Drawing;
using System.Numerics;

namespace Shard
{
    internal abstract class Collider
    {
        private CollisionHandler gameObject;
        private float[] minAndMaxX;
        private float[] minAndMaxY;
        private bool rotateAtOffset;
        private int collisionLayer = 0;

        public abstract void recalculate();

        public Collider(CollisionHandler gob)
        {
            gameObject = gob;
            MinAndMaxX = new float[2];
            MinAndMaxY = new float[2];
        }

        internal CollisionHandler GameObject { get => gameObject; set => gameObject = value; }
        internal float[] MinAndMaxX { get => minAndMaxX; set => minAndMaxX = value; }
        internal float[] MinAndMaxY { get => minAndMaxY; set => minAndMaxY = value; }
        public bool RotateAtOffset { get => rotateAtOffset; set => rotateAtOffset = value; }

        public abstract Vector2? checkCollision(ColliderRect c);

        public abstract Vector2? checkCollision(Vector2 c);

        public abstract Vector2? checkCollision(ColliderCircle c);

        public abstract Vector2? checkCollision(ColliderSegment c);

        public virtual Vector2? checkCollision(Collider c)
        {
            if (c is ColliderRect)
            {
                return checkCollision((ColliderRect)c);
            }

            if (c is ColliderCircle)
            {
                return checkCollision((ColliderCircle)c);
            }

            if (c is ColliderSegment)
            {
                return checkCollision((ColliderSegment)c);
            }

            Debug.getInstance().log("Bug");
            // Not sure how we got here but c'est la vie
            return null;
        }

        public abstract bool checkAgainstRay(Vector2 origin, Vector2 direction, out Vector2 intersection);

        public abstract void drawMe(Color col);

        public abstract float[] getMinAndMaxX();

        public abstract float[] getMinAndMaxY();
    }
}