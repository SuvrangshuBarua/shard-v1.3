/*
*
*   This is the implementation of the Simple Directmedia Layer through C#.   This isn't a course on
*       graphics, so we're not going to roll our own implementation.   If you wanted to replace it with
*       something using OpenGL, that'd be a pretty good extension to the base Shard engine.
*
*   Note that it extends from DisplayText, which also uses SDL.
*
*   @author Michael Heron
*   @version 1.0
*
*/

using SDL2;
using Shard.Shard;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using static SDL2.SDL;

namespace Shard
{
    internal class Line
    {
        private int sx, sy;
        private int ex, ey;
        private int r, g, b, a;

        public int Sx { get => sx; set => sx = value; }
        public int Sy { get => sy; set => sy = value; }
        public int Ex { get => ex; set => ex = value; }
        public int Ey { get => ey; set => ey = value; }
        public int R { get => r; set => r = value; }
        public int G { get => g; set => g = value; }
        public int B { get => b; set => b = value; }
        public int A { get => a; set => a = value; }
    }

    internal class Circle
    {
        private int x, y, rad;
        private int r, g, b, a;

        public int X { get => x; set => x = value; }
        public int Y { get => y; set => y = value; }
        public int Radius { get => rad; set => rad = value; }
        public int R { get => r; set => r = value; }
        public int G { get => g; set => g = value; }
        public int B { get => b; set => b = value; }
        public int A { get => a; set => a = value; }
    }

    internal class DisplaySDL : DisplayText
    {
        private List<Transform> _toDraw;
        private List<Line> _linesToDraw;
        private List<Circle> _circlesToDraw;
        private Dictionary<string, IntPtr> spriteBuffer;

        public override void initialize()
        {
            spriteBuffer = new Dictionary<string, IntPtr>();

            base.initialize();

            _toDraw = new List<Transform>();
            _linesToDraw = new List<Line>();
            _circlesToDraw = new List<Circle>();
        }

        public IntPtr loadTexture(Transform trans)
        {
            IntPtr ret;
            uint format;
            int access;
            int w;
            int h;

            ret = loadTexture(trans.SpritePath);

            SDL.SDL_QueryTexture(ret, out format, out access, out w, out h);
            trans.Ht = h;
            trans.Wid = w;
            trans.recalculateCentre();

            return ret;
        }

        public IntPtr loadTexture(string path)
        {
            IntPtr img;

            if (spriteBuffer.ContainsKey(path))
            {
                return spriteBuffer[path];
            }

            img = SDL_image.IMG_Load(path);

            Debug.getInstance().log("IMG_Load: " + SDL_image.IMG_GetError());

            spriteBuffer[path] = SDL.SDL_CreateTextureFromSurface(_rend, img);

            SDL.SDL_SetTextureBlendMode(spriteBuffer[path], SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);

            return spriteBuffer[path];
        }

        public override (int, int) getTextureDimensions(string texturePath)
        {
            IntPtr sprite = loadTexture(Bootstrap.getAssetManager().getAssetPath(texturePath));
            uint format;
            int access;
            int w;
            int h;

            SDL.SDL_QueryTexture(sprite, out format, out access, out w, out h);

            return (w, h);
        }

        public override void addToDraw(GameObject gob)
        {
            _toDraw.Add(gob.Transform);

            if (gob.Transform.SpritePath == null)
            {
                return;
            }

            loadTexture(gob.Transform.SpritePath);
        }

        public override void removeToDraw(GameObject gob)
        {
            _toDraw.Remove(gob.Transform);
        }

        private void renderCircle(int centreX, int centreY, int rad)
        {
            int dia = (rad * 2);
            byte r, g, b, a;
            int x = (rad - 1);
            int y = 0;
            int tx = 1;
            int ty = 1;
            int error = (tx - dia);

            SDL.SDL_GetRenderDrawColor(_rend, out r, out g, out b, out a);

            // We draw an octagon around the point, and then turn it a bit.  Do
            // that until we have an outline circle.  If you want a filled one,
            // do the same thing with an ever decreasing radius.
            while (x >= y)
            {
                SDL.SDL_RenderDrawPoint(_rend, centreX + x, centreY - y);
                SDL.SDL_RenderDrawPoint(_rend, centreX + x, centreY + y);
                SDL.SDL_RenderDrawPoint(_rend, centreX - x, centreY - y);
                SDL.SDL_RenderDrawPoint(_rend, centreX - x, centreY + y);
                SDL.SDL_RenderDrawPoint(_rend, centreX + y, centreY - x);
                SDL.SDL_RenderDrawPoint(_rend, centreX + y, centreY + x);
                SDL.SDL_RenderDrawPoint(_rend, centreX - y, centreY - x);
                SDL.SDL_RenderDrawPoint(_rend, centreX - y, centreY + x);

                if (error <= 0)
                {
                    y += 1;
                    error += ty;
                    ty += 2;
                }

                if (error > 0)
                {
                    x -= 1;
                    tx += 2;
                    error += (tx - dia);
                }
            }
        }

