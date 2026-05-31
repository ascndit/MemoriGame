using System;

namespace MemoriGame.Models
{
    public class TopPlayer
    {
        public string PlayerName { get; set; }
        public int Moves { get; set; }
        public int TimeInSeconds { get; set; }
        public DateTime Date { get; set; }
        public int Rank { get; set; }
    }
}