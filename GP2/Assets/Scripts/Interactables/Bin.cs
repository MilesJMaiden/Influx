// Bin.cs
using UnityEngine;

public class Bin : Interactable
{
    protected override void OnClicked()
    {
        // your Bin-specific click logic here
        Debug.Log("Bin clicked!");
    }
}
