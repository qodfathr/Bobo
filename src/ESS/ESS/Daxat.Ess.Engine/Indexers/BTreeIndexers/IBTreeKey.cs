// Copyright (c) 2002-2016 Daxat, Inc. and Todd A. Mancini. All rights reserved.
// Daxat, Inc. and Todd A. Mancini licenses this file to you under the MIT license. 
// See the LICENSE file in the project root for more information. 
using System;
using Daxat.Ess.Utils;

namespace Daxat.Ess.Indexers.BTreeIndexers
{
	/// <summary>
	/// 
	/// </summary>
	internal interface IBTreeKey : IComparable
	{
#if BTREE_KEYS_32BITS
		int GetBTreeKey();
#else
		long GetBTreeKey();
#endif
		IPatternComparable GetBTreeKeyNative();
	}
}
