// Copyright (c) 2002-2016 Daxat, Inc. and Todd A. Mancini. All rights reserved.
// Daxat, Inc. and Todd A. Mancini licenses this file to you under the MIT license. 
// See the LICENSE file in the project root for more information. 
using System;

namespace Daxat.Ess.Utils.Collections
{
	/// <summary>
	/// Summary description for LiteralOccurrence.
	/// </summary>
	/// <exclude/>
	[Serializable]
	public sealed class LiteralOccurrence
	{
		public LiteralOccurrence(string literal, long occurrence)
		{
			Literal = literal;
			Occurrence = occurrence;
		}

		public string Literal;
		public long Occurrence;
	}
}
