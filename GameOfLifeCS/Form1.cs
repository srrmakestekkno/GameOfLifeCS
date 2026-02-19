using System.Runtime.InteropServices;

namespace GameOfLifeCS
{
    public partial class Form1 : Form
    {
        private const int CellSize = 12;

        private readonly GameController gameController;
        private readonly ThemeManager themeManager;
        private readonly PatternManager patternManager;
        private readonly PatternDialogManager dialogManager;
        private readonly Renderer3D renderer3D;
        private bool use3DRendering = true;
        private SplashScreen? splashScreen;
        private Panel? fadeOverlay;

        private GamePanel gamePanel = null!;
        private Panel controlPanel = null!;
        private MenuStrip menuStrip = null!;
        private Button startStopButton = null!;
        private Button clearButton = null!;
        private Button randomButton = null!;
        private Label generationLabel = null!;
        private TrackBar speedTrackBar = null!;
        private Label speedLabel = null!;
        private TrackBar volumeTrackBar = null!;
        private Label modeLabel = null!;

        public Form1()
        {
            InitializeComponent();

            var board = new Board(50, 50);
            themeManager = new ThemeManager();
            // Set default theme to Sunset
            themeManager.CurrentTheme = ColorTheme.AllThemes.FirstOrDefault(t => t.Name == "Sunset") ?? ColorTheme.Classic;
            
            gameController = new GameController(board, OnGameTick);
            patternManager = new PatternManager();
            dialogManager = new PatternDialogManager(patternManager, themeManager, gameController);
            renderer3D = new Renderer3D();

            // Handle cleanup on form closing
            FormClosing += Form1_FormClosing;

            InitializeUI();
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Clean up resources
            gameController?.SoundManager?.StopBackgroundMusic();
            gameController?.Dispose();
            renderer3D?.Dispose();
            splashScreen?.Dispose();
            fadeOverlay?.Dispose();
        }

        private void InitializeUI()
        {
            Text = "Conway's Game of Life - 3D";
            ClientSize = new Size(1000, 700);
            MinimumSize = new Size(800, 500);

            CreateMenuBar();
            CreateControlPanel();
            CreateGamePanel();
            CreateControls();
            ApplyTheme();
        }

        private void ShowSplashScreen()
        {
            // Hide menu and control panel during splash
            menuStrip.Visible = false;
            controlPanel.Visible = false;

            splashScreen = new SplashScreen();
            splashScreen.PlayClicked += OnSplashPlayClicked;
            Controls.Add(splashScreen);
            splashScreen.BringToFront();
        }

        private void OnSplashPlayClicked(object? sender, EventArgs e)
        {
            // Play start sound
            gameController.SoundManager.PlayGameStart();

            // Fade out splash screen
            var fadeTimer = new System.Windows.Forms.Timer { Interval = 16 };
            fadeTimer.Tick += (s, ev) =>
            {
                if (splashScreen != null && splashScreen.BackColor.A > 10)
                {
                    int alpha = Math.Max(0, splashScreen.BackColor.A - 15);
                    splashScreen.BackColor = Color.FromArgb(alpha, 0, 0, 0);
                }
                else
                {
                    fadeTimer.Stop();
                    fadeTimer.Dispose();
                    
                    if (splashScreen != null)
                    {
                        Controls.Remove(splashScreen);
                        splashScreen.Dispose();
                        splashScreen = null;
                    }

                    // Fade in controls after splash is gone
                    FadeInGameControls();
                }
            };
            fadeTimer.Start();
        }

        private void FadeInGameControls()
        {
            // First, show the controls (but they'll be covered by overlay)
            menuStrip.Visible = true;
            controlPanel.Visible = true;

            // Create a fade overlay panel
            fadeOverlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = themeManager.CurrentTheme.Background
            };
            Controls.Add(fadeOverlay);
            fadeOverlay.BringToFront();

            // Animate the overlay fading out
            var fadeInTimer = new System.Windows.Forms.Timer { Interval = 16 };
            int alpha = 255;

            fadeInTimer.Tick += (s, e) =>
            {
                alpha -= 15;
                
                if (alpha <= 0)
                {
                    fadeInTimer.Stop();
                    fadeInTimer.Dispose();
                    
                    // Remove the overlay completely
                    if (fadeOverlay != null)
                    {
                        Controls.Remove(fadeOverlay);
                        fadeOverlay.Dispose();
                        fadeOverlay = null;
                    }
                }
                else
                {
                    if (fadeOverlay != null)
                    {
                        fadeOverlay.BackColor = Color.FromArgb(alpha, themeManager.CurrentTheme.Background);
                    }
                }
            };

