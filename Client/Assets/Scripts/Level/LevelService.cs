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
            // Bundled levels live under Resources/Levels, not StreamingAssets - on Android,
            // StreamingAssets sits inside the compressed APK/AAB and plain System.IO.File
            // calls can't read it at all (this used to silently return every level as
            // missing on Android). Resources.Load works identically on every platform.
            string resourcePath = $"Levels/level{levelId}/level{levelId}";

            TextAsset confAsset = Resources.Load<TextAsset>(resourcePath);
            Texture2D tex = Resources.Load<Texture2D>(resourcePath);

            // Both are required for a level to actually be playable. In practice, level
            // folders can sit around with just a config and no exported image yet (e.g. stubs
            // left over from the level editor) - those aren't real content, so treat them the
            // same as a level that doesn't exist at all rather than failing loudly.
            if (confAsset == null || tex == null)
                return null;

            LevelConf lc = JsonUtility.FromJson<LevelConf>(confAsset.text);
            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f));

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

            level.GameMode = lc.game_mode == "switch" ? GameMode.Switch : GameMode.Slide;

            return level;
        }

        public Level GetNextLevel()
        {
            if (progressionService == null)
            {
                progressionService = ServiceLocator.GetInstance().Get<ProgressionService>();
            }

            Progression progression = progressionService.GetProgression();
            int nextLevel = progression.lastBeatenLevel + 1;
            return GetLevel(nextLevel);
        }
    }
}
