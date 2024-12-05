using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using Unity.VisualScripting;
using UnityEngine;

public enum MoveDirection
{
    LEFT,
    RIGHT,
    UP,
    DOWN,
    NULL
}

public class BoardManager : MonoBehaviour
{
    
    public static BoardManager instance;
    
    private Tile[][] board;
    private int boardCols = 3;
    private int boardRows = 5;

    private int maxFilledTiles = 13;

    private GameObject tileObj;

    private Sprite level;

    private Sprite[] sprites;

    private ImagePiece[] images;

    private Dictionary<string,Sprite> imageMap;

    private int totalMoves = 0;
    

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("BoardManager.Start");
        instance = this;
        tileObj = Resources.Load<GameObject>("Prefabs/Tile");
        level = Resources.Load<Sprite>("Images/paris2");
        sprites = Resources.LoadAll<Sprite>("Images/level_1");
        imageMap = new Dictionary<string, Sprite>();
        //Debug.Log("tileObj: "+tileObj.ToString());
        CreateImagePieces();
        CreateBoard();
        ScrambleBoard();
        

    }

    private void CreateImagePieces()
    {
        if(images != null)
            return;

        foreach(Sprite s in sprites)
        {
            imageMap.Add(s.name,s);
            Debug.Log("Adding sprite "+s.name+" to dictionary");
        }
        
    }

    private void CreateBoard()
    {
        Debug.Log("Creating a new board");
        board = new Tile[boardRows][];
        int totalTiles = 0;
        
        for(int i=0; i<board.Length; i++)
        {
            Tile[] row = new Tile[boardCols];
            
            for(int j=0;j<row.Length;j++)
            {
               
                Tile tile = totalTiles < maxFilledTiles ? CreateTile(j,i) : null;;
                Debug.Log("Create Tile for col:"+j+" and row:"+i);
                row[j] = tile;
                totalTiles++;
            }
            board[i] = row;

        }
    }

    private void ScrambleBoard()
    {
        if (board == null)
            return;

        Dictionary<string,Sprite>.KeyCollection keys = imageMap.Keys;
        List<string> list = new List<string>();
        Tile[][] scrambledBoard = new Tile[boardRows][];

        for(int i=0;i<scrambledBoard.Length;i++)
        {
            scrambledBoard[i] = new Tile[boardCols];
        }

        foreach(string s in keys)
        {   
            list.Add(s);
        }

        int count = 0;
        while(list.Count > 0)
        {
            int index = UnityEngine.Random.Range(0,list.Count);
            string key = list[index];
            

            string[] parts = key.Split("_", StringSplitOptions.None);
            int r = Int32.Parse(parts[0]); //Row
            int c = Int32.Parse(parts[1]); //Col

            int[] rowCol = ConvertCountToRowCol(count);

            int newRow = rowCol[0];
            int newCol = rowCol[1];
            Tile t =  board[r][c];
            
            if(t != null)
            {
                t.Move(newRow, newCol);
            
                scrambledBoard[newRow][newCol] = t;
            }
            

            list.RemoveAt(index);
            count++;
        }

        board = scrambledBoard;

        
    }

    private int[] ConvertCountToRowCol(int count)
    {
        int[] rowCol = {0,1};

        int row = count/boardCols;
        int col = count - (row * boardCols);

        rowCol[0] = row;
        rowCol[1] = col;

        return rowCol;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private Tile CreateTile(int col, int row)
    {

        var tile = Instantiate(tileObj,
            GetTilePos(col,row),
            tileObj.transform.rotation).AddComponent<Tile>();
        tile.SetupTile(col,row);
        tile.SetupSprite(imageMap[row+"_"+col]);
            

        return tile;
    }

    public Vector3 GetTilePos(int col, int row)
    {
        //Debug.Log("GetTilePos");
        float x = (float) col - 1;
        float y = (float) 2.0f - row;
        
        float z = -0.2f;
        Vector3 pos = new Vector3(x,y,z);
        return pos;
    }

    public void MoveTile(int row, int col, MoveDirection md)
    {
         Tile t = null;
         Tile fromTile = board[row][col];
         int toRow = 0;
         int toCol = 0;

        if(md == MoveDirection.NULL)
        {
            Debug.Log("MoveDirection is null.");
            return;
        }

        if(md == MoveDirection.UP)
        {
            if(row == 0) // Can't move as the tile is on top row already
            {
                Debug.Log("Can't move tile UP when on top row");    
                return;
            }
        
            toCol = col;
            toRow = row-1;
            t = board[toRow][toCol];
            
        }
        else if (md == MoveDirection.DOWN) // Can't move as the tile is on bottom row already
        {
            if(row == boardRows-1)
            {
                Debug.Log("Can't move tile DOWN when on bottom row");
                return;
            }
                
            toCol = col;
            toRow = row+1;
            t = board[toRow][toCol];

        }
        else if(md == MoveDirection.LEFT) // Can't move as the tile is on far left already
        {
            if(col == 0)
            {
                Debug.Log("Can't move tile LEFT when on the far left");
                return;
            }

            toCol = col-1;
            toRow = row;
            t = board[toRow][toCol];    
             
        }
        else if(md == MoveDirection.RIGHT)  // Can't move as the tile is on far right already
        {
            if(col == boardCols-1)
            {
                Debug.Log("Can't move tile RIGHT when on the far right");
                return;
            }
            toCol = col+1;
            toRow = row;
            t = board[toRow][toCol];    
            
        }


        if(t == null)
        {
            Debug.Log("Moving tile from pos "+row+":"+col+" in direction: "+md.ToString());
            board[toRow][toCol] = fromTile; //Moving tile to its new place on the board
            board[row][col] = null; //removing the tile from the old slot on the board.
            fromTile.Move(toRow, toCol);
            totalMoves++;
            Debug.Log("Total Moves: "+totalMoves);
            GameEnded();
        }
        else
        {
            Debug.Log("Can't move tile from pos "+row+":"+col+" in direction: "+md.ToString()+". Another tile occupying the slot");            
        }


    }

    public bool GameEnded()
    {
        int numCorrect = 0;
        int numFaulty = 0;

        for(int i=0;i<board.Length;i++)
        {
            Tile[] cols = board[i];
            
            for(int j=0;j<cols.Length;j++)
            {
                Tile tile = cols[j];
                if(tile == null) continue;

                if(tile.CorrectPlace())
                {
                    numCorrect++;
                }
                else
                {
                    numFaulty++;
                }
            }


        }

        //Debug.Log("Tiles correct:"+numCorrect);
        //Debug.Log("Tiles wrong:"+numFaulty);

        if(numFaulty>0)
            return false;
        
        Debug.Log("GAME ENDED");
        return true;


    }

}
