using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using System.Net;
using SR.Packets;

namespace SR.Tracker
{
	public class TreeManager
	{
		private static readonly ILog log = LogManager.GetLogger (typeof(TreeManager));

		private readonly object mutex = new object ();
		private readonly List<TreeNode> nodes = new List<TreeNode> ();

		/**
		 * Finds node with given ID
		 */
		public TreeNode GetNodeById (string id)
		{
			lock (mutex) {
				foreach (TreeNode node in nodes) {
					if (node.Id == id)
						return node;
				}
				return null;
			}
		}

		/**
		 * Updated node information. If node doesn't exists add it to internal list.
		 */
		public TreeNode AddOrUpdateNode (string id, IPEndPoint endPoint)
		{
			lock (mutex) {
				TreeNode node = GetOrCreateNode (id);
				node.ListenEndPoint = endPoint;
				//node.Children.Clear ();
				//UpdateNodeFromConnections (node, connections);
				//if (connections.Count == 0) {
					node.Parent = FindParentForNode (node);
				//}
				node.Joined = true;
				return node;
			}
		}

		/**
		 * Removes node from internal structures.
		 */
		public void RemoveNode (TreeNode node)
		{
			lock (mutex) {
				log.Info ("Removing node " + node.Id);
				nodes.Remove (node);
				foreach (TreeNode child in node.Children) {
					child.Parent = null; // children should ask for new parent
					child.Joined = false;
				}
			}
		}

		private TreeNode GetOrCreateNode(string id)
		{
			TreeNode node = GetNodeById (id);
			if (node == null) {
				log.Info ("New node " + id);
				node = new TreeNode (id);
				node.Joined = false;
				nodes.Add (node);
			}
			return node;
		}

		/*private void UpdateNodeFromConnections(TreeNode node, List<ConnectionInfo> connections)
		{
			foreach (ConnectionInfo info in connections) {
				if (info.isIncomming) {
					TreeNode child = GetOrCreateNode(info.id);
					child.Parent = node;
					node.Children.Add (child);
				} else {
					node.Parent = GetOrCreateNode(info.id);
					if (node.Parent.Children.IndexOf (node) == -1) {
						node.Parent.Children.Add (node);
					}
				}
			}
		}*/

		private TreeNode FindParentForNode (TreeNode node)
		{
			var parentCandidates = nodes.Where ((TreeNode currentNode) => {
				if (!currentNode.Joined) {
					// Ignore nodes which lost parent and are expected to join again
					return false;
				}
				if (node != null && (currentNode.Equals(node) || node.HasChildRecursive (currentNode))) {
					// Ignore recursive children of this node
					return false;
				}
				return true;
			}).ToList ();

			if (parentCandidates.Count == 0) {
				return null; // this is the root node
			}

			// Find nodes with 1-7 children
			var bestParents = parentCandidates.Where ((TreeNode currentNode) => {
				return currentNode.Children.Count > 0 && currentNode.Children.Count < 8;
			});

			if (bestParents.Count () > 0) {
				return bestParents.First ();
			} else {
				return parentCandidates.First ();
			}
		}
	}
}

