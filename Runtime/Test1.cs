// 版权所有[成都创人所爱科技股份有限公司]
// 根据《保密信息使用许可证》获得许可;
// 除非符合许可，否则您不得使用此文件。
// 您可以在以下位置获取许可证副本，链接地址：
// https://wiki.tap4fun.com/display/MO/Confidentiality
// 除非适用法律要求或书面同意，否则保密信息按照使用许可证要求使用，不附带任何明示或暗示的保证或条件。
// 有关管理权限的特定语言，请参阅许可证副本。

using UnityEngine;

namespace TFW.AStar
{
    public class Test1 : MonoBehaviour
    {
        public GameObject WallSprite;

        private GameObject m_CurWall;

        // Update is called once per frame

        private void Start()
        {
            //Application.targetFrameRate = 60;
        }

        void Update()
        {
            // if (Input.GetMouseButtonDown(0))
            // {
            //     var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //     if (Physics.Raycast(ray, out var hit))
            //     {
            //         RNpcMgr.Instance.TestSetDestination(hit.point);
            //     }
            // }
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.A))
            {
                Destroy(m_CurWall);
                m_CurWall = new GameObject("wall");
                m_CurWall.transform.SetParent(null);
                var area = AStarMgr.Instance.GetMapAreaMgr(AStarMapIDConst.ShopMapId).GetArea(100011);
                for (int i = 0; i < area.Data.Points.Count; i++)
                {
                    if (!area.IsPointIsNotWall(i))
                    {
                        var pos = area.GetRealPosByIndex(i, 1);
                        var s = GameObject.Instantiate(WallSprite, m_CurWall.transform, true);
                        s.transform.position = pos + new Vector3(0, 0.1f, 0);
                    }
                }
            }
            //
            // if (Input.GetKeyDown(KeyCode.B))
            // {
            //     QuestMgr.Test();
            // }
#endif
        }
    }
}