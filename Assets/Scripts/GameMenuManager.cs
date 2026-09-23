using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; 

public class GameMenuManager : MonoBehaviour
{
    public GameObject gameMenu;
    public InputActionProperty showButton;        

    void Start()
    {
        
    }

    void Update()
    {
        if (showButton.action.WasPressedThisFrame())
        {
            gameMenu.SetActive(!gameMenu.activeSelf);
        }
    }
}