// 版权所有[成都创人所爱科技股份有限公司]
// 根据《保密信息使用许可证》获得许可;
// 除非符合许可，否则您不得使用此文件。
// 您可以在以下位置获取许可证副本，链接地址：
// https://wiki.tap4fun.com/display/MO/Confidentiality
// 除非适用法律要求或书面同意，否则保密信息按照使用许可证要求使用，不附带任何明示或暗示的保证或条件。
// 有关管理权限的特定语言，请参阅许可证副本。

using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace TFW.AStar
{
    [BurstCompile]
    public struct Point
    {
        public float F;
        public float G;
        public float H;
        public int X;
        public int Y;
        public int ParentIndex;
        public bool IsWall;
        public float Weight;
        public bool CloseFlag;
        public bool OpenFlag;
    }

    [BurstCompile]
    public struct AStarCustomData
    {
        public int StartIndex;
        public int EndIndex;
        public int Sensitivity;
    }


    [BurstCompile]
    public struct PathJob : IJobParallelFor
    {
        [WriteOnly, NativeDisableContainerSafetyRestriction, NativeDisableParallelForRestriction]
        public NativeArray<int> Result;

        [Unity.Collections.ReadOnly] public NativeHashSet<int> PointsAround;
        [Unity.Collections.ReadOnly] public NativeArray<Point> Map;
        [Unity.Collections.ReadOnly] public NativeArray<int> Dir;
        [Unity.Collections.ReadOnly] public NativeArray<AStarCustomData> CustomDats;
        [Unity.Collections.ReadOnly] public int MaxSearchCount;
        [Unity.Collections.ReadOnly] public int MapSize;
        [Unity.Collections.ReadOnly] public int XLen;
        [Unity.Collections.ReadOnly] public int YLen;
        [Unity.Collections.ReadOnly] public int BaseIndex;

        public void Execute(int jobIndex)
        {
            NativeList<int> roundPointList = new NativeList<int>(Dir.Length, Allocator.Temp);
            NativeList<int> openSmallTopHeap = new NativeList<int>(MaxSearchCount, Allocator.Temp);
            NativeHashMap<int, Point> changeMap = new NativeHashMap<int, Point>(MaxSearchCount, Allocator.Temp);
            var maxSearchCount = MaxSearchCount;
            var realJobIndex = jobIndex + BaseIndex;
            int rIndex = MaxSearchCount * realJobIndex;
            var offset = realJobIndex * MapSize;
            SmallTopHeapAdd(changeMap, openSmallTopHeap, CustomDats[realJobIndex].StartIndex);
            //选取最近的点
            int approximateIndex = -1;
            float minDis = float.MaxValue;
            if (CustomDats[realJobIndex].StartIndex == CustomDats[realJobIndex].EndIndex ||
                CheckPointAround(changeMap, PointsAround, CustomDats[realJobIndex].StartIndex, offset, realJobIndex))
            {
                Result[rIndex] = 1;
                Result[++rIndex] = -1;
                changeMap.Dispose();
                openSmallTopHeap.Dispose();
                roundPointList.Dispose();
                return;
            }

            while (openSmallTopHeap.Length > 0)
            {
                maxSearchCount--;
                if (maxSearchCount < 0) break;
                int point = openSmallTopHeap[0];
                SmallTopHeapPopTop(changeMap, openSmallTopHeap);
                PointToCloseSet(changeMap, point);
                var dis = GetDistance_OS(GetPoint(changeMap, CustomDats[realJobIndex].EndIndex),
                    GetPoint(changeMap, point));
                if (dis < minDis)
                {
                    minDis = dis;
                    approximateIndex = point;
                }

                roundPointList.Clear();
                GetRoundPoint(changeMap, point, roundPointList, realJobIndex);
                for (int i = 0; i < roundPointList.Length; i++)
                {
                    int nextIndex = roundPointList[i];
                    Point item = GetPoint(changeMap, nextIndex);
                    if (GetPoint(changeMap, nextIndex).OpenFlag)
                    {
                        //格子地图极端情况下会出现这种情况暂不处理
                        // float nowG = CalcG(item, GetPoint(changeMap, point));
                        // if (nowG < item.G)
                        // {
                        //     item.ParentIndex = point;
                        //     item.G = nowG;
                        //     item.F = nowG + item.H;
                        //     SetPoint(changeMap, nextIndex, item);
                        //     OnPointFChange(changeMap, openSmallTopHeap, nextIndex);
                        // }
                    }
                    else
                    {
                        item.ParentIndex = point;
                        item = CalcF(changeMap, item, GetPoint(changeMap, CustomDats[realJobIndex].EndIndex));
                        SetPoint(changeMap, nextIndex, item);
                        SmallTopHeapAdd(changeMap, openSmallTopHeap, nextIndex);
                    }

                    if (nextIndex == CustomDats[realJobIndex].EndIndex ||
                        CheckPointAround(changeMap, PointsAround, nextIndex, offset, realJobIndex))
                    {
                        //表示寻到路径
                        Result[rIndex] = 1;
                        while (nextIndex != CustomDats[realJobIndex].StartIndex)
                        {
                            Result[++rIndex] = nextIndex;
                            nextIndex = GetPoint(changeMap, nextIndex).ParentIndex;
                        }

                        Result[++rIndex] = -1;
                        changeMap.Dispose();
                        openSmallTopHeap.Dispose();
                        roundPointList.Dispose();
                        return;
                    }
                }
            }

            //表示没有寻到找最近的
            Result[rIndex] = 0;
            while (approximateIndex != CustomDats[realJobIndex].StartIndex)
            {
                Result[++rIndex] = approximateIndex;
                approximateIndex = GetPoint(changeMap, approximateIndex).ParentIndex;
            }

            Result[++rIndex] = -1;
            changeMap.Dispose();
            openSmallTopHeap.Dispose();
            roundPointList.Dispose();
        }

        private Point GetPoint(NativeHashMap<int, Point> changeMap, int index)
        {
            if (changeMap.ContainsKey(index))
            {
                return (changeMap[index]);
            }
            else
            {
                return Map[index];
            }
        }

        private void SetPoint(NativeHashMap<int, Point> changeMap, int index, Point point)
        {
            changeMap[index] = point;
        }

        private bool CheckPointAround(NativeHashMap<int, Point> changeMap, NativeHashSet<int> pointAround, int index,
            int offset, int realJobIndex)
        {
            var point = GetPoint(changeMap, index);
            for (int i = point.X; i < point.X + CustomDats[realJobIndex].Sensitivity; i++)
            {
                for (int j = point.Y; j < point.Y + CustomDats[realJobIndex].Sensitivity; j++)
                {
                    if (pointAround.Contains(GetMapIndex(i, j, YLen) + offset))
                        return true;
                }
            }

            return false;
        }


        /// <summary>
        /// 优化close列表
        /// </summary>
        /// <param name="changeMap"></param>
        /// <param name="index"></param>
        private void PointToCloseSet(NativeHashMap<int, Point> changeMap, int index)
        {
            Point p = GetPoint(changeMap, index);
            p.CloseFlag = true;
            SetPoint(changeMap, index, p);
        }

        /// <summary>
        /// 优化Open列表查询
        /// </summary>
        /// <param name="changeMap"></param>
        /// <param name="index"></param>
        /// <param name="isIn"></param>
        private void PointInOutOpenSet(NativeHashMap<int, Point> changeMap, int index, bool isIn)
        {
            Point p = GetPoint(changeMap, index);
            p.OpenFlag = isIn;
            SetPoint(changeMap, index, p);
        }

        /// <summary>
        /// 小顶堆添加元素
        /// </summary>
        /// <param name="changeMap"></param>
        /// <param name="heap"></param>
        /// <param name="value"></param>
        private void SmallTopHeapAdd(NativeHashMap<int, Point> changeMap, NativeList<int> heap, int value)
        {
            PointInOutOpenSet(changeMap, value, true);
            heap.Add(value);
            int end = heap.Length - 1;
            while (end > 0)
            {
                int parent = end / 2 - (end % 2 == 0 ? 1 : 0);
                if (GetPoint(changeMap, heap[end]).F < GetPoint(changeMap, heap[parent]).F)
                {
                    (heap[parent], heap[end]) = (heap[end], heap[parent]);
                    end = parent;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// F值变小,考虑上浮
        /// </summary>
        /// <param name="changeMap"></param>
        /// <param name="heap"></param>
        /// <param name="value"></param>
        private void OnPointFChange(NativeHashMap<int, Point> changeMap, NativeList<int> heap, int value)
        {
            var index = -1;
            for (int i = 0; i < heap.Length; i++)
            {
                if (heap[i] == value)
                {
                    index = i;
                    break;
                }
            }

            while (index > 0)
            {
                int parent = index / 2 - (index % 2 == 0 ? 1 : 0);
                if (GetPoint(changeMap, heap[index]).F < GetPoint(changeMap, heap[parent]).F)
                {
                    (heap[parent], heap[index]) = (heap[index], heap[parent]);
                    index = parent;
                }
                else
                {
                    break;
                }
            }
        }


        /// <summary>
        /// 移除堆顶元素
        /// </summary>
        /// <param name="changeMap"></param>
        /// <param name="heap"></param>
        private void SmallTopHeapPopTop(NativeHashMap<int, Point> changeMap, NativeList<int> heap)
        {
            int topIndex = heap[0];
            PointInOutOpenSet(changeMap, topIndex, false);
            if (heap.Length == 1)
            {
                heap.Clear();
                return;
            }

            //将此列表的最后一个元素复制到指定的索引。将长度减 1
            heap.RemoveAtSwapBack(0);
            int start = 0;
            while (start < heap.Length)
            {
                int min;
                int left = start * 2 + 1;
                int right = start * 2 + 2;
                if (left >= heap.Length) return;
                if (right >= heap.Length)
                {
                    min = left;
                }
                else
                {
                    min = GetPoint(changeMap, heap[left]).F > GetPoint(changeMap, heap[right]).F ? right : left;
                }

                if (GetPoint(changeMap, heap[start]).F <= GetPoint(changeMap, heap[min]).F) return;

                (heap[min], heap[start]) = (heap[start], heap[min]);
                start = min;
            }
        }

        /// <summary>
        /// 获取地图节点下标
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="yLen"></param>
        /// <returns></returns>
        private int GetMapIndex(int x, int y, int yLen)
        {
            return x * yLen + y;
        }


        /// <summary>
        /// 获取可走的下一个点
        /// </summary>
        /// <param name="changeMap"></param>
        /// <param name="pIndex"></param>
        /// <param name="ret"></param>
        private void GetRoundPoint(NativeHashMap<int, Point> changeMap, int pIndex,
            NativeList<int> ret, int realJobIndex)
        {
            NativeArray<bool> book = new NativeArray<bool>(4, Allocator.Temp);
            Point p = GetPoint(changeMap, pIndex);
            for (int i = 0; i < 4; i++)
            {
                int x = p.X + Dir[GetMapIndex(i, 0, 2)];
                int y = p.Y + Dir[GetMapIndex(i, 1, 2)];
                int nextIndex = GetMapIndex(x, y, YLen);
                if (x >= 0 && x < XLen && y >= 0 && y < YLen &&
                    IsPointCanMove(changeMap, x, y, XLen, YLen, p, realJobIndex))
                {
                    if (!GetPoint(changeMap, nextIndex).CloseFlag) ret.Add(nextIndex);
                    book[i] = true;
                }
            }

            for (int i = 0; i < 2; i++)
            {
                for (int j = 2; j < 4; j++)
                {
                    int x = p.X + Dir[GetMapIndex(i, 0, 2)] + Dir[GetMapIndex(j, 0, 2)];
                    int y = p.Y + Dir[GetMapIndex(i, 1, 2)] + Dir[GetMapIndex(j, 1, 2)];
                    int nextIndex = GetMapIndex(x, y, YLen);
                    if (book[i] && book[j] && x >= 0 && x < XLen && y >= 0 &&
                        y < YLen &&
                        IsPointCanMove(changeMap, x, y, XLen, YLen, p, realJobIndex))
                    {
                        if (!GetPoint(changeMap, nextIndex).CloseFlag)
                            ret.Add(nextIndex);
                    }
                }
            }

            book.Dispose();
        }

        private bool IsPointCanMove(NativeHashMap<int, Point> changeMap, int x, int y, int xLen, int yLen,
            Point p, int realJobIndex)
        {
            for (int i = x; i < x + CustomDats[realJobIndex].Sensitivity; i++)
            {
                for (int j = y; j < y + CustomDats[realJobIndex].Sensitivity; j++)
                {
                    if (i >= p.X && i < p.X + CustomDats[realJobIndex].Sensitivity && j >= p.Y &&
                        j < p.Y + CustomDats[realJobIndex].Sensitivity) continue;
                    if (i >= xLen || j >= yLen) continue;
                    if (GetPoint(changeMap, GetMapIndex(i, j, yLen)).IsWall) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 计算G值
        /// </summary>
        /// <param name="now"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        private float CalcG(Point now, Point parent)
        {
            return GetCost(now, parent) + parent.G;
        }


        /// <summary>
        /// 计算F值
        /// </summary>
        /// <param name="changeMap"></param>
        /// <param name="now"></param>
        /// <param name="end"></param>
        private Point CalcF(NativeHashMap<int, Point> changeMap, Point now, Point end)
        {
            float h = GetDistance_DJ(now, end);
            if (h > XLen * math.SQRT2 / 2) h *= 1.5f;
            float g = 0;
            if (now.ParentIndex != -1)
            {
                g = CalcG(now, GetPoint(changeMap, now.ParentIndex));
            }

            float f = g + h;
            now.F = f;
            now.G = g;
            now.H = h;
            return now;
        }

        /// <summary>
        /// 计算a到b的花费
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private float GetCost(Point a, Point b)
        {
            return GetDistance_DJ(a, b) * (a.Weight + b.Weight) / 2;
        }

        /// <summary>
        /// 欧式距离的平方
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private float GetDistance_OS(Point a, Point b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// 曼哈顿距离
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private float GetDistance_MHD(Point a, Point b)
        {
            return math.abs(a.X - b.X) + math.abs(a.Y - b.Y);
        }

        /// <summary>
        /// 对角距离
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private float GetDistance_DJ(Point a, Point b)
        {
            float dx = math.abs(a.X - b.X);
            float dy = math.abs(a.Y - b.Y);
            return dx + dy + (math.SQRT2 - 2) * math.min(dx, dy);
        }
    }
}