// Copyright (c) 2002-2016 Daxat, Inc. and Todd A. Mancini. All rights reserved.
// Daxat, Inc. and Todd A. Mancini licenses this file to you under the MIT license. 
// See the LICENSE file in the project root for more information. 
using System;
using Daxat.Ess.Indexers.UniqueWords;
using Daxat.Ess.Utils;

namespace Daxat.Ess.Indexers.BTreeIndexers
{
	/// <summary>
	/// 
	/// </summary>
	internal sealed class CompareLiteralBTreeKeyToNumeric : ICompareIBTreeKeyToInternalNumericKey
	{
		public CompareLiteralBTreeKeyToNumeric(UniqueWordList uw)
		{
			this.uw = uw;
		}
		
		// I'm making this less pretty in hopes that it's more efficient.
#if BTREE_KEYS_32BITS
		int ICompareIBTreeKeyToInternalNumericKey.CompareIBTreeKeyToNumeric(IBTreeKey btkey, int key)
#else
		int ICompareIBTreeKeyToInternalNumericKey.CompareIBTreeKeyToNumeric(IBTreeKey btkey, long key)
#endif
		{
			if (!(btkey is LiteralBTreeKey)) throw new ArgumentException("btkey not of type LiteralBTreeKey", "btkey");
			return btkey.GetBTreeKeyNative().CompareTo(uw.GetAt(key));
		}

#if BTREE_KEYS_32BITS
		IBTreeKey ICompareIBTreeKeyToInternalNumericKey.NumericToIBTreeKey(int key)
#else
		IBTreeKey ICompareIBTreeKeyToInternalNumericKey.NumericToIBTreeKey(long key)
#endif
		{
			LiteralBTreeKey btkey = new LiteralBTreeKey();
			btkey.key = key;
			btkey.literal = uw.GetAt(key);
			return btkey;
		}

		PatternComparison ICompareIBTreeKeyToInternalNumericKey.PatternCompare(string pattern, IPatternComparable literal)
		{
			return literal.PatternCompareTo(pattern);
		}

		private UniqueWordList uw;
	}
}
