// Copyright (c) 2002-2016 Daxat, Inc. and Todd A. Mancini. All rights reserved.
// Daxat, Inc. and Todd A. Mancini licenses this file to you under the MIT license. 
// See the LICENSE file in the project root for more information. 
using System;
using System.IO;
using Daxat.Ess.Utils.Collections;
using Daxat.Ess.Indexers.FullTextIndexers;
using Daxat.Ess.Utils;

namespace Daxat.Ess.Indexers.BTreeIndexers
{
	/// <summary>
	/// Summary description for BTreeNode.
	/// </summary>
	internal sealed class BTreeNode : ICacheNode
	{
		public static short MINIMUM_DEGREE = 15;
		public static int PAGE_SIZE = 2 +
			(
#if BTREE_KEYS_32BITS
			4
#else
			8
#endif
			+
#if BTREE_DATA_POINTERS_32BITS
			4
#else
			8
#endif
			)
			*(2*MINIMUM_DEGREE - 1) +
#if BTREE_NODE_POINTERS_32BITS
			4
#else
			8
#endif
			*(2*MINIMUM_DEGREE) + 1;

		public BTreeNode(Stream storage, IMapKeyToObject mapper)
		{
			this.storage = storage;
			_mapper = mapper;
			binaryWriter = new BinaryWriter(storage);
			for (int i=0; i < (2*MINIMUM_DEGREE - 1); i++)
			{
				keys[i] = 0;
				dataPointers[i] = 0;
				children[i] = 0;
			}
			children[2*MINIMUM_DEGREE - 1] = 0;
			changed = true;
		}

#if BTREE_NODE_POINTERS_32BITS
		public BTreeNode(Stream storage, int position, IMapBTreeKeyToObject mapper)
#else
		public BTreeNode(Stream storage, long position, IMapKeyToObject mapper)
#endif
		{
			this.storage = storage;
			_mapper = mapper;
			binaryWriter = new BinaryWriter(storage);
			this.position = position;
			changed = true;
		}

#if BTREE_NODE_POINTERS_32BITS
		public BTreeNode(Stream storage, int position, bool isLeaf, IMapBTreeKeyToObject mapper) : this(storage, position, mapper)
#else
		public BTreeNode(Stream storage, long position, bool isLeaf, IMapKeyToObject mapper) : this(storage, position, mapper)
#endif
		{
			this.isLeaf = isLeaf;
			changed = true;
		}

		// This piece of code is some of the slowest, but that problem is
		// masked by using a node cache elsewhere.
#if BTREE_NODE_POINTERS_32BITS
		public BTreeNode(int position, BinaryReader imageReader, IMapBTreeKeyToObject mapper) : this(imageReader.BaseStream, position, mapper)
#else
		public BTreeNode(long position, BinaryReader imageReader, IMapKeyToObject mapper) : this(imageReader.BaseStream, position, mapper)
#endif
		{
			storage.Position = position;
			numKeys = imageReader.ReadInt16();
			int i = 0;
			for (i=0; i < numKeys; i++)
			{
#if BTREE_KEYS_32BITS
				keys[i] = imageReader.ReadInt32();
#else
				keys[i] = imageReader.ReadInt64();
#endif
#if BTREE_DATA_POINTERS_32BITS
				dataPointers[i] = imageReader.ReadInt32();
#else
				dataPointers[i] = imageReader.ReadInt64();
#endif
#if BTREE_NODE_POINTERS_32BITS
				children[i] = imageReader.ReadInt32();
#else
				children[i] = imageReader.ReadInt64();
#endif
			}
#if BTREE_NODE_POINTERS_32BITS
			if (numKeys != 0) children[i] = imageReader.ReadInt32();
			else imageReader.BaseStream.Position += 4;
#else
			if (numKeys != 0) children[i] = imageReader.ReadInt64();
			else imageReader.ReadInt64();
#endif
#if BTREE_KEYS_32_BITS
 #if BTREE_DATA_POINTERS_32BITS
  #if BTREE_NODE_POINTERS_32BITS
			//k32,d32,n32
			if (numKeys < (2*MINIMUM_DEGREE - 1)) imageReader.BaseStream.Position += 12*((2*MINIMUM_DEGREE - 1)-numKeys);
  #else
			//k32,d32,n64
			if (numKeys < (2*MINIMUM_DEGREE - 1)) imageReader.BaseStream.Position += 16*((2*MINIMUM_DEGREE - 1)-numKeys);
  #endif
 #else
  #if BTREE_NODE_POINTERS_32BITS
			//k32,d64,n32
			if (numKeys < (2*MINIMUM_DEGREE - 1)) imageReader.BaseStream.Position += 16*((2*MINIMUM_DEGREE - 1)-numKeys);
  #else
			//k32,d64,n64
			if (numKeys < (2*MINIMUM_DEGREE - 1)) imageReader.BaseStream.Position += 20*((2*MINIMUM_DEGREE - 1)-numKeys);
  #endif
 #endif
#else
 #if BTREE_DATA_POINTERS_32BITS
  #if BTREE_NODE_POINTERS_32BITS
			//k64,d32,n32
			if (numKeys < (2*MINIMUM_DEGREE - 1)) imageReader.BaseStream.Position += 16*((2*MINIMUM_DEGREE - 1)-numKeys);
  #else
			//k64,d32,n64
			if (numKeys < (2*MINIMUM_DEGREE - 1)) imageReader.BaseStream.Position += 20*((2*MINIMUM_DEGREE - 1)-numKeys);
  #endif
 #else
  #if BTREE_NODE_POINTERS_32BITS
			//k64,d64,n32
			if (numKeys < (2*MINIMUM_DEGREE - 1)) imageReader.BaseStream.Position += 20*((2*MINIMUM_DEGREE - 1)-numKeys);
  #else
			//k64,d64,n64
			if (numKeys < (2*MINIMUM_DEGREE - 1)) 
			{
				// imageReader.BaseStream.Position += 24*((2*MINIMUM_DEGREE - 1)-numKeys);
				for (int j=numKeys; j < (2*MINIMUM_DEGREE - 1); j++)
				{
					imageReader.ReadUInt64(); imageReader.ReadUInt64(); imageReader.ReadUInt64();
				}
			}
  #endif
 #endif
#endif

			isLeaf = imageReader.ReadBoolean();

			// As this node was just read from disk, it is both committed and
			// unchanged -- safe for discarding.
			changed = false;
		}

