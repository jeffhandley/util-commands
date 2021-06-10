using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AreaPodTeamManager
{
    class GitHub
    {
        private const string ApiTeams = "https://api.github.com/orgs/dotnet/teams";
        private string _authToken;
        private IEnumerable<Team> _existingTeams;

        public GitHub(string authToken)
        {
            _authToken = authToken;
        }

        public IEnumerable<Team> ExistingTeams
        {
            get
            {
                if (_existingTeams is null)
                {
                    _existingTeams = GetExistingTeams();
                }

                return _existingTeams;
            }
        }

        private string GetTeamSlugFromTeamResponse(string teamResponse)
        {
            var createdTeam = JsonDocument.Parse(teamResponse);
            return createdTeam.RootElement.GetProperty("slug").GetString();
        }

        private IEnumerable<Team> ParseTeams(string teamsResponse)
        {
            var teams = JsonDocument.Parse(teamsResponse);
            foreach (var team in teams.RootElement.EnumerateArray())
            {
                var name = team.GetProperty("name").GetString();
                var slug = team.GetProperty("slug").GetString();

                yield return new Team(name, slug);
            }
        }

        public IEnumerable<Team> GetExistingTeams()
        {
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", "Bearer " + _authToken);

            var teams = new List<Team>();

            for (var page = 1; ; page++)
            {
                var url = $"{ApiTeams}?per_page=100&page={page}";
                var client = new RestClient(url) { Timeout = -1 };
                var response = client.Execute(request);

                if (!response.IsSuccessful)
                {
                    throw new ApplicationException(response.ErrorMessage);
                }

                var teamsOnPage = ParseTeams(response.Content);

                if (!teamsOnPage.Any())
                {
                    return teams;
                }

                teams.AddRange(teamsOnPage);
            }
        }

        public IEnumerable<string> GetExistingTeamMembers(Team team)
        {
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", "Bearer " + _authToken);

            var url = $"{ApiTeams}/{team.Slug}/members";
            var client = new RestClient(url) { Timeout = -1 };
            var response = client.Execute(request);

            if (!response.IsSuccessful)
            {
                throw new ApplicationException(response.ErrorMessage);
            }

            var members = JsonDocument.Parse(response.Content).RootElement;

            foreach (var member in members.EnumerateArray())
            {
                yield return member.GetProperty("login").GetString();
            }
        }

        public Team PopulateTeamSlug(Team team)
        {
            var existingTeam = ExistingTeams.SingleOrDefault(t => t.Name.ToLowerInvariant() == team.Name.ToLowerInvariant());

            if (existingTeam is not null)
            {
                team = team with { Slug = existingTeam.Slug };
            }

            return team;
        }

        public Team CreateTeam(Team team, bool removeOldMembers = false)
        {
            team = PopulateTeamSlug(team);
            var members = Enumerable.Empty<string>();

            if (team.Slug is not null)
            {
                members = GetExistingTeamMembers(team);
                Console.WriteLine($"Team {team.Name} ({team.Slug}) already exists.");
            }
            else
            {
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", "Bearer " + _authToken);
                request.AddHeader("Content-Type", "application/json");
                request.AddJsonBody(new
                {
                    name = team.Name,
                    description = team.Description,
                    maintainers = team.Maintainers.ToArray(),
                    privacy = "closed"
                });

                var client = new RestClient(ApiTeams) { Timeout = -1 };
                var response = client.Execute(request);

                if (!response.IsSuccessful)
                {
                    throw new ApplicationException(response.ErrorMessage);
                }

                var slug = GetTeamSlugFromTeamResponse(response.Content);
                team = team with { Slug = slug };

                Console.WriteLine($"Team {team.Name} ({team.Slug}) was created. Maintainers: {string.Join(',', team.Maintainers)}");
            }

            var membersAdded = new List<string>();

            foreach(var member in team.Members)
            {
                if (!members.Contains(member, StringComparer.OrdinalIgnoreCase))
                {
                    if (!AddTeamMember(team, member))
                    {
                        Console.WriteLine($"Could not add members to {team.Name} ({team.Slug}). You must be a team maintainer.");
                        break;
                    }

                    membersAdded.Add(member);
                }
            }

            if (membersAdded.Any())
            {
                Console.WriteLine($"Added members to {team.Name} ({team.Slug}): {string.Join(',', membersAdded)}");
            }

            if (removeOldMembers)
            {
                var membersRemoved = new List<string>();

                foreach (var member in members)
                {
                    if (!team.Members.Contains(member, StringComparer.OrdinalIgnoreCase) && !team.Maintainers.Contains(member, StringComparer.OrdinalIgnoreCase))
                    {
                        RemoveTeamMember(team, member);
                        membersRemoved.Add(member);
                    }
                }

                if (membersRemoved.Any())
                {
                    Console.WriteLine($"Removed members from {team.Name} ({team.Slug}): {string.Join(',', membersRemoved)}");
                }
            }

            return team;
        }

        public bool AddTeamMember(Team team, string member)
        {
            if (team.Slug is null)
            {
                throw new InvalidOperationException($"Team Slug cannot be null. Team Name: {team.Name}");
            }

            var request = new RestRequest(Method.PUT);
            request.AddHeader("Authorization", "Bearer " + _authToken);

            var url = $"{ApiTeams}/{team.Slug}/memberships/{member}";
            var client = new RestClient(url) { Timeout = -1 };
            var response = client.Execute(request);

            return response.IsSuccessful;
        }

        public void RemoveTeamMember(Team team, string member)
        {
            if (team.Slug is null)
            {
                throw new InvalidOperationException($"Team Slug cannot be null. Team Name: {team.Name}");
            }

            var request = new RestRequest(Method.DELETE);
            request.AddHeader("Authorization", "Bearer " + _authToken);

            var url = $"{ApiTeams}/{team.Slug}/memberships/{member}";
            var client = new RestClient(url) { Timeout = -1 };
            var response = client.Execute(request);

            if (!response.IsSuccessful)
            {
                throw new ApplicationException($"Could not remove {member} from {team.Name} ({team.Slug}).");
            }
        }
    }

    record Team(string Name, IEnumerable<string> Maintainers, IEnumerable<string> Members)
    {
        public string Slug { get; set; }

        public string Description { get => $"Area owners for {Name}"; }

        public Team(string name, string slug) : this(name, Array.Empty<string>(), Array.Empty<string>())
        {
            Slug = slug;
        }
    }
}
