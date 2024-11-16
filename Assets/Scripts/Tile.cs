using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;


public class Tile : MonoBehaviour
{
    
    private int column;

    private int row;

    private bool moving;

    private float dragThreshold = 0.5f;

    private SpriteRenderer spriteRenderer; 

    private int correctColumn;

    private int correctRow;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    void FixedUpdate()
    {
        if (!moving)
        {
            return;
        }

        Debug.Log("Dragging tile "+column+":"+row);

        Vector3 pos = Input.mousePosition;
        Vector3 position = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        var startX = transform.position.x;
        var startY = transform.position.y;

        Debug.Log("start X: "+startX+" start Y:"+startY);

        var diffX = Mathf.Abs(startX - position.x);
	    var diffY = Mathf.Abs(startY - position.y);
        Debug.Log("pos X: "+position.x+" pos Y:"+position.y);


        bool right = position.x > (startX+dragThreshold);
        bool left = position.x < (startX-dragThreshold);
        bool up = position.y > (startY+dragThreshold);
        bool down = position.y < (startY-dragThreshold);

        MoveDirection md = MoveDirection.NULL;
        if (diffX > diffY) //horizontal move
        {
            Debug.Log("Horizontal move");
            if(right) md = MoveDirection.RIGHT;
            if(left) md = MoveDirection.LEFT;
        } 
        
        if(diffY > diffX) //vertical move
        {
            Debug.Log("vertical move");
            if(up) md = MoveDirection.UP;
            if(down) md = MoveDirection.DOWN;
        }

        if(md != MoveDirection.NULL)
        {

            Debug.Log("Moving " + md.ToString());
            BoardManager.instance.MoveTile(this.row,this.column,md);
            moving = false;
        }
        
    }

    public void Move(int row, int col)
    {
        this.column = col;
        this.row = row;

        Vector3 pos = BoardManager.instance.GetTilePos(this.column, this.row);
        this.transform.position = pos; //Should do a nice animation and not just switch pos. Fixing later.
    }

    public void SetupTile(int col, int row)
    {
        Debug.Log("Setting up Tile: "+col+":"+row);
        this.column = col;
        this.row = row;
        this.correctColumn = col;
        this.correctRow = row;
        this.moving = false;
    }

    public void SetupSprite(Sprite level)
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = level;
    }

    public bool CorrectPlace()
    {
        if(correctColumn == column && correctRow == row)
            return true;

        return false;    
    }

    void OnMouseDown()
    {
        Debug.Log("Trying to drag tile: "+this.column+":"+this.row);
        moving = true;
    }

    void OnMouseUp()
    {
        Debug.Log("Stopped moving");
        moving = false;
    }
}
