# Implementing the Maker-Checker Pattern in Wisdom-Guided Approach

The Maker-Checker pattern is a crucial validation mechanism that adds an extra layer of security, accuracy, and accountability to critical operations. This document explains how to integrate this pattern into the Wisdom-Guided approach with .NET Aspire and Semantic Kernel AI Agent Framework.

## Overview of the Maker-Checker Pattern

The Maker-Checker pattern involves two distinct roles:

- **Maker**: The entity that creates or initiates an action (e.g., configuring an AI agent, defining a new prompt, or setting a milestone)
- **Checker**: The entity that reviews and validates the action before it is finalized

This pattern is especially valuable in AI systems where accuracy and security are paramount.

## Implementation Areas

In the Wisdom-Guided approach, the Maker-Checker pattern can be applied to several critical areas:

1. **AI Agent Configuration**
2. **Prompt Template Management**
3. **Project Milestone Validation**
4. **Sales and Revenue Verification**

Let's implement this pattern for each of these areas.

## 1. AI Agent Configuration Validation

### Create Models

First, let's create models to track the Maker-Checker workflow for agent configurations.

Create a file `WisdomGuidedAspire.ApiService/Models/MakerCheckerModels.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace WisdomGuidedAspire.ApiService.Models
{
    // Base class for all maker-checker entities
    public abstract class MakerCheckerEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string RejectedBy { get; set; }
        public DateTime? RejectedDate { get; set; }
        public string RejectionReason { get; set; }
        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
        public List<ApprovalComment> Comments { get; set; } = new List<ApprovalComment>();
    }
    
    public class ApprovalComment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Comment { get; set; }
        public string Author { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
    
    public enum ApprovalStatus
    {
        Pending,
        Approved,
        Rejected
    }
    
    // Specific entity for agent configuration
    public class AgentConfigurationApproval : MakerCheckerEntity
    {
        public string AgentType { get; set; }
        public string AgentName { get; set; }
        public string SystemPrompt { get; set; }
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
        public string Description { get; set; }
        public bool IsUpdate { get; set; } = false;
        public string PreviousVersionId { get; set; }
    }
    
    // Specific entity for prompt template
    public class PromptTemplateApproval : MakerCheckerEntity
    {
        public string TemplateName { get; set; }
        public string TemplateContent { get; set; }
        public string UseCase { get; set; }
        public string Description { get; set; }
        public bool IsUpdate { get; set; } = false;
        public string PreviousVersionId { get; set; }
    }
    
    // Specific entity for milestone
    public class MilestoneApproval : MakerCheckerEntity
    {
        public string MilestoneName { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public Dictionary<string, string> ExpectedMetrics { get; set; } = new Dictionary<string, string>();
    }
    
    // Specific entity for sales record
    public class SaleRecordApproval : MakerCheckerEntity
    {
        public string ProjectType { get; set; }
        public decimal Amount { get; set; }
        public string ClientPlatform { get; set; }
        public string ClientName { get; set; }
        public string ClientEmail { get; set; }
        public List<string> Features { get; set; } = new List<string>();
        public string Notes { get; set; }
    }
}
```

### Create the Maker-Checker Service

Now, let's create a service to manage the Maker-Checker workflow:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WisdomGuidedAspire.ApiService.Models;

namespace WisdomGuidedAspire.ApiService.Services
{
    public class MakerCheckerService
    {
        private readonly WisdomLogger _logger;
        private readonly IDistributedCache _cache;
        private readonly EmailService _emailService;
        
        private readonly string _agentConfigurationsCacheKey = "MakerChecker:AgentConfigurations";
        private readonly string _promptTemplatesCacheKey = "MakerChecker:PromptTemplates";
        private readonly string _milestonesCacheKey = "MakerChecker:Milestones";
        private readonly string _salesCacheKey = "MakerChecker:Sales";
        
        public MakerCheckerService(
            WisdomLogger logger,
            IDistributedCache cache,
            EmailService emailService)
        {
            _logger = logger;
            _cache = cache;
            _emailService = emailService;
        }
        
