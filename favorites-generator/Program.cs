using System;
using System.Collections.Generic;

namespace favorites_generator
{
    class Program
    {
        private const string FavoriteTemplate = @"<DT><A HREF=""{1}"">{0}</A>";
        private const string FolderTemplate = @"<DT><H3>{0}</H3>
<DL><p>
{1}
</DL><p>";

        private const string FileTemplate = @"<!DOCTYPE NETSCAPE-Bookmark-file-1>
<META HTTP-EQUIV=""Content-Type"" CONTENT=""text/html; charset=UTF-8"">
<TITLE>Bookmarks</TITLE>
<H1>Bookmarks</H1>
<DL><p>
<DT><H3 PERSONAL_TOOLBAR_FOLDER=""true"">Favorites bar</H3>
<DL><p>
{0}
</DL><p>
</DL><p>
";

        private static readonly Dictionary<string, string> UrlTemplates = new()
        {
            { "stale", "https://github.com/dotnet/runtime/pulls?q=is%3Apr+is%3Aopen+label%3Aarea-{0}+sort%3Aupdated-asc+-is:draft" },
            { "untriaged", "https://github.com/dotnet/runtime/issues?q=is%3Aopen+is%3Aissue+label%3Auntriaged+label%3Aarea-{0}+sort%3Aupdated-asc" },
            { "needs-further-triage", "https://github.com/dotnet/runtime/issues?q=is%3Aopen+is%3Aissue+label%3A\"needs%20further%20triage\"+label%3Aarea-{0}+sort%3Aupdated-asc" },
            { "no-milestone", "https://github.com/dotnet/runtime/issues?q=is%3Aopen+is%3Aissue+no%3Amilestone+-label%3Auntriaged+label%3Aarea-{0}+sort%3Aupdated-asc" },
            { "6.0.0-issues", "https://github.com/dotnet/runtime/issues?q=is%3Aopen+is%3Aissue+milestone%3A6.0.0+label%3Aarea-{0}+sort%3Aupdated-asc" },
            { "6.0.0-pulls", "https://github.com/dotnet/runtime/pulls?q=is%3Aopen+is%3Apr+milestone%3A6.0.0+label%3Aarea-{0}+sort%3Aupdated-asc" },
            { "issues", "https://github.com/dotnet/runtime/issues?q=is%3Aopen+is%3Aissue+label%3Aarea-{0}+sort%3Aupdated-asc+" }
        };

        private static readonly Dictionary<string, string> CommonFolders = new()
        {
            { "stale", "issues / prs,Pulls (stale)," },
            { "untriaged", "issues / prs,Issues (untriaged)," },
            { "needs-further-triage", "issues / prs,Issues (needs further triage)," },
            { "no-milestone", "issues / prs,Issues (no milestone)," },
            { "6.0.0-issues", "issues / prs,Issues (6.0.0)," },
            { "6.0.0-pulls", "issues / prs,Pulls (6.0.0)," },
            { "issues", "issues / prs,Issues (all)," }
        };

        private static readonly Dictionary<string, string> PodMembers = new()
        {
            { "adam", "Adam / Carlos / David" },
            { "carlos", "Adam / Carlos / David" },
            { "david", "Adam / Carlos / David" },
            { "buyaa", "Buyaa / Jose / Krzysztof" },
            { "krzysztof", "Buyaa / Jose / Krzysztof" },
            { "eirik", "Eirik / Layomi" },
            { "jeremy", "Jeremy / Levi" },
            { "levi", "Jeremy / Levi" },
            { "prashanth", "Prashanth / Tanner" },
            { "tanner", "Prashanth / Tanner" }
        };

        private static readonly Dictionary<string, string> AreaPods = new()
        {
            { "Adam / Carlos / David", "Extensions-FileSystem,System.Console,System.Diagnostics.Process,System.IO,System.IO.Compression,System.Linq.Parallel,System.Memory,System.Threading.Channels,System.Threading.Tasks" },
            { "Buyaa / Jose / Krzysztof", "Meta,System.CodeDom,System.ComponentModel.Composition,System.Composition,System.Configuration,System.DirectoryServices,System.Reflection,System.Reflection.Emit,System.Reflection.Metadata,System.Resources,System.Xml" },
            { "Eirik / Layomi", "System.Collections,System.Formats.Cbor,System.Linq,System.Text.Encoding,System.Text.Encodings.Web,System.Text.Json" },
            { "Jeremy / Levi", "System.Formats.Asn1,System.Security,PAL-libraries" },
            { "Prashanth / Tanner", "System.Buffers,System.Numerics,System.Numerics.Tensors,System.Runtime,System.Runtime.Intrinsics,System.Text.RegularExpressions" },
        };

