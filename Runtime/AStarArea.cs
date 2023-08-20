// 版权所有[成都创人所爱科技股份有限公司]
// 根据《保密信息使用许可证》获得许可;
// 除非符合许可，否则您不得使用此文件。
// 您可以在以下位置获取许可证副本，链接地址：
// https://wiki.tap4fun.com/display/MO/Confidentiality
// 除非适用法律要求或书面同意，否则保密信息按照使用许可证要求使用，不附带任何明示或暗示的保证或条件。
// 有关管理权限的特定语言，请参阅许可证副本。

using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Pool;

namespace TFW.AStar
{
    public class AStarArea
    {
        public int ID => m_AreaId;
        public bool IsCityEditor => m_AreaData.IsCityEditor;

        public Vector2Int CityEditorOffset => m_AreaData.CityEditorOffset;
        public AStarAreaData Data => m_AreaData;

        public float GridSize => m_GridSize;

        public int MapID => map.MapId;

        public NativeArray<Point> AreaPointData => m_AreaPointData;

        public void Init(AreaInfo areaInfo, Transform pivot, AStarMap mgr)
        {
            m_Pivot = pivot;
            map = mgr;
            m_AreaId = areaInfo.AreaId;
            m_AreaData = areaInfo.AreaData;
            m_LocalPosition = areaInfo.Position;
            m_GridSize = m_AreaData.X / m_AreaData.XGridNum;
            m_AreaPointData = new NativeArray<Point>(m_AreaData.Points.Count, Allocator.Persistent);
            m_AgentsInArea = new Dictionary<int, AStarAgent>();
            m_NeedCompleteMission = new List<AStarMission>();
            for (var index = 0; index < m_AreaData.Points.Count; index++)
            {
                var areaDataPoint = m_AreaData.Points[index];
                GetXY(index, out var x, out var y);
                m_AreaPointData[index] = new Point()
                {
                    X = x,
                    Y = y,
                    Weight = 1,
                    ParentIndex = -1,
                    IsWall = areaDataPoint,
                    CloseFlag = false,
                    OpenFlag = false,
                };
            }

            //m_AreaData.Points.Clear();
        }

        public void DeInit()
        {
            m_AreaPointData.Dispose();
        }


        public Vector3 GetRealPosByIndex(int index, int sensitivity)
        {
            GetXY(index, out var x, out var y);

            var ret = GetRealPosByXY(x, y);
            if (sensitivity > 1)
            {
                ret = (ret + GetRealPosByXY(x + sensitivity - 1, y + sensitivity - 1)) / 2;
            }

            return ret;
        }

        public Vector3 GetRealPosByXY(int x, int y)
        {
            return new Vector3((x + 0.5f) * m_GridSize, 0, (y + 0.5f) * m_GridSize) + m_WorldPosition;
        }

        public Vector3 GetRealPos(float x, float y)
        {
            return new Vector3(x * m_GridSize, 0, y * m_GridSize) + m_WorldPosition;
        }

        public Vector3 GetRealLocalPos(float x, float y)
        {
            return new Vector3(x * m_GridSize, 0, y * m_GridSize) + m_LocalPosition;
        }

        public bool IsPointInArea(Vector3 point)
        {
            return point.x >= m_WorldPosition.x && point.x <= m_WorldPosition.x + m_AreaData.X &&
                   point.z >= m_WorldPosition.z &&
                   point.z <= m_WorldPosition.z + m_AreaData.Y;
        }

        public float GetDistance(Vector3 point)
        {
            float ret = 0;
            if (point.x <= m_WorldPosition.x || point.x >= m_WorldPosition.x + m_AreaData.X)
            {
                ret += Mathf.Abs(m_WorldPosition.z + m_AreaData.Y / 2 - point.z) - m_AreaData.Y / 2;
            }

            if (point.z <= m_WorldPosition.y || point.z >= m_WorldPosition.y + m_AreaData.Y)
            {
                ret += Mathf.Abs(m_WorldPosition.x + m_AreaData.X / 2 - point.x) - m_AreaData.X / 2;
            }

            return ret;
        }

