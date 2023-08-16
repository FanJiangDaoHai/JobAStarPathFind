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
    /// <summary>
    /// 会存在多套地图所以加一个MapMgr 统一管理
    /// </summary>
    public class AStarMgr : Singleton<AStarMgr>
    {
        public IReadOnlyDictionary<int, AStarMap> Maps => m_Maps;


        public void Init()
        {
            if (m_Maps != null)
            {
                return;
            }

            if (!m_Root)
            {
                m_Root = new GameObject("AStarRoot");
                m_Root.AddComponent<AStarMissionMgr>();
                Object.DontDestroyOnLoad(m_Root);
            }

            m_Maps = new Dictionary<int, AStarMap>();
        }

        public void DeInit()
        {
            if (m_Maps == null) return;
            foreach (var areaMgr in m_Maps.Values)
            {
                areaMgr.DeInit();
            }

            m_Maps = null;
        }

        public void RemoveMap(int mapId)
        {
            if (!Maps.ContainsKey(mapId)) return;
            m_Maps[mapId].DeInit();
            m_Maps.Remove(mapId);
        }

        public void AddMap(int mapId, Areas mapData, Transform pivot)
        {
            RemoveMap(mapId);
            AStarMap map = new AStarMap();
            map.Init(mapId, mapData, pivot);
            m_Maps.Add(mapId, map);
        }


        public AStarMap GetMapAreaMgr(int mapId)
        {
            if (m_Maps == null) return null;
            m_Maps.TryGetValue(mapId, out var ret);
            return ret;
        }


        private Dictionary<int, AStarMap> m_Maps;
        private GameObject m_Root;
        
    }
}