        static void Main(string[] args)
        {
            string[][] combos = new string[][]
            {
                new string[] { "stale", "adam" },
                new string[] { "untriaged", "adam" },
                new string[] { "needs-further-triage", "adam" },
                new string[] { "no-milestone", "adam" },
                new string[] { "6.0.0-issues", "adam" },
                new string[] { "6.0.0-pulls", "adam" },
                new string[] { "issues", "adam" },

                new string[] { "stale", "buyaa" },
                new string[] { "untriaged", "buyaa" },
                new string[] { "needs-further-triage", "buyaa" },
                new string[] { "no-milestone", "buyaa" },
                new string[] { "6.0.0-issues", "buyaa" },
                new string[] { "6.0.0-pulls", "buyaa" },
                new string[] { "issues", "buyaa" },

                new string[] { "stale", "eirik" },
                new string[] { "untriaged", "eirik" },
                new string[] { "needs-further-triage", "eirik" },
                new string[] { "no-milestone", "eirik" },
                new string[] { "6.0.0-issues", "eirik" },
                new string[] { "6.0.0-pulls", "eirik" },
                new string[] { "issues", "eirik" },

                new string[] { "stale", "jeremy" },
                new string[] { "untriaged", "jeremy" },
                new string[] { "needs-further-triage", "jeremy" },
                new string[] { "no-milestone", "jeremy" },
                new string[] { "6.0.0-issues", "jeremy" },
                new string[] { "6.0.0-pulls", "jeremy" },
                new string[] { "issues", "jeremy" },

                new string[] { "stale", "prashanth" },
                new string[] { "untriaged", "prashanth" },
                new string[] { "needs-further-triage", "prashanth" },
                new string[] { "no-milestone", "prashanth" },
                new string[] { "6.0.0-issues", "prashanth" },
                new string[] { "6.0.0-pulls", "prashanth" },
                new string[] { "issues", "prashanth" },
            };

            List<string> comboFavorites = new();

            foreach (var combo in combos)
            {
                string favorites = GetFavorites(combo);

                if (favorites is not null)
                {
                    comboFavorites.Add(favorites);
                }
            }

            string file = String.Format(FileTemplate, String.Join(Environment.NewLine, comboFavorites));
            Console.WriteLine(file);
        }

        static string GetFavorites(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Syntax: dotnet run <urlTemplate> <folder(s)> <areas>");
                return null;
            }

            string urlTemplate = args[0];
            string folderNames = args[1];
            string areas;

            if (PodMembers.ContainsKey(folderNames))
            {
                folderNames = PodMembers[folderNames];
            }

            if (args.Length < 3)
            {
                if (AreaPods.ContainsKey(folderNames))
                {
                    areas = AreaPods[folderNames];
                }
                else
                {
                    Console.WriteLine("Either an area pod (member) or a list of areas needs to be specified");
                    return null;
                }
            }
            else
            {
                areas = String.Join(",", args[2..]);
            }

            if (UrlTemplates.ContainsKey(urlTemplate))
            {
                folderNames = CommonFolders[urlTemplate] + folderNames;
                urlTemplate = UrlTemplates[urlTemplate];
            }

            if (!urlTemplate.Contains("{0}"))
            {
                Console.WriteLine("The urlTemplate argument must have a `{0}` area placeholder.");
                return null;
            }

            string[] folders = folderNames.Split(",");
            List<string> areaLinks = new();

            foreach (var area in areas.Split(","))
            {
                string areaUrl = String.Format(urlTemplate, area);
                string areaLink = String.Format(FavoriteTemplate, area, areaUrl);

                areaLinks.Add(areaLink);
            }

            string favorites = String.Join(Environment.NewLine, areaLinks);

            for (var f = folders.Length - 1; f >= 0; f--)
            {
                favorites = String.Format(FolderTemplate, folders[f], favorites);
            }

            return favorites;
        }
    }
}
