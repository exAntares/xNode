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

    public static class Texture2DExt {
        public static Texture2D SetPixelFluent(this Texture2D tex, int x, int y, Color color) {
            tex.SetPixel(x, y, color);
            tex.Apply();
            return tex;
        }
    }

    public class CreateNodeMenu : PopupWindowContent {
        public Type[] AvailableTypes;
        public NodeEditorWindow ParentWindow;
        public Vector2 RequestedPos { get; internal set; }

        private static TreeNode root;
        private static TreeNode currentNode;
        private string _searchText;

        private const int Height = 40;
        private const int SpaceHeight = 20;
        private const int SpaceHeightSmall = 5;

        private static readonly Texture FolderTexture = EditorGUIUtility.Load("Folder Icon") as Texture2D;
        private static readonly Texture PlayTexture = EditorGUIUtility.Load("d_PlayButton") as Texture2D;

        private static readonly GUIStyle ButtonStyle = new GUIStyle(GUI.skin.box) {
            alignment = TextAnchor.MiddleCenter,
            active = GUI.skin.button.active,
            hover = new GUIStyleState() { textColor = Color.white, background = new Texture2D(1, 1).SetPixelFluent(0, 0, new Color(140.0f/250.0f, 0.0f, 1.0f, 0.5f)) },
            normal = new GUIStyleState() { textColor = Color.white, background = Texture2D.blackTexture }
        };

        private static readonly GUIStyle BackButtonStyle = new GUIStyle(GUI.skin.button) {
            alignment = TextAnchor.MiddleLeft,
        };

        private static readonly GUIStyle ToolbarSeachTextField = new GUIStyle(GUI.skin.FindStyle("ToolbarSeachTextField")) {
        };

        public override Vector2 GetWindowSize() => new Vector2(600, 500);

        public override void OnGUI(Rect rect) => DrawWindow(ref _searchText, AvailableTypes, ParentWindow);

        public void DrawWindow(ref string searchText, Type[] nodeTypes, NodeEditorWindow parentWindow) {
            if (Event.current.type == EventType.MouseMove) {
                editorWindow.Repaint();
            }
            
            GUILayout.Space(SpaceHeight);
            searchText = GUILayout.TextField(searchText, ToolbarSeachTextField);
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

                GUILayout.Space(SpaceHeight);
                foreach (var availableNodeType in typeNames) {
                    var controlRect = EditorGUILayout.GetControlRect(GUILayout.Height(Height));
                    GUI.Box(controlRect, string.Empty);
                    if (GUI.Button(controlRect, Path.GetFileName(availableNodeType.name), ButtonStyle)) {
                        CreateNode(RequestedPos, parentWindow, availableNodeType.type);
                    }
                    GUILayout.Space(SpaceHeightSmall);
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
                    GUILayout.Space(SpaceHeight);
                    if (GUI.Button(EditorGUILayout.GetControlRect(GUILayout.Height(50)), "< Back", BackButtonStyle)) {
                        currentNode = currentNode.Parent;
                    }
                }

                GUILayout.Space(SpaceHeight);
                foreach (var keyvalue in currentNode.Children) {
                    var isLeafNode = keyvalue.Value.Children.Count <= 0;
                    var controlRect = EditorGUILayout.GetControlRect(GUILayout.Height(Height));
                    var labelRect = new Rect(controlRect);
                    GUI.Box(controlRect, string.Empty);
                    if (!isLeafNode) {
                        labelRect.xMin += Height;
                        var textureRect = new Rect(controlRect);
                        textureRect.width = Height;
                        GUI.DrawTexture(textureRect, FolderTexture);
                        textureRect.x = controlRect.width - textureRect.width;
                        GUI.DrawTexture(textureRect, PlayTexture);
                    }

                    if (GUI.Button(controlRect, keyvalue.Key, ButtonStyle)) {
                        if (keyvalue.Value.Children.Count <= 0) {
                            CreateNode(RequestedPos, parentWindow, keyvalue.Value.NodeType);
                        } else {
                            currentNode = keyvalue.Value;
                        }
                    }
                    GUILayout.Space(SpaceHeightSmall);
                }
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
