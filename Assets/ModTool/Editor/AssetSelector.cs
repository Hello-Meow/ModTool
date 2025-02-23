using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ModTool.Shared;

namespace ModTool.Editor
{
    /// <summary>
    /// A window for selecting shared assets.
    /// </summary>
    public class AssetSelector : SelectionWindow<AssetSelector>
    {
        protected override string title => "Shared Assets";

        protected override string message => "Select assets to be included with the mod exporter package.";

        private List<string> selected => ModToolSettings.sharedAssets;

        private FileNode root;

        protected override void Init()
        {
            root = new FileNode(new DirectoryInfo(Application.dataPath + "/"));

            SelectAssets(selected);
            ExpandSelected(root);
        }

        protected override void RenderItems()
        {
            List<FileNode> nodes = new List<FileNode>();

            GetExpandedNodes(root, nodes);

            Rect rect = EditorGUILayout.GetControlRect(false, nodes.Count * 16);
            
            List<FileNode> selectedChildren = new List<FileNode>();

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];

                Rect nodeRect = new Rect(rect.x + (node.depth - 1) * 18, rect.y + i * 16, 20, 20);

                if (node.isDirectory && node.children.Count > 0)
                    node.expanded = GUI.Toggle(nodeRect, node.expanded, GUIContent.none, "IN foldout");

                nodeRect.x += 18;

                if (node.isDirectory)
                {
                    bool selected = node.selected;

                    using (new EditorGUI.DisabledGroupScope(node.children.Count == 0))
                        node.selected = GUI.Toggle(nodeRect, selected, GUIContent.none);

                    if (node.selected != selected)
                        SelectChildren(node, node.selected);

                    selectedChildren.Clear();
                    GetSelectedNodes(node, selectedChildren);

                    node.selected = selectedChildren.Count > 0;
                }
                else
                    node.selected = GUI.Toggle(nodeRect, node.selected, GUIContent.none);

                nodeRect.x += 16;

                node.RenderNode(nodeRect);
            }
        }

        protected override void SelectItems()
        {
            selected.Clear();

            GetSelectedNodes(root, selected);
            
            EditorUtility.SetDirty(ModToolSettings.instance);
        }

        private void SelectAssets(List<string> toBeSelected)
        {
            List<FileNode> selected = new List<FileNode>();

            GetNodesByName(root, selected, toBeSelected);

            foreach (var node in selected)
            {
                node.selected = true;
                node.expanded = true;
            }
        }

        private void GetNodesByName(FileNode root, List<FileNode> nodes, List<string> names)
        {
            foreach (var node in root.children)
            {
                if (names.Contains(node.relativePath))
                    nodes.Add(node);

                GetNodesByName(node, nodes, names);
            }
        }

        private void GetSelectedNodes(FileNode root, List<FileNode> nodes)
        {
            foreach (var node in root.children)
            {
                if (node.selected)
                    nodes.Add(node);

                GetSelectedNodes(node, nodes);
            }
        }

        private void GetSelectedNodes(FileNode root, List<string> selected)
        {
            foreach (var node in root.children)
            {
                if (!node.isDirectory && node.selected)
                    selected.Add(node.relativePath);

                GetSelectedNodes(node, selected);
            }
        }

        private void GetExpandedNodes(FileNode root, List<FileNode> nodes)
        {
            foreach (var node in root.children)
            {
                nodes.Add(node);

                if (node.expanded)
                    GetExpandedNodes(node, nodes);
            }
        }

        private void SelectChildren(FileNode root, bool select)
        {
            foreach (var node in root.children)
            {
                node.selected = select;
                SelectChildren(node, select);
            }
        }

        private void ExpandSelected(FileNode root)
        {
            List<FileNode> selected = new List<FileNode>();

            GetSelectedNodes(root, selected);

            if (selected.Count > 0)
                root.expanded = true;

            foreach (var node in root.children)
            {
                if (node.isDirectory)
                    ExpandSelected(node);
            }
        }

        private class AlphabeticComparer : IComparer<FileNode>
        {
            public int Compare(FileNode a, FileNode b) => a.relativePath.CompareTo(b.relativePath);
        }

        private class FileNode
        {
            public bool selected { get; set; }
            public bool expanded { get; set; }
            public bool isDirectory { get; private set; }

            public int depth { get; private set; }

            public string name { get; private set; }
            public string fullPath { get; private set; }
            public string relativePath { get; private set; }

            public List<FileNode> children { get; private set; }

            private static IComparer<FileNode> alphabeticalComparer = new AlphabeticComparer();

            private Texture icon;

            public FileNode(FileSystemInfo fileInfo, int depth = 0)
            {
                fullPath = fileInfo.FullName;
                name = fileInfo.Name;

                this.depth = depth;

                relativePath = "Assets/" + fullPath.Substring(Application.dataPath.Length + 1);
                relativePath = relativePath.Replace("\\", "/");

                children = new List<FileNode>();

                if (fileInfo is DirectoryInfo)
                {
                    icon = EditorGUIUtility.FindTexture("Folder Icon");
                    isDirectory = true;

                    foreach (FileSystemInfo fileSystemInfo in (fileInfo as DirectoryInfo).GetFileSystemInfos())
                    {
                        if (fileSystemInfo.Name.EndsWith(".meta") || fileSystemInfo.Name == "ModTool")
                            continue;

                        FileNode fileNode = new FileNode(fileSystemInfo, depth + 1);
                        children.Add(fileNode);                        
                    }

                    children.Sort(alphabeticalComparer);
                }
                else
                {
                    icon = (AssetDatabase.GetCachedIcon(relativePath) as Texture2D);

                    if (icon)
                        return;

                    icon = EditorGUIUtility.ObjectContent(null, typeof(MonoBehaviour)).image;
                }
            }

            public void RenderIconText()
            {
                GUILayout.Label(icon, GUILayout.Height(20f), GUILayout.Width(20f));
                GUILayout.Label(name, GUILayout.Height(15f));

                GUILayout.FlexibleSpace();
            }

            public void RenderNode(Rect rect)
            {
                rect.width = 20;

                GUI.Label(rect, icon);

                rect.x += 17;
                rect.width = 200;

                GUI.Label(rect, name);
            }
        }
    }
}
