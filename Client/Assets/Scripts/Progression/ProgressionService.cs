using System;
using System.IO;
using UnityEngine;

namespace PuzzleParty.Progressions
{
    public class ProgressionService : IProgressionService
    {
        // Set to true to reset progression every time the game starts (development only)
        private const bool RESET_PROGRESSION_ON_STARTUP = true;
        private static bool hasResetThisSession = false;

        private static string SavePath => Path.Combine(Application.persistentDataPath, "progression.json");
        public Progression GetProgression()
    {
        Debug.Log($"Loading progression from: {SavePath}");

        // Reset progression on startup if enabled (development only)
        // Only reset once per application session, not every scene load
        if (RESET_PROGRESSION_ON_STARTUP && !hasResetThisSession && File.Exists(SavePath))
        {
            Debug.LogWarning("RESET_PROGRESSION_ON_STARTUP is enabled - deleting progression file");
            File.Delete(SavePath);
            hasResetThisSession = true;
        }

        if (!File.Exists(SavePath)) //no progression found. Create a new progression
        {
            Debug.Log("No progression file found, creating new progression");
            return CreateNewProgression();
        }

        string json = File.ReadAllText(SavePath);
        Debug.Log($"Loaded progression JSON: {json}");
        Progression prog = JsonUtility.FromJson<Progression>(json);
        Debug.Log($"Progression loaded - Level: {prog.lastBeatenLevel}, Coins: {prog.coins}");
        return prog;
    }

    public void SaveProgression(Progression progression)
    {
        string json = JsonUtility.ToJson(progression);
        Debug.Log($"Saving progression to: {SavePath}");
        Debug.Log($"Progression data: {json}");
        File.WriteAllText(SavePath, json);
        Debug.Log("Progression saved successfully");
    }

    public void WipeProgression()
    {
        //For dev purposes only
        Progression p = GetProgression();
        p.lastBeatenLevel = 0;
        p.coins = 0;

        SaveProgression(p);
    }

    private Progression CreateNewProgression()
    {
        Progression prog = new Progression();
        prog.lastBeatenLevel = 0;
        prog.coins = 0;
        SaveProgression(prog);
        return prog;

    }
    }
}