        // Agent Configuration Methods
        public async Task<List<AgentConfigurationApproval>> GetAgentConfigurationApprovals()
        {
            string json = await _cache.GetStringAsync(_agentConfigurationsCacheKey);
            
            if (string.IsNullOrEmpty(json))
            {
                return new List<AgentConfigurationApproval>();
            }
            
            return System.Text.Json.JsonSerializer.Deserialize<List<AgentConfigurationApproval>>(json);
        }
        
        public async Task<AgentConfigurationApproval> GetAgentConfigurationApproval(string id)
        {
            var approvals = await GetAgentConfigurationApprovals();
            return approvals.FirstOrDefault(a => a.Id == id);
        }
        
        public async Task<AgentConfigurationApproval> SubmitAgentConfigurationForApproval(
            AgentConfigurationApproval configuration, 
            string createdBy)
        {
            configuration.CreatedBy = createdBy;
            configuration.Status = ApprovalStatus.Pending;
            
            var approvals = await GetAgentConfigurationApprovals();
            approvals.Add(configuration);
            
            await SaveAgentConfigurationApprovals(approvals);
            
            // Log the submission
            await _logger.LogActivity(
                "AgentConfigSubmitted",
                $"Agent configuration submitted for approval: {configuration.AgentName} ({configuration.AgentType})",
                $"Submitted by: {createdBy}, Status: Pending"
            );
            
            // Send notification email to reviewers
            await _emailService.SendApprovalNotificationAsync(
                "New Agent Configuration Pending Approval",
                $"A new agent configuration for {configuration.AgentName} requires your approval.",
                configuration.Id,
                "AgentConfiguration"
            );
            
            return configuration;
        }
        
        public async Task<AgentConfigurationApproval> ApproveAgentConfiguration(
            string id, 
            string approvedBy, 
            string comment = null)
        {
            var approvals = await GetAgentConfigurationApprovals();
            var configuration = approvals.FirstOrDefault(a => a.Id == id);
            
            if (configuration == null)
            {
                throw new ArgumentException($"Agent configuration with ID {id} not found");
            }
            
            if (configuration.Status != ApprovalStatus.Pending)
            {
                throw new InvalidOperationException($"Agent configuration is not in pending status: {configuration.Status}");
            }
            
            // Add comment if provided
            if (!string.IsNullOrEmpty(comment))
            {
                configuration.Comments.Add(new ApprovalComment
                {
                    Comment = comment,
                    Author = approvedBy
                });
            }
            
            // Update approval information
            configuration.ApprovedBy = approvedBy;
            configuration.ApprovedDate = DateTime.UtcNow;
            configuration.Status = ApprovalStatus.Approved;
            
            await SaveAgentConfigurationApprovals(approvals);
            
            // Log the approval
            await _logger.LogActivity(
                "AgentConfigApproved",
                $"Agent configuration approved: {configuration.AgentName} ({configuration.AgentType})",
                $"Approved by: {approvedBy}, Comment: {comment}"
            );
            
            // Send notification email to creator
            await _emailService.SendApprovalResultNotificationAsync(
                "Agent Configuration Approved",
                $"Your agent configuration for {configuration.AgentName} has been approved by {approvedBy}.",
                configuration.CreatedBy,
                comment
            );
            
            return configuration;
        }
        
