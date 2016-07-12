// Copyright (c) 2002-2016 Daxat, Inc. and Todd A. Mancini. All rights reserved.
// Daxat, Inc. and Todd A. Mancini licenses this file to you under the MIT license. 
// See the LICENSE file in the project root for more information. 
using System;

namespace Daxat.Ess.Indexers.FullTextIndexers
{
	/// <summary>
	/// Summary description for IKeyToDataPointerStore.
	/// </summary>
	internal interface IKeyToDataPointerStore
	{
		// TODO: Hmmm..I think we need a IKey interface that is a superinterface
		// to IBTreeKey
#if BTREE_DATA_POINTERS_32BITS
		void Insert(IComparable key, int val);
#else
		void Insert(IComparable key, long val);
#endif
		// All refs to BTree's need to be replaced with more generic interfaces
		IKeyDataPair Search(IComparable key);
	}
}
