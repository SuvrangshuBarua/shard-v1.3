using DoomTypeGame;
using Shard.Shard;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace Shard
{
    internal class DoomTypeGame : Game
    {
        private Player player;
        private MovementSystem movementSystem;
        private DoomSoundSystem bgm = new DoomSoundSystem();
        private GunUI gun;

        //mute/unmute
        public bool isMuted = false; // Tracks mute state

        private bool showMuteIndicator = false;
        private DateTime muteIndicatorStartTime;

        private DoomRenderer doomRenderer;

        // Add a Weapon object for the player's gun
        private Weapon playerWeapon;

        private Color[,] heartSprite = {
            { Color.Transparent, Color.Red, Color.Transparent, Color.Red, Color.Transparent },
            { Color.Red, Color.Red, Color.Red, Color.Red, Color.Red },
            { Color.Red, Color.Red, Color.Red, Color.Red, Color.Red },
            { Color.Transparent, Color.Red, Color.Red, Color.Red, Color.Transparent },
            { Color.Transparent, Color.Transparent, Color.Red, Color.Transparent, Color.Transparent }
        };

        // Draw the hearts based on fixed health
        public void DrawHearts(int playerHealth, int scale)
        {
            int heartSize = 5; // Size of each heart sprite (5x5 pixels)
            int spacing = 2 * scale; // Scale the spacing between hearts
            int startY = 10;   // Y position in the top-right corner

            // Calculate the starting X position for the hearts
            int totalHeartsWidth = playerHealth * (heartSize * scale + spacing) - spacing; // Total width of all hearts
            int startX = Bootstrap.getDisplay().getWidth() - totalHeartsWidth - 20; // 10 pixels from the right edge

            for (int i = 0; i < playerHealth; i++)
            {
                int heartX = startX + i * (heartSize * scale + spacing);
                int heartY = startY;

                // Draw the heart sprite
                for (int y = 0; y < heartSize; y++)
                {
                    for (int x = 0; x < heartSize; x++)
                    {
                        Color pixelColor = heartSprite[y, x];
                        if (pixelColor != Color.Transparent)
                        {
                            // Draw the pixel as a scaled rectangle
                            Bootstrap.getDisplay().drawFilledRectangle(
                                heartX + x * scale, // Scaled X position
                                heartY + y * scale, // Scaled Y position
                                scale,              // Scaled width
                                scale,              // Scaled height
                                pixelColor          // Pixel color
                            );
                        }
                    }
                }
            }
        }

        // In the engine, collision layers are just integers, nothing fancy. When making the actual game, the developers can map an enum to ints to make it more readable
        public enum CollisionLayer
        {
            STATIC,
            PLAYER,
            ENEMY,
            STATIC_PROJECTILE_PASSTHROUGH,
            PLAYER_PROJECTILE,
            ENEMY_PROJECTILE,
            INTERACTABLE_PASSTHROUGH,
        }

        public override void update()
        {
            int fps = Bootstrap.getSecondFPS();
            Bootstrap.getDisplay().clearDisplay();
            Bootstrap.getDisplay().showText("FPS: " + fps + " / " + Bootstrap.getFPS(), 21, 21, 24, 255, 255, 255); // Shadow
            Bootstrap.getDisplay().showText("FPS: " + fps + " / " + Bootstrap.getFPS(), 20, 20, 24, 255, 255, 0); // Main text

            //Bootstrap.getDisplay().showText("FPS: " + Bootstrap.getSecondFPS() + " / " + Bootstrap.getFPS(), 10, 10, 12, 255, 255, 255);
            DrawHearts(5, 5);

            //Refactored!

            // WASD to move
            // SPACE to shoot
            // I to interact
            // CTRL + LEFT_SHIFT to rebind last used input binding

            movementSystem.HandlePlayerMovement(player);
        }

        public override int getTargetFrameRate()
        {
            return 100;
        }

        public override void initialize()
        {
            movementSystem = new MovementSystem();
            Bootstrap.getInput().addListener(movementSystem);
            player = new Player();
            player.Transform.X = 0;
            player.Transform.Y = 0;

            bgm.isLooping(true, "game_bgm_2.wav");

            // Lots of DOOM textures can be found here https://www.deviantart.com/hoover1979, these are the ones I used for this demo scene

            List<DoomSegment> staticWalls = new List<DoomSegment>(); // This one is for only static opaque segments and will be baked into a grid
            List<DoomSegment> dynamicDoomSegments = new List<DoomSegment>(); // All movable and transparent static segments must be in this one
            List<DoomBillboard> doomBillboards = new List<DoomBillboard>(); // This one is for billboards that rotate towards the camera

            // Layer 0: static objects
            // Layer 1: player
            // Layer 2: enemies
            // Layer 3: static objects that entities collide with but can be shot through (like the fence)
            // Layer 4: player projectiles (like projectile bullets)
            // Layer 5: enemy projectiles
            // Layer 6: weapon pickups

            PhysicsManager.getInstance().setCollisionBetweenLayers((int)CollisionLayer.STATIC, (int)CollisionLayer.STATIC, false); // Static layer shouldn't collide with itself
            PhysicsManager.getInstance().setCollisionBetweenLayers((int)CollisionLayer.STATIC, (int)CollisionLayer.STATIC_PROJECTILE_PASSTHROUGH, false); // Static objects should not collide with static objects the player can shoot through
            PhysicsManager.getInstance().setCollisionBetweenLayers((int)CollisionLayer.STATIC_PROJECTILE_PASSTHROUGH, (int)CollisionLayer.PLAYER_PROJECTILE, false); // Fence and player projectiles should not collide
            PhysicsManager.getInstance().setCollisionBetweenLayers((int)CollisionLayer.STATIC_PROJECTILE_PASSTHROUGH, (int)CollisionLayer.ENEMY_PROJECTILE, false); // Fence and enemy projectiles should not collide
            PhysicsManager.getInstance().setCollisionBetweenLayers((int)CollisionLayer.PLAYER, (int)CollisionLayer.PLAYER_PROJECTILE, false); // Player and Player projectile should not collide
            PhysicsManager.getInstance().setCollisionBetweenLayers((int)CollisionLayer.PLAYER_PROJECTILE, (int)CollisionLayer.PLAYER_PROJECTILE, false); // Player and Player projectile should not collide
            PhysicsManager.getInstance().setCollisionBetweenLayers((int)CollisionLayer.INTERACTABLE_PASSTHROUGH, (int)CollisionLayer.PLAYER_PROJECTILE, false);
            PhysicsManager.getInstance().setCollisionBetweenLayers((int)CollisionLayer.INTERACTABLE_PASSTHROUGH, (int)CollisionLayer.STATIC, false);
            PhysicsManager.getInstance().setCollisionBetweenLayers((int)CollisionLayer.INTERACTABLE_PASSTHROUGH, (int)CollisionLayer.ENEMY, false);

            // An outline size of 4000 fits nicely on the screen while debugging
            int mapOutlineSize = 4000;

            staticWalls.Add(new Wall(0, -mapOutlineSize / 2, mapOutlineSize, 0, "split_stone_wall_01.jpg", false));
            staticWalls.Add(new Wall(0, mapOutlineSize / 2, mapOutlineSize, 0, "split_stone_wall_01.jpg", false));
            staticWalls.Add(new Wall(-mapOutlineSize / 2, 0, mapOutlineSize, 90, "split_stone_wall_01.jpg", false));
            staticWalls.Add(new Wall(mapOutlineSize / 2, 0, mapOutlineSize, 90, "split_stone_wall_01.jpg", false));

            // ============================================================================= DEMO ROOM ============================================================================= //
            // Hallway
            dynamicDoomSegments.Add(new Door(-110, 0, 100, 90, "doom_ii_wolfenstein_door_01.jpg"));
            staticWalls.Add(new Wall(-100, -55, 10, 90, "bronze_wall_01_remake.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 50));
            staticWalls.Add(new Wall(-105, -50, 10, 0, "bronze_wall_01_remake.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 50));
            staticWalls.Add(new Wall(0, -60, 200, 0, "bronze_wall_01_remake.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 50));
            staticWalls.Add(new Wall(100, -55, 10, -90, "bronze_wall_01_remake.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 50));
            staticWalls.Add(new Wall(-100, 55, 10, 90, "bronze_wall_01_remake.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 50));
            staticWalls.Add(new Wall(-105, 50, 10, 0, "bronze_wall_01_remake.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 50));
            staticWalls.Add(new Wall(0, 60, 200, 0, "bronze_wall_01_remake.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 50));
            staticWalls.Add(new Wall(100, 55, 10, -90, "bronze_wall_01_remake.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 50));
            staticWalls.Add(new Wall(200, -50, 200, 0, "bronze_wall_01_remake.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 50));
            staticWalls.Add(new Wall(200, 50, 200, 0, "bronze_wall_01_remake.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 50));
            staticWalls.Add(new Wall(300, -60, 20, -90, "bronze_wall_01_remake.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 50));
            staticWalls.Add(new Wall(300, 60, 20, -90, "bronze_wall_01_remake.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 50));
            staticWalls.Add(new Wall(295, -70, 10, 0, "bronze_wall_01_remake.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 50));
            staticWalls.Add(new Wall(295, 70, 10, 0, "bronze_wall_01_remake.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 50));
            dynamicDoomSegments.Add(new Door(270, 0, 100, 90, "doom_ii_wolfenstein_door_01.jpg"));

            // Large Room
            staticWalls.Add(new Wall(290, -200, 300, 90, "slimey_bricks_with_rusted_base.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 100));
            staticWalls.Add(new Wall(290, 350, 600, 90, "slimey_bricks_with_rusted_base.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 100));
            staticWalls.Add(new Wall(490, -350, 400, 0, "slimey_bricks_with_rusted_base.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 100));
            staticWalls.Add(new Wall(490, 650, 400, 0, "slimey_bricks_with_rusted_base.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 100));
            staticWalls.Add(new Wall(690, 150, 1000, 90, "slimey_bricks_with_rusted_base.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 100));
            staticWalls.Add(new Wall(700, 150, 1000, 90, "slimey_bricks_with_rusted_base.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 100));

            // Fence
            Wall fence = new Wall(415, 425, 150, 0, "grating_01.png", true, TEXTURE_WRAPPING_SPACE.WORLD, 50);
            fence.MyBody.CollisionLayer = (int)DoomTypeGame.CollisionLayer.STATIC_PROJECTILE_PASSTHROUGH; // An example of how collision layers can be mapped from the CollisionLayer enum
            dynamicDoomSegments.Add(fence);
            staticWalls.Add(new Wall(315, 400, 50, 0, "grey_wall_01.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 50));
            staticWalls.Add(new Wall(315, 450, 50, 0, "grey_wall_01.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 50));
            staticWalls.Add(new Wall(340, 425, 50, 90, "grey_wall_01.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 50));
            staticWalls.Add(new Wall(515, 400, 50, 0, "grey_wall_01.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 50));
            staticWalls.Add(new Wall(515, 450, 50, 0, "grey_wall_01.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 50));
            staticWalls.Add(new Wall(540, 425, 50, 90, "grey_wall_01.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 50));
            staticWalls.Add(new Wall(490, 425, 50, 90, "grey_wall_01.jpg", false, TEXTURE_WRAPPING_SPACE.WORLD, 50));

            // Table
            doomBillboards.Add(new DoomBillboard(550, -50, 30, 0.35f, 0.9f, "small_round_table.png", true));

            // Enemy
            doomBillboards.Add(new PinkyMonster(400, 550, player));
            doomBillboards.Add(new PinkyMonster(500, 500, player));
            doomBillboards.Add(new PinkyMonster(450, 500, player));
            doomBillboards.Add(new PinkyMonster(500, 50, player));
            doomBillboards.Add(new PinkyMonster(500, -50, player));

            Vector2 p1World = new Vector2(100, 100);
            Vector2 p2World = new Vector2(100, 200);
            Vector2 p3World = new Vector2(200, 200);
            Vector2 p4World = new Vector2(200, 100);

            // Stress testing static segments
            for (int i = 0; i < 10000; i++)
            {
                //staticWalls.Add(new DoomSegment(-1500, -mapOutlineSize / 2 + i * 20, 100, 45, "wall.jpg", false));
            }

            // Stress testing billboards segments
            for (int i = 0; i < 1000; i++)
            {
                //doomBillboards.Add(new DoomBillboard(-1000, -mapOutlineSize / 2 + i * 100, 50, 1.0f, "invader1.png", true));
            }

            DoomRenderer.getInstance().ConstructStaticGrid(staticWalls.ToArray());
            DoomRenderer.getInstance().SetDynamicSegments(dynamicDoomSegments.ToArray());
            DoomRenderer.getInstance().SetDoomBillboards(doomBillboards.ToArray());
            DoomRenderer.getInstance().options.floorTexturePath = "cobble_floor.jpg";
            DoomRenderer.getInstance().options.ceilingTexturePath = "metal_ceiling_01.jpg";
            DoomRenderer.getInstance().SetCamera(player);

            new WeaponPickup(new Weapon(WEAPON_TYPE.PROJECTILE, "Projectile Launcher", 10, "projectile_boom.mp3", "DoomGun.png", -20, 0), "ShotgunPickup.png", 500, -100, "projectile_gun_pickup.mp3", 50, 0.3f);
            new WeaponPickup(new Weapon(WEAPON_TYPE.RAYCAST, "Handgun", 10, "shoot.mp3", "Handgun.png", 80, 30), "HandgunPickup.png", 200, 0, "handgun_pickup.mp3");
        }
    }
}