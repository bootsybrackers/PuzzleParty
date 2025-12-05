using System.Collections.Generic;
using UnityEngine;

namespace PuzzleParty.Levels
{
    public class Level
    {
        public int Id {get;set;}
        public int Rows {get;set;}
        public int Columns {get;set;}

        public int Moves {get;set;}

        public int Holes {get;set;}

        public Sprite LevelSprite {get;set;}

        public string Name {get;set;}

        public List<(int row, int column)> LockedTiles {get;set;} = new List<(int row, int column)>();
    }
}