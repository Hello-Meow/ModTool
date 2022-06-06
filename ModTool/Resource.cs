using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ModTool.Shared;

namespace ModTool
{
    /// <summary>
    /// Represents the load state of a Resource.
    /// </summary>
    public enum LoadState
    {
        /// <summary>
        /// The resource is unloaded.
        /// </summary>
        Unloaded,

        /// <summary>
        /// The resource is loading.
        /// </summary>
        Loading,

        /// <summary>
        /// The resource is fully loaded.
        /// </summary>
        Loaded,

        /// <summary>
        /// The resource is unloading.
        /// </summary>
        Unloading
    }

    /// <summary>
    /// A class that supports async loading of various resources.
    /// </summary>
    /// <typeparam name="T">Self referencing type of Resource</typeparam>
    public abstract class Resource<T> : Resource where T : Resource<T>
    {
        /// <summary>
        /// Occurs when the resource has completed loading.
        /// </summary>
        public new event Action<T> Loaded;

        /// <summary>
        /// Occurs when the resource has completed unloading.
        /// </summary>
        public new event Action<T> Unloaded;

        /// <summary>
        /// Make a new resource with a name.
        /// </summary>
        /// <param name="name"></param>
        public Resource(string name) : base(name)
        {
            
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            Loaded?.Invoke(this as T);
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();

            Unloaded?.Invoke(this as T);
        }
    }


    /// <summary>
    /// A class that supports async loading of various resources.
    /// </summary>
    public abstract class Resource
    {
        /// <summary>
        /// Occurs when the resource has completed loading.
        /// </summary>
        public event Action<Resource> Loaded;

        /// <summary>
        /// Occurs when the resource has completed unloading.
        /// </summary>
        public event Action<Resource> Unloaded;

        /// <summary>
        /// The current load state of the resource.
        /// </summary>
        public LoadState loadState { get; private set; }

        /// <summary>
        /// The resource's name.
        /// </summary>
        public string name { get; private set; }

        /// <summary>
        /// Can this resource currently be loaded?
        /// </summary>
        public virtual bool canLoad { get { return true; } }

        /// <summary>
        /// A value representing the loading progress ranging from 0 to 1.
        /// </summary>
        public float progress { get; protected set; }

        /// <summary>
        /// A collection of error messages related to this resource.
        /// </summary>
        public IReadOnlyList<string> errors { get; private set; }

        private List<string> _errors;

        public Resource(string name)
        {
            this.name = name;

            _errors = new List<string>();
            errors = _errors.AsReadOnly();
        }

        /// <summary>
        /// Load this resource.
        /// </summary>
        public void Load()
        {
            if (!canLoad)
                return;

            if (loadState == LoadState.Unloaded)
                Dispatcher.StartCoroutine(Loading());

            if (loadState == LoadState.Unloading)
                loadState = LoadState.Loading;
        }


        /// <summary>
        /// Unload this resource.
        /// </summary>
        public void Unload()
        {
            if (loadState == LoadState.Loaded)
                Dispatcher.StartCoroutine(Unloading());

            if (loadState == LoadState.Loading)
                loadState = LoadState.Unloading;
        }

        private IEnumerator Loading()
        {
            LogUtility.LogDebug("Loading " + name);

            loadState = LoadState.Loading;

            yield return LoadResources();

            if (loadState == LoadState.Unloading)
                yield return Unloading();
            else
            {
                loadState = LoadState.Loaded;
                progress = 1;
                OnLoaded();
            }
        }

        private IEnumerator Unloading()
        {
            LogUtility.LogDebug("Unloading " + name);

            loadState = LoadState.Unloading;

            yield return UnloadResources();

            if (loadState == LoadState.Loading)
                yield return Loading();
            else
            {
                loadState = LoadState.Unloaded;
                progress = 0;
                OnUnloaded();
            }
        }

        protected virtual void OnLoaded()
        {
            LogUtility.LogDebug("Loaded " + name);

            Loaded?.Invoke(this);
        }

        protected virtual void OnUnloaded()
        {
            LogUtility.LogDebug("Unloaded " + name);

            Unloaded?.Invoke(this);
        }

        protected void AddError(string message)
        {
            _errors.Add(message);
        }

        protected void AddErrors(IEnumerable<string> messages)
        {
            foreach (var message in messages)
                AddError(message);
        }

        /// <summary>
        /// Use this to implement a process to load resources.
        /// </summary>
        /// <returns></returns>
        protected abstract IEnumerator LoadResources();

        /// <summary>
        /// Use this to implement a process to unload resources.
        /// </summary>
        /// <returns></returns>
        protected abstract IEnumerator UnloadResources();
    }
}