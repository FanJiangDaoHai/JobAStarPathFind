

using System;
using System.Collections.Generic;
using UnityEngine;

namespace TFW.AStar
{
    public class AreaElement : MonoBehaviour
    {
        [Range(1, 100)] public float X;
        [Range(1, 100)] public float Y;
        [Range(1, 300)] public int XGridNum;
        [HideInInspector] public Terrain Terrain;
        [HideInInspector] public int ParentId;
        [HideInInspector] public float G;
        [DisplayOnly] public AStarAreaData AreaData;
        public float GridSize => X > 0 ? X / XGridNum : 1;
        public int YGridNum => (int)Mathf.Floor(Y > 0 ? Y / GridSize : 1);

        [DisplayOnly] public int AreaId = 111111;

        public List<AreaElement> ConnectArea;

        public float MaxHeight;
        public float MinHeight;

        public bool FastLine = false;

        public bool IsCityEditor;
        public Vector2Int CityEditorOffset;
        public Vector2Int CityEditorSize;

        private void OnDrawGizmos()
        {
            if (AreaData == null) return;
            var offset = new Vector3(0, 0.3f, 0);
            Gizmos.color = Color.green;
            var pos1 = transform.position + offset;
            var pos2 = transform.position + new Vector3(0, 0, Y) + offset;
            var pos3 = transform.position + new Vector3(X, 0, Y) + offset;
            var pos4 = transform.position + new Vector3(X, 0, 0) + offset;
            Gizmos.DrawLine(pos1, pos2);
            Gizmos.DrawLine(pos2, pos3);
            Gizmos.DrawLine(pos3, pos4);
            Gizmos.DrawLine(pos4, pos1);
            Gizmos.color = Color.red;
            if (AreaData.AreaConnectInfos == null) return;
            foreach (var areaDataAreaConnectInfo in AreaData.AreaConnectInfos)
            {
                if (areaDataAreaConnectInfo.IsInitiator)
                {
                    var targetArea = transform.parent.Find(areaDataAreaConnectInfo.TargetAreaId.ToString())
                        ?.GetComponent<AreaElement>();
                    if (targetArea != null && areaDataAreaConnectInfo.SelfPoints != null)
                    {
                        for (int i = 0; i < areaDataAreaConnectInfo.SelfPoints.Count; i++)
                        {
                            Gizmos.DrawLine(GetGridWorldPos(areaDataAreaConnectInfo.SelfPoints[i]) + offset,
                                targetArea.GetGridWorldPos(areaDataAreaConnectInfo.TargetPoints[i]) + offset);
                        }
                    }
                }
            }
        }

        public void InitData()
        {
            AreaData.Points ??= new List<bool>();
            AreaData.AreaConnectInfos ??= new List<AreaConnectInfo>();
            X = AreaData.X;
            Y = AreaData.Y;
            XGridNum = AreaData.XGridNum;
            IsCityEditor = AreaData.IsCityEditor;
            CityEditorOffset = AreaData.CityEditorOffset;
            CityEditorSize = AreaData.CityEditorSize;
        }

        public void AlignData()
        {
            int x = XGridNum;
            int y = YGridNum;
            var size = x * y;
            if (AreaData.Points.Count > size)
            {
                AreaData.Points.RemoveRange(size, AreaData.Points.Count - size);
            }

            AreaData.X = X;
            AreaData.Y = Y;
            AreaData.XGridNum = XGridNum;
            AreaData.YGridNum = YGridNum;
            AreaData.AreaId = AreaId;
            AreaData.IsCityEditor = IsCityEditor;
            AreaData.CityEditorOffset = CityEditorOffset;
            AreaData.CityEditorSize = CityEditorSize;
        }

        public Vector3 GetGridLocalPos(Vector2Int pos)
        {
            return new Vector3((pos.x + 0.5f) * GridSize, 0, (pos.y + 0.5f) * GridSize) + transform.localPosition;
        }

        private Vector3 GetGridWorldPos(Vector2Int pos)
        {
            return new Vector3((pos.x + 0.5f) * GridSize, 0, (pos.y + 0.5f) * GridSize) + transform.position;
        }

        public void ClearConnect()
        {
            if (AreaData.AreaConnectInfos != null)
                AreaData.AreaConnectInfos.Clear();
            else
            {
                AreaData.AreaConnectInfos = new List<AreaConnectInfo>();
            }
        }

        public void AreaConnect()
        {
            if (ConnectArea != null && ConnectArea.Count > 0)
            {
                AreaData.AreaConnectInfos = new List<AreaConnectInfo>();
                foreach (var area in ConnectArea)
                {
                    CalcConnect(area);
                }
            }
        }

