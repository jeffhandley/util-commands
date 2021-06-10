using System;
using System.Diagnostics;
using System.Linq;

namespace AreaPodTeamManager
{
    class Program
    {
        static void Main(string[] args)
        {
            string lead = null;
            bool removeOldMembers = false;
            string authToken = null;

            void ShowCommandSyntax()
            {
                Console.WriteLine();
                Console.WriteLine("Syntax: dotnet run --lead|-l <lead> --token|-t <auth_token> --removeOldMembers");
                Console.WriteLine();
                Environment.Exit(1);
            }

            for (var a = 0; a < args.Length; a++)
            {
                var arg = args[a];

                string GetArgumentValue()
                {
                    if (a == args.Length - 1) ShowCommandSyntax();
                    return args[++a];
                }

                switch (arg.ToLowerInvariant())
                {
                    case "--lead":
                    case "-l":
                        lead = GetArgumentValue();
                        break;

                    case "--token":
                    case "-t":
                        authToken = GetArgumentValue();
                        break;

                    case "--removeoldmembers":
                        removeOldMembers = true;
                        break;

                    default:
                        GetArgumentValue();
                        break;
                }
            }

            if (lead is null || authToken is null)
            {
                ShowCommandSyntax();
            }

            var areas = AreaOwners.GetAsync("dotnet", "runtime").Result;
            var leadAreas = areas.Where(a => a.Lead == lead);
            var gh = new GitHub(authToken);

            foreach (var area in leadAreas)
            {
                var team = new Team(area.AreaLabel, new[] { lead }, area.Owners);
                gh.CreateTeam(team, removeOldMembers);
            }
        }
    }
}
