

using System.Collections.Generic;
using UnityEngine;

namespace TFW.AStar
{
    public class AStarMap
    {
        public int MapId { get; private set; }
        public List<AStarArea> Areas => m_Areas;

        public void Init(int mapId, Areas data, Transform pivot)
        {
            m_Areas = new List<AStarArea>();
            m_AreasDict = new Dictionary<int, AStarArea>();
            MapId = mapId;
            m_AreasData = data;
            foreach (var areaInfo in m_AreasData.AreasInfo)
            {
                var area = new AStarArea();
                area.Init(areaInfo, pivot, this);
                m_Areas.Add(area);
                m_AreasDict.Add(areaInfo.AreaId, area);
            }
        }

        public void DeInit()
        {
            foreach (var area in m_Areas)
            {
                area.DeInit();
            }

            m_Areas = null;
            m_AreasDict = null;
            m_AreasData = null;
        }

        public ConnectPoint GetConnectPoint(int index)
        {
            return m_AreasData.ConnectPoints[index];
        }

        public AStarArea GetCityEditorArea()
        {
            foreach (var areasDictValue in m_AreasDict.Values)
            {
                if (areasDictValue.IsCityEditor)
                {
                    return areasDictValue;
                }
            }

            return null;
        }

        public AStarArea GetArea(int areaId)
        {
            return m_AreasDict.TryGetValue(areaId, out var ret) ? ret : null;
        }


        public AStarArea GetPositionArea(Vector3 point, out bool isInArea)
        {
            foreach (var area in m_Areas)
            {
                if (area.IsPointInArea(point))
                {
                    isInArea = true;
                    return area;
                }
            }

            isInArea = false;
            // 找不到就找最近的
            return FindNearestArea(point);
        }

        public AStarArea FindNearestArea(Vector3 point)
        {
            float minDis = float.MaxValue;
            AStarArea ret = null;
            foreach (var area in m_Areas)
            {
                var dis = area.GetDistance(point);
                if (minDis > dis)
                {
                    ret = area;
                    minDis = dis;
                }
            }

            return ret;
        }


        private Areas m_AreasData;
        private List<AStarArea> m_Areas;

        private Dictionary<int, AStarArea> m_AreasDict;
    }
}