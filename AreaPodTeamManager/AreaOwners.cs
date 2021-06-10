using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using static AreaPodTeamManager.AreaOwners;

namespace AreaPodTeamManager
{
    public sealed class AreaOwners : IEnumerable<Area>
    {
        private readonly IEnumerable<Area> _areas;

        public AreaOwners(IEnumerable<Area> areas)
        {
            _areas = areas;
        }

        public static async Task<AreaOwners> GetAsync(string org, string repo)
        {
            var maxTries = 3;
            var url = $"https://raw.githubusercontent.com/{org}/{repo}/main/docs/area-owners.md";

            while (maxTries-- > 0)
            {
                var client = new HttpClient();
                try
                {
                    var contents = await client.GetStringAsync(url);
                    return Parse(contents);
                }
                catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    // In this case we know that the file doesn't exist.
                    return null;
                }
                catch (HttpRequestException ex)
                {
                    // This might be a transient error.
                    Debug.WriteLine(ex);
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            return null;
        }

        private static AreaOwners Parse(string contents)
        {
            var lines = GetLines(contents);
            var entries = new List<Area>();

            foreach (var line in lines)
            {
                var parts = line.Split('|');
                if (parts.Length != 6)
                    continue;

                var areaText = parts[1].Trim();
                var lead = GetUntaggedUserName(parts[2].Trim());
                var ownerText = parts[3].Trim();
                var owners = ownerText.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(o => GetUntaggedUserName(o.Trim()))
                                      .ToArray();

                if (!TextTokenizer.TryParseAreaLabel(areaText, out var areaLabel))
                    continue;

                var entry = new Area(areaLabel, lead, owners);
                entries.Add(entry);
            }

            return new AreaOwners(entries);

            static string GetUntaggedUserName(string userName)
            {
                return userName.StartsWith("@") ? userName[1..] : userName;
            }

            static IEnumerable<string> GetLines(string text)
            {
                using var stringReader = new StringReader(text);
                while (true)
                {
                    var line = stringReader.ReadLine();
                    if (line == null)
                        yield break;

                    yield return line;
                }
            }
        }

        public IEnumerator<Area> GetEnumerator()
        {
            return _areas.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable)_areas).GetEnumerator();
        }

        public sealed class Area
        {
            public Area(string areaLabel, string lead, IReadOnlyList<string> owners)
            {
                AreaLabel = areaLabel;
                Lead = lead;
                Owners = owners;
            }

            public string AreaLabel { get; }
            public string Lead { get; }
            public IReadOnlyList<string> Owners { get; }
        }
    }
}