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
		public JoinedEventArgs (string nodeId, IPEndPoint listenEndPoint) {
			this.NodeId = nodeId;
			this.ListenEndPoint = listenEndPoint;
		}

		public string NodeId { get; private set; }

		public IPEndPoint ListenEndPoint { get; private set; }
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
		public IPEndPoint EndPoint {
			get;
			private set;
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

		private readonly object mutex = new object();
		private TcpClient tcpClient;
		private Thread recvThread;
		private DateTime lastPing;

		public ConnectedClient (TcpClient tcpClient)
		{
			this.tcpClient = tcpClient;
			EndPoint = (IPEndPoint) tcpClient.Client.RemoteEndPoint;
			recvThread = new Thread (new ThreadStart (RecvThreadProc));
			//recvThread.Name = "Tracker Client Receive";
			lastPing = DateTime.Now;
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
			Disconnect ();
			if (!Thread.CurrentThread.Equals(recvThread)) {
				recvThread.Join ();
			}
		}
		
		private void Disconnect()
		{
			lock (mutex) {
				if (tcpClient != null) {
					try {
						tcpClient.Close ();
					} catch (Exception e) {
						log.Debug ("Exception when closing connection to peer: " + e.Message);
					}
					tcpClient = null;
				}
			}
		}

		/**
		 * Checks if client is timed out.
		 */
		public bool IsTimedOut ()
		{
			lock (mutex) {
				return (DateTime.Now - lastPing).TotalMilliseconds > TimeOutValueMs;
			}
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

		private void RecvThreadProc ()
		{
			while (tcpClient != null) {
				NetworkPacket packet;
				try {
					packet = NetworkPacket.Read (tcpClient.GetStream ());
				} catch (Exception e) {
					log.Info ("Disconnected from peer: " + e.Message);
					break;
				}
				HandlePacket (packet);
			}

			DisconnectedEvent?.Invoke (this, EventArgs.Empty);
		}

		private void HandlePacket (NetworkPacket packet)
		{
			switch ((NetworkPacket.Type)packet.type) {

			case NetworkPacket.Type.JoinReq:
				HandleJoinReqPacket (packet);
				break;

			case NetworkPacket.Type.Ping:
				HandlePingPacket (packet);
				break;

			case NetworkPacket.Type.Disconnected:
				HandleDisconnectedPacket (packet);
				break;

			case NetworkPacket.Type.ElectionReq:
				HandleElectionReqPacket (packet);
				break;

			case NetworkPacket.Type.ElectionFinish:
				HandleElectionEndPacket (packet);
				break;

			default:
				log.Warn ("Unknown packet " + packet.type);
				break;
			}
		}

		private void HandlePingPacket (NetworkPacket packet)
		{
			log.Info ("Ping Packet - " + EndPoint);
			lock (mutex) {
				lastPing = DateTime.Now;
			}
		}

		private bool SendPacket(NetworkPacket packet)
		{
			lock (mutex) {
				if (tcpClient == null) {
					return false;
				}

				try {
					packet.Write (tcpClient.GetStream ());
				} catch (Exception e) {
					log.Debug ("Exception when writing packer: " + e.Message);
					return false;
				}
			}
			return true;
		}

		private void HandleJoinReqPacket (NetworkPacket packet)
		{
			Debug.Assert (packet.id != null && packet.port != null);
			log.Info ("Join Packet - " + EndPoint + " " + packet.id);
			IPEndPoint endPoint = (IPEndPoint)EndPoint;
			IPEndPoint listenEndPoint = new IPEndPoint (endPoint.Address, (int)packet.port);

			JoinedEvent?.Invoke (this, new JoinedEventArgs(packet.id, listenEndPoint));

			// Send response now (Note: JoinedEvent handler could changed our TreeNode).
			NetworkPacket resp = new NetworkPacket (NetworkPacket.Type.JoinResp);
			if (TreeNode.Parent != null) {
				resp.id = TreeNode.Parent.Id;
				resp.ip = TreeNode.Parent.ListenEndPoint.Address.ToString ();
				resp.port = TreeNode.Parent.ListenEndPoint.Port;
			} else {
				resp.id = packet.id;
			}
			SendPacket (resp);
		}

		private void HandleDisconnectedPacket (NetworkPacket packet)
		{
			log.Info ("Disconnected Packet - ID " + packet.id);
			if (packet.id == TreeNode.Id) {
				// disconnect ourself
				Disconnect ();
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

