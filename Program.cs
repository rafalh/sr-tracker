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
		static readonly ILog log = LogManager.GetLogger(typeof(Tracker));

		public static void Main (string[] args)
		{
			BasicConfigurator.Configure();

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
