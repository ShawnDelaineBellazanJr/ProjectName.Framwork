using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GitHubProjectManager.ApiService.Models;
using Microsoft.Extensions.Caching.Memory;

namespace GitHubProjectManager.ApiService.Services.GitHub;

/// <summary>
/// Service for interacting with the GitHub API
/// </summary>
public class GitHubService : IGitHubService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GitHubService> _logger;
    private readonly IConfiguration _configuration;

    public GitHubService(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<GitHubService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _configuration = configuration;
        
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        _httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("GitHubProjectManager", "1.0"));
        
        _httpClient.BaseAddress = new Uri("https://api.github.com/");
    }

    /// <summary>
    /// Set the authentication token for GitHub API requests
    /// </summary>
    public void SetAuthToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Get the authenticated user's information
    /// </summary>
    public async Task<GitHubUser?> GetUserAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("user", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            return await JsonSerializer.DeserializeAsync<GitHubUser>(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GitHub user");
            return null;
        }
    }

    /// <summary>
    /// Get repositories for the authenticated user
    /// </summary>
    public async Task<List<GitHubRepository>> GetRepositoriesAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "user_repositories";
        
        if (_cache.TryGetValue(cacheKey, out List<GitHubRepository>? cachedRepos) && cachedRepos != null)
        {
            return cachedRepos;
        }
        
        try
        {
            var response = await _httpClient.GetAsync("user/repos?sort=updated&per_page=100", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var repos = await JsonSerializer.DeserializeAsync<List<GitHubRepository>>(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken) ?? new List<GitHubRepository>();
            
            _cache.Set(cacheKey, repos, TimeSpan.FromMinutes(10));
            return repos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GitHub repositories");
            return new List<GitHubRepository>();
        }
    }

    /// <summary>
    /// Get issues for a specific repository
    /// </summary>
    public async Task<List<GitHubIssue>> GetIssuesAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"issues_{owner}_{repo}";
        
        if (_cache.TryGetValue(cacheKey, out List<GitHubIssue>? cachedIssues) && cachedIssues != null)
        {
            return cachedIssues;
        }
        
        try
        {
            var response = await _httpClient.GetAsync($"repos/{owner}/{repo}/issues?state=open&sort=updated&per_page=100", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var issues = await JsonSerializer.DeserializeAsync<List<GitHubIssue>>(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken) ?? new List<GitHubIssue>();
            
            _cache.Set(cacheKey, issues, TimeSpan.FromMinutes(5));
            return issues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GitHub issues for {Owner}/{Repo}", owner, repo);
            return new List<GitHubIssue>();
        }
    }

    /// <summary>
    /// Get a specific issue
    /// </summary>
    public async Task<GitHubIssue?> GetIssueAsync(string owner, string repo, int issueNumber, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"issue_{owner}_{repo}_{issueNumber}";
        
        if (_cache.TryGetValue(cacheKey, out GitHubIssue? cachedIssue) && cachedIssue != null)
        {
            return cachedIssue;
        }
        
        try
        {
            var response = await _httpClient.GetAsync($"repos/{owner}/{repo}/issues/{issueNumber}", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var issue = await JsonSerializer.DeserializeAsync<GitHubIssue>(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);
            
            if (issue != null)
            {
                _cache.Set(cacheKey, issue, TimeSpan.FromMinutes(5));
            }
            
            return issue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GitHub issue {IssueNumber} for {Owner}/{Repo}", issueNumber, owner, repo);
            return null;
        }
    }

    /// <summary>
    /// Create a new issue
    /// </summary>
    public async Task<GitHubIssue?> CreateIssueAsync(string owner, string repo, NewIssueRequest issueRequest, CancellationToken cancellationToken = default)
    {
        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(issueRequest),
                Encoding.UTF8,
                "application/json");
            
            var response = await _httpClient.PostAsync($"repos/{owner}/{repo}/issues", content, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            return await JsonSerializer.DeserializeAsync<GitHubIssue>(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating GitHub issue for {Owner}/{Repo}", owner, repo);
            return null;
        }
    }

    /// <summary>
    /// Update an issue
    /// </summary>
    public async Task<GitHubIssue?> UpdateIssueAsync(string owner, string repo, int issueNumber, UpdateIssueRequest updateRequest, CancellationToken cancellationToken = default)
    {
        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(updateRequest),
                Encoding.UTF8,
                "application/json");
            
            var response = await _httpClient.PatchAsync($"repos/{owner}/{repo}/issues/{issueNumber}", content, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var updatedIssue = await JsonSerializer.DeserializeAsync<GitHubIssue>(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);
            
            // Invalidate cache
            if (updatedIssue != null)
            {
                var cacheKey = $"issue_{owner}_{repo}_{issueNumber}";
                _cache.Remove(cacheKey);
                
                var issuesCacheKey = $"issues_{owner}_{repo}";
                _cache.Remove(issuesCacheKey);
            }
            
            return updatedIssue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating GitHub issue {IssueNumber} for {Owner}/{Repo}", issueNumber, owner, repo);
            return null;
        }
    }

    /// <summary>
    /// Get project details
    /// </summary>
    public async Task<GitHubProject?> GetProjectAsync(string owner, string projectNumber, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"project_{owner}_{projectNumber}";
        
        if (_cache.TryGetValue(cacheKey, out GitHubProject? cachedProject) && cachedProject != null)
        {
            return cachedProject;
        }
        
        try
        {
            // Note: This uses the GraphQL API as the REST API for projects is deprecated
            var query = @"
            query($owner: String!, $number: Int!) {
                user(login: $owner) {
                    projectV2(number: $number) {
                        id
                        title
                        number
                        shortDescription
                        url
                        closed
                        items(first: 100) {
                            nodes {
                                id
                                content {
                                    ... on Issue {
                                        id
                                        number
                                        title
                                    }
                                    ... on PullRequest {
                                        id
                                        number
                                        title
                                    }
                                }
                            }
                        }
                    }
                }
            }";
            
            var variables = new
            {
                owner,
                number = int.Parse(projectNumber)
            };
            
            var graphQLRequest = new
            {
                query,
                variables
            };
            
            var content = new StringContent(
                JsonSerializer.Serialize(graphQLRequest),
                Encoding.UTF8,
                "application/json");
            
            // GraphQL API has a different endpoint
            var graphQLClient = new HttpClient();
            graphQLClient.DefaultRequestHeaders.Authorization = _httpClient.DefaultRequestHeaders.Authorization;
            graphQLClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            graphQLClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GitHubProjectManager", "1.0"));
            
            var response = await graphQLClient.PostAsync("https://api.github.com/graphql", content, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);
            
            var projectData = responseJson.RootElement.GetProperty("data").GetProperty("user").GetProperty("projectV2");
            
            var project = new GitHubProject
            {
                Id = projectData.GetProperty("id").GetString() ?? string.Empty,
                Title = projectData.GetProperty("title").GetString() ?? string.Empty,
                Number = projectData.GetProperty("number").GetInt32(),
                Description = projectData.GetProperty("shortDescription").GetString() ?? string.Empty,
                Url = projectData.GetProperty("url").GetString() ?? string.Empty,
                Closed = projectData.GetProperty("closed").GetBoolean(),
                Items = new List<GitHubProjectItem>()
            };
            
            if (projectData.GetProperty("items").GetProperty("nodes").ValueKind == JsonValueKind.Array)
            {
                foreach (var item in projectData.GetProperty("items").GetProperty("nodes").EnumerateArray())
                {
                    if (item.GetProperty("content").ValueKind != JsonValueKind.Null)
                    {
                        var content = item.GetProperty("content");
                        project.Items.Add(new GitHubProjectItem
                        {
                            Id = item.GetProperty("id").GetString() ?? string.Empty,
                            ContentId = content.GetProperty("id").GetString() ?? string.Empty,
                            Number = content.GetProperty("number").GetInt32(),
                            Title = content.GetProperty("title").GetString() ?? string.Empty
                        });
                    }
                }
            }
            
            _cache.Set(cacheKey, project, TimeSpan.FromMinutes(10));
            return project;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GitHub project {ProjectNumber} for {Owner}", projectNumber, owner);
            return null;
        }
    }

    /// <summary>
    /// Get milestones for a repository
    /// </summary>
    public async Task<List<GitHubMilestone>> GetMilestonesAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"milestones_{owner}_{repo}";
        
        if (_cache.TryGetValue(cacheKey, out List<GitHubMilestone>? cachedMilestones) && cachedMilestones != null)
        {
            return cachedMilestones;
        }
        
        try
        {
            var response = await _httpClient.GetAsync($"repos/{owner}/{repo}/milestones?state=open&sort=due_on&direction=asc", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var milestones = await JsonSerializer.DeserializeAsync<List<GitHubMilestone>>(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken) ?? new List<GitHubMilestone>();
            
            _cache.Set(cacheKey, milestones, TimeSpan.FromMinutes(15));
            return milestones;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GitHub milestones for {Owner}/{Repo}", owner, repo);
            return new List<GitHubMilestone>();
        }
    }

    /// <summary>
    /// Create a new milestone
    /// </summary>
    public async Task<GitHubMilestone?> CreateMilestoneAsync(string owner, string repo, NewMilestoneRequest milestoneRequest, CancellationToken cancellationToken = default)
    {
        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(milestoneRequest),
                Encoding.UTF8,
                "application/json");
            
            var response = await _httpClient.PostAsync($"repos/{owner}/{repo}/milestones", content, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var milestone = await JsonSerializer.DeserializeAsync<GitHubMilestone>(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);
            
            // Invalidate cache
            if (milestone != null)
            {
                var cacheKey = $"milestones_{owner}_{repo}";
                _cache.Remove(cacheKey);
            }
            
            return milestone;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating GitHub milestone for {Owner}/{Repo}", owner, repo);
            return null;
        }
    }

    /// <summary>
    /// Get repository labels
    /// </summary>
    public async Task<List<GitHubLabel>> GetLabelsAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"labels_{owner}_{repo}";
        
        if (_cache.TryGetValue(cacheKey, out List<GitHubLabel>? cachedLabels) && cachedLabels != null)
        {
            return cachedLabels;
        }
        
        try
        {
            var response = await _httpClient.GetAsync($"repos/{owner}/{repo}/labels?per_page=100", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var labels = await JsonSerializer.DeserializeAsync<List<GitHubLabel>>(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken) ?? new List<GitHubLabel>();
            
            _cache.Set(cacheKey, labels, TimeSpan.FromHours(1));
            return labels;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GitHub labels for {Owner}/{Repo}", owner, repo);
            return new List<GitHubLabel>();
        }
    }

    /// <summary>
    /// Create a new label
    /// </summary>
    public async Task<GitHubLabel?> CreateLabelAsync(string owner, string repo, NewLabelRequest labelRequest, CancellationToken cancellationToken = default)
    {
        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(labelRequest),
                Encoding.UTF8,
                "application/json");
            
            var response = await _httpClient.PostAsync($"repos/{owner}/{repo}/labels", content, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var label = await JsonSerializer.DeserializeAsync<GitHubLabel>(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);
            
            // Invalidate cache
            if (label != null)
            {
                var cacheKey = $"labels_{owner}_{repo}";
                _cache.Remove(cacheKey);
            }
            
            return label;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating GitHub label for {Owner}/{Repo}", owner, repo);
            return null;
        }
    }
    
    /// <summary>
    /// Get pull requests for a repository
    /// </summary>
    public async Task<List<GitHubPullRequest>> GetPullRequestsAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"pulls_{owner}_{repo}";
        
        if (_cache.TryGetValue(cacheKey, out List<GitHubPullRequest>? cachedPulls) && cachedPulls != null)
        {
            return cachedPulls;
        }
        
        try
        {
            var response = await _httpClient.GetAsync($"repos/{owner}/{repo}/pulls?state=open&sort=updated&per_page=100", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var pullRequests = await JsonSerializer.DeserializeAsync<List<GitHubPullRequest>>(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken) ?? new List<GitHubPullRequest>();
            
            _cache.Set(cacheKey, pullRequests, TimeSpan.FromMinutes(5));
            return pullRequests;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GitHub pull requests for {Owner}/{Repo}", owner, repo);
            return new List<GitHubPullRequest>();
        }
    }
}