        public void GetAreaTasks(Vector3 curPos, Vector3 destination, List<Vector2Int> pointAround,
            int curIndex, Queue<AStarTask> ret)
        {
            ret.Clear();
            var endArea = map.GetPositionArea(destination, out var isInArea);
            bool isAreaConnect = true;
            if (this != endArea)
            {
                var minCost = float.MaxValue;
                Connect minConnect = null;
                //遍历当前区域的所有连接区域点
                foreach (var areaDataConnectArea in m_AreaData.ConnectAreas)
                {
                    if (areaDataConnectArea.TargetAreaId == endArea.ID)
                    {
                        //选择当前区域到目标区域的最小估计代价路径
                        foreach (var connect in areaDataConnectArea.Connects)
                        {
                            var connectPoint = map.GetConnectPoint(connect.Points[0]);
                            var nextConnectPoint = map.GetConnectPoint(connect.NextPoint);
                            var cost = Vector3.Distance(curPos, connectPoint.Position + m_Pivot.transform.position) +
                                       connect.Cost +
                                       Vector3.Distance(destination,
                                           nextConnectPoint.Position + m_Pivot.transform.position);
                            if (minCost > cost)
                            {
                                minCost = cost;
                                minConnect = connect;
                            }
                        }
                    }
                }

                //找到目标路径创建Tasks
                if (minConnect != null)
                {
                    var curAreaId = m_AreaId;
                    foreach (var point in minConnect.Points)
                    {
                        var connectPoint = map.GetConnectPoint(point);
                        //下一区域对应点
                        var nextList = connectPoint.AreaId1 == curAreaId
                            ? connectPoint.Area2Points
                            : connectPoint.Area1Points;
                        //当前区域对应点
                        var curList = connectPoint.AreaId1 == curAreaId
                            ? connectPoint.Area1Points
                            : connectPoint.Area2Points;
                        var nextAreaId = connectPoint.AreaId1 == curAreaId
                            ? connectPoint.AreaId2
                            : connectPoint.AreaId1;
                        var nextArea = map.GetArea(nextAreaId);
                        Vector3 nextPos = Vector3.zero;
                        float minLen = float.MaxValue;
                        bool isFind = false;
                        var curArea = map.GetArea(curAreaId);
                        var nextAreaStarIndex = 0;
                        for (var i = 0; i < nextList.Count; i++)
                        {
                            var p = nextList[i];
                            var index = nextArea.GetIndex(p.x, p.y);
                            var cIndex = curArea.GetIndex(curList[i].x, curList[i].y);
                            //先检测是否可以通过
                            if (!nextArea.IsPointIsNotWall(index) ||
                                (cIndex != curIndex && !curArea.IsPointIsNotWall(cIndex)) ||
                                (curArea != this && !curArea.IsPointIsNotWall(cIndex)))
                                continue;
                            var realPos = nextArea.GetRealPosByIndex(index, 1);
                            var dis = Vector3.Distance(curPos, realPos);
                            if (dis < minLen)
                            {
                                isFind = true;
                                nextPos = realPos;
                                minLen = dis;
                                nextAreaStarIndex = index;
                            }
                        }

                        //没找到找最近的
                        if (!isFind)
                        {
                            for (var i = 0; i < nextList.Count; i++)
                            {
                                var p = nextList[i];
                                var index = nextArea.GetIndex(p.x, p.y);
                                var realPos = nextArea.GetRealPosByIndex(index, 1);
                                var dis = Vector3.Distance(curPos, realPos);
                                if (dis < minLen)
                                {
                                    nextPos = realPos;
                                    minLen = dis;
                                    nextAreaStarIndex = index;
                                }
                            }
                        }

                        var task = AStarPool.TaskPool.Get();
                        task.StartPos = curPos;
                        task.Destination = nextPos;
                        task.AreaId = curAreaId;
                        task.NextAreaStartPointId = nextAreaStarIndex;
                        ret.Enqueue(task);
                        curPos = nextPos;
                        curAreaId = nextAreaId;
                    }
                }
                else
                {
                    //两个区域没有路径连接直接在本区域内找与目标点最近的格子寻
                    endArea = this;
                    isAreaConnect = false;
                }
            }

            if (!isInArea || !isAreaConnect)
                destination = endArea.GetRealPosByIndex(endArea.GetIndex(destination, 1), 1);
            var newTask = AStarPool.TaskPool.Get();
            newTask.StartPos = curPos;
            newTask.Destination = destination;
            newTask.AreaId = endArea.m_AreaId;
            newTask.PointsAround = pointAround;
            ret.Enqueue(newTask);
        }


