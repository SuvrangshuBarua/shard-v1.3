/*
*
*   The abstract display class setting out the consistent interface all display implementations need.
*   @author Michael Heron
*   @version 1.0
*
*/

using Shard.Shard;
using System.Drawing;
using System.Numerics;

namespace Shard
{
    internal abstract class Display
    {
        protected int _height, _width;

        public virtual (int, int) getTextureDimensions(string texturePath)
        {
            return (0, 0);
        }

        public virtual void drawDoomScene2D(float camx, float camy, Vector2 dir, float fov, DoomSegment[] segments, float renderDistance)
        {
        }

        public virtual void drawDoomScene3D(float camx, float camy, Vector2 dir, DoomGrid staticDoomGrid, DoomSegment[] dynamicDoomSegments, DoomBillboard[] billboards, DoomRenderOptions options)
        {
        }

        public virtual void drawLine(int x, int y, int x2, int y2, int r, int g, int b, int a)
        {
        }

        public virtual void drawLine(int x, int y, int x2, int y2, Color col)
        {
            drawLine(x, y, x2, y2, col.R, col.G, col.B, col.A);
        }

        public virtual void drawCircle(int x, int y, int rad, int r, int g, int b, int a)
        {
        }

        public virtual void drawCircle(int x, int y, int rad, Color col)
        {
            drawCircle(x, y, rad, col.R, col.G, col.B, col.A);
        }

        public virtual void drawFilledCircle(int x, int y, int rad, Color col)
        {
            drawFilledCircle(x, y, rad, col.R, col.G, col.B, col.A);
        }

        public virtual void drawFilledCircle(int x, int y, int rad, int r, int g, int b, int a)
        {
            while (rad > 0)
            {
                drawCircle(x, y, rad, r, g, b, a);
                rad -= 1;
            }
        }

        // Very specific method for drawing a sprite at the bottom center of the screen
        public virtual void drawGun(string texturePath, int width, int height, int offsetX, int offsetY)
        {
        }

        public virtual void drawRect(int x, int y, int width, int height, Color col)
        {
            // Top Line
            drawLine(x, y, x + width, y, col);
            // Bottom Line
            drawLine(x, y + height, x + width, y + height, col);
            // Left Line
            drawLine(x, y, x, y + height, col);
            // Right Line
            drawLine(x + width, y, x + width, y + height, col);
        }

        public virtual void drawFilledRectangle(int x, int y, int width, int height, Color col)
        {
            for (int i = 0; i < height; i++)
            {
                drawLine(x, y + i, x + width, y + i, col);
            }
        }

        public void showText(string text, double x, double y, int size, Color col)
        {
            showText(text, x, y, size, col.R, col.G, col.B);
        }

        public virtual void setFullscreen()
        {
        }

        public virtual void addToDraw(GameObject gob)
        {
        }

        public virtual void removeToDraw(GameObject gob)
        {
        }

        public int getHeight()
        {
            return _height;
        }

        public int getWidth()
        {
            return _width;
        }

        public virtual void setSize(int w, int h)
        {
            _height = h;
            _width = w;
        }

        public abstract void initialize();

        public abstract void clearDisplay();

        public abstract void display();

        public abstract void showText(string text, double x, double y, int size, int r, int g, int b);

        public abstract void showText(char[,] text, double x, double y, int size, int r, int g, int b);
    }
}