        public override void drawCircle(int x, int y, int rad, int r, int g, int b, int a)
        {
            Circle c = new Circle();

            c.X = x;
            c.Y = y;
            c.Radius = rad;

            c.R = r;
            c.G = g;
            c.B = b;
            c.A = a;

            _circlesToDraw.Add(c);
        }

        public override void drawLine(int x, int y, int x2, int y2, int r, int g, int b, int a)
        {
            Line l = new Line();
            l.Sx = x;
            l.Sy = y;
            l.Ex = x2;
            l.Ey = y2;

            l.R = r;
            l.G = g;
            l.B = b;
            l.A = a;

            _linesToDraw.Add(l);
        }

        private struct TransparentHitResult
        {
            public TransparentHitResult(DoomSegment segment, float distance, float u)
            {
                this.segment = segment;
                this.distance = distance;
                this.u = u;
            }

            public DoomSegment segment;
            public float distance;
            public float u;
        }

        // Define a struct to hold the computed intersection data for a single column
        private struct ColumnIntersection
        {
            public bool didIntersectWithOpaque;
            public DoomSegment closestOpaqueSegment;
            public float closestDistanceToOpaqueSegment;
            public float opaqueIntersectionU;
            public float angle;
            public TransparentHitResult[] transparentResults; // Fixed-size or list, up to maxTransparentSegments
            public int numTransparentResults;
        }

        private struct TransparentIntersection
        {
            public DoomSegment segment;
            public float distance;
            public float intersectionU;
        }

