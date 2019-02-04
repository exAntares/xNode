using System;

namespace XNode {
    /// <summary> Mark a serializable field as an input port. You can access this through <see cref="GetInputPort(string)"/> </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class InputAttribute : Attribute {
        public ShowBackingValue backingValue { get; }
        public ConnectionType connectionType { get; }
        public bool instancePortList { get; }
        public string EditorIconName { get; }
        public bool ShouldTint { get; }

        /// <summary> Mark a serializable field as an input port. You can access this through <see cref="GetInputPort(string)"/> </summary>
        /// <param name="backingValue">Should we display the backing value for this port as an editor field? </param>
        /// <param name="connectionType">Should we allow multiple connections? </param>
        public InputAttribute(ShowBackingValue backingValue = ShowBackingValue.Unconnected, ConnectionType connectionType = ConnectionType.Multiple, bool instancePortList = false, string editorIconName = "white", bool shouldTint = true) {
            this.backingValue = backingValue;
            this.connectionType = connectionType;
            this.instancePortList = instancePortList;
            EditorIconName = editorIconName;
            ShouldTint = shouldTint;
        }
    }
}
