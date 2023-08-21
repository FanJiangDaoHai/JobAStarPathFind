

using System.Collections.Generic;

namespace TFW.AStar
{
    public static class AStarPool
    {
        public static readonly UnityEngine.Pool.ObjectPool<AStarTask> TaskPool =
            new UnityEngine.Pool.ObjectPool<AStarTask>(() => new AStarTask(), (e) =>
            {
                e.IsFind = false;
                e.Path ??= new List<int>();
            }, (e) =>
            {
                e.PointsAround = null;
                e.Path.Clear();
            });

        public static readonly UnityEngine.Pool.ObjectPool<Queue<AStarTask>> TaskQueuePool =
            new UnityEngine.Pool.ObjectPool<Queue<AStarTask>>(() => new Queue<AStarTask>(), null,
                (e) =>
                {
                    foreach (var aStarTask in e)
                    {
                        TaskPool.Release(aStarTask);
                    }

                    e.Clear();
                });

        public static readonly UnityEngine.Pool.ObjectPool<AStarMission> MissionPool =
            new UnityEngine.Pool.ObjectPool<AStarMission>(() => new AStarMission(),
                null, (mission) => { mission.OnRelease(); });
    }
}