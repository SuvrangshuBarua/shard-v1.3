using Shard.Shard;
using System.Numerics;

namespace Shard
{
    internal class Projectile : AnimatedBillboard, CollisionHandler
    {
        private float speed = 4f;
        private Vector2 dir;
        private bool isDestroyed = false;
        private float damage;
        private SpritesheetAnimation projectileAnimation;

        public Projectile(float x, float y, Vector2 direction, float damage) : base(x, y, 10f, .1f, "plasmaball.png", true)
        {
            this.dir = direction;
            this.damage = damage;
            isDestroyed = false;
            setPhysicsEnabled();
            MyBody.addCircleCollider(0, 0, 10);
            MyBody.StopOnCollision = true;
            MyBody.AngularDrag = 0.5f;
            MyBody.Drag = 0.5f;
            MyBody.MaxForce = speed;
            initializeAnimations();
            playAnimation(projectileAnimation);
            addTag("Projectile");
            MyBody.CollisionLayer = (int)DoomTypeGame.CollisionLayer.PLAYER_PROJECTILE;
            Debug.Log($"projectle spawned at {x} and {y}");
        }

        public override void update()
        {
            base.update();
            moveTowardsDirection();
        }

        private void moveTowardsDirection()
        {
            if (isDestroyed) return;

            MyBody.addForce(Vector2.Normalize(dir) * speed);
        }

        private void initializeAnimations()
        {
            projectileAnimation = new SpritesheetAnimation("projectile", "plasmaball.png", 1.0f, AnimationType.LOOP);
            projectileAnimation.keyFrames = [
                new SpritesheetAnimationKeyframe { offsetX = 1, offsetY = 1, width = 526, height = 525, screenOffsetY = 1f },
                new SpritesheetAnimationKeyframe { offsetX = 529, offsetY = 1, width = 524, height = 525, screenOffsetY = 1f},
                new SpritesheetAnimationKeyframe { offsetX = 1055, offsetY = 1, width = 524, height = 526, screenOffsetY = 1f},
                new SpritesheetAnimationKeyframe { offsetX = 1581, offsetY = 1, width = 526, height = 524, screenOffsetY = 1f},
            ];
        }

        private void Destroy()
        {
            isDestroyed = true;
            MyBody.getColliders().Clear();
            killMe();
        }

        public void onCollisionEnter(PhysicsBody x)
        {
            DamageHandler? d = x.Parent as DamageHandler;
            if (d != null)
            {
                d.takeDamage(damage);
            }
            Destroy();
        }

        public void onCollisionExit(PhysicsBody x)
        {
        }

        public void onCollisionStay(PhysicsBody x)
        {
        }
    }
}