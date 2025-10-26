using System;
using System.Drawing;
using System.Numerics;

namespace Shard.Shard
{
    internal class ColliderSegment : Collider
    {
        private DoomSegment segment;

        public ColliderSegment(CollisionHandler gob, DoomSegment segment) : base(gob)
        {
            this.segment = segment;
            calculateBoundingBox();
        }

        public ColliderSegment(CollisionHandler gob) : base(gob)
        {
        }

        public DoomSegment Segment { get => segment; set => segment = value; }

        public override Vector2? checkCollision(ColliderRect c)
        {
            bool left = segmentSegmentCollision(new Vector2(c.Left, c.Top), new Vector2(c.Left, c.Bottom));
            bool right = segmentSegmentCollision(new Vector2(c.Right, c.Top), new Vector2(c.Right, c.Bottom));
            bool top = segmentSegmentCollision(new Vector2(c.Left, c.Top), new Vector2(c.Right, c.Top));
            bool bottom = segmentSegmentCollision(new Vector2(c.Left, c.Bottom), new Vector2(c.Right, c.Bottom));

            if (left || right || top || bottom)
            {
                // return a penetration vector
                Vector2 intersection = new Vector2(this.Segment.StartPoint.X + (0.5f * (this.Segment.EndPoint.X - this.Segment.StartPoint.X)), this.Segment.StartPoint.Y + (0.5f * (this.Segment.EndPoint.Y - this.Segment.StartPoint.Y)));
                Vector2 midPoint = new Vector2(c.X, c.Y);
                Vector2 penetrationVector = intersection - midPoint;
                return penetrationVector;
            }
            return null;
        }

        private bool segmentSegmentCollision(Vector2 startPoint, Vector2 endPoint)
        {
            // calculate the distance to intersection point
            float uA = ((endPoint.X - startPoint.X) * (segment.StartPoint.Y - startPoint.Y) - (endPoint.Y - startPoint.Y) * (segment.StartPoint.X - startPoint.X)) / ((endPoint.Y - startPoint.Y) * (segment.EndPoint.X - segment.StartPoint.X) - (endPoint.X - startPoint.X) * (segment.EndPoint.Y - segment.StartPoint.Y));
            float uB = ((segment.EndPoint.X - segment.StartPoint.X) * (segment.StartPoint.Y - startPoint.Y) - (segment.EndPoint.Y - segment.StartPoint.Y) * (segment.StartPoint.X - startPoint.X)) / ((endPoint.Y - startPoint.Y) * (segment.EndPoint.X - segment.StartPoint.X) - (endPoint.X - startPoint.X) * (segment.EndPoint.Y - segment.StartPoint.Y));

            // if uA and uB are between 0-1, lines are colliding
            if (uA >= 0 && uA <= 1 && uB >= 0 && uB <= 1)
            {
                return true;
            }
            return false;
        }

        public override Vector2? checkCollision(ColliderSegment c)
        {
            // calculate the distance to intersection point
            float uA = ((c.segment.EndPoint.X - c.segment.StartPoint.X) * (segment.StartPoint.Y - c.segment.StartPoint.Y) - (c.segment.EndPoint.Y - c.segment.StartPoint.Y) * (segment.StartPoint.X - c.segment.StartPoint.X)) / ((c.segment.EndPoint.Y - c.segment.StartPoint.Y) * (segment.EndPoint.X - segment.StartPoint.X) - (c.segment.EndPoint.X - c.segment.StartPoint.X) * (segment.EndPoint.Y - segment.StartPoint.Y));
            float uB = ((segment.EndPoint.X - segment.StartPoint.X) * (segment.StartPoint.Y - c.segment.StartPoint.Y) - (segment.EndPoint.Y - segment.StartPoint.Y) * (segment.StartPoint.X - c.segment.StartPoint.X)) / ((c.segment.EndPoint.Y - c.segment.StartPoint.Y) * (segment.EndPoint.X - segment.StartPoint.X) - (c.segment.EndPoint.X - c.segment.StartPoint.X) * (segment.EndPoint.Y - segment.StartPoint.Y));

            // if uA and uB are between 0-1, lines are colliding
            if (uA >= 0 && uA <= 1 && uB >= 0 && uB <= 1)
            {
                // return a penetration vector
                Vector2 intersection = new Vector2(segment.StartPoint.X + (uA * (segment.EndPoint.X - segment.StartPoint.X)), segment.StartPoint.Y + (uA * (segment.EndPoint.Y - segment.StartPoint.Y)));
                Vector2 penetrationVector = intersection - c.segment.StartPoint;
                return penetrationVector;
            }
            return null;
        }

