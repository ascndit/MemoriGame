using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MemoriGame.Models;
using MemoriGame.Services;

namespace MemoriGame.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private GameService _gameService;
        private ScoreService _scoreService;

        private List<Card> _allCards;
        private Card _firstOpenedCard;
        private Card _secondOpenedCard;
        private bool _isWaiting;
        private System.Timers.Timer _gameTimer;

        private string _playerName;
        private Difficulty _selectedDifficulty;
        private string _selectedTheme;
        private ObservableCollection<Card> _cards;
        private int _moves;
        private int _pairsFound;
        private int _totalPairs;
        private string _timeText;
        private int _secondsElapsed;
        private bool _isGameActive;
        private bool _showStartDialog;

        public ICommand StartGameCommand { get; private set; }
        public ICommand NewGameCommand { get; private set; }
        public ICommand CardClickCommand { get; private set; }
        public ICommand ExitCommand { get; private set; }

        public MainWindowViewModel()
        {
            _gameService = new GameService();
            _scoreService = new ScoreService();

            // Команды
            StartGameCommand = new RelayCommand((p) => StartGame(), (p) => CanStartGame());
            NewGameCommand = new RelayCommand((p) => NewGame());
            CardClickCommand = new RelayCommand((p) => OnCardClick(p));
            ExitCommand = new RelayCommand((p) => Exit());

            // Начальные значения
            _showStartDialog = true;
            _selectedDifficulty = Difficulty.Medium;
            _selectedTheme = "Мои фото";  // Тема по умолчанию
            _cards = new ObservableCollection<Card>();
            _timeText = "00:00";
            _isGameActive = false;
        }

        // Свойства
        public bool ShowStartDialog
        {
            get { return _showStartDialog; }
            set
            {
                _showStartDialog = value;
                OnPropertyChanged("ShowStartDialog");
            }
        }

        public string PlayerName
        {
            get { return _playerName; }
            set
            {
                _playerName = value;
                OnPropertyChanged("PlayerName");
                ((RelayCommand)StartGameCommand).RaiseCanExecuteChanged();
            }
        }

        public Difficulty SelectedDifficulty
        {
            get { return _selectedDifficulty; }
            set
            {
                _selectedDifficulty = value;
                OnPropertyChanged("SelectedDifficulty");
            }
        }

        public string SelectedTheme
        {
            get { return _selectedTheme; }
            set
            {
                _selectedTheme = value;
                OnPropertyChanged("SelectedTheme");
            }
        }

        public ObservableCollection<Card> Cards
        {
            get { return _cards; }
            set
            {
                _cards = value;
                OnPropertyChanged("Cards");
            }
        }

        public int Moves
        {
            get { return _moves; }
            set
            {
                _moves = value;
                OnPropertyChanged("Moves");
            }
        }

        public int PairsFound
        {
            get { return _pairsFound; }
            set
            {
                _pairsFound = value;
                OnPropertyChanged("PairsFound");
            }
        }

        public string TimeText
        {
            get { return _timeText; }
            set
            {
                _timeText = value;
                OnPropertyChanged("TimeText");
            }
        }

        public bool IsGameActive
        {
            get { return _isGameActive; }
            set
            {
                _isGameActive = value;
                OnPropertyChanged("IsGameActive");
            }
        }

        // Списки для выбора
        public List<Difficulty> Difficulties
        {
            get
            {
                return new List<Difficulty>
                {
                    Difficulty.Easy,
                    Difficulty.Medium,
                    Difficulty.Hard
                };
            }
        }

        public List<string> Themes
        {
            get
            {
                return new List<string>
                {
                    "Мои фото"
                };
            }
        }

        // Методы
        private void StartGame()
        {
            if (string.IsNullOrWhiteSpace(PlayerName))
                return;

            ShowStartDialog = false;
            InitializeNewGame();
        }

        private bool CanStartGame()
        {
            return !string.IsNullOrWhiteSpace(PlayerName);
        }

        private void NewGame()
        {
            StopTimer();
            InitializeNewGame();
        }

        private void InitializeNewGame()
        {
            _firstOpenedCard = null;
            _secondOpenedCard = null;
            _isWaiting = false;
            _secondsElapsed = 0;
            TimeText = "00:00";
            Moves = 0;
            PairsFound = 0;

            // Генерируем карточки с выбранной темой
            _allCards = _gameService.GenerateCards(SelectedDifficulty, SelectedTheme);
            _totalPairs = _allCards.Count / 2;

            Cards.Clear();
            foreach (Card card in _allCards)
            {
                Cards.Add(card);
            }

            IsGameActive = true;
            StartTimer();
        }

        private void OnCardClick(object parameter)
        {
            Card clickedCard = parameter as Card;
            if (clickedCard == null) return;

            if (!IsGameActive || _isWaiting || clickedCard.IsMatched || clickedCard.IsOpened)
                return;

            clickedCard.IsOpened = true;

            if (_firstOpenedCard == null)
            {
                _firstOpenedCard = clickedCard;
                return;
            }

            if (_secondOpenedCard == null && _firstOpenedCard != clickedCard)
            {
                _secondOpenedCard = clickedCard;
                Moves++;

                if (_firstOpenedCard.PairId == _secondOpenedCard.PairId)
                {
                    // Совпадение!
                    _firstOpenedCard.IsMatched = true;
                    _secondOpenedCard.IsMatched = true;

                    PairsFound++;

                    _firstOpenedCard = null;
                    _secondOpenedCard = null;

                    CheckVictory();
                }
                else
                {
                    // Не совпали - ждем и закрываем
                    _isWaiting = true;

                    Task.Delay(1000).ContinueWith(_ =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _firstOpenedCard.IsOpened = false;
                            _secondOpenedCard.IsOpened = false;

                            _firstOpenedCard = null;
                            _secondOpenedCard = null;
                            _isWaiting = false;
                        });
                    });
                }
            }
        }

        private void CheckVictory()
        {
            if (PairsFound == _totalPairs)
            {
                IsGameActive = false;
                StopTimer();

                GameResult result = new GameResult
                {
                    PlayerName = PlayerName,
                    Moves = Moves,
                    TimeInSeconds = _secondsElapsed,
                    Difficulty = _gameService.GetDifficultyName(SelectedDifficulty),
                    Date = DateTime.Now
                };

                bool isTop = _scoreService.IsTopResult(result.Difficulty, result.Moves, result.TimeInSeconds);
                _scoreService.SaveResult(result);

                string timeStr = string.Format("{0:D2}:{1:D2}", _secondsElapsed / 60, _secondsElapsed % 60);
                string topMessage = isTop ? "\n\nПоздравляем! Вы вошли в топ-5!" : "";

                MessageBox.Show(
                    string.Format("Поздравляем, {0}! Вы нашли все пары!\nХодов: {1}\nВремя: {2}{3}",
                        PlayerName, Moves, timeStr, topMessage),
                    "Победа!",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                ShowStartDialog = true;
            }
        }

        private void StartTimer()
        {
            _gameTimer = new System.Timers.Timer(1000);
            _gameTimer.Elapsed += TimerElapsed;
            _gameTimer.Start();
        }

        private void StopTimer()
        {
            if (_gameTimer != null)
            {
                _gameTimer.Stop();
                _gameTimer.Dispose();
                _gameTimer = null;
            }
        }

        private void TimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _secondsElapsed++;
            Application.Current.Dispatcher.Invoke(() =>
            {
                TimeText = string.Format("{0:D2}:{1:D2}", _secondsElapsed / 60, _secondsElapsed % 60);
            });
        }

        private void Exit()
        {
            StopTimer();
            Application.Current.Shutdown();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    // RelayCommand класс
    public class RelayCommand : ICommand
    {
        private Action<object> _execute;
        private Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}