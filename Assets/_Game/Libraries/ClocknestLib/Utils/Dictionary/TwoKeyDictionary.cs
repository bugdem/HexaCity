using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClocknestGames.Library.Utils
{
	public class TwoKeyDictionary<TKey1, TKey2, TValue>
	{
		private Dictionary<(TKey1, TKey2), TValue> dictionary = new Dictionary<(TKey1, TKey2), TValue>();

		public void Add(TKey1 key1, TKey2 key2, TValue value)
		{
			dictionary[(key1, key2)] = value;
		}

		public bool TryGetValue(TKey1 key1, TKey2 key2, out TValue value)
		{
			return dictionary.TryGetValue((key1, key2), out value);
		}
	}
}