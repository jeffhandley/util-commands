﻿using System;
using System.Collections.Generic;

using Humanizer;

namespace AreaPodTeamManager
{
    public static class TextTokenizer
    {
        public static IEnumerable<string> Tokenize(string text)
        {
            var result = new SortedSet<string>();

            foreach (var token in SplitByPunctuation(text))
            {
                var singular = token.Singularize();
                result.Add(singular.ToLowerInvariant());

                foreach (var nestedToken in SplitByCaseChanges(token))
                    result.Add(nestedToken.ToLowerInvariant());
            }

            result.RemoveWhere(x => x.Length < 2);

            return result;
        }

        private static IEnumerable<string> SplitByPunctuation(string token)
        {
            var position = 0;
            var start = -1;

            while (position < token.Length)
            {
                var c = token[position];

                if (char.IsLetterOrDigit(c))
                {
                    if (start < 0)
                        start = position;
                    position++;
                }
                else
                {
                    if (start >= 0)
                        yield return token[start..position];

                    start = -1;
                    position++;
                }
            }

            if (start >= 0)
                yield return token[start..];
        }

        private static IEnumerable<string> SplitByCaseChanges(string token)
        {
            var position = 0;
            var start = 0;

            while (position < token.Length)
            {
                var c = token[position];
                var l = position < token.Length - 1 ? token[position + 1] : '\0';
                var nextCharacterChangesToUpperCase = char.IsLower(c) && char.IsUpper(l);
                var wordHasStarted = start > 0;
                var isLastCharacter = l == '\0';
                var wordHasEnded = nextCharacterChangesToUpperCase ||
                                   wordHasStarted && isLastCharacter;
                position++;

                if (wordHasEnded)
                {
                    yield return token[start..position];
                    start = position;
                }
            }
        }

        public static IReadOnlyList<string> GetAreaPaths(string label, bool segmentsOnly = false)
        {
            const string prefix = "area-";
            if (!label.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return Array.Empty<string>();

            var result = new List<string>();
            var remainder = label.Substring(prefix.Length);
            var wasSeparator = true;
            var start = 0;

            for (var i = 0; i < remainder.Length; i++)
            {
                var c = remainder[i];
                var isAreaPathText = char.IsLetterOrDigit(c) ||
                                     char.IsWhiteSpace(c);

                if (isAreaPathText)
                {
                    if (wasSeparator)
                    {
                        wasSeparator = false;
                        if (segmentsOnly)
                            start = i;
                    }
                }
                else if (!wasSeparator)
                {
                    result.Add(remainder[start..i]);
                    wasSeparator = true;
                }
            }

            result.Add(remainder[start..]);
            return result;
        }

        public static bool TryParseAreaLabel(string label, out string areaLabel)
        {
            const string AreaPrefix = "area-";

            if (label is not null && label.StartsWith(AreaPrefix, StringComparison.OrdinalIgnoreCase))
            {
                areaLabel = label;
                return true;
            }

            areaLabel = null;
            return false;
        }
    }
}