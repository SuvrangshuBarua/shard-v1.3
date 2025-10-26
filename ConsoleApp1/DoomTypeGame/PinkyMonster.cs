using Shard.Shard;
using System.Numerics;

namespace Shard
{
    internal class PinkyMonster : AnimatedBillboard, CollisionHandler, DamageHandler
    {
        private SpritesheetAnimation walkAnimation;
        private SpritesheetAnimation deathAnimation;
        private GameObject player;
        private float movementSpeed = 2f;
        private float chaseStoppingDistance = 75f;
        private bool isDead = false;
        private DoomSoundSystem walkSound = new DoomSoundSystem();
        private DoomSoundSystem deathSound = new DoomSoundSystem();

        public PinkyMonster(float x, float y, GameObject player) : base(x, y, 70, 0.7f, "monster_pinky_spritesheet.png", true)
        {
            this.player = player;
            setPhysicsEnabled();
            MyBody.addCircleCollider(0, 0, 20);
            MyBody.StopOnCollision = true;
            MyBody.AngularDrag = 1.5f; // This value defines how quickly the player stops rotating when no input is given
            MyBody.Drag = 1.5f; // This value defines how quickly the player stops moving when no input is given
            MyBody.MaxForce = movementSpeed; // This essentially acts as the maximum movement speed
            MyBody.CollisionLayer = (int)DoomTypeGame.CollisionLayer.ENEMY;
            addTag("PinkyMonster");

            initializeAnimations();
            playAnimation(walkAnimation);
        }

        private void initializeAnimations()
        {
            walkAnimation = new SpritesheetAnimation("walk", "pinky_spritesheet.png", 1.0f, AnimationType.LOOP);
            walkAnimation.keyFrames = [
                new SpritesheetAnimationKeyframe { offsetX = 0, offsetY = 0, width = 64, height = 60, screenOffsetY = 0.2f },
                new SpritesheetAnimationKeyframe { offsetX = 0, offsetY = 60, width = 64, height = 60, screenOffsetY = 0.2f},
            ];

            deathAnimation = new SpritesheetAnimation("death", "pinky_spritesheet.png", 0.75f, AnimationType.ONCE);
            deathAnimation.keyFrames = [
                new SpritesheetAnimationKeyframe { offsetX = 64*1, offsetY = 60, width = 64, height = 60, screenOffsetY = 0.2f },
                new SpritesheetAnimationKeyframe { offsetX = 64*2, offsetY = 60, width = 64, height = 60, screenOffsetY = 0.2f, },
                new SpritesheetAnimationKeyframe { offsetX = 64*3, offsetY = 60, width = 64, height = 60, screenOffsetY = 0.2f, },
                new SpritesheetAnimationKeyframe { offsetX = 64*4, offsetY = 60, width = 64, height = 60, screenOffsetY = 0.25f, },
                new SpritesheetAnimationKeyframe { offsetX = 64*5, offsetY = 60, width = 64, height = 60, screenOffsetY = 0.25f, },
                new SpritesheetAnimationKeyframe { offsetX = 64*6, offsetY = 60, width = 64, height = 60, screenOffsetY = 0.25f, },
            ];
        }

        public override void update()
        {
            base.update();

            moveTowardsPlayer();
        }

        private void moveTowardsPlayer()
        {
            if (isDead)
                return;

            if (player == null)
                return;

            Vector2 pl = new Vector2(player.Transform.X, player.Transform.Y);
            Vector2 me = new Vector2(Transform.X, Transform.Y);

            float d = Vector2.Distance(pl, me);

            if (d < chaseStoppingDistance)
            {
                return;
            }

            MyBody.addForce(Vector2.Normalize(new Vector2(player.Transform.X - Transform.X, player.Transform.Y - Transform.Y)) * movementSpeed);
        }

        private void die()
        {
            isDead = true;
            deathSound.playSound("monster_dead.wav");
            playAnimation(deathAnimation);
            MyBody.getColliders().Clear();
        }

        public void takeDamage(float damage)
        {
            die();
        }

        public void onCollisionEnter(PhysicsBody x)
        {
            //if (x.Parent.checkTag("Wall"))
            //{
            //    Debug.Log("Player collision with Wall");
            //}
        }

        public void onCollisionExit(PhysicsBody x)
        {
            //Debug.Log("Player collision exit");
        }

        public void onCollisionStay(PhysicsBody x)
        {
            //Debug.Log("Player collision stay");
        }
    }
}