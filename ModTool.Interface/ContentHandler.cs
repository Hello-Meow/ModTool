using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace ModTool.Interface
{
    /// <summary>
    /// Handles a Mod's content.
    /// </summary>
    public class ContentHandler
    {
        /// <summary>
        /// The Mod resource.
        /// </summary>
        public IResource mod { get; private set; }

        /// <summary>
        /// The Mod's ModScene resources.
        /// </summary>
        public ReadOnlyCollection<IResource> modScenes { get; private set; }

        /// <summary>
        /// The Mod's prefabs. 
        /// </summary>
        public ReadOnlyCollection<GameObject> prefabs { get; private set; }
                
        private List<GameObject> gameObjects;
        
        /// <summary>
        /// Initialize a new ContentHandler with a Mod, ModScenes and prefabs.
        /// </summary>
        /// <param name="mod">A Mod resource</param>
        /// <param name="modScenes">ModScene resources</param>
        /// <param name="prefabs">prefab GameObjects</param>
        public ContentHandler(IResource mod, ReadOnlyCollection<IResource> modScenes, ReadOnlyCollection<GameObject> prefabs)
        {
            this.mod = mod;
            this.modScenes = modScenes;
            this.prefabs = prefabs;

            gameObjects = new List<GameObject>();
        }

        /// <summary>
        /// Add a Component to a GameObject.
        /// </summary>
        /// <typeparam name="T">The Component Type.</typeparam>
        /// <param name="gameObject">The GameObject to which to add the Component.</param>
        /// <returns>The added Component.</returns>
        public T AddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.AddComponent<T>();

            InitializeComponent(component);

            return component;
        }

        /// <summary>
        /// Add a Component to a GameObject.
        /// </summary>
        /// <param name="componentType">The Component Type.</param>
        /// <param name="gameObject">The GameObject to which to add the Component.</param>
        /// <returns>The added Component.</returns>
        public Component AddComponent(System.Type componentType, GameObject gameObject)
        {
            Component component = gameObject.AddComponent(componentType);

            InitializeComponent(component);

            return component;
        }
               
        private void InitializeComponent(Component component)
        {
            if (component is IModHandler)
            {
                IModHandler modHandler = component as IModHandler;
                modHandler.OnLoaded(this);
            }
        }
        
        private void InitializeGameObject(GameObject go)
        {
            Component[] components = go.GetComponentsInChildren<Component>();

            foreach(Component component in components)
            {
                InitializeComponent(component);
            }
        }

        private void InitializeObject(Object obj)
        {
            if (obj is GameObject)
            {
                GameObject gameObject = obj as GameObject;
                gameObjects.Add(gameObject);
                InitializeGameObject(gameObject);
            }
            else if (obj is Component)
            {
                Component component = obj as Component;
                gameObjects.Add(component.gameObject);
                InitializeGameObject(component.gameObject);
            }
        }

        /// <summary>
        /// Create a copy of the Object original.
        /// </summary>
        /// <typeparam name="T">The Object's Type.</typeparam>
        /// <param name="original">An existing Object to copy.</param>
        /// <returns>The new Object.</returns>
        public T Instantiate<T>(T original) where T : UnityEngine.Object
        {
            T obj = UnityEngine.Object.Instantiate(original);

            InitializeObject(obj);

            return obj;
        }

        /// <summary>
        /// Create a copy of the Object original.
        /// </summary>
        /// <param name="original">An existing Object to copy.</param>
        /// <param name="position">The position for the new Object.</param>
        /// <param name="rotation">The roration for the new Object.</param>
        /// <returns>The new Object.</returns>
        public Object Instantiate(UnityEngine.Object original, Vector3 position, Quaternion rotation)
        {
            var obj = UnityEngine.Object.Instantiate(original, position, rotation);

            InitializeObject(obj);

            return obj;
        }

        /// <summary>
        /// Create a copy of the Object original.
        /// </summary>
        /// <param name="original">An existing Object to copy.</param>
        /// <returns>The new Object.</returns>
        public UnityEngine.Object Instantiate(UnityEngine.Object original)
        {
            return Instantiate(original, Vector3.zero, Quaternion.identity);
        }

        /// <summary>
        /// Destroy an Object.
        /// </summary>
        /// <param name="obj">The Object to destroy.</param>
        public void Destroy(Object obj)
        {
            if (obj == null)
                return;
            
            if(obj is GameObject)
            {
                GameObject gameObject = obj as GameObject;

                if (gameObjects.Contains(gameObject)) 
                    gameObjects.Remove(gameObject);
            }
            
            Object.Destroy(obj);
        }

        /// <summary>
        /// Destroy all instantiated GameObjects.
        /// </summary>
        public void Clear()
        {
            foreach(GameObject gameObject in gameObjects)
            {
                if (gameObject != null)
                {
                    Object.Destroy(gameObject);
                }
            }

            gameObjects.Clear();
        }
    }
}
