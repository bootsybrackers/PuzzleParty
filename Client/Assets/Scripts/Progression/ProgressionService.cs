using System.IO;
using UnityEngine;

public class ProgressionService : IProgressionService
{

    private static string SavePath => Path.Combine(Application.persistentDataPath, "progression.json");
    public Progression GetProgression()
    {
        if (!File.Exists(SavePath)) //no progression found. Create a new progression
            return CreateNewProgression();

        string json = File.ReadAllText(SavePath);
        return JsonUtility.FromJson<Progression>(json);
    }

    public void SaveProgression(Progression progression)
    {
        string json = JsonUtility.ToJson(progression);
        File.WriteAllText(SavePath, json);
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