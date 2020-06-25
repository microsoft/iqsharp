// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text.RegularExpressions;

namespace Microsoft.Quantum.IQSharp
{
    public static partial class Extensions
    {
        /// <summary>
        ///      Removes common indents from each line in a string,
        ///      similarly to Python's <c>textwrap.dedent()</c> function.
        /// </summary>
        public static string Dedent(this string text)
        {
            // First, start by finding the length of common indents,
            // disregarding lines that are only whitespace.
            var leadingWhitespaceRegex = new Regex(@"^[ \t]*");
            var minWhitespace = int.MaxValue;
            foreach (var line in text.Split("\n"))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    var match = leadingWhitespaceRegex.Match(line);
                    minWhitespace = match.Success
                                ? System.Math.Min(minWhitespace, match.Value.Length)
                                : minWhitespace = 0;
                }
            }

            // We can use that to build a new regex that strips
            // out common indenting.
            var leftTrimRegex = new Regex(@$"^[ \t]{{{minWhitespace}}}", RegexOptions.Multiline);
            return leftTrimRegex.Replace(text, "");
        }
    }
}
