using System;
using System.Linq;
using MoreLinq;
using WPFComputationalGeometry.Source.Common.Extensions.Collections;

namespace WPFComputationalGeometry.Source.Common.Extensions
{
    public static class StringExtensions
    {
        public static string Remove(this string str, string substring) => str.Replace(substring, "");

        public static string AfterFirst(this string str, string substring)
        {
            if (!string.IsNullOrEmpty(substring) && str.Contains(substring))
            {
                var split = str.Split(substring);
                if (str.StartsWith(substring))
                    split = new[] { "" }.Concat(split).ToArray();
                return split.Skip(1).JoinAsString(substring);
            }
            return str;
        }

        public static string BeforeFirst(this string str, string substring)
        {
            if (!string.IsNullOrEmpty(substring) && str.Contains(substring))
                return str.Split(substring).First();
            return str;
        }

        public static string[] Split(this string str, string separator, bool includeSeparator = false)
        {
            var split = str.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);

            if (includeSeparator)
            {
                var splitWithSeparator = new string[split.Length + split.Length - 1];
                var j = 0;
                for (var i = 0; i < splitWithSeparator.Length; i++)
                {
                    if (i % 2 == 1)
                        splitWithSeparator[i] = separator;
                    else
                        splitWithSeparator[i] = split[j++];
                }
                split = splitWithSeparator;
            }
            return split;
        }

        public static string Take(this string str, int n)
        {
            return new string(str.AsEnumerable().Take(n).ToArray());
        }

        public static string Take(this string str, uint u) => str.Take((int)u);
        public static string Take(this string str, long l) => str.Take((int)l);

        public static string Skip(this string str, int n)
        {
            return new string(str.AsEnumerable().Skip(n).ToArray());
        }

        public static string TakeLast(this string str, int n)
        {
            return new string(str.AsEnumerable().TakeLast(n).ToArray());
        }

        public static string SkipLast(this string str, int n)
        {
            return new string(str.AsEnumerable().SkipLast(n).ToArray());
        }

        public static bool IsNullOrWhiteSpace(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }
    }
}
