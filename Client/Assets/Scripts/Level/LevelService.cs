using System.IO;
using UnityEngine;
using PuzzleParty.Progressions;
using PuzzleParty.Service;

namespace PuzzleParty.Levels
{
    public class LevelService : ILevelService
    {
        private ProgressionService progressionService;

    public Level GetLevel(int levelId)
    {
        LevelConf lc = LoadLevelConf(levelId);
        Sprite sprite = LoadLevelImage(levelId);

        Level level = new Level();
        level.Id = lc.id;
        level.Columns = lc.columns;
        level.Rows = lc.rows;
        level.Moves = lc.moves;
        level.Holes = lc.holes;
        level.LevelSprite = sprite;
        level.Name = lc.name;

        if (lc.locked_tiles != null && lc.locked_tiles.Length > 0)
        {
            foreach (var lockedTile in lc.locked_tiles)
                level.LockedTiles.Add((lockedTile.row, lockedTile.column));
        }

        if (lc.ice_rows != null && lc.ice_rows.Length > 0)
        {
            foreach (var row in lc.ice_rows)
                level.IceRows.Add(row);
        }

        return level;
    }

   


    public Level GetNextLevel()
    {
        if(progressionService == null)
        {
            progressionService = ServiceLocator.GetInstance().Get<ProgressionService>();
        }

        Progression progression = progressionService.GetProgression();
        int nextLevel = progression.lastBeatenLevel + 1;
        return GetLevel(nextLevel);
        
    }

    private LevelConf LoadLevelConf(int levelId)
    {
        
        string confPath = Path.Combine(Application.streamingAssetsPath, "levels/level" + levelId + "/level" + levelId +".json");
        string json = File.ReadAllText(confPath);
        return JsonUtility.FromJson<LevelConf>(json);
    }

    private Sprite LoadLevelImage(int levelId)
    {
        string imagePath = Path.Combine(Application.streamingAssetsPath, "levels/level" + levelId + "/level" + levelId + ".png");
        byte[] fileData = File.ReadAllBytes(imagePath);
        Texture2D tex = new Texture2D(2, 2); // Size doesn't matter; will be replaced
        tex.LoadImage(fileData);             // Automatically resizes and loads
        Sprite sprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f));

        return sprite;

    }

    }
}