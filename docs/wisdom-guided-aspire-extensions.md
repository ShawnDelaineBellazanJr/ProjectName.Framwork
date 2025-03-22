                await client.SendMailAsync(message);
                
                // Log the email sending
                await _wisdomLogger.LogActivity(
                    "EmailSent",
                    $"Sent email to {to} with subject '{subject}'",
                    $"Email sent successfully"
                );
                
                _logger.LogInformation("Email sent to {Recipient} with subject '{Subject}'", to, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Recipient} with subject '{Subject}'", to, subject);
                throw;
            }
        }
        
        public async Task SendSaleNotificationAsync(
            string clientEmail, 
            string projectType, 
            decimal amount)
        {
            string subject = $"Your {projectType} Purchase Confirmation";
            
            string body = $@"
                <html>
                <body>
                    <h2>Thank You for Your Purchase!</h2>
                    
                    <p>We're excited that you've chosen our {projectType} solution.</p>
                    
                    <h3>Purchase Details:</h3>
                    <ul>
                        <li><strong>Product:</strong> {projectType}</li>
                        <li><strong>Amount:</strong> ${amount}</li>
                        <li><strong>Date:</strong> {DateTime.UtcNow:yyyy-MM-dd}</li>
                    </ul>
                    
                    <p>Your download link and setup instructions will be sent in a separate email within the next hour.</p>
                    
                    <p>If you have any questions, please don't hesitate to contact us.</p>
                    
                    <p>Best regards,<br>Your AI Solutions Team</p>
                </body>
                </html>
            ";
            
            await SendEmailAsync(clientEmail, subject, body, true);
        }
        
        public async Task SendEscalationNotificationAsync(
            string agentType, 
            string userQuery, 
            string escalationReason)
        {
            string subject = $"Escalation Required: {agentType}";
            
            string body = $@"
                <html>
                <body>
                    <h2>Agent Escalation Required</h2>
                    
                    <p>An AI agent has identified a query that requires human attention.</p>
                    
                    <h3>Details:</h3>
                    <ul>
                        <li><strong>Agent Type:</strong> {agentType}</li>
                        <li><strong>User Query:</strong> {userQuery}</li>
                        <li><strong>Escalation Reason:</strong> {escalationReason}</li>
                        <li><strong>Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</li>
                    </ul>
                    
                    <p>Please review and respond to this query as soon as possible.</p>
                </body>
                </html>
            ";
            
            await SendEmailAsync(_settings.SupportEmail, subject, body, true);
        }
    }
    
    public class EmailSettings
    {
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public bool EnableSsl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string FromEmail { get; set; }
        public string FromName { get; set; }
        public string SupportEmail { get; set; }
    }
}
```

### 2. Register Email Service in Program.cs

```csharp
// Add email settings configuration
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));

