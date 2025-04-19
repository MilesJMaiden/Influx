// Container.cs
using UnityEngine;

public class Container : Interactable
{
    protected override void OnClicked()
    {
        // your Container-specific click logic here
        Debug.Log("Container clicked!");
    }
}
