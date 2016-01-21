using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using System.Net;

namespace SR.Tracker
{
	public class NodeManager
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(NodeManager));
		private Object mutex = new Object();

		private List<ClientNode> nodes = new List<ClientNode> ();

		public ClientNode getNodeById(String id)
		{
			foreach (ClientNode node in nodes)
				if (node.Id == id)
					return node;
			return null;
		}

		public ClientNode getNodeByEndpoint(EndPoint endPoint)
		{
			foreach (ClientNode node in nodes)
				if (node.EndPoint.Equals(endPoint))
					return node;
			return null;
		}

		private ClientNode getParentForNewNode()
		{
			if (nodes.Count == 0)
				return null;
			return nodes.First ();
		}

		public void addNode(ClientNode node)
		{
			lock (mutex) {
				log.Info ("Adding node " + node.Id);
				node.Parent = getParentForNewNode ();
				log.Info ("Parent for new node: " + node.Parent?.Id);
				nodes.Add (node);
				log.Info ("Number of nodes " + nodes.Count);
			}

		}

		public void RemoveNode(ClientNode node)
		{
			lock (mutex) {
				log.Info ("Removing node " + node.Id);
				nodes.Remove (node);
				// FIXME: children
			}
		}
	}
}

