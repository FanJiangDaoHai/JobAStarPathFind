

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