        public override void drawDoomScene3D(float camx, float camy, Vector2 dir, DoomGrid staticGrid, DoomSegment[] dynamicDoomSegments, DoomBillboard[] billboards, DoomRenderOptions options)
        {
            SDL_SetRenderDrawColor(_rend, 50, 50, 50, 255);
            SDL.SDL_Rect topHalf = new SDL.SDL_Rect { x = 0, y = 0, w = _width, h = _height / 2 };
            SDL_RenderFillRect(_rend, ref topHalf);

            SDL_SetRenderDrawColor(_rend, 30, 35, 30, 255);
            SDL.SDL_Rect bottomHalf = new SDL.SDL_Rect { x = 0, y = _height / 2, w = _width, h = _height / 2 };
            SDL_RenderFillRect(_rend, ref bottomHalf);

            // View setup
            Vector2 cam = new Vector2(camx, camy);
            Vector2 normalizedDir = Vector2.Normalize(dir);
            float fovInRadians = options.fov * MathF.PI / 180;
            float halfFov = fovInRadians * 0.5f;

            // Uses the formula cos(x) = adj/hyp where x is halfFov and the known hypotenuse
            // Not sure why 1000.0f works, this should be investigated further. Also, the resulting view angle does not correspond with the FOV exactly...
            // This is evident if you show both the lines of each ray and the frustum lines, the latter showing the correct angle as specified
            float nearPlaneDistance = MathF.Cos(halfFov) * 1000f;

            int numOpaqueSegments = 0;
            int numTransparentSegments = 0; // This is the number of transparent, non-billboard segments
            for (int i = 0; i < dynamicDoomSegments.Length; i++)
            {
                if (dynamicDoomSegments[i].IsTransparent)
                {
                    numTransparentSegments++;
                }
                else
                {
                    numOpaqueSegments++;
                }
            }

            // Rotate all billboards to face camera
            float billboardAngleRad = MathF.Atan2(normalizedDir.X, normalizedDir.Y);
            float billboardAngleDeg = billboardAngleRad * 57.295779513f;
            float ca = MathF.Cos(billboardAngleRad);
            float sa = MathF.Sin(billboardAngleRad);
            for (int i = 0; i < billboards.Length; i++)
            {
                billboards[i].SetBillboardFacing(billboardAngleRad, billboardAngleDeg, ca, sa);
            }

            // Allocate arrays for opaque and transparent, non-billboard segments
            DoomSegment[] opaqueSegments = new DoomSegment[numOpaqueSegments];
            DoomSegment[] transparentSegments = new DoomSegment[numTransparentSegments];
            int currentOpaqueIndex = 0;
            int currentTransparentIndex = 0;
            for (int i = 0; i < dynamicDoomSegments.Length; i++)
            {
                if (dynamicDoomSegments[i].IsTransparent)
                {
                    transparentSegments[currentTransparentIndex++] = dynamicDoomSegments[i];
                }
                else
                {
                    opaqueSegments[currentOpaqueIndex++] = dynamicDoomSegments[i];
                }
            }

            // Insight: if billboards always face the player, it's enough to sort them by center point, transparent non-billboard segments need to be depth sorted per-pixel-column to support intersection though
            Array.Sort(billboards, (a, b) => (int)(Vector2.Distance(cam, a.getMidpoint()) - Vector2.Distance(cam, b.getMidpoint())));

            // Allocate an array to hold one result per pixel column
            ColumnIntersection[] intersections = new ColumnIntersection[_width];

            // We want to keep track of which grid cells we have visited in the static grid so that we can render floors and ceilings there
            ConcurrentDictionary<(int, int), bool> visitedGridCells = new();

            // Use Parallel.For to compute the intersection data for each column concurrently.
            // (Make sure that each iteration only writes to intersections[i] so that there is no race condition.)
            Parallel.For(0, _width, i =>
            {
                // Compute the current ray angle and direction for column i.
                float angle = MathF.Atan((_width / 2 - i) / nearPlaneDistance);
                Matrix3x2 rotationMatrix = new Matrix3x2(MathF.Cos(angle), -MathF.Sin(angle), MathF.Sin(angle), MathF.Cos(angle), 0, 0);
                Vector2 rayDirection = Vector2.Normalize(Vector2.Transform(normalizedDir, rotationMatrix));

                ColumnIntersection ci = new ColumnIntersection();
                ci.didIntersectWithOpaque = false;
                ci.angle = angle;
                ci.transparentResults = [];
                ci.numTransparentResults = 0;

                // Create the start and end points for the line along which we'll traverse the grid
                float x1 = camx * 0.01f;
                float y1 = camy * 0.01f;
                float x2 = (camx + (rayDirection * options.renderDistance).X) * 0.01f;
                float y2 = (camy + (rayDirection * options.renderDistance).Y) * 0.01f;

                // Convert to grid coordinates
                int x = (int)Math.Floor(x1);
                int y = (int)Math.Floor(y1);
                int xEnd = (int)Math.Floor(x2);
                int yEnd = (int)Math.Floor(y2);

                float dx = x2 - x1;
                float dy = y2 - y1;

                int sx = (dx > 0) ? 1 : (dx < 0) ? -1 : 0;
                int sy = (dy > 0) ? 1 : (dy < 0) ? -1 : 0;

                // Avoid division by zero by handling exact vertical/horizontal cases
                float tDeltaX = (sx != 0) ? Math.Abs(1.0f / dx) : float.MaxValue;
                float tDeltaY = (sy != 0) ? Math.Abs(1.0f / dy) : float.MaxValue;

                // Compute initial tMax values based on fractional positions
                float tMaxX = (sx > 0) ? ((x + 1 - x1) * tDeltaX) : ((x1 - x) * tDeltaX);
                float tMaxY = (sy > 0) ? ((y + 1 - y1) * tDeltaY) : ((y1 - y) * tDeltaY);

                // Initialize default values
                bool didIntersectWithOpaque = false;
                float closestOpaqueDistance = options.renderDistance;
                DoomSegment closestSegment = null;
                float intersectionUCoordinate = 0f;

                // Process static doom segments by traversing the grid using the Amanatides and Woo algorithm
                while (x != xEnd || y != yEnd)
                {
                    DoomSegment[]? segmentsInGridCell = staticGrid.GetCellAt(x, y, out bool isOutOfBounds);

                    if (isOutOfBounds)
                    {
                        break;
                    }

                    visitedGridCells.TryAdd((x, y), true);

                    if (segmentsInGridCell != null)
                    {
                        // Process opaque segments: loop over opaqueSegments to find the closest hit.
                        for (int j = 0; j < segmentsInGridCell.Length; j++)
                        {
                            if (segmentsInGridCell[j].getIntersectionWithRay(cam, rayDirection, out Vector2 intersection, out float u))
                            {
                                // Discard this intersection if it isn't inside of the grid cell (with some padding to help with floating point precision)
                                if (intersection.X > (x + 1.0001f) * 100 || intersection.X < (x - 0.0001f) * 100 ||
                                    intersection.Y > (y + 1.0001f) * 100 || intersection.Y < (y - 0.0001f) * 100)
                                {
                                    continue;
                                }

                                float d = Vector2.Distance(intersection, cam);
                                if (d < closestOpaqueDistance)
                                {
                                    didIntersectWithOpaque = true;
                                    closestOpaqueDistance = d;
                                    closestSegment = segmentsInGridCell[j];
                                    intersectionUCoordinate = u;
                                }
                            }
                        }

                        if (didIntersectWithOpaque)
                        {
                            break;
                        }
                    }

                    if (tMaxX < tMaxY)
                    {
                        tMaxX += tDeltaX;
                        x += sx;
                    }
                    else
                    {
                        tMaxY += tDeltaY;
                        y += sy;
                    }
                }

                // Process dynamic doom segments
                for (int j = 0; j < opaqueSegments.Length; j++)
                {
                    if (opaqueSegments[j].getIntersectionWithRay(cam, rayDirection, out Vector2 intersection, out float u))
                    {
                        float d = Vector2.Distance(intersection, cam);
                        if (d < closestOpaqueDistance)
                        {
                            didIntersectWithOpaque = true;
                            closestOpaqueDistance = d;
                            closestSegment = opaqueSegments[j];
                            intersectionUCoordinate = u;
                        }
                    }
                }

                if (didIntersectWithOpaque)
                {
                    ci.didIntersectWithOpaque = true;
                    ci.closestOpaqueSegment = closestSegment;
                    ci.closestDistanceToOpaqueSegment = closestOpaqueDistance;
                    ci.opaqueIntersectionU = intersectionUCoordinate;
                }

                TransparentHitResult[] transparentResults = new TransparentHitResult[options.maxTransparentSegmentDepth];

                // Process billboards, up to a maximum of options.maxTransparentSegmentDepth
                // As the billboards can be pre-sorted, this is incredibly efficient, we can simply stop when we reach the maximum number of transparent segments
                int numTransparent = 0;
                for (int j = 0; j < billboards.Length; j++)
                {
                    DoomSegment s = billboards[j];
                    if (s.getIntersectionWithRay(cam, rayDirection, out Vector2 intersection, out float u))
                    {
                        float d = Vector2.Distance(intersection, cam);

                        if (d < closestOpaqueDistance)
                        {
                            transparentResults[numTransparent++] = new TransparentHitResult(s, d, u);
                            if (numTransparent == options.maxTransparentSegmentDepth)
                                break;
                        }
                    }
                }

                // Process transparent segments that aren't billboards
                // These are much trickier as they cannot be pre-sorted, and they must be inserted into the transparentSegment array at their correct location
                // NOTE: this makes such segments the most inefficient ones to render, by FAR
                for (int j = 0; j < transparentSegments.Length; j++)
                {
                    DoomSegment s = transparentSegments[j];

                    if (s.getIntersectionWithRay(cam, rayDirection, out Vector2 intersection, out float u))
                    {
                        float d = Vector2.Distance(intersection, cam);

                        if (d < closestOpaqueDistance)
                        {
                            // If there were no billboards in this pixel column, numTransparent is going to be 0 -> initialize the first transparent hit
                            if (numTransparent == 0)
                            {
                                transparentResults[0] = new TransparentHitResult(s, d, u);
                                numTransparent++;
                                continue;
                            }

                            // Else, we must iterate over all transparent segments, back to front, and insert our new segment in its right position in the array
                            bool hasAddedNewSegment = false;
                            for (int k = numTransparent - 1; k >= 0; k--)
                            {
                                // If the new transparent segment hit is closer than the currently checked one (transparentResults[k]), shift the current one step back and place the new one here
                                if (d < transparentResults[k].distance)
                                {
                                    if (k < options.maxTransparentSegmentDepth - 1)
                                    {
                                        transparentResults[k + 1] = transparentResults[k];
                                        hasAddedNewSegment = true;
                                    }
                                    transparentResults[k] = new TransparentHitResult(s, d, u);
                                    // We must then continue and check against the next one in the list
                                }
                                // If the new transparent segment is further away from the currently checked one, place the new segment at the end of the list and break
                                else
                                {
                                    if (k < options.maxTransparentSegmentDepth - 1)
                                    {
                                        transparentResults[k + 1] = new TransparentHitResult(s, d, u);
                                        hasAddedNewSegment = true;
                                    }
                                    // Since transparentResults is sorted, there is no need to continue traversal, all subsequent segment hits are closer than the new one
                                    break;
                                }
                            }
                            if (hasAddedNewSegment)
                                numTransparent = Math.Min(numTransparent + 1, options.maxTransparentSegmentDepth);
                        }
                    }
                }

                ci.transparentResults = transparentResults.ToArray();
                ci.numTransparentResults = numTransparent;

                intersections[i] = ci;
            });

            // Draw ceiling and floor for each visited grid cell, the result is "baked in" view frustum culling and occlusion culling!
            foreach (var cell in visitedGridCells)
            {
                int x = cell.Key.Item1;
                int y = cell.Key.Item2;

                drawHorizontalPlanesAt(x, y, cam, normalizedDir, nearPlaneDistance, options);
            }

            // Now that all the heavy intersection work is done in parallel,
            // draw the pixel columns sequentially (since SDL drawing must run on one thread).
            for (int i = 0; i < _width; i++)
            {
                // Draw the closest opaque segment for each pixel column
                var col = intersections[i];
                if (col.didIntersectWithOpaque)
                {
                    drawPixelColumn(i, col.closestOpaqueSegment, col.closestDistanceToOpaqueSegment, col.angle, col.opaqueIntersectionU, options);
                }
                // Draw transparent segments in reverse order (back-to-front)
                for (int j = col.numTransparentResults - 1; j >= 0; j--)
                {
                    var t = col.transparentResults[j];
                    drawPixelColumn(i, t.segment, t.distance, col.angle, t.u, options);
                }
            }
        }

