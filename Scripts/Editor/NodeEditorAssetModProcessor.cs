using UnityEditor;
using UnityEngine;

namespace XNodeEditor {
    /// <summary> Deals with modified assets </summary>
    class NodeEditorAssetModProcessor : UnityEditor.AssetModificationProcessor {

        /// <summary> Automatically delete Node sub-assets before deleting their script.
        /// <para/> This is important to do, because you can't delete null sub assets. </summary> 
        private static AssetDeleteResult OnWillDeleteAsset (string path, RemoveAssetOptions options) {
            // Get the object that is requested for deletion
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object> (path);

            // If we aren't deleting a script, return
            if (!(obj is UnityEditor.MonoScript)) return AssetDeleteResult.DidNotDelete;

            // Check script type. Return if deleting a non-node script
            UnityEditor.MonoScript script = obj as UnityEditor.MonoScript;
            System.Type scriptType = script.GetClass ();
            if (scriptType == null || (scriptType != typeof (XNode.INode) && !scriptType.IsSubclassOf (typeof (XNode.INode)))) return AssetDeleteResult.DidNotDelete;

            // Find all ScriptableObjects using this script
            string[] guids = AssetDatabase.FindAssets ("t:" + scriptType);
            for (int i = 0; i < guids.Length; i++) {
                string assetpath = AssetDatabase.GUIDToAssetPath (guids[i]);
                Object[] objs = AssetDatabase.LoadAllAssetRepresentationsAtPath (assetpath);
                for (int k = 0; k < objs.Length; k++) {
                    XNode.INode node = objs[k] as XNode.INode;
                    if (node.GetType () == scriptType) {
                        if (node != null && node.Graph != null) {
                            // Delete the node and notify the user
                            Debug.LogWarning (node.Name + " of " + node.Graph + " depended on deleted script and has been removed automatically.", node.Graph as UnityEngine.Object);
                            node.Graph.RemoveNode (node);
                        }
                    }
                }
            }
            // We didn't actually delete the script. Tell the internal system to carry on with normal deletion procedure
            return AssetDeleteResult.DidNotDelete;
        }
    }
}