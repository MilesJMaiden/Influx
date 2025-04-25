// Container.cs
using UnityEngine;

public class Container : Interactable
{
    [Tooltip("Drop your 'AlienSpawnPoint' child here, or leave blank to auto-find.")]
    public GameObject alienSpawnPoint;

    protected override void Awake()
    {
        base.Awake();
        if (alienSpawnPoint == null)
        {
            var t = transform.Find("AlienSpawnPoint");
            if (t != null) alienSpawnPoint = t.gameObject;
        }
    }

    protected override void OnClicked()
    {
        base.OnClicked();
        // If the currently selected agent is carrying an alien, tell it to drop here:
        var agent = Agent.SelectedAgent;
        if (agent != null && agent.isCarryingAlien)
        {
            agent.CommandDropAlien(this);
        }
    }
}
