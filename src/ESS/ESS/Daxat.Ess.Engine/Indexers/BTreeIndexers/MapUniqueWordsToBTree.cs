// Copyright (c) 2002-2016 Daxat, Inc. and Todd A. Mancini. All rights reserved.
// Daxat, Inc. and Todd A. Mancini licenses this file to you under the MIT license. 
// See the LICENSE file in the project root for more information. 
using System;
using Daxat.Ess.Indexers.FullTextIndexers;
using Daxat.Ess.Indexers.UniqueWords;
using Daxat.Ess.Literals;

namespace Daxat.Ess.Indexers.BTreeIndexers
{
	/// <summary>
	/// 
	/// </summary>
	internal sealed class MapUniqueWordsToBTree : IMapKeyToObject
	{
		public MapUniqueWordsToBTree(UniqueWordList uw)
		{
			this.uw = uw;
		}

#if BTREE_KEYS_32BITS
		public IComparable GetObjectFromKey(int key)
#else
		public IComparable GetObjectFromKey(long key)
#endif
		{
			LiteralBTreeKey btkey = new LiteralBTreeKey();
			btkey.key = key;
			btkey.literal = uw.GetAt(key);
			return btkey;
		}

		public IComparable CreateKeyForObject(object obj)
		{
			ILiteral literal = uw.NewLiteral(obj.ToString());
			return new LiteralBTreeKey(uw.Add(literal), literal);
		}

		public IComparable CreateKeyForILiteral(ILiteral literal)
		{
			return new LiteralBTreeKey(uw.Add(literal), literal);
		}

		UniqueWordList uw;
	}
}
