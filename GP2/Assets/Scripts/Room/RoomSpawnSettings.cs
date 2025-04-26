using UnityEngine;

[System.Serializable]
public class RoomSpawnSettings
{
    // An optional identifier for this room type.
    public string roomIdentifier;
    // The list of spawn entries for this room.
    public SpawnEntry[] spawnEntries;
}