        public async Task<AgentConfigurationApproval> RejectAgentConfiguration(
            string id, 
            string rejectedBy, 
            string reason,
            string comment = null)
        {
            var approvals = await GetAgentConfigurationApprovals();
            var configuration = approvals.FirstOrDefault(a => a.Id == id);
            
            if (configuration == null)
            {
                throw new ArgumentException($"Agent configuration with ID {id} not found");
            }
            
            if (configuration.Status != ApprovalStatus.Pending)
            {
                throw new InvalidOperationException($"Agent configuration is not in pending status: {configuration.Status}");
            }
            
            // Add comment if provided
            if (!string.IsNullOrEmpty(comment))
            {
                configuration.Comments.Add(new ApprovalComment
                {
                    Comment = comment,
                    Author = rejectedBy
                });
            }
            
            // Update rejection information
            configuration.RejectedBy = rejectedBy;
            configuration.RejectedDate = DateTime.UtcNow;
            configuration.RejectionReason = reason;
            configuration.Status = ApprovalStatus.Rejected;
            
            await SaveAgentConfigurationApprovals(approvals);
            
            // Log the rejection
            await _logger.LogActivity(
                "AgentConfigRejected",
                $"Agent configuration rejected: {configuration.AgentName} ({configuration.AgentType})",
                $"Rejected by: {rejectedBy}, Reason: {reason}, Comment: {comment}"
            );
            
            // Send notification email to creator
            await _emailService.SendApprovalResultNotificationAsync(
                "Agent Configuration Rejected",
                $"Your agent configuration for {configuration.AgentName} has been rejected by {rejectedBy}. Reason: {reason}",
                configuration.CreatedBy,
                comment
            );
            
            return configuration;
        }
        
        private async Task SaveAgentConfigurationApprovals(List<AgentConfigurationApproval> approvals)
        {
            string json = System.Text.Json.JsonSerializer.Serialize(approvals);
            
            await _cache.SetStringAsync(
                _agentConfigurationsCacheKey,
                json,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(60)
                }
            );
        }
        
        // Prompt Template Methods
        // Similar methods for prompt templates, milestones, and sales records
        // ...
    }
}
```

### Enhance Email Service for Approval Notifications

Update the EmailService to include methods for sending approval notifications:

```csharp
// Add these methods to the EmailService class

public async Task SendApprovalNotificationAsync(
    string subject,
    string message,
    string entityId,
    string entityType)
{
    string body = $@"
        <html>
        <body>
            <h2>Approval Required</h2>
            
            <p>{message}</p>
            
            <p>Please review and approve or reject this item.</p>
            
            <p>Entity Type: {entityType}<br>
            Entity ID: {entityId}<br>
            Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
            
            <p>You can approve or reject this item through the administration interface.</p>
        </body>
        </html>
    ";
    
    // Send to the approval email address (could be a group email)
    await SendEmailAsync(_settings.ApprovalEmail, subject, body, true);
}

public async Task SendApprovalResultNotificationAsync(
    string subject,
    string message,
    string recipientEmail,
    string comment = null)
{
    string commentHtml = string.IsNullOrEmpty(comment) 
        ? "" 
        : $"<p><strong>Comment:</strong> {comment}</p>";
    
    string body = $@"
        <html>
        <body>
            <h2>{subject}</h2>
            
            <p>{message}</p>
            
            {commentHtml}
            
            <p>Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
        </body>
        </html>
    ";
    
    await SendEmailAsync(recipientEmail, subject, body, true);
}
```

### Create a Controller for the Maker-Checker Service

```csharp
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WisdomGuidedAspire.ApiService.Models;
using WisdomGuidedAspire.ApiService.Services;

