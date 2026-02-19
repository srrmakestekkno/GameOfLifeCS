using System;
using System.Collections.Generic;
using System.Text;

namespace GameOfLifeCS
{
    public class PatternDialogManager
    {
        private readonly PatternManager patternManager;
        private readonly ThemeManager themeManager;
        private readonly GameController gameController;

        public PatternDialogManager(PatternManager patternManager, ThemeManager themeManager, GameController gameController)
        {
            this.patternManager = patternManager;
            this.themeManager = themeManager;
            this.gameController = gameController;
        }

        public void ShowSaveDialog()
        {
            using var dialog = CreateThemedDialog("Save Pattern", new Size(350, 150));

            var label = new Label
            {
                Text = "Pattern Name:",
                Location = new Point(10, 20),
                AutoSize = true
            };
            dialog.Controls.Add(label);

            var textBox = new TextBox
            {
                Location = new Point(10, 45),
                Size = new Size(310, 25)
            };
            dialog.Controls.Add(textBox);

            var okButton = CreateThemedButton("Save", new Point(160, 75), DialogResult.OK);
            var cancelButton = CreateThemedButton("Cancel", new Point(245, 75), DialogResult.Cancel);

            dialog.Controls.Add(okButton);
            dialog.Controls.Add(cancelButton);
            dialog.AcceptButton = okButton;
            dialog.CancelButton = cancelButton;

            themeManager.ApplyToDialog(dialog);

            if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                string patternName = textBox.Text.Trim();
                
                // Check if pattern already exists
                bool exists = patternManager.CustomPatterns.Any(p => 
                    string.Equals(p.Name, patternName, StringComparison.OrdinalIgnoreCase));

                if (exists)
                {
                    var result = MessageBox.Show(
                        $"A pattern named '{patternName}' already exists.\n\nDo you want to overwrite it?",
                        "Overwrite Pattern",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result != DialogResult.Yes)
                    {
                        return; // User chose not to overwrite
                    }

                    // Delete the existing pattern before saving the new one
                    var existingPattern = patternManager.CustomPatterns
                        .First(p => string.Equals(p.Name, patternName, StringComparison.OrdinalIgnoreCase));
                    patternManager.DeletePattern(existingPattern);
                }

                try
                {
                    patternManager.SavePattern(patternName, gameController.Board);
                    MessageBox.Show($"Pattern '{patternName}' saved successfully!", "Success",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving pattern: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public void ShowManageDialog(Action onPatternLoaded)
        {
            using var dialog = CreateThemedDialog("Manage Patterns", new Size(400, 350));

            var listBox = new ListBox
            {
                Location = new Point(10, 10),
                Size = new Size(360, 240),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            foreach (var pattern in patternManager.CustomPatterns)
            {
                listBox.Items.Add(pattern.Name);
            }
            dialog.Controls.Add(listBox);

            var loadButton = CreateThemedButton("Load", new Point(10, 260));
            loadButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            loadButton.Click += (s, e) =>
            {
                if (listBox.SelectedIndex >= 0)
                {
                    var pattern = patternManager.CustomPatterns[listBox.SelectedIndex];
                    gameController.PlacePattern(pattern);
                    onPatternLoaded?.Invoke();
                    dialog.Close();
                }
            };
            dialog.Controls.Add(loadButton);

            var deleteButton = CreateThemedButton("Delete", new Point(100, 260));
            deleteButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            deleteButton.Click += (s, e) =>
            {
                if (listBox.SelectedIndex >= 0)
                {
                    var result = MessageBox.Show(
                        $"Delete pattern '{listBox.SelectedItem}'?",
                        "Confirm Delete",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        var pattern = patternManager.CustomPatterns[listBox.SelectedIndex];
                        patternManager.DeletePattern(pattern);
                        listBox.Items.RemoveAt(listBox.SelectedIndex);
                    }
                }
            };
            dialog.Controls.Add(deleteButton);

            var closeButton = CreateThemedButton("Close", new Point(290, 260));
            closeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            closeButton.Click += (s, e) => dialog.Close();
            dialog.Controls.Add(closeButton);

            themeManager.ApplyToDialog(dialog);
            dialog.ShowDialog();
        }

        private Form CreateThemedDialog(string title, Size size)
        {
            return new Form
            {
                Text = title,
                Size = size,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
        }

        private Button CreateThemedButton(string text, Point location, DialogResult? dialogResult = null)
        {
            var button = new Button
            {
                Text = text,
                Location = location,
                Size = new Size(75, 30)
            };

            if (dialogResult.HasValue)
            {
                button.DialogResult = dialogResult.Value;
            }

            return button;
        }
    }
}