        private void CalcConnect(AreaElement areaElement)
        {
            var AreaInfo = new AreaConnectInfo()
            {
                IsInitiator = true,
                TargetAreaId = areaElement.AreaId,
                TargetPoints = new List<Vector2Int>(),
                SelfPoints = new List<Vector2Int>(),
                Cost = Vector3.Distance(GetCenter(), areaElement.GetCenter())
            };
            AreaData.AreaConnectInfos.Add(AreaInfo);

            var rect1 = GetRect();
            var rect2 = areaElement.GetRect();
            //左边连接
            if (rect1.x >= rect2.y)
            {
                for (int i = 0; i < YGridNum; i++)
                {
                    var startGrid = new Vector2Int(0, i);
                    var pos = GetGridLocalPos(startGrid);
                    if (pos.z <= rect2.w && pos.z >= rect2.z)
                    {
                        var endGrid = new Vector2Int(areaElement.XGridNum - 1, areaElement.GetY(pos.z));
                        if (GetGridInfo(startGrid) || areaElement.GetGridInfo(endGrid)) continue;
                        AreaInfo.SelfPoints.Add(startGrid);
                        AreaInfo.TargetPoints.Add(endGrid);
                    }
                }
            }
            //下边连接
            else if (rect1.z >= rect2.w)
            {
                for (int i = 0; i < XGridNum; i++)
                {
                    var startGrid = new Vector2Int(i, 0);
                    var pos = GetGridLocalPos(startGrid);
                    if (pos.x <= rect2.y && pos.x >= rect2.x)
                    {
                        var endGrid = new Vector2Int(areaElement.GetX(pos.x), areaElement.YGridNum - 1);
                        if (GetGridInfo(startGrid) || areaElement.GetGridInfo(endGrid)) continue;
                        AreaInfo.SelfPoints.Add(startGrid);
                        AreaInfo.TargetPoints.Add(endGrid);
                    }
                }
            }
            //右边连接
            else if (rect1.y <= rect2.x)
            {
                for (int i = 0; i < YGridNum; i++)
                {
                    var startGrid = new Vector2Int(XGridNum - 1, i);
                    var pos = GetGridLocalPos(startGrid);
                    if (pos.z <= rect2.w && pos.z >= rect2.z)
                    {
                        var endGrid = new Vector2Int(0, areaElement.GetY(pos.z));
                        if (GetGridInfo(startGrid) || areaElement.GetGridInfo(endGrid)) continue;
                        AreaInfo.SelfPoints.Add(startGrid);
                        AreaInfo.TargetPoints.Add(endGrid);
                    }
                }
            }
            //上边连接
            else if (rect1.w <= rect2.w)
            {
                for (int i = 0; i < XGridNum; i++)
                {
                    var startGrid = new Vector2Int(i, YGridNum - 1);
                    var pos = GetGridLocalPos(startGrid);
                    if (pos.x <= rect2.y && pos.x >= rect2.x)
                    {
                        var endGrid = new Vector2Int(areaElement.GetX(pos.x), 0);
                        if (GetGridInfo(startGrid) || areaElement.GetGridInfo(endGrid)) continue;
                        AreaInfo.SelfPoints.Add(startGrid);
                        AreaInfo.TargetPoints.Add(endGrid);
                    }
                }
            }

            areaElement.AreaData.AreaConnectInfos.Add(new AreaConnectInfo()
            {
                IsInitiator = false,
                SelfPoints = new List<Vector2Int>(AreaInfo.TargetPoints),
                TargetPoints = new List<Vector2Int>(AreaInfo.SelfPoints),
                TargetAreaId = AreaId,
                Cost = AreaInfo.Cost,
            });
        }

        private bool GetGridInfo(Vector2Int pos)
        {
            return AreaData.Points[pos.x * YGridNum + pos.y];
        }

        private Vector4 GetRect()
        {
            var pos = transform.localPosition;
            return new Vector4(pos.x, pos.x + X, pos.z, pos.z + Y);
        }

        private Vector3 GetCenter()
        {
            return transform.localPosition + new Vector3(X, 0, Y) / 2;
        }

        private int GetY(float y)
        {
            return (int)Math.Round((y - transform.localPosition.z) / GridSize - 0.5f);
        }

        private int GetX(float x)
        {
            return (int)Math.Round((x - transform.localPosition.x) / GridSize - 0.5f);
        }
    }
}