// RockBin.cs
using UnityEngine;

public class RockBin : Interactable
{
    protected override void OnClicked()
    {
        Debug.Log("RockBin clicked!");
        var agent = Agent.SelectedAgent;
        if (agent != null && !agent.IsCarryingRock && !agent.IsCarryingRefined)
        {
            agent.CommandPickupRock(this);
        }
    }
}
