using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace SurvivalEngine
{

    /// <summary>
    /// This is a wrapper class to use the NavMesh system functions simply
    /// It was supposed to be using a separate thread, but since Unity don't support NavMesh.CalculatePath on another thread, the 2nd thread is disabled for now.
    /// The only function you need to call is CalculatePath and it should return a path in callback when completed
    /// </summary>

    public class NavMeshToolPath
    {

        public Vector3 from;
        public Vector3 to;
        public int layerMask;

        public bool completed = false;
        public bool success = false;
        public Vector3[] path;
    }

    public class NavMeshTool
    {

        private static ConcurrentQueue<NavMeshToolPath> thread_list = new ConcurrentQueue<NavMeshToolPath>();

        public static void CalculatePath(Vector3 from, Vector3 to, int layerMask, UnityAction<NavMeshToolPath> callback)
        {
            NavMeshToolPath tpath = new NavMeshToolPath();
            tpath.from = from;
            tpath.to = to;
            tpath.layerMask = layerMask;
            thread_list.Enqueue(tpath);

            //Async (NavMesh.CalculatePath) dont work outside of main thread, when unity fix this we can use this function instead
            //DoCalculatePath(tpath, callback);

            //Temporary until unity add support for NavMesh.CalculatePath outside of main thread 
            CalculateThread();
            callback.Invoke(tpath);
        }

        private static async void DoCalculatePath(NavMeshToolPath tpath, UnityAction<NavMeshToolPath> callback)
        {
            await Task.Run(CalculateThread);

            callback.Invoke(tpath);
        }

        private static void CalculateThread()
        {
            NavMeshToolPath tpath;
            bool succ = thread_list.TryDequeue(out tpath);
            if (succ)
            {
                NavMeshPath path = new NavMeshPath();
                bool success = NavMesh.CalculatePath(tpath.from, tpath.to, tpath.layerMask, path);
                tpath.success = success && path.status == NavMeshPathStatus.PathComplete;
                tpath.path = path.corners;
                tpath.completed = true;
            }
        }
    }

}