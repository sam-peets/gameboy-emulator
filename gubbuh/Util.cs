using System.Collections.Generic;
using System.Linq;

namespace gubbuh
{
    public static class Util
    {
        public static T[] Subsequence<T>(this IEnumerable<T> arr,int startIndex, int length)
        {
            return arr.Skip(startIndex).Take(length).ToArray();
        }
    }
}