using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BoardUIManager : MonoBehaviour
{
    [SerializeField]
    public TextMeshProUGUI moves;

    [SerializeField]
    public Canvas canvas;
    // Start is called before the first frame update
    public static BoardUIManager instance;
    void Start()
    {
        if(instance == null)
        {
            instance = this;
        }
        moves.text = "poooop";
        canvas.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateMoves(int movesLeft)
    {
        moves.text = ""+movesLeft;
    }

    public void TriggerOverlay(bool success)
    {
        Debug.Log("Trigger overlay");
        if(success)
        {

        }
        else 
        {
            Debug.Log("Triggering "+canvas.name);
            canvas.enabled = true;
            Debug.Log("Triggered");      
        }
    }


}
