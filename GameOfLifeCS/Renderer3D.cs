using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;

namespace GameOfLifeCS
{
    public class Renderer3D
    {
        private const float CellElevation = 15f;
        private const float PerspectiveFactor = 0.15f;
        private const float CellHeight = 20f;
        
        // Cached brushes and pens for performance
        private readonly Dictionary<Color, SolidBrush> brushCache = new();
        private readonly Dictionary<(Color, float), Pen> penCache = new();

        public void RenderWithDepth(Graphics g, Board board, ColorTheme theme, int cellSize, Rectangle bounds)
        {
            // Use faster rendering settings
            g.SmoothingMode = SmoothingMode.HighSpeed;
            g.CompositingQuality = CompositingQuality.HighSpeed;
            g.InterpolationMode = InterpolationMode.Low;
            g.PixelOffsetMode = PixelOffsetMode.HighSpeed;

            // Draw background (simpler version)
            DrawOptimizedBackground(g, theme, bounds);

            // Draw simplified grid
            DrawOptimizedGrid(g, board, theme, cellSize);

            // Collect all alive cells for batch processing
            var aliveCells = new List<(int row, int col)>();
            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)
                {
                    if (board[row, col])
                    {
                        aliveCells.Add((row, col));
                    }
                }
            }

            // Sort back to front for proper depth
            aliveCells.Sort((a, b) => b.row.CompareTo(a.row));

            // Draw all cells in batches
            DrawCellsBatch(g, aliveCells, cellSize, theme, board.Rows);

            // Light fog overlay
            DrawSimpleFog(g, bounds, theme);
            
