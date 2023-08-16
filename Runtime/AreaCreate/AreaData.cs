// 版权所有[成都创人所爱科技股份有限公司]
// 根据《保密信息使用许可证》获得许可;
// 除非符合许可，否则您不得使用此文件。
// 您可以在以下位置获取许可证副本，链接地址：
// https://wiki.tap4fun.com/display/MO/Confidentiality
// 除非适用法律要求或书面同意，否则保密信息按照使用许可证要求使用，不附带任何明示或暗示的保证或条件。
// 有关管理权限的特定语言，请参阅许可证副本。

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