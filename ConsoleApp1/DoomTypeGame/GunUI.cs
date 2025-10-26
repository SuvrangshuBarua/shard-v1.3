using System;
using System.Drawing;

namespace Shard
{
    internal class GunUI : GameObject
    {
        private Color[,] gunPixelArt; // Pixel art for the gun
        private DateTime muzzleFlashEndTime; // Timer for muzzle flash
        private bool isMuzzleFlashVisible; // Tracks if the muzzle flash is visible
        private string texturePath;
        private float offsetX, offsetY; // In pixels

        public GunUI(string texturePath, int offsetX, int offsetY)
        {
            this.texturePath = texturePath;
            this.offsetX = offsetX;
            this.offsetY = offsetY;
            gunPixelArt = new Color[,]
              {
                { Color.Transparent, Color.Transparent, Color.Transparent, Color.Black, Color.Black, Color.Black, Color.Black, Color.Black, Color.Transparent, Color.Transparent, Color.Transparent },
                { Color.Transparent, Color.Transparent, Color.Black, Color.Gray, Color.LightGray, Color.Gray, Color.LightGray, Color.Gray, Color.Black, Color.Transparent, Color.Transparent },
                { Color.Transparent, Color.Black, Color.Gray, Color.LightGray, Color.Gray, Color.LightGray, Color.Gray, Color.LightGray, Color.Gray, Color.Black, Color.Transparent },
                { Color.Transparent, Color.Black, Color.Gray, Color.Gray, Color.Gray, Color.Gray, Color.Gray, Color.Gray, Color.Gray, Color.Black, Color.Transparent },
                { Color.Transparent, Color.Black, Color.Gray, Color.Gray, Color.Gray, Color.Gray, Color.Gray, Color.Gray, Color.Gray, Color.Black, Color.Transparent },
                { Color.Transparent, Color.Transparent, Color.Black, Color.Black, Color.Black, Color.Black, Color.Black, Color.Black, Color.Black, Color.Transparent, Color.Transparent },
                { Color.Transparent, Color.Transparent, Color.Transparent, Color.SaddleBrown, Color.DarkGoldenrod, Color.SaddleBrown, Color.DarkGoldenrod, Color.SaddleBrown, Color.Transparent, Color.Transparent, Color.Transparent },
                { Color.Transparent, Color.Transparent, Color.Transparent, Color.SaddleBrown, Color.DarkGoldenrod, Color.SaddleBrown, Color.DarkGoldenrod, Color.SaddleBrown, Color.Transparent, Color.Transparent, Color.Transparent },
                { Color.Transparent, Color.Transparent, Color.Transparent, Color.SaddleBrown, Color.DarkGoldenrod, Color.SaddleBrown, Color.DarkGoldenrod, Color.SaddleBrown, Color.Transparent, Color.Transparent, Color.Transparent },
                { Color.Transparent, Color.Transparent, Color.Transparent, Color.Black, Color.Transparent, Color.Gray, Color.Gray, Color.Gray, Color.Transparent, Color.Transparent, Color.Transparent },
                { Color.Transparent, Color.Transparent, Color.Transparent, Color.Gray, Color.Gray, Color.Gray, Color.Gray, Color.Gray, Color.Transparent, Color.Transparent, Color.Transparent },
                { Color.Transparent, Color.Transparent, Color.Transparent, Color.Gray, Color.Gray, Color.Gray, Color.Gray, Color.Gray, Color.Transparent, Color.Transparent, Color.Transparent },
                { Color.Transparent, Color.Transparent, Color.Transparent, Color.Gray, Color.Gray, Color.Gray, Color.Gray, Color.Gray, Color.Transparent, Color.Transparent, Color.Transparent },
                { Color.Transparent, Color.Transparent, Color.Transparent, Color.Black, Color.Transparent, Color.Gray, Color.Gray, Color.Gray, Color.Transparent, Color.Transparent, Color.Transparent },
               { Color.Transparent, Color.Transparent, Color.Transparent, Color.Gray, Color.Gray, Color.Gray, Color.Gray, Color.Gray, Color.Transparent, Color.Transparent, Color.Transparent },
               { Color.Transparent, Color.Transparent, Color.Transparent, Color.Gray, Color.Gray, Color.Gray, Color.Gray, Color.Gray, Color.Transparent, Color.Transparent, Color.Transparent },
               { Color.Transparent, Color.Transparent, Color.Transparent, Color.Gray, Color.Gray, Color.Gray, Color.Gray, Color.Gray, Color.Transparent, Color.Transparent, Color.Transparent },
              };

            isMuzzleFlashVisible = false;

            this.Transform.SpritePath = Bootstrap.getAssetManager().getAssetPath(texturePath);
            (int w, int h) = Bootstrap.getDisplay().getTextureDimensions(texturePath);
            this.Transform.Wid = w;
            this.Transform.Ht = h;
            this.Transform.X = Bootstrap.getDisplay().getWidth() / 2 - w / 2 + offsetX;
            this.Transform.Y = Bootstrap.getDisplay().getHeight() - h + offsetY;
        }