namespace WisdomGuidedAspire.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApprovalController : ControllerBase
    {
        private readonly MakerCheckerService _makerCheckerService;
        private readonly ILogger<ApprovalController> _logger;

        public ApprovalController(
            MakerCheckerService makerCheckerService,
            ILogger<ApprovalController> logger)
        {
            _makerCheckerService = makerCheckerService;
            _logger = logger;
        }

        // Agent Configuration Endpoints
        [HttpGet("agent-configurations")]
        public async Task<IActionResult> GetAgentConfigurationApprovals()
        {
            try
            {
                var approvals = await _makerCheckerService.GetAgentConfigurationApprovals();
                return Ok(approvals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving agent configuration approvals");
                return StatusCode(500, new { error = "An error occurred retrieving approvals" });
            }
        }
        
        [HttpGet("agent-configurations/{id}")]
        public async Task<IActionResult> GetAgentConfigurationApproval(string id)
        {
            try
            {
                var approval = await _makerCheckerService.GetAgentConfigurationApproval(id);
                
                if (approval == null)
                {
                    return NotFound(new { error = $"Agent configuration approval with ID {id} not found" });
                }
                
                return Ok(approval);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving agent configuration approval");
                return StatusCode(500, new { error = "An error occurred retrieving the approval" });
            }
        }
        
        [HttpPost("agent-configurations")]
        public async Task<IActionResult> SubmitAgentConfigurationForApproval([FromBody] AgentConfigurationApproval approval)
        {
            try
            {
                // In a real application, you would get the user from authentication
                string user = Request.Headers["X-User"].ToString() ?? "system";
                
                var result = await _makerCheckerService.SubmitAgentConfigurationForApproval(approval, user);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting agent configuration for approval");
                return StatusCode(500, new { error = "An error occurred submitting the approval" });
            }
        }
        
        [HttpPost("agent-configurations/{id}/approve")]
        public async Task<IActionResult> ApproveAgentConfiguration(
            string id, 
            [FromBody] ApproveRequest request)
        {
            try
            {
                // In a real application, you would get the user from authentication
                string user = Request.Headers["X-User"].ToString() ?? "system";
                
                var result = await _makerCheckerService.ApproveAgentConfiguration(id, user, request?.Comment);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving agent configuration");
                return StatusCode(500, new { error = "An error occurred approving the configuration" });
            }
        }
        
        [HttpPost("agent-configurations/{id}/reject")]
        public async Task<IActionResult> RejectAgentConfiguration(
            string id, 
            [FromBody] RejectRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.Reason))
                {
                    return BadRequest(new { error = "Rejection reason is required" });
                }
                
                // In a real application, you would get the user from authentication
                string user = Request.Headers["X-User"].ToString() ?? "system";
                
                var result = await _makerCheckerService.RejectAgentConfiguration(id, user, request.Reason, request.Comment);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting agent configuration");
                return StatusCode(500, new { error = "An error occurred rejecting the configuration" });
            }
        }
        
        // Similar endpoints for prompt templates, milestones, and sales records
        // ...
    }
    
    public class ApproveRequest
    {
        public string Comment { get; set; }
    }
    
    public class RejectRequest
    {
        public string Reason { get; set; }
        public string Comment { get; set; }
    }
}
```

### Integrate with Agent Factory

Now, let's modify the AgentFactory to integrate with the Maker-Checker pattern:

```csharp
// Add this method to the AgentFactory class

public async Task<string> SubmitAgentConfigurationForApproval(
    string agentType,
    string agentName,
    string systemPrompt,
    Dictionary<string, string> parameters,
    string description,
    bool isUpdate = false,
    string previousVersionId = null)
{
    // Create the approval request
    var approval = new AgentConfigurationApproval
    {
        AgentType = agentType,
        AgentName = agentName,
        SystemPrompt = systemPrompt,
        Parameters = parameters,
        Description = description,
        IsUpdate = isUpdate,
        PreviousVersionId = previousVersionId
    };
    
    // Submit for approval
    var result = await _makerCheckerService.SubmitAgentConfigurationForApproval(
        approval,
        "system" // In a real application, use the actual user
    );
    
    return result.Id;
}

