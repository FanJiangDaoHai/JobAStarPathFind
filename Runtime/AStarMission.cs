// 版权所有[成都创人所爱科技股份有限公司]
// 根据《保密信息使用许可证》获得许可;
// 除非符合许可，否则您不得使用此文件。
// 您可以在以下位置获取许可证副本，链接地址：
// https://wiki.tap4fun.com/display/MO/Confidentiality
// 除非适用法律要求或书面同意，否则保密信息按照使用许可证要求使用，不附带任何明示或暗示的保证或条件。
// 有关管理权限的特定语言，请参阅许可证副本。

using Unity.Collections;

namespace TFW.AStar
{
    public class AStarMission
    {
        public AStarAgent Agent;
        public AStarTask Tasks;
        public AStarArea Area;
        public AStarCustomData CustomData;

        public AStarMission()
        {
            CustomData = new AStarCustomData();
        }


        public void DefineCustomData(int offset, NativeHashSet<int> PointAround)
        {
            CustomData.StartIndex = Area.GetIndex(Tasks.StartPos, Agent.Sensitivity);
            CustomData.EndIndex = Area.GetIndex(Tasks.Destination, Agent.Sensitivity);
            CustomData.Sensitivity = Agent.Sensitivity;
            var list = Tasks.PointsAround;
            if (list != null && list.Count > 0)
            {
                foreach (var lis in list)
                {
                    PointAround.Add(Area.GetIndex(lis.x, lis.y) + offset);
                }
            }
        }

        public void Complete(NativeArray<int> Result, int rIndex)
        {
            Tasks.IsFind = Result[rIndex] == 1;
            while (true)
            {
                var value = Result[++rIndex];
                if (value == -1) break;
                Tasks.Path.Add(value);
            }

            Tasks.Path.Reverse();
            if (Agent != null)
            {
                Agent.SetPath(Tasks);
            }

            AStarPool.MissionPool.Release(this);
        }

        public void OnRelease()
        {
            Agent = null;
            Tasks = null;
            Area = null;
        }

        public void DeInit()
        {
            //if (!Handle.IsCompleted) Handle.Complete();
            AStarPool.MissionPool.Release(this);
        }
    }
}