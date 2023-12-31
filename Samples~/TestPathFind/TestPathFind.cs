using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace TFW.AStar
{
    public class TestPathFind : MonoBehaviour
    {
        public GameObject WallSprite;
        private GameObject m_CurWall;
        public GameObject Actor;
        public Transform SpawnerPos;

        public TestType TestType;
        public Areas MapData;

        public static TestPathFind Instance;

        public int ActorCount = 500;

        public bool UseRandomRadius = false;
        
        public bool UseRandomPos = true;

        private void Awake()
        {
            Instance = this;
        }


        // Start is called before the first frame update
        void Start()
        {
            AStarMgr.Instance.Init();
            AStarMgr.Instance.AddMap(200003, MapData, transform);
            AStarMissionMgr.Instance.SetOneTimeDealMissionCount(ActorCount);
            for (int i = 0; i < ActorCount; i++)
            {
                var offset = Vector3.zero;
                if (UseRandomPos)
                {
                    offset = new Vector3(Random.Range(-24, 24), 0, Random.Range(-24, 24));
                }
                var newActor = GameObject.Instantiate(Actor, SpawnerPos.position + offset, Quaternion.identity);
                switch (TestType)
                {
                    case TestType.AStar:
                    case TestType.CmAStar:
                        var radius = UseRandomRadius ? Random.Range(0.5f, 2.5f) : 0.5f;
                        newActor.transform.localScale = new Vector3(radius, 1, radius);
                        var aStarAgent = newActor.AddComponent<AStarAgent>();
                        aStarAgent.Radius = radius;
                        aStarAgent.Speed = 5;
                        aStarAgent.SetCurMap(200003);
                        m_Agents.Add(aStarAgent);
                        break;
                    case TestType.NavMesh:
                        var agent = newActor.AddComponent<NavMeshAgent>();
                        agent.speed = 5;
                        m_NavMeshAgents.Add(agent);
                        break;
                }

                m_Actors.Add(newActor.transform);
            }

            //MultithreadingCMPathFind.Instance.InitPathFinder(AStarMgr.Instance.GetMapAreaMgr(200003).GetArea(100021));
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit))
                {
                    var dir = new Vector3(hit.point.x, 0, hit.point.z);
                    switch (TestType)
                    {
                        case TestType.AStar:
                            foreach (var aStarAgent in m_Agents)
                            {
                                aStarAgent.SetDestination(dir);
                            }

                            break;
                        case TestType.NavMesh:
                            foreach (var navMeshAgent in m_NavMeshAgents)
                            {
                                navMeshAgent.SetDestination(dir);
                            }

                            break;
                        // case TestType.CmAStar:
                        //     MultithreadingCMPathFind.Instance.SetData(m_Actors, hit.point);
                        //     //MultithreadingCMPathFind.Instance.Test1();
                        //     var time = GameTime.Time;
                        //     MultithreadingCMPathFind.Instance.DoStep();
                        //     Debug.Log("CMPathFind Time:" + (GameTime.Time - time) + "ms");
                        //     break;
                        // default:
                        //     throw new ArgumentOutOfRangeException();
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.A))
            {
                Destroy(m_CurWall);
                m_CurWall = new GameObject("wall");
                m_CurWall.transform.SetParent(null);
                var areaMgr = AStarMgr.Instance.GetMapAreaMgr(200003);
                foreach (var aStarArea in areaMgr.Areas)
                {
                    for (int i = 0; i < aStarArea.Data.Points.Count; i++)
                    {
                        if (!aStarArea.IsPointIsNotWall(i))
                        {
                            var pos = aStarArea.GetRealPosByIndex(i, 1);
                            var s = GameObject.Instantiate(WallSprite, m_CurWall.transform, true);
                            s.transform.position = pos + new Vector3(0, 0.1f, 0);
                        }
                    }
                }
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Click To Move");
        }
        

        private List<AStarAgent> m_Agents = new List<AStarAgent>();
        private List<NavMeshAgent> m_NavMeshAgents = new List<NavMeshAgent>();
        private List<Transform> m_Actors = new List<Transform>();
    }
    

    [System.Serializable]
    public enum TestType
    {
        AStar,
        NavMesh,
        CmAStar, //暂时没用到
    }
}