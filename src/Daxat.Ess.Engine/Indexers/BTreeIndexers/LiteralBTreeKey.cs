// Copyright (c) 2002-2016 Daxat, Inc. and Todd A. Mancini. All rights reserved.
// Daxat, Inc. and Todd A. Mancini licenses this file to you under the MIT license. 
// See the LICENSE file in the project root for more information. 
using System;
using Daxat.Ess.Literals;
using Daxat.Ess.Utils;

namespace Daxat.Ess.Indexers.BTreeIndexers
{
	/// <summary>
	/// 
	/// </summary>
	internal sealed class LiteralBTreeKey : IBTreeKey
	{
		public LiteralBTreeKey() {}

#if BTREE_KEYS_32BITS
		public LiteralBTreeKey(int key, Utils.ILiteral literal)
#else
		public LiteralBTreeKey(long key, ILiteral literal)
#endif
		{
			this.key = key;
			this.literal = literal;
		}

		public int CompareTo(object obj)
		{
			if (obj is LiteralBTreeKey)
			{
				ILiteral literal2 = ((LiteralBTreeKey)obj).literal;
				return literal.CompareTo(literal2);
			}
			else throw new ArgumentException("obj not of type LiteralBTreeKey", "obj");
		}

#if BTREE_KEYS_32BITS
		public int GetBTreeKey()
#else
		public long GetBTreeKey()
#endif
		{
			return key;
		}
		
		public IPatternComparable GetBTreeKeyNative()
		{
			return literal;
		}

		public override string ToString()
		{
			return literal.ToString();
		}

#if BTREE_KEYS_32BITS
		public int key;
#else
		public long key;
#endif
		public ILiteral literal;
	}
}
