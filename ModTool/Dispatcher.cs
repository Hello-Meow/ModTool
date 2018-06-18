using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading;

namespace ModTool
{
    /// <summary>
    /// Singleton Component for dispatching Coroutines and Actions on the main Thread.
    /// </summary>
    internal class Dispatcher : UnitySingleton<Dispatcher>
    {
        private readonly Queue<Action> _executionQueue = new Queue<Action>();
        private Thread main;

        protected override void Awake()
        {
            base.Awake();

            main = Thread.CurrentThread;
        }

        void Update()
        {
            ProcessQueue();
        }
        
        protected override void OnDestroy()
        {
            ProcessQueue();
            base.OnDestroy();
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
        /// Enqueue a Coroutine.
        /// </summary>
        /// <param name="routine"></param>
        public void Enqueue(IEnumerator routine)
        {
            Enqueue(() =>
            {
                StartCoroutine(routine);
            });            
        }

        /// <summary>
        /// Enqueue an action on the main Thread.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="delayCall"></param>
        public void Enqueue(Action action, bool delayCall = false)
        {
            //Don't queue if we're on the main thread.
            if (Thread.CurrentThread == main && !delayCall)
            {
                action.Invoke();
                return;
            }

            lock (_executionQueue)
            {
                _executionQueue.Enqueue(action);
            }
        }        
    }
}