            // Clear cache periodically to prevent memory buildup
            if (brushCache.Count > 50)
            {
                ClearCaches();
            }
        }

        private void DrawOptimizedBackground(Graphics g, ColorTheme theme, Rectangle bounds)
        {
            Color topColor = GetDarkerColor(theme.Background, 0.4f);
            Color bottomColor = theme.Background;

            using var brush = new LinearGradientBrush(
                bounds,
                topColor,
                bottomColor,
                LinearGradientMode.Vertical);

            g.FillRectangle(brush, bounds);
        }

        private void DrawOptimizedGrid(Graphics g, Board board, ColorTheme theme, int cellSize)
        {
            Color gridColor = Color.FromArgb(40, theme.Grid);
            using var pen = new Pen(gridColor, 1f);

            // Draw fewer grid lines for performance
            int step = board.Rows > 80 ? 2 : 1; // Skip lines on huge boards

            // Horizontal lines
            for (int i = 0; i <= board.Rows; i += step)
            {
                float y = i * cellSize;
                float perspectiveOffset = y * PerspectiveFactor * 0.3f;
                
                g.DrawLine(pen, 
                    perspectiveOffset * 20, y, 
                    board.Columns * cellSize - perspectiveOffset * 20, y);
            }

            // Vertical lines
            for (int j = 0; j <= board.Columns; j += step)
            {
                float x = j * cellSize;
                float xOffset = (j - board.Columns / 2f) * PerspectiveFactor * 10;
                
                g.DrawLine(pen, 
                    x + xOffset, 0, 
                    x + xOffset * 1.5f, board.Rows * cellSize);
            }
        }

        private void DrawCellsBatch(Graphics g, List<(int row, int col)> cells, int cellSize, ColorTheme theme, int totalRows)
        {
            // Pre-calculate common colors
            Color cellColor = theme.Cell;
            Color sideColor = DarkenColor(cellColor, 0.4f);
            Color topLightColor = LightenColor(cellColor, 0.4f);
            Color shadowColor = Color.FromArgb(100, 0, 0, 0);

            // Get cached brushes
            var sideBrush = GetCachedBrush(sideColor);
            var shadowBrush = GetCachedBrush(shadowColor);
            var topPen = GetCachedPen(topLightColor, 1.5f);

            foreach (var (row, col) in cells)
            {
                float x = col * cellSize;
                float y = row * cellSize;

                float depthRatio = 1.0f - (row / (float)totalRows) * 0.25f;
                float perspectiveX = (col - totalRows / 2f) * row * PerspectiveFactor * 0.3f;
                x += perspectiveX;

                float height = CellHeight * depthRatio * 0.7f;

                // Simplified shadow (just a rectangle)
                RectangleF shadowRect = new(x + 3, y + 3, cellSize - 1, cellSize - 1);
                g.FillRectangle(shadowBrush, shadowRect);

                // Draw 3D block - simplified to just 2 visible faces
                
                // Bottom face (simplified)
                PointF[] bottomFace = new[]
                {
                    new PointF(x, y),
                    new PointF(x + cellSize - 1, y),
                    new PointF(x + cellSize - 1 + height * 0.4f, y + height * 0.4f),
                    new PointF(x + height * 0.4f, y + height * 0.4f)
                };
                g.FillPolygon(sideBrush, bottomFace);

                // Right face (simplified)
                PointF[] rightFace = new[]
                {
                    new PointF(x + cellSize - 1, y - height),
                    new PointF(x + cellSize - 1, y),
                    new PointF(x + cellSize - 1 + height * 0.4f, y + height * 0.4f),
                    new PointF(x + cellSize - 1 + height * 0.4f, y - height + height * 0.4f)
                };
                g.FillPolygon(sideBrush, rightFace);

                // Top face (flat rectangle with simple gradient)
                RectangleF topRect = new(x, y - height, cellSize - 1, cellSize - 1);
                
                using (var topBrush = new LinearGradientBrush(
                    topRect,
                    topLightColor,
                    cellColor,
                    LinearGradientMode.Vertical))
                {
                    g.FillRectangle(topBrush, topRect);
                }

                // Simple highlight lines
                g.DrawLine(topPen, x, y - height, x + cellSize - 1, y - height);
                g.DrawLine(topPen, x, y - height, x, y - height + cellSize - 1);
            }
        }

        private void DrawSimpleFog(Graphics g, Rectangle bounds, ColorTheme theme)
        {
            Color fogColor = Color.FromArgb(30, theme.Background);
            
            using var fogBrush = new LinearGradientBrush(
                new Point(0, 0),
                new Point(0, bounds.Height),
                Color.Transparent,
                fogColor);

            g.FillRectangle(fogBrush, bounds);
        }

        private SolidBrush GetCachedBrush(Color color)
        {
            if (!brushCache.TryGetValue(color, out var brush))
            {
                brush = new SolidBrush(color);
                brushCache[color] = brush;
            }
            return brush;
        }

        private Pen GetCachedPen(Color color, float width)
        {
            var key = (color, width);
            if (!penCache.TryGetValue(key, out var pen))
            {
                pen = new Pen(color, width);
                penCache[key] = pen;
            }
            return pen;
        }

        private void ClearCaches()
        {
            foreach (var brush in brushCache.Values)
            {
                brush.Dispose();
            }
            brushCache.Clear();

            foreach (var pen in penCache.Values)
            {
                pen.Dispose();
            }
            penCache.Clear();
        }

        private Color LightenColor(Color color, float amount)
        {
            return Color.FromArgb(
                color.A,
                Math.Min(255, (int)(color.R + (255 - color.R) * amount)),
                Math.Min(255, (int)(color.G + (255 - color.G) * amount)),
                Math.Min(255, (int)(color.B + (255 - color.B) * amount))
            );
        }

        private Color DarkenColor(Color color, float amount)
        {
            return Color.FromArgb(
                color.A,
                (int)(color.R * (1 - amount)),
                (int)(color.G * (1 - amount)),
                (int)(color.B * (1 - amount))
            );
        }

        private Color GetDarkerColor(Color color, float factor)
        {
            return Color.FromArgb(
                color.A,
                (int)(color.R * factor),
                (int)(color.G * factor),
                (int)(color.B * factor)
            );
        }

        // Call this when done to clean up resources
        public void Dispose()
        {
            ClearCaches();
        }
    }
}
