using System.Collections.Generic;
using UnityEngine;

public interface IRoomGenerator
{
    void GenerateRoom();
    HashSet<Vector3> GetWindowWallPositions();
}
