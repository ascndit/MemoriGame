using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MemoriGame.Views
{
    public partial class MainWindow : Window
    {
        public class GameCard
        {
            public int Id { get; set; }
            public int PairId { get; set; }
            public bool IsOpened { get; set; }
            public bool IsMatched { get; set; }
            public Button Button { get; set; }
            public ImageSource CardImage { get; set; }
        }

        private List<GameCard> _cards = new List<GameCard>();
        private GameCard _firstOpenedCard = null;
        private GameCard _secondOpenedCard = null;
        private bool _isWaiting = false;
        private DispatcherTimer _gameTimer;
        private int _secondsElapsed = 0;
        private int _moves = 0;
        private int _pairsFound = 0;
        private int _totalPairs = 0;
        private string _playerName = "";
        private string _currentDifficulty = "Medium";
        private Random _random = new Random();

        private ImageSource _cardImageSource;
        private MediaPlayer _backgroundMusic = new MediaPlayer();
        private bool _isMusicOn = true;

        private List<GameRecord> _records = new List<GameRecord>();
        private string _recordsFile = "records.json";

        public class GameRecord
        {
            public string PlayerName { get; set; }
            public string Difficulty { get; set; }
            public int Moves { get; set; }
            public int TimeSeconds { get; set; }
            public DateTime Date { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();
            LoadImages();
            LoadRecords();
            UpdateLeaderboardDisplay();
            LoadMusic();

            if (DifficultyCombo != null)
            {
                DifficultyCombo.SelectionChanged += DifficultyCombo_SelectionChanged;
            }
        }

        private void LoadMusic()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string musicPath = Path.Combine(baseDir, "music.mp3");

                if (File.Exists(musicPath))
                {
                    _backgroundMusic.Open(new Uri(musicPath));
                    _backgroundMusic.MediaEnded += (s, e) =>
                    {
                        _backgroundMusic.Position = TimeSpan.Zero;
                        if (_isMusicOn) _backgroundMusic.Play();
                    };
                    _backgroundMusic.Volume = 0.2;
                    _backgroundMusic.Play();
                    System.Diagnostics.Debug.WriteLine("Музыка загружена");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Музыка не найдена: " + musicPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Ошибка загрузки музыки: " + ex.Message);
            }
        }

        private void MusicToggleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isMusicOn)
            {
                _backgroundMusic.Pause();
                _isMusicOn = false;
                MusicToggleBtn.Content = "🔇";
            }
            else
            {
                _backgroundMusic.Play();
                _isMusicOn = true;
                MusicToggleBtn.Content = "🔊";
            }
        }

        private void StopMusic()
        {
            _backgroundMusic.Stop();
        }

        private void LoadImages()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                string photoPath = Path.Combine(baseDir, "photo.jpg");
                if (File.Exists(photoPath))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(photoPath);
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    _cardImageSource = bitmap;
                    System.Diagnostics.Debug.WriteLine("Фото загружено: " + photoPath);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Фото не найдено: " + photoPath);
                    _cardImageSource = null;
                }

                string bgPath = Path.Combine(baseDir, "background.jpg");
                if (File.Exists(bgPath))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(bgPath);
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    ImageBrush backgroundBrush = new ImageBrush(bitmap);
                    backgroundBrush.Stretch = Stretch.UniformToFill;
                    this.Background = backgroundBrush;
                }
                else
                {
                    this.Background = new SolidColorBrush(Color.FromRgb(26, 26, 46));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Ошибка загрузки: " + ex.Message);
                this.Background = new SolidColorBrush(Color.FromRgb(26, 26, 46));
            }
        }

        private void LoadRecords()
        {
            try
            {
                if (File.Exists(_recordsFile))
                {
                    string json = File.ReadAllText(_recordsFile);
                    var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                    _records = serializer.Deserialize<List<GameRecord>>(json);
                }
            }
            catch { _records = new List<GameRecord>(); }
            if (_records == null) _records = new List<GameRecord>();
        }

        private void SaveRecords()
        {
            try
            {
                var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                string json = serializer.Serialize(_records);
                File.WriteAllText(_recordsFile, json);
            }
            catch { }
        }

        private void UpdateLeaderboardDisplay()
        {
            try
            {
                var leaderboardPanel = FindName("LeaderboardPanel") as StackPanel;
                if (leaderboardPanel == null) return;

                leaderboardPanel.Dispatcher.Invoke(() =>
                {
                    leaderboardPanel.Children.Clear();

                    var topRecords = _records
                        .Where(r => r.Difficulty == _currentDifficulty)
                        .OrderBy(r => r.Moves)
                        .ThenBy(r => r.TimeSeconds)
                        .Take(10)
                        .ToList();

                    if (topRecords.Count == 0)
                    {
                        TextBlock emptyText = new TextBlock();
                        emptyText.Text = "📋 Нет рекордов\nНачните игру!";
                        emptyText.Foreground = Brushes.White;
                        emptyText.FontSize = 12;
                        emptyText.TextAlignment = TextAlignment.Center;
                        emptyText.Margin = new Thickness(5);
                        leaderboardPanel.Children.Add(emptyText);
                        return;
                    }

                    for (int i = 0; i < topRecords.Count; i++)
                    {
                        var record = topRecords[i];

                        Border recordBorder = new Border();
                        recordBorder.Background = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
                        recordBorder.CornerRadius = new CornerRadius(8);
                        recordBorder.Margin = new Thickness(3, 2, 3, 2);
                        recordBorder.Padding = new Thickness(8, 6, 8, 6);

                        Grid recordGrid = new Grid();
                        recordGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        recordGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        recordGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                        TextBlock numberBlock = new TextBlock();
                        numberBlock.Text = $"{i + 1}.";
                        numberBlock.FontSize = 13;
                        numberBlock.FontWeight = FontWeights.Bold;
                        numberBlock.Foreground = Brushes.White;
                        numberBlock.Width = 30;
                        numberBlock.VerticalAlignment = VerticalAlignment.Center;
                        Grid.SetColumn(numberBlock, 0);
                        recordGrid.Children.Add(numberBlock);

                        StackPanel infoPanel = new StackPanel();
                        TextBlock nameBlock = new TextBlock();
                        nameBlock.Text = record.PlayerName.Length > 12 ? record.PlayerName.Substring(0, 10) + ".." : record.PlayerName;
                        nameBlock.FontSize = 12;
                        nameBlock.FontWeight = FontWeights.Bold;
                        nameBlock.Foreground = Brushes.White;
                        nameBlock.Margin = new Thickness(0, 0, 0, 2);
                        infoPanel.Children.Add(nameBlock);

                        TextBlock statsBlock = new TextBlock();
                        string timeStr = $"{record.TimeSeconds / 60}:{record.TimeSeconds % 60:D2}";
                        statsBlock.Text = $"🎯 {record.Moves} ходов  ⏱️ {timeStr}";
                        statsBlock.FontSize = 10;
                        statsBlock.Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180));
                        infoPanel.Children.Add(statsBlock);

                        Grid.SetColumn(infoPanel, 1);
                        recordGrid.Children.Add(infoPanel);

                        TextBlock dateBlock = new TextBlock();
                        dateBlock.Text = record.Date.ToString("dd.MM");
                        dateBlock.FontSize = 10;
                        dateBlock.Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150));
                        dateBlock.VerticalAlignment = VerticalAlignment.Center;
                        dateBlock.Margin = new Thickness(8, 0, 0, 0);
                        Grid.SetColumn(dateBlock, 2);
                        recordGrid.Children.Add(dateBlock);

                        recordBorder.Child = recordGrid;
                        leaderboardPanel.Children.Add(recordBorder);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Ошибка: " + ex.Message);
            }
        }

        private void DifficultyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = DifficultyCombo.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                _currentDifficulty = selectedItem.Tag.ToString();
                UpdateLeaderboardDisplay();
            }
        }

        private void SaveGameResult(int moves, int timeSeconds)
        {
            var record = new GameRecord
            {
                PlayerName = _playerName,
                Difficulty = _currentDifficulty,
                Moves = moves,
                TimeSeconds = timeSeconds,
                Date = DateTime.Now
            };

            _records.Add(record);
            SaveRecords();

            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateLeaderboardDisplay();
            });

            var topRecords = _records
                .Where(r => r.Difficulty == _currentDifficulty)
                .OrderBy(r => r.Moves)
                .ThenBy(r => r.TimeSeconds)
                .Take(5)
                .ToList();

            bool isTop = topRecords.Any(r => r.PlayerName == _playerName && r.Moves == moves && r.TimeSeconds == timeSeconds);

            if (isTop)
            {
                MessageBox.Show($"🎉 ПОЗДРАВЛЯЕМ! 🎉\n\nВы вошли в ТОП-5 игроков!\n\nИмя: {_playerName}\nСложность: {GetDifficultyName()}\nХодов: {moves}\nВремя: {timeSeconds / 60}:{timeSeconds % 60:D2}",
                    "НОВЫЙ РЕКОРД!", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private string GetDifficultyName()
        {
            switch (_currentDifficulty)
            {
                case "Easy": return "Easy (2x2)";
                case "Medium": return "Medium (4x4)";
                case "Hard": return "Hard (6x6)";
                case "Expert": return "Expert (8x8)";
                default: return "Medium";
            }
        }

        private void StartGameBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Введите ваше имя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _playerName = NameTextBox.Text;
            PlayerNameText.Text = _playerName;

            var selectedItem = DifficultyCombo.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                _currentDifficulty = selectedItem.Tag.ToString();
            }

            UpdateLeaderboardDisplay();

            StartDialog.Visibility = Visibility.Collapsed;
            GamePanel.Visibility = Visibility.Visible;

            InitializeNewGame();
        }

        private void NewGameBtn_Click(object sender, RoutedEventArgs e)
        {
            StopTimer();
            InitializeNewGame();
        }

        private void BackToMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            StopTimer();
            StartDialog.Visibility = Visibility.Visible;
            GamePanel.Visibility = Visibility.Collapsed;
            UpdateLeaderboardDisplay();
        }

        private void ExitGameBtn_Click(object sender, RoutedEventArgs e)
        {
            StopTimer();
            StopMusic();
            Application.Current.Shutdown();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            StopMusic();
            Application.Current.Shutdown();
        }

        private void ShowLeaderboard_Click(object sender, RoutedEventArgs e)
        {
            var topRecords = _records
                .OrderBy(r => r.Moves)
                .ThenBy(r => r.TimeSeconds)
                .Take(10)
                .ToList();

            string message = "🏆 ТАБЛИЦА РЕКОРДОВ 🏆\n\n";

            if (topRecords.Count == 0)
            {
                message += "Пока нет рекордов.\nНачните игру!";
            }
            else
            {
                for (int i = 0; i < topRecords.Count; i++)
                {
                    var r = topRecords[i];
                    string timeStr = $"{r.TimeSeconds / 60}:{r.TimeSeconds % 60:D2}";
                    message += $"{i + 1}. {r.PlayerName} - {r.Moves} ходов, {timeStr} ({r.Difficulty})\n";
                }
            }

            MessageBox.Show(message, "🏆 Рекорды", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void InitializeNewGame()
        {
            _firstOpenedCard = null;
            _secondOpenedCard = null;
            _isWaiting = false;
            _secondsElapsed = 0;
            _moves = 0;
            _pairsFound = 0;

            TimeText.Text = "00:00";
            MovesText.Text = "0";
            PairsFoundText.Text = "0";

            switch (_currentDifficulty)
            {
                case "Easy": _totalPairs = 2; break;
                case "Medium": _totalPairs = 8; break;
                case "Hard": _totalPairs = 18; break;
                case "Expert": _totalPairs = 32; break;
                default: _totalPairs = 8; break;
            }

            GenerateCards();
            RenderCards();
            StartTimer();
        }

        private void GenerateCards()
        {
            _cards.Clear();
            int cardId = 0;

            List<ImageSource> cardImages = LoadCardImages();

            for (int pairId = 1; pairId <= _totalPairs; pairId++)
            {
                ImageSource cardImage = (pairId - 1 < cardImages.Count) ? cardImages[pairId - 1] : null;

                for (int i = 0; i < 2; i++)
                {
                    _cards.Add(new GameCard
                    {
                        Id = cardId++,
                        PairId = pairId,
                        IsOpened = false,
                        IsMatched = false,
                        CardImage = cardImage
                    });
                }
            }

            ShuffleCards();
        }

        private List<ImageSource> LoadCardImages()
        {
            List<ImageSource> images = new List<ImageSource>();
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string imagesPath = Path.Combine(baseDir, "Images");

            if (!Directory.Exists(imagesPath))
            {
                Directory.CreateDirectory(imagesPath);
            }

            for (int i = 1; i <= 32; i++)
            {
                string imageFile = Path.Combine(imagesPath, $"card{i}.png");
                if (!File.Exists(imageFile))
                {
                    imageFile = Path.Combine(imagesPath, $"card{i}.jpg");
                }
                if (!File.Exists(imageFile))
                {
                    imageFile = Path.Combine(imagesPath, $"card{i}.jpeg");
                }

                if (File.Exists(imageFile))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imageFile);
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    images.Add(bitmap);
                }
                else
                {
                    images.Add(null);
                }
            }

            return images;
        }

        private void ShuffleCards()
        {
            Random rnd = new Random();
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                var temp = _cards[i];
                _cards[i] = _cards[j];
                _cards[j] = temp;
            }
        }

        private void RenderCards()
        {
            CardsPanel.Children.Clear();

            if (_currentDifficulty == "Hard")
                CardsPanel.MaxWidth = 650;
            else if (_currentDifficulty == "Medium")
                CardsPanel.MaxWidth = 500;
            else if (_currentDifficulty == "Expert")
                CardsPanel.MaxWidth = 850;
            else
                CardsPanel.MaxWidth = 250;

            foreach (var card in _cards)
            {
                Button btn = new Button();
                btn.Width = 90;
                btn.Height = 90;
                btn.Margin = new Thickness(6);
                btn.Cursor = System.Windows.Input.Cursors.Hand;
                btn.Tag = card;

                Grid grid = new Grid();

                Border backBorder = new Border();
                backBorder.CornerRadius = new CornerRadius(12);
                backBorder.Background = Brushes.White;
                backBorder.Visibility = Visibility.Visible;

                if (_cardImageSource != null)
                {
                    Image cardImage = new Image();
                    cardImage.Source = _cardImageSource;
                    cardImage.Stretch = Stretch.UniformToFill;
                    cardImage.HorizontalAlignment = HorizontalAlignment.Center;
                    cardImage.VerticalAlignment = VerticalAlignment.Center;
                    cardImage.Width = 88;
                    cardImage.Height = 88;
                    backBorder.Child = cardImage;
                }
                else
                {
                    TextBlock questionMark = new TextBlock();
                    questionMark.Text = "?";
                    questionMark.FontSize = 40;
                    questionMark.FontWeight = FontWeights.Bold;
                    questionMark.Foreground = new SolidColorBrush(Color.FromRgb(52, 73, 94));
                    questionMark.HorizontalAlignment = HorizontalAlignment.Center;
                    questionMark.VerticalAlignment = VerticalAlignment.Center;
                    backBorder.Child = questionMark;
                }

                Border frontBorder = new Border();
                frontBorder.CornerRadius = new CornerRadius(12);
                frontBorder.Background = Brushes.White;
                frontBorder.Visibility = Visibility.Collapsed;

                if (card.CardImage != null)
                {
                    Image cardImageOpen = new Image();
                    cardImageOpen.Source = card.CardImage;
                    cardImageOpen.Stretch = Stretch.UniformToFill;
                    cardImageOpen.HorizontalAlignment = HorizontalAlignment.Center;
                    cardImageOpen.VerticalAlignment = VerticalAlignment.Center;
                    cardImageOpen.Width = 88;
                    cardImageOpen.Height = 88;
                    frontBorder.Child = cardImageOpen;
                }
                else
                {
                    TextBlock numberText = new TextBlock();
                    numberText.Text = card.PairId.ToString();
                    numberText.FontSize = 32;
                    numberText.FontWeight = FontWeights.Bold;
                    numberText.Foreground = new SolidColorBrush(Color.FromRgb(255, 107, 107));
                    numberText.HorizontalAlignment = HorizontalAlignment.Center;
                    numberText.VerticalAlignment = VerticalAlignment.Center;
                    frontBorder.Child = numberText;
                }

                Border matchedBorder = new Border();
                matchedBorder.CornerRadius = new CornerRadius(12);
                matchedBorder.Background = new SolidColorBrush(Color.FromRgb(46, 204, 113));
                matchedBorder.Visibility = Visibility.Collapsed;

                if (_cardImageSource != null)
                {
                    Image foundImage = new Image();
                    foundImage.Source = _cardImageSource;
                    foundImage.Stretch = Stretch.UniformToFill;
                    foundImage.Width = 70;
                    foundImage.Height = 70;
                    foundImage.Opacity = 0.8;

                    StackPanel matchedStack = new StackPanel();
                    matchedStack.HorizontalAlignment = HorizontalAlignment.Center;
                    matchedStack.VerticalAlignment = VerticalAlignment.Center;
                    matchedStack.Children.Add(foundImage);

                    matchedBorder.Child = matchedStack;
                }
                else
                {
                    StackPanel matchedStack = new StackPanel();
                    matchedStack.HorizontalAlignment = HorizontalAlignment.Center;
                    matchedStack.VerticalAlignment = VerticalAlignment.Center;
                    matchedStack.Children.Add(new TextBlock { Text = "✓", FontSize = 35, Foreground = Brushes.White, FontWeight = FontWeights.Bold });
                    matchedStack.Children.Add(new TextBlock { Text = "Найдено", FontSize = 10, Foreground = Brushes.White });
                    matchedBorder.Child = matchedStack;
                }

                grid.Children.Add(backBorder);
                grid.Children.Add(frontBorder);
                grid.Children.Add(matchedBorder);

                btn.Content = grid;
                btn.Click += Card_Click;

                CardsPanel.Children.Add(btn);
                card.Button = btn;
            }
        }

        private void UpdateCardVisual(GameCard card)
        {
            Grid grid = card.Button.Content as Grid;
            Border backBorder = grid.Children[0] as Border;
            Border frontBorder = grid.Children[1] as Border;
            Border matchedBorder = grid.Children[2] as Border;

            if (card.IsMatched)
            {
                backBorder.Visibility = Visibility.Collapsed;
                frontBorder.Visibility = Visibility.Collapsed;
                matchedBorder.Visibility = Visibility.Visible;
            }
            else if (card.IsOpened)
            {
                backBorder.Visibility = Visibility.Collapsed;
                frontBorder.Visibility = Visibility.Visible;
                matchedBorder.Visibility = Visibility.Collapsed;
            }
            else
            {
                backBorder.Visibility = Visibility.Visible;
                frontBorder.Visibility = Visibility.Collapsed;
                matchedBorder.Visibility = Visibility.Collapsed;
            }
        }

        private async void Card_Click(object sender, RoutedEventArgs e)
        {
            if (_isWaiting) return;

            Button btn = sender as Button;
            GameCard clickedCard = btn.Tag as GameCard;

            if (clickedCard.IsMatched || clickedCard.IsOpened) return;

            clickedCard.IsOpened = true;
            UpdateCardVisual(clickedCard);

            if (_firstOpenedCard == null)
            {
                _firstOpenedCard = clickedCard;
                return;
            }

            if (_secondOpenedCard == null && _firstOpenedCard != clickedCard)
            {
                _secondOpenedCard = clickedCard;
                _moves++;
                MovesText.Text = _moves.ToString();

                if (_firstOpenedCard.PairId == _secondOpenedCard.PairId)
                {
                    _firstOpenedCard.IsMatched = true;
                    _secondOpenedCard.IsMatched = true;
                    UpdateCardVisual(_firstOpenedCard);
                    UpdateCardVisual(_secondOpenedCard);

                    _pairsFound++;
                    PairsFoundText.Text = _pairsFound.ToString();

                    _firstOpenedCard = null;
                    _secondOpenedCard = null;

                    CheckVictory();
                }
                else
                {
                    _isWaiting = true;
                    await System.Threading.Tasks.Task.Delay(800);

                    _firstOpenedCard.IsOpened = false;
                    _secondOpenedCard.IsOpened = false;
                    UpdateCardVisual(_firstOpenedCard);
                    UpdateCardVisual(_secondOpenedCard);

                    _firstOpenedCard = null;
                    _secondOpenedCard = null;
                    _isWaiting = false;
                }
            }
        }

        private void CheckVictory()
        {
            if (_pairsFound == _totalPairs)
            {
                StopTimer();
                SaveGameResult(_moves, _secondsElapsed);

                string timeStr = $"{_secondsElapsed / 60}:{_secondsElapsed % 60:D2}";

                MessageBox.Show($"🎉 ПОЗДРАВЛЯЕМ, {_playerName}! 🎉\n\n✅ Вы нашли все {_totalPairs} пар!\n🎯 Ходов сделано: {_moves}\n⏱️ Время игры: {timeStr}\n\n🏆 Результат сохранен!",
                    "ПОБЕДА!", MessageBoxButton.OK, MessageBoxImage.Information);

                StartDialog.Visibility = Visibility.Visible;
                GamePanel.Visibility = Visibility.Collapsed;
                UpdateLeaderboardDisplay();
            }
        }

        private void StartTimer()
        {
            _gameTimer = new DispatcherTimer();
            _gameTimer.Interval = TimeSpan.FromSeconds(1);
            _gameTimer.Tick += Timer_Tick;
            _gameTimer.Start();
        }

        private void StopTimer()
        {
            if (_gameTimer != null)
            {
                _gameTimer.Stop();
                _gameTimer = null;
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _secondsElapsed++;
            TimeText.Text = $"{_secondsElapsed / 60:D2}:{_secondsElapsed % 60:D2}";
        }
    }
}