using System;
using UnityEditor;
using UnityEngine;
using XNode;

namespace XNodeEditor {
    /// <summary> Base class to derive custom Node Graph editors from. Use this to override how graphs are drawn in the editor. </summary>
    [CustomEditorAttribute(typeof(XNode.INodeGraph))]
    public class NodeGraphEditor : XNodeEditor.Internal.NodeEditorBase<NodeGraphEditor, XNode.INodeGraph>, ICustomEditor<XNode.INodeGraph> {
        /// <summary> The position of the window in screen space. </summary>
        public Rect position;
        /// <summary> Are we currently renaming a node? </summary>
        protected bool isRenaming;

        public INodeGraph Target { get; set; }
        public SerializedObject SerializedObject { get; set; }

        public UnityEngine.Object target { get { return Target as UnityEngine.Object; } }

        public virtual void OnGUI() { }

        public virtual Texture2D GetGridTexture() {
            return NodeEditorPreferences.GetSettings().gridTexture;
        }

        public virtual Texture2D GetSecondaryGridTexture() {
            return NodeEditorPreferences.GetSettings().crossTexture;
        }

        /// <summary> Return default settings for this graph type. This is the settings the user will load if no previous settings have been saved. </summary>
        public virtual NodeEditorPreferences.Settings GetDefaultPreferences() {
            return new NodeEditorPreferences.Settings();
        }

        /// <summary> Returns context node menu path. Null or empty strings for hidden nodes. </summary>
        public virtual string GetNodeMenuName(Type type) {
            //Check if type has the CreateNodeMenuAttribute
            XNode.CreateNodeMenuAttribute attrib;
            if (NodeEditorUtilities.GetAttrib(type, out attrib)) // Return custom path
                return attrib.menuName;
            else // Return generated path
                return ObjectNames.NicifyVariableName(type.ToString().Replace('.', '/'));
        }

        /// <summary> Add items for the context menu when right-clicking this node. Override to add custom menu items. </summary>
        public virtual void AddContextMenuItems(GenericMenu menu) {
            Vector2 pos = NodeEditorWindow.current.WindowToGridPosition(Event.current.mousePosition);
            for (int i = 0; i < NodeEditorWindow.nodeTypes.Length; i++) {
                Type type = NodeEditorWindow.nodeTypes[i];

                //Get node context menu path
                string path = GetNodeMenuName(type);
                if (string.IsNullOrEmpty(path)) continue;

                menu.AddItem(new GUIContent(path), false, () => {
                    CreateNode(type, pos);
                });
            }
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Preferences"), false, () => NodeEditorWindow.OpenPreferences());
            NodeEditorWindow.AddCustomContextMenuItems(menu, target);
        }

        public virtual Color GetTypeColor(Type type) {
            return NodeEditorPreferences.GetTypeColor(type);
        }

        /// <summary> Create a node and save it in the graph asset </summary>
        public virtual INode CreateNode(Type type, Vector2 position) {
            INode node = Target.AddNode(type);
            node.Position = position;
            node.Name = UnityEditor.ObjectNames.NicifyVariableName(type.Name);
            if (NodeEditorPreferences.GetSettings().autoSave) {
                AssetDatabase.SaveAssets();
            }
            NodeEditorWindow.RepaintAll();
            return node;
        }

        /// <summary> Creates a copy of the original node in the graph </summary>
        public XNode.INode CopyNode(XNode.INode original) {
            var node = Target.CopyNode(original);
            node.Name = original.Name;
            if (NodeEditorPreferences.GetSettings().autoSave) AssetDatabase.SaveAssets();
            return node;
        }

        /// <summary> Safely remove a node and all its connections. </summary>
        public void RemoveNode(XNode.INode node) {
            UnityEngine.Object.DestroyImmediate(node as UnityEngine.Object, true);
            Target.RemoveNode(node);
            if (NodeEditorPreferences.GetSettings().autoSave) AssetDatabase.SaveAssets();
        }
    }
}