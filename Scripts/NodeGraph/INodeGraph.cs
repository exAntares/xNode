using System;

namespace XNode {
    public interface INodeGraph {
        int NodesCount { get; }
        void MoveNodeToTop(INode node);
        INode[] GetNodes();
        INode AddNode(Type type);
        INode CopyNode(INode original);
        void RemoveNode(INode node);
    }
}
