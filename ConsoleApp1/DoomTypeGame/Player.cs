using System.Collections.Generic;
using System.Numerics;

namespace Shard
{
    internal class Player : GameObject, CollisionHandler
    {
        private Weapon? currentWeapon;

        private const float walkSpeed = 3f;
        private const float interactionDistance = 150;

        public Player()
        {
            setPhysicsEnabled();
            MyBody.addCircleCollider(0, 0, 20);
            MyBody.StopOnCollision = true;
            MyBody.AngularDrag = 0.6f; // This value defines how quickly the player stops rotating when no input is given
            MyBody.Drag = 0.6f; // This value defines how quickly the player stops moving when no input is given
            MyBody.MaxForce = walkSpeed * 3f; // This essentially acts as the maximum movement speed
            MyBody.MaxTorque = 2f;
            MyBody.CollisionLayer = (int)DoomTypeGame.CollisionLayer.PLAYER;
            addTag("Player");
        }

        public override void update()
        {
            base.update();
            if (currentWeapon != null)
            {
                currentWeapon.draw();
            }
        }

        public void pickupWeapon(Weapon weapon)
        {
            currentWeapon = weapon;
        }

        public float WalkSpeed
        {
            get => walkSpeed;
            private set { }
        }

        private const float runSpeed = 5f;

        public float RunSpeed
        {
            get => runSpeed;
            private set { }
        }

        private const float rotationSpeed = .7f;

        public float RotationSpeed
        {
            get => rotationSpeed;
            private set { }
        }

        private bool wasShootingLastFrame = false;

        internal void startShooting()
        {
            if (wasShootingLastFrame)
            {
                return;
            }
            wasShootingLastFrame = true;

            if (currentWeapon == null)
                return;

            Vector2 player = new Vector2(Transform.X, Transform.Y);
            currentWeapon.shoot(player, Transform.Forward);
        }

        internal void stopShooting()
        {
            wasShootingLastFrame = false;
        }

        private bool wasInteractingLastFrame = false;

        internal void startInteracting()
        {
            if (wasInteractingLastFrame)
            {
                return;
            }
            wasInteractingLastFrame = true;

            interact();
        }

        internal void stopInteracting()
        {
            wasInteractingLastFrame = false;
        }

        private void interact()
        {
            bool didHit = PhysicsManager.getInstance().rayCast(new Vector2(Transform.X, Transform.Y), Transform.Forward, [(int)DoomTypeGame.CollisionLayer.PLAYER], out List<RayCastHit> hits);

            if (didHit)
            {
                if (hits.Count > 0)
                {
                    // Find the closest hit
                    RayCastHit closestHit = hits[0];
                    for (int i = 1; i < hits.Count; i++)
                    {
                        if (hits[i].distance < closestHit.distance)
                            closestHit = hits[i];
                    }

                    if (closestHit.distance > interactionDistance)
                    {
                        return;
                    }

                    InteractionHandler? ih = closestHit.other.Parent as InteractionHandler;
                    if (ih != null)
                    {
                        ih.Interact();
                    }

                    WeaponPickup? wp = closestHit.other.Parent as WeaponPickup;
                    if (wp != null)
                    {
                        pickupWeapon(wp.weapon);
                    }
                }
            }
        }

        public void onCollisionEnter(PhysicsBody x)
        {
            if (x.Parent.checkTag("Wall"))
            {
                Debug.Log("Player collision with Wall");
            }
        }

        public void onCollisionExit(PhysicsBody x)
        {
            Debug.Log("Player collision exit");
        }

        public void onCollisionStay(PhysicsBody x)
        {
            //Debug.Log("Player collision stay");
        }
    }
}