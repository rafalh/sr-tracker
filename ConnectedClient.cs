using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using log4net;
using System.Threading;
using SR.Packets;
using System.IO;
using System.Diagnostics;

namespace SR.Tracker
{
	public class JoinedEventArgs : EventArgs
	{
		public JoinedEventArgs (string nodeId, IPEndPoint listenEndPoint, List<ConnectionInfo> connections) {
			this.NodeId = nodeId;
			this.ListenEndPoint = listenEndPoint;
			this.Connections = connections;
		}

		public string NodeId { get; private set; }

		public IPEndPoint ListenEndPoint { get; private set; }

		public List<ConnectionInfo> Connections { get; private set; }
	}

	public delegate void JoinedEventHandler (object sender, JoinedEventArgs e);

	public class ConnectedClient
	{
		/**
		 * Ping time-out value in milliseconds.
		 */
		public const int TimeOutValueMs = 15000;

		/**
		 * Tree node reference or null.
		 */
		public TreeNode TreeNode { get; set; }

		/**
		 * Client IP address and port.
		 */
		public EndPoint EndPoint {
			get {
				return tcpClient.Client.RemoteEndPoint;
			}
		}

		/**
		 * Event - received node ID and listen port from client
		 */
		public event JoinedEventHandler JoinedEvent;

		/**
		 * Event - disconnected from this client (socket is closed)
		 */
		public event EventHandler DisconnectedEvent;

		private static readonly ILog log = LogManager.GetLogger (typeof(ConnectedClient));

		private TcpClient tcpClient;
		private readonly object mutex = new object();
		private Thread recvThread;
		private DateTime lastPing;
		private bool disconnecting = false;

		public ConnectedClient (TcpClient tcpClient)
		{
			this.tcpClient = tcpClient;
			recvThread = new Thread (new ThreadStart (RecvThreadProc));
			recvThread.Name = "Tracker Client Receive";
		}

		/**
		 * Start receive thread.
		 */
		public void Start ()
		{
			recvThread.Start ();
		}

		/**
		 * Stop client thread and closes connection - DisconnectEvent is not invoked.
		 */
		public void Stop ()
		{
			disconnecting = true;
			tcpClient.Close ();
			if (!Thread.CurrentThread.Equals(recvThread)) {
				recvThread.Join ();
			}
		}

		/**
		 * Checks if client is timed out.
		 */
		public bool IsTimedOut ()
		{
			return (DateTime.UtcNow - lastPing).TotalMilliseconds > TimeOutValueMs;
		}

		/**
		 * Runs Stop() if client is timed out.
		 */
		public bool StopIfTimedOut ()
		{
			if (IsTimedOut ()) {
				log.Info ("Node " + TreeNode?.Id + " timed out!");
				Stop ();
				return true;
			} else {
				return false;
			}
		}

		private void OnSocketError (Exception e)
		{
			if (!disconnecting) {
				// this is unexpected error
				log.Warn ("Socket error: " + e.Message);
				tcpClient.Close ();
				DisconnectedEvent?.Invoke (this, EventArgs.Empty);
			}
		}

		private void RecvThreadProc ()
		{
			NetworkStream stream = tcpClient.GetStream ();

			while (true) {
				try {
					NetworkPacket packet = NetworkPacket.Read (stream);
					lock (mutex) {
						HandlePacket (packet);
					}
				} catch (SocketException e) {
					OnSocketError (e);
					break;
				} catch (IOException e) {
					OnSocketError (e);
					break;
				}
			}
		}

		private void HandlePacket (NetworkPacket packet)
		{
			switch ((NetworkPacket.Type)packet.type) {

			case NetworkPacket.Type.JOIN:
				HandleJoinReqPacket (packet);
				break;

			case NetworkPacket.Type.PING:
				HandlePingPacket (packet);
				break;

			case NetworkPacket.Type.DISCONNECTED:
				HandleDisconnectedPacket (packet);
				break;

			case NetworkPacket.Type.ELECTION_REQ:
				HandleElectionReqPacket (packet);
				break;

			case NetworkPacket.Type.ELECTION_END:
				HandleElectionEndPacket (packet);
				break;

			default:
				log.Warn ("Unknown packet " + packet.type);
				break;
			}
		}

		private void HandlePingPacket (NetworkPacket packet)
		{
			log.Info ("Ping Packet - " + tcpClient.Client.RemoteEndPoint);
			lastPing = DateTime.UtcNow;
		}

		private void HandleJoinReqPacket (NetworkPacket packet)
		{
			Debug.Assert (packet.id != null && packet.port != null);
			log.Info ("Join Packet - " + tcpClient.Client.RemoteEndPoint + " - " + packet.id);
			IPEndPoint endPoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
			IPEndPoint listenEndPoint = new IPEndPoint (endPoint.Address, (int)packet.port);

			JoinedEvent?.Invoke (this, new JoinedEventArgs(packet.id, listenEndPoint, packet.connections));

			// Send response now (Note: JoinedEvent handler could changed our TreeNode).
			NetworkPacket resp = new NetworkPacket (NetworkPacket.Type.JOIN_RESP);
			if (TreeNode.Parent != null) {
				resp.id = TreeNode.Parent.Id;
				resp.ip = TreeNode.Parent.ListenEndPoint.Address.ToString ();
				resp.port = TreeNode.Parent.ListenEndPoint.Port;
			} else {
				resp.id = packet.id;
			}
			resp.Write (tcpClient.GetStream ());
		}

		private void HandleDisconnectedPacket (NetworkPacket packet)
		{
			log.Info ("Disconnected Packet - ID " + packet.id);
			if (packet.id == TreeNode.Id) {
				// disconnect ourself
				tcpClient.Close ();
			} else {
				// disconnect packet with invalid ID
				log.Warn("Invalid node ID in disconnected packet");
			}
		}

		private void HandleElectionReqPacket (NetworkPacket packet)
		{
			/*log.Info ("Election Req Packet - ID " + packet.id);
			// TODO

			NetworkPacket resp = new NetworkPacket (NetworkPacket.Type.ELECTION_RESP);
			resp.allowed = !election;
			resp.Write (client.GetStream ());
			election = true;*/
		}

		private void HandleElectionEndPacket (NetworkPacket packet)
		{
			/*log.Info ("Election End Packet - ID " + packet.id);
			election = false;*/
			// TODO
		}
	}
}

