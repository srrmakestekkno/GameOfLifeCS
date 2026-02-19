using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace GameOfLifeCS
{
    public class ThemeManager
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public ColorTheme CurrentTheme { get; set; } = ColorTheme.Classic;

        public void ApplyToForm(Form form, MenuStrip menuStrip, Panel controlPanel, Control gamePanel)
        {
            form.BackColor = CurrentTheme.Background;
            UseImmersiveDarkMode(form.Handle, IsDarkTheme());

            ApplyToMenuStrip(menuStrip);
            ApplyToControlPanel(controlPanel);
            gamePanel.BackColor = CurrentTheme.Grid;
        }

        private void ApplyToMenuStrip(MenuStrip menuStrip)
        {
            menuStrip.BackColor = GetDarkerColor(CurrentTheme.Background, 0.9f);
            menuStrip.ForeColor = GetContrastColor(CurrentTheme.Background);
            
            // Set custom renderer for proper hover/selection colors
            menuStrip.Renderer = new ThemedMenuRenderer(CurrentTheme);

            foreach (ToolStripMenuItem item in menuStrip.Items)
            {
                ApplyToMenuItem(item);
            }
        }

        private void ApplyToMenuItem(ToolStripMenuItem menuItem)
        {
            menuItem.BackColor = GetDarkerColor(CurrentTheme.Background, 0.9f);
            menuItem.ForeColor = GetContrastColor(CurrentTheme.Background);

            foreach (var item in menuItem.DropDownItems)
            {
                if (item is ToolStripMenuItem subMenuItem)
                {
                    ApplyToMenuItem(subMenuItem);
                }
                else if (item is ToolStripSeparator separator)
                {
                    separator.BackColor = CurrentTheme.Grid;
                    separator.ForeColor = CurrentTheme.Grid;
                }
            }

            if (menuItem.DropDown != null)
            {
                menuItem.DropDown.BackColor = GetDarkerColor(CurrentTheme.Background, 0.85f);
                menuItem.DropDown.Renderer = new ThemedMenuRenderer(CurrentTheme);
            }
        }

        private void ApplyToControlPanel(Panel controlPanel)
        {
            controlPanel.BackColor = GetDarkerColor(CurrentTheme.Background, 0.95f);

            foreach (Control control in controlPanel.Controls)
            {
                if (control is Button button)
                {
                    button.BackColor = GetDarkerColor(CurrentTheme.Background, 0.8f);
                    button.ForeColor = CurrentTheme.Cell;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = CurrentTheme.Grid;
                }
                else if (control is Label label)
                {
                    label.ForeColor = GetContrastColor(CurrentTheme.Background);
                }
                else if (control is TrackBar trackBar)
                {
                    trackBar.BackColor = controlPanel.BackColor;
                }
            }
        }

        public void ApplyToDialog(Form dialog)
        {
            dialog.BackColor = CurrentTheme.Background;
            dialog.ForeColor = GetContrastColor(CurrentTheme.Background);
            UseImmersiveDarkMode(dialog.Handle, IsDarkTheme());

            foreach (Control control in dialog.Controls)
            {
                if (control is Button button)
                {
                    button.BackColor = GetDarkerColor(CurrentTheme.Background, 0.8f);
                    button.ForeColor = CurrentTheme.Cell;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = CurrentTheme.Grid;
                }
                else if (control is Label label)
                {
                    label.ForeColor = GetContrastColor(CurrentTheme.Background);
                }
                else if (control is TextBox textBox)
                {
                    textBox.BackColor = GetDarkerColor(CurrentTheme.Background, 0.9f);
                    textBox.ForeColor = GetContrastColor(CurrentTheme.Background);
                }
                else if (control is ListBox listBox)
                {
                    listBox.BackColor = GetDarkerColor(CurrentTheme.Background, 0.9f);
                    listBox.ForeColor = GetContrastColor(CurrentTheme.Background);
                }
            }
        }

        public bool IsDarkTheme()
        {
            double brightness = (CurrentTheme.Background.R * 0.299 +
                                CurrentTheme.Background.G * 0.587 +
                                CurrentTheme.Background.B * 0.114);
            return brightness < 128;
        }

        public Color GetDarkerColor(Color color, float factor)
        {
            return Color.FromArgb(
                color.A,
                (int)(color.R * factor),
                (int)(color.G * factor),
                (int)(color.B * factor)
            );
        }

        public Color GetLighterColor(Color color, float factor)
        {
            return Color.FromArgb(
                color.A,
                Math.Min(255, (int)(color.R + (255 - color.R) * factor)),
                Math.Min(255, (int)(color.G + (255 - color.G) * factor)),
                Math.Min(255, (int)(color.B + (255 - color.B) * factor))
            );
        }

        public Color GetContrastColor(Color backgroundColor)
        {
            double brightness = (backgroundColor.R * 0.299 + backgroundColor.G * 0.587 + backgroundColor.B * 0.114);
            return brightness < 128 ? Color.White : Color.Black;
        }

        private bool UseImmersiveDarkMode(IntPtr handle, bool enabled)
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
            {
                int attribute = DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;
                if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18985))
                {
                    attribute = DWMWA_USE_IMMERSIVE_DARK_MODE;
                }

                int useImmersiveDarkMode = enabled ? 1 : 0;
                return DwmSetWindowAttribute(handle, attribute, ref useImmersiveDarkMode, sizeof(int)) == 0;
            }

            return false;
        }

        // Custom menu renderer for proper theming
        private class ThemedMenuRenderer : ToolStripProfessionalRenderer
        {
            private readonly ColorTheme theme;

            public ThemedMenuRenderer(ColorTheme theme) : base(new ThemedColorTable(theme))
            {
                this.theme = theme;
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (e.Item.Selected || e.Item.Pressed)
                {
                    // Highlight color for hover/pressed
                    Color highlightColor = GetHighlightColor(theme.Background);
                    using var brush = new SolidBrush(highlightColor);
                    e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
                }
                else
                {
                    // Normal background
                    using var brush = new SolidBrush(e.Item.BackColor);
                    e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
                }
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                // Ensure text is always visible with proper contrast
                e.TextColor = GetContrastColor(theme.Background);
                base.OnRenderItemText(e);
            }

            private Color GetHighlightColor(Color baseColor)
            {
                double brightness = (baseColor.R * 0.299 + baseColor.G * 0.587 + baseColor.B * 0.114);
                
                // For dark themes, lighten; for light themes, darken
                if (brightness < 128)
                {
                    return Color.FromArgb(
                        Math.Min(255, baseColor.R + 40),
                        Math.Min(255, baseColor.G + 40),
                        Math.Min(255, baseColor.B + 40)
                    );
                }
                else
                {
                    return Color.FromArgb(
                        Math.Max(0, baseColor.R - 40),
                        Math.Max(0, baseColor.G - 40),
                        Math.Max(0, baseColor.B - 40)
                    );
                }
            }

            private Color GetContrastColor(Color backgroundColor)
            {
                double brightness = (backgroundColor.R * 0.299 + backgroundColor.G * 0.587 + backgroundColor.B * 0.114);
                return brightness < 128 ? Color.White : Color.Black;
            }
        }

        // Custom color table for the renderer
        private class ThemedColorTable : ProfessionalColorTable
        {
            private readonly ColorTheme theme;

            public ThemedColorTable(ColorTheme theme)
            {
                this.theme = theme;
            }

            public override Color MenuItemSelected => GetHighlightColor();
            public override Color MenuItemSelectedGradientBegin => GetHighlightColor();
            public override Color MenuItemSelectedGradientEnd => GetHighlightColor();
            public override Color MenuItemPressedGradientBegin => GetDarkerHighlight();
            public override Color MenuItemPressedGradientEnd => GetDarkerHighlight();
            public override Color MenuItemBorder => theme.Grid;
            public override Color MenuBorder => theme.Grid;
            public override Color ImageMarginGradientBegin => GetDarkerColor(theme.Background, 0.85f);
            public override Color ImageMarginGradientMiddle => GetDarkerColor(theme.Background, 0.85f);
            public override Color ImageMarginGradientEnd => GetDarkerColor(theme.Background, 0.85f);

            private Color GetHighlightColor()
            {
                double brightness = (theme.Background.R * 0.299 + theme.Background.G * 0.587 + theme.Background.B * 0.114);
                
                if (brightness < 128)
                {
                    // Dark theme - lighten
                    return Color.FromArgb(
                        Math.Min(255, theme.Background.R + 40),
                        Math.Min(255, theme.Background.G + 40),
                        Math.Min(255, theme.Background.B + 40)
                    );
                }
                else
                {
                    // Light theme - darken
                    return Color.FromArgb(
                        Math.Max(0, theme.Background.R - 40),
                        Math.Max(0, theme.Background.G - 40),
                        Math.Max(0, theme.Background.B - 40)
                    );
                }
            }

            private Color GetDarkerHighlight()
            {
                Color highlight = GetHighlightColor();
                return Color.FromArgb(
                    (int)(highlight.R * 0.9f),
                    (int)(highlight.G * 0.9f),
                    (int)(highlight.B * 0.9f)
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
        }
    }
}
