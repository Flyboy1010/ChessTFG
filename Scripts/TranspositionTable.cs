using Godot;
using System;
using System.Reflection.Metadata;

public class TranspositionTable
{
	// node type

    public enum NodeType
    {
        Exact,  // Exact score
        LowerBound, // Lower bound (beta cut-off)
        UpperBound // Upper bound (alpha cut-off)
    }

	// transposition entry struct

    public struct Entry
    {
        public ulong key;
        public byte depth;
        public int value;
        public NodeType nodeType;
		public Move move;

		public static int GetSize()
		{
			return System.Runtime.InteropServices.Marshal.SizeOf<Entry>();
        }
    }

	// lookup failed value

	public const int lookupFailed = int.MinValue;

    // entries

    private Entry[] entries;

	// ctor

	public TranspositionTable(int size)
	{
		entries = new Entry[size];
	}

	// get entry

	public Entry GetEntry(ulong key)
	{
        int index = (int)(key % (ulong)entries.Length);
		return entries[index];
    }

	// store entry

	public void Store(ulong key, int depth, int value, NodeType nodeType, Move move)
	{
		// just replace 

		int index = (int)(key % (ulong)entries.Length);

		entries[index] = new Entry()
		{
			key = key,
			depth = (byte)depth,
			value = value,
			nodeType = nodeType,
			move = move
		};
	}

	// lookup

	public int Lookup(ulong key, int depth, int alpha, int beta)
	{
		// get the entry

		int index = (int)(key % (ulong)entries.Length);
		Entry entry = entries[index];

		// if the key matches

		if (entry.key == key)
		{
			// if the entry depth is at least equal

			if (entry.depth >= depth)
			{
				switch (entry.nodeType)
				{
					case NodeType.Exact:
						return entry.value;
					case NodeType.LowerBound:
						if (entry.value >= beta)
						{
							return entry.value;
						}
						break;
					case NodeType.UpperBound:
						if (entry.value <= alpha)
						{
							return entry.value;
						}
						break;
				}
			}
		}

		// if not

		return lookupFailed;
	}
}
