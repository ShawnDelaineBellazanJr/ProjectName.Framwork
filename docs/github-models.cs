using System.Text.Json.Serialization;

namespace GitHubProjectManager.ApiService.Models;

/// <summary>
/// Represents a GitHub user
/// </summary>
public class GitHubUser
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Bio { get; set; }
    public string AvatarUrl { get; set; } = string.Empty;
    public string HtmlUrl { get; set; } = string.Empty;
    public int PublicRepos { get; set; }
    public int Followers { get; set; }
    public int Following { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// Represents a GitHub repository
/// </summary>
public class GitHubRepository
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public GitHubUser Owner { get; set; } = new();
    public string HtmlUrl { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Private { get; set; }
    public bool Fork { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? PushedAt { get; set; }
    public string DefaultBranch { get; set; } = "main";
    public int OpenIssuesCount { get; set; }
    public int ForksCount { get; set; }
    public int StargazersCount { get; set; }
    public int WatchersCount { get; set; }
    public string? Language { get; set; }
    public bool HasIssues { get; set; }
    public bool HasProjects { get; set; }
    public bool HasWiki { get; set; }
    public bool HasPages { get; set; }
    public bool HasDownloads { get; set; }
    public bool Archived { get; set; }
    public bool Disabled { get; set; }
}

/// <summary>
/// Represents a GitHub issue
/// </summary>
public class GitHubIssue
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public bool Locked { get; set; }
    public GitHubUser? User { get; set; }
    public List<GitHubUser> Assignees { get; set; } = new();
    public GitHubMilestone? Milestone { get; set; }
    public int Comments { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public string? Body { get; set; }
    public List<GitHubLabel> Labels { get; set; } = new();
    public GitHubUser? ClosedBy { get; set; }
    [JsonPropertyName("pull_request")]
    public GitHubPullRequestRef? PullRequest { get; set; }
    public string HtmlUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Indicates if this issue is a pull request
    /// </summary>
    public bool IsPullRequest => PullRequest != null;
}

/// <summary>
/// Reference to a pull request from an issue
/// </summary>
public class GitHubPullRequestRef
{
    public string Url { get; set; } = string.Empty;
    public string HtmlUrl { get; set; } = string.Empty;
}

/// <summary>
/// Represents a GitHub pull request
/// </summary>
public class GitHubPullRequest
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public bool Locked { get; set; }
    public GitHubUser User { get; set; } = new();
    public string HtmlUrl { get; set; } = string.Empty;
    public string? Body { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public DateTimeOffset? MergedAt { get; set; }
    public string? MergeCommitSha { get; set; }
    public GitHubUser? AssignedTo { get; set; }
    public List<GitHubUser> Assignees { get; set; } = new();
    public List<GitHubLabel> Labels { get; set; } = new();
    public GitHubMilestone? Milestone { get; set; }
    public bool Draft { get; set; }
    public string Head { get; set; } = string.Empty;
    public string Base { get; set; } = string.Empty;
    public GitHubRepository HeadRepo { get; set; } = new();
    public GitHubRepository BaseRepo { get; set; } = new();
    public bool Merged { get; set; }
    public bool? Mergeable { get; set; }
    public bool? Rebaseable { get; set; }
    public string MergeableState { get; set; } = string.Empty;
    public int Comments { get; set; }
    public int ReviewComments { get; set; }
    public int Commits { get; set; }
    public int Additions { get; set; }
    public int Deletions { get; set; }
    public int ChangedFiles { get; set; }
}

/// <summary>
/// Represents a GitHub milestone
/// </summary>
public class GitHubMilestone
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public GitHubUser Creator { get; set; } = new();
    public int OpenIssues { get; set; }
    public int ClosedIssues { get; set; }
    public string State { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DueOn { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public string HtmlUrl { get; set; } = string.Empty;
}

/// <summary>
/// Represents a GitHub label
/// </summary>
public class GitHubLabel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Default { get; set; }
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// Represents a GitHub project
/// </summary>
public class GitHubProject
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Number { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool Closed { get; set; }
    public List<GitHubProjectItem> Items { get; set; } = new();
}

/// <summary>
/// Represents a GitHub project item
/// </summary>
public class GitHubProjectItem
{
    public string Id { get; set; } = string.Empty;
    public string ContentId { get; set; } = string.Empty;
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
}