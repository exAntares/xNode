using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace XNodeEditor {
    public class TreeNode {
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
        private Vector2 scrollPosition;
        private const int Height = 40;
        private const int SpaceHeight = 20;
        private const int SpaceHeightSmall = 5;

        private static readonly Texture FolderTexture = EditorGUIUtility.Load("Folder Icon") as Texture2D;
        private static readonly Texture PlayTexture = EditorGUIUtility.Load("d_PlayButton") as Texture2D;
        private static readonly Texture CSScriptIcon = EditorGUIUtility.Load("cs Script Icon") as Texture2D;
        
        private static readonly GUIStyle ButtonStyle = new GUIStyle(GUI.skin.box) {
            alignment = TextAnchor.MiddleCenter,
            active = GUI.skin.button.active,
            hover = new GUIStyleState() { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black, background = new Texture2D(1, 1).SetPixelFluent(0, 0, new Color(140.0f/250.0f, 0.0f, 1.0f, 0.5f)) },
            normal = new GUIStyleState() { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black, background = Texture2D.blackTexture },
            fontSize = 25,
        };

        private static readonly GUIStyle BackButtonStyle = new GUIStyle(GUI.skin.button) {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 25,
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
            GUI.SetNextControlName("search_field");
            searchText = GUILayout.TextField(searchText, ToolbarSeachTextField);
            GUI.FocusControl("search_field");
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
                        var tags = x.tags.Select(t => t.ToLower());
                        var matchedWords = words.Where(w => tags.Any(tag => tag.Contains(w)));
                        return matchedWords.Count() == words.Length;
                    })
                    .OrderBy(x => x.name)
                    .ToArray();

                GUILayout.Space(SpaceHeight);
                foreach (var availableNodeType in typeNames) {
                    var controlRect = EditorGUILayout.GetControlRect(GUILayout.Height(Height));
                    if(DrawNodeButton(controlRect, Path.GetFileName(availableNodeType.name), true)) {
                        CreateNode(RequestedPos, availableNodeType.type);
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
                    GUILayout.Space(SpaceHeight);
                    if (GUI.Button(EditorGUILayout.GetControlRect(GUILayout.Height(50)), "< Back", BackButtonStyle)) {
                        scrollPosition = Vector2.zero;
                        currentNode = currentNode.Parent;
                    }
                }

                GUILayout.Space(SpaceHeight);
                scrollPosition = GUILayout.BeginScrollView(scrollPosition);
                var children = currentNode.Children.OrderBy(x => x.Key);
                foreach (var keyvalue in children) {
                    var controlRect = EditorGUILayout.GetControlRect(GUILayout.Height(Height));
                    if(DrawNodeButton(controlRect, keyvalue.Key, keyvalue.Value.Children.Count <= 0)) {
                        if (keyvalue.Value.Children.Count <= 0) {
                            CreateNode(RequestedPos, keyvalue.Value.NodeType);
                        } else {
                            scrollPosition = Vector2.zero;
                            currentNode = keyvalue.Value;
                        }
                    }

                }
                GUILayout.EndScrollView();
                GUILayout.Space(SpaceHeight);
            }
        }

        public static bool DrawNodeButton(Rect controlRect, string text, bool isLeaf) {
            var result = false;
            GUI.Box(controlRect, string.Empty);
            var textureRect = new Rect(controlRect);
            textureRect.width = Height;

            if (!isLeaf) {
                GUI.DrawTexture(textureRect, FolderTexture);
                textureRect.x = controlRect.width - textureRect.width;
                GUI.DrawTexture(textureRect, PlayTexture);
            } else {
                text = ObjectNames.NicifyVariableName(text);
                GUI.DrawTexture(textureRect, CSScriptIcon);
            }

            result = GUI.Button(controlRect, text, ButtonStyle);
            GUILayout.Space(SpaceHeightSmall);
            return result;
        }

        public void CreateNode(Vector2 position, Type nodeType) {
            Vector2 curPos = ParentWindow.WindowToGridPosition(position);
            ParentWindow.graphEditor.CreateNode(nodeType, curPos);
            ParentWindow.Repaint();
            editorWindow.Close();
        }

        public static (Type type, string name, string[] tags) GetNodeMenuData(Type sourcetype) {
            //Check if type has the CreateNodeMenuAttribute
            XNode.CreateNodeMenuAttribute attrib;
            var nicifyVariableName = ObjectNames.NicifyVariableName(sourcetype.ToString().Replace('.', '/'));
            if (NodeEditorUtilities.GetAttrib(sourcetype, out attrib)) {// Return custom path
                var tags = nicifyVariableName.Split(' ').Union(attrib.Tags).Distinct().ToArray();
                return (sourcetype, attrib.menuName, tags);
            }
            
            return (sourcetype, nicifyVariableName, nicifyVariableName.Split(' '));
        }
    }
}