/// <summary>
/// Interface for GitHub service
/// </summary>
public interface IGitHubService
{
    void SetAuthToken(string token);
    Task<GitHubUser?> GetUserAsync(CancellationToken cancellationToken = default);
    Task<List<GitHubRepository>> GetRepositoriesAsync(CancellationToken cancellationToken = default);
    Task<List<GitHubIssue>> GetIssuesAsync(string owner, string repo, CancellationToken cancellationToken = default);
    Task<GitHubIssue?> GetIssueAsync(string owner, string repo, int issueNumber, CancellationToken cancellationToken = default);
    Task<GitHubIssue?> CreateIssueAsync(string owner, string repo, NewIssueRequest issueRequest, CancellationToken cancellationToken = default);
    Task<GitHubIssue?> UpdateIssueAsync(string owner, string repo, int issueNumber, UpdateIssueRequest updateRequest, CancellationToken cancellationToken = default);
    Task<GitHubProject?> GetProjectAsync(string owner, string projectNumber, CancellationToken cancellationToken = default);
    Task<List<GitHubMilestone>> GetMilestonesAsync(string owner, string repo, CancellationToken cancellationToken = default);
    Task<GitHubMilestone?> CreateMilestoneAsync(string owner, string repo, NewMilestoneRequest milestoneRequest, CancellationToken cancellationToken = default);
    Task<List<GitHubLabel>> GetLabelsAsync(string owner, string repo, CancellationToken cancellationToken = default);
    Task<GitHubLabel?> CreateLabelAsync(string owner, string repo, NewLabelRequest labelRequest, CancellationToken cancellationToken = default);
    Task<List<GitHubPullRequest>> GetPullRequestsAsync(string owner, string repo, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request model for creating a new issue
/// </summary>
public class NewIssueRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public List<string>? Assignees { get; set; }
    public int? Milestone { get; set; }
    public List<string>? Labels { get; set; }
}

/// <summary>
/// Request model for updating an issue
/// </summary>
public class UpdateIssueRequest
{
    public string? Title { get; set; }
    public string? Body { get; set; }
    public string? State { get; set; }
    public int? Milestone { get; set; }
    public List<string>? Labels { get; set; }
    public List<string>? Assignees { get; set; }
}

/// <summary>
/// Request model for creating a new milestone
/// </summary>
public class NewMilestoneRequest
{
    public string Title { get; set; } = string.Empty;
    public string? State { get; set; }
    public string? Description { get; set; }
    public string? DueOn { get; set; }
}

/// <summary>
/// Request model for creating a new label
/// </summary>
public class NewLabelRequest
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string? Description { get; set; }
}
