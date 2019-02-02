using System;
using System.Collections;
using ModTool.Interface;

namespace ModTool
{
    /// <summary>
    /// Represents a load state.
    /// </summary>
    public enum ResourceLoadState { Unloaded, Loading, Loaded, Cancelling, Unloading }

    /// <summary>
    /// A class that supports async loading of various resources.
    /// </summary>
    public abstract class Resource : IResource
    {
        /// <summary>
        /// Occurs when this Resource has been loaded.
        /// </summary>
        public event Action<Resource> Loaded;

        /// <summary>
        /// Occurs when this Resource has been unloaded.
        /// </summary>
        public event Action<Resource> Unloaded;

        /// <summary>
        /// Occurs when this Resource's async loading has been cancelled.
        /// </summary>
        public event Action<Resource> LoadCancelled;

        /// <summary>
        /// Occurs when this Resources async loading has been resumed.
        /// </summary>
        public event Action<Resource> LoadResumed;

        /// <summary>
        /// Occurs when this Resource's loadProgress changes.
        /// </summary>
        public event Action<float> LoadProgress;
        
        /// <summary>
        /// This Resource's name.
        /// </summary>
        public string name { get; private set; }

        /// <summary>
        /// Is this Resource busy loading?
        /// </summary>
        public virtual bool isBusy { get { return _loadState.isBusy; } }

        /// <summary>
        /// Can this Resource be loaded?
        /// </summary>
        public virtual bool canLoad { get { return true; } }

        /// <summary>
        /// This Resource's current load state.
        /// </summary>
        public ResourceLoadState loadState { get { return _loadState.loadState; } }

        /// <summary>
        /// What is the Resource's load progress.
        /// </summary>
        public float loadProgress
        {
            get
            {
                return _loadProgress;
            }
            protected set
            {
                if (value == _loadProgress)
                    return;

                _loadProgress = value;
                LoadProgress?.Invoke(_loadProgress);
            }
        }

        private LoadState _loadState;
        private float _loadProgress = 0;
        
        /// <summary>
        /// Initialize a Resource with a name.
        /// </summary>
        /// <param name="name">The Resource's name</param>
        protected Resource(string name)
        {
            this.name = name;
            _loadState = new UnloadedState(this);
        }

        /// <summary>
        /// Load this Resource.
        /// </summary>
        public void Load()
        {
            Dispatcher.instance.Enqueue(LoadCoroutine());
        }

        /// <summary>
        /// Load this Resource asynchronously.
        /// </summary>
        public void LoadAsync()
        {
            Dispatcher.instance.Enqueue(LoadAsyncCoroutine());
        }

        /// <summary>
        /// Coroutine that loads this Resource.
        /// </summary>
        public IEnumerator LoadCoroutine()
        {
            yield return _loadState.Load();
        }

        /// <summary>
        /// Coroutine that loads this Resource asynchronously.
        /// </summary>
        public IEnumerator LoadAsyncCoroutine()
        {
            yield return _loadState.LoadAsync();
        }

        /// <summary>
        /// Unload this Resource.
        /// </summary>
        public void Unload()
        {
            _loadState.Unload();
        }

        /// <summary>
        /// Finalize the current LoadState.
        /// </summary>
        protected void End()
        {
            _loadState.End();
        }

        /// <summary>
        /// Use this to implement anything that should happen before unloading this Resource.
        /// </summary>
        protected virtual void PreUnLoadResources()
        {

        }

        /// <summary>
        /// Use this to implement unloading this Resource.
        /// </summary>
        protected abstract void UnloadResources();

        /// <summary>
        /// Use this to implement loading this Resource.
        /// </summary>
        protected abstract IEnumerator LoadResources();

        /// <summary>
        /// Use this to implement loading this Resource asynchronously.
        /// </summary>
        protected abstract IEnumerator LoadResourcesAsync();

        /// <summary>
        /// Handle end of loading.
        /// </summary>
        protected virtual void OnLoaded()
        {
            loadProgress = 1;
            Loaded?.Invoke(this);
        }

        /// <summary>
        /// Handle end of unloading.
        /// </summary>
        protected virtual void OnUnloaded()
        {
            _loadProgress = 0;
            Unloaded?.Invoke(this);
        }

