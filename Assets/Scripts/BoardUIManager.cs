using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BoardUIManager : MonoBehaviour
{
    [SerializeField]
    public TextMeshProUGUI moves;
    // Start is called before the first frame update
    public static BoardUIManager instance;
    void Start()
    {
        if(instance == null)
        {
            instance = this;
        }
        moves.text = "poooop";
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateMoves(int movesLeft)
    {
        moves.text = ""+movesLeft;
    }


}
