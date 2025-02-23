using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using UnityEngine;

namespace ModTool
{
    /// <summary>
    /// Dispatcher for running Coroutines and Actions on the main Thread.
    /// </summary>
    internal class Dispatcher : MonoBehaviour
    {
        private static Dispatcher instance;

        private static readonly Queue<Action> _executionQueue = new Queue<Action>();

        private static Thread main;

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            var go = new GameObject("Dispatcher");
            instance = go.AddComponent<Dispatcher>();

            DontDestroyOnLoad(go);
        }

        void Awake()
        {
            main = Thread.CurrentThread;
        }

        void Update()
        {
            ProcessQueue();
        }
        
        void OnDestroy()
        {
            ProcessQueue();
        }

        private void ProcessQueue()
        {
            lock (_executionQueue)
            {
                while (_executionQueue.Count > 0)
                {
                    _executionQueue.Dequeue().Invoke();
                }
            }
        }

        /// <summary>
        /// Enqueue an action on the main Thread.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="delayCall">If we already are on the main thread, enqueue and delay the call anyways.</param>
        public static void Enqueue(Action action, bool delayCall = true)
        {
            if (Thread.CurrentThread == main && !delayCall)
            {
                action.Invoke();
                return;
            }

            lock (_executionQueue)
                _executionQueue.Enqueue(action);            
        }
        
        /// <summary>
        /// Starts a coroutine.
        /// </summary>
        /// <param name="routine"></param>
        /// <returns></returns>
        public static new Coroutine StartCoroutine(IEnumerator routine)
        {
            return (instance as MonoBehaviour).StartCoroutine(routine);
        }

        /// <summary>
        /// Stops a coroutine.
        /// </summary>
        /// <param name="routine"></param>
        public static new void StopCoroutine(Coroutine routine)
        {
             (instance as MonoBehaviour).StopCoroutine(routine);
        }
    }
}
