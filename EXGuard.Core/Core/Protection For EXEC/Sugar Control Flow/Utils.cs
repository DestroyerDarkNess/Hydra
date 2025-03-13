using System;
using System.Collections.Generic;

namespace SugarGuard.Protector.Protections.ControlFlow
{
	public static class Utils
	{
		public static void AddListEntry<TKey, TValue>(this IDictionary<TKey, List<TValue>> self, TKey key, TValue value)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			List<TValue> list;
			if (!self.TryGetValue(key, out list))
				list = self[key] = new List<TValue>();
			list.Add(value);
		}

		public static IList<T> RemoveWhere<T>(this IList<T> self, Predicate<T> match)
		{
			for (int i = self.Count - 1; i >= 0; i--)
			{
				if (match(self[i]))
					self.RemoveAt(i);
			}
			return self;
		}
	}
}