        private void drawPixelColumn(int x, DoomSegment segment, float distanceToIntersection, float angleToCameraForward, float intersectionFraction, DoomRenderOptions options)
        {
            // Interpolating distance in 1/z space for accurate pixel column height, avoids fish-eye distortion through * cos(angle)
            // Floating point division precision issues (fuzzy lines) are mitigated by multiplying by 10000 and then dividing the resulting int by 10000
            int lineHeight = (int)(segment.Height * MathF.Ceiling(_height * 10000 / MathF.Max(distanceToIntersection * MathF.Cos(angleToCameraForward) / options.worldUnitScale, 0.001f)) / 10000);
            int Sy = _height / 2 - lineHeight / 2 + (int)(segment.ScreenOffsetY * lineHeight);
            int Ey = _height / 2 + lineHeight / 2 + (int)(segment.ScreenOffsetY * lineHeight);

            // Simple 50% ambient, 50% distance shading for fun, change in future, perhaps use light sources?
            float maxLightDistance = 400f;
            float shading = (MathF.Max(1, (maxLightDistance - distanceToIntersection) / maxLightDistance) * 0.5f + 0.5f) * 255;

            // Suggestion: check if the new intersected object is the same as the last one, if so, don't load a new texture but reuse the old one
            IntPtr sprite = loadTexture(Bootstrap.getAssetManager().getAssetPath(segment.TexturePath));
            uint format;
            int access;
            int w;
            int h;

            SDL.SDL_Rect sRect;
            SDL.SDL_Rect tRect;
            SDL.SDL_QueryTexture(sprite, out format, out access, out w, out h);

            (float u, float yStart, float yHeight) = segment.getTextureCoordinate(intersectionFraction, w, h);

            sRect.x = (int)(u * w);
            sRect.y = (int)(yStart * h);
            sRect.w = 1;
            sRect.h = (int)(yHeight * h);

            tRect.x = x;
            tRect.y = Sy;
            tRect.w = 1;
            tRect.h = lineHeight;

            SDL.SDL_SetTextureColorMod(sprite, (byte)shading, (byte)shading, (byte)shading);
            SDL.SDL_RenderCopy(_rend, sprite, ref sRect, ref tRect);
            // Uncomment this for a glitchy (WARNING: flickering!) debug view with colors
            //drawLine(i, Sy, i, Ey, closestSegment.R, closestSegment.G, closestSegment.B, 127);
        }

