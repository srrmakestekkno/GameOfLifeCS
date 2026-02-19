using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace GameOfLifeCS
{
    public class PatternManager
    {
        private const string PatternsDirectory = "SavedPatterns";
        private readonly List<Pattern> customPatterns = new();

        public IReadOnlyList<Pattern> CustomPatterns => customPatterns.AsReadOnly();

        public PatternManager()
        {
            EnsureDirectoryExists();
            LoadAllPatterns();
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(PatternsDirectory))
            {
                Directory.CreateDirectory(PatternsDirectory);
            }
        }

        public void SavePattern(string name, Board board)
        {
            // Find the bounds of living cells
            int minRow = board.Rows, maxRow = -1;
            int minCol = board.Columns, maxCol = -1;

            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)
                {
                    if (board[row, col])
                    {
                        minRow = Math.Min(minRow, row);
                        maxRow = Math.Max(maxRow, row);
                        minCol = Math.Min(minCol, col);
                        maxCol = Math.Max(maxCol, col);
                    }
                }
            }

            // If no living cells, save empty pattern
            if (maxRow == -1)
            {
                int[,] emptyCells = new int[1, 1];
                Pattern emptyPattern = new(name, emptyCells);
                SavePatternToFile(emptyPattern);
                customPatterns.Add(emptyPattern);
                return;
            }

            // Create pattern array with only the living cells region
            int height = maxRow - minRow + 1;
            int width = maxCol - minCol + 1;
            int[,] cells = new int[height, width];

            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    cells[row, col] = board[minRow + row, minCol + col] ? 1 : 0;
                }
            }

            Pattern pattern = new(name, cells);
            SavePatternToFile(pattern);
            customPatterns.Add(pattern);
        }

        private void SavePatternToFile(Pattern pattern)
        {
            string fileName = GetSafeFileName(pattern.Name) + ".json";
            string filePath = Path.Combine(PatternsDirectory, fileName);

            var data = new PatternData
            {
                Name = pattern.Name,
                Width = pattern.Width,
                Height = pattern.Height,
                Cells = ConvertToList(pattern.Cells)
            };

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public void DeletePattern(Pattern pattern)
        {
            customPatterns.Remove(pattern);
            string fileName = GetSafeFileName(pattern.Name) + ".json";
            string filePath = Path.Combine(PatternsDirectory, fileName);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        private void LoadAllPatterns()
        {
            customPatterns.Clear();
            
            if (!Directory.Exists(PatternsDirectory))
                return;

            foreach (string file in Directory.GetFiles(PatternsDirectory, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var data = JsonSerializer.Deserialize<PatternData>(json);
                    
                    if (data != null)
                    {
                        int[,] cells = ConvertToArray(data.Cells, data.Height, data.Width);
                        customPatterns.Add(new Pattern(data.Name, cells));
                    }
                }
                catch
                {
                    // Skip invalid files
                }
            }
        }

        private static string GetSafeFileName(string name)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", name.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        }

        private static List<List<int>> ConvertToList(int[,] array)
        {
            var list = new List<List<int>>();
            for (int i = 0; i < array.GetLength(0); i++)
            {
                var row = new List<int>();
                for (int j = 0; j < array.GetLength(1); j++)
                {
                    row.Add(array[i, j]);
                }
                list.Add(row);
            }
            return list;
        }

        private static int[,] ConvertToArray(List<List<int>> list, int height, int width)
        {
            int[,] array = new int[height, width];
            for (int i = 0; i < height && i < list.Count; i++)
            {
                for (int j = 0; j < width && j < list[i].Count; j++)
                {
                    array[i, j] = list[i][j];
                }
            }
            return array;
        }

        private class PatternData
        {
            public string Name { get; set; } = "";
            public int Width { get; set; }
            public int Height { get; set; }
            public List<List<int>> Cells { get; set; } = new();
        }
    }
}