        public void SetPoint(int index, bool isWall, AStarAgent agent = null)
        {
            if (!m_AreaPointData.IsCreated) return;
            if (index >= m_AreaPointData.Length)
            {
                Debug.LogError($"{index} 长度超出地图{m_AreaId}长度 {m_AreaPointData.Length}");
                return;
            }

            if (agent != null && m_AgentsInArea != null)
            {
                GetXY(index, out var x, out var y);
                for (int i = x; i < x + agent.Sensitivity; i++)
                {
                    for (int j = y; j < y + agent.Sensitivity; j++)
                    {
                        var newIndex = GetIndex(i, j);
                        if (newIndex >= m_AreaPointData.Length)
                        {
                            continue;
                        }

                        if (isWall)
                        {
                            m_AgentsInArea[newIndex] = agent;
                        }
                        else
                        {
                            m_AgentsInArea.Remove(newIndex);
                        }

                        SetPointWall(newIndex, isWall);
                    }
                }
            }

            SetPointWall(index, isWall);
        }

        private void SetPointWall(int index, bool isWall)
        {
            var point = m_AreaPointData[index];
            point.IsWall = isWall;
            m_AreaPointData[index] = point;
        }

        public bool IsCanSetRect(RectInt oldRect, RectInt rect)
        {
            for (int i = rect.xMin; i < rect.xMax; i++)
            {
                for (int j = rect.yMin; j < rect.yMax; j++)
                {
                    var index = GetIndex(i, j);
                    if (index >= m_AreaPointData.Length || index < 0)
                    {
                        return false;
                    }

                    if (i >= oldRect.xMin && i < oldRect.xMax && j >= oldRect.yMin && j < oldRect.yMax) continue;
                    if (index >= m_AreaPointData.Length || m_AreaPointData[index].IsWall) return false;
                }
            }

            return true;
        }

        public bool IsCanSetRect(RectInt rect)
        {
            for (int i = rect.xMin; i < rect.xMax; i++)
            {
                for (int j = rect.yMin; j < rect.yMax; j++)
                {
                    var index = GetIndex(i, j);
                    if (index >= m_AreaPointData.Length || index < 0)
                    {
                        return false;
                    }

                    if (!IsPointIsNotWall(index)) return false;
                    // bool isChange = m_ChangeDict.ContainsKey(index);
                    // if (index >= m_MapData.Length || (m_MapData[index].IsWall && !isChange) ||
                    //     (isChange && m_ChangeDict[index])) return false;
                }
            }

            return true;
        }

        public bool IsCanSetRectWithNoAgent(RectInt oldRect, RectInt rect)
        {
            for (int i = rect.xMin; i < rect.xMax; i++)
            {
                for (int j = rect.yMin; j < rect.yMax; j++)
                {
                    var index = GetIndex(i, j);
                    if (index >= m_AreaPointData.Length || index < 0)
                    {
                        return false;
                    }

                    if (m_AgentsInArea.ContainsKey(index)) continue;
                    if (i >= oldRect.xMin && i < oldRect.xMax && j >= oldRect.yMin && j < oldRect.yMax) continue;
                    if (index >= m_AreaPointData.Length || m_AreaPointData[index].IsWall) return false;
                }
            }

            return true;
        }


        public void SetRectWall(RectInt rect)
        {
            List<AStarAgent> agentListPool = ListPool<AStarAgent>.Get();
            for (int i = rect.xMin; i < rect.xMax; i++)
            {
                for (int j = rect.yMin; j < rect.yMax; j++)
                {
                    var index = GetIndex(i, j);
                    SetPoint(index, true);
                    if (m_AgentsInArea.TryGetValue(index, out var agent))
                    {
                        //if(index != agent.CurrentIndex)Debug.LogError($"{index} != {agent.CurrentIndex}");
                        agentListPool.Add(agent);
                    }
                }
            }

            foreach (var aStarAgent in agentListPool)
            {
                var index = aStarAgent.CurrentIndex;
                var newIndex = FindRectNotWall(index, aStarAgent.Sensitivity, aStarAgent.Sensitivity);
                if (index != newIndex)
                {
                    GetXY(index, out var x, out var y);
                    for (int i = x; i < x + aStarAgent.Sensitivity; i++)
                    {
                        for (int j = y; j < y + aStarAgent.Sensitivity; j++)
                        {
                            var setIndex = GetIndex(i, j);
                            if (setIndex >= m_AreaPointData.Length)
                            {
                                continue;
                            }

                            m_AgentsInArea.Remove(newIndex);
                        }
                    }

                    aStarAgent.RemoveAndSetNewPoint(newIndex);
                }
            }

            ListPool<AStarAgent>.Release(agentListPool);
        }

