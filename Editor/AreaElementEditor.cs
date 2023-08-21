

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TFW.AStar.Editor
{
    [CanEditMultipleObjects, CustomEditor(typeof(AreaElement))]
    public class AreaElementEditor : UnityEditor.Editor
    {
        private AreaElement Target => target as AreaElement;
        private Vector2Int m_PreIndex = new Vector2Int(-1, -1);
        private bool m_PreValue = false;

        private void OnSceneGUI()
        {
            DrawPositionControl();
            if (Target.AreaData == null)
            {
                return;
            }

            float size = Target.GridSize;
            int x = Target.XGridNum;
            int y = Target.YGridNum;
            if (Target.AreaData.Points.Count < x * y)
            {
                while (Target.AreaData.Points.Count < x * y)
                {
                    Target.AreaData.Points.Add(false);
                }
            }

            for (int i = 0; i < x; i++)
            {
                for (int j = 0; j < y; j++)
                {
                    var index = GetIndex(i, j);
                    var pos = Target.transform.position +
                              new Vector3((i + 0.5f) * size, 0, (j + 0.5f) * size);
                    float pickSize = size / 3;
                    Handles.color = Target.AreaData.Points[index] ? Color.black : Color.green;
                    if (Handles.Button(pos, Quaternion.Euler(90, 0, 0), pickSize, pickSize, Handles.RectangleHandleCap))
                    {
                        var newValue = !Target.AreaData.Points[index];
                        Target.AreaData.Points[index] = newValue;
                        var isLine = false;
                        if (m_PreIndex.x != -1 && Target.FastLine && m_PreValue == newValue)
                        {
                            var minX = Mathf.Min(i, m_PreIndex.x);
                            var maxX = Mathf.Max(i, m_PreIndex.x);
                            var minY = Mathf.Min(j, m_PreIndex.y);
                            var maxY = Mathf.Max(j, m_PreIndex.y);
                            for (int x1 = minX; x1 <= maxX; x1++)
                            {
                                for (int y1 = minY; y1 <= maxY; y1++)
                                {
                                    Target.AreaData.Points[GetIndex(x1, y1)] = newValue;
                                }
                            }

                            isLine = true;
                        }

                        m_PreValue = newValue;
                        m_PreIndex = isLine ? new Vector2Int(-1, -1) : new Vector2Int(i, j);
                    }
                }
            }
        }

        void DrawPositionControl()
        {
            Vector3 pos = Target.transform.position;
            EditorGUI.BeginChangeCheck();
            Handles.color = Color.yellow;
            pos = Handles.PositionHandle(pos, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Target.transform.position = pos;
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Target.AreaData == null)
            {
                return;
            }


            if (Target.Terrain != null)
            {
                if (GUILayout.Button("适配地形"))
                {
                    float size = Target.GridSize;
                    int x = Target.XGridNum;
                    int y = Target.YGridNum;
                    var len = Target.Terrain.terrainData.size;
                    for (int i = 0; i < x; i++)
                    {
                        for (int j = 0; j < y; j++)
                        {
                            var index = GetIndex(i, j);
                            var pos = Target.transform.position +
                                      new Vector3((i + 0.5f) * size, 0, (j + 0.5f) * size);

                            var height = Target.Terrain.terrainData.GetInterpolatedHeight(pos.x / len.x, pos.z / len.z);
                            if (height < Target.MinHeight || height > Target.MaxHeight)
                            {
                                Target.AreaData.Points[index] = true;
                            }
                        }
                    }
                }
            }


            if (GUILayout.Button("重置数据"))
            {
                int x = Target.XGridNum;
                int y = Target.YGridNum;
                for (int i = 0; i < x; i++)
                {
                    for (int j = 0; j < y; j++)
                    {
                        var index = GetIndex(i, j);
                        Target.AreaData.Points[index] = false;
                    }
                }
            }

            // if (GUILayout.Button("保存数据"))
            // {
            //     Target.AlignData();
            //     EditorUtility.SetDirty(Target.AreaData);
            //     AssetDatabase.SaveAssets();
            //     AssetDatabase.Refresh();
            //     Debug.Log("区域寻路数据保存成功");
            // }
        }

        private int GetIndex(int x, int y)
        {
            return x * Target.YGridNum + y;
        }

        public static T LoadData<T>(string RootDir, string name) where T : ScriptableObject
        {
            if (!Directory.Exists(RootDir))
            {
                Debug.LogError("根目录不存在");
                RootDir = "Assets";
            }

            string filePath = $"{RootDir}/{name}.asset";
            if (!File.Exists(filePath))
            {
                Debug.Log($"Create new {name}.asset");
                var setting = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(setting, filePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return setting;
            }
            else
            {
                var setting = AssetDatabase.LoadAssetAtPath<T>(filePath);
                return setting;
            }
        }
    }
}
#endif