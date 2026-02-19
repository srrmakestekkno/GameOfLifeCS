using System;
using System.Collections.Generic;
using System.Text;

namespace GameOfLifeCS
{
    public class GameController
    {
        private const int DefaultTimerInterval = 100;
        private const int MinTimerInterval = 10;
        private const int MaxTimerInterval = 1000;

        public Board Board { get; private set; }
        public int Generation => Board.Generation;
        public bool IsRunning { get; private set; }
        public SoundManager SoundManager { get; }

        private readonly System.Windows.Forms.Timer gameTimer;
        private readonly Action onTick;
        private bool[,]? previousState;

        public GameController(Board board, Action onTick)
        {
            Board = board;
            this.onTick = onTick;
            SoundManager = new SoundManager();
            gameTimer = new System.Windows.Forms.Timer { Interval = DefaultTimerInterval };
            gameTimer.Tick += (s, e) => Tick();
        }

        public void Start()
        {
            IsRunning = true;
            gameTimer.Start();
            SoundManager.PlayGameStart();
            CopyBoardState(); // Initialize previous state
        }

        public void Stop()
        {
            IsRunning = false;
            gameTimer.Stop();
            SoundManager.PlayGameStop();
        }

        public void Toggle()
        {
            if (IsRunning) Stop();
            else Start();
        }

        public void Clear()
        {
            Board.Clear();
            SoundManager.PlayClear();
        }

        public void Randomize()
        {
            Board.Randomize();
            SoundManager.PlayButtonClick();
        }

        public void SetSpeed(int value)
        {
            int newInterval;
            
            if (value <= 50)
            {
                newInterval = MaxTimerInterval - ((value - 1) * (MaxTimerInterval - DefaultTimerInterval) / 49);
            }
            else
            {
                newInterval = DefaultTimerInterval - ((value - 50) * (DefaultTimerInterval - MinTimerInterval) / 50);
            }
            
            gameTimer.Interval = newInterval;
        }

        public void PlacePattern(Pattern pattern)
        {
            Board.Clear();

            int startRow = (Board.Rows - pattern.Height) / 2;
            int startCol = (Board.Columns - pattern.Width) / 2;

            for (int row = 0; row < pattern.Height; row++)
            {
                for (int col = 0; col < pattern.Width; col++)
                {
                    if (startRow + row < Board.Rows && startCol + col < Board.Columns)
                    {
                        Board[startRow + row, startCol + col] = pattern.Cells[row, col] == 1;
                    }
                }
            }

            SoundManager.PlayPatternLoad();
        }

        public void ResizeBoard(int newRows, int newCols)
        {
            if (newRows != Board.Rows || newCols != Board.Columns)
            {
                Board newBoard = new(newRows, newCols);
                int copyRows = Math.Min(Board.Rows, newRows);
                int copyCols = Math.Min(Board.Columns, newCols);

                for (int row = 0; row < copyRows; row++)
                {
                    for (int col = 0; col < copyCols; col++)
                    {
                        newBoard[row, col] = Board[row, col];
                    }
                }

                newBoard.Generation = Board.Generation;
                Board = newBoard;
                previousState = null; // Reset state tracking
            }
        }

        private void Tick()
        {
            // Detect changes for sound effects
            if (SoundManager.SoundEnabled && previousState != null)
            {
                DetectChangesAndPlaySounds();
            }

            Board.NextGeneration();
            CopyBoardState();
            onTick?.Invoke();
        }

        private void DetectChangesAndPlaySounds()
        {
            if (previousState == null) return;

            int births = 0;
            int deaths = 0;

            // Count births and deaths
            for (int row = 0; row < Board.Rows && row < previousState.GetLength(0); row++)
            {
                for (int col = 0; col < Board.Columns && col < previousState.GetLength(1); col++)
                {
                    bool wasAlive = previousState[row, col];
                    bool isAlive = Board[row, col];

                    if (!wasAlive && isAlive) births++;
                    if (wasAlive && !isAlive) deaths++;
                }
            }

            // Play sounds based on changes (limit to prevent audio overload)
            if (births > 0 && births < 100)
            {
                for (int i = 0; i < Math.Min(births, 5); i++)
                {
                    SoundManager.PlayCellBirth();
                }
            }

            if (deaths > 0 && deaths < 100)
            {
                for (int i = 0; i < Math.Min(deaths, 3); i++)
                {
                    SoundManager.PlayCellDeath();
                }
            }
        }

        private void CopyBoardState()
        {
            if (previousState == null || 
                previousState.GetLength(0) != Board.Rows || 
                previousState.GetLength(1) != Board.Columns)
            {
                previousState = new bool[Board.Rows, Board.Columns];
            }

            for (int row = 0; row < Board.Rows; row++)
            {
                for (int col = 0; col < Board.Columns; col++)
                {
                    previousState[row, col] = Board[row, col];
                }
            }
        }

        public void Dispose()
        {
            SoundManager?.Dispose();
        }
    }
}
