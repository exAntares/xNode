using System;
using System.Collections.Generic;
using UnityEditor;

namespace XNodeEditor.Internal {
    /// <summary> Handles caching of custom editor classes and their target types. Accessible with GetEditor(Type type) </summary>
    /// <typeparam name="T">Editor Type. Should be the type of the deriving script itself (eg. NodeEditor) </typeparam>
    /// <typeparam name="A">Attribute Type. The attribute used to connect with the runtime type (eg. CustomNodeEditorAttribute) </typeparam>
    /// <typeparam name="K">Runtime Type. The ScriptableObject this can be an editor for (eg. Node) </typeparam>
    public abstract class NodeEditorBase<T, K> where T : class, ICustomEditor<K> where K : class {
		/// <summary> Custom editors defined with [CustomNodeEditor] </summary>
		private static Dictionary<Type, Type> editorTypes;
		private static Dictionary<UnityEngine.Object, T> editors = new Dictionary<UnityEngine.Object, T>();

		public static T GetEditor(K target) {
			if ((target as UnityEngine.Object) == null) return default(T);
			T editor;
			if (!editors.TryGetValue(target as UnityEngine.Object, out editor)) {
				Type type = target.GetType();
				Type editorType = GetEditorType(type);
				editor = Activator.CreateInstance(editorType) as T;
				editor.Target = target;
				editor.SerializedObject = new SerializedObject(target as UnityEngine.Object);
				editors.Add(target as UnityEngine.Object, editor);
			}
            if ((editor.Target as UnityEngine.Object) == null) {
                editor.Target = target;
            }
			if (editor.SerializedObject == null) editor.SerializedObject = new SerializedObject(target as UnityEngine.Object);
			return editor;
		}

		private static Type GetEditorType(Type type) {
			if (type == null) return null;
			if (editorTypes == null) CacheCustomEditors();
			Type result;
			if (editorTypes.TryGetValue(type, out result)) return result;
            //If type isn't found, try base type
            var baseTypeEditor = GetEditorType(type.BaseType);
            if (baseTypeEditor != null) { return baseTypeEditor; }

            //If base type isn't found, try interfaces
            var interfaces = type.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++) {
                var editorType = GetEditorType(interfaces[i]);
                if (editorType != null) {
                    return editorType;
                }
            }

            return null;
		}

		private static void CacheCustomEditors() {
			editorTypes = new Dictionary<Type, Type>();

			//Get all classes deriving from NodeEditor via reflection
			Type[] nodeEditors = XNodeEditor.NodeEditorWindow.GetDerivedTypes(typeof(T));
			for (int i = 0; i < nodeEditors.Length; i++) {
				if (nodeEditors[i].IsAbstract) continue;
				var attribs = nodeEditors[i].GetCustomAttributes(typeof(CustomEditorAttribute), false);
				if (attribs == null || attribs.Length == 0) continue;
                CustomEditorAttribute attrib = attribs[0] as CustomEditorAttribute;
				editorTypes.Add(attrib.GetInspectedType(), nodeEditors[i]);
			}
		}
	}
}
