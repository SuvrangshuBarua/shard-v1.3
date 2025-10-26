using Shard.Shard;
using System;
using System.Collections;

namespace Shard
{
    internal class WeaponPickup : DoomBillboard, CollisionHandler
    {
        internal Weapon weapon;
        internal string texturePath;
        internal string pickupSoundPath;
        private DoomSoundSystem soundSystem = new DoomSoundSystem();

        public WeaponPickup(Weapon weapon, string texturePath, float x, float y, string pickupSoundPath, float width = 30, float height = 0.3f) : base(x, y, width, height, texturePath, true)
        {
            this.weapon = weapon;
            this.texturePath = texturePath;
            this.pickupSoundPath = pickupSoundPath;
            this.ScreenOffsetY = 0;
            setPhysicsEnabled();
            MyBody.addCircleCollider(0, 0, 5);
            MyBody.Kinematic = false;
            MyBody.CollisionLayer = (int)DoomTypeGame.CollisionLayer.INTERACTABLE_PASSTHROUGH;

            DoomRenderer.getInstance().AddDoomBillboard(this);

            animation = Bootstrap.getRunningGame().StartCoroutine(floatingAnimation());
        }

        private IEnumerator animation;

        private IEnumerator floatingAnimation()
        {
            long startMillis = Bootstrap.getCurrentMillis();
            float offsetY = 0.5f;

            while (true)
            {
                float t = (Bootstrap.getCurrentMillis() - startMillis) / 1000f;

                float s = MathF.Sin(t * 3);

                this.ScreenOffsetY = offsetY + s * 0.2f;

                yield return null;
            }
        }

        void CollisionHandler.onCollisionEnter(PhysicsBody x)
        {
            Player p = x.Parent as Player;
            if (p == null)
            {
                return;
            }
            Bootstrap.getRunningGame().StopCoroutine(animation);
            p.pickupWeapon(weapon);
            ToBeDestroyed = true;

            // Play the shooting sound
            if (!string.IsNullOrEmpty(pickupSoundPath))
            {
                soundSystem.playSound(pickupSoundPath);
            }
        }

        void CollisionHandler.onCollisionExit(PhysicsBody x)
        {
        }

        void CollisionHandler.onCollisionStay(PhysicsBody x)
        {
        }
    }
}