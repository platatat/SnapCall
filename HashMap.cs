using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SnapCall
{
	class HashMapEntry
	{
		public ulong Key { get; set; }
		public uint Value { get; set; }

		public static byte[] ToBytes(HashMapEntry entry)
		{
			byte[] bytes = new byte[12];
			return bytes;
		}
	}

	[ProtoContract]
	class ArrayWrapper
	{
		[ProtoMember(1)]
		public ulong[] Array { get; set; }
	}

	/// <summary>
	/// High performance, quickly serializable dictionary implementation.
	/// Several warnings:
	/// - Can't use zero as a key
	/// - There's no duplicate key checking, it will fuck you up
	/// - No delete operation
	/// </summary>
	[ProtoContract]
	public class HashMap
	{
		[ProtoMember(1)]
		private uint Size { get; set; }
		[ProtoMember(2)]
		private uint Count { get; set; }
		[ProtoMember(3)]
		private uint TotalSize { get; set; }
		[ProtoMember(4)]
		private List<ArrayWrapper> Data { get; set; }
		[ProtoMember(5)]
		public int Misses { get; set; }

		public HashMap() { }

		public HashMap(uint size)
		{
			TotalSize = size * 2;
			Count = 1;
			while (TotalSize / Count > 10000000) Count *= 2;
			Size = TotalSize / Count;
			Data = new List<ArrayWrapper>();
			TotalSize = Size * Count;
			for (int i = 0; i < Count; i++)
			{
				var wrapper = new ArrayWrapper();
				wrapper.Array = new ulong[Size];
				Data.Add(wrapper);
			}
			Misses = 0;
		}

		public ulong this[ulong key]
		{
			get
			{
				ulong index = (key * 2) % TotalSize;
				int subarray = (int)(index / Size);
				while (true)
				{
					if (Data[subarray].Array[index % Size] == key) return Data[subarray].Array[index % Size + 1];
					index += 2;
				}
			}
			set
			{
				ulong index = (key * 2) % TotalSize;
				int subarray = (int)(index / Size);
				while (true)
				{
					if (Data[subarray].Array[index % Size] == 0)
					{
						Data[subarray].Array[index % Size] = key;
						Data[subarray].Array[index % Size + 1] = value;
						break;
					}
					index += 2;
					Misses++;
				}
			}
		}

		public static byte[] Serialize(HashMap tData)
		{
			using (var ms = new MemoryStream())
			{
				Serializer.Serialize(ms, tData);
				return ms.ToArray();
			}
		}

		public static HashMap Deserialize(byte[] tData)
		{
			using (var ms = new MemoryStream(tData))
			{
				return Serializer.Deserialize<HashMap>(ms);
			}
		}
	}
}
