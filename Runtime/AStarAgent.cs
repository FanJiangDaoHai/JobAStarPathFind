// 版权所有[成都创人所爱科技股份有限公司]
// 根据《保密信息使用许可证》获得许可;
// 除非符合许可，否则您不得使用此文件。
// 您可以在以下位置获取许可证副本，链接地址：
// https://wiki.tap4fun.com/display/MO/Confidentiality
// 除非适用法律要求或书面同意，否则保密信息按照使用许可证要求使用，不附带任何明示或暗示的保证或条件。
// 有关管理权限的特定语言，请参阅许可证副本。

using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TFW.AStar
{
    public class AStarAgent : MonoBehaviour
    {
        public float Speed = 1;
        public bool IsStop => m_IsStop;

        //是否到达目的地
        public bool IsArrive => m_IsArrive;

        public int CurrentIndex => m_CurPointIndex;

        public int CurrentAreaId => m_CurArea?.ID ?? 0;

        //标记没有找到终点的状态
        public bool IsNotFind => m_IsNotFind;

        //判断是否在任务中
        public bool IsInTask => m_IsInTask;

        //是否在Move中
        public bool IsMotion => !m_Sleep && !m_IsStop && !m_IsMoveEnd;

        //被原地阻塞次数
        public int Clog => m_Clog;

        public int MapID => m_MapId;

        //产生碰撞的回调
        public event Action OnBump;

        public float Radius = 0.5f;

        public int Sensitivity { get; private set; } = 1;


        private void Awake()
        {
            ResetData();
        }

        /// <summary>
        /// 设置目的地
        /// </summary>
        /// <param name="position"></param>
        /// <param name="pointAround">可以选取周围的点</param>
        /// <param name="mustReach">是否必达（可以无限等待直到到达目的地）</param>
        public void SetDestination(Vector3 position, List<Vector2Int> pointAround = null, bool mustReach = false)
        {
            if (m_CurArea == null)
            {
                Debug.LogError("未指定地图数据");
                return;
            }

            m_IsMustReach = mustReach;
            m_IsArrive = false;
            m_Sleep = false;
            m_IsMoveEnd = true;
            m_IsNotFind = false;
            m_CurDestination = position;
            m_CurPointsAround = pointAround;
            m_IsInTask = true;
            m_Clog = 0;
            //可能当前位置存在偏差，所以重新计算curIndex
            SetCurPointIndex(m_CurArea.GetIndex(transform.position, Sensitivity));
            m_Tasks ??= AStarPool.TaskQueuePool.Get();
            m_CurArea.GetAreaTasks(transform.position, position, pointAround, m_CurPointIndex, m_Tasks);
            var t = m_Tasks.Peek();
            AStarMissionMgr.Instance.AddMission(m_CurArea, this, t);
        }

        public void SetPath(AStarTask task)
        {
            if (task == m_Tasks.Peek() && task.AreaId == m_CurArea.ID)
            {
                m_IsNotFind = !task.IsFind;
                m_Tasks.Dequeue();
                SetCurTask(task);
                //被堵住了
                if (!task.IsFind && task.Path.Count == 0)
                {
                    m_Clog++;
                    //设置被阻塞次数超过一定数值便不再寻路
                    if (m_Clog < m_MaxClogTime || m_IsMustReach)
                    {
                        SetSleep();
                    }
                    else
                    {
                        m_IsInTask = false;
                        m_Clog = 0;
                    }

                    return;
                }

                m_Clog = 0;
                m_NextIndex = 0;
                if (task.Path.Count != 0)
                {
                    m_NextPosition = m_CurArea.GetRealPosByIndex(task.Path[m_NextIndex], Sensitivity);
                    //判断下一个节点是否可走，不可走就睡眠
                    if (m_CurArea.IsPointCanMove(m_CurPointIndex, task.Path[m_NextIndex], Sensitivity))
                    {
                        SetCurPointIndex(task.Path[m_NextIndex]);
                    }
                    else
                    {
                        //这里可以不设，不显突兀。
                        //transform.position = m_CurArea.GetRealPos(task.Path[m_NextIndex - 1]);
                        SetCurPointIndex(m_CurArea.GetIndex(transform.position, Sensitivity));
                        SetSleep(Random.Range(0.1f, 0.5f));
                        return;
                    }
                }
                else if (task.IsFind)
                {
                    m_NextPosition = task.Destination;
                    if (m_Tasks.Count == 0 && m_CurPointsAround != null && m_CurPointsAround.Count != 0)
                    {
                        m_IsArrive = true;
                        m_IsInTask = false;
                        transform.forward = (m_NextPosition - transform.position).normalized;
                        return;
                    }
                }

                transform.forward = (m_NextPosition - transform.position).normalized;

                //开始移动
                m_IsMoveEnd = false;
            }
        }

        //停止当前的一切寻路行为包括睡眠
        public void Stop()
        {
            m_Sleep = false;
            m_IsStop = true;
        }

        public void Launch()
        {
            m_IsStop = false;
        }


        public void SetCurMap(int mapId)
        {
            if (m_MapId == mapId) return;
            var area = AStarMgr.Instance.GetMapAreaMgr(mapId)
                ?.GetPositionArea(transform.position, out var isInArea);
            if (area == null)
            {
                Debug.LogError("未初始化地图数据");
                return;
            }

            Sensitivity = Mathf.Max(Mathf.RoundToInt(Radius / area.GridSize), 1);
            var index = area.GetIndex(transform.position, Sensitivity);
            var nextIndex = area.FindRectNotWall(index, Sensitivity, Sensitivity);
            if (nextIndex != index)
            {
                transform.position = area.GetRealPosByIndex(nextIndex, Sensitivity);
            }

            SetCurArea(area, transform.position);
            m_MapId = mapId;
        }


        public int GetPointIndex(Vector2Int pos)
        {
            if (m_CurArea == null) return 0;
            return m_CurArea.GetIndex(pos.x, pos.y);
        }

        public List<int> GetPointIndexList(Vector3 pos)
        {
            return m_CurArea.GetIndexList(pos, Sensitivity);
        }


        public void RemoveAndSetNewPoint(int index)
        {
            SetPoint(index, true);
            m_CurPointIndex = index;
            transform.position = m_CurArea.GetRealPosByIndex(index, Sensitivity);
            if (m_IsInTask) SetDestination(m_CurDestination, m_CurPointsAround, m_IsMustReach);
        }


        public void SetPosition(Vector3 pos)
        {
            ResetData();
            transform.position = pos;
            var area = AStarMgr.Instance.GetMapAreaMgr(m_MapId)
                ?.GetPositionArea(transform.position, out var isInArea);
            SetCurArea(area, transform.position);
        }

        private void SetCurPointIndex(int index)
        {
            if (m_CurPointIndex == index) return;
            if (m_CurPointIndex != -1) SetPoint(m_CurPointIndex, false);
            SetPoint(index, true);
            m_CurPointIndex = index;
        }

        private void SetCurArea(AStarArea value, Vector3 nextStartPos)
        {
            if (m_CurArea == value) return;
            SetPoint(m_CurPointIndex, false);
            m_CurArea = value;
            Sensitivity = Mathf.Max(Mathf.RoundToInt(Radius / value.GridSize), 1);
            m_CurPointIndex = m_CurArea.GetIndex(nextStartPos, Sensitivity);
            SetPoint(m_CurPointIndex, true);
        }

        private void SetCurTask(AStarTask task)
        {
            if (m_CurTask != null) AStarPool.TaskPool.Release(m_CurTask);
            m_CurTask = task;
        }

        private void OnMoveEnd()
        {
            m_IsMoveEnd = true;
            if (m_CurTask.IsFind)
            {
                //下一个区域的任务
                if (m_Tasks.Count > 0)
                {
                    var nextTask = m_Tasks.Peek();
                    SetCurArea(AStarMgr.Instance.GetMapAreaMgr(m_MapId).GetArea(nextTask.AreaId),
                        transform.position);
                    AStarMissionMgr.Instance.AddMission(m_CurArea, this, nextTask);
                }
                else
                {
                    m_IsArrive = true;
                    m_IsInTask = false;
                    if (m_CurPointsAround != null)
                    {
                        var dir = m_CurDestination - transform.position;
                        transform.forward = dir.normalized;
                    }
                }
            }
            else //结果不是终点
            {
                //重新寻路
                SetDestination(m_CurDestination, m_CurPointsAround, m_IsMustReach);
            }
        }

        private void ResetData()
        {
            m_IsStop = false;
            m_IsMoveEnd = true;
            m_Clog = 0;
            m_Sleep = false;
            m_IsMustReach = false;
            m_IsNotFind = false;
        }


        private void Update()
        {
            if (m_Sleep)
            {
                m_CurSleepTime += Time.deltaTime;
                if (m_CurSleepTime >= m_MaxSleepTime)
                {
                    m_CurSleepTime = 0;
                    m_Sleep = false;
                    //睡眠结束，还是不能走,就重新寻路

                    if (m_IsMoveEnd ||
                        !m_CurArea.IsPointCanMove(m_CurPointIndex, m_CurTask.Path[m_NextIndex], Sensitivity))
                    {
                        var clog = m_Clog;
                        SetDestination(m_CurDestination, m_CurPointsAround, m_IsMustReach);
                        m_Clog = clog;
                    }
                    else
                    {
                        SetCurPointIndex(m_CurTask.Path[m_NextIndex]);
                    }
                }
            }

            if (!m_Sleep && !m_IsStop && !m_IsMoveEnd)
            {
                Move();
            }
        }

        private void Move()
        {
            //var dir = (m_NextPosition - transform.position).normalized;
            var dis = Vector3.Distance(m_NextPosition, transform.position);
            if (dis <= 0.01f)
            {
                m_NextIndex++;
                if (m_NextIndex >= m_CurTask.Path.Count)
                {
                    if (m_CurTask.IsFind)
                    {
                        if (m_Tasks.Count == 0 && m_CurPointsAround != null && m_CurPointsAround.Count != 0)
                        {
                            transform.position = m_NextPosition;
                            OnMoveEnd();
                            return;
                        }

                        if (m_NextPosition == m_CurTask.Destination)
                        {
                            transform.position = m_NextPosition;
                            OnMoveEnd();
                            return;
                        }

                        if (m_Tasks.Count != 0)
                        {
                            var nextTask = m_Tasks.Peek();
                            //if(nextTask)
                            var nextArea = AStarMgr.Instance.GetMapAreaMgr(m_MapId).GetArea(nextTask.AreaId);
                            if (nextArea.IsPointIsNotWall(m_CurTask.NextAreaStartPointId))
                            {
                                SetCurArea(nextArea, m_CurTask.Destination);
                            }
                            else
                            {
                                SetDestination(m_CurDestination, m_CurPointsAround, m_IsMustReach);
                                return;
                            }
                        }

                        m_NextPosition = m_CurTask.Destination;
                        transform.forward = (m_NextPosition - transform.position).normalized;
                    }
                    else
                    {
                        transform.position = m_NextPosition;
                        OnMoveEnd();
                        return;
                    }
                }
                else
                {
                    m_NextPosition = m_CurArea.GetRealPosByIndex(m_CurTask.Path[m_NextIndex], Sensitivity);
                    transform.forward = (m_NextPosition - transform.position).normalized;
                    //判断下一个节点是否可走，可走就睡眠
                    if (m_CurArea.IsPointCanMove(m_CurPointIndex, m_CurTask.Path[m_NextIndex], Sensitivity))
                    {
                        SetCurPointIndex(m_CurTask.Path[m_NextIndex]);
                    }
                    else
                    {
                        //检查碰撞
                        var bumpAgent = m_CurArea.GetNpcByIndex(m_CurTask.Path[m_NextIndex]);
                        if (bumpAgent != null)
                        {
                            if (!bumpAgent.IsMotion) OnBump?.Invoke();
                        }

                        SetCurPointIndex(m_CurArea.GetIndex(transform.position, Sensitivity));
                        SetSleep(Random.Range(0.1f, 0.5f));
                        return;
                    }
                }
            }

            transform.position = Vector3.MoveTowards(transform.position, m_NextPosition, Speed * Time.deltaTime);
            //transform.position += transform.forward * Speed * Time.deltaTime;
        }


        private void SetSleep(float sleepTime = 0.5f)
        {
            m_Sleep = true;
            m_CurSleepTime = 0;
            m_MaxSleepTime = sleepTime;
        }

        private void SetPoint(int index, bool value)
        {
            if (index == -1) return;
            m_CurArea?.SetPoint(index, value, this);
        }

        private void OnEnable()
        {
            Launch();
            if (m_CurPointIndex != -1)
            {
                SetPoint(m_CurPointIndex, true);
            }
        }

        private void OnDisable()
        {
            Stop();
            if (m_CurPointIndex != -1)
            {
                SetPoint(m_CurPointIndex, false);
            }
        }


        private void OnDestroy()
        {
            if (m_Tasks != null) AStarPool.TaskQueuePool.Release(m_Tasks);
            SetPoint(m_CurPointIndex, false);
        }

        private bool m_Sleep;
        private float m_MaxSleepTime = 0.5f;
        private float m_CurSleepTime;
        private Vector3 m_NextPosition;
        private Vector3 m_CurDestination;
        private AStarArea m_CurArea;
        private int m_NextIndex;
        private bool m_IsStop;
        private bool m_IsMoveEnd;
        private Queue<AStarTask> m_Tasks;
        private int m_CurPointIndex = -1;
        private int m_MapId;
        private AStarTask m_CurTask;
        private int m_Clog;
        private readonly int m_MaxClogTime = 3;
        private bool m_IsArrive;
        private bool m_IsMustReach;
        private bool m_IsNotFind;
        private bool m_IsInTask;
        private List<Vector2Int> m_CurPointsAround;
    }
}