using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using log4net;

namespace SR.Tracker
{
	public class NetworkNode
	{
		public const int TimeOutValueMs = 15000;

		private static readonly ILog log = LogManager.GetLogger (typeof(NetworkNode));

		public String Id { get; private set; }
		public int ListenPort { get; private set; }
		public NetworkNode Parent { get; set; }
		public DateTime LastPing { get; private set; }
		public bool Joined { get; private set; }
		public EndPoint EndPoint {
			get {
				return Client.Client.RemoteEndPoint;
			}
		}
		public List<NetworkNode> Children { get; private set; } = new List<NetworkNode>();
		public TcpClient Client { get; private set; }

		public NetworkNode (String id, TcpClient tcpClient, int listenPort)
		{
			this.Id = id;
			this.Client = tcpClient;
			this.ListenPort = listenPort;
			Joined = true;
		}

		public void AddChild(NetworkNode node)
		{
			Children.Add(node);
		}

		public bool HasChildRecursive(NetworkNode node)
		{
			if (Children.Exists ((NetworkNode currentNode) => currentNode.Equals (node)))
				return true;

			foreach (NetworkNode child in Children)
				if (child.HasChildRecursive (node))
					return true;

			return false;
		}

		public List<NetworkNode> GetChildrenRecursive ()
		{
			List<NetworkNode> result = new List<NetworkNode> ();
			foreach (NetworkNode node in Children)
				result.AddRange(node.GetChildrenRecursive ());
			return result;
		}

		public void UpdateLastPing ()
		{
			LastPing = DateTime.UtcNow;
		}

		public bool IsTimedOut ()
		{
			return (DateTime.UtcNow - LastPing).TotalMilliseconds > TimeOutValueMs;
		}

		public void KickIfTimedOut ()
		{
			if (IsTimedOut ()) {
				log.Info ("Node " + Id + " timed out!");
				Client.Close ();
			}
		}
	}
}

