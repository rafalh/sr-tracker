using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using log4net;

namespace SR.Tracker
{
	public class TreeNode
	{
		//private static readonly ILog log = LogManager.GetLogger (typeof(TreeNode));

		public string Id { get; protected set; }
		public IPEndPoint ListenEndPoint { get; set; }
		public TreeNode Parent { get; set; }
		public bool Joined { get; set; }
		public List<TreeNode> Children { get; protected set; } = new List<TreeNode>();

		public TreeNode(string nodeId, IPEndPoint endPoint)
		{
			Id = nodeId;
			ListenEndPoint = endPoint;
			Joined = true;
		}

		public TreeNode(string nodeId)
		{
			Id = nodeId;
			Joined = false;
		}

		public bool HasChildRecursive(TreeNode node)
		{
			if (Children.Exists ((TreeNode currentNode) => currentNode.Equals (node)))
				return true;

			foreach (TreeNode child in Children)
				if (child.HasChildRecursive (node))
					return true;

			return false;
		}

		public List<TreeNode> GetChildrenRecursive ()
		{
			List<TreeNode> result = new List<TreeNode> ();
			foreach (TreeNode node in Children)
				result.AddRange(node.GetChildrenRecursive ());
			return result;
		}
	}
}

