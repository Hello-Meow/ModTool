using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using UnityEngine;
using ModTool.Shared;

namespace ModTool
{
    internal class AssetBundleResource : Resource
    {
        public string path { get; private set; }
        
        public AssetBundle assetBundle { get; private set; }

        public ReadOnlyCollection<string> assetPaths { get; private set; }

        public override bool canLoad
        {
            get
            {
                return _canLoad;
            }
        }

        private bool _canLoad;

        public AssetBundleResource(string name, string path) : base(name)
        {
            this.path = path;

            _canLoad = false;

            GetAssetPaths();
        }

        protected override IEnumerator LoadResources()
        {
            assetBundle = AssetBundle.LoadFromFile(path);

            yield break;
        }

        protected override IEnumerator LoadResourcesAsync()
        {
            AssetBundleCreateRequest assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(path);
            
            while(!assetBundleCreateRequest.isDone)
            {
                loadProgress = assetBundleCreateRequest.progress;
                yield return null;
            }

            assetBundle = assetBundleCreateRequest.assetBundle;
        }

        protected override void UnloadResources()
        {
            if(assetBundle != null)
                assetBundle.Unload(true);

            assetBundle = null;
        }

        private void GetAssetPaths()
        {
            List<string> assetPaths = new List<string>();

            this.assetPaths = assetPaths.AsReadOnly();

            if (string.IsNullOrEmpty(path))
                return;

            if (!File.Exists(path))
                return;

            string manifestPath = path + ".manifest";

            if (!File.Exists(manifestPath))
            {
                LogUtility.LogWarning(name + " manifest missing");
                return;
            }

            _canLoad = true;

            //TODO: long lines in manifest are formatted?
            string[] lines = File.ReadAllLines(manifestPath);

            int start = Array.IndexOf(lines, "Assets:") + 1;

            for (int i = start; i < lines.Length; i++)
            {
                if (!lines[i].StartsWith("- "))
                    break;

                string assetPath = lines[i].Substring(2);

                //Note: if the asset is a scene, we only need the name
                if (assetPath.EndsWith(".unity"))
                    assetPath = Path.GetFileNameWithoutExtension(assetPath);

                assetPaths.Add(assetPath);
            }
        }
    }
}
