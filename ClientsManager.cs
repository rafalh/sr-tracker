﻿using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace SR.Tracker
{
	public class ClientsManager
	{
		private static readonly ILog log = LogManager.GetLogger (typeof(ClientsManager));

		private readonly object mutex = new object ();
		private readonly List<ConnectedClient> clients = new List<ConnectedClient> ();
		private readonly TreeManager treeMgr = new TreeManager ();
		private DateTime lastElection = DateTime.Now;
		private const int TimeElectionValueMs = 10000;
		private bool stoping = false;

		public void AddClient (TcpClient tcpClient)
		{
			ConnectedClient client = new ConnectedClient (tcpClient);
			client.DisconnectedEvent += OnClientDisconnected;
			client.JoinedEvent += OnClientJoined;
			client.ElectionEvent += OnElectionReq;
			lock (mutex) {
				client.Start ();
				clients.Add (client);
			}
		}

		public void CheckForTimeOuts ()
		{
			log.Debug ("Checking for timeout...");
			lock (mutex) {
				List<ConnectedClient> clientsCopy = new List<ConnectedClient> (clients);
				foreach (ConnectedClient client in clientsCopy) {
					if (client.StopIfTimedOut ()) {
						// client timed out - remove him from our lists
						RemoveClient (client);
					}
				}
			}
		}

		public void StopAll()
		{
			lock (mutex) {
				stoping = true;
				foreach (ConnectedClient client in clients) {
					client.Stop ();
				}
				clients.Clear ();
				stoping = false;
			}

		}

		private void RemoveClient (ConnectedClient client)
		{
			if (client.TreeNode != null) {
				treeMgr.RemoveNode (client.TreeNode);
			}
			clients.Remove (client);
		}

		private void OnElectionReq (Object sender, EventArgs eventArgs)
		{
			lock (mutex) {
				if ((DateTime.Now - lastElection).TotalMilliseconds > TimeElectionValueMs) {
					ConnectedClient max = clients [0];
					foreach (ConnectedClient client in clients) {
						if (String.Compare (max.TreeNode.Id, client.TreeNode.Id, true) > 0)
							max = client;
					}
					max.SetElectionLeader ();
					lastElection = DateTime.Now;
				}
			}
		}

		private void OnClientDisconnected (Object sender, EventArgs eventArgs)
		{
			ConnectedClient client = (ConnectedClient) sender;
			if (stoping) {
				return;
			}

			lock (mutex) {
				
				if (client.TreeNode != null) {
					log.Info ("Node " + client.TreeNode.Id + " disconnected.");
				} else {
					log.Info (client.EndPoint + " disconnected before establishing connection.");
				}

				RemoveClient (client);
			}
		}

		private void OnClientJoined (Object sender, JoinedEventArgs e)
		{
			ConnectedClient client = (ConnectedClient) sender;
			TreeNode node = treeMgr.AddOrUpdateNode (e.NodeId, e.ListenEndPoint);
			client.TreeNode = node;

			if (node.Parent != null) {
				log.Info ("Parent for node " + e.NodeId + ": " + node.Parent.Id);
			} else {
				log.Info ("Node " + e.NodeId + " is a root node");
			}
		}
	}
}

