using System.Collections.Generic;
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
        
        string imagePath = Path.Combine(Application.streamingAssetsPath, "/levels/level" + levelId + "/level" + levelId + ".png");
        string confPath = Path.Combine(Application.streamingAssetsPath, "/levels/level" + levelId + "/level" + levelId +".json");
        
        LevelConf lc = LoadLevelConf(levelId);
        Sprite sprite = LoadLevelImage(levelId);

        Debug.Log("LevelConf: id:"+ levelId +
            " rows: "+lc.rows);
        
        Level level = new Level();
        level.Id = lc.id;
        level.Columns = lc.columns;
        level.Rows = lc.rows;
        level.Moves = lc.moves;
        level.Holes = lc.holes;
        level.LevelSprite = sprite;
        level.Name = lc.name;



        //level.Id = 
        

        
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

    private void PopulateLevelShardDictionary(Level level)
    {
        Dictionary<int,Sprite> sprites = new Dictionary<int, Sprite>();

        for(int i=0; i < level.Rows; i++)
        {
            for(int j=0; j < level.Columns; j++)
            {
                string id = ""+i+"_"+j;

            }
        }
    }

    private Texture2D CopyRegion(int row, int col, Sprite original)
    {
        int pixelsWidth = 256;

        Texture2D orgTex = original.texture;
       
        Texture2D newTex = new Texture2D(GetSize(row, col), GetSize(row, col), orgTex.format, false);
        Color[] pixels = orgTex.GetPixels(0, 0, pixelsWidth, pixelsWidth);
        newTex.SetPixels(pixels);
        newTex.Apply();
        return newTex;
    
    }

    private int GetSize(int rows, int cols)
    {
        return 256;

    }
    }
}