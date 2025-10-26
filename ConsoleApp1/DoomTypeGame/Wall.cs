using Shard.Shard;

namespace Shard
{
    internal class Wall : DoomSegment, CollisionHandler
    {
        public Wall(float x, float y, float width, float rotz, string texturePath, bool isTransparent) : base(x, y, width, rotz, texturePath, isTransparent)
        {
            setPhysicsEnabled();
            MyBody.addSegmentCollider(this);
            MyBody.Kinematic = true;
            MyBody.CollisionLayer = (int)DoomTypeGame.CollisionLayer.STATIC;
            addTag("Wall");
        }

        public Wall(float x, float y, float width, float rotz, string texturePath, bool isTransparent, TEXTURE_WRAPPING_SPACE textureWrappingSpace, float textureWrappingValue) : base(x, y, width, rotz, texturePath, isTransparent, textureWrappingSpace, textureWrappingValue)
        {
            setPhysicsEnabled();
            MyBody.addSegmentCollider(this);
            MyBody.Kinematic = true;
            MyBody.CollisionLayer = (int)DoomTypeGame.CollisionLayer.STATIC;
            addTag("Wall");
        }

        public void onCollisionEnter(PhysicsBody x)
        {
            //Debug.Log("Wall collision enter");
        }

        public void onCollisionExit(PhysicsBody x)
        {
            //Debug.Log("Wall collision exit");
        }

        public void onCollisionStay(PhysicsBody x)
        {
            //Debug.Log("Wall collision stay");
        }
    }
}