        // This "UVPlane" is a floor or a ceiling
        private struct UVPlane
        {
            public UVPlane(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Vector2 startUV, Vector2 endUV)
            {
                this.a = a;
                this.b = b;
                this.c = c;
                this.d = d;
                this.startUV = startUV;
                this.endUV = endUV;
            }

            public Vector2 a, b, c, d;
            public Vector2 startUV;
            public Vector2 endUV;
        }

        // Returns four planes, subdivided along the middle lines of the original plane
        private static UVPlane[] subdivideUVPlane(UVPlane p)
        {
            Vector2 p1 = p.a;
            Vector2 p2 = p.b;
            Vector2 p3 = p.c;
            Vector2 p4 = p.d;

            Vector2 p1p2 = Vector2.Lerp(p1, p2, 0.5f);
            Vector2 p2p3 = Vector2.Lerp(p2, p3, 0.5f);
            Vector2 p3p4 = Vector2.Lerp(p3, p4, 0.5f);
            Vector2 p4p1 = Vector2.Lerp(p4, p1, 0.5f);
            Vector2 mid = Vector2.Lerp(p1p2, p3p4, 0.5f);

            float lerpUVX = float.Lerp(p.startUV.X, p.endUV.X, 0.5f);
            float lerpUVY = float.Lerp(p.startUV.Y, p.endUV.Y, 0.5f);

            UVPlane[] result = new UVPlane[4];

            result[0] = new UVPlane(p1, p1p2, mid, p4p1, p.startUV, new Vector2(lerpUVX, lerpUVY));
            result[1] = new UVPlane(p1p2, p2, p2p3, mid, new Vector2(lerpUVX, p.startUV.Y), new Vector2(p.endUV.X, lerpUVY));
            result[2] = new UVPlane(mid, p2p3, p3, p3p4, new Vector2(lerpUVX, lerpUVY), p.endUV);
            result[3] = new UVPlane(p4p1, mid, p3p4, p4, new Vector2(p.startUV.X, lerpUVY), new Vector2(lerpUVX, p.endUV.Y));

            return result;
        }

