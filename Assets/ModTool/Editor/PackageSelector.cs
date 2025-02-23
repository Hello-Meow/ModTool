using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using ModTool.Shared;

namespace ModTool.Editor
{
    /// <summary>
    /// A window for selecting shared packages.
    /// </summary>
    public class PackageSelector : SelectionWindow<PackageSelector>
    {
        protected override string title => "Shared Packages";

        protected override string message => "Select packages to be shared with the mod exporter package.";

        private List<string> selected => ModToolSettings.sharedPackages;

        private ListRequest request;

        private List<PackageItem> items;

        private bool refreshing;

        private PropertyInfo source; 

        protected override void Init()
        {
            source = typeof(UnityEditor.PackageManager.PackageInfo).GetProperty("source");

            items = new List<PackageItem>();
            Refresh();
        }

        private void Update()
        {
            if (!refreshing)
                return;

            if (request.IsCompleted)
                UpdateItems();

            Repaint();
        }

        protected override void RenderItems()
        {
            if (refreshing)
            { 
                GUILayout.Label("Refreshing...");
                return;
            }

            Rect rect = EditorGUILayout.GetControlRect(false, items.Count * 16);

            for (int i = 0; i < items.Count; i++)
            {
                PackageItem item = items[i];

                Rect nodeRect = new Rect(rect.x, rect.y + i * 16, 200, 20);

                item.selected = GUI.Toggle(nodeRect, item.selected, string.IsNullOrEmpty(item.displayName) ? item.name : item.displayName);
            }
        }

        protected override void RenderFooter()
        {
            using (new EditorGUI.DisabledGroupScope(refreshing))
            {
                if (GUILayout.Button("Refresh"))
                    Refresh();
            }
        }

        protected override void SelectItems()
        {
            if (refreshing)
                return;

            selected.Clear();

            foreach (var item in items)
            {
                if (item.selected)
                    selected.Add(item.name);
            }

            EditorUtility.SetDirty(ModToolSettings.instance);
        }

        private void Refresh()
        {
            if (refreshing)
                return;

            refreshing = true;
            request = Client.List();
        }

        private void UpdateItems()
        {
            refreshing = false;

            items.Clear();

            foreach (var package in request.Result)
            {
                if (IsBuiltin(package))
                    continue;
                
                var item = new PackageItem()
                {
                    name = package.name,
                    displayName = package.displayName,
                    selected = selected.Contains(package.name)
                };

                items.Add(item);
            }
        }       

        private bool IsBuiltin(UnityEditor.PackageManager.PackageInfo package)
        {
            if (source == null)
                return false;

            int value = (int)source.GetValue(package, null);

            return value == 2;
        }

        private class PackageItem
        {
            public string name;
            public string displayName;
            public bool selected;
        }
    }
}