        public void RemoveRectWall(RectInt rect)
        {
            for (int i = rect.xMin; i < rect.xMax; i++)
            {
                for (int j = rect.yMin; j < rect.yMax; j++)
                {
                    var index = GetIndex(i, j);
                    SetPoint(index, false);
                }
            }
        }

        public bool CanPlaceRectArray(List<RectInt> oldRectList, List<RectInt> newRectList)
        {
            HashSet<int> oldRects = new HashSet<int>();
            foreach (var rect in oldRectList)
            {
                for (int i = rect.xMin; i < rect.xMax; i++)
                {
                    for (int j = rect.yMin; j < rect.yMax; j++)
                    {
                        var index = GetIndex(i, j);
                        oldRects.Add(index);
                    }
                }
            }

            foreach (var rect in newRectList)
            {
                for (int i = rect.xMin; i < rect.xMax; i++)
                {
                    for (int j = rect.yMin; j < rect.yMax; j++)
                    {
                        var index = GetIndex(i, j);
                        if (oldRects.Contains(index)) continue;
                        if (index >= m_AreaPointData.Length || m_AreaPointData[index].IsWall) return false;
                    }
                }
            }

            return true;
        }

        public void SetRectWallArray(List<RectInt> rects)
        {
            foreach (var rect in rects)
            {
                SetRectWall(rect);
            }
        }

        public void RemoveRectWallArray(List<RectInt> rects)
        {
            foreach (var rect in rects)
            {
                RemoveRectWall(rect);
            }
        }

        public int GetIndex(Vector3 point, int sensitivity)
        {
            if (sensitivity > 1)
            {
                sensitivity -= 1;
                point = point - new Vector3(sensitivity * m_GridSize / 2, 0, sensitivity * m_GridSize / 2);
            }

            int x = Mathf.Clamp((int)Mathf.Floor((point.x - m_WorldPosition.x) / m_GridSize), 0,
                m_AreaData.XGridNum - 1);
            int y = Mathf.Clamp((int)Mathf.Floor((point.z - m_WorldPosition.z) / m_GridSize), 0,
                m_AreaData.YGridNum - 1);
            return GetIndex(x, y);
        }