// Register email service
builder.Services.AddSingleton<EmailService>();
```

### 3. Update appsettings.json

```json
{
  "Email": {
    "SmtpServer": "smtp.example.com",
    "SmtpPort": 587,
    "EnableSsl": true,
    "Username": "your-email@example.com",
    "Password": "your-password",
    "FromEmail": "no-reply@example.com",
    "FromName": "AI Solutions Team",
    "SupportEmail": "support@example.com"
  }
}
```

### 4. Integrate Email Notifications with Revenue Tracking

Update the `RevenueController.cs` to send email notifications on successful sales:

```csharp
[HttpPost("sales")]
public async Task<IActionResult> RecordSale([FromBody] SaleRecord sale)
{
    try
    {
        _logger.LogInformation("Recording sale: {ProjectType} for ${Amount}", sale.ProjectType, sale.Amount);
        
        var result = await _revenueService.RecordSale(sale);
        
        // Check if we need to perform a milestone check
        bool onTrack = await _revenueService.CheckMilestone();
        
        // Send email notification if client email is provided
        if (!string.IsNullOrEmpty(sale.ClientEmail))
        {
            await _emailService.SendSaleNotificationAsync(
                sale.ClientEmail,
                sale.ProjectType,
                sale.Amount);
        }
        
        return Ok(new { 
            sale = result,
            onTrack = onTrack
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error recording sale");
        return StatusCode(500, new { error = "An error occurred recording the sale" });
    }
}
```

### 5. Integrate Email Notifications with Agent Escalations

Update the `ChatbotService.cs` to send email notifications when an agent escalates a query:

```csharp
// In the ProcessCustomerQuery method, after detecting escalation:
if (needsEscalation)
{
    // Send escalation notification
    await _emailService.SendEscalationNotificationAsync(
        "CustomerSupportAgent",
        query,
        escalationReason);
}
```

## Milestone Tracking System

Let's enhance our system to track milestones within our 30-day project timeline, as outlined in the Oral Contract and Articles of Operations.

### 1. Create Milestone Models

Create a file `WisdomGuidedAspire.ApiService/Models/MilestoneModels.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace WisdomGuidedAspire.ApiService.Models
{
    public class Milestone
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public MilestoneStatus Status { get; set; } = MilestoneStatus.Pending;
        public DateTime? CompletedDate { get; set; }
        public List<MilestoneAction> Actions { get; set; } = new List<MilestoneAction>();
        public Dictionary<string, string> Metrics { get; set; } = new Dictionary<string, string>();
    }
    
    public class MilestoneAction
    {
        public string Description { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string Outcome { get; set; }
    }
    
    public enum MilestoneStatus
    {
        Pending,
        InProgress,
        Completed,
        Missed
    }
}
```

### 2. Create Milestone Service

Create a service for milestone tracking in `WisdomGuidedAspire.ApiService/Services/MilestoneService.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WisdomGuidedAspire.ApiService.Models;

namespace WisdomGuidedAspire.ApiService.Services
{
    public class MilestoneService
    {
        private readonly WisdomLogger _logger;
        private readonly ContractService _contractService;
        private readonly IDistributedCache _cache;
        private readonly string _milestonesCacheKey = "Milestones";
        
        public MilestoneService(
            WisdomLogger logger,
            ContractService contractService,
            IDistributedCache cache)
        {
            _logger = logger;
            _contractService = contractService;
            _cache = cache;
            
            // Initialize default milestones
            _ = InitializeDefaultMilestones();
        }
        
        private async Task InitializeDefaultMilestones()
        {
            // Only initialize if no milestones exist
            var milestones = await GetAllMilestones();
            if (milestones.Count > 0)
            {
                return;
            }
            
            var contract = _contractService.GetContract();
            
            // Create default milestones based on contract
            var defaultMilestones = new List<Milestone>
            {
                // Day 5: First projects built
                new Milestone
                {
                    Name = "First Projects Built",
                    Description = "Complete development of initial projects (Chatbot and Summarizer)",
                    DueDate = contract.StartDate.AddDays(5)
                },
                
                // Day 10: First sales milestone
                new Milestone
                {
                    Name = "First Sales Milestone",
                    Description = "Achieve 5 sales with a minimum revenue of $2,500",
                    DueDate = contract.StartDate.AddDays(10)
                },
                
                // Day 15: Mid-point review
                new Milestone
                {
                    Name = "Mid-point Review",
                    Description = "Review progress towards revenue goal and optimize strategy if needed",
                    DueDate = contract.StartDate.AddDays(15)
                },
                
                // Day 20: Second sales milestone
                new Milestone
                {
                    Name = "Second Sales Milestone",
                    Description = "Achieve 10 sales with a minimum revenue of $5,000",
                    DueDate = contract.StartDate.AddDays(20)
                },
                
                // Day 30: Final goal
                new Milestone
                {
                    Name = "Final Goal",
                    Description = $"Achieve revenue goal of ${contract.RevenueGoalMin}-${contract.RevenueGoalMax}",
                    DueDate = contract.EndDate
                }
            };
            
            // Save default milestones
            await SaveMilestones(defaultMilestones);
            
            // Log the initialization
            await _logger.LogActivity(
                "MilestonesInitialized",
                $"Initialized {defaultMilestones.Count} default milestones",
                $"First milestone due: {defaultMilestones.First().DueDate:yyyy-MM-dd}"
            );
        }
        
        public async Task<List<Milestone>> GetAllMilestones()
        {
            string milestonesJson = await _cache.GetStringAsync(_milestonesCacheKey);
            
            if (string.IsNullOrEmpty(milestonesJson))
            {
                return new List<Milestone>();
            }
            
            return System.Text.Json.JsonSerializer.Deserialize<List<Milestone>>(milestonesJson);
        }
        
        public async Task<Milestone> GetMilestoneById(string id)
        {
            var milestones = await GetAllMilestones();
            return milestones.FirstOrDefault(m => m.Id == id);
        }
        
        private async Task SaveMilestones(List<Milestone> milestones)
        {
            string milestonesJson = System.Text.Json.JsonSerializer.Serialize(milestones);
            
            await _cache.SetStringAsync(
                _milestonesCacheKey,
                milestonesJson,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(60)
                }
            );
        }
        
        public async Task<Milestone> UpdateMilestone(Milestone milestone)
        {
            var milestones = await GetAllMilestones();
            
            // Find and remove existing milestone with the same ID if it exists
            int existingIndex = milestones.FindIndex(m => m.Id == milestone.Id);
            if (existingIndex >= 0)
            {
                milestones.RemoveAt(existingIndex);
            }
            
            // Add the updated milestone
            milestones.Add(milestone);
            
            // Save the updated milestones
            await SaveMilestones(milestones);
            
            // Log the update
            await _logger.LogActivity(
                "MilestoneUpdated",
                $"Updated milestone: {milestone.Name}",
                $"Status: {milestone.Status}, Due: {milestone.DueDate:yyyy-MM-dd}"
            );
            
            return milestone;
        }
        
        public async Task<Milestone> CompleteMilestone(string id, Dictionary<string, string> metrics = null)
        {
            var milestone = await GetMilestoneById(id);
            
            if (milestone == null)
            {
                throw new ArgumentException($"Milestone with ID {id} not found");
            }
            
            // Update milestone status
            milestone.Status = MilestoneStatus.Completed;
            milestone.CompletedDate = DateTime.UtcNow;
            
            // Add metrics if provided
            if (metrics != null)
            {
                foreach (var metric in metrics)
                {
                    milestone.Metrics[metric.Key] = metric.Value;
                }
            }
            
            // Add completion action
            milestone.Actions.Add(new MilestoneAction
            {
                Description = "Milestone completed",
                Outcome = metrics != null ? $"Metrics: {string.Join(", ", metrics.Select(m => $"{m.Key}={m.Value}"))}" : "No metrics provided"
            });
            
            // Save the updated milestone
            await UpdateMilestone(milestone);
            
            return milestone;
        }
        
        public async Task CheckMilestones(RevenueTrackingService revenueService)
        {
            var milestones = await GetAllMilestones();
            var summary = await revenueService.GetRevenueSummary();
            
            foreach (var milestone in milestones.Where(m => m.Status == MilestoneStatus.Pending || m.Status == MilestoneStatus.InProgress))
            {
                // Check if milestone is due
                if (DateTime.UtcNow >= milestone.DueDate)
                {
                    // Check if milestone conditions are met
                    bool isCompleted = false;
                    Dictionary<string, string> metrics = new Dictionary<string, string>();
                    
                    switch (milestone.Name)
                    {
                        case "First Sales Milestone":
                            isCompleted = summary.TotalSales >= 5 && summary.TotalRevenue >= 2500;
                            metrics["TotalSales"] = summary.TotalSales.ToString();
                            metrics["TotalRevenue"] = summary.TotalRevenue.ToString();
                            break;
                            
                        case "Second Sales Milestone":
                            isCompleted = summary.TotalSales >= 10 && summary.TotalRevenue >= 5000;
                            metrics["TotalSales"] = summary.TotalSales.ToString();
                            metrics["TotalRevenue"] = summary.TotalRevenue.ToString();
                            break;
                            
                        case "Final Goal":
                            isCompleted = summary.TotalRevenue >= summary.RevenueGoalMin;
                            metrics["TotalRevenue"] = summary.TotalRevenue.ToString();
                            metrics["GoalCompletionPercentage"] = summary.GoalCompletionPercentageMin.ToString("F2") + "%";
                            break;
                            
                        // For other milestones, we need manual completion
                    }
                    
                    if (isCompleted)
                    {
                        await CompleteMilestone(milestone.Id, metrics);
                    }
                    else if (DateTime.UtcNow > milestone.DueDate.AddDays(1))
                    {
                        // Mark as missed if more than 1 day past due and not completed
                        milestone.Status = MilestoneStatus.Missed;
                        
                        // Add missed action
                        milestone.Actions.Add(new MilestoneAction
                        {
                            Description = "Milestone missed",
                            Outcome = $"Current metrics: TotalSales={summary.TotalSales}, TotalRevenue=${summary.TotalRevenue}"
                        });
                        
                        await UpdateMilestone(milestone);
                    }
                }
                else if (milestone.Status == MilestoneStatus.Pending && DateTime.UtcNow >= milestone.DueDate.AddDays(-3))
                {
                    // Mark as in progress if within 3 days of due date
                    milestone.Status = MilestoneStatus.InProgress;
                    
                    // Add in progress action
                    milestone.Actions.Add(new MilestoneAction
                    {
                        Description = "Milestone approaching due date",
                        Outcome = "Status changed to In Progress"
                    });
                    
                    await UpdateMilestone(milestone);
                }
            }
        }
    }
}
```

### 3. Create Milestone Controller

Create a controller for milestone tracking in `WisdomGuidedAspire.ApiService/Controllers/MilestoneController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WisdomGuidedAspire.ApiService.Models;
using WisdomGuidedAspire.ApiService.Services;

namespace WisdomGuidedAspire.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MilestoneController : ControllerBase
    {
        private readonly MilestoneService _milestoneService;
        private readonly RevenueTrackingService _revenueService;
        private readonly ILogger<MilestoneController> _logger;

        public MilestoneController(
            MilestoneService milestoneService,
            RevenueTrackingService revenueService,
            ILogger<MilestoneController> logger)
        {
            _milestoneService = milestoneService;
            _revenueService = revenueService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllMilestones()
        {
            try
            {
                var milestones = await _milestoneService.GetAllMilestones();
                return Ok(milestones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving milestones");
                return StatusCode(500, new { error = "An error occurred retrieving milestones" });
            }
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMilestoneById(string id)
        {
            try
            {
                var milestone = await _milestoneService.GetMilestoneById(id);
                
                if (milestone == null)
                {
                    return NotFound(new { error = $"Milestone with ID {id} not found" });
                }
                
                return Ok(milestone);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving milestone");
                return StatusCode(500, new { error = "An error occurred retrieving the milestone" });
            }
        }
        
        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteMilestone(string id, [FromBody] Dictionary<string, string> metrics = null)
        {
            try
            {
                var milestone = await _milestoneService.CompleteMilestone(id, metrics);
                return Ok(milestone);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing milestone");
                return StatusCode(500, new { error = "An error occurred completing the milestone" });
            }
        }
        
        [HttpPost("check")]
        public async Task<IActionResult> CheckMilestones()
        {
            try
            {
                await _milestoneService.CheckMilestones(_revenueService);
                var milestones = await _milestoneService.GetAllMilestones();
                return Ok(milestones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking milestones");
                return StatusCode(500, new { error = "An error occurred checking milestones" });
            }
        }
    }
}
```

### 4. Register the Milestone Service

Update `Program.cs` to register the milestone service:

```csharp
// Add this line to the service registration section
builder.Services.AddSingleton<MilestoneService>();
```

## Conclusion

This advanced implementation guide provides a comprehensive approach to building AI-powered applications using the Wisdom-Guided approach with Semantic Kernel AI Agent Framework and .NET Aspire. By following this guide, you can create sophisticated, agent-based solutions that are easy to develop, maintain, and sell.

The key components of this implementation include:

1. **Enhanced Project Structure**: A modular, use case-focused organization that promotes reusability and maintainability.

2. **Advanced Agent Patterns**: Factory pattern for creating agents, enhanced telemetry for monitoring, and feedback loops for continuous improvement.

3. **Revenue Tracking System**: Complete tracking of sales, revenue projections, and progress towards your $10,000-$20,000 goal.

4. **Automated Testing Framework**: Built-in testing capabilities to ensure agent quality and performance.

5. **Client Delivery Package**: Tools for preparing professional delivery packages for clients.

6. **Email Notifications**: Integration with email services for sale confirmations and escalation alerts.

7. **Milestone Tracking**: A system for tracking progress against defined milestones in the 30-day project timeline.

All of these components are built on top of .NET Aspire's cloud-native capabilities, ensuring your applications are observable, scalable, and ready for production.

By leveraging this approach, you'll be well-positioned to achieve your revenue goals while building a foundation for future AI-powered projects.
            try
            {
                _logger.LogInformation("Recording sale: {ProjectType} for ${Amount}", sale.ProjectType, sale.Amount);
                
                var result = await _revenueService.RecordSale(sale);
                
                // Check if we need to perform a milestone check
                bool onTrack = await _revenueService.CheckMilestone();
                
                return Ok(new { 
                    sale = result,
                    onTrack = onTrack
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording sale");
                return StatusCode(500, new { error = "An error occurred recording the sale" });
            }
        }
        
        [HttpGet("summary")]
        public async Task<IActionResult> GetRevenueSummary()
        {
            try
            {
                var summary = await _revenueService.GetRevenueSummary();
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving revenue summary");
                return StatusCode(500, new { error = "An error occurred retrieving the summary" });
            }
        }
        
        [HttpGet("sales")]
        public async Task<IActionResult> GetSales()
        {
            try
            {
                var sales = await _revenueService.GetAllSales();
                return Ok(sales);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sales");
                return StatusCode(500, new { error = "An error occurred retrieving sales" });
            }
        }
        
        [HttpGet("sales/daily")]
        public async Task<IActionResult> GetSalesByDay()
        {
            try
            {
                var salesByDay = await _revenueService.GetSalesByDay();
                return Ok(salesByDay);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving daily sales");
                return StatusCode(500, new { error = "An error occurred retrieving daily sales" });
            }
        }
    }
}
```

### 4. Register the Revenue Service

Update `Program.cs` to register the revenue tracking service:

```csharp
// Add this line to the service registration section
builder.Services.AddSingleton<RevenueTrackingService>();
```

## Automated Testing Framework

Let's implement an automated testing framework to ensure the quality of our AI agents.

### 1. Create Test Models

Create a file `WisdomGuidedAspire.ApiService/Models/TestModels.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace WisdomGuidedAspire.ApiService.Models
{
    public class AgentTest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string AgentType { get; set; }
        public List<TestCase> TestCases { get; set; } = new List<TestCase>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastRunAt { get; set; }
        public TestResult LastResult { get; set; }
    }
    
    public class TestCase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Input { get; set; }
        public List<string> ExpectedOutputContains { get; set; } = new List<string>();
        public List<string> ExpectedOutputNotContains { get; set; } = new List<string>();
        public bool ShouldEscalate { get; set; }
        public int MaxResponseTimeMs { get; set; } = 5000; // Default to 5 seconds
    }
    
    public class TestResult
    {
        public string TestId { get; set; }
        public DateTime RunAt { get; set; } = DateTime.UtcNow;
        public bool Success { get; set; }
        public List<TestCaseResult> CaseResults { get; set; } = new List<TestCaseResult>();
        public int TotalCases => CaseResults.Count;
        public int PassedCases => CaseResults.Count(c => c.Passed);
        public int FailedCases => CaseResults.Count(c => !c.Passed);
        public double SuccessRate => TotalCases > 0 ? (double)PassedCases / TotalCases : 0;
    }
    
    public class TestCaseResult
    {
        public string TestCaseId { get; set; }
        public string TestCaseName { get; set; }
        public bool Passed { get; set; }
        public string ActualOutput { get; set; }
        public List<string> FailedChecks { get; set; } = new List<string>();
        public long ResponseTimeMs { get; set; }
        public bool EscalationCorrect { get; set; }
    }
}
```

### 2. Create Testing Service

Create a service for agent testing in `WisdomGuidedAspire.ApiService/Services/AgentTestingService.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.ChatCompletion;
using WisdomGuidedAspire.ApiService.Models;

namespace WisdomGuidedAspire.ApiService.Services
{
    public class AgentTestingService
    {
        private readonly AgentFactory _agentFactory;
        private readonly WisdomLogger _logger;
        private readonly IDistributedCache _cache;
        private readonly AgentTelemetry _telemetry;
        
        private readonly string _testsCacheKey = "AgentTests";
        
        public AgentTestingService(
            AgentFactory agentFactory,
            WisdomLogger logger,
            IDistributedCache cache,
            AgentTelemetry telemetry)
        {
            _agentFactory = agentFactory;
            _logger = logger;
            _cache = cache;
            _telemetry = telemetry;
        }
        
        public async Task<List<AgentTest>> GetAllTests()
        {
            string testsJson = await _cache.GetStringAsync(_testsCacheKey);
            
            if (string.IsNullOrEmpty(testsJson))
            {
                return new List<AgentTest>();
            }
            
            return System.Text.Json.JsonSerializer.Deserialize<List<AgentTest>>(testsJson);
        }
        
        public async Task<AgentTest> GetTestById(string id)
        {
            var tests = await GetAllTests();
            return tests.FirstOrDefault(t => t.Id == id);
        }
        
        public async Task<AgentTest> GetTestByAgentType(string agentType)
        {
            var tests = await GetAllTests();
            return tests.FirstOrDefault(t => t.AgentType == agentType);
        }
        
        public async Task<AgentTest> SaveTest(AgentTest test)
        {
            var tests = await GetAllTests();
            
            // Find and remove existing test with the same ID if it exists
            int existingIndex = tests.FindIndex(t => t.Id == test.Id);
            if (existingIndex >= 0)
            {
                tests.RemoveAt(existingIndex);
            }
            
            // Add the new/updated test
            tests.Add(test);
            
            // Save the updated tests
            await SaveTests(tests);
            
            // Log the operation
            await _logger.LogActivity(
                "TestSaved",
                $"Saved test for agent type {test.AgentType} with {test.TestCases.Count} test cases",
                $"Test ID: {test.Id}"
            );
            
            return test;
        }
        
        private async Task SaveTests(List<AgentTest> tests)
        {
            string testsJson = System.Text.Json.JsonSerializer.Serialize(tests);
            
            await _cache.SetStringAsync(
                _testsCacheKey,
                testsJson,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
                }
            );
        }
        
        public async Task<TestResult> RunTest(string testId)
        {
            var test = await GetTestById(testId);
            
            if (test == null)
            {
                throw new ArgumentException($"Test with ID {testId} not found");
            }
            
            return await RunTestForAgent(test);
        }
        
        public async Task<TestResult> RunTestForAgentType(string agentType)
        {
            var test = await GetTestByAgentType(agentType);
            
            if (test == null)
            {
                throw new ArgumentException($"No test found for agent type {agentType}");
            }
            
            return await RunTestForAgent(test);
        }
        
        private async Task<TestResult> RunTestForAgent(AgentTest test)
        {
            using var activity = _telemetry.StartAgentActivity(
                test.AgentType, 
                "RunTest", 
                new Dictionary<string, string> { ["test.id"] = test.Id }
            );
            
            var result = new TestResult
            {
                TestId = test.Id
            };
            
            // Create the agent system based on agent type
            var agentChat = await _agentFactory.CreateMultiAgentSystem(
                MapAgentTypeToSystemType(test.AgentType));
                
            // Run each test case
            foreach (var testCase in test.TestCases)
            {
                var caseResult = await RunTestCase(agentChat, testCase, test.AgentType);
                result.CaseResults.Add(caseResult);
            }
            
            // Determine overall success
            result.Success = result.CaseResults.All(c => c.Passed);
            
            // Update the test with the last run result
            test.LastRunAt = DateTime.UtcNow;
            test.LastResult = result;
            await SaveTest(test);
            
            // Log the test run
            await _logger.LogActivity(
                "TestRun",
                $"Ran test for agent type {test.AgentType}: {result.PassedCases}/{result.TotalCases} passed",
                $"Success: {result.Success}, Success Rate: {result.SuccessRate:P}"
            );
            
            return result;
        }
        
        private string MapAgentTypeToSystemType(string agentType)
        {
            return agentType switch
            {
                "CustomerSupportAgent" => "CustomerSupport",
                "LegalSummarizerAgent" => "LegalSummarizer",
                "ContentWriterAgent" => "SocialMediaContentGenerator",
                _ => throw new ArgumentException($"Unknown agent type: {agentType}")
            };
        }
        
        private async Task<TestCaseResult> RunTestCase(AgentChat agentChat, TestCase testCase, string agentType)
        {
            using var activity = _telemetry.StartAgentActivity(
                agentType,
                "RunTestCase",
                new Dictionary<string, string> { ["test.case.id"] = testCase.Id }
            );
            
            var result = new TestCaseResult
            {
                TestCaseId = testCase.Id,
                TestCaseName = testCase.Name
            };
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Create chat history
                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage(testCase.Input);
                
                // Process the query
                string response = "";
                bool needsEscalation = false;
                
                // Use the agent system to process the query
                await foreach (var message in agentChat.InvokeAsync(chatHistory))
                {
                    if (message.Role == AuthorRole.Assistant)
                    {
                        response += message.Content + "\n";
                    }
                    
                    // Check for escalation
                    if (message.Role == AuthorRole.Tool && 
                        message.Name?.Contains("Escalation", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        if (message.Content.Contains("ESCALATE") || 
                            message.Content.Contains("need human assistance"))
                        {
                            needsEscalation = true;
                        }
                    }
                }
                
                stopwatch.Stop();
                result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
                result.ActualOutput = response.Trim();
                result.EscalationCorrect = needsEscalation == testCase.ShouldEscalate;
                
                // Record telemetry
                _telemetry.RecordResponseTime(agentType, result.ResponseTimeMs);
                
                // Validate response
                result.Passed = true;
                
                // Check response time
                if (result.ResponseTimeMs > testCase.MaxResponseTimeMs)
                {
                    result.Passed = false;
                    result.FailedChecks.Add($"Response time {result.ResponseTimeMs}ms exceeds maximum {testCase.MaxResponseTimeMs}ms");
                }
                
                // Check escalation
                if (!result.EscalationCorrect)
                {
                    result.Passed = false;
                    result.FailedChecks.Add($"Escalation incorrect. Expected: {testCase.ShouldEscalate}, Actual: {needsEscalation}");
                }
                
                // Check for required content
                foreach (var requiredContent in testCase.ExpectedOutputContains)
                {
                    if (!result.ActualOutput.Contains(requiredContent, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Passed = false;
                        result.FailedChecks.Add($"Missing required content: '{requiredContent}'");
                    }
                }
                
                // Check for prohibited content
                foreach (var prohibitedContent in testCase.ExpectedOutputNotContains)
                {
                    if (result.ActualOutput.Contains(prohibitedContent, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Passed = false;
                        result.FailedChecks.Add($"Contains prohibited content: '{prohibitedContent}'");
                    }
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
                result.Passed = false;
                result.FailedChecks.Add($"Exception: {ex.Message}");
                
                // Record telemetry
                _telemetry.RecordAgentError(agentType, "TestCaseException");
            }
            
            return result;
        }
        
        public async Task<AgentTest> CreateDefaultTestForAgentType(string agentType)
        {
            var test = new AgentTest
            {
                AgentType = agentType
            };
            
            switch (agentType)
            {
                case "CustomerSupportAgent":
                    test.TestCases.Add(new TestCase
                    {
                        Name = "Order Status Query",
                        Input = "Where is my order #A12345?",
                        ExpectedOutputContains = new List<string> { "processing", "order" },
                        ShouldEscalate = false,
                        MaxResponseTimeMs = 3000
                    });
                    
                    test.TestCases.Add(new TestCase
                    {
                        Name = "Complex Return Request",
                        Input = "I want to return 5 items from different orders and I need a refund immediately to my original payment method.",
                        ExpectedOutputContains = new List<string>(),
                        ShouldEscalate = true,
                        MaxResponseTimeMs = 3000
                    });
                    break;
                    
                case "LegalSummarizerAgent":
                    test.TestCases.Add(new TestCase
                    {
                        Name = "Contract Summary",
                        Input = "This agreement is made between Party A and Party B. Party A agrees to provide consulting services for 12 months. Party B agrees to pay $5,000 per month.",
                        ExpectedOutputContains = new List<string> { "Party A", "Party B", "consulting", "12 months", "$5,000" },
                        ShouldEscalate = false,
                        MaxResponseTimeMs = 3000
                    });
                    break;
                    
                default:
                    throw new ArgumentException($"No default test available for agent type {agentType}");
            }
            
            return await SaveTest(test);
        }
    }
}
```

### 3. Create Test Controller

Create a controller for agent testing in `WisdomGuidedAspire.ApiService/Controllers/TestController.cs`:

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
    public class TestController : ControllerBase
    {
        private readonly AgentTestingService _testingService;
        private readonly ILogger<TestController> _logger;

        public TestController(
            AgentTestingService testingService,
            ILogger<TestController> logger)
        {
            _testingService = testingService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTests()
        {
            try
            {
                var tests = await _testingService.GetAllTests();
                return Ok(tests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tests");
                return StatusCode(500, new { error = "An error occurred retrieving tests" });
            }
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTestById(string id)
        {
            try
            {
                var test = await _testingService.GetTestById(id);
                
                if (test == null)
                {
                    return NotFound(new { error = $"Test with ID {id} not found" });
                }
                
                return Ok(test);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving test");
                return StatusCode(500, new { error = "An error occurred retrieving the test" });
            }
        }
        
        [HttpGet("agent/{agentType}")]
        public async Task<IActionResult> GetTestByAgentType(string agentType)
        {
            try
            {
                var test = await _testingService.GetTestByAgentType(agentType);
                
                if (test == null)
                {
                    return NotFound(new { error = $"No test found for agent type {agentType}" });
                }
                
                return Ok(test);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving test");
                return StatusCode(500, new { error = "An error occurred retrieving the test" });
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> SaveTest([FromBody] AgentTest test)
        {
            try
            {
                var result = await _testingService.SaveTest(test);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving test");
                return StatusCode(500, new { error = "An error occurred saving the test" });
            }
        }
        
        [HttpPost("run/{id}")]
        public async Task<IActionResult> RunTest(string id)
        {
            try
            {
                var result = await _testingService.RunTest(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running test");
                return StatusCode(500, new { error = "An error occurred running the test" });
            }
        }
        
        [HttpPost("run/agent/{agentType}")]
        public async Task<IActionResult> RunTestForAgentType(string agentType)
        {
            try
            {
                var result = await _testingService.RunTestForAgentType(agentType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running test");
                return StatusCode(500, new { error = "An error occurred running the test" });
            }
        }
        
        [HttpPost("create-default/{agentType}")]
        public async Task<IActionResult> CreateDefaultTest(string agentType)
        {
            try
            {
                var test = await _testingService.CreateDefaultTestForAgentType(agentType);
                return Ok(test);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating default test");
                return StatusCode(500, new { error = "An error occurred creating the default test" });
            }
        }
    }
}
```

### 4. Register the Testing Service

Update `Program.cs` to register the testing service:

```csharp
// Add this line to the service registration section
builder.Services.AddSingleton<AgentTestingService>();
```

## Client Delivery Package

When selling your projects on freelance platforms, you'll need to prepare a client delivery package that includes all necessary files and documentation.

### 1. Create Project README Template

Create a README template for client deliveries:

```markdown
# {{ProjectName}} - AI Agent Powered by Semantic Kernel

## Overview

This package contains a {{ProjectType}} built with Microsoft's Semantic Kernel AI Agent Framework. The system uses advanced AI agents to {{ProjectDescription}}.

## Features

{{#Features}}
- {{Name}}: {{Description}}
{{/Features}}

## Requirements

- .NET 8.0 SDK or later
- Azure subscription (for Azure OpenAI)
- API Keys:
  - Azure OpenAI API key
  {{#AdditionalAPIs}}
  - {{Name}} API key
  {{/AdditionalAPIs}}

## Setup Instructions

1. **Clone or extract** this repository to your local machine

2. **Configure API keys**:
   - Open the `appsettings.json` file
   - Update the Azure OpenAI settings:
     ```json
     "AzureOpenAI": {
       "ApiKey": "your-api-key",
       "Endpoint": "https://your-endpoint.openai.azure.com/",
       "DeploymentName": "gpt-4"
     }
     ```
   {{#AdditionalAPIs}}
   - Update the {{Name}} settings:
     ```json
     "{{SettingsKey}}": {
       "ApiKey": "your-api-key"
     }
     ```
   {{/AdditionalAPIs}}

3. **Run the application**:
   ```bash
   cd {{ProjectFolder}}
   dotnet run
   ```

4. **Access the API**:
   - Open a browser and navigate to `https://localhost:5001/swagger` to see available endpoints
   - The main endpoints are:
     {{#Endpoints}}
     - `{{Path}}`: {{Description}}
     {{/Endpoints}}

## Customization

You can customize the agent's behavior by modifying the Prompty files in the `Prompts` folder:

{{#PromptyFiles}}
- `{{Path}}`: {{Description}}
{{/PromptyFiles}}

## Support

This package includes 30 days of support. If you have any questions or need assistance, please contact me through the platform where you purchased this project.

## License

This software is provided for your use only and may not be redistributed. All rights reserved.
```

### 2. Create Deployment Script

Create a script to prepare the client delivery package:

```powershell
# deployment-scripts/prepare-client-package.ps1

param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectName,
    
    [Parameter(Mandatory=$true)]
    [string]$ProjectType,
    
    [Parameter(Mandatory=$true)]
    [string]$OutputDir
)

# Create output directory
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# Create project directory structure
$projectDir = Join-Path $OutputDir $ProjectName
New-Item -ItemType Directory -Path $projectDir -Force | Out-Null

# Copy the necessary files
Copy-Item -Path "WisdomGuidedAspire.ApiService\bin\Release\net8.0\publish\*" -Destination $projectDir -Recurse

# Copy the Prompty files
$promptsDir = Join-Path $projectDir "Prompts"
New-Item -ItemType Directory -Path $promptsDir -Force | Out-Null
Copy-Item -Path "WisdomGuidedAspire.ApiService\Prompts\*" -Destination $promptsDir -Recurse

# Generate a sample appsettings.json with placeholders
$appsettings = @{
    "AzureOpenAI" = @{
        "ApiKey" = "your-api-key"
        "Endpoint" = "https://your-endpoint.openai.azure.com/"
        "DeploymentName" = "gpt-4"
    }
    "Logging" = @{
        "LogLevel" = @{
            "Default" = "Information"
            "Microsoft.AspNetCore" = "Warning"
        }
    }
    "AllowedHosts" = "*"
}

ConvertTo-Json $appsettings -Depth 10 | Out-File (Join-Path $projectDir "appsettings.json")

# Generate README based on project type
$readmeTemplateContent = Get-Content -Path "deployment-scripts/readme-template.md" -Raw

$features = @()
$endpoints = @()
$promptyFiles = @()

switch ($ProjectType) {
    "CustomerSupportChatbot" {
        $projectDescription = "handle customer support queries, provide helpful responses, and escalate complex issues when needed"
        
        $features += @{
            Name = "Multi-Agent System"
            Description = "Uses specialized agents for query validation, response generation, and escalation detection"
        }
        $features += @{
            Name = "Empathetic Responses"
            Description = "Provides friendly, understanding responses to customer queries"
        }
        
        $endpoints += @{
            Path = "api/chatbot/query"
            Description = "Process a customer query"
        }
        
        $promptyFiles += @{
            Path = "Prompts/UseCases/CustomerSupport/CustomerSupportAgentInstructions.txt"
            Description = "Main agent instructions and behavior"
        }
    }
    "LegalDocumentSummarizer" {
        $projectDescription = "analyze legal documents, extract key information, and identify potential risks"
        
        $features += @{
            Name = "Intelligent Summarization"
            Description = "Condenses lengthy legal documents while preserving key information"
        }
        $features += @{
            Name = "Risk Assessment"
            Description = "Identifies potential legal risks and concerns in documents"
        }
        
        $endpoints += @{
            Path = "api/summarizer/summarize"
            Description = "Summarize a legal document"
        }
        
        $promptyFiles += @{
            Path = "Prompts/UseCases/LegalDocuments/LegalSummarizerAgentInstructions.txt"
            Description = "Main summarizer agent instructions"
        }
    }
}

# Replace placeholders in README template
$readmeContent = $readmeTemplateContent
$readmeContent = $readmeContent -replace "{{ProjectName}}", $ProjectName
$readmeContent = $readmeContent -replace "{{ProjectType}}", $ProjectType
$readmeContent = $readmeContent -replace "{{ProjectDescription}}", $projectDescription
$readmeContent = $readmeContent -replace "{{ProjectFolder}}", $ProjectName

# Replace features
$featuresContent = ""
foreach ($feature in $features) {
    $featuresContent += "- $($feature.Name): $($feature.Description)`n"
}
$readmeContent = $readmeContent -replace "{{#Features}}([\s\S]*?){{/Features}}", $featuresContent

# Replace endpoints
$endpointsContent = ""
foreach ($endpoint in $endpoints) {
    $endpointsContent += "- `$($endpoint.Path)`: $($endpoint.Description)`n"
}
$readmeContent = $readmeContent -replace "{{#Endpoints}}([\s\S]*?){{/Endpoints}}", $endpointsContent

# Replace prompty files
$promptyFilesContent = ""
foreach ($file in $promptyFiles) {
    $promptyFilesContent += "- `$($file.Path)`: $($file.Description)`n"
}
$readmeContent = $readmeContent -replace "{{#PromptyFiles}}([\s\S]*?){{/PromptyFiles}}", $promptyFilesContent

# Remove any remaining template markers
$readmeContent = $readmeContent -replace "{{#AdditionalAPIs}}([\s\S]*?){{/AdditionalAPIs}}", ""

# Save README
$readmeContent | Out-File (Join-Path $projectDir "README.md")

# Create a simple startup script
@"
@echo off
echo Starting $ProjectName...
dotnet WisdomGuidedAspire.ApiService.dll
"@ | Out-File (Join-Path $projectDir "start.bat")

# Compress the package
Compress-Archive -Path $projectDir -DestinationPath (Join-Path $OutputDir "$ProjectName.zip") -Force

Write-Host "Client package prepared successfully: $(Join-Path $OutputDir "$ProjectName.zip")"
```

## Extending with Additional Services

As your business grows, you may want to extend the system with additional services. Here's how to integrate them with the Wisdom-Guided approach:

### 1. Email Notification Service

Create a service for sending email notifications in `WisdomGuidedAspire.ApiService/Services/EmailService.cs`:

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace WisdomGuidedAspire.ApiService.Services
{
    public class EmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;
        private readonly WisdomLogger _wisdomLogger;
        
        public EmailService(
            IOptions<EmailSettings> settings,
            ILogger<EmailService> logger,
            WisdomLogger wisdomLogger)
        {
            _settings = settings.Value;
            _logger = logger;
            _wisdomLogger = wisdomLogger;
        }
        
        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false)
        {
            try
            {
                using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
                {
                    EnableSsl = _settings.EnableSsl,
                    Credentials = new NetworkCredential(_settings.Username, _settings.Password)
                };
                
                var message = new MailMessage
                {
                    From = new MailAddress(_settings.FromEmail, _settings.FromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };
                
                message.To.Add(to);
                
                await client.SendMail# Wisdom-Guided Approach: Advanced Implementation and Usage Patterns

This document extends the main implementation guide with advanced patterns, real-world examples, and detailed scenarios for using the Wisdom-Guided approach with the Semantic Kernel AI Agent Framework and .NET Aspire.

## Table of Contents
1. [Building the Customer Support Chatbot](#building-the-customer-support-chatbot)
2. [Creating the Legal Document Summarizer](#creating-the-legal-document-summarizer)
3. [Advanced Agent Patterns](#advanced-agent-patterns)
4. [Revenue Tracking System](#revenue-tracking-system)
5. [Automated Testing Framework](#automated-testing-framework)
6. [Client Delivery Package](#client-delivery-package)
7. [Extending with Additional Services](#extending-with-additional-services)

## Building the Customer Support Chatbot

Here we'll implement a complete Customer Support Chatbot using the Wisdom-Guided approach.

### 1. Define the Chatbot's Requirements

Create a requirements definition file in `WisdomGuidedAspire.ApiService/Models/ChatbotRequirements.cs`:

```csharp
namespace WisdomGuidedAspire.ApiService.Models
{
    public class ChatbotRequirements
    {
        public static ChatbotRequirement[] Requirements => new[]
        {
            new ChatbotRequirement
            {
                Id = "REQ-01",
                Description = "The chatbot must handle customer queries about order status",
                Priority = "High",
                AcceptanceCriteria = "Given an order number, returns the current status"
            },
            new ChatbotRequirement
            {
                Id = "REQ-02",
                Description = "The chatbot must respond with empathy and maintain a friendly tone",
                Priority = "Medium",
                AcceptanceCriteria = "Response includes acknowledgment of customer feeling"
            },
            new ChatbotRequirement
            {
                Id = "REQ-03",
                Description = "The chatbot must escalate complex queries to a human agent",
                Priority = "High",
                AcceptanceCriteria = "Detects when a query is beyond its capabilities and routes to human support"
            },
            new ChatbotRequirement
            {
                Id = "REQ-04",
                Description = "The chatbot must integrate with existing CRM systems via API",
                Priority = "Medium",
                AcceptanceCriteria = "Successfully retrieves customer data from CRM API"
            },
            new ChatbotRequirement
            {
                Id = "REQ-05",
                Description = "The chatbot must have a response time under 2 seconds",
                Priority = "High",
                AcceptanceCriteria = "95% of responses are delivered in under 2 seconds"
            }
        };
    }

    public class ChatbotRequirement
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public string AcceptanceCriteria { get; set; }
    }
}
```

### 2. Create a Specialized Agent Service

Create a new service specifically for the Chatbot in `WisdomGuidedAspire.ApiService/Services/ChatbotService.cs`:

```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WisdomGuidedAspire.ApiService.Services
{
    public class ChatbotService
    {
        private readonly Kernel _kernel;
        private readonly ILogger<ChatbotService> _logger;
        private readonly WisdomLogger _wisdomLogger;
        private readonly IDistributedCache _cache;
        
        public ChatbotService(
            Kernel kernel, 
            ILogger<ChatbotService> logger,
            WisdomLogger wisdomLogger,
            IDistributedCache cache)
        {
            _kernel = kernel;
            _logger = logger;
            _wisdomLogger = wisdomLogger;
            _cache = cache;
        }
        
        public async Task<AgentChat> CreateChatbotAgentSystem()
        {
            // Create the main chatbot agent
            var chatbotAgent = new ChatCompletionAgent
            {
                Name = "CustomerSupportAgent",
                Instructions = await LoadAgentInstructions("CustomerSupportAgent"),
                Kernel = _kernel
            };
            
            // Create the order status agent
            var orderStatusAgent = new ChatCompletionAgent
            {
                Name = "OrderStatusAgent",
                Instructions = await LoadAgentInstructions("OrderStatusAgent"),
                Kernel = _kernel
            };
            
            // Create the escalation agent
            var escalationAgent = new ChatCompletionAgent
            {
                Name = "EscalationAgent",
                Instructions = await LoadAgentInstructions("EscalationAgent"),
                Kernel = _kernel
            };
            
            // Create the agent chat system
            var chat = new AgentChat();
            chat.AddAgent(chatbotAgent);
            chat.AddAgent(orderStatusAgent);
            chat.AddAgent(escalationAgent);
            
            return chat;
        }
        
        private async Task<string> LoadAgentInstructions(string agentName)
        {
            // Try to get from cache first
            string cacheKey = $"AgentInstructions:{agentName}";
            string instructions = await _cache.GetStringAsync(cacheKey);
            
            if (!string.IsNullOrEmpty(instructions))
            {
                _logger.LogInformation("Loaded {Agent} instructions from cache", agentName);
                return instructions;
            }
            
            // Load from file if not in cache
            string filepath = Path.Combine("Prompts", "Agents", $"{agentName}Instructions.txt");
            
            if (!File.Exists(filepath))
            {
                _logger.LogWarning("Instructions file not found for {Agent}", agentName);
                
                // Use default instructions based on agent type
                instructions = agentName switch
                {
                    "CustomerSupportAgent" => "You are a helpful customer support agent. Respond with empathy and assist customers with their inquiries.",
                    "OrderStatusAgent" => "You are an order status specialist. Help customers track their orders and provide updates on shipping and delivery.",
                    "EscalationAgent" => "You determine when a query is too complex and needs human assistance. Identify queries that require escalation.",
                    _ => "You are a helpful assistant."
                };
            }
            else
            {
                instructions = await File.ReadAllTextAsync(filepath);
            }
            
            // Cache the instructions
            await _cache.SetStringAsync(
                cacheKey, 
                instructions,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                });
            
            _logger.LogInformation("Loaded {Agent} instructions from file", agentName);
            return instructions;
        }
        
        public async Task<ChatbotResponse> ProcessCustomerQuery(string query, string customerId = null)
        {
            var stopwatch = Stopwatch.StartNew();
            
            _logger.LogInformation("Processing customer query: {Query}", query);
            
            try
            {
                // Create the agent system
                var agentChat = await CreateChatbotAgentSystem();
                
                // Create chat history
                var chatHistory = new ChatHistory();
                
                // Add context if customer ID is provided
                if (!string.IsNullOrEmpty(customerId))
                {
                    // In a real system, you would look up customer info from a CRM
                    chatHistory.AddSystemMessage($"This is customer ID: {customerId}. They have been a customer for 2 years.");
                }
                
                // Add the user query
                chatHistory.AddUserMessage(query);
                
                // Process the query
                string responseContent = "";
                bool needsEscalation = false;
                string escalationReason = null;
                
                // Use the agent system to process the query
                await foreach (var response in agentChat.InvokeAsync(chatHistory))
                {
                    if (response.Role == AuthorRole.Assistant)
                    {
                        responseContent += response.Content + "\n";
                    }
                    else if (response.Role == AuthorRole.Tool && response.Name == "EscalationAgent")
                    {
                        // Check if the escalation agent indicates the query needs human assistance
                        if (response.Content.Contains("ESCALATE") || response.Content.Contains("need human assistance"))
                        {
                            needsEscalation = true;
                            escalationReason = response.Content;
                        }
                    }
                }
                
                stopwatch.Stop();
                
                // Log the interaction
                await _wisdomLogger.LogActivity(
                    "ChatbotInteraction",
                    $"Query: {query}, Customer ID: {customerId ?? "anonymous"}, Response Time: {stopwatch.ElapsedMilliseconds}ms",
                    $"Escalated: {needsEscalation}, Reason: {escalationReason ?? "N/A"}"
                );
                
                return new ChatbotResponse
                {
                    Content = responseContent.Trim(),
                    NeedsEscalation = needsEscalation,
                    EscalationReason = escalationReason,
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing customer query");
                
                return new ChatbotResponse
                {
                    Content = "I'm sorry, there was an error processing your request. Please try again later.",
                    NeedsEscalation = true,
                    EscalationReason = $"Error: {ex.Message}",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Timestamp = DateTime.UtcNow,
                    Error = ex.Message
                };
            }
        }
    }
    
    public class ChatbotResponse
    {
        public string Content { get; set; }
        public bool NeedsEscalation { get; set; }
        public string EscalationReason { get; set; }
        public long ResponseTimeMs { get; set; }
        public DateTime Timestamp { get; set; }
        public string Error { get; set; }
    }
}
```

### 3. Create Agent Instructions Files

Create the following files for agent instructions:

**CustomerSupportAgentInstructions.txt** in `Prompts/Agents/CustomerSupportAgentInstructions.txt`:
```
You are a helpful and empathetic customer support agent for an e-commerce store. Your role is to assist customers with their queries, especially regarding orders, products, and general information.

GUIDELINES:
- Always respond with empathy and acknowledge customer concerns
- Be concise but thorough in your explanations
- Use a friendly, professional tone
- If you don't know something, admit it rather than making up information
- For order status requests, collaborate with the OrderStatusAgent
- For complex issues beyond your scope, work with the EscalationAgent

COMMON SCENARIOS:
1. Order Status: "Where is my order?" or "When will my order arrive?"
2. Product Information: "Does this product have X feature?" or "What are the dimensions?"
3. Return Policy: "How do I return an item?" or "What's your return policy?"
4. Account Issues: "I can't log in" or "How do I reset my password?"

You can ask clarifying questions when needed, but focus on resolving the customer's issue efficiently.
```

**OrderStatusAgentInstructions.txt** in `Prompts/Agents/OrderStatusAgentInstructions.txt`:
```
You are a specialized agent focusing on order status information. You help customers track their orders and provide updates on shipping and delivery.

CAPABILITIES:
- Identify order number mentions in customer queries
- Provide status updates for orders (simulate looking up real data)
- Estimate delivery dates based on shipping method and location
- Suggest actions for delayed or missing orders

SIMULATED ORDER STATUS RESPONSES:
- For order numbers starting with "A": "Your order has been placed and is being processed."
- For order numbers starting with "B": "Your order has been shipped and is in transit."
- For order numbers starting with "C": "Your order has been delivered."
- For order numbers starting with "D": "There's a delay with your order. Please contact customer service."

If no order number is provided, ask the customer for their order number to provide more specific information.
```

**EscalationAgentInstructions.txt** in `Prompts/Agents/EscalationAgentInstructions.txt`:
```
You are an escalation specialist who determines when a customer query requires human assistance. Your role is to identify complex or sensitive issues that the AI cannot handle appropriately.

ESCALATION CRITERIA:
1. Technical complexity beyond AI capabilities
2. Legal or compliance issues
3. Customer expressing significant frustration or anger
4. Refund requests above standard policy
5. Account security concerns
6. Repeated failure to resolve an issue
7. Explicit request for human assistance

RESPONSE FORMAT:
If escalation is needed, respond with:
"ESCALATE: [reason for escalation]"

If no escalation is needed, respond with:
"NO_ESCALATION: [brief explanation]"

Always err on the side of caution - if you're unsure whether a query needs escalation, recommend escalation.
```

### 4. Create a Chatbot Controller

Create a controller for the chatbot API in `WisdomGuidedAspire.ApiService/Controllers/ChatbotController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WisdomGuidedAspire.ApiService.Services;

namespace WisdomGuidedAspire.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private readonly ChatbotService _chatbotService;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(
            ChatbotService chatbotService,
            ILogger<ChatbotController> logger)
        {
            _chatbotService = chatbotService;
            _logger = logger;
        }

        [HttpPost("query")]
        public async Task<IActionResult> ProcessQuery([FromBody] ChatbotQueryRequest request)
        {
            try
            {
                _logger.LogInformation("Chatbot query received: {Query}", request.Query);
                
                var response = await _chatbotService.ProcessCustomerQuery(request.Query, request.CustomerId);
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chatbot query");
                return StatusCode(500, new { error = "An error occurred processing your request" });
            }
        }
        
        [HttpGet("requirements")]
        public IActionResult GetRequirements()
        {
            return Ok(ChatbotRequirements.Requirements);
        }
    }

    public class ChatbotQueryRequest
    {
        public string Query { get; set; }
        public string CustomerId { get; set; }
    }
}
```

### 5. Register the Chatbot Service

Update `Program.cs` to register the chatbot service:

```csharp
// Add this line to the service registration section
builder.Services.AddSingleton<ChatbotService>();
```

### 6. Create a Marketing Description for the Chatbot

Create a file `Chatbot_Marketing.md` to use when selling the chatbot on freelance platforms:

```markdown
# AI-Powered Customer Support Chatbot

## Description
Transform your customer support experience with our intelligent, empathetic AI chatbot built with the latest Semantic Kernel AI Agent Framework and .NET Aspire. This multi-agent system understands customer queries, provides accurate responses, and knows when to escalate to human agents.

## Key Features
- **Multi-Agent Architecture**: Uses specialized agents for general support, order tracking, and escalation detection
- **Empathetic Responses**: Communicates with customers in a friendly, understanding tone
- **Order Status Tracking**: Provides real-time updates on customer orders
- **Smart Escalation**: Identifies complex issues that need human attention
- **Performance Optimized**: Delivers responses in under 2 seconds
- **Easy Integration**: Connects to your existing systems via API
- **Fully Customizable**: Tailor the chatbot to your brand voice and specific needs

## Technical Details
- Built with Microsoft's Semantic Kernel AI Agent Framework
- Cloud-ready with .NET Aspire architecture
- Includes comprehensive logs and analytics
- Deployable on-premises or in the cloud
- Includes all source code and documentation

## Price: $750

### Delivery includes:
- Complete source code
- Setup instructions
- Customization guide
- 30 days of support

Contact me for a personalized demo or to discuss customization options!
```

## Advanced Agent Patterns

Let's enhance our agent implementation with more sophisticated patterns for flexibility and adaptability.

### 1. Agent Factory Pattern

Create a reusable factory for generating agents in `WisdomGuidedAspire.ApiService/Services/AgentFactory.cs`:

```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace WisdomGuidedAspire.ApiService.Services
{
    public class AgentFactory
    {
        private readonly Kernel _kernel;
        private readonly ILogger<AgentFactory> _logger;
        private readonly IDistributedCache _cache;
        
        public AgentFactory(
            Kernel kernel,
            ILogger<AgentFactory> logger,
            IDistributedCache cache)
        {
            _kernel = kernel;
            _logger = logger;
            _cache = cache;
        }
        
        public async Task<ChatCompletionAgent> CreateAgent(
            string agentType,
            Dictionary<string, string> parameters = null)
        {
            // Get base instructions
            string instructions = await LoadAgentInstructions(agentType);
            
            // Apply dynamic parameters if provided
            if (parameters != null && parameters.Count > 0)
            {
                foreach (var param in parameters)
                {
                    instructions = instructions.Replace($"{{{{{param.Key}}}}}", param.Value);
                }
            }
            
            // Create the agent with appropriate configuration
            var agent = new ChatCompletionAgent
            {
                Name = agentType,
                Instructions = instructions,
                Kernel = _kernel
            };
            
            _logger.LogInformation("Created agent of type {AgentType}", agentType);
            
            return agent;
        }
        
        public async Task<AgentChat> CreateMultiAgentSystem(
            string systemType,
            Dictionary<string, Dictionary<string, string>> agentParameters = null)
        {
            var chat = new AgentChat();
            
            switch (systemType)
            {
                case "CustomerSupport":
                    // Create customer support agents
                    chat.AddAgent(await CreateAgent("CustomerSupportAgent", 
                        agentParameters?.GetValueOrDefault("CustomerSupportAgent")));
                    chat.AddAgent(await CreateAgent("OrderStatusAgent", 
                        agentParameters?.GetValueOrDefault("OrderStatusAgent")));
                    chat.AddAgent(await CreateAgent("EscalationAgent", 
                        agentParameters?.GetValueOrDefault("EscalationAgent")));
                    break;
                    
                case "LegalSummarizer":
                    // Create legal document summarizer agents
                    chat.AddAgent(await CreateAgent("LegalSummarizerAgent", 
                        agentParameters?.GetValueOrDefault("LegalSummarizerAgent")));
                    chat.AddAgent(await CreateAgent("RiskAssessmentAgent", 
                        agentParameters?.GetValueOrDefault("RiskAssessmentAgent")));
                    break;
                    
                case "SocialMediaContentGenerator":
                    // Create content generation agents
                    chat.AddAgent(await CreateAgent("ContentIdeaAgent", 
                        agentParameters?.GetValueOrDefault("ContentIdeaAgent")));
                    chat.AddAgent(await CreateAgent("ContentWriterAgent", 
                        agentParameters?.GetValueOrDefault("ContentWriterAgent")));
                    chat.AddAgent(await CreateAgent("ContentReviewAgent", 
                        agentParameters?.GetValueOrDefault("ContentReviewAgent")));
                    break;
                    
                default:
                    throw new ArgumentException($"Unknown agent system type: {systemType}");
            }
            
            _logger.LogInformation("Created multi-agent system of type {SystemType} with {AgentCount} agents", 
                systemType, chat.Agents.Count);
            
            return chat;
        }
        
        private async Task<string> LoadAgentInstructions(string agentType)
        {
            // Try to get from cache first
            string cacheKey = $"AgentInstructions:{agentType}";
            string instructions = await _cache.GetStringAsync(cacheKey);
            
            if (!string.IsNullOrEmpty(instructions))
            {
                return instructions;
            }
            
            // Determine the correct path based on agent type
            string useCase = DetermineUseCase(agentType);
            string filePath = Path.Combine("Prompts", "UseCases", useCase, $"{agentType}Instructions.txt");
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Instructions file not found at {FilePath}", filePath);
                
                // Fallback to the Agents folder
                filePath = Path.Combine("Prompts", "Agents", $"{agentType}Instructions.txt");
                
                if (!File.Exists(filePath))
                {
                    _logger.LogError("No instructions file found for {AgentType}", agentType);
                    return $"You are a helpful {agentType}.";
                }
            }
            
            // Load instructions from file
            instructions = await File.ReadAllTextAsync(filePath);
            
            // Cache the instructions
            await _cache.SetStringAsync(
                cacheKey, 
                instructions,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                });
            
            return instructions;
        }
        
        private string DetermineUseCase(string agentType)
        {
            // Map agent types to use cases
            return agentType switch
            {
                "CustomerSupportAgent" or "OrderStatusAgent" or "EscalationAgent" 
                    => "CustomerSupport",
                    
                "LegalSummarizerAgent" or "RiskAssessmentAgent" 
                    => "LegalDocuments",
                    
                "ContentIdeaAgent" or "ContentWriterAgent" or "ContentReviewAgent" 
                    => "SocialMedia",
                    
                _ => "Agents" // Default to the general Agents folder
            };
        }
    }
}
```

### 2. Enhanced Agent Telemetry

Create a service for custom agent telemetry in `WisdomGuidedAspire.ApiService/Services/AgentTelemetry.cs`:

```csharp
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Collections.Generic;

namespace WisdomGuidedAspire.ApiService.Services
{
    public class AgentTelemetry
    {
        private static readonly ActivitySource _activitySource = new("WisdomGuidedAspire.Agents");
        private static readonly Meter _meter = new("WisdomGuidedAspire.Agents", "1.0");
        
        // Counters
        private readonly Counter<int> _agentInvocationsCounter;
        private readonly Counter<int> _agentErrorsCounter;
        private readonly Counter<int> _escalationsCounter;
        
        // Histograms
        private readonly Histogram<double> _responseTimeHistogram;
        private readonly Histogram<double> _tokenUsageHistogram;
        
        private readonly ILogger<AgentTelemetry> _logger;
        
        public AgentTelemetry(ILogger<AgentTelemetry> logger)
        {
            _logger = logger;
            
            // Initialize metrics
            _agentInvocationsCounter = _meter.CreateCounter<int>(
                "agent_invocations_total", 
                "count", 
                "Total number of agent invocations");
                
            _agentErrorsCounter = _meter.CreateCounter<int>(
                "agent_errors_total", 
                "count", 
                "Total number of agent errors");
                
            _escalationsCounter = _meter.CreateCounter<int>(
                "agent_escalations_total", 
                "count", 
                "Total number of escalations to human agents");
                
            _responseTimeHistogram = _meter.CreateHistogram<double>(
                "agent_response_time", 
                "ms", 
                "Response time in milliseconds");
                
            _tokenUsageHistogram = _meter.CreateHistogram<double>(
                "agent_token_usage", 
                "tokens", 
                "Token usage per agent invocation");
        }
        
        public Activity StartAgentActivity(string agentType, string operationType, Dictionary<string, string> tags = null)
        {
            var activity = _activitySource.StartActivity(operationType);
            
            if (activity != null)
            {
                activity.SetTag("agent.type", agentType);
                activity.SetTag("operation.type", operationType);
                
                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        activity.SetTag(tag.Key, tag.Value);
                    }
                }
            }
            
            _logger.LogDebug("Started activity {Operation} for agent {AgentType}", operationType, agentType);
            
            return activity;
        }
        
        public void RecordAgentInvocation(string agentType)
        {
            _agentInvocationsCounter.Add(1, new KeyValuePair<string, object>("agent.type", agentType));
            _logger.LogDebug("Recorded invocation for agent {AgentType}", agentType);
        }
        
        public void RecordAgentError(string agentType, string errorType)
        {
            _agentErrorsCounter.Add(1, new[] { 
                new KeyValuePair<string, object>("agent.type", agentType),
                new KeyValuePair<string, object>("error.type", errorType)
            });
            
            _logger.LogDebug("Recorded error for agent {AgentType}: {ErrorType}", agentType, errorType);
        }
        
        public void RecordEscalation(string agentType, string reason)
        {
            _escalationsCounter.Add(1, new[] {
                new KeyValuePair<string, object>("agent.type", agentType),
                new KeyValuePair<string, object>("reason", reason)
            });
            
            _logger.LogDebug("Recorded escalation for agent {AgentType}: {Reason}", agentType, reason);
        }
        
        public void RecordResponseTime(string agentType, double milliseconds)
        {
            _responseTimeHistogram.Record(milliseconds, new KeyValuePair<string, object>("agent.type", agentType));
            _logger.LogDebug("Recorded response time for agent {AgentType}: {ResponseTime}ms", agentType, milliseconds);
        }
        
        public void RecordTokenUsage(string agentType, double tokens)
        {
            _tokenUsageHistogram.Record(tokens, new KeyValuePair<string, object>("agent.type", agentType));
            _logger.LogDebug("Recorded token usage for agent {AgentType}: {TokenCount} tokens", agentType, tokens);
        }
    }
}
```

### 3. Agent Feedback Loop

Implement a feedback mechanism for agent improvement in `WisdomGuidedAspire.ApiService/Services/AgentFeedback.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WisdomGuidedAspire.ApiService.Services
{
    public class AgentFeedback
    {
        private readonly WisdomLogger _logger;
        private readonly IDistributedCache _cache;
        
        // Feedback schema
        public class FeedbackEntry
        {
            public string AgentType { get; set; }
            public string UserQuery { get; set; }
            public string AgentResponse { get; set; }
            public FeedbackRating Rating { get; set; }
            public string Comments { get; set; }
            public DateTime Timestamp { get; set; }
        }
        
        public enum FeedbackRating
        {
            Poor = 1,
            Fair = 2,
            Good = 3,
            Excellent = 4
        }
        
        public AgentFeedback(WisdomLogger logger, IDistributedCache cache)
        {
            _logger = logger;
            _cache = cache;
        }
        
        public async Task RecordFeedback(FeedbackEntry feedback)
        {
            // Log the feedback
            await _logger.LogActivity(
                "AgentFeedback",
                $"Agent: {feedback.AgentType}, Rating: {feedback.Rating}",
                $"Comments: {feedback.Comments}"
            );
            
            // Store feedback for learning
            await StoreAgentFeedback(feedback);
        }
        
        private async Task StoreAgentFeedback(FeedbackEntry feedback)
        {
            // Get existing feedback for this agent
            string cacheKey = $"AgentFeedback:{feedback.AgentType}";
            string feedbackJson = await _cache.GetStringAsync(cacheKey);
            
            List<FeedbackEntry> feedbackList;
            
            if (string.IsNullOrEmpty(feedbackJson))
            {
                feedbackList = new List<FeedbackEntry>();
            }
            else
            {
                feedbackList = System.Text.Json.JsonSerializer.Deserialize<List<FeedbackEntry>>(feedbackJson);
            }
            
            // Add new feedback
            feedbackList.Add(feedback);
            
            // Store updated feedback
            string updatedJson = System.Text.Json.JsonSerializer.Serialize(feedbackList);
            await _cache.SetStringAsync(
                cacheKey,
                updatedJson,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
                }
            );
        }
        
        public async Task<List<FeedbackEntry>> GetAgentFeedback(string agentType)
        {
            string cacheKey = $"AgentFeedback:{agentType}";
            string feedbackJson = await _cache.GetStringAsync(cacheKey);
            
            if (string.IsNullOrEmpty(feedbackJson))
            {
                return new List<FeedbackEntry>();
            }
            
            return System.Text.Json.JsonSerializer.Deserialize<List<FeedbackEntry>>(feedbackJson);
        }
        
        public async Task<Dictionary<string, object>> GetAgentFeedbackMetrics(string agentType)
        {
            var feedback = await GetAgentFeedback(agentType);
            
            if (feedback.Count == 0)
            {
                return new Dictionary<string, object>
                {
                    ["count"] = 0,
                    ["averageRating"] = 0.0,
                    ["ratingDistribution"] = new Dictionary<FeedbackRating, int>()
                };
            }
            
            // Calculate metrics
            double averageRating = feedback.Average(f => (int)f.Rating);
            
            var ratingDistribution = feedback
                .GroupBy(f => f.Rating)
                .ToDictionary(g => g.Key, g => g.Count());
            
            return new Dictionary<string, object>
            {
                ["count"] = feedback.Count,
                ["averageRating"] = averageRating,
                ["ratingDistribution"] = ratingDistribution
            };
        }
        
        public async Task<bool> ShouldOptimizeAgent(string agentType)
        {
            var metrics = await GetAgentFeedbackMetrics(agentType);
            
            if ((int)metrics["count"] < 5)
            {
                // Not enough feedback yet
                return false;
            }
            
            // Check if average rating is below threshold
            double averageRating = (double)metrics["averageRating"];
            if (averageRating < 2.5)
            {
                return true;
            }
            
            return false;
        }
    }
}
```

### 4. Update the Controller for Feedback

Add a feedback endpoint to the agent controller:

```csharp
[HttpPost("feedback")]
public async Task<IActionResult> SubmitFeedback([FromBody] AgentFeedbackRequest request)
{
    try
    {
        var feedback = new AgentFeedback.FeedbackEntry
        {
            AgentType = request.AgentType,
            UserQuery = request.UserQuery,
            AgentResponse = request.AgentResponse,
            Rating = request.Rating,
            Comments = request.Comments,
            Timestamp = DateTime.UtcNow
        };
        
        await _agentFeedback.RecordFeedback(feedback);
        
        // Check if we should optimize the agent based on feedback
        bool shouldOptimize = await _agentFeedback.ShouldOptimizeAgent(request.AgentType);
        
        return Ok(new { 
            received = true,
            shouldOptimize = shouldOptimize
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing agent feedback");
        return StatusCode(500, new { error = "An error occurred processing your feedback" });
    }
}

public class AgentFeedbackRequest
{
    public string AgentType { get; set; }
    public string UserQuery { get; set; }
    public string AgentResponse { get; set; }
    public AgentFeedback.FeedbackRating Rating { get; set; }
    public string Comments { get; set; }
}
```

## Revenue Tracking System

To ensure you can track progress towards your $10,000-$20,000 goal, let's implement a revenue tracking system.

### 1. Create Revenue Models

Create a new file `WisdomGuidedAspire.ApiService/Models/RevenueModels.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace WisdomGuidedAspire.ApiService.Models
{
    public class SaleRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ProjectType { get; set; } // e.g., "Chatbot", "Summarizer"
        public decimal Amount { get; set; }
        public string ClientPlatform { get; set; } // e.g., "Upwork", "Fiverr", "Direct"
        public string ClientName { get; set; }
        public DateTime SaleDate { get; set; } = DateTime.UtcNow;
        public List<string> Features { get; set; } = new List<string>();
        public string Notes { get; set; }
    }
    
    public class RevenueSummary
    {
        public decimal TotalRevenue { get; set; }
        public decimal RevenueGoalMin { get; set; }
        public decimal RevenueGoalMax { get; set; }
        public decimal GoalCompletionPercentageMin => TotalRevenue / RevenueGoalMin * 100;
        public decimal GoalCompletionPercentageMax => TotalRevenue / RevenueGoalMax * 100;
        public int TotalSales { get; set; }
        public Dictionary<string, int> SalesByProjectType { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, decimal> RevenueByProjectType { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, int> SalesByPlatform { get; set; } = new Dictionary<string, int>();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DaysElapsed => (int)(DateTime.UtcNow - StartDate).TotalDays;
        public int DaysRemaining => (int)(EndDate - DateTime.UtcNow).TotalDays;
        public decimal AverageDailyRevenue => DaysElapsed > 0 ? TotalRevenue / DaysElapsed : 0;
        public decimal ProjectedTotalRevenue => AverageDailyRevenue * (DaysElapsed + DaysRemaining);
        public bool OnTrackForMinGoal => ProjectedTotalRevenue >= RevenueGoalMin;
        public bool OnTrackForMaxGoal => ProjectedTotalRevenue >= RevenueGoalMax;
    }
    
    public class SalesByDay
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
        public decimal Revenue { get; set; }
    }
}
```

### 2. Create Revenue Tracking Service

Create a revenue tracking service in `WisdomGuidedAspire.ApiService/Services/RevenueTrackingService.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WisdomGuidedAspire.ApiService.Models;

namespace WisdomGuidedAspire.ApiService.Services
{
    public class RevenueTrackingService
    {
        private readonly WisdomLogger _logger;
        private readonly ContractService _contractService;
        private readonly IDistributedCache _cache;
        private readonly string _salesCacheKey = "Sales:Records";
        
        public RevenueTrackingService(
            WisdomLogger logger,
            ContractService contractService,
            IDistributedCache cache)
        {
            _logger = logger;
            _contractService = contractService;
            _cache = cache;
        }
        
        public async Task<SaleRecord> RecordSale(SaleRecord sale)
        {
            // Get existing sales
            var sales = await GetAllSales();
            
            // Add new sale
            sales.Add(sale);
            
            // Save updated sales
            await SaveSales(sales);
            
            // Log the sale
            await _logger.LogActivity(
                "Sale",
                $"Recorded sale of {sale.ProjectType} for ${sale.Amount} to {sale.ClientName} on {sale.ClientPlatform}",
                $"Sale ID: {sale.Id}, Features: {string.Join(", ", sale.Features)}"
            );
            
            return sale;
        }
        
        public async Task<List<SaleRecord>> GetAllSales()
        {
            string salesJson = await _cache.GetStringAsync(_salesCacheKey);
            
            if (string.IsNullOrEmpty(salesJson))
            {
                return new List<SaleRecord>();
            }
            
            return System.Text.Json.JsonSerializer.Deserialize<List<SaleRecord>>(salesJson);
        }
        
        private async Task SaveSales(List<SaleRecord> sales)
        {
            string salesJson = System.Text.Json.JsonSerializer.Serialize(sales);
            
            await _cache.SetStringAsync(
                _salesCacheKey,
                salesJson,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(60)
                }
            );
        }
        
        public async Task<RevenueSummary> GetRevenueSummary()
        {
            var sales = await GetAllSales();
            var contract = _contractService.GetContract();
            
            var summary = new RevenueSummary
            {
                TotalRevenue = sales.Sum(s => s.Amount),
                TotalSales = sales.Count,
                RevenueGoalMin = contract.RevenueGoalMin,
                RevenueGoalMax = contract.RevenueGoalMax,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate
            };
            
            // Calculate sales by project type
            summary.SalesByProjectType = sales
                .GroupBy(s => s.ProjectType)
                .ToDictionary(g => g.Key, g => g.Count());
                
            // Calculate revenue by project type
            summary.RevenueByProjectType = sales
                .GroupBy(s => s.ProjectType)
                .ToDictionary(g => g.Key, g => g.Sum(s => s.Amount));
                
            // Calculate sales by platform
            summary.SalesByPlatform = sales
                .GroupBy(s => s.ClientPlatform)
                .ToDictionary(g => g.Key, g => g.Count());
            
            return summary;
        }
        
        public async Task<List<SalesByDay>> GetSalesByDay()
        {
            var sales = await GetAllSales();
            var contract = _contractService.GetContract();
            
            // Group sales by day
            var salesByDay = sales
                .GroupBy(s => s.SaleDate.Date)
                .Select(g => new SalesByDay
                {
                    Date = g.Key,
                    Count = g.Count(),
                    Revenue = g.Sum(s => s.Amount)
                })
                .OrderBy(s => s.Date)
                .ToList();
                
            // Fill in days with no sales
            var allDays = new List<SalesByDay>();
            var currentDate = contract.StartDate.Date;
            var endDate = DateTime.UtcNow.Date;
            
            while (currentDate <= endDate)
            {
                var dayData = salesByDay.FirstOrDefault(s => s.Date == currentDate);
                
                if (dayData == null)
                {
                    allDays.Add(new SalesByDay
                    {
                        Date = currentDate,
                        Count = 0,
                        Revenue = 0
                    });
                }
                else
                {
                    allDays.Add(dayData);
                }
                
                currentDate = currentDate.AddDays(1);
            }
            
            return allDays;
        }
        
        public async Task<bool> CheckMilestone()
        {
            var summary = await GetRevenueSummary();
            var contract = _contractService.GetContract();
            
            // Check if we're at day 15
            int daysElapsed = (int)(DateTime.UtcNow - contract.StartDate).TotalDays;
            
            if (daysElapsed == 15)
            {
                // Mid-point milestone check
                bool onTrack = summary.OnTrackForMinGoal;
                
                await _logger.LogActivity(
                    "MilestoneCheck",
                    $"Day 15 milestone check: Revenue ${summary.TotalRevenue} of ${contract.RevenueGoalMin}-${contract.RevenueGoalMax}",
                    $"On track: {onTrack}, Projected total: ${summary.ProjectedTotalRevenue}"
                );
                
                return onTrack;
            }
            
            // Check after every 5 sales
            if (summary.TotalSales > 0 && summary.TotalSales % 5 == 0)
            {
                bool onTrack = summary.OnTrackForMinGoal;
                
                await _logger.LogActivity(
                    "MilestoneCheck",
                    $"Sales milestone check ({summary.TotalSales} sales): Revenue ${summary.TotalRevenue} of ${contract.RevenueGoalMin}-${contract.RevenueGoalMax}",
                    $"On track: {onTrack}, Projected total: ${summary.ProjectedTotalRevenue}"
                );
                
                return onTrack;
            }
            
            return true; // No milestone check needed
        }
    }
}
```

### 3. Create Revenue Controller

Create a controller for revenue tracking in `WisdomGuidedAspire.ApiService/Controllers/RevenueController.cs`:

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
    public class RevenueController : ControllerBase
    {
        private readonly RevenueTrackingService _revenueService;
        private readonly ILogger<RevenueController> _logger;

        public RevenueController(
            RevenueTrackingService revenueService,
            ILogger<RevenueController> logger)
        {
            _revenueService = revenueService;
            _logger = logger;
        }

        [HttpPost("sales")]
        public async Task<IActionResult> RecordSale([FromBody] SaleRecord sale)
        {
             Includes all source code and documentation

## Price: $500

### Delivery includes:
- Complete source code
- Setup instructions
- Customization guide
- 30 days of support

Contact me for a personalized demo or to discuss customization options!
```

## Enhanced Project Structure

Based on the optimization suggestions, let's refine our project structure for better modularity and reusability:

```
WisdomGuidedAspire/
├── WisdomGuidedAspire.AppHost/              # Orchestration project
├── WisdomGuidedAspire.ServiceDefaults/      # Shared service configurations
├── WisdomGuidedAspire.ApiService/           # API service project
│   ├── Controllers/                         # API controllers
│   ├── Services/                            # Business logic services
│   ├── Models/                              # Data models
│   ├── Prompts/                             # Prompty files
│   │   ├── UseCases/                        # Use case specific prompts
│   │   │   ├── CustomerSupport/             # Customer support specific prompts
│   │   │   ├── LegalDocuments/              # Legal document specific prompts
│   │   │   └── SocialMedia/                 # Social media content generation prompts
│   │   ├── Configurations/                  # Configuration prompts
│   │   ├── Templates/                       # Base templates
│   │   └── Agents/                          # Agent configuration prompts
│   └── Logs/                                # Store logs for tracking
└── WisdomGuidedAspire.Web/                 # Web frontend project
```

Creating this structure will help with scaling the solution and making components more reusable. Let's implement this with PowerShell:

```powershell
# Create use case folders
cd WisdomGuidedAspire.ApiService
mkdir -p Prompts/UseCases/CustomerSupport
mkdir -p Prompts/UseCases/LegalDocuments
mkdir -p Prompts/UseCases/SocialMedia
```

## Creating the Legal Document Summarizer

Now let's implement the Legal Document Summarizer:

### 1. Define the Summarizer Requirements

Create a requirements file in `WisdomGuidedAspire.ApiService/Models/SummarizerRequirements.cs`:

```csharp
namespace WisdomGuidedAspire.ApiService.Models
{
    public class SummarizerRequirements
    {
        public static SummarizerRequirement[] Requirements => new[]
        {
            new SummarizerRequirement
            {
                Id = "REQ-01",
                Description = "The summarizer must accept legal documents in plain text, PDF, or DOCX format",
                Priority = "High",
                AcceptanceCriteria = "Successfully processes all three file formats"
            },
            new SummarizerRequirement
            {
                Id = "REQ-02",
                Description = "The summarizer must identify key legal points and clauses",
                Priority = "High",
                AcceptanceCriteria = "Extracts at least 90% of critical legal terms and conditions"
            },
            new SummarizerRequirement
            {
                Id = "REQ-03",
                Description = "The summarizer must generate summaries in adjustable lengths (brief, medium, detailed)",
                Priority = "Medium",
                AcceptanceCriteria = "Produces coherent summaries at all three detail levels"
            },
            new SummarizerRequirement
            {
                Id = "REQ-04",
                Description = "The summarizer must highlight potential legal risks or concerns",
                Priority = "High",
                AcceptanceCriteria = "Identifies legal risks that have been pre-annotated in test documents"
            },
            new SummarizerRequirement
            {
                Id = "REQ-05",
                Description = "The summarizer must process documents up to 50 pages in length",
                Priority = "Medium",
                AcceptanceCriteria = "Successfully processes documents of 50 pages within 5 minutes"
            }
        };
    }

    public class SummarizerRequirement
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public string AcceptanceCriteria { get; set; }
    }
}
```

### 2. Create a Specialized Summarizer Service

Create a new service for document summarization in `WisdomGuidedAspire.ApiService/Services/SummarizerService.cs`:

```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;

namespace WisdomGuidedAspire.ApiService.Services
{
    public class SummarizerService
    {
        private readonly Kernel _kernel;
        private readonly ILogger<SummarizerService> _logger;
        private readonly WisdomLogger _wisdomLogger;
        
        public SummarizerService(
            Kernel kernel,
            ILogger<SummarizerService> logger,
            WisdomLogger wisdomLogger)
        {
            _kernel = kernel;
            _logger = logger;
            _wisdomLogger = wisdomLogger;
        }
        
        public async Task<AgentChat> CreateSummarizerAgentSystem()
        {
            // Create the main summarizer agent
            var summarizerAgent = new ChatCompletionAgent
            {
                Name = "LegalSummarizerAgent",
                Instructions = await LoadAgentInstructions("LegalSummarizerAgent"),
                Kernel = _kernel
            };
            
            // Create the risk assessment agent
            var riskAgent = new ChatCompletionAgent
            {
                Name = "RiskAssessmentAgent",
                Instructions = await LoadAgentInstructions("RiskAssessmentAgent"),
                Kernel = _kernel
            };
            
            // Create the agent chat system
            var chat = new AgentChat();
            chat.AddAgent(summarizerAgent);
            chat.AddAgent(riskAgent);
            
            return chat;
        }
        
        private async Task<string> LoadAgentInstructions(string agentName)
        {
            // Load from file
            string filepath = Path.Combine("Prompts", "Agents", $"{agentName}Instructions.txt");
            
            if (!File.Exists(filepath))
            {
                _logger.LogWarning("Instructions file not found for {Agent}", agentName);
                
                // Use default instructions based on agent type
                return agentName switch
                {
                    "LegalSummarizerAgent" => "You are a legal document summarizer. Extract and summarize key information from legal documents.",
                    "RiskAssessmentAgent" => "You identify potential legal risks and concerns in documents. Highlight areas that require attention.",
                    _ => "You are a helpful assistant."
                };
            }
            
            return await File.ReadAllTextAsync(filepath);
        }
        
        public async Task<SummarizerResponse> SummarizeDocument(string documentText, SummaryLength length)
        {
            var stopwatch = Stopwatch.StartNew();
            
            _logger.LogInformation("Summarizing document of length {Length} characters", documentText.Length);
            
            try
            {
                // Create the agent system
                var agentChat = await CreateSummarizerAgentSystem();
                
                // Create chat history
                var chatHistory = new ChatHistory();
                
                // Add instructions based on desired length
                string lengthInstructions = length switch
                {
                    SummaryLength.Brief => "Create a brief summary (3-5 sentences) highlighting only the most critical points.",
                    SummaryLength.Medium => "Create a medium-length summary (1-2 paragraphs) covering the main points and key details.",
                    SummaryLength.Detailed => "Create a detailed summary covering all significant points while condensing the original text by at least 70%.",
                    _ => "Create a medium-length summary of the document."
                };
                
                // Add system message
                chatHistory.AddSystemMessage($"Please summarize the following legal document. {lengthInstructions}");
                
                // Add the document
                // Note: In a real implementation, you'd need to handle token limits by chunking the document
                chatHistory.AddUserMessage(documentText);
                
                // Process the document
                string summary = "";
                List<string> risks = new List<string>();
                
                // Use the agent system to process the query
                await foreach (var response in agentChat.InvokeAsync(chatHistory))
                {
                    if (response.Role == AuthorRole.Assistant && response.Name == "LegalSummarizerAgent")
                    {
                        summary += response.Content;
                    }
                    else if (response.Role == AuthorRole.Assistant && response.Name == "RiskAssessmentAgent")
                    {
                        // Parse risks from the response
                        var riskLines = response.Content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in riskLines)
                        {
                            if (line.Contains("RISK:") || line.Contains("Risk:") || line.Contains("- Risk"))
                            {
                                risks.Add(line.Trim());
                            }
                        }
                    }
                }
                
                stopwatch.Stop();
                
                // Log the interaction
                await _wisdomLogger.LogActivity(
                    "DocumentSummarization",
                    $"Document Length: {documentText.Length} chars, Summary Length: {summary.Length} chars, Risks Identified: {risks.Count}",
                    $"Processing Time: {stopwatch.ElapsedMilliseconds}ms, Summary Type: {length}"
                );
                
                return new SummarizerResponse
                {
                    Summary = summary.Trim(),
                    Risks = risks.ToArray(),
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    Timestamp = DateTime.UtcNow,
                    OriginalLength = documentText.Length,
                    SummaryLength = summary.Length,
                    CompressionRatio = documentText.Length > 0 
                        ? (float)summary.Length / documentText.Length 
                        : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error summarizing document");
                
                return new SummarizerResponse
                {
                    Summary = "Error: Could not summarize the document.",
                    Risks = new[] { "Error during processing" },
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    Timestamp = DateTime.UtcNow,
                    Error = ex.Message
                };
            }
        }
    }
    
    public enum SummaryLength
    {
        Brief,
        Medium,
        Detailed
    }
    
    public class SummarizerResponse
    {
        public string Summary { get; set; }
        public string[] Risks { get; set; }
        public long ProcessingTimeMs { get; set; }
        public DateTime Timestamp { get; set; }
        public int OriginalLength { get; set; }
        public int SummaryLength { get; set; }
        public float CompressionRatio { get; set; }
        public string Error { get; set; }
    }
}
```

### 3. Create Agent Instructions Files

Create the following files for agent instructions:

**LegalSummarizerAgentInstructions.txt** in `Prompts/Agents/LegalSummarizerAgentInstructions.txt`:
```
You are a specialized legal document summarizer. Your role is to analyze legal documents and extract the most important information in a clear, concise format.

GUIDELINES:
- Focus on key legal points, terms, conditions, and obligations
- Maintain the legally significant meaning while reducing length
- Use plain language where possible, but preserve necessary legal terminology
- Organize the summary logically, typically following the structure of the original document
- Be comprehensive yet concise

SUMMARY STRUCTURE:
1. Document Type and Parties: Identify the type of legal document and the parties involved
2. Purpose: Explain the main purpose of the document
3. Key Terms: List and explain the most important provisions
4. Obligations: Outline what each party must do
5. Conditions: Note any important conditions or contingencies
6. Timeframes: Mention relevant dates or deadlines
7. Consequences: Explain what happens if terms are not met

Your summary should be accurate, objective, and balanced, ensuring no important legal details are omitted.
```

**RiskAssessmentAgentInstructions.txt** in `Prompts/Agents/RiskAssessmentAgentInstructions.txt`:
```
You are a legal risk assessment specialist. Your role is to identify potential legal risks, ambiguities, or concerns in legal documents.

FOCUS AREAS:
1. Vague or Ambiguous Language: Identify terms that could be interpreted in multiple ways
2. Missing Elements: Note any standard clauses or elements that appear to be missing
3. Unbalanced Terms: Highlight provisions that heavily favor one party
4. Regulatory Concerns: Flag terms that may conflict with known regulations or laws
5. Enforcement Issues: Identify terms that may be difficult to enforce
6. Liability Exposure: Note areas of significant liability risk
7. Inconsistencies: Point out contradictions within the document

FORMAT YOUR RESPONSE:
For each risk identified, use the following format:
RISK: [Brief description of the risk]
LOCATION: [Where in the document the risk appears]
CONCERN: [Why this is problematic]
SUGGESTION: [Possible way to address the risk]

Focus on the most significant risks rather than minor issues. Be thorough but practical in your assessment.
```

### 4. Create a Summarizer Controller

Create a controller for the summarizer API in `WisdomGuidedAspire.ApiService/Controllers/SummarizerController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WisdomGuidedAspire.ApiService.Services;

namespace WisdomGuidedAspire.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SummarizerController : ControllerBase
    {
        private readonly SummarizerService _summarizerService;
        private readonly ILogger<SummarizerController> _logger;

        public SummarizerController(
            SummarizerService summarizerService,
            ILogger<SummarizerController> logger)
        {
            _summarizerService = summarizerService;
            _logger = logger;
        }

        [HttpPost("summarize")]
        public async Task<IActionResult> SummarizeDocument([FromBody] SummarizeRequest request)
        {
            try
            {
                _logger.LogInformation("Document summarization request received, length: {Length} chars", 
                    request.DocumentText.Length);
                
                var response = await _summarizerService.SummarizeDocument(
                    request.DocumentText, 
                    request.Length);
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error summarizing document");
                return StatusCode(500, new { error = "An error occurred processing your request" });
            }
        }
        
        [HttpGet("requirements")]
        public IActionResult GetRequirements()
        {
            return Ok(SummarizerRequirements.Requirements);
        }
    }

    public class SummarizeRequest
    {
        public string DocumentText { get; set; }
        public SummaryLength Length { get; set; } = SummaryLength.Medium;
    }
}
```

### 5. Register the Summarizer Service

Update `Program.cs` to register the summarizer service:

```csharp
// Add this line to the service registration section
builder.Services.AddSingleton<SummarizerService>();
```

### 6. Create a Marketing Description for the Summarizer

Create a file `Summarizer_Marketing.md` to use when selling the summarizer on freelance platforms:

```markdown
# AI-Powered Legal Document Summarizer

## Description
Save hours of reading and analysis with our intelligent Legal Document Summarizer. Built with the cutting-edge Semantic Kernel AI Agent Framework and .NET Aspire, this tool quickly extracts the most important information from contracts, agreements, and legal documents while identifying potential risks.

## Key Features
- **Intelligent Summarization**: Condenses lengthy legal documents while preserving key information
- **Risk Assessment**: Identifies potential legal risks and concerns
- **Multiple Summary Lengths**: Choose brief, medium, or detailed summaries based on your needs
- **Legal Terminology Preservation**: Maintains important legal language while making documents accessible
- **Fast Processing**: Handles documents up to 50 pages in minutes
- **Format Support**: Works with plain text, PDF, and DOCX files

## Technical Details
- Built with Microsoft's Semantic Kernel AI Agent Framework
- Cloud-ready with .NET Aspire architecture
- Processes documents securely with data privacy in mind
- Deployable on-premises or in the cloud
-