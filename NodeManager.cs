using System;
using System.Collections.Generic;

namespace SR.Tracker
{
	public class NodeManager
	{
		private List<ClientNode> nodes = new List<ClientNode> ();

		public ClientNode getNodeByIp(String ip)
		{
			foreach (ClientNode node in nodes)
				if (node.Ip == ip)
					return node;
			return null;
		}

		public ClientNode getNodeById(String id)
		{
			foreach (ClientNode node in nodes)
				if (node.Id == id)
					return node;
			return null;
		}

		private ClientNode getParentForNewNode()
		{
			if (nodes.Count == 0)
				return null;
			return nodes.GetEnumerator ().Current;
		}

		public void addNode(ClientNode node)
		{
			node.Parent = getParentForNewNode();
			nodes.Add (node);
		}

		public void RemoveNode(ClientNode node)
		{
			nodes.Remove (node);
			// FIXME: children
		}
	}
}

