// Copyright (c) 2002-2016 Daxat, Inc. and Todd A. Mancini. All rights reserved.
// Daxat, Inc. and Todd A. Mancini licenses this file to you under the MIT license. 
// See the LICENSE file in the project root for more information. 
using System;
using Daxat.Ess.Utils;

namespace Daxat.Ess.Indexers.BTreeIndexers
{
	/// <summary>
	/// Summary description for ICompareIBTreeKeyToInternalNumericKey.
	/// </summary>
	internal interface ICompareIBTreeKeyToInternalNumericKey
	{
#if BTREE_KEYS_32BITS
		int CompareIBTreeKeyToNumeric(IBTreeKey btkey, int key);
		IBTreeKey NumericToIBTreeKey(int key);
#else
		int CompareIBTreeKeyToNumeric(IBTreeKey btkey, long key);
		IBTreeKey NumericToIBTreeKey(long key);
#endif
		PatternComparison PatternCompare(string pattern, IPatternComparable literal);
	}
}
