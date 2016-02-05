using System;
using System.Runtime.Serialization;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization.Json;
using log4net;
using System.Collections.Generic;
using System.Net;

namespace SR.Packets
{
	[DataContract]
	public class ConnectionInfo
	{
		[DataMember]
		public string id;

		[DataMember]
		public bool isIncomming;
	}

	[DataContract]
	public class TokenClient
	{
		[DataMember]
		public string id;

		[DataMember]
		public int r;

		[DataMember]
		public int g;
	}

	[DataContract]
	public class NetworkPacket
	{
		public enum Type
		{
			Ping = 0,
			JoinReq = 1,
			JoinResp = 2,
			PeerHello = 3,
			Disconnected = 4,
			TokenReq = 5, // zadanie dostepu do zasobu
			TokenBusy = 6, // odpowiedz na zadanie
			Token = 7, // przekazywanie tokenu
			ElectionReq = 10, // początek elekcji
			ElectionFinish = 12,
		};

		static readonly ILog log = LogManager.GetLogger(typeof(NetworkPacket));

		public NetworkPacket(Type type)
		{
			this.type = (int) type;
		}

		[DataMember(IsRequired = true)]
		public int type;

		[DataMember(EmitDefaultValue = false)]
		public string id;

		[DataMember(EmitDefaultValue = false)]
		public int? port;

		[DataMember(EmitDefaultValue = false)]
		public string ip;

		[DataMember(EmitDefaultValue = false)]
		public int? r;

		[DataMember(EmitDefaultValue = false)]
		public List<TokenClient> table;

		public void Write(NetworkStream stream)
		{
			MemoryStream memStream = new MemoryStream();
			DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(NetworkPacket));
			ser.WriteObject(memStream, this);

			BinaryWriter writer = new BinaryWriter (stream);
			int size = (int) memStream.Position; // number of bytes
			size = IPAddress.HostToNetworkOrder(size); // convert to Big Endian
			writer.Write (size);
			memStream.Position = 0;
			memStream.CopyTo(stream);

			if (log.IsDebugEnabled) {
				memStream.Position = 0;
				StreamReader sr = new StreamReader (memStream);
				log.Debug ("Sent packet: " + sr.ReadToEnd ());
			}
		}

		public static NetworkPacket Read(NetworkStream stream)
		{
			BinaryReader binaryReader = new BinaryReader (stream);
			int size = binaryReader.ReadInt32 ();
			size = IPAddress.NetworkToHostOrder(size); // convert from Big Endian
			byte[] data = binaryReader.ReadBytes (size);

			DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(NetworkPacket));
			MemoryStream memStream = new MemoryStream(data);
			NetworkPacket packet = (NetworkPacket) ser.ReadObject (memStream);

			if (log.IsDebugEnabled) {
				memStream.Position = 0;
				StreamReader sr = new StreamReader (memStream);
				log.Debug ("Received packet: " + sr.ReadToEnd ());
			}

			return packet;
		}
	}
}

