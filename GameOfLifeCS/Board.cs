using System;
using System.Collections.Generic;
using System.Text;

namespace GameOfLifeCS
{
    public class Board
    {
        private bool[,] cells;
        private bool[,] nextCells;
        
        public int Rows { get; }
        public int Columns { get; }
        public int Generation { get; set; }

        public Board(int rows, int columns)
        {
            Rows = rows;
            Columns = columns;
            cells = new bool[rows, columns];
            nextCells = new bool[rows, columns];
            Generation = 0;
        }

        public bool this[int row, int col]
        {
            get => cells[row, col];
            set => cells[row, col] = value;
        }

        public void Clear()
        {
            Array.Clear(cells);
            Generation = 0;
        }

        public void Randomize(double aliveProbability = 0.25)
        {
            Random random = new();
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    cells[row, col] = random.NextDouble() < aliveProbability;
                }
            }
            Generation = 0;
        }

        public void NextGeneration()
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    int neighbors = CountLiveNeighbors(row, col);
                    nextCells[row, col] = cells[row, col] 
                        ? neighbors is 2 or 3  // Survival
                        : neighbors == 3;      // Birth
                }
            }

            (cells, nextCells) = (nextCells, cells);
            Generation++;
        }

        private int CountLiveNeighbors(int row, int col)
        {
            int count = 0;
            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;

                    int newRow = row + dr;
                    int newCol = col + dc;

                    if (IsValidCell(newRow, newCol) && cells[newRow, newCol])
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        private bool IsValidCell(int row, int col) => 
            row >= 0 && row < Rows && col >= 0 && col < Columns;
    }
}
