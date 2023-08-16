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