		// This code is painfully slow.  There is no easy way to fix this (e.g. use
		// a cache) because write-caches are too dangerous.
		// Perhaps if, instead of a for-loop over data we just dumpped a huge
		// byte array, things would be better.  But this would require the
		// internal use of a byte array and the constant Int64 get's and set's
		// to that byte array -- and that may cause a worse performance problem
		// elsewhere.
		public void /*MemoryStream*/ Serialization()
		{
			//MemoryStream imageStream = new MemoryStream(4096);
			//			MemoryStream imageStream = new MemoryStream(PAGE_SIZE);
			//			BinaryWriter imageWriter = new BinaryWriter(imageStream);
			//			imageWriter.Write(numKeys);
			binaryWriter.Write(numKeys);
			int i=0;
			for (i=0; i < numKeys; i++)
			{
				//				imageWriter.Write(keys[i]);
				//				imageWriter.Write(dataPointers[i]);
				//				imageWriter.Write(children[i]);
				binaryWriter.Write(keys[i]);
				binaryWriter.Write(dataPointers[i]);
				binaryWriter.Write(children[i]);
			}
			//			imageWriter.Write(children[i]);
			//			if (numKeys < (2*MINIMUM_DEGREE - 1)) imageWriter.BaseStream.Position += 24*((2*MINIMUM_DEGREE - 1)-numKeys);
			//			imageWriter.Write(isLeaf);
			binaryWriter.Write(children[i]);
#if BTREE_KEYS_32_BITS
#if BTREE_DATA_POINTERS_32BITS
#if BTREE_NODE_POINTERS_32BITS
			//k32,d32,n32
			if (numKeys < (2*MINIMUM_DEGREE - 1)) storage.Position += 12*((2*MINIMUM_DEGREE - 1)-numKeys);
#else
			//k32,d32,n64
			if (numKeys < (2*MINIMUM_DEGREE - 1)) storage.Position += 16*((2*MINIMUM_DEGREE - 1)-numKeys);
#endif
#else
#if BTREE_NODE_POINTERS_32BITS
			//k32,d64,n32
			if (numKeys < (2*MINIMUM_DEGREE - 1)) storage.Position += 16*((2*MINIMUM_DEGREE - 1)-numKeys);
#else
			//k32,d64,n64
			if (numKeys < (2*MINIMUM_DEGREE - 1)) storage.Position += 20*((2*MINIMUM_DEGREE - 1)-numKeys);
#endif
#endif
#else
#if BTREE_DATA_POINTERS_32BITS
#if BTREE_NODE_POINTERS_32BITS
			//k64,d32,n32
			if (numKeys < (2*MINIMUM_DEGREE - 1)) storage.Position += 16*((2*MINIMUM_DEGREE - 1)-numKeys);
#else
			//k64,d32,n64
			if (numKeys < (2*MINIMUM_DEGREE - 1)) storage.Position += 20*((2*MINIMUM_DEGREE - 1)-numKeys);
#endif
#else
#if BTREE_NODE_POINTERS_32BITS
			//k64,d64,n32
			if (numKeys < (2*MINIMUM_DEGREE - 1)) storage.Position += 20*((2*MINIMUM_DEGREE - 1)-numKeys);
#else
			//k64,d64,n64
			if (numKeys < (2*MINIMUM_DEGREE - 1)) storage.Position += 24*((2*MINIMUM_DEGREE - 1)-numKeys);
  #endif
 #endif
#endif
			
			binaryWriter.Write(isLeaf);
			// If you want to pad out the page, do so now
			//			return imageStream;
		}

