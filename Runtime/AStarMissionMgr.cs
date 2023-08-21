

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace TFW.AStar
{
    public class AStarMissionMgr : MonoBehaviour
    {
        public static AStarMissionMgr Instance { get; private set; }

        //正常情况下并发寻路此时不会有那么高1000个物体在地图中随机移动每帧请求次数顶天10多个，当然不包括一起寻路的情况
        //当然这不代表场景中只能有64个Agent，后面排队的任务会在下一帧处理。
        public int MaxDealMissionCount { get; } = 1000;
        public int OneTimeDealMissionCount { get; private set; } = 64;
        public int MaxSearchCount { get; private set; } = 1000;

        /// <summary>
        /// 设置任务每帧最大批处理数
        /// </summary>
        /// <param name="count"></param>
        public void SetOneTimeDealMissionCount(int count)
        {
            if (count <= 0) return;
            OneTimeDealMissionCount = Math.Min(count, 1000);
        }

        /// <summary>
        /// 设置每次寻路最大搜索次数
        /// </summary>
        /// <param name="count"></param>
        public void SetMaxSearchCount(int count)
        {
            MaxSearchCount = count;
        }


        private void Awake()
        {
            m_Dir = new NativeArray<int>(8, Allocator.Persistent);
            m_Dir[0] = 0;
            m_Dir[1] = 1;
            m_Dir[2] = 0;
            m_Dir[3] = -1;
            m_Dir[4] = 1;
            m_Dir[5] = 0;
            m_Dir[6] = -1;
            m_Dir[7] = 0;
            m_NeedCompleteMission = new List<AStarMission>();
            m_Dats = new NativeArray<AStarCustomData>(MaxDealMissionCount, Allocator.Persistent);
            m_PointAround = new NativeHashSet<int>(MaxDealMissionCount * 20, Allocator.Persistent);
            m_Result = new NativeArray<int>(MaxDealMissionCount * MaxSearchCount, Allocator.Persistent);
            m_AgentMissionDict = new Dictionary<AStarAgent, AStarMission>();
            m_AreaMissionDict = new Dictionary<AStarArea, Queue<AStarMission>>();
            m_CurDealCount = 0;
            Instance = this;
        }

        void Update()
        {
            foreach (var aStarMission in m_AreaMissionDict)
            {
                while (aStarMission.Value.Count > 0)
                {
                    var mission = aStarMission.Value.Dequeue();
                    if (!m_AgentMissionDict.ContainsKey(mission.Agent)) continue;
                    m_AgentMissionDict.Remove(mission.Agent);
                    mission.DefineCustomData(m_CurDealCount * mission.Area.AreaPointData.Length, m_PointAround);
                    m_Dats[m_CurDealCount] = mission.CustomData;
                    m_NeedCompleteMission.Add(mission);
                    ++m_CurDealCount;
                    if (m_CurDealCount >= OneTimeDealMissionCount)
                    {
                        break;
                    }
                }

                var time = System.DateTime.Now;
                if (m_CurDealCount > 0)
                {
                    int loopTimes = m_CurDealCount / IterationsPerJob;
                    int remainder = m_CurDealCount % IterationsPerJob;
                    loopTimes = remainder > 0 ? loopTimes + 1 : loopTimes;

                    NativeArray<JobHandle> handles = new NativeArray<JobHandle>(loopTimes, Allocator.TempJob,
                        NativeArrayOptions.UninitializedMemory);


                    int numberOfScheduledPaths = 0;

                    for (int i = 0; i < loopTimes; ++i)
                    {
                        int iterations = (i < loopTimes - 1 || remainder == 0) ? IterationsPerJob : remainder;
                        var pathJob = new PathJob()
                        {
                            Dir = m_Dir,
                            CustomDats = m_Dats,
                            Result = m_Result,
                            PointsAround = m_PointAround,
                            Map = aStarMission.Key.AreaPointData,
                            MaxSearchCount = MaxSearchCount,
                            MapSize = aStarMission.Key.AreaPointData.Length,
                            XLen = aStarMission.Key.Data.XGridNum,
                            YLen = aStarMission.Key.Data.YGridNum,
                            BaseIndex = i * IterationsPerJob,
                        };
                        handles[i] = pathJob.Schedule(iterations, 1);
                        numberOfScheduledPaths += iterations;
                        if (numberOfScheduledPaths > ScheduleAfterNumOfPaths)
                        {
                            //允许批量调度作业，而不是逐个调度，从而减少调度开销.
                            JobHandle.ScheduleBatchedJobs();
                            numberOfScheduledPaths %= ScheduleAfterNumOfPaths;
                        }
                    }

                    var deps = JobHandle.CombineDependencies(handles);
                    deps.Complete();
                    handles.Dispose();


                    // 一次性处理会产生更多临时分配的内存，一旦零时分配内存不足会造成性能变差
                    // 猜测还有可能是某某线程被分到的任务全是不可达路径，势必照成更多的计算量。。从而导致性能变差
                    // 某案列经验是一次8个效果最好，根据机器不同可能还存在差异

                    // var pathJob = new PathJob()
                    // {
                    //     Dir = m_Dir,
                    //     CustomDats = m_Dats,
                    //     Result = m_Result,
                    //     PointsAround = m_PointAround,
                    //     Map = aStarMission.Key.AreaPointData,
                    //     MaxSearchCount = MaxSearchCount,
                    //     MapSize = aStarMission.Key.AreaPointData.Length,
                    //     XLen = aStarMission.Key.Data.XGridNum,
                    //     YLen = aStarMission.Key.Data.YGridNum,
                    //     BaseIndex = 0,
                    // };
                    // var handle = pathJob.Schedule(m_CurDealCount, InnerLoopBatchCount);
                    // handle.Complete();
                    m_CurDealCount = 0;
                    m_PointAround.Clear();

                    //Debug.Log($"time : {(System.DateTime.Now - time).TotalMilliseconds} ms");
                }

                if (m_NeedCompleteMission.Count > 0)
                {
                    for (var index = 0; index < m_NeedCompleteMission.Count; index++)
                    {
                        var cMission = m_NeedCompleteMission[index];

                        cMission.Complete(m_Result, index * MaxSearchCount);
                    }

                    m_NeedCompleteMission.Clear();
                }
            }
        }


        public void AddMission(AStarArea area, AStarAgent agent, AStarTask tasks)
        {
            //默认一个角色同时只有一个任务
            if (m_AgentMissionDict.TryGetValue(agent, out var mission))
            {
                mission.Tasks = tasks;
                mission.Area = area;
            }
            else
            {
                mission = AStarPool.MissionPool.Get();
                mission.Agent = agent;
                mission.Tasks = tasks;
                mission.Area = area;
                m_AgentMissionDict.Add(agent, mission);
                if (!m_AreaMissionDict.ContainsKey(area))
                {
                    m_AreaMissionDict.Add(area, new Queue<AStarMission>());
                }

                m_AreaMissionDict[area].Enqueue(mission);
            }
        }

        private void OnDestroy()
        {
            m_PointAround.Dispose();
            m_Dats.Dispose();
            m_Result.Dispose();
            m_Dir.Dispose();
            AStarMgr.Instance.DeInit();
        }

        const int ScheduleAfterNumOfPaths = 256;
        const int IterationsPerJob = 8;
        private NativeHashSet<int> m_PointAround;
        private NativeArray<AStarCustomData> m_Dats;
        private NativeArray<int> m_Result;
        private List<AStarMission> m_NeedCompleteMission;
        private Dictionary<AStarAgent, AStarMission> m_AgentMissionDict;
        private Dictionary<AStarArea, Queue<AStarMission>> m_AreaMissionDict;
        private int m_CurDealCount;
        private NativeArray<int> m_Dir;
    }
}