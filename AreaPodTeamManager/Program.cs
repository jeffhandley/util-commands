using System;
using System.Collections.Generic;
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
            bool dryrun = false;
            List<string> includeAreas = new();
            List<string> excludeAreas = new();
            List<string> addMembers = new();
            List<string> addRepositories = new();

            void ShowCommandSyntax()
            {
                Console.WriteLine();
                Console.WriteLine("Usage:");
                Console.WriteLine("    dotnet run --includeArea <area> --excludeArea <area> --lead <lead> --token <auth_token> [--removeOldMembers]");
                Console.WriteLine("    dotnet run --includeArea <area> --excludeArea <area> --lead <lead> --token <auth_token> --addMember <member1> --addMember <member2>");
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
                        lead = GetArgumentValue();
                        break;

                    case "--token":
                        authToken = GetArgumentValue();
                        break;

                    case "--removeoldmembers":
                        if (addMembers.Any()) ShowCommandSyntax();
                        removeOldMembers = true;
                        break;

                    case "--includearea":
                        includeAreas.Add(GetArgumentValue());
                        break;

                    case "--excludearea":
                        excludeAreas.Add(GetArgumentValue());
                        break;

                    case "--addmember":
                        if (removeOldMembers) ShowCommandSyntax();
                        addMembers.Add(GetArgumentValue());
                        break;

                    case "--addrepository":
                        addRepositories.Add(GetArgumentValue());
                        break;

                    case "--dryrun":
                        dryrun = true;
                        break;

                    default:
                        ShowCommandSyntax();
                        break;
                }
            }

            if (authToken is null || (lead is null && includeAreas is null))
            {
                ShowCommandSyntax();
            }

            var areas = AreaOwners.GetAsync("dotnet", "runtime").Result.AsEnumerable();

            if (lead is not null)
            {
                areas = areas.Where(a => a.Lead.ToLowerInvariant() == lead.ToLowerInvariant());
            }

            if (includeAreas.Any())
            {
                areas = areas.Where(a => includeAreas.Any(included => a.AreaLabel.Contains(included, StringComparison.InvariantCultureIgnoreCase)));
            }

            if (excludeAreas.Any())
            {
                areas = areas.Where(a => !excludeAreas.Any(excluded => a.AreaLabel.Contains(excluded, StringComparison.InvariantCultureIgnoreCase)));
            }

            var gh = new GitHub(authToken);

            foreach (var area in areas)
            {
                var team = new Team(area.AreaLabel, new[] { lead }, area.Owners);
                team = gh.PopulateTeamSlug(team);

                if (team.Slug is null)
                {
                    team = gh.CreateTeam(team, removeOldMembers, dryrun);
                }
                else if (addMembers.Any())
                {
                    bool hadError = false;

                    foreach (var member in addMembers)
                    {
                        if (!gh.AddTeamMember(team, member, dryrun))
                        {
                            hadError = true;
                            break;
                        }
                    }

                    if (!hadError)
                    {
                        Console.WriteLine($"Added members to {team.Name} ({team.Slug}): {string.Join(',', addMembers)}");
                    }
                    else
                    {
                        Console.WriteLine($"Could not add members to {team.Name} ({team.Slug}). You must be a team maintainer.");
                    }
                }

                if (addRepositories.Any())
                {
                    bool hadError = false;

                    foreach (var repo in addRepositories)
                    {
                        if (!gh.AddTeamRepository(team, repo, dryrun))
                        {
                            hadError = true;
                            break;
                        }
                    }

                    if (!hadError)
                    {
                        Console.WriteLine($"Added repositories to {team.Name} ({team.Slug}): {string.Join(',', addRepositories)}");
                    }
                    else
                    {
                        Console.WriteLine($"Could not add repositories to {team.Name} ({team.Slug}). You must be a repository administrator.");
                    }
                }
            }
        }
    }
}