        public List<int> GetIndexList(Vector3 point, int sensitivity)
        {
            List<int> ret = new List<int>();
            if (sensitivity > 1)
            {
                point = point - new Vector3((sensitivity - 1) * m_GridSize / 2, 0, (sensitivity - 1) * m_GridSize / 2);
            }

            int x = Mathf.Clamp((int)Mathf.Floor((point.x - m_WorldPosition.x) / m_GridSize), 0,
                m_AreaData.XGridNum - 1);
            int y = Mathf.Clamp((int)Mathf.Floor((point.z - m_WorldPosition.z) / m_GridSize), 0,
                m_AreaData.YGridNum - 1);
            if (sensitivity == 1)
            {
                ret.Add(GetIndex(x, y));
            }
            else
            {
                for (int i = x; i < x + sensitivity; i++)
                {
                    for (int j = y; j < y + sensitivity; j++)
                    {
                        ret.Add(GetIndex(i, j));
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// 返回可以越界的格子坐标
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Vector2Int GetGridPosUnClamp(Vector3 point)
        {
            int x = (int)Mathf.Floor((point.x - m_WorldPosition.x) / m_GridSize);
            int y = (int)Mathf.Floor((point.z - m_WorldPosition.z) / m_GridSize);
            return new Vector2Int(x, y);
        }

        public Vector2Int GetGridPos(Vector3 point)
        {
            int x = Mathf.Clamp((int)Mathf.Floor((point.x - m_WorldPosition.x) / m_GridSize), 0,
                m_AreaData.XGridNum - 1);
            int y = Mathf.Clamp((int)Mathf.Floor((point.z - m_WorldPosition.z) / m_GridSize), 0,
                m_AreaData.YGridNum - 1);
            return new Vector2Int(x, y);
        }

        public bool IsPointIsNotWall(int index)
        {
            return m_AreaPointData.Length > index && !m_AreaPointData[index].IsWall;
        }


        public bool IsPointCanMove(int curIndex, int nextIndex, int sensitive = 1)
        {
            GetXY(curIndex, out var x1, out var y1);
            GetXY(nextIndex, out var x2, out var y2);
            if (sensitive > 1)
            {
                for (int i = x2; i < x2 + sensitive; i++)
                {
                    for (int j = y2; j < y2 + sensitive; j++)
                    {
                        if (i >= x1 && i < x1 + sensitive && j >= y1 && j < y1 + sensitive) continue;
                        var index = GetIndex(i, j);
                        if (index > m_AreaPointData.Length) continue;
                        if (!IsPointIsNotWall(index)) return false;
                    }
                }

                return true;
            }

            var offsetX = x2 - x1;
            var offsetY = y2 - y1;
            var p1 = GetIndex(x1 + offsetX, y1);
            var p2 = GetIndex(x1, y1 + offsetY);
            return IsPointIsNotWall(nextIndex) && (p1 == curIndex || IsPointIsNotWall(p1)) &&
                   (p2 == curIndex || IsPointIsNotWall(p2));
        }

        public int GetIndex(int x, int y)
        {
            return x * m_AreaData.YGridNum + y;
        }


        public void GetXY(int index, out int x, out int y)
        {
            x = index / m_AreaData.YGridNum;
            y = index % m_AreaData.YGridNum;
        }


        public AStarAgent GetNpcByIndex(int index)
        {
            if (m_AgentsInArea.TryGetValue(index, out var agent))
            {
                return agent;
            }

            return null;
        }


        public Rect GetAreaRect()
        {
            return new Rect(m_WorldPosition.x, m_WorldPosition.z, m_AreaData.XGridNum * m_GridSize,
                m_AreaData.YGridNum * m_GridSize);
        }

        private int FindNotWall(int index)
        {
            HashSet<int> book = HashSetPool<int>.Get();
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(index);
            book.Add(index);
            while (queue.Count > 0)
            {
                index = queue.Dequeue();
                if (IsPointIsNotWall(index)) return index;
                for (var i = 0; i < 4; i++)
                {
                    GetXY(index, out var x, out var y);
                    var newX = x + m_Dir[i, 0];
                    var newY = y + m_Dir[i, 1];
                    var nextIndex = GetIndex(newX, newY);
                    if (nextIndex >= m_AreaPointData.Length) continue;
                    if (!book.Contains(nextIndex))
                    {
                        if (IsPointIsNotWall(index)) return index;
                        book.Add(nextIndex);
                        queue.Enqueue(nextIndex);
                    }
                }
            }

            HashSetPool<int>.Release(book);
            return -1;
        }


        public int FindRectNotWall(int index, int w, int h)
        {
            Queue<int> queue = new Queue<int>();
            var book = new HashSet<int>();
            queue.Enqueue(index);
            book.Add(index);
            while (queue.Count > 0)
            {
                index = queue.Dequeue();
                GetXY(index, out var _x, out var _y);
                if (IsCanSetRect(new RectInt(_x, _y, w, h))) return index;
                for (var i = 0; i < 4; i++)
                {
                    GetXY(index, out var x, out var y);
                    var newX = x + m_Dir[i, 0];
                    var newY = y + m_Dir[i, 1];
                    var nextIndex = GetIndex(newX, newY);
                    if (nextIndex >= m_AreaPointData.Length) continue;
                    if (!book.Contains(nextIndex))
                    {
                        GetXY(index, out _x, out _y);
                        if (IsCanSetRect(new RectInt(_x, _y, w, h))) return index;
                        book.Add(nextIndex);
                        queue.Enqueue(nextIndex);
                    }
                }
            }

            return -1;
        }


        private Vector3 m_LocalPosition;
        private AStarAreaData m_AreaData;
        private float m_GridSize;
        private int m_AreaId;
        private NativeArray<Point> m_AreaPointData;
        private List<AStarMission> m_NeedCompleteMission;
        private Transform m_Pivot;
        private Vector3 m_WorldPosition => m_LocalPosition + m_Pivot.position;

        private AStarMap map;

        //private Dictionary<int, bool> m_ChangeDict;
        private Dictionary<int, AStarAgent> m_AgentsInArea;

        private static readonly int[,] m_Dir = new int[,]
        {
            { 0, 1 }, { 0, -1 }, { 1, 0 }, { -1, 0 }
        };
    }
}