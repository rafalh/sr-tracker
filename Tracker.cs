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
	/**
	 * Tracker main class.
	 * Creates listening socket and accepts clients.
	 */
	public class Tracker
	{
		private const int PORT = 6666;

		private static readonly ILog log = LogManager.GetLogger (typeof(Tracker));

		private TcpListener tcpListener;
		private readonly ClientsManager clientsMgr = new ClientsManager ();
		private readonly ManualResetEvent stopEvent = new ManualResetEvent (false);
		private Thread acceptThread;
		//private bool election = false;

		public Tracker ()
		{
			// Create server socket
			log.Info ("Starting tracker...");
			tcpListener = new TcpListener (IPAddress.Any, PORT);
			tcpListener.Start ();

			// start ping checker thread
			acceptThread = new Thread(new ThreadStart(TimeoutCheckerThreadProc));
			acceptThread.Name = "Tracker Accept";
			acceptThread.Start ();
		}

		/**
		 * Stop tracker threads.
		 */
		public void Stop ()
		{
			stopEvent.Set ();
			acceptThread.Join ();
		}

		/**
		 * Main tracker loop waiting for new clients.
		 */
		public void MainLoop ()
		{
			while (true) {
				log.Info ("Waiting for a connection...");

				// Perform a blocking call to accept requests.
				TcpClient tcpClient = tcpListener.AcceptTcpClient ();
				log.Info ("Connected: " + tcpClient.Client.RemoteEndPoint);
				clientsMgr.AddClient (tcpClient);
			}
		}

		private void TimeoutCheckerThreadProc ()
		{
			// periodically check if any client timed out
			while (!stopEvent.WaitOne (ConnectedClient.TimeOutValueMs)) {
				clientsMgr.CheckForTimeOuts ();
			}
		}
	}
}

