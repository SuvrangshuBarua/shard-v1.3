using Shard.Shard;
using System.Collections.Generic;
using System.Numerics;

namespace Shard
{
    public enum WEAPON_TYPE
    {
        RAYCAST,
        PROJECTILE
    }

    internal class Weapon
    {
        private WEAPON_TYPE type;
        private string name;
        private float damage;
        private string texturePath;
        private DoomTypeGame game;
        private bool beep = false;

        private GunUI gunUI;

        //soundeffect
        private DoomSoundSystem soundSystem = new DoomSoundSystem();

        private string soundEffectPath;

        public Weapon(WEAPON_TYPE type, string name, float damage, string soundEffectPath, string texturePath, int offsetX = 0, int offsetY = 0)
        {
            this.type = type;
            this.name = name;
            this.damage = damage;
            this.soundEffectPath = soundEffectPath;
            this.texturePath = texturePath;
            gunUI = new GunUI(texturePath, offsetX, offsetY);
        }

        public void draw()
        {
            if (gunUI == null)
            {
                return;
            }
            gunUI.DrawGun();
        }

        // Returns true if the weapon was fired (implement cooldown in future?)
        public bool shoot(Vector2 origin, Vector2 direction)
        {
            switch (type)
            {
                case WEAPON_TYPE.RAYCAST:
                    // Ignores layers 1 and 3, i.e. player and fence
                    bool didHit = PhysicsManager.getInstance().rayCast(origin, direction, [(int)DoomTypeGame.CollisionLayer.PLAYER, (int)DoomTypeGame.CollisionLayer.STATIC_PROJECTILE_PASSTHROUGH, (int)DoomTypeGame.CollisionLayer.INTERACTABLE_PASSTHROUGH], out List<RayCastHit> hits);

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

                            DamageHandler? d = closestHit.other.Parent as DamageHandler;
                            if (d != null)
                            {
                                d.takeDamage(damage);
                            }
                        }
                    }
                    break;

                case WEAPON_TYPE.PROJECTILE:
                    Vector2 projectileSpawnpoint = origin + direction * 50;
                    var pr = new Projectile(projectileSpawnpoint.X, projectileSpawnpoint.Y, direction, this.damage);
                    DoomRenderer.getInstance().AddDoomBillboard(pr);
                    break;
            }

            // Play the shooting sound
            if (!string.IsNullOrEmpty(soundEffectPath))
            {
                soundSystem.playSound(soundEffectPath);
            }
            // Activate the muzzle flash
            gunUI.ActivateMuzzleFlash();
            gunUI.DrawMuzzleFlash();
            return true;
        }
    }
}