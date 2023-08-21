

using UnityEngine;

namespace TFW.AStar
{
    public class AreaCreate : MonoBehaviour
    {
        public string DataSaveRootDirectory = "Assets";
        public Terrain Terrain;
        [DisplayOnly] public Areas Areas;
        public int CurMapId;
        public int CurAddAreaId;
    }
}