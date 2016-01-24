using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using System.Net;

namespace SR.Tracker
{
	public class NodeManager
	{
		private static readonly ILog log = LogManager.GetLogger (typeof(NodeManager));

		private Object mutex = new Object ();
		private List<NetworkNode> nodes = new List<NetworkNode> ();

		public NetworkNode GetNodeById (String id)
		{
			lock (mutex) {
				foreach (NetworkNode node in nodes)
					if (node.Id == id)
						return node;
				return null;
			}
		}

		public NetworkNode GetNodeByEndpoint (EndPoint endPoint)
		{
			lock (mutex) {
				foreach (NetworkNode node in nodes)
					if (node.EndPoint.Equals (endPoint))
						return node;
				return null;
			}
		}

		public void UpdateNodeParent (NetworkNode node)
		{
			lock (mutex) {
				node.Parent = FindParentForNode (node);
			}
		}

		public void AddNode (NetworkNode node)
		{
			lock (mutex) {
				log.Info ("Adding node " + node.Id);
				node.Parent = FindParentForNode (null);
				log.Info ("Parent for new node: " + node.Parent?.Id);
				nodes.Add (node);
				log.Info ("Number of nodes " + nodes.Count);
			}
		}

		public void RemoveNode (NetworkNode node)
		{
			lock (mutex) {
				log.Info ("Removing node " + node.Id);
				nodes.Remove (node);
				foreach (NetworkNode child in node.Children)
					child.Parent = null; // children should ask for new parent
			}
		}

		public void CheckForTimeOuts ()
		{
			log.Debug ("Checking for timeout...");
			foreach (NetworkNode node in nodes)
				node.KickIfTimedOut ();
		}

		private NetworkNode FindParentForNode (NetworkNode node)
		{
			var parentCandidates = nodes;
			if (node != null) {
				// Ignore recursive children of this node
				parentCandidates = nodes.Where ((NetworkNode currentNode) => {
					return currentNode != node && !node.HasChildRecursive (currentNode);
				}).ToList ();
			}

			if (parentCandidates.Count == 0)
				return null; // this is the root node

			// Find nodes with 1-7 children
			var bestParents = parentCandidates.Where ((NetworkNode currentNode) => {
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

