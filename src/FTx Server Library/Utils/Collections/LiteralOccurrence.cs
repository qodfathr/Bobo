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
