﻿using System;
using System.Collections;
using System.Linq;
using System.Text;

namespace DicomTypeTranslation.Helpers
{
    /// <summary>
    /// Helper methods for <see cref="Array"/> including equality and representation as strings
    /// </summary>
    public static class ArrayHelperMethods
    {
        /// <summary>
        /// Returns true if the two arrays contain the same elements (using <see cref="FlexibleEquality"/>)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool ArrayEquals(Array a, Array b)
        {
            if (a.Length != b.Length)
                return false;

            for (var i = 0; i < a.Length; i++)
                if (!FlexibleEquality.FlexibleEquals(a.GetValue(i), b.GetValue(i)))
                    return false;

            return true;
        }

        /// <summary>
        /// Returns a string representation of the array suitable for human visualisation
        /// </summary>
        /// <param name="a"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static string AsciiArt(Array a, string prefix = "")
        {
            var sb = new StringBuilder();

            for (var i = 0; i < a.Length; i++)
            {
                sb.Append($"{prefix} [{i}] - ");

                //if run out of values in dictionary 1
                object val = a.GetValue(i) ?? "Null";

                if (DictionaryHelperMethods.IsDictionary(val))
                    sb.AppendLine(string.Format("\r\n {0}",
                        DictionaryHelperMethods.AsciiArt((IDictionary)val, $"{prefix}\t")));
                else if (val is Array)
                    sb.AppendLine(string.Format("\r\n {0}", AsciiArt((Array)val, $"{prefix}\t")));
                else
                    sb.AppendLine(val.ToString());
            }

            return sb.ToString();
        }

        /// <summary>
        /// Returns a string representation of both arrays highlighting differences in array elements
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static string AsciiArt(Array a, Array b, string prefix = "")
        {
            var sb = new StringBuilder();

            for (var i = 0; i < Math.Max(a.Length, b.Length); i++)
            {
                sb.Append($"{prefix} [{i}] - ");

                //if run out of values in dictionary 1
                if (i > a.Length)
                    sb.AppendLine(string.Format(" \t <NULL> \t {0}", b.GetValue(i)));
                //if run out of values in dictionary 2
                else if (i > b.Length)
                    sb.AppendLine(string.Format(" \t {0} \t <NULL>", a.GetValue(i)));
                else
                {
                    object val1 = a.GetValue(i);
                    object val2 = b.GetValue(i);

                    if (DictionaryHelperMethods.IsDictionary(val1) && DictionaryHelperMethods.IsDictionary(val2))
                        sb.Append(string.Format("\r\n {0}",
                            DictionaryHelperMethods.AsciiArt((IDictionary)val1,
                            (IDictionary)val2, $"{prefix}\t")));
                    else
                    if (val1 is Array && val2 is Array)
                        sb.Append(string.Format("\r\n {0}",
                            AsciiArt((Array)val1,
                            (Array)val2, $"{prefix}\t")));
                    else
                        //if we haven't outrun of either array
                        sb.AppendLine(string.Format(" \t {0} \t {1} {2}",
                            val1,
                            val2,
                            FlexibleEquality.FlexibleEquals(val1, val2) ? "" : "<DIFF>"));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Returns true if <paramref name="a"/> contains any elements which are <see cref="Array"/> or <see cref="IDictionary"/>
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        private static bool ContainsSubArraysOrSubtrees(Array a)
        {
            return a.OfType<Array>().Any() || a.OfType<IDictionary>().Any();
        }

        /// <summary>
        /// Separates array elements with backslashes unless the array contains sub arrays or dictionaries in which case it resorts to ASCIIArt
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static string GetStringRepresentation(Array a)
        {
            if (ContainsSubArraysOrSubtrees(a))
                return AsciiArt(a);

            var sb = new StringBuilder();

            for (var i = 0; i < a.Length; i++)
            {
                sb.Append(a.GetValue(i));

                if (i + 1 < a.Length)
                    sb.Append("\\");
            }

            return sb.ToString();
        }
    }
}
