using System;
using System.Net;
using System.Net.Sockets;
using SR;
using SR.Packets;
using System.IO;
using log4net;
using System.Collections.Generic;
using System.Threading;

namespace SR.Tracker
{
	public class Tracker
	{
		private const int PORT = 6666;

		static readonly ILog log = LogManager.GetLogger(typeof(Tracker));

		private TcpListener server;
		private NodeManager nodeMgr = new NodeManager();

		public Tracker ()
		{
			log.Info ("Starting tracker...");
			IPAddress ipAddress = Dns.GetHostEntry ("localhost").AddressList [0];
			server = new TcpListener (ipAddress, PORT);
			server.Start ();
		}

		private void ThreadProc(Object argument)
		{
			TcpClient client = (TcpClient) argument;
			// Get a stream object for reading and writing
			NetworkStream stream = client.GetStream ();

			while (true) {
				NetworkPacket packet;
				try {
					packet = NetworkPacket.Read (stream);
				} catch (Exception) {
					log.Info ("Disconnected.");
					break;
				}
				HandlePacket (packet, client);
			}

			// Shutdown and end connection
			client.Close ();
		}

		public void MainLoop ()
		{
			while (true) {
				log.Info ("Waiting for a connection...");

				// Perform a blocking call to accept requests.
				// You could also user server.AcceptSocket() here.
				TcpClient client = server.AcceptTcpClient ();
				log.Info ("Connected: " + client.Client.RemoteEndPoint);

				ThreadPool.QueueUserWorkItem(state => {
					ThreadProc(client);
				});
			}
		}

		private void HandlePacket (NetworkPacket packet, TcpClient client)
		{
			switch ((NetworkPacket.Type) packet.type) {

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

		private void HandlePingPacket (NetworkPacket packet, TcpClient client)
		{
			String ip = client.Client.RemoteEndPoint.ToString ();
			ClientNode node = nodeMgr.getNodeByIp (ip);
			if (node == null) {
				log.Error ("Unknown node " + ip);
				return;
			}
			node.UpdateLastPing ();
		}

		private void HandleJoinPacket (NetworkPacket packet, TcpClient client)
		{
			log.Info ("Join Packet - ID " + packet.id);

			String ip = client.Client.RemoteEndPoint.ToString ();
			ClientNode node = new ClientNode (packet.id, ip);
			nodeMgr.addNode (node);

			NetworkPacket resp = new NetworkPacket (NetworkPacket.Type.JOIN_RESP);
			resp.id = packet.id;
			resp.ip = node.Parent?.Ip;
			resp.Write(client.GetStream());
		}

		private void HandleDisconnectedPacket (NetworkPacket packet, TcpClient client)
		{
			log.Info ("Disconnected Packet - ID " + packet.id);
			ClientNode node = nodeMgr.getNodeById (packet.id);
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
			resp.allowed = true;
			resp.Write(client.GetStream());
		}

		private void HandleElectionEndPacket (NetworkPacket packet, TcpClient client)
		{
			log.Info ("Election End Packet - ID " + packet.id);
			// TODO
		}

		private void BroadcastPacket (NetworkPacket packet)
		{
			// TODO
		}
	}
}

