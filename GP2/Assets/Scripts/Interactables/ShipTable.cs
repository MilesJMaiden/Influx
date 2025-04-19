// PlanetTable.cs
using UnityEngine;

public class ShipTable : Interactable
{
    protected override void OnClicked()
    {
        // your PlanetTable-specific click logic here
        Debug.Log("PlanetTable clicked!");
    }
}
