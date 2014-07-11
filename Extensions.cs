using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace Libber
{
    public static class Extensions
    {

        public static string CommaJoin<T>(this IEnumerable<T> obj)
        {
            return String.Join(", ", obj);
        }
    }
}
