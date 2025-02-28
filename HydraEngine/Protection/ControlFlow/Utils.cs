using System;
using System.Collections.Generic;

namespace HydraEngine.Protection.ControlFlow
{
    public static class Utils
    {
        public static void AddListEntry<TKey, TValue>(this IDictionary<TKey, List<TValue>> self, TKey key, TValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }
            if (!self.TryGetValue(key, out var value2))
            {
                List<TValue> list2 = (self[key] = new List<TValue>());
                value2 = list2;
            }
            value2.Add(value);
        }

        public static IList<T> RemoveWhere<T>(this IList<T> self, Predicate<T> match)
        {
            for (int num = self.Count - 1; num >= 0; num--)
            {
                if (match(self[num]))
                {
                    self.RemoveAt(num);
                }
            }
            return self;
        }
    }
}