            fadeInTimer.Start();
        }

        private void CreateMenuBar()
        {
            menuStrip = new MenuStrip();
                
            CreateFileMenu();
            CreateEditMenu();
            CreateViewMenu();

            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;
        }

        private void CreateFileMenu()
        {
            var fileMenu = new ToolStripMenuItem("&File");

            fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Save Pattern...", null,
                (s, e) => dialogManager.ShowSaveDialog()) { ShortcutKeys = Keys.Control | Keys.S });

            var loadPatternMenuItem = new ToolStripMenuItem("&Load Pattern");
            LoadPatternMenuItems(loadPatternMenuItem);
            fileMenu.DropDownItems.Add(loadPatternMenuItem);

            fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Manage Patterns...", null,
                (s, e) => dialogManager.ShowManageDialog(UpdateDisplay)));

            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(new ToolStripMenuItem("E&xit", null,
                (s, e) => Application.Exit()) { ShortcutKeys = Keys.Alt | Keys.F4 });

            menuStrip.Items.Add(fileMenu);
        }

        private void CreateEditMenu()
        {
            var editMenu = new ToolStripMenuItem("&Edit");

            editMenu.DropDownItems.Add(new ToolStripMenuItem("&Clear Board", null,
                (s, e) => { gameController.Clear(); UpdateDisplay(); }) { ShortcutKeys = Keys.Control | Keys.N });

            editMenu.DropDownItems.Add(new ToolStripMenuItem("&Random Fill", null,
                (s, e) => { gameController.Randomize(); UpdateDisplay(); }) { ShortcutKeys = Keys.Control | Keys.R });

            editMenu.DropDownItems.Add(new ToolStripSeparator());

            var themeMenuItem = new ToolStripMenuItem("&Theme");
            foreach (var theme in ColorTheme.AllThemes)
            {
                themeMenuItem.DropDownItems.Add(new ToolStripMenuItem(theme.Name, null, (s, e) =>
                {
                    themeManager.CurrentTheme = theme;
                    ApplyTheme();
                }));
            }
            editMenu.DropDownItems.Add(themeMenuItem);

            var patternsMenuItem = new ToolStripMenuItem("&Built-in Patterns");
            foreach (var pattern in Pattern.AllPatterns)
            {
                patternsMenuItem.DropDownItems.Add(new ToolStripMenuItem(pattern.Name, null, (s, e) =>
                {
                    gameController.PlacePattern(pattern);
                    UpdateDisplay();
                }));
            }
            editMenu.DropDownItems.Add(patternsMenuItem);

            menuStrip.Items.Add(editMenu);
        }

        private void CreateViewMenu()
        {
            var viewMenu = new ToolStripMenuItem("&View");

            var toggle3DMenuItem = new ToolStripMenuItem("&3D Mode", null, (s, e) =>
            {
                use3DRendering = !use3DRendering;
                ((ToolStripMenuItem)s!).Checked = use3DRendering;
                
                // Update mode label
                modeLabel.Text = use3DRendering ? "Mode: 3D (View Only)" : "Mode: 2D (Editable)";
                modeLabel.ForeColor = use3DRendering ? Color.Orange : Color.LimeGreen;
                
                gamePanel.Invalidate();
            })
            {
                CheckOnClick = true,
                Checked = use3DRendering,
                ShortcutKeys = Keys.Control | Keys.D
            };

            viewMenu.DropDownItems.Add(toggle3DMenuItem);

            // Sound effects toggle
            var soundToggleMenuItem = new ToolStripMenuItem("&Sound Effects", null, (s, e) =>
            {
                gameController.SoundManager.SoundEnabled = !gameController.SoundManager.SoundEnabled;
                ((ToolStripMenuItem)s!).Checked = gameController.SoundManager.SoundEnabled;
            })
            {
                CheckOnClick = true,
                Checked = true,
                ShortcutKeys = Keys.Control | Keys.M
            };

            viewMenu.DropDownItems.Add(soundToggleMenuItem);
                        
            var musicToggleMenuItem = new ToolStripMenuItem("&Background Music", null, (s, e) =>
            {
                var item = (ToolStripMenuItem)s!;                
                
                if (item.Checked)
                {
                    gameController.SoundManager.PlayBackgroundMusic();
                }
                else
                {
                    gameController.SoundManager.StopBackgroundMusic();
                }
            })
            {
                CheckOnClick = true,
                Checked = false,
                ShortcutKeys = Keys.Control | Keys.B
            };

            viewMenu.DropDownItems.Add(musicToggleMenuItem);

            menuStrip.Items.Add(viewMenu);
        }

        private void LoadPatternMenuItems(ToolStripMenuItem parentItem)
        {
            parentItem.DropDownItems.Clear();

            if (patternManager.CustomPatterns.Count == 0)
            {
                parentItem.DropDownItems.Add(new ToolStripMenuItem("(No saved patterns)") { Enabled = false });
            }
            else
            {
                foreach (var pattern in patternManager.CustomPatterns)
                {
                    parentItem.DropDownItems.Add(new ToolStripMenuItem(pattern.Name, null, (s, e) =>
                    {
                        gameController.PlacePattern(pattern);
                        UpdateDisplay();
                    }));
                }
            }

            parentItem.DropDownItems.Add(new ToolStripSeparator());
            parentItem.DropDownItems.Add(new ToolStripMenuItem("Refresh List", null,
                (s, e) => LoadPatternMenuItems(parentItem)));
        }

        private void CreateGamePanel()
        {
            gamePanel = new GamePanel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(10)
            };
            gamePanel.Paint += OnPanelPaint;
            gamePanel.MouseDown += OnPanelMouseDown;
            gamePanel.MouseMove += OnPanelMouseMove;
            gamePanel.Resize += OnPanelResize;
            Controls.Add(gamePanel);
        }

        private void CreateControlPanel()
        {
            controlPanel = new Panel
            {
                Height = 60,
                Dock = DockStyle.Bottom,
                Padding = new Padding(10)
            };
            Controls.Add(controlPanel);
        }

        private void CreateControls()
        {
            startStopButton = new Button
            {
                Text = "Start",
                Location = new Point(10, 10),
                Size = new Size(80, 35)
            };
            startStopButton.Click += (s, e) =>
            {
                gameController.Toggle();
                startStopButton.Text = gameController.IsRunning ? "Stop" : "Start";
                UpdateButtonStates();
            };
            controlPanel.Controls.Add(startStopButton);

            clearButton = new Button
            {
                Text = "Clear",
                Location = new Point(100, 10),
                Size = new Size(80, 35)
            };
            clearButton.Click += (s, e) => { gameController.Clear(); UpdateDisplay(); };
            controlPanel.Controls.Add(clearButton);

            randomButton = new Button
            {
                Text = "Random",
                Location = new Point(190, 10),
                Size = new Size(80, 35)
            };
            randomButton.Click += (s, e) => { gameController.Randomize(); UpdateDisplay(); };
            controlPanel.Controls.Add(randomButton);

            speedLabel = new Label
            {
                Text = "Speed: Medium",
                Location = new Point(300, 15),
                Size = new Size(100, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };
            controlPanel.Controls.Add(speedLabel);

            speedTrackBar = new TrackBar
            {
                Location = new Point(400, 10),
                Size = new Size(150, 45),
                Minimum = 1,
                Maximum = 100,
                Value = 50,
                TickFrequency = 10
            };
            speedTrackBar.ValueChanged += (s, e) =>
            {
                gameController.SetSpeed(speedTrackBar.Value);
                speedLabel.Text = $"Speed: {GetSpeedText(speedTrackBar.Value)}";
            };
            controlPanel.Controls.Add(speedTrackBar);

            // Volume control
            var volumeLabel = new Label
            {
                Text = "🔊",
                Location = new Point(560, 15),
                Size = new Size(20, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };
            controlPanel.Controls.Add(volumeLabel);

            volumeTrackBar = new TrackBar
            {
                Location = new Point(580, 10),
                Size = new Size(100, 45),
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                TickFrequency = 20
            };
            volumeTrackBar.ValueChanged += (s, e) =>
            {
                gameController.SoundManager.Volume = volumeTrackBar.Value / 100f;
            };
            controlPanel.Controls.Add(volumeTrackBar);

            generationLabel = new Label
            {
                Text = "Generation: 0",
                Location = new Point(690, 15),
                AutoSize = true,
                Font = new Font(Font.FontFamily, 10, FontStyle.Bold)
            };
            controlPanel.Controls.Add(generationLabel);

            modeLabel = new Label
            {
                Text = "Mode: 3D (View Only)",
                Location = new Point(800, 15),
                AutoSize = true,
                ForeColor = Color.Orange,
                Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
            };
            controlPanel.Controls.Add(modeLabel);
        }

        private void ApplyTheme()
        {
            themeManager.ApplyToForm(this, menuStrip, controlPanel, gamePanel);
            gamePanel.Invalidate();
        }

        private void OnPanelResize(object? sender, EventArgs e)
        {
            int newRows = Math.Max(1, gamePanel.Height / CellSize);
            int newCols = Math.Max(1, gamePanel.Width / CellSize);
            gameController.ResizeBoard(newRows, newCols);
            gamePanel.Invalidate();
        }

        private void OnPanelPaint(object? sender, PaintEventArgs e)
        {
            if (use3DRendering)
            {
                renderer3D.RenderWithDepth(
                    e.Graphics,
                    gameController.Board,
                    themeManager.CurrentTheme,
                    CellSize,
                    gamePanel.ClientRectangle);
            }
            else
            {
                Render2D(e.Graphics);
            }
        }

        private void Render2D(Graphics g)
        {
            using (SolidBrush backgroundBrush = new(themeManager.CurrentTheme.Background))
            {
                g.FillRectangle(backgroundBrush, gamePanel.ClientRectangle);
            }

            using (SolidBrush cellBrush = new(themeManager.CurrentTheme.Cell))
            {
                for (int row = 0; row < gameController.Board.Rows; row++)
                {
                    for (int col = 0; col < gameController.Board.Columns; col++)
                    {
                        if (gameController.Board[row, col])
                        {
                            Rectangle rect = new(col * CellSize, row * CellSize, CellSize, CellSize);
                            g.FillRectangle(cellBrush, rect);
                        }
                    }
                }
            }

            using Pen gridPen = new(themeManager.CurrentTheme.Grid);
            for (int i = 0; i <= gameController.Board.Rows; i++)
            {
                int y = i * CellSize;
                g.DrawLine(gridPen, 0, y, gameController.Board.Columns * CellSize, y);
            }
            for (int j = 0; j <= gameController.Board.Columns; j++)
            {
                int x = j * CellSize;
                g.DrawLine(gridPen, x, 0, x, gameController.Board.Rows * CellSize);
            }
        }

        // Update these mouse event handlers:

        private void OnPanelMouseDown(object? sender, MouseEventArgs e)
        {
            // Disable editing in 3D mode
            if (use3DRendering)
            {
                // Show tooltip or status message
                MessageBox.Show(
                    "Cell editing is disabled in 3D mode.\n\nPress Ctrl+D to switch to 2D mode for editing.",
                    "3D Mode Active",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (e.Button == MouseButtons.Left && TryGetCellCoordinates(e.Location, out int row, out int col))
            {
                gameController.Board[row, col] = !gameController.Board[row, col];
                gamePanel.Invalidate();
            }
        }

        private void OnPanelMouseMove(object? sender, MouseEventArgs e)
        {
            // Disable editing in 3D mode
            if (use3DRendering)
            {
                return;
            }

            if (e.Button == MouseButtons.Left && TryGetCellCoordinates(e.Location, out int row, out int col))
            {
                gameController.Board[row, col] = true;
                gamePanel.Invalidate();
            }
        }

        private bool TryGetCellCoordinates(Point location, out int row, out int col)
        {
            col = location.X / CellSize;
            row = location.Y / CellSize;
            return row >= 0 && row < gameController.Board.Rows && col >= 0 && col < gameController.Board.Columns;
        }

        private void OnGameTick()
        {
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            generationLabel.Text = $"Generation: {gameController.Generation}";
            gamePanel.Invalidate();
        }

        private void UpdateButtonStates()
        {
            clearButton.Enabled = !gameController.IsRunning;
            randomButton.Enabled = !gameController.IsRunning;
        }

        private string GetSpeedText(int value) => value switch
        {
            <= 20 => "Very Slow",
            <= 40 => "Slow",
            <= 60 => "Medium",
            <= 80 => "Fast",
            _ => "Very Fast"
        };

        private void Form1_Load(object sender, EventArgs e)
        {
            // Auto-start background music
            gameController.SoundManager.PlayBackgroundMusic();
            
            // Show splash screen
            ShowSplashScreen();
        }
    }
}
