// Container.cs
using UnityEngine;

public class Container : Interactable
{
    public GameObject alienSpawnPoint;

    protected override void OnClicked()
    {
        // your Container-specific click logic here
        Debug.Log("Container clicked!");
    }
}
