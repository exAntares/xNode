using System;

namespace XNode {
    /// <summary> Mark a serializable field as an output port. You can access this through <see cref="GetOutputPort(string)"/> </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class OutputAttribute : Attribute {
        public ShowBackingValue backingValue { get; }
        public ConnectionType connectionType { get; }
        public bool instancePortList { get; }
        public string EditorIconName { get; }
        public bool ShouldTint { get; }

        /// <summary> Mark a serializable field as an output port. You can access this through <see cref="GetOutputPort(string)"/> </summary>
        /// <param name="backingValue">Should we display the backing value for this port as an editor field? </param>
        /// <param name="connectionType">Should we allow multiple connections? </param>
        public OutputAttribute(ShowBackingValue backingValue = ShowBackingValue.Never, ConnectionType connectionType = ConnectionType.Multiple, bool instancePortList = false, string editorIconName = "white", bool shouldTint = true) {
            this.backingValue = backingValue;
            this.connectionType = connectionType;
            this.instancePortList = instancePortList;
            EditorIconName = editorIconName;
            ShouldTint = shouldTint;
        }
    }
}
