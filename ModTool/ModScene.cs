using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;
using ModTool.Interface;

namespace ModTool
{
    /// <summary>
    /// Represents a Scene that is included in a Mod.
    /// </summary>
    public class ModScene : Resource
    {
        /// <summary>
        /// This ModScene's Scene.
        /// </summary>
        public Scene? scene { get; private set; }

        /// <summary>
        /// The Mod this scene belongs to.
        /// </summary>
        public Mod mod { get; private set; }

        /// <summary>
        /// Can the scene be loaded? False if this scene's Mod is not loaded.
        /// </summary>
        public override bool canLoad
        {
            get { return mod.loadState == ResourceLoadState.Loaded; }
        }

        /// <summary>
        /// Initialize a new ModScene with a Scene name and a Mod
        /// </summary>
        /// <param name="name">The scene's name</param>
        /// <param name="mod">The Mod this ModScene belongs to.</param>
        public ModScene(string name, Mod mod) : base(name)
        {
            this.mod = mod;
            scene = null;
        }
                  
        protected override IEnumerator LoadResources()
        {
            //NOTE: Loading a scene synchronously prevents the scene from being initialized, so force async loading.
            yield return LoadResourcesAsync();            
        }
                
        protected override IEnumerator LoadResourcesAsync()
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
            loadOperation.allowSceneActivation = false;

            while (loadOperation.progress < .9f)
            {
                loadProgress = loadOperation.progress;
                yield return null;
            }

            loadOperation.allowSceneActivation = true;

            yield return loadOperation;
            
            scene = SceneManager.GetSceneByName(name);
                        
            SetActive();
        }
        
        protected override void UnloadResources()
        {
            if (scene.HasValue)
                scene.Value.Unload();

            scene = null;
        }

        /// <summary>
        /// Set this ModScene's Scene as the active scene.
        /// </summary>
        public void SetActive()
        {
            if (scene.HasValue)
                SceneManager.SetActiveScene(scene.Value);
        }

        protected override void OnLoaded()
        {           
            foreach (IModHandler modHandler in GetComponentsInScene<IModHandler>())
                modHandler.OnLoaded(mod.contentHandler);

            base.OnLoaded();
        }

        /// <summary>
        /// Returns the first Component of type T in this Scene.
        /// </summary>
        /// <typeparam name="T">The Component that will be looked for.</typeparam>
        /// <returns>An array of found Components of Type T.</returns>
        public T GetComponentInScene<T>()
        {
            if (loadState != ResourceLoadState.Loaded)
                return default(T);

            return scene.Value.GetComponentInScene<T>();
        }

        /// <summary>
        /// Returns all Components of type T in this Scene.
        /// </summary>
        /// <typeparam name="T">The Component that will be looked for.</typeparam>
        /// <returns>An array of found Components of Type T.</returns>
        public T[] GetComponentsInScene<T>()
        {
            if (loadState != ResourceLoadState.Loaded)
                return new T[0];

            return scene.Value.GetComponentsInScene<T>();
        }        
    }
}
