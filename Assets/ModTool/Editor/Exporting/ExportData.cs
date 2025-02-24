using System;
using System.Collections.Generic;
using ModTool.Shared;

namespace ModTool.Editor.Exporting
{
    /// <summary>
    /// Class that stores data during the exporting process.
    /// </summary>
    [Serializable]
    public class ExportData
    {
        public List<Asset> assemblyDefinitions = new List<Asset>();

        public List<Asset> assemblies = new List<Asset>();

        public List<Asset> assets = new List<Asset>();

        public List<Asset> scenes = new List<Asset>();

        public ModContent content = 0;

        public string loadedScene;
    }
}
