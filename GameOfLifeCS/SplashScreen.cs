using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Text;

namespace GameOfLifeCS
{
    public class SplashScreen : Panel
    {
        private readonly System.Windows.Forms.Timer animationTimer;
        private float animationProgress = 0f;
        private readonly Button playButton;
        private readonly Random random = new();
        private readonly List<AnimatedCell> animatedCells = new();

        public event EventHandler? PlayClicked;

        public SplashScreen()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(240, 0, 0, 0); // Semi-transparent dark overlay
            DoubleBuffered = true;

            // Create animated cells for background
            for (int i = 0; i < 50; i++)
            {
                animatedCells.Add(new AnimatedCell
                {
                    X = random.Next(0, 1000),
                    Y = random.Next(0, 700),
                    Size = random.Next(8, 20),
                    Speed = (float)(random.NextDouble() * 2 + 0.5),
                    Brightness = (float)random.NextDouble()
                });
            }

            // Create Play button
            playButton = new Button
            {
                Text = "▶ PLAY",
                Size = new Size(200, 60),
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 255, 100),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            playButton.FlatAppearance.BorderSize = 0;
            playButton.FlatAppearance.BorderColor = Color.FromArgb(0, 200, 80);
            playButton.Click += (s, e) => PlayClicked?.Invoke(this, EventArgs.Empty);

            // Add hover effects
            playButton.MouseEnter += (s, e) =>
            {
                playButton.BackColor = Color.FromArgb(50, 255, 150);
                playButton.Font = new Font("Segoe UI", 22, FontStyle.Bold);
            };
            playButton.MouseLeave += (s, e) =>
            {
                playButton.BackColor = Color.FromArgb(0, 255, 100);
                playButton.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            };

            Controls.Add(playButton);

            // Start animation
            animationTimer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60 FPS
            animationTimer.Tick += OnAnimationTick;
            animationTimer.Start();

            Resize += (s, e) => CenterButton();
        }

        private void CenterButton()
        {
            playButton.Location = new Point(
                (Width - playButton.Width) / 2,
                (Height - playButton.Height) / 2 + 100
            );
        }

        private void OnAnimationTick(object? sender, EventArgs e)
        {
            animationProgress += 0.02f;
            if (animationProgress > Math.PI * 2) animationProgress = 0;

            // Update animated cells
            foreach (var cell in animatedCells)
            {
                cell.Y += cell.Speed;
                if (cell.Y > Height) cell.Y = -20;
                
                cell.Brightness += (float)(random.NextDouble() * 0.1 - 0.05);
                cell.Brightness = Math.Clamp(cell.Brightness, 0.3f, 1f);
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // Draw animated background cells
            DrawAnimatedCells(g);

            // Draw title with glow effect
            DrawTitle(g);

            // Draw subtitle
            DrawSubtitle(g);

            // Draw animated border around play button
            DrawPlayButtonGlow(g);
        }

        private void DrawAnimatedCells(Graphics g)
        {
            foreach (var cell in animatedCells)
            {
                int alpha = (int)(100 * cell.Brightness);
                using var brush = new SolidBrush(Color.FromArgb(alpha, 0, 255, 100));
                g.FillRectangle(brush, cell.X, cell.Y, cell.Size, cell.Size);

                // Draw glow
                using var glowBrush = new SolidBrush(Color.FromArgb(alpha / 3, 0, 255, 100));
                g.FillRectangle(glowBrush, 
                    cell.X - 2, cell.Y - 2, 
                    cell.Size + 4, cell.Size + 4);
            }
        }

        private void DrawTitle(Graphics g)
        {
            string title = "CONWAY'S";
            string subtitle = "GAME OF LIFE";

            using var titleFont = new Font("Segoe UI", 72, FontStyle.Bold);
            using var subtitleFont = new Font("Segoe UI", 56, FontStyle.Bold);

            var titleSize = g.MeasureString(title, titleFont);
            var subtitleSize = g.MeasureString(subtitle, subtitleFont);

            float titleX = (Width - titleSize.Width) / 2;
            float titleY = Height / 2 - 200;
            float subtitleX = (Width - subtitleSize.Width) / 2;
            float subtitleY = titleY + titleSize.Height - 20;

            // Draw glow effect
            float glowIntensity = (float)(Math.Sin(animationProgress * 2) * 0.3 + 0.7);
            for (int i = 5; i > 0; i--)
            {
                using var glowBrush = new SolidBrush(
                    Color.FromArgb((int)(30 * glowIntensity), 0, 255, 100));
                g.DrawString(title, titleFont, glowBrush, titleX + i, titleY + i);
                g.DrawString(subtitle, subtitleFont, glowBrush, subtitleX + i, subtitleY + i);
            }

            // Draw main title
            using var titleBrush = new LinearGradientBrush(
                new RectangleF(titleX, titleY, titleSize.Width, titleSize.Height),
                Color.FromArgb(0, 255, 150),
                Color.FromArgb(0, 200, 100),
                LinearGradientMode.Vertical);
            g.DrawString(title, titleFont, titleBrush, titleX, titleY);

            using var subtitleBrush = new LinearGradientBrush(
                new RectangleF(subtitleX, subtitleY, subtitleSize.Width, subtitleSize.Height),
                Color.FromArgb(100, 255, 200),
                Color.FromArgb(0, 200, 100),
                LinearGradientMode.Vertical);
            g.DrawString(subtitle, subtitleFont, subtitleBrush, subtitleX, subtitleY);
        }

        private void DrawSubtitle(Graphics g)
        {
            string text = "A Cellular Automaton Simulation";
            using var font = new Font("Segoe UI", 18, FontStyle.Italic);
            var size = g.MeasureString(text, font);
            float x = (Width - size.Width) / 2;
            float y = Height / 2 - 60;

            using var brush = new SolidBrush(Color.FromArgb(180, 200, 200, 200));
            g.DrawString(text, font, brush, x, y);
        }

        private void DrawPlayButtonGlow(Graphics g)
        {
            float glowSize = (float)(Math.Sin(animationProgress * 3) * 10 + 15);
            var glowRect = new Rectangle(
                playButton.Left - (int)glowSize,
                playButton.Top - (int)glowSize,
                playButton.Width + (int)(glowSize * 2),
                playButton.Height + (int)(glowSize * 2)
            );

            using var path = new GraphicsPath();
            path.AddRectangle(glowRect);

            using var brush = new PathGradientBrush(path)
            {
                CenterColor = Color.FromArgb(100, 0, 255, 100),
                SurroundColors = new[] { Color.Transparent }
            };

            g.FillPath(brush, path);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                animationTimer?.Stop();
                animationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        private class AnimatedCell
        {
            public float X { get; set; }
            public float Y { get; set; }
            public int Size { get; set; }
            public float Speed { get; set; }
            public float Brightness { get; set; }
        }
    }
}
