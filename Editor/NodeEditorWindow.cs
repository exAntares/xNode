using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace XNodeEditor {
    [InitializeOnLoad]
    public partial class NodeEditorWindow : EditorWindow {
        public static NodeEditorWindow current;

        /// <summary> Stores node positions for all nodePorts. </summary>
        public Dictionary<XNode.NodePort, Rect> portConnectionPoints { get { return _portConnectionPoints; } }
        private Dictionary<XNode.NodePort, Rect> _portConnectionPoints = new Dictionary<XNode.NodePort, Rect>();
        [SerializeField] private NodePortReference[] _references = new NodePortReference[0];
        [SerializeField] private Rect[] _rects = new Rect[0];

        [System.Serializable] private class NodePortReference {
            [SerializeField] private UnityEngine.Object _node;
            [SerializeField] private string _name;

            public NodePortReference(XNode.NodePort nodePort) {
                _node = nodePort.node as UnityEngine.Object;
                _name = nodePort.fieldName;
            }

            public XNode.NodePort GetNodePort() {
                if (_node == null) {
                    return null;
                }
                return (_node as XNode.INode).GetPort(_name);
            }
        }

        private void OnDisable() {
            EditorApplication.playModeStateChanged -= LogPlayModeState;

            // Cache portConnectionPoints before serialization starts
            int count = portConnectionPoints.Count;
            _references = new NodePortReference[count];
            _rects = new Rect[count];
            int index = 0;
            foreach (var portConnectionPoint in portConnectionPoints) {
                _references[index] = new NodePortReference(portConnectionPoint.Key);
                _rects[index] = portConnectionPoint.Value;
                index++;
            }
        }

        private void OnEnable() {
            EditorApplication.playModeStateChanged += LogPlayModeState;

            // Reload portConnectionPoints if there are any
            int length = _references.Length;
            if (length == _rects.Length) {
                for (int i = 0; i < length; i++) {
                    XNode.NodePort nodePort = _references[i].GetNodePort();
                    if (nodePort != null)
                        _portConnectionPoints[nodePort] = _rects[i];
                }
            }
        }

        public Dictionary<XNode.INode, Vector2> nodeSizes { get { return _nodeSizes; } }
        private Dictionary<XNode.INode, Vector2> _nodeSizes = new Dictionary<XNode.INode, Vector2>();
        public UnityEngine.Object graph;
        public Vector2 panOffset { get { return _panOffset; } set { _panOffset = value; Repaint(); } }
        private Vector2 _panOffset;
        public float zoom { get { return _zoom; } set { _zoom = Mathf.Clamp(value, 1f, 5f); Repaint(); } }
        private float _zoom = 1;

        void OnFocus() {
            current = this;
            graphEditor = NodeGraphEditor.GetEditor(graph as XNode.INodeGraph);
            if (graphEditor != null && NodeEditorPreferences.GetSettings().autoSave) AssetDatabase.SaveAssets();
            OpenSelectedObject();
        }

        /// <summary> Create editor window </summary>
        public static NodeEditorWindow Init() {
            NodeEditorWindow w = CreateInstance<NodeEditorWindow>();
            w.titleContent = new GUIContent("xNode");
            w.wantsMouseMove = true;
            w.Show();
            return w;
        }

        public void Save() {
            if (AssetDatabase.Contains(graph as UnityEngine.Object)) {
                EditorUtility.SetDirty(graph as UnityEngine.Object);
                if (NodeEditorPreferences.GetSettings().autoSave) AssetDatabase.SaveAssets();
            } else SaveAs();
        }

        public void SaveAs() {
            string path = EditorUtility.SaveFilePanelInProject("Save NodeGraph", "NewNodeGraph", "asset", "");
            if (string.IsNullOrEmpty(path)) return;
            else {
                XNode.INodeGraph existingGraph = AssetDatabase.LoadAssetAtPath<Object>(path) as XNode.INodeGraph;
                if (existingGraph != null) AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(graph as UnityEngine.Object, path);
                EditorUtility.SetDirty(graph as UnityEngine.Object);
                if (NodeEditorPreferences.GetSettings().autoSave) AssetDatabase.SaveAssets();
            }
        }

        private void DraggableWindow(int windowID) {
            GUI.DragWindow();
        }

        public Vector2 WindowToGridPosition(Vector2 windowPosition) {
            return (windowPosition - (position.size * 0.5f) - (panOffset / zoom)) * zoom;
        }

        public Vector2 GridToWindowPosition(Vector2 gridPosition) {
            return (position.size * 0.5f) + (panOffset / zoom) + (gridPosition / zoom);
        }

        public Rect GridToWindowRectNoClipped(Rect gridRect) {
            gridRect.position = GridToWindowPositionNoClipped(gridRect.position);
            return gridRect;
        }

        public Rect GridToWindowRect(Rect gridRect) {
            gridRect.position = GridToWindowPosition(gridRect.position);
            gridRect.size /= zoom;
            return gridRect;
        }

        public Vector2 GridToWindowPositionNoClipped(Vector2 gridPosition) {
            Vector2 center = position.size * 0.5f;
            float xOffset = (center.x * zoom + (panOffset.x + gridPosition.x));
            float yOffset = (center.y * zoom + (panOffset.y + gridPosition.y));
            return new Vector2(xOffset, yOffset);
        }

        public void SelectNode(XNode.INode node, bool add) {
            if (add) {
                List<Object> selection = new List<Object>(Selection.objects);
                selection.Add(node as UnityEngine.Object);
                Selection.objects = selection.ToArray();
            } else Selection.objects = new Object[] { node as UnityEngine.Object };
        }

        public void DeselectNode(XNode.INode node) {
            List<Object> selection = new List<Object>(Selection.objects);
            selection.Remove(node as UnityEngine.Object);
            Selection.objects = selection.ToArray();
        }

        [OnOpenAsset(0)]
        public static bool OnOpen(int instanceID, int line) {
            XNode.INodeGraph nodeGraph = EditorUtility.InstanceIDToObject(instanceID) as XNode.INodeGraph;
            if (nodeGraph != null) {
                OpenWithGraph(nodeGraph);
                return true;
            }
            return false;
        }

        [MenuItem("Window/XNode/OpenGraph with Selected")]
        public static void OpenSelectedObject() {
            if(Selection.activeGameObject != null) {
                var graph = Selection.activeGameObject.GetComponent<XNode.INodeGraph>();
                if(graph == null) {
                    graph = Selection.activeGameObject.GetComponentInParent<XNode.INodeGraph>();
                }
                OpenWithGraph(graph);
            } else {
                OpenWithGraph(Selection.activeObject as XNode.INodeGraph);
            }
        }

        public static void OpenWithGraph(XNode.INodeGraph nodeGraph) {
            if (nodeGraph != null) {
                NodeEditorWindow w = GetWindow(typeof(NodeEditorWindow), false, "xNode", false) as NodeEditorWindow;
                w.wantsMouseMove = true;
                w.graph = nodeGraph as UnityEngine.Object;
                w.Repaint();
            }
        }

        private void OnSelectionChange() {
            OpenSelectedObject();
        }

        /// <summary> Repaint all open NodeEditorWindows. </summary>
        public static void RepaintAll() {
            NodeEditorWindow[] windows = Resources.FindObjectsOfTypeAll<NodeEditorWindow>();
            for (int i = 0; i < windows.Length; i++) {
                windows[i].Repaint();
            }
        }

        private static void LogPlayModeState(PlayModeStateChange state) {
            if(state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode) {
                OpenSelectedObject();
            }
        }
    }
}
