using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace XNodeEditor {
    /// <summary> xNode-specific version of <see cref="EditorGUILayout"/> </summary>
    public static class NodeEditorGUILayout {

        /// <summary> Listen for this event if you want to alter the default ReorderableList </summary>
        public static Action<ReorderableList> onCreateReorderableList;
        private static readonly Dictionary<UnityEngine.Object, Dictionary<string, ReorderableList>> reorderableListCache = new Dictionary<UnityEngine.Object, Dictionary<string, ReorderableList>>();
        private static int reorderableListIndex = -1;

        /// <summary> Make a field for a serialized property. Automatically displays relevant node port. </summary>
        public static void PropertyField(SerializedProperty property, bool includeChildren = true, params GUILayoutOption[] options) {
            PropertyField(property, (GUIContent) null, includeChildren, options);
        }

        /// <summary> Make a field for a serialized property. Automatically displays relevant node port. </summary>
        public static void PropertyField(SerializedProperty property, GUIContent label, bool includeChildren = true, params GUILayoutOption[] options) {
            if (property == null) throw new NullReferenceException();
            XNode.INode node = property.serializedObject.targetObject as XNode.INode;
            XNode.NodePort port = node.GetPort(property.name);
            PropertyField(property, label, port, includeChildren);
        }

        /// <summary> Make a field for a serialized property. Manual node port override. </summary>
        public static void PropertyField(SerializedProperty property, XNode.NodePort port, bool includeChildren = true, params GUILayoutOption[] options) {
            PropertyField(property, null, port, includeChildren, options);
        }

        /// <summary> Make a field for a serialized property. Manual node port override. </summary>
        public static void PropertyField(SerializedProperty property, GUIContent label, XNode.NodePort port, bool includeChildren = true, params GUILayoutOption[] options) {
            if (property == null) throw new NullReferenceException();

            // If property is not a port, display a regular property field
            if (port == null) {
                EditorGUILayout.PropertyField(property, label, includeChildren, GUILayout.MinWidth(30));
                return;
            }

            DrawNodePropertyField(property, label, port, includeChildren);
        }

        private static void DrawNodePropertyField(SerializedProperty property, GUIContent label, XNode.NodePort port, bool includeChildren) {
            float spacePadding = 0;
            if (NodeEditorUtilities.GetCachedAttrib(port.node.GetType(), property.name, out SpaceAttribute spaceAttribute)) {
                spacePadding = spaceAttribute.height;
            }

            XNode.ShowBackingValue showBacking = XNode.ShowBackingValue.Unconnected;
            XNode.ConnectionType connectionType = XNode.ConnectionType.Multiple;
            bool instancePortList = false;
            Vector2 handleOffset = Vector2.zero;
            Texture2D handleTexture = NodeEditorResources.dot;
            bool shouldTint = true;

            if (port.direction == XNode.NodePort.IO.Input) {
                // Get data from [Input] attribute
                if (NodeEditorUtilities.GetCachedAttrib(port.node.GetType(), property.name, out XNode.InputAttribute inputAttribute)) {
                    instancePortList = inputAttribute.instancePortList;
                    showBacking = inputAttribute.backingValue;
                    connectionType = inputAttribute.connectionType;
                    handleTexture = EditorGUIUtility.Load(inputAttribute.EditorIconName) as Texture2D;
                    shouldTint = inputAttribute.ShouldTint;
                }
            } else if (port.direction == XNode.NodePort.IO.Output) {
                // Get data from [Output] attribute
                if (NodeEditorUtilities.GetCachedAttrib(port.node.GetType(), property.name, out XNode.OutputAttribute outputAttribute)) {
                    instancePortList = outputAttribute.instancePortList;
                    showBacking = outputAttribute.backingValue;
                    connectionType = outputAttribute.connectionType;
                    handleTexture = EditorGUIUtility.Load(outputAttribute.EditorIconName) as Texture2D;
                    shouldTint = outputAttribute.ShouldTint;
                }
            }

            spacePadding = DrawSpaceIfNeeded(port, spacePadding, showBacking, instancePortList);

            if (instancePortList) {
                Type type = GetType(property);
                InstancePortList(property.name, type, property.serializedObject, port.direction, connectionType);
                return;
            }

            // If property is an input, display a regular property field and put a port handle on the left side
            if (port.direction == XNode.NodePort.IO.Input) {
                handleOffset = DrawInputPropertyField(property, label, port, includeChildren, spacePadding, showBacking);
                // If property is an output, display a text label and put a port handle on the right side
            } else if (port.direction == XNode.NodePort.IO.Output) {
                handleOffset = DrawOutputPropertyField(property, label, port, includeChildren, spacePadding, showBacking);
            }

            DrawPortHandle(port, handleOffset, handleTexture, shouldTint);
        }

        private static Vector2 DrawOutputPropertyField(SerializedProperty property, GUIContent label, XNode.NodePort port, bool includeChildren, float spacePadding, XNode.ShowBackingValue showBacking) {
            switch (showBacking) {
                case XNode.ShowBackingValue.Unconnected:
                    // Display a label if port is connected
                    if (port.IsConnected) EditorGUILayout.LabelField(label != null ? label : new GUIContent(property.displayName), NodeEditorResources.OutputPort, GUILayout.MinWidth(30));
                    // Display an editable property field if port is not connected
                    else EditorGUILayout.PropertyField(property, label, includeChildren, GUILayout.MinWidth(30));
                    break;
                case XNode.ShowBackingValue.Never:
                    // Display a label
                    EditorGUILayout.LabelField(label != null ? label : new GUIContent(property.displayName), NodeEditorResources.OutputPort, GUILayout.MinWidth(30));
                    break;
                case XNode.ShowBackingValue.Always:
                    // Display an editable property field
                    EditorGUILayout.PropertyField(property, label, includeChildren, GUILayout.MinWidth(30));
                    break;
            }

            return new Vector2(GUILayoutUtility.GetLastRect().width, spacePadding+2);
        }

        private static Vector2 DrawInputPropertyField(SerializedProperty property, GUIContent label, XNode.NodePort port, bool includeChildren, float spacePadding, XNode.ShowBackingValue showBacking) {
            switch (showBacking) {
                case XNode.ShowBackingValue.Unconnected:
                    // Display a label if port is connected
                    if (port.IsConnected) EditorGUILayout.LabelField(label != null ? label : new GUIContent(property.displayName));
                    // Display an editable property field if port is not connected
                    else EditorGUILayout.PropertyField(property, label, includeChildren, GUILayout.MinWidth(30));
                    break;
                case XNode.ShowBackingValue.Never:
                    // Display a label
                    EditorGUILayout.LabelField(label != null ? label : new GUIContent(property.displayName));
                    break;
                case XNode.ShowBackingValue.Always:
                    // Display an editable property field
                    EditorGUILayout.PropertyField(property, label, includeChildren, GUILayout.MinWidth(30));
                    break;
            }

            return new Vector2(16, -spacePadding) * -1;
        }

        private static float DrawSpaceIfNeeded(XNode.NodePort port, float spacePadding, XNode.ShowBackingValue showBacking, bool instancePortList) {
            //Call GUILayout.Space if Space attribute is set and we are NOT drawing a PropertyField
            bool useLayoutSpace = instancePortList ||
                showBacking == XNode.ShowBackingValue.Never ||
                (showBacking == XNode.ShowBackingValue.Unconnected && port.IsConnected);
            if (spacePadding > 0 && useLayoutSpace) {
                GUILayout.Space(spacePadding);
                spacePadding = 0;
            }

            return spacePadding;
        }

        private static void DrawPortHandle(XNode.NodePort port, Vector2 handleOffset, Texture2D handleTexture, bool shouldTint) {
            Rect handleRect = GUILayoutUtility.GetLastRect();
            handleRect.position = handleRect.position + handleOffset;
            handleRect.size = new Vector2(16, 16);

            Color col = NodeEditorWindow.current.graphEditor.GetTypeColor(port.ValueType);
            DrawPortHandle(handleRect, col, handleTexture, shouldTint);

            // Register the handle position
            Vector2 portPos = handleRect.center;
            if (NodeEditor.portPositions.ContainsKey(port)) {
                NodeEditor.portPositions[port] = portPos;
            } else {
                NodeEditor.portPositions.Add(port, portPos);
            }
        }

        private static System.Type GetType(SerializedProperty property) {
            System.Type parentType = property.serializedObject.targetObject.GetType();
            System.Reflection.FieldInfo fi = parentType.GetField(property.propertyPath);
            return fi.FieldType;
        }

        /// <summary> Make a simple port field. </summary>
        public static void PortField(XNode.NodePort port, params GUILayoutOption[] options) {
            PortField(null, port, options);
        }

        /// <summary> Make a simple port field. </summary>
        public static void PortField(GUIContent label, XNode.NodePort port, params GUILayoutOption[] options) {
            if (port == null) return;
            if (options == null) options = new GUILayoutOption[] { GUILayout.MinWidth(30) };
            Vector2 position = Vector3.zero;
            GUIContent content = label != null ? label : new GUIContent(ObjectNames.NicifyVariableName(port.fieldName));

            // If property is an input, display a regular property field and put a port handle on the left side
            if (port.direction == XNode.NodePort.IO.Input) {
                // Display a label
                EditorGUILayout.LabelField(content, options);

                Rect rect = GUILayoutUtility.GetLastRect();
                position = rect.position - new Vector2(16, 0);

            }
            // If property is an output, display a text label and put a port handle on the right side
            else if (port.direction == XNode.NodePort.IO.Output) {
                // Display a label
                EditorGUILayout.LabelField(content, NodeEditorResources.OutputPort, options);

                Rect rect = GUILayoutUtility.GetLastRect();
                position = rect.position + new Vector2(rect.width, 0);
            }
            PortField(position, port);
        }

        /// <summary> Make a simple port field. </summary>
        public static void PortField(Vector2 position, XNode.NodePort port) {
            if (port == null) return;

            Rect rect = new Rect(position, new Vector2(16, 16));
            Color col = NodeEditorWindow.current.graphEditor.GetTypeColor(port.ValueType);
            DrawPortHandle(rect, col, NodeEditorResources.dot);

            // Register the handle position
            Vector2 portPos = rect.center;
            if (NodeEditor.portPositions.ContainsKey(port)) NodeEditor.portPositions[port] = portPos;
            else NodeEditor.portPositions.Add(port, portPos);
        }

        /// <summary> Add a port field to previous layout element. </summary>
        public static void AddPortField(XNode.NodePort port) {
            if (port == null) return;
            Rect rect = new Rect();

            // If property is an input, display a regular property field and put a port handle on the left side
            if (port.direction == XNode.NodePort.IO.Input) {
                rect = GUILayoutUtility.GetLastRect();
                rect.position = rect.position - new Vector2(16, 0);
                // If property is an output, display a text label and put a port handle on the right side
            } else if (port.direction == XNode.NodePort.IO.Output) {
                rect = GUILayoutUtility.GetLastRect();
                rect.position = rect.position + new Vector2(rect.width, 0);
            }

            rect.size = new Vector2(16, 16);
            Color col = NodeEditorWindow.current.graphEditor.GetTypeColor(port.ValueType);
            DrawPortHandle(rect, col, NodeEditorResources.dot);

            // Register the handle position
            Vector2 portPos = rect.center;
            if (NodeEditor.portPositions.ContainsKey(port)) NodeEditor.portPositions[port] = portPos;
            else NodeEditor.portPositions.Add(port, portPos);
        }

        /// <summary> Draws an input and an output port on the same line </summary>
        public static void PortPair(XNode.NodePort input, XNode.NodePort output) {
            GUILayout.BeginHorizontal();
            NodeEditorGUILayout.PortField(input, GUILayout.MinWidth(0));
            NodeEditorGUILayout.PortField(output, GUILayout.MinWidth(0));
            GUILayout.EndHorizontal();
        }

        public static void DrawPortHandle(Rect rect, Color typeColor, Texture2D handleTexture, bool shouldTint = true) {
            Color col = GUI.color;
            if (shouldTint) {
                GUI.color = typeColor;
            }
            
            GUI.DrawTexture(rect, handleTexture);
            GUI.color = col;
        }

        [Obsolete("Use InstancePortList(string, Type, SerializedObject, NodePort.IO, Node.ConnectionType) instead")]
        public static void InstancePortList(string fieldName, Type type, SerializedObject serializedObject, XNode.ConnectionType connectionType = XNode.ConnectionType.Multiple) {
            InstancePortList(fieldName, type, serializedObject, XNode.NodePort.IO.Output, connectionType);
        }

        /// <summary> Draw an editable list of instance ports. Port names are named as "[fieldName] [index]" </summary>
        /// <param name="fieldName">Supply a list for editable values</param>
        /// <param name="type">Value type of added instance ports</param>
        /// <param name="serializedObject">The serializedObject of the node</param>
        /// <param name="connectionType">Connection type of added instance ports</param>
        public static void InstancePortList(string fieldName, Type type, SerializedObject serializedObject, XNode.NodePort.IO io, XNode.ConnectionType connectionType = XNode.ConnectionType.Multiple) {
            var node = serializedObject.targetObject as XNode.INode;
            SerializedProperty arrayData = serializedObject.FindProperty(fieldName);

            Predicate<string> isMatchingInstancePort =
                x => {
                    string[] split = x.Split(' ');
                    if (split != null && split.Length == 2) return split[0] == fieldName;
                    else return false;
                };
            List<XNode.NodePort> instancePorts = node.InstancePorts.Where(x => isMatchingInstancePort(x.fieldName)).OrderBy(x => x.fieldName).ToList();

            ReorderableList list = null;
            Dictionary<string, ReorderableList> rlc;
            if (reorderableListCache.TryGetValue(serializedObject.targetObject, out rlc)) {
                if (!rlc.TryGetValue(fieldName, out list)) list = null;
            }
            // If a ReorderableList isn't cached for this array, do so.
            if (list == null) {
                string label = serializedObject.FindProperty(fieldName).displayName;
                list = CreateReorderableList(instancePorts, arrayData, type, serializedObject, io, label, connectionType);
                if (reorderableListCache.TryGetValue(serializedObject.targetObject, out rlc)) rlc.Add(fieldName, list);
                else reorderableListCache.Add(serializedObject.targetObject, new Dictionary<string, ReorderableList>() { { fieldName, list } });
            }
            list.list = instancePorts;
            list.DoLayoutList();
        }

        private static ReorderableList CreateReorderableList(List<XNode.NodePort> instancePorts, SerializedProperty arrayData, Type type, SerializedObject serializedObject, XNode.NodePort.IO io, string label, XNode.ConnectionType connectionType = XNode.ConnectionType.Multiple) {
            bool hasArrayData = arrayData != null && arrayData.isArray;
            int arraySize = hasArrayData ? arrayData.arraySize : 0;
            var node = serializedObject.targetObject as XNode.INode;
            ReorderableList list = new ReorderableList(instancePorts, null, true, true, true, true);

            list.drawElementCallback =
                (Rect rect, int index, bool isActive, bool isFocused) => {
                    XNode.NodePort port = node.GetPort(arrayData.name + " " + index);
                    if (hasArrayData) {
                        if (arrayData.arraySize <= index) {
                            EditorGUI.LabelField(rect, "Invalid element " + index);
                            return;
                        }
                        SerializedProperty itemData = arrayData.GetArrayElementAtIndex(index);
                        EditorGUI.PropertyField(rect, itemData);
                    } else EditorGUI.LabelField(rect, port.fieldName);
                    Vector2 pos = rect.position + (port.IsOutput?new Vector2(rect.width + 6, 0) : new Vector2(-36, 0));
                    NodeEditorGUILayout.PortField(pos, port);
                };
            list.elementHeightCallback =
                (int index) => {
                    if (hasArrayData) {
                        if (arrayData.arraySize <= index) return EditorGUIUtility.singleLineHeight;
                        SerializedProperty itemData = arrayData.GetArrayElementAtIndex(index);
                        return EditorGUI.GetPropertyHeight(itemData);
                    } else return EditorGUIUtility.singleLineHeight;
                };
            list.drawHeaderCallback =
                (Rect rect) => {
                    EditorGUI.LabelField(rect, label);
                };
            list.onSelectCallback =
                (ReorderableList rl) => {
                    reorderableListIndex = rl.index;
                };
            list.onReorderCallback =
                (ReorderableList rl) => {

                    // Move up
                    if (rl.index > reorderableListIndex) {
                        for (int i = reorderableListIndex; i < rl.index; ++i) {
                            XNode.NodePort port = node.GetPort(arrayData.name + " " + i);
                            XNode.NodePort nextPort = node.GetPort(arrayData.name + " " + (i + 1));
                            port.SwapConnections(nextPort);

                            // Swap cached positions to mitigate twitching
                            Rect rect = NodeEditorWindow.current.portConnectionPoints[port];
                            NodeEditorWindow.current.portConnectionPoints[port] = NodeEditorWindow.current.portConnectionPoints[nextPort];
                            NodeEditorWindow.current.portConnectionPoints[nextPort] = rect;
                        }
                    }
                    // Move down
                    else {
                        for (int i = reorderableListIndex; i > rl.index; --i) {
                            XNode.NodePort port = node.GetPort(arrayData.name + " " + i);
                            XNode.NodePort nextPort = node.GetPort(arrayData.name + " " + (i - 1));
                            port.SwapConnections(nextPort);

                            // Swap cached positions to mitigate twitching
                            Rect rect = NodeEditorWindow.current.portConnectionPoints[port];
                            NodeEditorWindow.current.portConnectionPoints[port] = NodeEditorWindow.current.portConnectionPoints[nextPort];
                            NodeEditorWindow.current.portConnectionPoints[nextPort] = rect;
                        }
                    }
                    // Apply changes
                    serializedObject.ApplyModifiedProperties();
                    serializedObject.Update();

                    // Move array data if there is any
                    if (hasArrayData) {
                        arrayData.MoveArrayElement(reorderableListIndex, rl.index);
                    }

                    // Apply changes
                    serializedObject.ApplyModifiedProperties();
                    serializedObject.Update();
                    NodeEditorWindow.current.Repaint();
                    EditorApplication.delayCall += NodeEditorWindow.current.Repaint;
                };
            list.onAddCallback =
                (ReorderableList rl) => {
                    // Add instance port postfixed with an index number
                    string newName = arrayData.name + " 0";
                    int i = 0;
                    while (node.HasPort(newName)) newName = arrayData.name + " " + (++i);

                    if (io == XNode.NodePort.IO.Output) node.AddInstanceOutput(type, connectionType, newName);
                    else node.AddInstanceInput(type, connectionType, newName);
                    serializedObject.Update();
                    EditorUtility.SetDirty(node as UnityEngine.Object);
                    if (hasArrayData) arrayData.InsertArrayElementAtIndex(arraySize);
                    serializedObject.ApplyModifiedProperties();
                };
            list.onRemoveCallback =
                (ReorderableList rl) => {
                    int index = rl.index;
                    // Clear the removed ports connections
                    instancePorts[index].ClearConnections();
                    // Move following connections one step up to replace the missing connection
                    for (int k = index + 1; k < instancePorts.Count(); k++) {
                        for (int j = 0; j < instancePorts[k].ConnectionCount; j++) {
                            XNode.NodePort other = instancePorts[k].GetConnection(j);
                            instancePorts[k].Disconnect(other);
                            instancePorts[k - 1].Connect(other);
                        }
                    }
                    // Remove the last instance port, to avoid messing up the indexing
                    node.RemoveInstancePort(instancePorts[instancePorts.Count() - 1].fieldName);
                    serializedObject.Update();
                    EditorUtility.SetDirty(node as UnityEngine.Object);
                    if (hasArrayData) {
                        arrayData.DeleteArrayElementAtIndex(index);
                        arraySize--;
                        // Error handling. If the following happens too often, file a bug report at https://github.com/Siccity/xNode/issues
                        if (instancePorts.Count <= arraySize) {
                            while (instancePorts.Count <= arraySize) {
                                arrayData.DeleteArrayElementAtIndex(--arraySize);
                            }
                            Debug.LogWarning("Array size exceeded instance ports size. Excess items removed.");
                        }
                        serializedObject.ApplyModifiedProperties();
                        serializedObject.Update();
                    }

                };

            if (hasArrayData) {
                int instancePortCount = instancePorts.Count;
                while (instancePortCount < arraySize) {
                    // Add instance port postfixed with an index number
                    string newName = arrayData.name + " 0";
                    int i = 0;
                    while (node.HasPort(newName)) newName = arrayData.name + " " + (++i);
                    if (io == XNode.NodePort.IO.Output) node.AddInstanceOutput(type, connectionType, newName);
                    else node.AddInstanceInput(type, connectionType, newName);
                    EditorUtility.SetDirty(node as UnityEngine.Object);
                    instancePortCount++;
                }
                while (arraySize < instancePortCount) {
                    arrayData.InsertArrayElementAtIndex(arraySize);
                    arraySize++;
                }
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
            if (onCreateReorderableList != null) onCreateReorderableList(list);
            return list;
        }
    }
}