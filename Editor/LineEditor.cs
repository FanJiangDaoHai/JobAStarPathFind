

#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TFW.AStar.Editor
{
    [CanEditMultipleObjects, CustomEditor(typeof(Line))]
    public class LineEditor : UnityEditor.Editor
    {
        private Line Target => target as Line;

        private void OnSceneGUI()
        {
            if (Target.Points == null || Target.Points.Count == 0) return;
            for (int i = 0; i < Target.Points.Count; i++)
            {
                DrawPositionControl(i);
            }

            float cost = 0;
            for (int i = 0; i < Target.Points.Count - 1; i++)
            {
                Handles.color = Color.red;
                Handles.DrawLine(Target.Points[i] + Target.transform.position,
                    Target.Points[i + 1] + Target.transform.position);
                cost += Vector3.Distance(Target.Points[i], Target.Points[i + 1]);
            }

            if (Target.startAreaElement != null)
            {
                var startPos = Target.startAreaElement.GetGridLocalPos(Target.StartAreaPos);
                Handles.color = Color.red;
                Handles.DrawLine(startPos, Target.Points[0] + Target.transform.position);
                cost += Vector3.Distance(startPos, Target.Points[0]);
            }

            if (Target.endAreaElement != null)
            {
                var endPos = Target.endAreaElement.GetGridLocalPos(Target.EndAreaPos);
                Handles.color = Color.red;
                Handles.DrawLine(Target.Points.Last() + Target.transform.position, endPos);
                cost += Vector3.Distance(endPos, Target.Points.Last());
            }

            Target.Cost = cost;
        }

        void DrawPositionControl(int i)
        {
            Vector3 pos = Target.Points[i] + Target.transform.position;
            EditorGUI.BeginChangeCheck();
            Handles.color = Color.yellow;
            //float size = HandleUtility.GetHandleSize(pos) * 0.2f;
            Handles.SphereHandleCap(0, pos, Quaternion.identity, 0.2f, EventType.Repaint);
            pos = Handles.PositionHandle(pos, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Target.Points[i] = pos - Target.transform.position;
            }
        }
    }
}
#endif