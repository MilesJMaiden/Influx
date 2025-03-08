using UnityEngine;

public enum RoomShape
{
    Square,
    LShaped,
    TShaped
}

[CreateAssetMenu(fileName = "LevelDesignSettings", menuName = "Level/Design Settings", order = 1)]
public class LevelDesignSettings : ScriptableObject
{
    [Header("General Settings")]
    public int roomCount = 10;

    [Header("Room Shape Settings")]
    public RoomShape defaultRoomShape = RoomShape.Square;
    public float probabilitySquare = 0.5f;
    public float probabilityLShaped = 0.3f;
    public float probabilityTShaped = 0.2f;

    [Header("Room Dimension Settings (in tiles)")]
    public Vector2 defaultRoomSize = new Vector2(5, 5);
    public Vector2 minRoomSize = new Vector2(4, 4);
    public Vector2 maxRoomSize = new Vector2(8, 8);

    [Header("Connection Settings")]
    [Range(0f, 1f)]
    public float connectionProbability = 0.7f;
}
