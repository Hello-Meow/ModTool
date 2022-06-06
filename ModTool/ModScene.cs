using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace ModTool
{
    /// <summary>
    /// Represents a Scene that is included in a Mod.
    /// </summary>
    public class ModScene : Resource<ModScene>
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
            get { return mod.loadState == LoadState.Loaded; }
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
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
            
            while(!loadOperation.isDone)
            {
                progress = loadOperation.progress;
                yield return null;
            }

            scene = SceneManager.GetSceneByName(name);

            SetActive();
        }
        
        protected override IEnumerator UnloadResources()
        {
            if (scene.HasValue)
                yield return SceneManager.UnloadSceneAsync(scene.Value);

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

        /// <summary>
        /// Returns the first Component of type T in this Scene.
        /// </summary>
        /// <typeparam name="T">The Component that will be looked for.</typeparam>
        /// <returns>An array of found Components of Type T.</returns>
        public T GetComponentInScene<T>()
        {
            if (loadState != LoadState.Loaded)
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
            if (loadState != LoadState.Loaded)
                return new T[0];

            return scene.Value.GetComponentsInScene<T>();
        }

        /// <summary>
        /// Returns all Components of type T in this Scene.
        /// </summary>
        /// <typeparam name="T">The Component that will be looked for.</typeparam>
        /// <param name="components">A List that will be filled with found Components.</param>
        public void GetComponentsInScene<T>(List<T> components)
        {
            scene.Value.GetComponentsInScene(components);
        }
    }
}
