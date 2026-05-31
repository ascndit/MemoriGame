using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using MemoriGame.Models;

namespace MemoriGame.Services
{
    public class ScoreService
    {
        private string filePath = "scores.json";

        public void SaveResult(GameResult result)
        {
            List<GameResult> results = LoadAllResults();
            results.Add(result);

            string json = JsonConvert.SerializeObject(results, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        public List<GameResult> LoadAllResults()
        {
            if (!File.Exists(filePath))
                return new List<GameResult>();

            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<List<GameResult>>(json) ?? new List<GameResult>();
        }

        public List<TopPlayer> GetTopPlayers(string difficulty)
        {
            var results = LoadAllResults()
                .Where(r => r.Difficulty == difficulty)
                .OrderBy(r => r.Moves)
                .ThenBy(r => r.TimeInSeconds)
                .Take(5)
                .Select((r, index) => new TopPlayer
                {
                    Rank = index + 1,
                    PlayerName = r.PlayerName,
                    Moves = r.Moves,
                    TimeInSeconds = r.TimeInSeconds,
                    Date = r.Date
                })
                .ToList();

            return results;
        }

        public bool IsTopResult(string difficulty, int moves, int timeInSeconds)
        {
            List<TopPlayer> topPlayers = GetTopPlayers(difficulty);

            if (topPlayers.Count < 5)
                return true;

            TopPlayer lastPlayer = topPlayers.Last();
            if (moves < lastPlayer.Moves)
                return true;
            if (moves == lastPlayer.Moves && timeInSeconds < lastPlayer.TimeInSeconds)
                return true;

            return false;
        }
    }
}