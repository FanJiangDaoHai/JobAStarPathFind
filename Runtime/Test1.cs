

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