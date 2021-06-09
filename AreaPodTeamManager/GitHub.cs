using RestSharp;
using System;
using System.Collections.Generic;
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

        public Team CreateTeam(Team team)
        {
            var existingTeam = ExistingTeams.SingleOrDefault(t => t.Name.ToLowerInvariant() == team.Name.ToLowerInvariant());

            if (existingTeam is not null)
            {
                team = team with { Slug = existingTeam.Slug };
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
            }

            var membership = AddTeamMembers(team);

            if (membership.Any(m => !m.IsSuccessful))
            {
                throw new ApplicationException($"Members could not be added to {team.Name} ({team.Slug}): {string.Join(',', membership.Where(m => !m.IsSuccessful).Select(m => m.Member))}");
            }

            return team;
        }

        public IEnumerable<(string Member, bool IsSuccessful)> AddTeamMembers(Team team)
        {
            if (team.Slug is null)
            {
                throw new InvalidOperationException($"Team Slug cannot be null. Team Name: {team.Name}");
            }

            var responses = new List<(string Member, bool IsSuccessful)>();

            var request = new RestRequest(Method.PUT);
            request.AddHeader("Authorization", "Bearer " + _authToken);

            foreach (var member in team.Members)
            {
                var url = $"{ApiTeams}/{team.Slug}/memberships/{member}";
                var client = new RestClient(url) { Timeout = -1 };
                var response = client.Execute(request);

                responses.Add((member, response.IsSuccessful));
            }

            return responses;
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
