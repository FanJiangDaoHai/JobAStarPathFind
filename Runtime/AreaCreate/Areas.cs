

using System.Collections.Generic;
using UnityEngine;

namespace TFW.AStar
{
    [System.Serializable]
    public class AreaInfo
    {
        public int AreaId;
        public Vector3 Position;
        public AStarAreaData AreaData;
    }

    [System.Serializable]
    public class ConnectPoint
    {
        public int PointId;
        public Vector3 Position;
        public int AreaId1;
        public int AreaId2;
        public int ParentId;
        public float G;
        public List<Vector2Int> Area1Points;
        public List<Vector2Int> Area2Points;
        public List<Connect> Connects;
    }

    [System.Serializable]
    public class Connect
    {
        public int NextPoint;
        public float Cost;
        public List<int> Points;
    }

    public class Areas : ScriptableObject
    {
        public List<AreaInfo> AreasInfo;
        public List<ConnectPoint> ConnectPoints;
    }
}