        private static UVPlane[] subdivideUVPlane(UVPlane[] p)
        {
            UVPlane[] result = new UVPlane[p.Length * 4];
            int i = 0;
            foreach (var plane in p)
            {
                var newPlanes = subdivideUVPlane(plane);
                result[i++] = newPlanes[0];
                result[i++] = newPlanes[1];
                result[i++] = newPlanes[3];
                result[i++] = newPlanes[2];
            }
            return result;
        }

        private enum PlaneType
        {
            FLOOR,
            CEILING
        }

        // A 100x100 units (1m X 1m) plane, no subdivisions, used for far away grid cells
        private static UVPlane Plane1x1 = new UVPlane(new Vector2(0, 0), new Vector2(100, 0), new Vector2(100, 100), new Vector2(0, 100), new Vector2(0, 0), new Vector2(1f, 1f));

        // A 100x100 units (1m X 1m) plane split into 2x2 = 4 sub-planes, used for low-quality
        private static UVPlane[] Plane2x2 = subdivideUVPlane(Plane1x1);

        // A 100x100 units (1m X 1m) plane split into 4x4 = 16 sub-planes, used for mid-range grid cells
        private static UVPlane[] Plane4x4 = subdivideUVPlane(subdivideUVPlane(Plane1x1));

        // A 100x100 units (1m X 1m) plane split into 8x8 = 64 sub-planes, used for closeup grid cells
        private static UVPlane[] Plane8x8 = subdivideUVPlane(subdivideUVPlane(subdivideUVPlane(Plane1x1)));

        private void drawHorizontalPlanesAt(int x, int y, Vector2 cameraPoint, Vector2 cameraDir, float nearPlaneDistance, DoomRenderOptions options)
        {
            int x1 = x * 100;
            int y1 = y * 100;

            // Stop drawing if cell is too far away
            Vector2 mid = new Vector2(x1 + 50, y1 + 50);
            float d = Vector2.Distance(mid, cameraPoint);
            if (d > options.UVPlaneCutoffDistance)
            {
                return;
            }

            void drawUVPlanes(UVPlane[] uVPlanes)
            {
                foreach (var plane in uVPlanes)
                {
                    if (!string.IsNullOrEmpty(options.floorTexturePath))
                        drawHorizontalPlane(plane, new Vector2(x1, y1), options.floorTexturePath, PlaneType.FLOOR, cameraPoint, cameraDir, nearPlaneDistance, options);
                    if (!string.IsNullOrEmpty(options.ceilingTexturePath))
                        drawHorizontalPlane(plane, new Vector2(x1, y1), options.ceilingTexturePath, PlaneType.CEILING, cameraPoint, cameraDir, nearPlaneDistance, options);
                }
            }

            // Draw subdivided planes at close range (kind of like LODs). This is necessary as textures are linearly interpolated by SDL_RenderGeometry. The artifacts become very noticeable and glitched if we do not subdivide.

            // Draw 8x8 subdivided planes at close range
            if (d < options.highQualityUVPlaneCutoffDistance)
            {
                drawUVPlanes(Plane8x8);
                return;
            }

            // Draw 4x4 subdivided planes at mid range
            if (d < options.midQualityUVPlaneCutoffDistance)
            {
                drawUVPlanes(Plane4x4);
                return;
            }

            // Draw 2x2 subdivided planes at mid range
            if (d < options.midQualityUVPlaneCutoffDistance)
            {
                drawUVPlanes(Plane2x2);
                return;
            }

            // Draw one plane for the ceiling and one for the floor for faraway grid cells
            if (!string.IsNullOrEmpty(options.floorTexturePath))
                drawHorizontalPlane(Plane1x1, new Vector2(x1, y1), options.floorTexturePath, PlaneType.FLOOR, cameraPoint, cameraDir, nearPlaneDistance, options);
            if (!string.IsNullOrEmpty(options.ceilingTexturePath))
                drawHorizontalPlane(Plane1x1, new Vector2(x1, y1), options.ceilingTexturePath, PlaneType.CEILING, cameraPoint, cameraDir, nearPlaneDistance, options);
        }

