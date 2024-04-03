using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace wpf_workspace
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
        private Random random = new Random();
        private enum Directions { Up, Right, Down, Left }

        private const int squareSize = 30;

        private Brush snakeColor = Brushes.DarkOliveGreen;
        private List<Point> snakeBody = new List<Point>();
        private Directions snakeDirection = Directions.Right;

        private Brush foodColor = Brushes.Red;
        private Point snakeFood;

        private Brush wallColor = Brushes.Black;
        private List<Point> walls = new List<Point>();

        private int _playerScore;
        private int PlayerScore
        {
            get { return _playerScore; }
            set 
            {
                _playerScore = value;
                CurrentScore.Content = "Счёт: " + _playerScore;
            }
        }

        private bool gamePaused = true;
        private bool gameRunning = false;
        private bool directionChangeDebounce = false;

        private SnakeLeaderboardDBEntities snakeDB = SnakeLeaderboardDBEntities.Instance;
        private LeaderboardWindow leaderboardWindow;

        public MainWindow()
        {
            InitializeComponent();
            
            KeyDown += new KeyEventHandler(SnakeMovementHandler);

            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = TimeSpan.FromSeconds(0.3);
        }

        private void SnakeMovementHandler(object sender, KeyEventArgs e)
        {
            if (timer.IsEnabled == false || directionChangeDebounce) return;

            switch (e.Key)
            {
                case Key.W:
                    if (snakeDirection != Directions.Down) { snakeDirection = Directions.Up; }
                    break;

                case Key.A:
                    if (snakeDirection != Directions.Right) { snakeDirection = Directions.Left; }
                    break;

                case Key.S:
                    if (snakeDirection != Directions.Up) { snakeDirection = Directions.Down; }
                    break;

                case Key.D:
                    if (snakeDirection != Directions.Left) { snakeDirection = Directions.Right; }
                    break;
            }
            directionChangeDebounce = true;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            directionChangeDebounce = false;
            Point snakeTail = snakeBody.First();

            MoveSnake();

            if (!CheckCollisions())
            {
                GameField.Children.Clear();

                if (snakeBody.Last() == snakeFood) 
                {
                    PlayerScore++;
                    snakeBody.Insert(0, snakeTail);
                    SpawnFood();
                }

                PaintSnake();
                PaintWalls();
                PaintFood();
            }
        }

        private void FlipPauseState()
        {
            if (gamePaused && gameRunning) 
            { 
                timer.Start();
                GamePauseSwitch.Content = "❙❙";

                NewWallX.IsEnabled = NewWallXLabel.IsEnabled = NewWallY.IsEnabled = NewWallYLabel.IsEnabled = false;
            } 
            else
            {
                timer.Stop();
                GamePauseSwitch.Content = "⏵";

                NewWallX.IsEnabled = NewWallXLabel.IsEnabled = NewWallY.IsEnabled = NewWallYLabel.IsEnabled = true;
            }

            gamePaused = !gamePaused;
        }


        private void StartNewGame()
        {
            GameField.Children.Clear();

            snakeBody.Clear();
            walls.Clear();

            snakeBody.Add(new Point { X = squareSize, Y = squareSize * 7 });
            snakeBody.Add(new Point { X = squareSize * 2, Y = squareSize * 7 });
            snakeBody.Add(new Point { X = squareSize * 3, Y = squareSize * 7 });

            snakeDirection = Directions.Right;

            SpawnFood();

            PaintSnake();
            PaintFood();

            Leaderboard playerEntry = snakeDB.Leaderboard.Find(PlayerName.Text);
            if (playerEntry == null)
            {
                PersonalBestScore.Content = $"Лучший счёт игрока {PlayerName.Text}: Отсутствует";
            }
            else
            {
                PersonalBestScore.Content = $"Лучший счёт игрока {playerEntry.PlayerName}: {playerEntry.PlayerScore}";
            }
            PlayerScore = 0;

            NewWallX.IsEnabled = NewWallXLabel.IsEnabled = NewWallY.IsEnabled = NewWallYLabel.IsEnabled = true;
            gamePaused = true;
            GamePauseSwitch.IsEnabled = true;
        }


        private void EndCurrentGame()
        {
            FlipPauseState();
            GamePauseSwitch.IsEnabled = false;

            NewWallX.IsEnabled = NewWallXLabel.IsEnabled = NewWallY.IsEnabled = NewWallYLabel.IsEnabled = CreateNewWall.IsEnabled = false;

            Leaderboard playerEntry = snakeDB.Leaderboard.Find(PlayerName.Text);
            if (playerEntry == null)
            {
                snakeDB.Leaderboard.Add(new Leaderboard { PlayerName = PlayerName.Text, PlayerScore = PlayerScore });
                snakeDB.SaveChanges();
                PersonalBestScore.Content = $"Лучший счёт игрока {PlayerName.Text}: {PlayerScore}";
            }
            else if (playerEntry.PlayerScore < PlayerScore)
            {
                playerEntry.PlayerScore = PlayerScore;
                snakeDB.SaveChanges();
                PersonalBestScore.Content = $"Лучший счёт игрока {playerEntry.PlayerName}: {PlayerScore}";
            }
        }

        private void PaintSnake()
        {
            foreach (Point snakeSegment in snakeBody)
            {
                Rectangle segmentRectangle = new Rectangle() { Fill = snakeColor, Width = squareSize, Height = squareSize };

                Canvas.SetLeft(segmentRectangle, snakeSegment.X);
                Canvas.SetTop(segmentRectangle, snakeSegment.Y);

                GameField.Children.Add(segmentRectangle);
            }
        }

        private void MoveSnake()
        {
            Point snakeHead = snakeBody.Last();
            switch (snakeDirection)
            {
                case Directions.Up:
                    snakeBody.Add(new Point { X = snakeHead.X, Y = snakeHead.Y - squareSize });
                    break;

                case Directions.Right:
                    snakeBody.Add(new Point { X = snakeHead.X + squareSize, Y = snakeHead.Y });
                    break;

                case Directions.Down:
                    snakeBody.Add(new Point { X = snakeHead.X, Y = snakeHead.Y + squareSize });
                    break;

                case Directions.Left:
                    snakeBody.Add(new Point { X = snakeHead.X - squareSize, Y = snakeHead.Y });
                    break;
            }
            snakeBody.Remove(snakeBody.First());
        }

        private bool CheckCollisions()
        {
            Point snakeHead = snakeBody.Last();
            if (snakeBody.Count != snakeBody.Distinct().Count() ||
                (snakeHead.X < 0 || snakeHead.X > GameField.Width - squareSize) || (snakeHead.Y < 0 || snakeHead.Y > GameField.Height - squareSize)
                || walls.Any(i => i == snakeHead))
            {
                FlipGameState();
                return true;
            }
            
            return false;
        }

        private void SpawnFood()
        {
            Point foodSquare = new Point { X = random.Next((int)GameField.Width / squareSize) * squareSize, 
                                           Y = random.Next((int)GameField.Height / squareSize) * squareSize };

            if (snakeFood == foodSquare || snakeBody.Any(i => i == foodSquare) || walls.Any(i => i == foodSquare)) { SpawnFood(); }
            else { snakeFood = foodSquare; }
        }

        private void PaintFood()
        {
            Rectangle foodRectangle = new Rectangle() { Fill = foodColor, Width = squareSize / 2, Height = squareSize / 2 };

            Canvas.SetLeft(foodRectangle, snakeFood.X + squareSize / 4);
            Canvas.SetTop(foodRectangle, snakeFood.Y + squareSize / 4);

            GameField.Children.Add(foodRectangle);
        }

        private void GamePauseSwitch_Click(object sender, RoutedEventArgs e)
        {
            FlipPauseState();
        }

        private void NumericXCordValidation(object sender, TextCompositionEventArgs e)
        {
            e.Handled = ("0123456789".IndexOf(e.Text) < 0) || NewWallX.Text.StartsWith("0");
        }

        private void NumericYCordValidation(object sender, TextCompositionEventArgs e)
        {
            e.Handled = ("0123456789".IndexOf(e.Text) < 0) || NewWallY.Text.StartsWith("0");
        }

        private void GameStateSwitch_Click(object sender, RoutedEventArgs e)
        {
            FlipGameState();
        }

        private void PlayerName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PlayerName.Text) || PlayerName.Text.StartsWith(" "))
            {
                GameStateSwitch.IsEnabled = false;
            }
            else { GameStateSwitch.IsEnabled = true; }
        }

        private void FlipGameState()
        {
            gameRunning = !gameRunning;
            if (!gameRunning)
            {
                EndCurrentGame();
                PlayerName.IsEnabled = true;
                GameStateSwitch.Content = "Начать новую игру";
            }
            else
            {
                StartNewGame();
                PlayerName.IsEnabled = false;
                GameStateSwitch.Content = "Закончить текущую игру";
            }
        }

        private void CreateNewWall_Click(object sender, RoutedEventArgs e)
        {
            Point newWall = new Point { X = (double.Parse(NewWallX.Text) - 1) * squareSize, Y = (double.Parse(NewWallY.Text) - 1) * squareSize};
            walls.Add(newWall);
            PaintWall(newWall);

            CreateNewWall.IsEnabled = false;
        }

        private void PaintWall(Point wall)
        {
            Rectangle wallRectangle = new Rectangle() { Fill = wallColor, Width = squareSize, Height = squareSize };

            Canvas.SetLeft(wallRectangle, wall.X);
            Canvas.SetTop(wallRectangle, wall.Y);

            GameField.Children.Add(wallRectangle);
        }

        private void PaintWalls()
        {
            foreach (Point wall in walls)
            {
                PaintWall(wall);
            }
        }

        private void NewWallCords_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (gamePaused && gameRunning && !string.IsNullOrEmpty(NewWallX.Text) && !string.IsNullOrEmpty(NewWallY.Text) && IsPossibleWall())
            {
                CreateNewWall.IsEnabled = true;
            }
            else
            {
                CreateNewWall.IsEnabled = false;
            }
        }

        private bool IsPossibleWall()
        {
            double newWallX = double.Parse(NewWallX.Text);
            double newWallY = double.Parse(NewWallY.Text);
            Point newWall = new Point { X = (newWallX - 1) * squareSize, Y = (newWallY - 1) * squareSize };

            if (newWallX > 0 && newWallY > 0 && newWallX <= GameField.Width && newWallY <= GameField.Height
                && !walls.Any(i => i == newWall) && !snakeBody.Any(i => i == newWall) && snakeFood != newWall) { return true; } else { return false; }
        }

        private void PlayerName_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = PlayerName.Text.Length > 24;
        }

        private void BestPlayerScoresButton_Click(object sender, RoutedEventArgs e)
        {
            BestPlayerScoresButton.IsEnabled = false;

            leaderboardWindow = new LeaderboardWindow(snakeDB);
            leaderboardWindow.Show();

            leaderboardWindow.Closed += LeaderboardWindow_Closed;
        }

        private void LeaderboardWindow_Closed(object sender, EventArgs e)
        {
            BestPlayerScoresButton.IsEnabled = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (leaderboardWindow != null)
            {
                leaderboardWindow.Close();
            }
        }
    }
}