using UnityEditor;
using UnityEngine;
using XNode;

namespace XNodeEditor {
    [UnityEditor.CustomEditor(typeof(MonoNodeGraph))]
    public class MonoNodeInspector : Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if(GUILayout.Button("Open Inspector", GUILayout.Height(50))) {
                NodeEditorWindow.OpenWithGraph(target as INodeGraph);
            }
        }
    }
}
