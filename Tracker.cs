using System;
using System.Net;
using System.Net.Sockets;
using SR;
using SR.Packets;
using System.IO;
using log4net;

namespace SR.Tracker
{
	public class Tracker
	{
		private TcpListener server;

		private const int PORT = 6666;

		static readonly ILog log = LogManager.GetLogger(typeof(Tracker));

		public Tracker ()
		{
			log.Info ("Starting tracker...");
			IPAddress ipAddress = Dns.GetHostEntry ("localhost").AddressList [0];
			server = new TcpListener (ipAddress, PORT);
			server.Start ();
		}

		public void MainLoop ()
		{
			while (true) {
				log.Info ("Waiting for a connection...");

				// Perform a blocking call to accept requests.
				// You could also user server.AcceptSocket() here.
				TcpClient client = server.AcceptTcpClient ();
				log.Info ("Connected: " + client.Client.RemoteEndPoint);

				// Get a stream object for reading and writing
				NetworkStream stream = client.GetStream ();

				while (true) {
					NetworkPacket packet;
					try {
						packet = NetworkPacket.Read (stream);
					} catch (EndOfStreamException e) {
						log.Info ("Disconnected.");
						break;
					}
					HandlePacket (packet, client);
				}

				// Shutdown and end connection
				client.Close ();
			}
		}

		private void HandlePacket (NetworkPacket packet, TcpClient client)
		{
			switch ((NetworkPacket.Type) packet.type) {

			case NetworkPacket.Type.JOIN:
				HandleJoinPacket (packet, client);
				break;

			default:
				log.Warn ("Unknown packet " + packet.type);
				break;
			}
		}

		private void HandleJoinPacket (NetworkPacket packet, TcpClient client)
		{
			log.Info ("Join Packet - ID " + packet.id);
			NetworkPacket resp = new NetworkPacket (NetworkPacket.Type.JOIN_RESP);
			resp.id = packet.id;
			resp.ip = null; // FIXME
			resp.Write(client.GetStream());
		}

		private void BroadcastPacket (NetworkPacket packet)
		{
			// TODO
		}
	}
}