		public void copyDataFrom(BTreeNode x)
		{
			for (int i=0; i < x.numKeys; i++)
			{
				keys[i] = x.keys[i];
				keysNative[i] = x.keysNative[i];
				dataPointers[i] = x.dataPointers[i];
			}
			for (int i=0; i <= x.numKeys; i++)
			{
				children[i] = x.children[i];
			}
			numKeys = x.numKeys;
			isLeaf = x.isLeaf;
			changed = true;
		}

		public void Flush()
		{
			if (!changed) return;
			if (storage.Length < position + 1)
				//storage.SetLength(position + PAGE_SIZE);
				storage.SetLength(System.Math.Max(position + PAGE_SIZE, storage.Length + 0x100000));
			storage.Position = position;
			Serialization();//.WriteTo(storage);
			changed = false;
		}

#if BTREE_KEYS_32BITS
		private int[] keys = new int[2*MINIMUM_DEGREE - 1];
		public int GetKey(int index)
#else
		private long[] keys = new long[2*MINIMUM_DEGREE - 1];
		public long GetKey(int index)
#endif
		{
			return keys[index];
		}
#if BTREE_KEYS_32BITS
		public void SetKey(int index, int value)
#else
		public void SetKey(int index, long value)
#endif
		{
			changed = true;
			keys[index] = value;
		}
		private IPatternComparable[] keysNative = new IPatternComparable[2*MINIMUM_DEGREE - 1];
		public IPatternComparable GetKeyNative(int index)
		{
			IPatternComparable result = keysNative[index];
			if (result == null)
			{
				result = keysNative[index] = ((IBTreeKey)_mapper.GetObjectFromKey(keys[index])).GetBTreeKeyNative();
			}
			return result;
		}
		public void SetKeyNative(int index, IPatternComparable value)
		{
			changed = true;
			keysNative[index] = value;
		}
		//		public IBTreeKey[] BTreeKeys;
		private short numKeys = 0;
		public short NumKeys
		{
			get { return numKeys; }
			set
			{
				changed = true;
				numKeys = value;
			}
		}

#if BTREE_DATA_POINTERS_32BITS
		private int[] dataPointers = new int[2*MINIMUM_DEGREE - 1];
		public int GetDataPointer(int index)
#else
		private long[] dataPointers = new long[2*MINIMUM_DEGREE - 1];
		public long GetDataPointer(int index)
#endif
		{
			return dataPointers[index];
		}
#if BTREE_DATA_POINTERS_32BITS
		public void SetDataPointer(int index, int value)
#else
		public void SetDataPointer(int index, long value)
#endif
		{
			changed = true;
			dataPointers[index] = value;
		}

#if BTREE_NODE_POINTERS_32BITS
		private int[] children = new int[2*MINIMUM_DEGREE];
		public int GetChild(int index)
#else
		private long[] children = new long[2*MINIMUM_DEGREE];
		public long GetChild(int index)
#endif
		{
			return children[index];
		}
#if BTREE_NODE_POINTERS_32BITS
		public void SetChild(int index, int value)
#else
		public void SetChild(int index, long value)
#endif
		{
			changed = true;
			children[index] = value;
		}

		private bool isLeaf = true;
		public bool IsLeaf
		{
			get { return isLeaf; }
			set
			{
				if (value != isLeaf)
				{
					changed = true;
					isLeaf = value;
				}
			}
		}

		// When serialized to or from storage, position is the phyiscal location
		// at which the node lives.
#if BTREE_NODE_POINTERS_32BITS
		public int position = 0;
#else
		public long position = 0;
#endif

		// Has this node been changed since last committed?
		private bool changed = true;

		// Storage to which this node should be written
		public Stream storage;
		public BinaryWriter binaryWriter;

		protected IMapKeyToObject _mapper;
	}
}
