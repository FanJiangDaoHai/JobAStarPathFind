// 版权所有[成都创人所爱科技股份有限公司]
// 根据《保密信息使用许可证》获得许可;
// 除非符合许可，否则您不得使用此文件。
// 您可以在以下位置获取许可证副本，链接地址：
// https://wiki.tap4fun.com/display/MO/Confidentiality
// 除非适用法律要求或书面同意，否则保密信息按照使用许可证要求使用，不附带任何明示或暗示的保证或条件。
// 有关管理权限的特定语言，请参阅许可证副本。

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