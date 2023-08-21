

using System.Collections.Generic;
using UnityEngine;

namespace TFW.AStar
{
    public class AStarTask
    {
        public Vector3 StartPos;
        public Vector3 Destination;
        public int AreaId;
        public bool IsFind;
        public List<int> Path;
        public List<Vector2Int> PointsAround;
        public int NextAreaStartPointId;
    }
}