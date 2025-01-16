using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImagePiece 
{
    public int row;
    public int col;

    public Sprite sprite;

    public ImagePiece(int row, int col, Sprite sprite)
    {
        this.row = row;
        this.col = col;
        this.sprite = sprite;
    }
}
