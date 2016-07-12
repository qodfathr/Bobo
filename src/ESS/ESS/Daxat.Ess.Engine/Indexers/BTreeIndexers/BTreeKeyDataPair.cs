// Copyright (c) 2002-2016 Daxat, Inc. and Todd A. Mancini. All rights reserved.
// Daxat, Inc. and Todd A. Mancini licenses this file to you under the MIT license. 
// See the LICENSE file in the project root for more information. 
using System;
using Daxat.Ess.Indexers.FullTextIndexers;

namespace Daxat.Ess.Indexers.BTreeIndexers
{
	/// <summary>
	/// 
	/// </summary>
	internal sealed class BTreeKeyDataPair : IKeyDataPair
	{
		public BTreeKeyDataPair()
		{
		}

#if BTREE_KEYS_32BITS
 #if BTREE_DATA_POINTERS_32BITS
		public BTreeKeyDataPair(int key, int dataPointer)
 #else
		public BTreeKeyDataPair(int key, long dataPointer)
 #endif
#else
 #if BTREEE_DATA_POINTERS_32BITS
		public BTreeKeyDataPair(long key, intdataPointer)
 #else
		public BTreeKeyDataPair(long key, long dataPointer)
 #endif
#endif
		{
			this.key = key;
			this.dataPointer = dataPointer;
		}

#if BTREE_KEYS_32BITS
		public int key;
#else
		public long key;
#endif
#if BTREE_DATA_POINTERS_32BITS
		public int dataPointer;
#else
		public long dataPointer;
#endif
	}
}
