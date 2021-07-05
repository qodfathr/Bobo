// Copyright (c) 2002-2016 Daxat, Inc. and Todd A. Mancini. All rights reserved.
// Daxat, Inc. and Todd A. Mancini licenses this file to you under the MIT license. 
// See the LICENSE file in the project root for more information. 
using System;
using System.IO;
using System.Collections;
using System.Threading;
using Daxat.Ess.Indexers.InvertedWordLists;
using Daxat.Ess.Indexers.FullTextIndexers;
using Daxat.Ess.Utils.Collections;
using Daxat.Ess.Utils;

namespace Daxat.Ess.Indexers.BTreeIndexers
{
	/// <summary>
	/// Summary description for BTree.
	/// </summary>
	internal sealed class BTree : IKeyToDataPointerStore
	{
		public BTree(string path, ICompareIBTreeKeyToInternalNumericKey comparer, IMapKeyToObject mapper, int bucket)
		{
			_mapper = mapper;
			// Store index on disk; do NOT use a write cache.
			string btreePath = Path.Combine(path, "index"+bucket+".idx");
			storage = new BTreeStorage(btreePath, new FileStream(btreePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None), false, _mapper);
			// Store index on disk; DO use a write cache.
			//storage = new BTreeStorage(new FileStream("index"+bucket+".idx", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None), true, _mapper);
			// Store index in memory
			//storage = new BTreeStorage();

			this.comparer = comparer;
			if (storage.BaseStream.Length == 0)
				Create();
			else root = storage.Read(0);
		}

		protected void Create()
		{
			root = storage.AllocateNode();
			root.IsLeaf = true;
			storage.Write(root);
		}

		public IKeyDataPair Search(IComparable comparableKey)
		{
			IBTreeKey key = comparableKey as IBTreeKey;
			_readerWriterLock.AcquireReaderLock(-1);
			BTreeKeyDataPair result = Search(root, key);
			_readerWriterLock.ReleaseReaderLock();
			return result;
		}

		internal BTreeKeyDataPair Search(BTreeNode x, IBTreeKey key)
		{
			int comparison = 0;
			object nativeKey2 = key.GetBTreeKeyNative() ;
			int i = FindKeyInNode(x, nativeKey2, ref comparison);
			if (i != -1 && comparison == 0)
			{
				return new BTreeKeyDataPair(key.GetBTreeKey(), x.GetDataPointer(i));
			}
			if (x.IsLeaf) return null;
			return Search(storage.Read(x.GetChild(i)), key);
		}

		protected int FindKeyInNode(BTreeNode x, object nativeKey2, ref int comparison)
		{
			// We use a binary, rather than linear, search to find the key, or, if
			// not found, the 'closest' key plus an indicatation of which child to follow.
			int i = 0;
			int high = x.NumKeys-1;
			if (high >= 0)
			{
				int low = 0;
				i = (high-low)/2;
				while ((comparison = x.GetKeyNative(i).CompareTo(nativeKey2)) != 0 && low < high)
				{
					if (comparison > 0)
					{
						if (i == high) break;
						low = i+1;
						i = (high-low)/2 + low;
					}
					else
					{
						if (i == low) break;
						high = i-1;
						i = (high-low)/2 + low;
					}
				}
				if (comparison > 0) i++;
				return i;
			}
			return -1;
		}

		internal bool Equals(BTreeNode x, int i, IBTreeKey key)
		{
			return x.GetKeyNative(i).Equals(key.GetBTreeKeyNative());
		}



		public IComparable[] GetAllMatches(string pattern)
		{
			_readerWriterLock.AcquireReaderLock(-1);
			ArrayList matches = new ArrayList();
			GetAllMatches(pattern, root, matches);
			_readerWriterLock.ReleaseReaderLock();
			return (IComparable[])matches.ToArray(typeof(IComparable));
		}

		private void GetAllMatches(string pattern, BTreeNode x, ArrayList matches)
		{
			for (int i=0; i < x.NumKeys; i++)
			{
				PatternComparison comparison = comparer.PatternCompare(pattern, x.GetKeyNative(i));
				if (comparison == PatternComparison.EQUAL)
					matches.Add(x.GetKeyNative(i));
				if (!x.IsLeaf && ((comparison == PatternComparison.EQUAL) || (comparison == PatternComparison.GREATERTHAN) || (comparison == PatternComparison.NOTEQUAL)))
					GetAllMatches(pattern, storage.Read(x.GetChild(i)), matches);
				if (comparison == PatternComparison.GREATERTHAN) break;
				if (!x.IsLeaf && i == x.NumKeys - 1)
				{
					if (storage.Read(x.GetChild(i+1)).position == 0) Console.WriteLine("   HERE!");
					GetAllMatches(pattern, storage.Read(x.GetChild(i+1)), matches);
				}
			}
		}

