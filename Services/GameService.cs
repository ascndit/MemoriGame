using System;
using System.Collections.Generic;
using MemoriGame.Models;

namespace MemoriGame.Services
{
    public class GameService
    {
        private Random _random = new Random();

        public List<Card> GenerateCards(Difficulty difficulty, string theme)
        {
            int pairsCount = 0;

            switch (difficulty)
            {
                case Difficulty.Easy:
                    pairsCount = 2;
                    break;
                case Difficulty.Medium:
                    pairsCount = 8;
                    break;
                case Difficulty.Hard:
                    pairsCount = 18;
                    break;
            }

            List<Card> cards = new List<Card>();
            int cardId = 0;

            for (int pairId = 1; pairId <= pairsCount; pairId++)
            {
                for (int i = 0; i < 2; i++)
                {
                    cards.Add(new Card
                    {
                        Id = cardId++,
                        PairId = pairId,
                        ImagePath = "/Images/photo.jpg",
                        IsOpened = false,
                        IsMatched = false
                    });
                }
            }

            return ShuffleCards(cards);
        }

        private List<Card> ShuffleCards(List<Card> cards)
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                Card temp = cards[i];
                cards[i] = cards[j];
                cards[j] = temp;
            }
            return cards;
        }

        public string GetDifficultyName(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy: return "Easy";
                case Difficulty.Medium: return "Medium";
                case Difficulty.Hard: return "Hard";
                default: return "Medium";
            }
        }
    }
}