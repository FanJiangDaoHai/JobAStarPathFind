

using System.Collections.Generic;
using UnityEngine;

namespace TFW.AStar
{
    [System.Serializable]
    public class AreaConnectInfo
    {
        public bool IsInitiator;
        public int TargetAreaId;
        public List<Vector2Int> SelfPoints;
        public List<Vector2Int> TargetPoints;
        public float Cost;
    }

    [System.Serializable]
    public class ShortestPath
    {
        public int TargetAreaId;
        public List<int> Path;
    }

    [System.Serializable]
    public class ConnectArea
    {
        public int TargetAreaId;
        public List<Connect> Connects;
    }

    [System.Serializable]
    public class AreaData : ScriptableObject
    {
        public List<bool> Points;
        public int XGridNum = 1;
        public int YGridNum = 1;
        public float X = 1;
        public float Y = 1;
        public int AreaId = 1;
        public List<AreaConnectInfo> AreaConnectInfos;
        public List<ConnectArea> ConnectAreas;
        public bool IsCityEditor;
        public Vector2Int CityEditorOffset;
        public Vector2Int CityEditorSize;
    }

    [System.Serializable]
    public class AStarAreaData
    {
        public List<bool> Points;
        public int XGridNum = 1;
        public int YGridNum = 1;
        public float X = 1;
        public float Y = 1;
        public int AreaId = 1;
        public List<AreaConnectInfo> AreaConnectInfos;
        public List<ConnectArea> ConnectAreas;
        public bool IsCityEditor;
        public Vector2Int CityEditorOffset;
        public Vector2Int CityEditorSize;
    }
}