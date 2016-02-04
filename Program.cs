using System;
using System.IO;
using System.Runtime.Serialization.Json;
using SR.Packets;
using log4net.Config;
using log4net;

namespace SR.Tracker
{
	class MainClass
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(MainClass));

		private static void configureLog4net()
		{
			//BasicConfigurator.Configure();
			string appDir = AppDomain.CurrentDomain.BaseDirectory;
			string confPath = Path.Combine (appDir, "..", "..", "log4net.xml");
			confPath = Path.GetFullPath (confPath);
			Console.WriteLine ("Loading log4net configuration from " + confPath);
			XmlConfigurator.Configure(new System.IO.FileInfo(confPath));
		}

		public static void Main (string[] args)
		{
			configureLog4net ();

			Console.WriteLine ("Projekt z przedmiotu SR");
			Console.WriteLine ("Implementacja algorytmu Ricarta-Agrawali w C#");
			Console.WriteLine ("TRACKER");
			Console.WriteLine ("Autor: Rafał Harabień");
			Console.WriteLine ();

			try {
				Tracker tracker = new Tracker ();
				tracker.MainLoop ();
			} catch (Exception e) {
				log.Error ("Exception " + e);
			}
		}
	}
}