// Modify the CreateAgent method to check if the agent is approved
public async Task<ChatCompletionAgent> CreateAgent(
    string agentType,
    Dictionary<string, string> parameters = null)
{
    // Check if this agent type requires approval
    bool requiresApproval = RequiresApproval(agentType);
    
    if (requiresApproval)
    {
        // Check if there's an approved configuration for this agent type
        var approvals = await _makerCheckerService.GetAgentConfigurationApprovals();
        var approvedConfig = approvals
            .Where(a => a.AgentType == agentType && a.Status == ApprovalStatus.Approved)
            .OrderByDescending(a => a.ApprovedDate)
            .FirstOrDefault();
        
        if (approvedConfig != null)
        {
            // Use the approved configuration
            string instructions = approvedConfig.SystemPrompt;
            
            // Apply dynamic parameters if provided
            if (parameters != null && parameters.Count > 0)
            {
                foreach (var param in parameters)
                {
                    instructions = instructions.Replace($"{{{{{param.Key}}}}}", param.Value);
                }
            }
            
            // Create the agent with approved configuration
            var agent = new ChatCompletionAgent
            {
                Name = agentType,
                Instructions = instructions,
                Kernel = _kernel
            };
            
            _logger.LogInformation("Created agent of type {AgentType} using approved configuration", agentType);
            
            return agent;
        }
        else
        {
            // No approved configuration found, fall back to default
            _logger.LogWarning("No approved configuration found for agent type {AgentType}, using default", agentType);
        }
    }
    
    // Default agent creation (original implementation)
    string defaultInstructions = await LoadAgentInstructions(agentType);
    
    // Apply dynamic parameters if provided
    if (parameters != null && parameters.Count > 0)
    {
        foreach (var param in parameters)
        {
            defaultInstructions = defaultInstructions.Replace($"{{{{{param.Key}}}}}", param.Value);
        }
    }
    
    // Create the agent with default configuration
    var defaultAgent = new ChatCompletionAgent
    {
        Name = agentType,
        Instructions = defaultInstructions,
        Kernel = _kernel
    };
    
    _logger.LogInformation("Created agent of type {AgentType}", agentType);
    
    return defaultAgent;
}

private bool RequiresApproval(string agentType)
{
    // Define which agent types require approval
    var typesRequiringApproval = new[]
    {
        "CustomerSupportAgent",
        "LegalSummarizerAgent",
        "EscalationAgent"
    };
    
    return typesRequiringApproval.Contains(agentType);
}
```

### Register the Maker-Checker Service

Update `Program.cs` to register the Maker-Checker service:

```csharp
// Add this line to the service registration section
builder.Services.AddSingleton<MakerCheckerService>();
```

## 2. Prompt Template Management with Maker-Checker

Let's implement the Maker-Checker pattern for managing prompt templates:

### Create a Prompt Template Service

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WisdomGuidedAspire.ApiService.Models;

namespace WisdomGuidedAspire.ApiService.Services
{
    public class PromptTemplateService
    {
        private readonly WisdomLogger _logger;
        private readonly MakerCheckerService _makerCheckerService;
        private readonly string _promptsDirectory;
        
        public PromptTemplateService(
            WisdomLogger logger,
            MakerCheckerService makerCheckerService)
        {
            _logger = logger;
            _makerCheckerService = makerCheckerService;
            _promptsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Prompts");
        }
        
        public async Task<string> SubmitPromptTemplateForApproval(
            string templateName,
            string templateContent,
            string useCase,
            string description,
            string createdBy,
            bool isUpdate = false,
            string previousVersionId = null)
        {
            // Create the approval request
            var approval = new PromptTemplateApproval
            {
                TemplateName = templateName,
                TemplateContent = templateContent,
                UseCase = useCase,
                Description = description,
                IsUpdate = isUpdate,
                PreviousVersionId = previousVersionId
            };
            
            // Submit for approval
            var result = await _makerCheckerService.SubmitPromptTemplateForApproval(
                approval,
                createdBy
            );
            
            await _logger.LogActivity(
                "PromptTemplateSubmitted",
                $"Prompt template submitted for approval: {templateName}",
                $"Submitted by: {createdBy}, Use case: {useCase}"
            );
            
            return result.Id;
        }
        
        public async Task<bool> DeployApprovedTemplate(string approvalId)
        {
            // Get the approval
            var approval = await _makerCheckerService.GetPromptTemplateApproval(approvalId);
            
            if (approval == null || approval.Status != ApprovalStatus.Approved)
            {
                throw new InvalidOperationException("Cannot deploy unapproved prompt template");
            }
            
            // Determine the directory based on use case
            string directory = Path.Combine(_promptsDirectory, "UseCases", approval.UseCase);
            
            // Ensure directory exists
            Directory.CreateDirectory(directory);
            
            // File path
            string filePath = Path.Combine(directory, $"{approval.TemplateName}.txt");
            
            // Write template content to file
            await File.WriteAllTextAsync(filePath, approval.TemplateContent);
            
            await _logger.LogActivity(
                "PromptTemplateDeployed",
                $"Prompt template deployed: {approval.TemplateName}",
                $"Deployed to: {filePath}, Approved by: {approval.ApprovedBy}"
            );
            
            return true;
        }
        
        public async Task<List<string>> ListPromptTemplates(string useCase = null)
        {
            var templates = new List<string>();
            
            // Get base directory
            string baseDir = _promptsDirectory;
            
            if (!string.IsNullOrEmpty(useCase))
            {
                baseDir = Path.Combine(baseDir, "UseCases", useCase);
                
                if (!Directory.Exists(baseDir))
                {
                    return templates;
                }
            }
            
            // Recursively get all .txt files
            var files = Directory.GetFiles(baseDir, "*.txt", SearchOption.AllDirectories);
            
            // Extract relative paths
            foreach (var file in files)
            {
                string relativePath = file.Substring(_promptsDirectory.Length).TrimStart('\\', '/');
                templates.Add(relativePath);
            }
            
            return templates;
        }
        
        public async Task<string> GetPromptTemplateContent(string path)
        {
            string fullPath = Path.Combine(_promptsDirectory, path);
            
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Prompt template not found: {path}");
            }
            
            return await File.ReadAllTextAsync(fullPath);
        }
    }
}
```

