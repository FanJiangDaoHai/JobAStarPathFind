

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TFW.AStar.Editor
{
    [CanEditMultipleObjects, CustomEditor(typeof(AreaCreate))]
    public class AreaCreateEditor : UnityEditor.Editor
    {
        private AreaCreate Target => target as AreaCreate;

        private void OnEnable()
        {
            if (Target.Terrain == null)
            {
                Target.Terrain = Target.gameObject.GetComponent<Terrain>();
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();


            if (GUILayout.Button("加载寻路数据"))
            {
                //Target.transform.position = Vector3.zero;
                for (int i = Target.transform.childCount - 1; i > -1; i--)
                {
                    GameObject.DestroyImmediate(Target.transform.GetChild(i).gameObject);
                }

                Target.Areas =
                    AreaElementEditor.LoadData<Areas>(Target.DataSaveRootDirectory, $"Map_{Target.CurMapId}");
                Target.Areas.AreasInfo ??= new List<AreaInfo>();
                foreach (var i in Target.Areas.AreasInfo)
                {
                    GameObject go = new GameObject(i.AreaId.ToString());
                    go.transform.SetParent(Target.transform);
                    go.transform.localPosition = i.Position;
                    var area = go.AddComponent<AreaElement>();
                    area.AreaId = i.AreaId;
                    area.AreaData = i.AreaData; //AreaElementEditor.LoadData<AreaData>(i.AreaId.ToString());
                    area.Terrain = Target.Terrain;
                    area.InitData();
                }

                foreach (Transform o in Target.transform)
                {
                    var area = o.GetComponent<AreaElement>();
                    if (area != null)
                    {
                        area.ConnectArea = new List<AreaElement>();
                        foreach (var areaDataAreaConnectInfo in area.AreaData.AreaConnectInfos)
                        {
                            if (areaDataAreaConnectInfo.IsInitiator)
                            {
                                var area2 = Target.transform.Find(areaDataAreaConnectInfo.TargetAreaId.ToString());
                                if (area2) area.ConnectArea.Add(area2.GetComponent<AreaElement>());
                            }
                        }
                    }
                }
            }

            if (Target.Areas == null)
            {
                return;
            }

            if (GUILayout.Button("添加区域"))
            {
                foreach (Transform o in Target.transform)
                {
                    if (o.name == Target.CurAddAreaId.ToString())
                    {
                        Debug.LogError("不能添加重复区域ID");
                        return;
                    }
                }

                GameObject go = new GameObject(Target.CurAddAreaId.ToString());
                var area = go.AddComponent<AreaElement>();
                go.transform.SetParent(Target.transform);
                go.transform.localPosition = Vector3.zero;
                area.AreaId = Target.CurAddAreaId;
                area.AreaData =
                    new AStarAreaData(); //AreaElementEditor.LoadData<AreaData>(Target.CurAddAreaId.ToString());
                area.Terrain = Target.Terrain;
                area.InitData();
                SaveData();
            }

            if (GUILayout.Button("保存数据"))
            {
                SaveData();
            }

            // if (GUILayout.Button("Test"))
            // {
            //     foreach (Transform o in Target.transform)
            //     {
            //         var area = o.GetComponent<AreaElement>();
            //         if (area != null)
            //         {
            //             var oldAreaData = AreaElementEditor.LoadData<AreaData>(area.AreaId.ToString());
            //             area.AreaData = new AStarAreaData()
            //             {
            //                 AreaId = oldAreaData.AreaId,
            //                 Points = oldAreaData.Points,
            //                 AreaConnectInfos = oldAreaData.AreaConnectInfos,
            //                 XGridNum = oldAreaData.XGridNum,
            //                 YGridNum = oldAreaData.YGridNum,
            //                 X = oldAreaData.X,
            //                 Y = oldAreaData.Y,
            //                 ConnectAreas = oldAreaData.ConnectAreas,
            //                 IsCityEditor = oldAreaData.IsCityEditor,
            //                 CityEditorOffset = oldAreaData.CityEditorOffset,
            //                 CityEditorSize = oldAreaData.CityEditorSize,
            //             };
            //         }
            //     }
            //     SaveData();
            // }
        }


        private void SaveData()
        {
            Target.Areas.AreasInfo.Clear();
            foreach (Transform o in Target.transform)
            {
                var area = o.GetComponent<AreaElement>();
                if (area != null)
                {
                    area.AlignData();
                    Target.Areas.AreasInfo.Add(new AreaInfo()
                    {
                        AreaId = int.Parse(o.name),
                        Position = o.transform.localPosition,
                        AreaData = area.AreaData,
                    });
                }
            }

            foreach (Transform o in Target.transform)
            {
                var area = o.GetComponent<AreaElement>();
                if (area != null)
                {
                    area.ClearConnect();
                }
            }

            foreach (Transform o in Target.transform)
            {
                var area = o.GetComponent<AreaElement>();
                if (area != null)
                {
                    area.AreaConnect();
                }
            }

            GetAreaPointPath();
            // foreach (Transform o in Target.transform)
            // {
            //     var area = o.GetComponent<AreaElement>();
            //     if (area != null)
            //     {
            //         EditorUtility.SetDirty(area.AreaData);
            //         AssetDatabase.SaveAssets();
            //     }
            // }

            EditorUtility.SetDirty(Target.Areas);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("数据保存成功");
        }

        private void GetAreaPointPath()
        {
            List<ConnectPoint> points = new List<ConnectPoint>();
            Dictionary<int, List<int>> areaPoints = new Dictionary<int, List<int>>();
            Dictionary<int, AreaElement> areaElements = new Dictionary<int, AreaElement>();
            int t = 0;
            foreach (Transform o in Target.transform)
            {
                var area = o.GetComponent<AreaElement>();
                if (area != null)
                {
                    areaElements.Add(area.AreaId, area);
                    var data = area.AreaData.AreaConnectInfos;
                    foreach (var areaConnectInfo in data)
                    {
                        if (areaConnectInfo.IsInitiator)
                        {
                            points.Add(new ConnectPoint()
                            {
                                PointId = t,
                                Position =
                                    area.GetGridLocalPos(
                                        areaConnectInfo.SelfPoints[areaConnectInfo.SelfPoints.Count / 2]),
                                AreaId1 = area.AreaId,
                                AreaId2 = areaConnectInfo.TargetAreaId,
                                Area1Points = areaConnectInfo.SelfPoints,
                                Area2Points = areaConnectInfo.TargetPoints,
                                Connects = new List<Connect>(),
                            });
                            if (!areaPoints.ContainsKey(area.AreaId))
                            {
                                areaPoints.Add(area.AreaId, new List<int>());
                            }

                            areaPoints[area.AreaId].Add(t);

                            if (!areaPoints.ContainsKey(areaConnectInfo.TargetAreaId))
                            {
                                areaPoints.Add(areaConnectInfo.TargetAreaId, new List<int>());
                            }

                            areaPoints[areaConnectInfo.TargetAreaId].Add(t);
                            t++;
                        }
                    }
                }
            }

            foreach (var areaElement in areaElements.Values)
            {
                areaElement.AreaData.ConnectAreas = new List<ConnectArea>();
                foreach (var v in areaElements.Values)
                {
                    if (areaElement.AreaId != v.AreaId)
                    {
                        areaElement.AreaData.ConnectAreas.Add(new ConnectArea()
                        {
                            TargetAreaId = v.AreaId,
                            Connects = new List<Connect>(),
                        });
                    }
                }
            }


            foreach (var areaPointsValue in areaPoints.Values)
            {
                for (var i = 0; i < areaPointsValue.Count; i++)
                {
                    var p1 = points[areaPointsValue[i]];
                    for (int j = 0; j < areaPointsValue.Count; j++)
                    {
                        if (i != j)
                        {
                            p1.Connects.Add(new Connect()
                            {
                                NextPoint = areaPointsValue[j],
                                Cost = Vector3.Distance(p1.Position, points[areaPointsValue[j]].Position),
                            });
                        }
                    }
                }
            }

            for (int i = 0; i < points.Count; i++)
            {
                foreach (var connectPoint in points)
                {
                    connectPoint.G = 0;
                    connectPoint.ParentId = -1;
                }

                HashSet<int> openList = new HashSet<int>();
                HashSet<int> closeList = new HashSet<int>();
                openList.Add(i);
                while (openList.Count > 0)
                {
                    var curIndex = FindMin(openList);
                    openList.Remove(curIndex);
                    closeList.Add(curIndex);
                    var data = points[curIndex].Connects;
                    if (data != null)
                    {
                        foreach (var areaConnectInfo in data)
                        {
                            var nextIndex = areaConnectInfo.NextPoint;
                            if (closeList.Contains(nextIndex)) continue;
                            var newG = points[curIndex].G + areaConnectInfo.Cost;
                            if (openList.Contains(nextIndex))
                            {
                                if (newG < points[nextIndex].G)
                                {
                                    points[nextIndex].G = newG;
                                    points[nextIndex].ParentId = curIndex;
                                }
                            }
                            else
                            {
                                points[nextIndex].G = newG;
                                points[nextIndex].ParentId = curIndex;
                                openList.Add(nextIndex);
                            }
                        }
                    }
                }

                points[i].Connects = new List<Connect>();
                foreach (var connectPoint in points)
                {
                    var data = connectPoint;
                    if (data.ParentId != -1)
                    {
                        var path = new List<int>();
                        while (data.ParentId != -1)
                        {
                            path.Add(data.PointId);
                            data = points[data.ParentId];
                        }

                        path.Add(points[i].PointId);
                        path.Reverse();
                        points[i].Connects.Add(new Connect()
                        {
                            NextPoint = connectPoint.PointId,
                            Cost = connectPoint.G,
                            Points = path,
                        });
                    }
                }

                points[i].Connects.Add(new Connect()
                {
                    NextPoint = points[i].PointId,
                    Cost = 0,
                    Points = new List<int>() { points[i].PointId },
                });
            }

            int FindMin(HashSet<int> openList)
            {
                float maxValue = float.MaxValue;
                int ret = 0;
                foreach (var i in openList)
                {
                    if (points[i].G < maxValue)
                    {
                        ret = i;
                        maxValue = points[i].G;
                    }
                }

                return ret;
            }

            Target.Areas.ConnectPoints = points;
            foreach (var connectPoint in points)
            {
                foreach (var connectPointConnect in connectPoint.Connects)
                {
                    var sArea1 = connectPoint.AreaId1;
                    var sArea2 = connectPoint.AreaId2;
                    var tArea1 = points[connectPointConnect.NextPoint].AreaId1;
                    var tArea2 = points[connectPointConnect.NextPoint].AreaId2;
                    foreach (var areaDataConnectArea in areaElements[sArea1].AreaData.ConnectAreas)
                    {
                        if (areaDataConnectArea.TargetAreaId == tArea1 || areaDataConnectArea.TargetAreaId == tArea2)
                        {
                            areaDataConnectArea.Connects.Add(connectPointConnect);
                        }

                        // if (areaDataConnectArea.TargetAreaId == sArea2)
                        // {
                        //     areaDataConnectArea.Connects.Add(new Connect()
                        //     {
                        //         Cost = 0,
                        //         NextPoint = connectPoint.PointId,
                        //         Points = new List<int>() { connectPoint.PointId },
                        //     });
                        // }
                    }

                    foreach (var areaDataConnectArea in areaElements[sArea2].AreaData.ConnectAreas)
                    {
                        if (areaDataConnectArea.TargetAreaId == tArea1 || areaDataConnectArea.TargetAreaId == tArea2)
                        {
                            areaDataConnectArea.Connects.Add(connectPointConnect);
                        }

                        // if (areaDataConnectArea.TargetAreaId == sArea1)
                        // {
                        //     areaDataConnectArea.Connects.Add(new Connect()
                        //     {
                        //         Cost = 0,
                        //         NextPoint = connectPoint.PointId,
                        //         Points = new List<int>() { connectPoint.PointId },
                        //     });
                        // }
                    }
                }
            }
        }
    }
}
#endif