        public override Vector2? checkCollision(Vector2 c)
        {
            float d1 = Vector2.Distance(this.segment.StartPoint, c);
            float d2 = Vector2.Distance(this.segment.EndPoint, c);

            // since floats are so minutely accurate, add
            // a little buffer zone that will give collision
            float buffer = 0.1f;

            if (d1 + d2 >= this.segment.Length - buffer && d1 + d2 <= this.segment.Length + buffer)
            {
                Vector2 segmentDirection = Vector2.Normalize(this.segment.EndPoint - this.segment.StartPoint);
                Vector2 pointToSegmentStart = c - this.segment.StartPoint;
                float projectionLength = Vector2.Dot(pointToSegmentStart, segmentDirection);
                Vector2 closestPointOnSegment = this.segment.StartPoint + projectionLength * segmentDirection;
                Vector2 penetrationVector = c - closestPointOnSegment;
                return penetrationVector;
            }
            return null;
        }

        public override Vector2? checkCollision(ColliderCircle c)
        {
            bool isStartInside = c.checkCollision(this.segment.StartPoint) != null;
            bool isEndInside = c.checkCollision(this.segment.EndPoint) != null;

            // Calculate the closest point on the segment to the circle's center
            Vector2 segmentDirection = Vector2.Normalize(this.segment.EndPoint - this.segment.StartPoint);
            Vector2 segmentNormal = Vector2.Normalize(new Vector2(segmentDirection.Y, -segmentDirection.X));
            Vector2 pointToSegmentStart = new Vector2(c.X, c.Y) - this.segment.StartPoint;
            float projectionLength = MathF.Min(MathF.Max(Vector2.Dot(pointToSegmentStart, segmentDirection), 0), this.segment.Length);
            Vector2 closestPointOnSegment = this.segment.StartPoint + projectionLength * segmentDirection;

            // Check if the closest point is inside the circle
            float distanceToCircle = Vector2.Distance(closestPointOnSegment, new Vector2(c.X, c.Y));
            if (distanceToCircle <= c.Rad)
            {
                Vector2 circleOrigin = new Vector2(c.X, c.Y);
                Vector2 throughVector = circleOrigin + Vector2.Normalize(closestPointOnSegment - circleOrigin) * c.Rad; // The vector from the circle's origin through the wall in the direction of intersection
                Vector2 penetrationVector = Vector2.Dot(throughVector - closestPointOnSegment, segmentNormal) * segmentNormal; // The vector from the point of intersection to the through vector
                //Vector2 end = closestPointOnSegment + penetrationVector;
                //Bootstrap.getDisplay().drawFilledCircle((int)closestPointOnSegment.X, (int)closestPointOnSegment.Y, 5, 255, 0, 0, 255);
                //Bootstrap.getDisplay().drawLine((int)end.X, (int)end.Y, (int)closestPointOnSegment.X, (int)closestPointOnSegment.Y, 255, 255, 0, 255);
                return penetrationVector;
            }

            return null;
        }

        public Vector2? checkCollision(Vector2 o, Vector2 rayDir, out Vector2 intersection, out float u)
        {
            intersection = Vector2.Zero;
            u = 0f;

            var v1 = o - this.segment.StartPoint;
            var v2 = this.segment.EndPoint - this.segment.StartPoint;
            var v3 = new Vector2(-rayDir.Y, rayDir.X);

            var dot = Vector2.Dot(v2, v3);
            if (Math.Abs(dot) < 0.000001)
            {
                return null;
            }

            var t1 = (v2.X * v1.Y - v2.Y * v1.X) / dot;
            var t2 = Vector2.Dot(v1, v3) / dot;

            if (t1 >= 0.0 && (t2 >= 0.0 && t2 <= 1.0))
            {
                u = t2;
                intersection = o + t1 * rayDir;
                Vector2 penetrationVector = t1 * rayDir;
                return penetrationVector;
            }

            return null;
        }

        public override void drawMe(Color col)
        {
            Display d = Bootstrap.getDisplay();

            d.drawLine((int)this.segment.StartPoint.X, (int)this.segment.StartPoint.Y, (int)this.segment.EndPoint.X, (int)this.segment.EndPoint.Y, col);
        }

        public override float[] getMinAndMaxX()
        {
            return MinAndMaxX;
        }

        public override float[] getMinAndMaxY()
        {
            return MinAndMaxY;
        }

        public override void recalculate()
        {
            calculateBoundingBox();
        }

        private void calculateBoundingBox()
        {
            MinAndMaxX = new float[] { Math.Min(this.segment.StartPoint.X, this.segment.EndPoint.X), Math.Max(this.segment.StartPoint.X, this.segment.EndPoint.X) };
            MinAndMaxY = new float[] { Math.Min(this.segment.StartPoint.Y, this.segment.EndPoint.Y), Math.Max(this.segment.StartPoint.Y, this.segment.EndPoint.Y) };
        }

        public override bool checkAgainstRay(Vector2 origin, Vector2 direction, out Vector2 intersection)
        {
            bool hit = segment.getIntersectionWithRay(origin, direction, out Vector2 i, out float u);

            if (!hit)
            {
                intersection = Vector2.Zero;
                return false;
            }

            intersection = i;
            return true;
        }
    }
}