using System;

namespace MemoriGame.Models
{
    public class GameResult
    {
        public string PlayerName { get; set; }
        public int Moves { get; set; }
        public int TimeInSeconds { get; set; }
        public string Difficulty { get; set; }
        public DateTime Date { get; set; }
    }
}