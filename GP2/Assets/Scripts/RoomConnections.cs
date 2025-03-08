using System;

[System.Serializable]
public struct RoomConnections
{
    public bool top;
    public bool bottom;
    public bool left;
    public bool right;

    public RoomConnections(bool top, bool bottom, bool left, bool right)
    {
        this.top = top;
        this.bottom = bottom;
        this.left = left;
        this.right = right;
    }
}