### Add Controller for Prompt Template Management

```csharp
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WisdomGuidedAspire.ApiService.Models;
using WisdomGuidedAspire.ApiService.Services;

namespace WisdomGuidedAspire.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PromptTemplateController : ControllerBase
    {
        private readonly PromptTemplateService _promptTemplateService;
        private readonly ILogger<PromptTemplateController> _logger;

        public PromptTemplateController(
            PromptTemplateService promptTemplateService,
            ILogger<PromptTemplateController> logger)
        {
            _promptTemplateService = promptTemplateService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> ListPromptTemplates([FromQuery] string useCase = null)
        {
            try
            {
                var templates = await _promptTemplateService.ListPromptTemplates(useCase);
                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing prompt templates");
                return StatusCode(500, new { error = "An error occurred listing prompt templates" });
            }
        }
        
        [HttpGet("{*path}")]
        public async Task<IActionResult> GetPromptTemplateContent(string path)
        {
            try
            {
                var content = await _promptTemplateService.GetPromptTemplateContent(path);
                return Ok(new { content });
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { error = $"Prompt template not found: {path}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving prompt template");
                return StatusCode(500, new { error = "An error occurred retrieving the prompt template" });
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> SubmitPromptTemplateForApproval([FromBody] PromptTemplateSubmission submission)
        {
            try
            {
                // In a real application, you would get the user from authentication
                string user = Request.Headers["X-User"].ToString() ?? "system";
                
                var approvalId = await _promptTemplateService.SubmitPromptTemplateForApproval(
                    submission.TemplateName,
                    submission.TemplateContent,
                    submission.UseCase,
                    submission.Description,
                    user,
                    submission.IsUpdate,
                    submission.PreviousVersionId
                );
                
                return Ok(new { approvalId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting prompt template for approval");
                return StatusCode(500, new { error = "An error occurred submitting the prompt template" });
            }
        }
        
        [HttpPost("deploy/{approvalId}")]
        public async Task<IActionResult> DeployApprovedTemplate(string approvalId)
        {
            try
            {
                var result = await _promptTemplateService.DeployApprovedTemplate(approvalId);
                return Ok(new { deployed = result });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deploying prompt template");
                return StatusCode(500, new { error = "An error occurred deploying the prompt template" });
            }
        }
    }
    
    public class PromptTemplateSubmission
    {
        public string TemplateName { get; set; }
        public string TemplateContent { get; set; }
        public string UseCase { get; set; }
        public string Description { get; set; }
        public bool IsUpdate { get; set; } = false;
        public string PreviousVersionId { get; set; }
    }
}
```

### Register the Prompt Template Service

Update `Program.cs` to register the Prompt Template service:

```csharp
// Add this line to the service registration section
builder.Services.AddSingleton<PromptTemplateService>();
```

## 3. Project Milestone Validation with Maker-Checker

Let's integrate the Maker-