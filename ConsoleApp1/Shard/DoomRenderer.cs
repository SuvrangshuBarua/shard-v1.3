using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace Shard.Shard
{
    internal struct DoomRenderOptions
    {
        public DoomRenderOptions()
        {
        }

        public float fov = 60;
        public float renderDistance = 5000;
        public float highQualityUVPlaneCutoffDistance = 300f; // 8x8
        public float midQualityUVPlaneCutoffDistance = 1000f; // 4x4
        public float lowQualityUVPlaneCutoffDistance = 2000f; // 2x2
        public float UVPlaneCutoffDistance = 5000f; // 1x1

        // This value is a scaling factor for the height of the game world compared to its width, 100 seems to be a nice number for converting between 2D size,
        // (where 100 units of length equals 100 pixels on the screen) and the 3D world, where 100 units is converted into 1 world unit (looks like around 1m)
        public float worldUnitScale = 100f;

        // TODO: Implement the ability to define textures individually for grid cells
        public string floorTexturePath = "";

        public string ceilingTexturePath = "";

        // The maximum depth (n) of transparent segments to draw on top of each other, the n + 1th and subsequent transparent segments will be skipped
        public int maxTransparentSegmentDepth = 10;
    }

    // This is used when building the grid, a list and coordinates is more convenient here
    internal struct DoomGridCell
    {
        internal int x;
        internal int y;
        internal List<DoomSegment> doomSegments;
    }

    // This is used for the final cell, an array is more performant here
    internal struct StaticGridCell
    {
        internal DoomSegment[] segments;
    }

    internal struct StaticGridChunk
    {
        public StaticGridChunk()
        {
            cells = new();
        }

        internal ConcurrentDictionary<(int, int), DoomSegment[]> cells;
    }

    internal struct DoomGrid
    {
        public DoomGrid()
        {
            chunks = new();
            minX = int.MaxValue;
            minY = int.MaxValue;
            maxX = -int.MaxValue;
            maxY = -int.MaxValue;
        }

        private int chunkWidth, chunkHeight = 16;
        private int minX, minY, maxX, maxY; // Used to check the grid's bounding box and exit grid traversal early
        internal int ChunkCount { get { return chunks.Count; } }

        internal int CellCount
        {
            get
            {
                int count = 0;
                foreach (var chunk in chunks)
                {
                    count += chunk.Value.cells.Count;
                }
                return count;
            }
        }

        private ConcurrentDictionary<(int, int), StaticGridChunk> chunks;

        internal DoomSegment[]? GetCellAt(int x, int y, out bool isOutOfBounds)
        {
            // Break if the current grid cell is out of bounds
            if (x < minX || x > maxX || y < minY || y > maxY)
            {
                isOutOfBounds = true;
                return null;
            }

            int chunkX = (int)(x / (float)chunkWidth);
            int chunkY = (int)(y / (float)chunkHeight);

            var chunkFound = chunks.TryGetValue((chunkX, chunkY), out StaticGridChunk chunk);

            if (!chunkFound)
            {
                isOutOfBounds = false;
                return null;
            }

            var cellFound = chunk.cells.TryGetValue((x, y), out DoomSegment[] segments);

            if (cellFound)
            {
                isOutOfBounds = false;
                return segments;
            }

            isOutOfBounds = false;
            return null;
        }

        internal bool PopulateCellAt(int x, int y, DoomSegment[] segments)
        {
            int chunkX = (int)(x / (float)chunkWidth);
            int chunkY = (int)(y / (float)chunkHeight);

            var chunkFound = chunks.TryGetValue((chunkX, chunkY), out StaticGridChunk chunk);

            if (!chunkFound)
            {
                chunk = new StaticGridChunk();
                chunks.TryAdd((chunkX, chunkY), chunk);
            }

            if (x < minX)
                minX = x;
            if (y < minY)
                minY = y;
            if (x > maxX)
                maxX = x;
            if (y > maxY)
                maxY = y;

            return chunk.cells.TryAdd((x, y), segments);
        }

        public ConcurrentDictionary<(int, int), StaticGridChunk> Chunks { get => chunks; }
    }

    internal class DoomRenderer
    {
        private static DoomRenderer me;
        public DoomRenderOptions options = new DoomRenderOptions();
        private GameObject? camera;

        public DoomRenderer()
        {
            options = new();
        }

        public static DoomRenderer getInstance()
        {
            if (me == null)
            {
                me = new DoomRenderer();
            }

            return me;
        }

        private DoomGrid staticGrid; // This contains static, opaque segments in a grid                                         Optimization rating: SUPERB     | ~100000       | tens, or even hundreds of thoudands of concurrent opaque walls possible
        private DoomBillboard[] doomBillboards = { }; // This is meant for transparent objects like enemies and particles       Optimization rating: GREAT      | ~1000         | thousands of concurrent billboards possible
        private DoomSegment[] dynamicDoomSegments = { }; // This is meant for dynamic (i.e. moving) segments like doors         Optimization rating: GOOD/POOR  | ~1000/~100    | thousands of opaque / ~100 (possibly few hundred) of transparent concurrently on screen :(

        internal void SetCamera(GameObject camera)
        {
            this.camera = camera;
        }

        internal void SetDoomBillboards(DoomBillboard[] DoomBillboards)
        {
            doomBillboards = DoomBillboards;
        }

        internal void AddDoomBillboard(DoomBillboard doomBillboard)
        {
            var temp = doomBillboards.ToList();
            temp.Add(doomBillboard);
            doomBillboards = temp.ToArray();
            Debug.Log($"{doomBillboards.Length}");
        }

        internal void RemoveDoomBillboard(DoomBillboard doomBillboard)
        {
            var temp = doomBillboards.ToList();
            temp.Remove(doomBillboard);
            doomBillboards = temp.ToArray();
        }

        internal void SetDynamicSegments(DoomSegment[] DynamicSegments)
        {
            dynamicDoomSegments = DynamicSegments;
        }

        // Currently only works with dynamic opaque segments, i.e. not those used in the static grid
        internal void AddDoomSegment(DoomSegment dynamicSegment)
        {
            var temp = dynamicDoomSegments.ToList();
            temp.Add(dynamicSegment);
            dynamicDoomSegments = temp.ToArray();
        }

        // Currently only works with dynamic opaque segments, i.e. not those used in the static grid
        internal void RemoveDoomSegment(DoomSegment dynamicSegment)
        {
            var temp = dynamicDoomSegments.ToList();
            temp.Remove(dynamicSegment);
            dynamicDoomSegments = temp.ToArray();
        }

        private List<(int, int)> GetGridIntersectionsAlongLine(float x1, float y1, float x2, float y2)
        {
            List<(int, int)> traversedCells = new List<(int, int)>();

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

            // Traverse the grid
            while (x != xEnd || y != yEnd)
            {
                traversedCells.Add((x, y));

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

            traversedCells.Add((xEnd, yEnd)); // Ensure last cell is added
            return traversedCells;
        }

        // Constructs the static grid, all segments in the static grid are opaque
        internal void ConstructStaticGrid(DoomSegment[] segments)
        {
            List<DoomGridCell> doomGridCells = new List<DoomGridCell>();
            Dictionary<(int, int), DoomGridCell> cellLookup = new();

            Debug.Log("Generating grid...");
            Stopwatch sw = Stopwatch.StartNew();

            int countSegments = segments.Length;
            int modPrintProgress = countSegments / 10 + 1;

            int i = 0;
            foreach (DoomSegment segment in segments)
            {
                i++;

                if (i % modPrintProgress == 0)
                {
                    Debug.Log("Creating cells " + (int)((i / (float)countSegments) * 100 + 0.01f) + "%");
                }

                segment.IsTransparent = false;
                Vector2 startPoint = segment.StartPoint;
                Vector2 endPoint = segment.EndPoint;

                float sx = startPoint.X * 0.01f;
                float sy = startPoint.Y * 0.01f;

                float ex = endPoint.X * 0.01f;
                float ey = endPoint.Y * 0.01f;

                List<(int, int)> cellsAlongLine = GetGridIntersectionsAlongLine(sx, sy, ex, ey);

                foreach (var c in cellsAlongLine)
                {
                    DoomGridCell cell;
                    if (!FindGridCellAtCoordinates(c.Item1, c.Item2, out cell))
                    {
                        doomGridCells.Add(cell);
                    }
                    cell.doomSegments.Add(segment);
                }
            }

            staticGrid = new DoomGrid();

            foreach (var cell in cellLookup)
            {
                staticGrid.PopulateCellAt(cell.Value.x, cell.Value.y, cell.Value.doomSegments.ToArray());
            }

            sw.Stop();

            Debug.Log("Done! Processed " + staticGrid.CellCount + " cells across " + staticGrid.ChunkCount + " chunks in " + (sw.ElapsedMilliseconds / (float)1000) + "s");

            bool FindGridCellAtCoordinates(int x, int y, out DoomGridCell cell)
            {
                if (cellLookup.TryGetValue((x, y), out cell))
                {
                    return true;
                }

                cell = new DoomGridCell();
                cell.x = x;
                cell.y = y;
                cell.doomSegments = new List<DoomSegment>();

                cellLookup.Add((x, y), cell);

                return false;
            }
        }

        internal void DrawDebugGrid(GameObject player)
        {
            foreach (var chunk in staticGrid.Chunks)
            {
                (int cx, int cy) = chunk.Key;

                foreach (var cell in chunk.Value.cells)
                {
                    (int ex, int ey) = cell.Key;

                    DrawDebugGridCell(cx + ex, cy + ey, cell.Value, true);
                }
            }

            int size = 50;

            Vector2 p1World = new Vector2(1, 1);
            Vector2 p2World = new Vector2(1, 2);
            Vector2 p3World = new Vector2(2, 2);
            Vector2 p4World = new Vector2(2, 1);

            Bootstrap.getDisplay().drawLine((int)p1World.X * size, (int)p1World.Y * size, (int)p2World.X * size, (int)p2World.Y * size, 0, 0, 255, 255);
            Bootstrap.getDisplay().drawLine((int)p2World.X * size, (int)p2World.Y * size, (int)p3World.X * size, (int)p3World.Y * size, 0, 0, 255, 255);
            Bootstrap.getDisplay().drawLine((int)p3World.X * size, (int)p3World.Y * size, (int)p4World.X * size, (int)p4World.Y * size, 0, 0, 255, 255);
            Bootstrap.getDisplay().drawLine((int)p4World.X * size, (int)p4World.Y * size, (int)p1World.X * size, (int)p1World.Y * size, 0, 0, 255, 255);

            Bootstrap.getDisplay().drawCircle(
                (int)((player.Transform.X * 0.01f) * size),
                (int)((player.Transform.Y * 0.01f) * size),
            5, 255, 255, 255, 255);
            Bootstrap.getDisplay().drawLine(
                (int)((player.Transform.X * 0.01f) * size), (int)((player.Transform.Y * 0.01f) * size),
                (int)(((player.Transform.X + player.Transform.Forward.X * 1000) * 0.01f) * size), (int)(((player.Transform.Y + player.Transform.Forward.Y * 1000) * 0.01f) * size),
            255, 255, 255, 255);
        }

        private void DrawDebugGridCell(int x, int y, DoomSegment[] segments, bool drawSegments = false, int r = 255, int g = 0, int b = 255)
        {
            int size = 50;
            int offset = 0;
            Bootstrap.getDisplay().drawLine((x + 0) * size + offset, (y + 0) * size + offset, (x + 1) * size + offset, (y + 0) * size + offset, r, g, b, 255);
            Bootstrap.getDisplay().drawLine((x + 0) * size + offset, (y + 0) * size + offset, (x + 0) * size + offset, (y + 1) * size + offset, r, g, b, 255);
            Bootstrap.getDisplay().drawLine((x + 1) * size + offset, (y + 0) * size + offset, (x + 1) * size + offset, (y + 1) * size + offset, r, g, b, 255);
            Bootstrap.getDisplay().drawLine((x + 0) * size + offset, (y + 1) * size + offset, (x + 1) * size + offset, (y + 1) * size + offset, r, g, b, 255);

            if (!drawSegments)
                return;

            foreach (DoomSegment s in segments)
            {
                Bootstrap.getDisplay().drawLine(
                    (int)((s.StartPoint.X * 0.01f) * size),
                    (int)((s.StartPoint.Y * 0.01f) * size),
                    (int)((s.EndPoint.X * 0.01f) * size),
                    (int)((s.EndPoint.Y * 0.01f) * size),
                    255, 255, 0, 255
                );
            }
        }

        internal void DrawDoomScene()
        {
            if (camera == null)
            {
                Bootstrap.getDisplay().showText("No camera object assigned in DoomRenderer", Bootstrap.getDisplay().getWidth() / 2 - 200, Bootstrap.getDisplay().getHeight() / 2, 20, 255, 255, 255);
                return;
            };

            Bootstrap.getDisplay().drawDoomScene3D(camera.Transform.X, camera.Transform.Y, camera.Transform.Forward, staticGrid, dynamicDoomSegments, doomBillboards, options);
        }
    }
}