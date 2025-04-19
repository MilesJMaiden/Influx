// RockBin.cs
using UnityEngine;

public class RockBin : Interactable
{
    protected override void OnClicked()
    {
        // your RockBin-specific click logic here
        Debug.Log("RockBin clicked!");
    }
}