        /// <summary>
        /// Handle load cancelling.
        /// </summary>
        protected virtual void OnLoadCancelled()
        {
            LoadCancelled?.Invoke(this);
        }

        /// <summary>
        /// Handle load resuming.
        /// </summary>
        protected virtual void OnLoadResumed()
        {
            LoadResumed?.Invoke(this);
        }
        
        private abstract class LoadState
        {
            protected Resource resource;

            public virtual bool isBusy { get { return false; } }

            public abstract ResourceLoadState loadState { get; }

            protected LoadState(Resource resource)
            {
                this.resource = resource;
            }

            public virtual IEnumerator Load()
            {
                yield break;
            }

            public virtual IEnumerator LoadAsync()
            {
                yield break;
            }

            public virtual void Unload()
            {

            }

            public virtual void End()
            {

            }
        }

        class UnloadedState : LoadState
        {
            public override ResourceLoadState loadState
            {
                get { return ResourceLoadState.Unloaded; }
            }

            public UnloadedState(Resource resource) : base(resource)
            {

            }

            public override IEnumerator Load()
            {
                if (resource.canLoad)
                {
                    resource._loadState = new LoadingState(resource);
                    yield return resource.LoadResources(); //TODO: this skips a frame
                    resource.End();                    
                }
            }

            public override IEnumerator LoadAsync()
            {
                if (resource.canLoad)
                {
                    resource._loadState = new LoadingState(resource);
                    yield return resource.LoadResourcesAsync();
                    resource.End();
                }
            }
        }

        class LoadingState : LoadState
        {
            public override bool isBusy
            {
                get { return true; }
            }

            public override ResourceLoadState loadState
            {
                get { return ResourceLoadState.Loading; }
            }

            public LoadingState(Resource resource) : base(resource)
            {

            }

            public override void End()
            {
                resource._loadState = new LoadedState(resource);                
                resource.OnLoaded();
            }

            public override void Unload()
            {
                resource._loadState = new CancellingState(resource);
            }
        }

        class LoadedState : LoadState
        {
            public override ResourceLoadState loadState
            {
                get { return ResourceLoadState.Loaded; }
            }

            public LoadedState(Resource resource) : base(resource)
            {

            }

            public override void Unload()
            {
                if (resource.isBusy)
                {
                    resource.PreUnLoadResources();
                    resource._loadState = new UnloadingState(resource);
                }
                else
                {
                    resource.PreUnLoadResources();
                    resource.UnloadResources();
                    resource._loadState = new UnloadedState(resource);
                    resource.OnUnloaded();
                }
            }
        }

        class CancellingState : LoadState
        {
            public override bool isBusy
            {
                get { return true; }
            }

            public override ResourceLoadState loadState
            {
                get { return ResourceLoadState.Cancelling; }
            }

            public CancellingState(Resource resource) : base(resource)
            {

            }

            public override IEnumerator Load()
            {               
                resource.OnLoadResumed();
                resource._loadState = new LoadingState(resource);
                yield break;
            }

            public override IEnumerator LoadAsync()
            {
                resource.OnLoadResumed();
                resource._loadState = new LoadingState(resource);
                yield break;
            }

            public override void End()
            {
                resource._loadState = new UnloadedState(resource);     
                resource.PreUnLoadResources();
                resource.UnloadResources();
                resource.OnLoadCancelled();
            }
        }

        class UnloadingState : LoadState
        {
            public override bool isBusy
            {
                get { return true; }
            }

            public override ResourceLoadState loadState
            {
                get { return ResourceLoadState.Unloading; }
            }

            public UnloadingState(Resource resource) : base(resource)
            {

            }

            public override IEnumerator Load()
            {
                resource._loadState = new LoadedState(resource);
                resource.OnLoaded();
                yield break;
            }

            public override IEnumerator LoadAsync()
            {
                resource._loadState = new LoadedState(resource);
                resource.OnLoaded();
                yield break;
            }

            public override void End()
            {               
                resource.PreUnLoadResources();
                resource.UnloadResources();
                resource._loadState = new UnloadedState(resource);
                resource.OnUnloaded();
            }
        }
    }
}
