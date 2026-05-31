using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MemoriGame.Services;

namespace MemoriGame.Views
{
    public partial class LeaderboardWindow : Window
    {
        private ScoreService _scoreService;

        public LeaderboardWindow()
        {
            InitializeComponent();
            _scoreService = new ScoreService();
            LoadLeaderboard("Medium");
            HighlightButton(MediumBtn);
        }

        private void LoadLeaderboard(string difficulty)
        {
            var players = _scoreService.GetTopPlayers(difficulty);
            var displayList = new ObservableCollection<LeaderboardItem>();

            foreach (var player in players)
            {
                displayList.Add(new LeaderboardItem
                {
                    Rank = player.Rank,
                    PlayerName = player.PlayerName,
                    Moves = player.Moves,
                    Time = string.Format("{0:D2}:{1:D2}", player.TimeInSeconds / 60, player.TimeInSeconds % 60),
                    Date = player.Date.ToString("dd.MM.yyyy HH:mm")
                });
            }

            LeaderboardList.ItemsSource = displayList;
        }

        private void EasyBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadLeaderboard("Easy");
            HighlightButton(EasyBtn);
        }

        private void MediumBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadLeaderboard("Medium");
            HighlightButton(MediumBtn);
        }

        private void HardBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadLeaderboard("Hard");
            HighlightButton(HardBtn);
        }

        private void HighlightButton(Button activeButton)
        {
            Button[] buttons = { EasyBtn, MediumBtn, HardBtn };
            foreach (Button btn in buttons)
            {
                btn.Background = btn == activeButton ?
                    new SolidColorBrush(Colors.Orange) :
                    new SolidColorBrush(Color.FromRgb(52, 152, 219));
            }
        }
    }

    public class LeaderboardItem
    {
        public int Rank { get; set; }
        public string PlayerName { get; set; }
        public int Moves { get; set; }
        public string Time { get; set; }
        public string Date { get; set; }
    }
}