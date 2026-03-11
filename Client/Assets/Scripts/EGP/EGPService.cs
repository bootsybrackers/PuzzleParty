using System.IO;
using UnityEngine;
using PuzzleParty.Progressions;

namespace PuzzleParty.EGP
{
    public class EGPService : IEGPService
    {
        private EGPConfig config;
        private int currentRound;

        public EGPService()
        {
            LoadConfig();
            currentRound = 0;
        }

        private void LoadConfig()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "config/egp.json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                config = JsonUtility.FromJson<EGPConfig>(json);
                Debug.Log($"EGP config loaded with {config.rounds.Length} rounds");
            }
            else
            {
                Debug.LogWarning("EGP config not found at: " + path);
                config = new EGPConfig { rounds = new EGPRound[0] };
            }
        }

        public EGPRound GetCurrentOffer()
        {
            if (config.rounds == null || currentRound >= config.rounds.Length)
            {
                return null;
            }
            return config.rounds[currentRound];
        }

        public bool CanAfford(IProgressionService progressionService)
        {
            EGPRound offer = GetCurrentOffer();
            if (offer == null) return false;

            Progression progression = progressionService.GetProgression();
            return progression.coins >= offer.price;
        }

        public EGPContents Purchase(IProgressionService progressionService)
        {
            EGPRound offer = GetCurrentOffer();
            if (offer == null)
            {
                Debug.LogWarning("No EGP offer available to purchase");
                return null;
            }

            Progression progression = progressionService.GetProgression();
            if (progression.coins < offer.price)
            {
                Debug.LogWarning("Not enough coins for EGP purchase");
                return null;
            }

            // Deduct coins
            progression.coins -= offer.price;
            progressionService.SaveProgression(progression);
            Debug.Log($"EGP purchase: spent {offer.price} coins, {progression.coins} remaining");

            // Advance to next round
            currentRound++;

            return offer.contents;
        }

        public void ResetRounds()
        {
            currentRound = 0;
            Debug.Log("EGP rounds reset");
        }
    }
}
