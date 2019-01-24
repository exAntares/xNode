using System;

namespace XNode {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CreateNodeMenuAttribute : Attribute {
        public string menuName;
        public string[] Tags { get; private set; }

        /// <summary> Manually supply node class with a context menu path </summary>
        /// <param name="menuName"> Path to this node in the context menu. Null or empty hides it. </param>
        public CreateNodeMenuAttribute(string menuName, params string[] tags) {
            this.menuName = menuName;
            Tags = tags;
        }
    }
}