        private void drawHorizontalPlane(UVPlane plane, Vector2 worldOffset, string texturePath, PlaneType type, Vector2 cameraPoint, Vector2 cameraDir, float nearPlaneDistance, DoomRenderOptions options)
        {
            Vector2 worldToScreenCoordinates(Vector2 vec)
            {
                Vector2 camToVec = (vec - cameraPoint);
                float distance = camToVec.Length();
                Vector2 direction = Vector2.Normalize(camToVec);

                float angle = MathF.Atan2(direction.X * cameraDir.Y - direction.Y * cameraDir.X,
                                          direction.X * cameraDir.X + direction.Y * cameraDir.Y);

                int x = (int)(_width / 2 - MathF.Tan(angle) * nearPlaneDistance);
                int lineHeight = (int)(1.0f * MathF.Ceiling(_height * 10000 / MathF.Max(distance * MathF.Cos(angle) / options.worldUnitScale, 0.001f)) / 10000);
                int y = _height / 2 + lineHeight / 2 * (type == PlaneType.FLOOR ? 1 : -1);

                return new Vector2(x, y); // TODO: Make linear interpolation over 1/z coordinates work properly!
            }

            Vector2 p1 = worldToScreenCoordinates(plane.a + worldOffset);
            Vector2 p2 = worldToScreenCoordinates(plane.b + worldOffset);
            Vector2 p3 = worldToScreenCoordinates(plane.c + worldOffset);
            Vector2 p4 = worldToScreenCoordinates(plane.d + worldOffset);

            IntPtr sprite = loadTexture(Bootstrap.getAssetManager().getAssetPath(texturePath));

            SDL_Vertex[] vertices = new SDL_Vertex[]
            {
                new SDL_Vertex { position = new SDL_FPoint { x = p1.X, y = p1.Y }, color = new SDL_Color { r = 255, g = 255, b = 255, a = 255 },
                    tex_coord = new SDL_FPoint { x = plane.startUV.X, y = plane.startUV.Y} },

                new SDL_Vertex { position = new SDL_FPoint { x = p2.X, y = p2.Y }, color = new SDL_Color { r = 255, g = 255, b = 255, a = 255 },
                    tex_coord = new SDL_FPoint { x = plane.endUV.X, y = plane.startUV.Y} },

                new SDL_Vertex { position = new SDL_FPoint { x = p3.X, y = p3.Y }, color = new SDL_Color { r = 255, g = 255, b = 255, a = 255 },
                    tex_coord = new SDL_FPoint { x = plane.endUV.X, y = plane.endUV.Y} },

                new SDL_Vertex { position = new SDL_FPoint { x = p4.X, y = p4.Y }, color = new SDL_Color { r = 255, g = 255, b = 255, a = 255 },
                    tex_coord = new SDL_FPoint { x = plane.startUV.X, y = plane.endUV.Y} }
            };

            int[] indices = { 0, 1, 2, 2, 3, 0 };

            SDL_RenderGeometry(_rend, sprite, vertices, vertices.Length, indices, indices.Length);
        }

        public override void drawDoomScene2D(float camx, float camy, Vector2 dir, float fov, DoomSegment[] segments, float renderDistance)
        {
            for (int i = 0; i < segments.Length; i++)
            {
                DoomSegment s = segments[i];
                drawLine((int)s.StartPoint.X, (int)s.StartPoint.Y, (int)s.EndPoint.X, (int)s.EndPoint.Y, s.R, s.G, s.B, s.A);
            }

            // View setup
            Vector2 cam = new Vector2(camx, camy);
            Vector2 normalizedDir = Vector2.Normalize(dir);
            float fovInRadians = fov * MathF.PI / 180;
            float halfFov = fovInRadians * 0.5f;
            // Uses the formula cos(x) = adj/hyp where x is halfFov and the hypotenuse of the right-angle triangle is 1000.0f
            // Not sure why 1000.0f works, this should be investigated further. Also, the resulting view angle does not correspond with the FOV exactly...
            // This is evident if you show both the lines of each ray and the frustum lines, the latter showing the correct angle as specified
            float nearPlaneDistance = MathF.Cos(halfFov) * 1000f;

            // Create rays for each pixel column and draw them as lines in 2D space
            // TODO: Store precomputed rays instead of creating them every time!
            for (int i = 0; i < _width; i++)
            {
                // Calculates the current angle based on the pixel's position along the near plane
                // This is necessary for correct texture scaling and wall height
                // Read more: https://stackoverflow.com/questions/24173966/raycasting-engine-rendering-creating-slight-distortion-increasing-towards-edges
                float angle = MathF.Atan((_width / 2 - i) / nearPlaneDistance);

                Matrix3x2 rotationMatrix = new Matrix3x2(MathF.Cos(angle), -MathF.Sin(angle), MathF.Sin(angle), MathF.Cos(angle), 0, 0);
                Vector2 rayDirection = Vector2.Transform(dir, rotationMatrix);

                Vector2 closestIntersection = Vector2.Zero;
                float closestDistance = renderDistance;
                bool didIntersect = false;
                DoomSegment closestSegment = null;

                for (int j = 0; j < segments.Length; j++)
                {
                    Vector2 intersection = Vector2.Zero;
                    float u = 0f;
                    bool intersectsWithSegment = segments[j].getIntersectionWithRay(cam, rayDirection, out intersection, out u);

                    if (intersectsWithSegment)
                    {
                        float d = Vector2.Distance(intersection, cam);
                        if (d < closestDistance)
                        {
                            didIntersect = true;
                            closestDistance = d;
                            closestSegment = segments[j];
                            closestIntersection = intersection;
                        }
                    }
                }

                if (didIntersect)
                {
                    drawFilledCircle((int)closestIntersection.X, (int)closestIntersection.Y, 4, closestSegment.R, closestSegment.G, closestSegment.B, closestSegment.A);
                }

                Vector2 rayEndPoint = cam + rayDirection * 100;
                drawLine((int)camx, (int)camy, (int)rayEndPoint.X, (int)rayEndPoint.Y, System.Drawing.Color.Yellow);
                continue; // Uncomment this to visualize each ray
            }

            // Draw view frustum lines
            Matrix3x2 rightFrustumLineRotationMatrix = new Matrix3x2(MathF.Cos(-halfFov), -MathF.Sin(-halfFov), MathF.Sin(-halfFov), MathF.Cos(-halfFov), 0, 0);
            Vector2 rightFrustumLineDirection = Vector2.Transform(dir, rightFrustumLineRotationMatrix);
            Vector2 rightFrustumLineEndPoint = cam + rightFrustumLineDirection * 1000.0f;
            drawLine((int)camx, (int)camy, (int)rightFrustumLineEndPoint.X, (int)rightFrustumLineEndPoint.Y, System.Drawing.Color.Yellow); // Right view frustum line
            Matrix3x2 leftFrustumLineRotationMatrix = new Matrix3x2(MathF.Cos(halfFov), -MathF.Sin(halfFov), MathF.Sin(halfFov), MathF.Cos(halfFov), 0, 0);
            Vector2 leftFrustumLineDirection = Vector2.Transform(dir, leftFrustumLineRotationMatrix);
            Vector2 leftFrustumLineEndPoint = cam + leftFrustumLineDirection * 1000.0f;
            drawLine((int)camx, (int)camy, (int)leftFrustumLineEndPoint.X, (int)leftFrustumLineEndPoint.Y, System.Drawing.Color.Yellow); // Left view frustum line

            // Draw camera point
            drawFilledCircle((int)camx, (int)camy, 5, System.Drawing.Color.Yellow);
        }