		protected BTreeNode SplitChild(BTreeNode x, short i, BTreeNode y)
		{
			BTreeNode z = storage.AllocateNode();
			z.IsLeaf = y.IsLeaf;
			z.NumKeys = (short)(BTreeNode.MINIMUM_DEGREE - 1);
			for (short j = 0; j < (short)(BTreeNode.MINIMUM_DEGREE - 1); j++)
			{
				z.SetKey(j, y.GetKey(j + BTreeNode.MINIMUM_DEGREE));
				z.SetKeyNative(j, y.GetKeyNative(j + BTreeNode.MINIMUM_DEGREE));
				z.SetDataPointer(j, y.GetDataPointer(j + BTreeNode.MINIMUM_DEGREE));
			}
			if (!y.IsLeaf)
				for (short j = 0; j < BTreeNode.MINIMUM_DEGREE; j++)
					z.SetChild(j, y.GetChild(j + BTreeNode.MINIMUM_DEGREE));
			y.NumKeys = (short)(BTreeNode.MINIMUM_DEGREE - 1);
			for (short j = x.NumKeys; j >= i + 1; j--)
				x.SetChild(j + 1, x.GetChild(j));
			x.SetChild(i+1, z.position);
			for (short j = (short)(x.NumKeys - 1); j >= i; j--)
			{
				x.SetKey(j+1, x.GetKey(j));
				x.SetKeyNative(j+1, x.GetKeyNative(j));
				x.SetDataPointer(j+1, x.GetDataPointer(j));
			}
			x.SetKey(i, y.GetKey(BTreeNode.MINIMUM_DEGREE - 1));
			x.SetKeyNative(i, y.GetKeyNative(BTreeNode.MINIMUM_DEGREE - 1));
			x.SetDataPointer(i, y.GetDataPointer(BTreeNode.MINIMUM_DEGREE - 1));
			x.NumKeys++;
			storage.Write(y);
			storage.Write(z);
			storage.Write(x);
			return z;
		}

#if BTREE_DATA_POINTERS_32BITS
		public void Insert(IComparable comparableKey, int val)
#else
		public void Insert(IComparable comparableKey, long val)
#endif
		{
			IBTreeKey key = comparableKey as IBTreeKey;
			_readerWriterLock.AcquireWriterLock(-1);
			if (root.NumKeys == (short)(2*BTreeNode.MINIMUM_DEGREE - 1))
			{
				BTreeNode s = storage.AllocateNode();
				s.copyDataFrom(root);
				root.IsLeaf = false;
				root.NumKeys = 0;
				root.SetChild(0, s.position);
				SplitChild(root, 0, s);
				InsertNonFull(root, key, val);
			}
			else InsertNonFull(root, key, val);
			_readerWriterLock.ReleaseWriterLock();
		}

#if BTREE_DATA_POINTERS_32BITS
		internal void InsertNonFull(BTreeNode x, IBTreeKey key, int val)
#else
		internal void InsertNonFull(BTreeNode x, IBTreeKey key, long val)
#endif
		{
#if BTREE_KEYS_32BITS
			int k = key.GetBTreeKey();
#else
			long k = key.GetBTreeKey();
#endif
			int i = x.NumKeys;
			if (x.IsLeaf)
			{
				//while (i >= 1 && comparer.CompareIBTreeKeyToLong(key, x.GetKey(i-1)) < 0)
				// First, we need to find the 1st key in the node which is greater than
				// the key we wish to insert.
				IPatternComparable nativeKey2 = key.GetBTreeKeyNative();
				int comparison = 0;
				int i2 = FindKeyInNode(x, nativeKey2, ref comparison);
				if (i2 == -1) i2 = 0;
				while (i > i2)
				{
					x.SetKey(i, x.GetKey(i-1));
					x.SetKeyNative(i, x.GetKeyNative(i-1));
					x.SetDataPointer(i, x.GetDataPointer(i-1));
					i--;
				}
				x.SetKey(i, k);
				x.SetKeyNative(i, nativeKey2);
				x.SetDataPointer(i, val);
				x.NumKeys++;
				storage.Write(x);
			}
			else
			{
				//while (i >= 1 && comparer.CompareIBTreeKeyToLong(key, x.GetKey(i-1)) < 0) i--;
				object nativeKey2 = key.GetBTreeKeyNative() ;
//				while (i >= 1 && x.GetKeyNative(i-1).CompareTo(nativeKey2) < 0) i--;
//				i++;
				int comparison = 0;
				i = FindKeyInNode(x, nativeKey2, ref comparison);
				if (i == -1) i = 0;
				i++;
//				if (i != i2)
//					Console.WriteLine("nope");
				BTreeNode y = storage.Read(x.GetChild(i-1));
				if (y.NumKeys == 2*BTreeNode.MINIMUM_DEGREE - 1)
				{
					BTreeNode z = SplitChild(x, (short)(i-1), y);
					//if (comparer.CompareIBTreeKeyToLong(key, x.GetKey(i-1)) > 0)
					if (x.GetKeyNative(i-1).CompareTo(nativeKey2) > 0)
					{
						i++;
						y = z;
					}
				}
				InsertNonFull(y, key, val);
			}
		}

