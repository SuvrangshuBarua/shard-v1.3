using Shard.Shard;
using System.Numerics;

namespace Shard
{
    internal class Door : DoomSegment, InteractionHandler, CollisionHandler
    {
        private DoomSoundSystem soundSystem = new DoomSoundSystem();

        public Door(float x, float y, float width, float rotz, string texturePath) : base(x, y, width, rotz, texturePath, false)
        {
            setPhysicsEnabled();
            MyBody.addSegmentCollider(this);
            MyBody.Kinematic = true;
            MyBody.CollisionLayer = (int)DoomTypeGame.CollisionLayer.STATIC;
            addTag("Door");
        }

        private bool isOpen = false;

        public void Interact()
        {
            if (isOpen)
            {
                return;
            }

            isOpen = true;

            Vector2 startPosition = new Vector2(Transform.X, Transform.Y);
            Vector2 dir = Vector2.Normalize(cachedEndPoint - cachedStartPoint);
            Vector2 targetPosition = startPosition + dir * 101;

            soundSystem.playSound("door_open_close.wav");

            Tween.MoveTo(this, targetPosition, 1.5f).setEaseOutBounce().setOnComplete(() =>
            {
                Tween.MoveTo(this, startPosition, 1.5f).setEaseOutBounce().setOnComplete(() =>
                {
                    isOpen = false;
                });
            });
        }

        public void onCollisionEnter(PhysicsBody x)
        {
        }

        public void onCollisionExit(PhysicsBody x)
        {
        }

        public void onCollisionStay(PhysicsBody x)
        {
        }
    }
}