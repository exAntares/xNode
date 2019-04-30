using System;

namespace XNodeEditor {
    [AttributeUsage(AttributeTargets.Class)]
    public class CustomEditorAttribute : Attribute, INodeEditorAttrib {
        private Type inspectedType;
        public string editorPrefsKey;

        /// <summary> Tells a NodeEditor which Node type it is an editor for </summary>
        /// <param name="inspectedType">Type that this editor can edit</param>
        public CustomEditorAttribute(Type inspectedType) {
            this.inspectedType = inspectedType;
            this.editorPrefsKey = "XNode.Settings." + inspectedType.FullName;
        }

        public Type GetInspectedType() {
            return inspectedType;
        }
    }
}
