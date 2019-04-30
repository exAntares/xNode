using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XNode;

namespace XNodeEditor {
    /// <summary> Base class to derive custom Node editors from. Use this to create your own custom inspectors and editors for your nodes. </summary>
    [CustomEditorAttribute(typeof(XNode.INode))]
    public class NodeEditor : XNodeEditor.Internal.NodeEditorBase<NodeEditor, INode>, ICustomEditor<INode> {
        /// <summary> Fires every whenever a node was modified through the editor </summary>
        public static Action<XNode.INode> onUpdateNode;
        public static Dictionary<XNode.NodePort, Vector2> portPositions;
        public int renaming;

        public INode Target { get; set; }
        public SerializedObject SerializedObject { get; set; }
        public UnityEngine.Object target { get { return Target as UnityEngine.Object; } }

        public virtual void OnHeaderGUI() {
            string title = Target.Name;
            if (renaming != 0) { 
                if (Selection.Contains(target)) {
                    int controlID = EditorGUIUtility.GetControlID(FocusType.Keyboard) + 1;
                    if (renaming == 1) {
                        EditorGUIUtility.keyboardControl = controlID;
                        EditorGUIUtility.editingTextField = true;
                        renaming = 2;
                    }
                    Target.Name = EditorGUILayout.TextField(Target.Name, NodeEditorResources.styles.nodeHeader, GUILayout.Height(30));
                    if (!EditorGUIUtility.editingTextField) {
                        Debug.Log("Finish renaming");
                        Rename(target.name);
                        renaming = 0;
                    }
                }
                else {
                    // Selection changed, so stop renaming.
                    GUILayout.Label(title, NodeEditorResources.styles.nodeHeader, GUILayout.Height(30));
                    Rename(Target.Name);
                    renaming = 0;
                }
            } else {
                GUILayout.Label(title, NodeEditorResources.styles.nodeHeader, GUILayout.Height(30));
            }
        }

        /// <summary> Draws standard field editors for all public fields </summary>
        public virtual void OnBodyGUI() {
            // Unity specifically requires this to save/update any serial object.
            // serializedObject.Update(); must go at the start of an inspector gui, and
            // serializedObject.ApplyModifiedProperties(); goes at the end.
            SerializedObject.Update();
            string[] excludes = { "m_Script", "graph", "position", "ports" };
            portPositions = new Dictionary<XNode.NodePort, Vector2>();

            SerializedProperty iterator = SerializedObject.GetIterator();
            bool enterChildren = true;
            EditorGUIUtility.labelWidth = 84;
            while (iterator.NextVisible(enterChildren)) {
                enterChildren = false;
                if (excludes.Contains(iterator.name)) continue;
                NodeEditorGUILayout.PropertyField(iterator, true);
            }
            SerializedObject.ApplyModifiedProperties();
        }

        public virtual int GetWidth() {
            Type type = target.GetType();
            int width;
            if (NodeEditorWindow.nodeWidth.TryGetValue(type, out width)) return width;
            else return 208;
        }

        public virtual Color GetTint() {
            Type type = target.GetType();
            Color color;
            if (NodeEditorWindow.nodeTint.TryGetValue(type, out color)) return color;
            else return Color.white;
        }

        public virtual GUIStyle GetBodyStyle() {
            return NodeEditorResources.styles.nodeBody;
        }

        /// <summary> Add items for the context menu when right-clicking this node. Override to add custom menu items. </summary>
        public virtual void AddContextMenuItems(GenericMenu menu) {
            // Actions if only one node is selected
            if (Selection.objects.Length == 1 && Selection.activeObject is XNode.INode) {
                XNode.INode node = Selection.activeObject as XNode.INode;
                menu.AddItem(new GUIContent("Move To Top"), false, () => NodeEditorWindow.current.MoveNodeToTop(node));
                menu.AddItem(new GUIContent("Rename"), false, NodeEditorWindow.current.RenameSelectedNode);
            }

            // Add actions to any number of selected nodes
            menu.AddItem(new GUIContent("Duplicate"), false, NodeEditorWindow.current.DuplicateSelectedNodes);
            menu.AddItem(new GUIContent("Remove"), false, NodeEditorWindow.current.RemoveSelectedNodes);

            // Custom sctions if only one node is selected
            if (Selection.objects.Length == 1 && Selection.activeObject is XNode.INode) {
                XNode.INode node = Selection.activeObject as XNode.INode;
                NodeEditorWindow.AddCustomContextMenuItems(menu, node);
            }
        }

        public void InitiateRename() {
            renaming = 1;
        }

        public void Rename(string newName) {
            Target.Name = newName;
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(target));
        }
    }
}
