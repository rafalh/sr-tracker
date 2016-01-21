using System;
using System.Collections.Generic;
using System.Net;

namespace SR.Tracker
{
	public class ClientNode
	{
		private List<ClientNode> children;

		public String Id { get; private set; }
		public int ListenPort { get; private set; }
		public ClientNode Parent { get; set; }
		public DateTime LastPing { get; private set; }
		public EndPoint EndPoint { get; private set; }

		public ClientNode (String id, EndPoint endPoint, int listenPort)
		{
			this.Id = id;
			this.EndPoint = endPoint;
			this.ListenPort = listenPort;
		}

		public void AddChild(ClientNode node)
		{
			children.Add(node);
		}

		public void UpdateLastPing()
		{
			LastPing = DateTime.UtcNow;
		}
	}
}

