using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace ModTool.Editor.Exporting
{
    [Serializable]
    public class Asset
    {
        public static readonly string backupDirectory = "Backup";

        public string name
        {
            get
            {
                return Path.GetFileNameWithoutExtension(assetPath);
            }
            set
            {
                string result = AssetDatabase.RenameAsset(assetPath, value);

                if (string.IsNullOrEmpty(result))
                    _assetPath = Path.Combine(Path.GetDirectoryName(assetPath), value + Path.GetExtension(assetPath));
            }
        }

        public string assetPath
        {
            get
            {
                return _assetPath;
            }
            set
            {
                string result = AssetDatabase.MoveAsset(assetPath, value);

                if (string.IsNullOrEmpty(result))
                    _assetPath = value;
            }
        }

        public string originalPath
        {
            get
            {
                return _originalPath;
            }
        }

        public string backupPath
        {
            get
            {
                return _backupPath;
            }
        }

        [SerializeField]
        private string _assetPath;
        [SerializeField]
        private string _originalPath;
        [SerializeField]
        private string _backupPath;

        public Asset(string assetPath)
        {
            _assetPath = assetPath;
            _originalPath = assetPath;            
        }

        public void Backup()
        {
            _backupPath = originalPath.Replace("Assets", Asset.backupDirectory);
            string backupDirectory = Path.GetDirectoryName(backupPath);

            if (!Directory.Exists(backupDirectory))
                Directory.CreateDirectory(backupDirectory);

            File.Copy(assetPath, backupPath);
            File.Copy(assetPath + ".meta", backupPath + ".meta");

            _originalPath = assetPath;
        }

        public void Restore()
        {
            if (!File.Exists(backupPath))
                return;
                        
            AssetDatabase.DeleteAsset(assetPath);

            _assetPath = originalPath;

            File.Copy(backupPath, assetPath, true);
            File.Copy(backupPath + ".meta", assetPath + ".meta", true);
        }

        public void SetAssetBundle(string assetBundleName, string assetBundleVariant = "")
        {
            AssetImporter importer = AssetImporter.GetAtPath(assetPath);

            importer.assetBundleName = assetBundleName;

            if (!string.IsNullOrEmpty(assetBundleName))
                importer.assetBundleVariant = assetBundleVariant;
        }
        
        public void Delete()
        {
            //Note: AssetDatabase.DeleteAsset updates the asset database, which can trigger an unwanted compilation/script reload
            File.Delete(assetPath);
            File.Delete(assetPath + ".meta");
        }
    }
}
