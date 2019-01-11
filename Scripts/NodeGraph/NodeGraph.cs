using System;
using System.Collections.Generic;
using UnityEngine;

namespace XNode {
    /// <summary> Base class for all node graphs </summary>
    [Serializable]
    public class NodeGraph : ScriptableObject, INodeGraph {

        /// <summary> All nodes in the graph. <para/>
        /// See: <see cref="AddNode{T}"/> </summary>
        [SerializeField] public List<Node> nodes = new List<Node>();

        public int NodesCount { get { return nodes.Count; } }

        public INode[] GetNodes() {
            var result = new INode[nodes.Count];
            for (int i = 0; i < nodes.Count; i++) {
                result[i] = nodes[i];
            }
            return result;
        }

        /// <summary> Add a node to the graph by type </summary>
        public T AddNode<T>() where T : Node {
            return AddNode(typeof(T)) as T;
        }

        /// <summary> Placing it last in the nodes list </summary>
        public void MoveNodeToTop(INode node) {
            var castedNode = node as Node;
            int index;
            while ((index = nodes.IndexOf(castedNode)) != NodesCount - 1) {
                nodes[index] = nodes[index + 1];
                nodes[index + 1] = castedNode;
            }
        }

        /// <summary> Add a node to the graph by type </summary>
        public virtual INode AddNode(Type type) {
            Node.graphHotfix = this;
            Node node = ScriptableObject.CreateInstance(type) as Node;
            node.graph = this;
            nodes.Add(node);
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.AddObjectToAsset(node, this);
#endif
            return node;
        }

        /// <summary> Creates a copy of the original node in the graph </summary>
        public virtual INode CopyNode(INode original) {
            Node castedNode = original as Node;
            if(castedNode == null) {
                throw new ArgumentException("NodeGraph can only copy nodes scriptable objects");
            }

            Node.graphHotfix = this;
            Node node = ScriptableObject.Instantiate(castedNode);
            node.graph = this;
            node.ClearConnections();
            nodes.Add(node);
            return node;
        }

        /// <summary> Safely remove a node and all its connections </summary>
        /// <param name="node"> The node to remove </param>
        public void RemoveNode(INode node) {
            node.ClearConnections();
            nodes.Remove(node as Node);
            if (Application.isPlaying) Destroy(node as UnityEngine.Object);
        }

        /// <summary> Remove all nodes and connections from the graph </summary>
        public void Clear() {
            if (Application.isPlaying) {
                for (int i = 0; i < nodes.Count; i++) {
                    Destroy(nodes[i]);
                }
            }
            nodes.Clear();
        }

        /// <summary> Create a new deep copy of this graph </summary>
        public XNode.NodeGraph Copy() {
            // Instantiate a new nodegraph instance
            NodeGraph graph = Instantiate(this);
            // Instantiate all nodes inside the graph
            for (int i = 0; i < nodes.Count; i++) {
                if (nodes[i] == null) continue;
                Node.graphHotfix = graph;
                Node node = Instantiate(nodes[i]) as Node;
                node.graph = graph;
                graph.nodes[i] = node;
            }

            var oldNodes = new List<INode>(nodes);
            var newNodes = new List<INode>(graph.nodes);

            // Redirect all connections
            for (int i = 0; i < graph.nodes.Count; i++) {
                if (graph.nodes[i] == null) continue;
                foreach (NodePort port in graph.nodes[i].Ports) {
                    port.Redirect(oldNodes, newNodes);
                }
            }

            return graph;
        }

        private void OnDestroy() {
            // Remove all nodes prior to graph destruction
            Clear();
        }
    }
}