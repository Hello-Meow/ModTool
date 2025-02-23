using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ModTool
{
    internal class AssetBundleResource : Resource<AssetBundleResource>
    {
        public string path { get; private set; }
        
        public AssetBundle assetBundle { get; private set; }

        public IReadOnlyList<string> assetPaths { get; private set; }

        public override bool canLoad => errors.Count == 0;

        private string manifestPath;

        public AssetBundleResource(string name, string path) : base(name)
        {
            this.path = path;

            manifestPath = path + ".manifest";

            GetAssetPaths();
        }

        protected override IEnumerator LoadResources()
        {
            AssetBundleCreateRequest assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(path);

            while (!assetBundleCreateRequest.isDone)
            {
                progress = assetBundleCreateRequest.progress;
                yield return null;
            }

            assetBundle = assetBundleCreateRequest.assetBundle;            
        }

        protected override IEnumerator UnloadResources()
        {
            if(assetBundle != null)
                assetBundle.Unload(true);
            
            assetBundle = null;

            yield break;
        }

        private void CheckFiles()
        {
            if (!File.Exists(path))            
                AddError(name + " asset bundle is missing");             

            if (!File.Exists(manifestPath))            
                AddError(name + " manifest is missing");
        }

        private void GetAssetPaths()
        {
            List<string> assetPaths = new List<string>();

            this.assetPaths = assetPaths.AsReadOnly();

            CheckFiles();

            if (!canLoad)
                return;
                        
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
