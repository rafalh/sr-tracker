using System;
using System.Runtime.Serialization;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization.Json;
using log4net;

namespace SR.Packets
{
	[DataContract]
	public class NetworkPacket
	{
		public enum Type
		{
			PING = 0,
			JOIN = 1,
			JOIN_RESP = 2,
			PEER_HELLO = 3,
			DISCONNECTED = 4,
			REQ = 5, // zadanie dostepu do zasobu
			REQ_RESP = 6, // odpowiedz na zadanie
			TOKEN = 7, // przekazywanie tokenu
			CONNECTIONS_INFO = 8,
			BULLY = 9, // tyran
			ELECTION_REQ = 10, // początek elekcji
			ELECTION_RESP = 11,
			ELECTION_END = 12,
		};

		static readonly ILog log = LogManager.GetLogger(typeof(NetworkPacket));

		public NetworkPacket(Type type)
		{
			this.type = (int) type;
		}

		[DataMember(IsRequired = true)]
		public int type;

		[DataMember(EmitDefaultValue = false)]
		public String id = null;

		[DataMember(EmitDefaultValue = false)]
		public String ip = null;

		[DataMember(EmitDefaultValue = false)]
		public int? r;

		[DataMember(EmitDefaultValue = false)]
		public String dst_id = null;

		[DataMember(EmitDefaultValue = false)]
		public int? idx;

		[DataMember(EmitDefaultValue = false)]
		public String table = null; // FIXME

		[DataMember(EmitDefaultValue = false)]
		public String connections = null; // FIXME

		[DataMember(EmitDefaultValue = false)]
		public int? elmode;

		[DataMember(EmitDefaultValue = false)]
		public bool? allowed;

		public void Write(NetworkStream stream)
		{
			MemoryStream memStream = new MemoryStream();
			DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(NetworkPacket));
			ser.WriteObject(memStream, this);

			BinaryWriter writer = new BinaryWriter (stream);
			int size = (int) memStream.Position;
			writer.Write (size); // number of bytes
			memStream.Position = 0;
			memStream.CopyTo(stream);

			memStream.Position = 0;
			StreamReader sr = new StreamReader(memStream);
			log.Debug("Sent packet: " + sr.ReadToEnd());
		}

		public static NetworkPacket Read(NetworkStream stream)
		{
			BinaryReader binaryReader = new BinaryReader (stream);
			int size = binaryReader.ReadInt32 ();
			byte[] data = binaryReader.ReadBytes (size);

			DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(NetworkPacket));
			MemoryStream memStream = new MemoryStream(data);
			NetworkPacket packet = (NetworkPacket) ser.ReadObject (memStream);

			memStream.Position = 0;
			StreamReader sr = new StreamReader(memStream);
			log.Debug("Received packet: " + sr.ReadToEnd());

			return packet;
		}
	}
}

