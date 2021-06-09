using System;
using System.Diagnostics;
using System.Linq;

namespace AreaPodTeamManager
{
    class Program
    {
        static void Main(string[] args)
        {
            Debugger.Launch();
            Debugger.Break();

            var team = new Team("area-System.IO.Compression", new[] { "jeffhandley" }, new[] { "adamsitnik", "carlossanlop", "Jozkee" });

            var gh = new GitHub(args[0]);
            team = gh.CreateTeam(team);

            Console.WriteLine($"Created {team.Name} ({team.Slug}) with {team.Members.Count()} members");
        }
    }
}