        private bool getLineRayIntersection(Vector2 o, Vector2 d, Line l, out Vector2 intersection, out float u)
        {
            intersection = Vector2.Zero;
            u = 0f;

            Vector2 p1 = new Vector2(l.Sx, l.Sy);
            Vector2 p2 = new Vector2(l.Ex, l.Ey);
            var v1 = o - p1;
            var v2 = p2 - p1;
            var v3 = new Vector2(-d.Y, d.X);

            var dot = Vector2.Dot(v2, v3);
            if (Math.Abs(dot) < 0.000001)
            {
                return false;
            }

            var t1 = (v2.X * v1.Y - v2.Y * v1.X) / dot;
            var t2 = Vector2.Dot(v1, v3) / dot;

            if (t1 >= 0.0 && (t2 >= 0.0 && t2 <= 1.0))
            {
                u = t2;
                intersection = o + t1 * d;
                return true;
            }

            return false;
        }

        public override void display()
        {
            DoomRenderer.getInstance().DrawDoomScene();

            SDL.SDL_Rect sRect;
            SDL.SDL_Rect tRect;

            foreach (Transform trans in _toDraw)
            {
                if (trans.SpritePath == null)
                {
                    continue;
                }

                var sprite = loadTexture(trans);

                sRect.x = 0;
                sRect.y = 0;
                sRect.w = (int)(trans.Wid * trans.Scalex);
                sRect.h = (int)(trans.Ht * trans.Scaley);

                tRect.x = (int)trans.X;
                tRect.y = (int)trans.Y;
                tRect.w = sRect.w;
                tRect.h = sRect.h;

                SDL.SDL_RenderCopyEx(_rend, sprite, ref sRect, ref tRect, (int)trans.Rotz, IntPtr.Zero, SDL.SDL_RendererFlip.SDL_FLIP_NONE);
            }

            foreach (Circle c in _circlesToDraw)
            {
                SDL.SDL_SetRenderDrawColor(_rend, (byte)c.R, (byte)c.G, (byte)c.B, (byte)c.A);
                renderCircle(c.X, c.Y, c.Radius);
            }

            foreach (Line l in _linesToDraw)
            {
                SDL.SDL_SetRenderDrawColor(_rend, (byte)l.R, (byte)l.G, (byte)l.B, (byte)l.A);
                SDL.SDL_RenderDrawLine(_rend, l.Sx, l.Sy, l.Ex, l.Ey);
            }

            // Show it off.
            base.display();
        }

        public override void clearDisplay()
        {
            _toDraw.Clear();
            _circlesToDraw.Clear();
            _linesToDraw.Clear();

            base.clearDisplay();
        }
    }
}