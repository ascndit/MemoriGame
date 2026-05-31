using System.ComponentModel;

namespace MemoriGame.Models
{
    public class Card : INotifyPropertyChanged
    {
        private bool _isOpened;
        private bool _isMatched;
        private string _imagePath;

        public int Id { get; set; }
        public int PairId { get; set; }

        public string ImagePath
        {
            get { return _imagePath; }
            set
            {
                _imagePath = value;
                OnPropertyChanged("ImagePath");
            }
        }

        public bool IsOpened
        {
            get { return _isOpened; }
            set
            {
                _isOpened = value;
                OnPropertyChanged("IsOpened");
            }
        }

        public bool IsMatched
        {
            get { return _isMatched; }
            set
            {
                _isMatched = value;
                OnPropertyChanged("IsMatched");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}