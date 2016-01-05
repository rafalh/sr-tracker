using System;
using System.Collections.Generic;

namespace SR.Tracker
{
	public class ClientNode
	{
		private List<ClientNode> children;

		public String Id { get; private set; }
		public String Ip { get; private set; }
		public ClientNode Parent { get; set; }
		public DateTime LastPing { get; private set; }

		public ClientNode (String id, String ip)
		{
			this.Id = id;
			this.Ip = ip;
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

