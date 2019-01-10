using System.Collections.Generic;
using UnityEngine;

namespace XNode {
    public interface INode {
        INodeGraph Graph { get; }
        Vector2 Position { get; set; }
        object GetValue(NodePort port);
        NodePort GetPort(string fieldName);
        void UpdateStaticPorts();
        IEnumerable<NodePort> Outputs { get; }
        IEnumerable<NodePort> Inputs { get; }
        NodePort GetInputPort(string fieldName);
        NodePort GetOutputPort(string fieldName);
        void OnCreateConnection(NodePort from, NodePort to);
        void OnRemoveConnection(NodePort port);
        void ClearConnections();
    }
}
