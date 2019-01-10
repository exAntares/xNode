using UnityEngine;

namespace XNode {
    public interface INode {
        NodeGraph Graph { get; }
        Vector2 Position { get; set; }
        object GetValue(NodePort port);
        NodePort GetPort(string fieldName);
        void UpdateStaticPorts();
        NodePort GetInputPort(string fieldName);
        NodePort GetOutputPort(string fieldName);
        void OnCreateConnection(NodePort from, NodePort to);
        void OnRemoveConnection(NodePort port);
    }
}
