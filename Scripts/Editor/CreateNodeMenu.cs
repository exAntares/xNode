using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace XNodeEditor {
    internal class TreeNode {
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

    public class CreateNodeMenu : PopupWindowContent {
        public Type[] AvailableTypes;
        public NodeEditorWindow ParentWindow;
        public Vector2 RequestedPos { get; internal set; }

        private static TreeNode root;
        private static TreeNode currentNode;
        private string _searchText;

        private static GUIStyle ButtonStyle = new GUIStyle(GUI.skin.button) {
            alignment = TextAnchor.MiddleRight,
            onHover = new GUIStyleState() { textColor = Color.black },
            //normal = new GUIStyleState() {  textColor = Color.blue, background = EditorGUIUtility.Load("Folder Icon") as Texture2D }
        };
        private static GUIStyle BackButtonStyle = new GUIStyle(GUI.skin.button) {
            alignment = TextAnchor.MiddleLeft,
        };

        public override Vector2 GetWindowSize() => new Vector2(600, 500);

        public override void OnGUI(Rect rect) => DrawWindow(ref _searchText, AvailableTypes, ParentWindow);

        public void DrawWindow(ref string searchText, Type[] nodeTypes, NodeEditorWindow parentWindow) {
            searchText = GUILayout.TextField(searchText, GUI.skin.FindStyle("ToolbarSeachTextField"), GUILayout.Height(50));
            var words = new string[0];
            bool IsSearching = !string.IsNullOrEmpty(searchText);
            if (IsSearching) {
                words = searchText.Split(' ')
                    .Distinct()
                    .Where(s => !string.IsNullOrEmpty(s) && s != " ")
                    .Select(x => x.ToLower())
                    .ToArray();

                var typeNames = nodeTypes.Select(GetNodeMenuData)
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
                        CreateNode(RequestedPos, parentWindow, availableNodeType.type);
                    }
                }
            } else {
                var typeNames = nodeTypes.Select(GetNodeMenuData);
                if (root == null) {
                    foreach (var nodeType in typeNames) {
                        var paths = nodeType.name.Split('/');
                        if (root == null) {
                            root = new TreeNode(paths, nodeType.type, null);
                        } else {
                            root.AddPaths(paths, nodeType.type);
                        }
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
                    if (GUI.Button(EditorGUILayout.GetControlRect(GUILayout.Height(50)), $"{keyvalue.Key}{(isLeafNode ? string.Empty : " >")}", ButtonStyle)) {
                        if (keyvalue.Value.Children.Count <= 0) {
                            CreateNode(RequestedPos, parentWindow, keyvalue.Value.NodeType);
                        } else {
                            currentNode = keyvalue.Value;
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }

            if (GUILayout.Button("Preferences")) {
                NodeEditorWindow.OpenPreferences();
                parentWindow.Repaint();
            }
        }

        public void CreateNode(Vector2 position, NodeEditorWindow parentWindow, Type nodeType) {
            Vector2 curPos = parentWindow.WindowToGridPosition(position);
            parentWindow.graphEditor.CreateNode(nodeType, curPos);
            parentWindow.Repaint();
            editorWindow.Close();
        }

        public static (Type type, string name, string[] tags) GetNodeMenuData(Type sourcetype) {
            //Check if type has the CreateNodeMenuAttribute
            XNode.CreateNodeMenuAttribute attrib;
            if (NodeEditorUtilities.GetAttrib(sourcetype, out attrib)) {// Return custom path
                return (sourcetype, attrib.menuName, attrib.Tags);
            }
            return (sourcetype, ObjectNames.NicifyVariableName(sourcetype.ToString().Replace('.', '/')), new string[0]);
        }
    }
}