		public void Flush()
		{
			_readerWriterLock.AcquireWriterLock(-1);
			storage.Flush();
			_readerWriterLock.ReleaseWriterLock();
		}

		public String ToXML()
		{
			_readerWriterLock.AcquireReaderLock(-1);
			string result = "<nodes>" + ToXML(root, null) + "</nodes>";
			_readerWriterLock.ReleaseReaderLock();
			return result;
		}

		public String ToXML(IInvertedWordList invertedWordList)
		{
			_readerWriterLock.AcquireReaderLock(-1);
			String result = "<nodes>" + ToXML(root, invertedWordList) + "</nodes>";
			_readerWriterLock.ReleaseReaderLock();
			return result;
		}

		protected String ToXML(BTreeNode x, IInvertedWordList invertedWordList)
		{
			String res = "";
			
			res += "<node>";
			res += "<keys>";
			for (int i=0; i < x.NumKeys; i++)
			{
				res += comparer.NumericToIBTreeKey(x.GetKey(i)).ToString() + " ";
				if (invertedWordList != null) res += invertedWordList.ToXML(x.GetDataPointer(i));
			}
			res += "</keys>";
			if (!x.IsLeaf)
			{
				res += "<children>";
				for (int i=0; i<= x.NumKeys; i++)
				{
					BTreeNode y = storage.Read(x.GetChild(i));
					res += ToXML(y, invertedWordList);
				}
				res += "</children>";
			}
			res += "</node>";
			return res;
		}

		public ArrayList DumpWords()
		{
			_readerWriterLock.AcquireReaderLock(-1);
			ArrayList words = new ArrayList();
			DumpWords(root, ref words);
			_readerWriterLock.ReleaseReaderLock();
			return words;
		}

		private void DumpWords(BTreeNode node, ref ArrayList words)
		{
			for (int i=0; i < node.NumKeys; i++)
			{
				if (!node.IsLeaf) DumpWords(storage.Read(node.GetChild(i)), ref words);
				words.Add(comparer.NumericToIBTreeKey(node.GetKey(i)).ToString());
			}
			if (!node.IsLeaf) DumpWords(storage.Read(node.GetChild(node.NumKeys)), ref words);
		}

		public void Close()
		{
			_readerWriterLock.AcquireWriterLock(-1);
			storage.Close();
			_readerWriterLock.ReleaseWriterLock();
		}

		/// <summary>
		/// Returns an array of literals (as strings) combined with their associated data pointers.
		/// Note that the return type suggests that the data pointer is the occurrence couunt -- it
		/// is NOT.  It is assumed that some higher-level function will convert the data pointers into
		/// occurrence counts.
		/// </summary>
		/// <returns></returns>
		internal LiteralOccurrence[] GetLiterals()
		{
			ArrayList literalsOccurrenceAL = new ArrayList();
			_readerWriterLock.AcquireReaderLock(-1);
			GetLiterals(root, literalsOccurrenceAL);
			_readerWriterLock.ReleaseReaderLock();
			LiteralOccurrence[] literalsOccurrence = new LiteralOccurrence[literalsOccurrenceAL.Count];
			literalsOccurrenceAL.CopyTo(literalsOccurrence);
			return literalsOccurrence;
		}

		private void GetLiterals(BTreeNode node, ArrayList literalsOccurrence)
		{
			for (int i=0; i < node.NumKeys; i++)
			{
				if (!node.IsLeaf) GetLiterals(storage.Read(node.GetChild(i)), literalsOccurrence);
				literalsOccurrence.Add(
					new LiteralOccurrence(node.GetKeyNative(i).ToString(), node.GetDataPointer(i))
					);
			}
			if (!node.IsLeaf) GetLiterals(storage.Read(node.GetChild(node.NumKeys)), literalsOccurrence);
		}

		BTreeNode root = null;
		ICompareIBTreeKeyToInternalNumericKey comparer;
		IMapKeyToObject _mapper;
		BTreeStorage storage;
		ReaderWriterLock _readerWriterLock = new ReaderWriterLock();
	}
}
