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
using System.Windows.Shapes;

namespace wpf_workspace
{
    /// <summary>
    /// Логика взаимодействия для LeaderboardWindow.xaml
    /// </summary>
    public partial class LeaderboardWindow : Window
    {
        public LeaderboardWindow(SnakeLeaderboardDBEntities snakeDB)
        {
            InitializeComponent();
            PlayerLeaderboard.ItemsSource = snakeDB.Leaderboard.OrderByDescending(entry => entry.PlayerScore).Take(10).ToList();
        }
    }
}
