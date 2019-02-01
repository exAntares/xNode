using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static XNodeEditor.NodeEditorWindow;

namespace XNodeEditor {
    public static class SearchWindowUtils {
        private static Vector2 scrollPosition;
        private static TreeNode root;
        private static TreeNode currentNode;
        private class TreeNode {
            public string Name { get; }
            public TreeNode Parent { get; }
            public Type NodeType { get; }
            public Dictionary<string, TreeNode> Children { get; }
            public TreeNode(string[] paths, Type nodeType, TreeNode parent) {
                Children = new Dictionary<string, TreeNode>();
                Parent = parent;
                if (Parent != null) {
                    Name = paths[0];
                    if (paths.Length > 1) {
                        AddPaths(paths.Skip(1).ToArray(), nodeType);
                    } else {
                        // leaf node
                        NodeType = nodeType;
                    }
                } else {
                    AddPaths(paths, nodeType);
                }
            }

            public void AddPaths(string[] paths, Type nodeType) {
                if (Children.TryGetValue(paths[0], out TreeNode node)) {
                    if (paths.Length > 1) {
                        node.AddPaths(paths.Skip(1).ToArray(), nodeType);
                    }
                } else {
                    node = new TreeNode(paths, nodeType, this);
                    Children[paths[0]] = node;
                }
            }
        }

        private static GUIStyle ButtonStyle = new GUIStyle(GUI.skin.button) {
            alignment = TextAnchor.MiddleRight,
            onHover = new GUIStyleState() { textColor = Color.black },
            //normal = new GUIStyleState() {  textColor = Color.blue, background = EditorGUIUtility.Load("Folder Icon") as Texture2D }
        };

        private static GUIStyle BackButtonStyle = new GUIStyle(GUI.skin.button) {
            alignment = TextAnchor.MiddleLeft,
        };

        public static void DrawWindow(ref string searchText, Type[] nodeTypes, Vector2 contextMenuMousePos, NodeEditorWindow parentWindow) {
            searchText = GUILayout.TextField(searchText, GUI.skin.FindStyle("ToolbarSeachTextField"), GUILayout.Height(50));
            var words = new string[0];
            if (!string.IsNullOrEmpty(searchText)) {
                words = searchText.Split(' ')
                    .Distinct()
                    .Where(s => !string.IsNullOrEmpty(s) && s != " ")
                    .Select(x => x.ToLower())
                    .ToArray();

                var typeNames = nodeTypes.Select(x => new { type = x, GetNodeMenuData(x).name, GetNodeMenuData(x).tags })
                    .Where(x => {
                        if (words.Length <= 0) {
                            return true;
                        }
                        var tags = x.tags.Union(new[] { x.name }).Select(t => t.ToLower());
                        var matchedWords = words.Where(w => tags.Any(tag => tag.Contains(w)));
                        return matchedWords.Count() == words.Length;
                    })
                    .ToArray();

                foreach (var availableNodeType in typeNames) {
                    if (GUILayout.Button(Path.GetFileName(availableNodeType.name), GUILayout.Height(50))) {
                        Vector2 curPos = parentWindow.WindowToGridPosition(contextMenuMousePos);
                        parentWindow.graphEditor.CreateNode(availableNodeType.type, curPos);
                        currentActivity = NodeActivity.Idle;
                        parentWindow.Repaint();
                    }
                }
            } else {
                var typeNames = nodeTypes.Select(x => new { type = x, GetNodeMenuData(x).name });
                foreach (var nodeType in typeNames) {
                    var paths = nodeType.name.Split('/');
                    if (root == null) {
                        root = new TreeNode(paths, nodeType.type, null);
                    } else {
                        root.AddPaths(paths, nodeType.type);
                    }
                }

                currentNode = currentNode ?? root;
                if (currentNode != root) {
                    if (GUI.Button(EditorGUILayout.GetControlRect(GUILayout.Height(20)), "< Back", BackButtonStyle)) {
                        currentNode = currentNode.Parent;
                    }
                    GUILayout.Space(20);
                }

                foreach (var keyvalue in currentNode.Children) {
                    var isLeafNode = keyvalue.Value.Children.Count <= 0;
                    GUILayout.BeginHorizontal();
                    if (GUI.Button(EditorGUILayout.GetControlRect(GUILayout.Height(50)), $"{keyvalue.Key}{(isLeafNode?string.Empty:" >")}", ButtonStyle)) {
                        if (keyvalue.Value.Children.Count <= 0) {
                            Vector2 curPos = parentWindow.WindowToGridPosition(contextMenuMousePos);
                            parentWindow.graphEditor.CreateNode(keyvalue.Value.NodeType, curPos);
                            currentActivity = NodeActivity.Idle;
                            parentWindow.Repaint();
                        } else {
                            currentNode = keyvalue.Value;
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }

            if (GUILayout.Button("Preferences")) {
                NodeEditorWindow.OpenPreferences();
                currentActivity = NodeActivity.Idle;
                parentWindow.Repaint();
            }
            GUI.DragWindow();
        }

        public static (string name, string[] tags) GetNodeMenuData(Type type) {
            //Check if type has the CreateNodeMenuAttribute
            XNode.CreateNodeMenuAttribute attrib;
            if (NodeEditorUtilities.GetAttrib(type, out attrib)) {// Return custom path
                return (attrib.menuName, attrib.Tags);
            }
            return (ObjectNames.NicifyVariableName(type.ToString().Replace('.', '/')), new string[0]);
        }
    }
}
