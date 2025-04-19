// Computer.cs
using UnityEngine;

public class Computer : Interactable
{
    protected override void OnClicked()
    {
        // your Computer-specific click logic here
        Debug.Log("Computer clicked!");
    }
}
