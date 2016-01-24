using System;
using System.Net;
using System.Net.Sockets;
using SR;
using SR.Packets;
using System.IO;
using log4net;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace SR.Tracker
{
	public class Tracker
	{
		private const int PORT = 6666;

		static readonly ILog log = LogManager.GetLogger (typeof(Tracker));

		private TcpListener server;
		private NodeManager nodeMgr = new NodeManager ();
		private bool election = false;
		private Object packetHandlingMutex = new object ();

		public Tracker ()
		{
			log.Info ("Starting tracker...");
			server = new TcpListener (IPAddress.Any, PORT);
			server.Start ();

			ThreadPool.QueueUserWorkItem (state => {
				TimeoutCheckerThreadProc ();
			});
		}

		private void TimeoutCheckerThreadProc()
		{
			while (true) {
				Thread.Sleep (NetworkNode.TimeOutValueMs);
				// TODO: check
			}
		}

		private void ThreadProc (Object argument)
		{
			TcpClient tcpClient = (TcpClient)argument;
			// Get a stream object for reading and writing
			NetworkStream stream = tcpClient.GetStream ();

			while (true) {
				NetworkPacket packet;
				try {
					packet = NetworkPacket.Read (stream);
				} catch (Exception) {
					OnNodeDisconnected (tcpClient.Client.RemoteEndPoint);
					break;
				}
				lock (packetHandlingMutex) {
					HandlePacket (packet, tcpClient);
				}
			}

			// Shutdown and end connection
			tcpClient.Close ();
		}

		private void OnNodeDisconnected (EndPoint endPoint)
		{
			NetworkNode node = nodeMgr.GetNodeByEndpoint (endPoint);

			if (node != null) {
				log.Info ("Node " + node.Id + " disconnected.");
				nodeMgr.RemoveNode (node);
			} else {
				log.Info (endPoint + " disconnected before establishing connection.");
			}
		}

		public void MainLoop ()
		{
			while (true) {
				log.Info ("Waiting for a connection...");

				// Perform a blocking call to accept requests.
				TcpClient client = server.AcceptTcpClient ();
				log.Info ("Connected: " + client.Client.RemoteEndPoint);

				ThreadPool.QueueUserWorkItem (state => {
					ThreadProc (client);
				});
			}
		}

		private void HandlePacket (NetworkPacket packet, TcpClient client)
		{
			switch ((NetworkPacket.Type)packet.type) {

			case NetworkPacket.Type.JOIN:
				HandleJoinPacket (packet, client);
				break;

			case NetworkPacket.Type.PING:
				HandlePingPacket (packet, client);
				break;

			case NetworkPacket.Type.DISCONNECTED:
				HandleDisconnectedPacket (packet, client);
				break;

			case NetworkPacket.Type.CONNECTIONS_INFO:
				HandleConnectionsInfoPacket (packet, client);
				break;

			case NetworkPacket.Type.ELECTION_REQ:
				HandleElectionReqPacket (packet, client);
				break;

			case NetworkPacket.Type.ELECTION_END:
				HandleElectionEndPacket (packet, client);
				break;

			default:
				log.Warn ("Unknown packet " + packet.type);
				break;
			}
		}

		private void HandlePingPacket (NetworkPacket packet, TcpClient tcpClient)
		{
			log.Info ("Ping Packet - " + tcpClient.Client.RemoteEndPoint);

			NetworkNode node = nodeMgr.GetNodeByEndpoint (tcpClient.Client.RemoteEndPoint);
			if (node == null) {
				log.Error ("Unknown node " + tcpClient.Client.RemoteEndPoint);
				return;
			}

			node.UpdateLastPing ();
		}

		private void HandleJoinPacket (NetworkPacket packet, TcpClient client)
		{
			Debug.Assert (packet.id != null && packet.port != null);
			IPEndPoint endPoint = (IPEndPoint)client.Client.RemoteEndPoint;
			log.Info ("Join Packet - " + endPoint + " - " + packet.id);

			NetworkNode node = nodeMgr.GetNodeById (packet.id);
			if (node != null) {
				log.Warn ("Node tried to join with existing ID");
				nodeMgr.UpdateNodeParent (node);
				return;
			} else {
				node = new NetworkNode (packet.id, client, (int)packet.port);
				nodeMgr.AddNode (node);
			}

			NetworkPacket resp = new NetworkPacket (NetworkPacket.Type.JOIN_RESP);
			resp.id = packet.id;
			if (node.Parent != null) {
				resp.ip = ((IPEndPoint)node.Parent.EndPoint).Address.ToString ();
				resp.port = node.Parent.ListenPort;
			}
			resp.Write (client.GetStream ());
		}

		private void HandleDisconnectedPacket (NetworkPacket packet, TcpClient client)
		{
			log.Info ("Disconnected Packet - ID " + packet.id);
			NetworkNode node = nodeMgr.GetNodeById (packet.id);
			if (node == null) {
				log.Error ("Unknown node " + packet.id);
				return;
			}

			nodeMgr.RemoveNode (node);
		}

		private void HandleConnectionsInfoPacket (NetworkPacket packet, TcpClient client)
		{
			log.Info ("Connections Info Packet - ID " + packet.id);
			// TODO
		}

		private void HandleElectionReqPacket (NetworkPacket packet, TcpClient client)
		{
			log.Info ("Election Req Packet - ID " + packet.id);
			// TODO

			NetworkPacket resp = new NetworkPacket (NetworkPacket.Type.ELECTION_RESP);
			resp.allowed = !election;
			resp.Write (client.GetStream ());
			election = true;
		}

		private void HandleElectionEndPacket (NetworkPacket packet, TcpClient client)
		{
			log.Info ("Election End Packet - ID " + packet.id);
			election = false;
			// TODO
		}
	}
}

