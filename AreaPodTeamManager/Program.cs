using System;
using System.Diagnostics;
using System.Linq;

namespace AreaPodTeamManager
{
    class Program
    {
        static void Main(string[] args)
        {
            var lead = args[0];
            var authToken = args[1];

            var areas = AreaOwners.GetAsync("dotnet", "runtime").Result;
            var leadAreas = areas.Where(a => a.Lead == lead);
            var gh = new GitHub(authToken);

            foreach (var area in leadAreas)
            {
                var team = new Team(area.AreaLabel, new[] { lead }, area.Owners);
                team = gh.CreateTeam(team);

                Console.WriteLine($"Created {team.Name} ({team.Slug}) with {team.Members.Count()} members");
            }
        }
    }
}