        public void DrawGun()
        {
            Bootstrap.getDisplay().addToDraw(this);
            return;

            // Get screen dimensions
            int screenWidth = Bootstrap.getDisplay().getWidth();
            int screenHeight = Bootstrap.getDisplay().getHeight();

            // Calculate the horizontal center position for the gun
            int gunX = screenWidth / 2;

            // Move the gun to the lower part of the screen (e.g., 80% from the top)
            int gunY = screenHeight - 100; // Adjusted to move the gun closer to the bottom

            // Define the size of each "pixel"
            int pixelSize = 10; // Size of each pixel (rectangle)

            // Loop through the gunPixelArt array
            for (int i = 0; i < gunPixelArt.GetLength(0); i++) // Rows
            {
                for (int j = 0; j < gunPixelArt.GetLength(1); j++) // Columns
                {
                    // Get the color of the current pixel
                    Color pixelColor = gunPixelArt[i, j];

                    // Only draw non-transparent pixels
                    if (pixelColor != Color.Transparent)
                    {
                        // Calculate the position of the pixel
                        int pixelX = gunX - (gunPixelArt.GetLength(1) * pixelSize) / 2 + j * pixelSize;
                        int pixelY = gunY - (gunPixelArt.GetLength(0) * pixelSize) / 2 + i * pixelSize;

                        // Draw the pixel as a filled rectangle
                        Bootstrap.getDisplay().drawFilledRectangle(pixelX, pixelY, pixelSize, pixelSize, pixelColor);
                    }
                }
            }
        }

        // Activate the muzzle flash
        public void ActivateMuzzleFlash()
        {
            isMuzzleFlashVisible = true;
            Console.WriteLine("Muzzle flash activated");
            muzzleFlashEndTime = DateTime.Now.AddMilliseconds(100); // 100ms duration
        }

        public void DrawMuzzleFlash()
        {
            if (isMuzzleFlashVisible)
            {
                if (DateTime.Now < muzzleFlashEndTime)
                {
                    // Get screen dimensions
                    int screenWidth = Bootstrap.getDisplay().getWidth();
                    int screenHeight = Bootstrap.getDisplay().getHeight();

                    // Calculate the position of the muzzle flash
                    int flashX = screenWidth / 2 + 50; // Adjust position to match the gun's barrel
                    int flashY = screenHeight - 100;   // Position just above the bottom of the screen

                    // Calculate flash size based on time elapsed (expand effect)
                    double elapsed = (DateTime.Now - muzzleFlashEndTime.AddMilliseconds(-100)).TotalMilliseconds;
                    int flashSize = Math.Min(10 + (int)(elapsed / 5), 20); // Flash size grows over time, max size 20

                    // Create a color gradient for the flash effect (yellow to orange)
                    Color flashColor = Color.FromArgb(
                        (int)(255 - Math.Min(elapsed / 3, 255)), // Fade the flash over time
                        255,
                        Math.Min(255 - (int)(elapsed / 5), 255),
                        0);

                    // Draw the flash as an expanding rectangle
                    Bootstrap.getDisplay().drawFilledRectangle(flashX, flashY, flashSize, flashSize, flashColor);
                }
                else
                {
                    // Hide the muzzle flash after the duration
                    isMuzzleFlashVisible = false;
                }
            }
        }
    }
}