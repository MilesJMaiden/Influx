using UnityEngine;

[System.Serializable]
public class RoomData
{
    public Vector2Int gridPosition;
    public RoomShape shape;
    public RoomConnections connections;
    public Vector2 dimensions; // Width and height in tiles

    public RoomData(Vector2Int gridPos, RoomShape shape, RoomConnections connections, Vector2 dimensions)
    {
        this.gridPosition = gridPos;
        this.shape = shape;
        this.connections = connections;
        this.dimensions = dimensions;
    }
}
