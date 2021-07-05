// Copyright (c) 2002-2016 Daxat, Inc. and Todd A. Mancini. All rights reserved.
// Daxat, Inc. and Todd A. Mancini licenses this file to you under the MIT license. 
// See the LICENSE file in the project root for more information. 
using System;
using System.IO;
using Daxat.Ess.Indexers.FullTextIndexers;
using Daxat.Ess.Utils.Collections;
using System.Threading;
using Daxat.Ess.Utils.System;

namespace Daxat.Ess.Indexers.BTreeIndexers
{
	/// <summary>
	/// 
	/// </summary>
	internal sealed class BTreeStorage
	{
		public BTreeStorage(String parentId, Stream stream, IMapKeyToObject mapper)
		{
			_parentId = parentId;
			_parentIdHash = _parentId.GetHashCode();
			BaseStream = stream;
#if BTREE_NODE_POINTERS_32BITS
 #warning Btree File Corruption Possible
			maxPosition = (int)BaseStream.Length;
#else
			maxPosition = BaseStream.Length;
#endif
			_mapper = mapper;

			_keyTest = new BucketedBoundedHashtable.KeyTest(IsCacheKeyFromThisBTreeStorage);
		}

		BucketedBoundedHashtable.KeyTest _keyTest;

		public BTreeStorage(String parentId, Stream stream, bool writeCache, IMapKeyToObject mapper) : this(parentId, stream, mapper)
		{
			this.writeCache = writeCache;
		}

		// create a new phsyical node at end of storage
		public BTreeNode AllocateNode()
		{
			rwLock.AcquireWriterLock(-1);
			BTreeNode newNode = new BTreeNode(baseStream, maxPosition, _mapper);
			maxPosition += BTreeNode.PAGE_SIZE;
			rwLock.ReleaseWriterLock();
			return newNode;
			
		}

		struct Int96
		{
			public Int96(Int64 a, Int32 b)
			{
				this.a = a;
				this.b = b;
			}

			public Int64 a;
			public Int32 b;

			public override int GetHashCode()
			{
				return a.GetHashCode() ^ b;
			}

			public override bool Equals(Object obj) 
			{
				if (!(obj is Int96)) 
				{
					return false;
				}
				Int96 o = (Int96)obj;
				return (a == o.a && b == o.b);
			}
		}

		public object GetKey(long position)
		{
			//return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}-{1}", position, _parentId);
			//return position.ToString()+_parentId;
			return new Int96(position, _parentIdHash);
		}

		public void Write(BTreeNode x)
		{
			rwLock.AcquireWriterLock(-1);
			if (!writeCache)
				x.Flush();
			nodeCache[GetKey(x.position)] = x;
			rwLock.ReleaseWriterLock();
		}

		static long cacheHit = 0;
		static long cacheMiss = 0;
		static long cacheHitRecent = 0;
		static long cacheMissRecent = 0;
#if BTREE_NODE_POINTERS_32BITS
		public BTreeNode Read(int position)
#else
		public BTreeNode Read(long position)
#endif
		{
			rwLock.AcquireReaderLock(-1);
			BTreeNode nodeFromCache = (BTreeNode)(nodeCache[GetKey(position)]);
			rwLock.ReleaseReaderLock();
			if (nodeFromCache != null)
			{
//				cacheHit++;
//				cacheHitRecent++;
//				if ((cacheHit + cacheMiss) % 10000 == 0)
//				{
//					Console.WriteLine("\tBTreeNode Cache hit percentage = {0:F2}% ({1:F2} recent)", cacheHit*100.0/(cacheHit + cacheMiss), cacheHitRecent*100.0/(cacheHitRecent + cacheMissRecent));
//					cacheHitRecent = cacheMissRecent = 0;
//				}
				return nodeFromCache;
			}
//			cacheMiss++;
//			cacheMissRecent++;
//			if ((cacheHit + cacheMiss) % 10000 == 0)
//			{
//				Console.WriteLine("\tBTreeNode Cache hit percentage = {0:F2}% ({1:F2} recent)", cacheHit*100.0/(cacheHit + cacheMiss), cacheHitRecent*100.0/(cacheHitRecent + cacheMissRecent));
//				cacheHitRecent = cacheMissRecent = 0;
//			}
			//baseStream.Position = position;
			rwLock.AcquireWriterLock(-1);
			BTreeNode btn = new BTreeNode(position, baseStreamReader, _mapper);
			nodeCache[GetKey(position)] = btn;
			rwLock.ReleaseWriterLock();
			return btn;
		}

		public void Flush()
		{
			// If we don't have a write cache, there cannot possibly
			// be any uncommitted node.
			if (writeCache)
			{
				rwLock.AcquireWriterLock(-1);
				nodeCache.FlushAll();
				rwLock.ReleaseWriterLock();
			}
		}

		private Stream baseStream = null;
		private BinaryReader baseStreamReader;
		public Stream BaseStream
		{
			get
			{
				Stream stream;
				rwLock.AcquireReaderLock(-1);
				stream = baseStream;
				rwLock.ReleaseReaderLock();
				return stream;
			}
			set
			{
				rwLock.AcquireWriterLock(-1);
				baseStream = value;
				baseStreamReader = new BinaryReader(baseStream);
				rwLock.ReleaseWriterLock();
			}
		}

		public void Close()
		{
			// We need to invalidate all of the cache entries belonging to this storage,
			// because a cache entry is not valid if we are closed!
			nodeCache.Remove(_keyTest);
			// truncate storage to match true size of data
			rwLock.AcquireWriterLock(-1);
			baseStream.SetLength(maxPosition);
			baseStream.Close();
			rwLock.ReleaseWriterLock();
		}

		public bool IsCacheKeyFromThisBTreeStorage(object key)
		{
			if (!(key is Int96)) return false;
			return ((Int96)key).b == _parentIdHash;
		}

		// Allocate approximately 1/18 of the targetted max heap size to the node cache of ALL buckets, ALL indexes.
		private static BucketedBoundedHashtable nodeCache = new BucketedBoundedHashtable((int)(((long)MyComputer.TargetMaxHeapSize*1024/18)/BTreeNode.PAGE_SIZE));
		private bool writeCache = false;

#if BTREE_NODE_POINTERS_32BITS
		int maxPosition = 0;
#else
		long maxPosition = 0;
#endif
		
		IMapKeyToObject _mapper;

		String _parentId;
		Int32 _parentIdHash;

		ReaderWriterLock rwLock = new ReaderWriterLock();
  }
}
