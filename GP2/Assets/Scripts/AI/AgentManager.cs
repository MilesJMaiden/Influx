using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class AgentManager : MonoBehaviour
{
    public static AgentManager Instance { get; private set; }
    private List<Agent> activeAgents = new List<Agent>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Spawns a number of agents for a given room.
    /// This version instantiates each agent at a specified height (e.g., y = 5), then uses a downward raycast
    /// to detect the floor, and finally snaps the agent onto the NavMesh.
    /// </summary>
    /// <param name="roomContainer">The transform of the room container.</param>
    /// <param name="roomTileDimensions">Room dimensions in tiles (width, height).</param>
    /// <param name="agentEntry">A SpawnEntry containing the agent prefab and the number of agents to spawn.</param>
    /// <param name="additionalMargin">Extra inset from room edges (in world units) to define a safe area.</param>
    public void SpawnAgentsForRoom(Transform roomContainer, Vector2 roomTileDimensions, SpawnEntry agentEntry, float additionalMargin = 2.5f)
    {
        // Use the same TileSize as in your RoomObjectSpawner.
        float TileSize = 5f;
        float roomWidth = roomTileDimensions.x * TileSize;
        float roomHeight = roomTileDimensions.y * TileSize;

        // Define the safe area (in room-local XZ space).
        Vector2 safeMin = new Vector2(additionalMargin, additionalMargin);
        Vector2 safeMax = new Vector2(roomWidth - additionalMargin, roomHeight - additionalMargin);

        for (int i = 0; i < agentEntry.count; i++)
        {
            float randomX = Random.Range(safeMin.x, safeMax.x);
            float randomZ = Random.Range(safeMin.y, safeMax.y);
            // Instantiate the agent at an elevated Y position (here, 5 units)
            Vector3 spawnPosLocal = new Vector3(randomX, 5f, randomZ);
            Vector3 candidateWorldPos = roomContainer.TransformPoint(spawnPosLocal);

            // Fire a downward raycast from the candidate position to detect the floor.
            RaycastHit hit;
            if (Physics.Raycast(candidateWorldPos, Vector3.down, out hit, 20f))
            {
                candidateWorldPos = hit.point;
            }
            else
            {
                Debug.LogWarning("AgentManager: Failed to detect floor via raycast near candidate position: " + candidateWorldPos);
            }

            // Snap the candidate position onto the NavMesh.
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(candidateWorldPos, out navHit, 5f, NavMesh.AllAreas))
            {
                candidateWorldPos = navHit.position;
            }
            else
            {
                Debug.LogWarning("AgentManager: Unable to find a valid NavMesh position near: " + candidateWorldPos);
            }

            // Instantiate the agent at the final position.
            GameObject agentGO = Instantiate(agentEntry.prefab, candidateWorldPos, Quaternion.identity, roomContainer);
            Agent agentComp = agentGO.GetComponent<Agent>();
            if (agentComp != null)
            {
                activeAgents.Add(agentComp);
            }
        }
    }

    public List<Agent> GetActiveAgents()
    {
        return activeAgents;
    }
}
