

## Running the Application

To run the application with .NET Aspire:

```powershell
# Navigate to the AppHost project
cd WisdomGuidedAspire.AppHost

# Run the application
dotnet run
```

This will start the .NET Aspire dashboard and launch your application. The dashboard provides a comprehensive view of your services, including:

- Health status of each service
- Logs from all services in one place
- Traces for distributed operations
- Metrics for performance monitoring

## Production Considerations

While .NET Aspire is primarily focused on the development experience, you'll need to consider how to deploy your application to production. Here are some recommendations:

### Containerization

Package your services as containers for deployment:

```powershell
# Build container images
dotnet publish WisdomGuidedAspire.ApiService -c Release -o ./publish
docker build -t wisdomguided-api:latest ./publish
```

### Kubernetes Deployment

For a production environment, consider deploying to Kubernetes:

1. Create Kubernetes manifests for your services:

```yaml
# api-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: wisdomguided-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: wisdomguided-api
  template:
    metadata:
      labels:
        app: wisdomguided-api
    spec:
      containers:
      - name: api
        image: wisdomguided-api:latest
        ports:
        - containerPort: 80
        env:
        - name: AzureOpenAI__ApiKey
          valueFrom:
            secretKeyRef:
              name: azure-openai-secrets
              key: apiKey
        - name: AzureOpenAI__Endpoint
          valueFrom:
            secretKeyRef:
              name: azure-openai-secrets
              key: endpoint
        - name: AzureOpenAI__DeploymentName
          value: "gpt-4"
```

2. Deploy to Kubernetes:

```powershell
kubectl apply -f api-deployment.yaml
kubectl apply -f api-service.yaml
```

### Monitoring in Production

Extend the observability capabilities to production:

1. Configure Application Insights for telemetry:

```csharp
// In Program.cs of each service
builder.Services.AddApplicationInsightsTelemetry();
```

2. Use Azure Monitor for Containers to monitor Kubernetes deployments

3. Set up dashboards for key metrics:
   - Agent response time
   - Success/failure rates
   - Sales progress against targets

### Backup and Disaster Recovery

Ensure your data is protected:

1. Set up regular backups of logs and contract data
2. Implement a recovery plan for service outages
3. Use Azure Storage or another cloud provider for durable storage

## Conclusion

This implementation guide provides a comprehensive approach to setting up the Wisdom-Guided methodology using the Semantic Kernel AI Agent Framework and .NET Aspire. The implementation takes full advantage of .NET Aspire's cloud-native capabilities, including:

- **Orchestration** of multiple services and AI agents
- **Integrated observability** with metrics, traces, and logs
- **Simplified service configuration** with dependency injection and configuration management
- **Health monitoring** with built-in health checks

By following this guide, you'll have a robust, scalable system for implementing agent-based AI solutions that can help achieve your goal of generating $10,000-$20,000 in 30 days through the development and sale of AI projects.

The structured approach, with formal Oral Contract and Articles of Operations, ensures accountability and clarity throughout the process, while the integration with .NET Aspire provides the technical foundation for success.
# Wisdom-Guided Approach with .NET Aspire: Enhanced Implementation Guide

This guide provides a comprehensive implementation strategy for the Wisdom-Guided approach using the Semantic Kernel AI Agent Framework, fully leveraging .NET Aspire's cloud-native capabilities. The implementation includes optimized orchestration, service integration, and observability for your AI-powered applications.

## Table of Contents
1. [Understanding .NET Aspire](#understanding-net-aspire)
2. [Setup Development Environment](#setup-development-environment)
3. [Project Structure Organization](#project-structure-organization)
4. [.NET Aspire Configuration](#net-aspire-configuration)
5. [Prompty File Creation](#prompty-file-creation)
6. [Semantic Kernel AI Agent Framework Integration](#semantic-kernel-ai-agent-framework-integration)
7. [Implementing the Oral Contract](#implementing-the-oral-contract)
8. [Governance Framework Setup](#governance-framework-setup)
9. [Deployment and Monitoring](#deployment-and-monitoring)
10. [Production Considerations](#production-considerations)

## Understanding .NET Aspire

.NET Aspire is a cloud-ready stack for building observable, production-ready, distributed applications. It provides:

- **Orchestration**: Manages how different services in your application interact during development
- **Integrations**: Pre-built components for common services like databases and messaging
- **Observability**: Built-in support for logging, metrics, and health checks
- **Tooling**: Dashboard for monitoring and managing your application components

For our Wisdom-Guided implementation, .NET Aspire offers significant advantages:

- Simplified management of multiple AI agent services
- Standardized configurations for Azure AI services
- Built-in observability for monitoring agent performance
- Local development environment that emulates production scenarios

## Setup Development Environment

### Prerequisites
- .NET 8.0 SDK or higher
- Azure subscription for Azure AI Foundry
- PowerShell 5.1+ installed
- Visual Studio 2022 (17.9+) or Visual Studio Code with C# extensions
- Docker Desktop (for containerized components)

### Initial Setup
1. Create a new .NET Aspire solution:

```powershell
# Create a new .NET Aspire solution
dotnet new aspire-starter --output WisdomGuidedAspire

# Navigate to the solution directory
cd WisdomGuidedAspire
```

2. Examine the generated solution structure:
```
WisdomGuidedAspire/
├── WisdomGuidedAspire.sln                  # Solution file
├── WisdomGuidedAspire.AppHost/             # Orchestration project
├── WisdomGuidedAspire.ServiceDefaults/     # Shared service configurations
└── WisdomGuidedAspire.Web/                 # Web frontend project
```

3. Add an API project for our agent services:

```powershell
# Create the API project
dotnet new webapi -o WisdomGuidedAspire.ApiService

# Add the project to the solution
dotnet sln add WisdomGuidedAspire.ApiService

# Reference ServiceDefaults in the API project
dotnet add WisdomGuidedAspire.ApiService reference WisdomGuidedAspire.ServiceDefaults
```

4. Add Semantic Kernel packages to the API project:

```powershell
dotnet add WisdomGuidedAspire.ApiService package Microsoft.SemanticKernel
dotnet add WisdomGuidedAspire.ApiService package Microsoft.SemanticKernel.Agents.Core
dotnet add WisdomGuidedAspire.ApiService package Microsoft.SemanticKernel.Agents.OpenAI
```

5. Configure Azure OpenAI services using user secrets:

```powershell
cd WisdomGuidedAspire.ApiService
dotnet user-secrets init
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-api-key"
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-endpoint.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4"
```

## Project Structure Organization

Create the following folder structure in your API project:

```
WisdomGuidedAspire.ApiService/
├── Controllers/         # API controllers
├── Services/            # Business logic services
├── Models/              # Data models
├── Prompts/             # Prompty files
│   ├── Configurations/  # Initial Prompt for Configuration files
│   ├── Messages/        # Initial Prompt Messages for development
│   ├── Templates/       # Base prompt templates
│   └── Agents/          # Agent prompt templates
└── Logs/                # Store logs for tracking
```

Create this structure using PowerShell:

```powershell
# Create folders
cd WisdomGuidedAspire.ApiService
mkdir -p Prompts/Configurations
mkdir -p Prompts/Messages
mkdir -p Prompts/Templates
mkdir -p Prompts/Agents
mkdir -p Logs
```

## .NET Aspire Configuration

Update the AppHost project to orchestrate our API service and configure Azure resources:

```csharp
// File: WisdomGuidedAspire.AppHost/Program.cs

var builder = DistributedApplication.CreateBuilder(args);

// Add shared project configuration
var serviceDefaults = builder.AddProject<Projects.WisdomGuidedAspire_ServiceDefaults>("servicedefaults");

// Add and configure Azure OpenAI service
var openAiService = builder.AddAzureOpenAI("openai")
    .WithEndpoint(builder.Configuration["AzureOpenAI:Endpoint"])
    .WithApiKey(builder.Configuration["AzureOpenAI:ApiKey"]);

// Add Redis for caching agent responses (improves performance)
var cache = builder.AddRedis("cache");

// Add API service with dependencies
builder.AddProject<Projects.WisdomGuidedAspire_ApiService>("apiservice")
    .WithReference(serviceDefaults)
    .WithReference(openAiService)
    .WithReference(cache);

// Add web frontend with references to API
builder.AddProject<Projects.WisdomGuidedAspire_Web>("webfrontend")
    .WithReference(serviceDefaults)
    .WithReference("apiservice");

// Build and run the application
await builder.BuildAsync().RunAsync();
```

Update the ServiceDefaults project to add standardized observability:

```csharp
// File: WisdomGuidedAspire.ServiceDefaults/Extensions.cs

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();
        
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Use service discovery
            http.UseServiceDiscovery();
            
            // Add resilience (retries, circuit breaker)
            http.AddStandardResilienceHandler();
        });

        return builder;
    }

    private static void ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddRuntimeInstrumentation()
                       .AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddProcessInstrumentation();
                
                // Add AI agent metrics
                metrics.AddMeter("Microsoft.SemanticKernel.Agents");
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation();
                
                // Add AI agent tracing
                tracing.AddSource("Microsoft.SemanticKernel.Agents");
            });
    }

    private static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            // Add a check for the AI service
            .AddCheck("AI Service", () => HealthCheckResult.Healthy(), ["ready"]);

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        return builder;
    }
}
```

## Prompty File Creation

Create the following Prompty files for your project:

### 1. Configuration Prompty File (ApiConfig.prompty)

Create this file in `WisdomGuidedAspire.ApiService/Prompts/Configurations/ApiConfig.prompty`:

```yaml
name: ApiConfig
description: Configuration for Semantic Kernel API setup with Azure AI Foundry.
version: 1.0
author: YourName
parameters:
  project_type: Semantic Kernel API with .NET Aspire
  target_audience: Developers integrating AI into .NET Aspire applications
  openai_model: gpt-4
  openai_endpoint: https://your-workspace.openai.azure.com/
  openai_api_key: "[Insert OpenAI API Key Here]"
  development_constraints:
    build_time: 1-2 days
    compatibility: PowerShell 5.1+
    cloud_ready: true
prompt: |
  Configure a Semantic Kernel API project with Azure AI Foundry using .NET Aspire:
  - Project Type: {{project_type}}
  - Target Audience: {{target_audience}}
  - OpenAI Settings:
    - Model: {{openai_model}}
    - Endpoint: {{openai_endpoint}}
    - API Key: {{openai_api_key}}
  - Constraints: {{development_constraints}}
  - .NET Aspire Features:
    - Orchestration for multi-service coordination
    - Built-in health checks and observability
    - Service discovery for component communication
    - Redis caching for performance optimization
```

### 2. Agent Configuration Prompty File (ChatbotAgentConfig.prompty)

Create this file in `WisdomGuidedAspire.ApiService/Prompts/Agents/ChatbotAgentConfig.prompty`:

```yaml
name: ChatbotAgentConfig
description: Configures agents for Customer Support Chatbot.
version: 1.0
author: YourName
parameters:
  agent_type: ChatCompletionAgent
  model: gpt-4
  role: Customer support specialist
  behavior: Respond with empathy, escalate complex issues
  metrics:
    response_time_target: 1000ms
    escalation_rate_target: 10%
prompt: |
  Configure a {{agent_type}} for a Customer Support Chatbot:
  - Model: {{model}}
  - Role: {{role}}
  - Behavior: {{behavior}}
  - Performance Metrics:
    - Target Response Time: {{metrics.response_time_target}}
    - Target Escalation Rate: {{metrics.escalation_rate_target}}
  - .NET Aspire Instrumentation:
    - Add telemetry for response times
    - Add health check endpoints
    - Configure service discovery for agent communication
```

### 3. Agent Collaboration Prompty File (ChatbotAgentCollaboration.prompty)

Create this file in `WisdomGuidedAspire.ApiService/Prompts/Agents/ChatbotAgentCollaboration.prompty`:

```yaml
name: ChatbotAgentCollaboration
description: Coordinates agents for Customer Support Chatbot.
version: 1.0
author: YourName
parameters:
  task_overview: Handle customer support queries
  agent_descriptions:
    - UserInputAgent: Validates user queries
    - ResponseAgent: Generates responses
    - EscalationAgent: Escalates complex issues
  user_request: Where's my order?
  orchestration: .NET Aspire distributed application
prompt: |
  Coordinate agents for the {{task_overview}} using {{orchestration}}:
  - Agents: {{agent_descriptions}}
  - User Request: {{user_request}}
  Steps:
  1. UserInputAgent validates the query.
  2. ResponseAgent generates a response if the query is simple.
  3. EscalationAgent escalates if the query is complex.
  
  Orchestration Pattern:
  - Use service discovery for agent communication
  - Implement circuit breakers for resilience
  - Add distributed tracing between agents
  - Configure centralized logging for all agent interactions
```

## Semantic Kernel AI Agent Framework Integration

Create a service class to implement the AI Agent Framework:

```csharp
// File: WisdomGuidedAspire.ApiService/Services/AgentService.cs

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;

namespace WisdomGuidedAspire.ApiService.Services
{
    public class AgentService
    {
        private readonly Kernel _kernel;
        private readonly ILogger<AgentService> _logger;
        private static readonly ActivitySource _activitySource = new("Microsoft.SemanticKernel.Agents");
        private static readonly Meter _meter = new("Microsoft.SemanticKernel.Agents", "1.0");
        private readonly Counter<int> _agentCallsCounter;
        private readonly Histogram<double> _agentResponseTimeHistogram;

        public AgentService(Kernel kernel, ILogger<AgentService> logger)
        {
            _kernel = kernel;
            _logger = logger;
            
            // Create metrics for observability
            _agentCallsCounter = _meter.CreateCounter<int>("agent_calls_total", "calls", "Total number of agent calls");
            _agentResponseTimeHistogram = _meter.CreateHistogram<double>("agent_response_time", "ms", "Agent response time in milliseconds");
        }

        public async Task<AgentChat> CreateMultiAgentChatbotSystem()
        {
            using var activity = _activitySource.StartActivity("CreateMultiAgentSystem");
            
            _logger.LogInformation("Creating multi-agent chatbot system");
            
            // Create agents
            var userInputAgent = new ChatCompletionAgent
            {
                Name = "UserInputAgent",
                Instructions = "Validate user queries for clarity.",
                Kernel = _kernel
            };

            var responseAgent = new ChatCompletionAgent
            {
                Name = "ResponseAgent",
                Instructions = "Generate empathetic responses for customer queries.",
                Kernel = _kernel
            };

            var escalationAgent = new ChatCompletionAgent
            {
                Name = "EscalationAgent",
                Instructions = "Identify complex issues that need human escalation.",
                Kernel = _kernel
            };

            // Create agent chat
            var chat = new AgentChat();
            chat.AddAgent(userInputAgent);
            chat.AddAgent(responseAgent);
            chat.AddAgent(escalationAgent);

            activity?.SetTag("agents.count", 3);
            
            return chat;
        }

        public async Task<string> ProcessUserQuery(AgentChat chat, string userQuery)
        {
            using var activity = _activitySource.StartActivity("ProcessUserQuery");
            activity?.SetTag("query", userQuery);
            
            var stopwatch = Stopwatch.StartNew();
            
            // Increment the counter for each agent call
            _agentCallsCounter.Add(1);
            
            _logger.LogInformation("Processing user query: {Query}", userQuery);

            // Process user query through the agent system
            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(userQuery);

            string response = "";
            
            try
            {
                await foreach (var message in chat.InvokeAsync(chatHistory))
                {
                    response += message.Content + "\n";
                }
                
                // Record response time
                stopwatch.Stop();
                _agentResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                
                activity?.SetTag("response.length", response.Length);
                activity?.SetTag("response.time_ms", stopwatch.ElapsedMilliseconds);
                
                _logger.LogInformation("Processed query in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                
                return response;
            }
            catch (Exception ex)
            {
                _defaultLogger.LogError(ex, "Error retrieving logs for date: {Date}", date);
                return StatusCode(500, new { error = "An error occurred retrieving logs" });
            }
        }
        
        [HttpPost("contract/review")]
        public async Task<IActionResult> ReviewContract([FromBody] ContractReviewRequest request)
        {
            try
            {
                await _contractService.LogContractReview(request.Reviewer, request.Approved, request.Comments);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _defaultLogger.LogError(ex, "Error processing contract review");
                return StatusCode(500, new { error = "An error occurred processing the review" });
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; }
    }
    
    public class ContractReviewRequest
    {
        public string Reviewer { get; set; }
        public bool Approved { get; set; }
        public string Comments { get; set; }
    }
}

// Create a health check controller to monitor system status
// File: WisdomGuidedAspire.ApiService/Controllers/HealthController.cs

using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WisdomGuidedAspire.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly AgentService _agentService;
        private readonly ILogger<HealthController> _logger;

        public HealthController(AgentService agentService, ILogger<HealthController> logger)
        {
            _agentService = agentService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetHealthStatus()
        {
            try
            {
                // Check agent service health
                var agentStatus = _agentService != null;
                
                // Return system health
                return Ok(new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    services = new
                    {
                        agent = agentStatus ? "available" : "unavailable"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(500, new
                {
                    status = "unhealthy",
                    error = ex.Message
                });
            }
        }
    }
            {
                _logger.LogError(ex, "Error processing query");
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }
    }
}
```

## Implementing the Oral Contract

Create a model and service for the Oral Contract:

```csharp
// File: WisdomGuidedAspire.ApiService/Models/OralContract.cs

using System;

namespace WisdomGuidedAspire.ApiService.Models
{
    public class OralContract
    {
        public string Developer { get; set; } = "YourName";
        public string AiPartner { get; set; } = "Wisdom";
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now.AddDays(30);
        public decimal RevenueGoalMin { get; set; } = 10000;
        public decimal RevenueGoalMax { get; set; } = 20000;

        public string VerbalAgreement => 
            $"I, {Developer}, partner with {AiPartner} to earn ${RevenueGoalMin}-${RevenueGoalMax} " +
            $"in 30 days via innovative, API-driven AI projects with agent-based solutions using " +
            $"Semantic Kernel and Azure AI Foundry, orchestrated with .NET Aspire. " +
            $"We commit to our roles, log progress, and review weekly from " +
            $"{StartDate.ToShortDateString()} to {EndDate.ToShortDateString()}.";

        public string[] DeveloperResponsibilities => new string[]
        {
            "Build AI projects in 2-3 days using Semantic Kernel, Azure AI Foundry API, and the AI Agent Framework",
            "Sell projects on platforms like Upwork and Fiverr, targeting $500-$1,000 per project",
            "Integrate client feedback into project updates and optimizations",
            "Log daily activities in the Logs/ folder for transparency",
            "Execute API setup and maintenance as per ApiDevelopment.prompty"
        };

        public string[] AiPartnerResponsibilities => new string[]
        {
            "Deliver reusable Meta-Meta-Prompts and Prompty files to streamline development",
            "Design multi-agent systems to enhance project functionality",
            "Enable agent collaboration using AgentChat and .NET Aspire orchestration",
            "Integrate agents with the Azure AI Foundry API",
            "Enhance the Sales Agent AI to draft listings highlighting agent features",
            "Optimize prompts based on client feedback",
            "Schedule API and agent performance reviews",
            "Configure .NET Aspire observability for performance monitoring"
        };
    }
}
```

Create a contract service:

```csharp
// File: WisdomGuidedAspire.ApiService/Services/ContractService.cs

using System;
using System.Threading.Tasks;
using WisdomGuidedAspire.ApiService.Models;

namespace WisdomGuidedAspire.ApiService.Services
{
    public class ContractService
    {
        private readonly OralContract _contract;
        private readonly WisdomLogger _logger;
        private readonly ILogger<ContractService> _defaultLogger;

        public ContractService(WisdomLogger logger, ILogger<ContractService> defaultLogger)
        {
            _contract = new OralContract();
            _logger = logger;
            _defaultLogger = defaultLogger;
        }

        public OralContract GetContract()
        {
            return _contract;
        }

        public async Task LogContractReview(string reviewer, bool approved, string comments)
        {
            _defaultLogger.LogInformation("Contract reviewed by {Reviewer}, Approved: {Approved}", reviewer, approved);
            
            await _logger.LogActivity(
                "ContractReview",
                $"Reviewer: {reviewer}, Approved: {approved}",
                $"Comments: {comments}"
            );
        }

        public async Task<bool> UpdateContractRevenue(decimal newMinRevenue, decimal newMaxRevenue, string justification)
        {
            if (newMinRevenue <= 0 || newMaxRevenue <= 0 || newMinRevenue > newMaxRevenue)
            {
                _defaultLogger.LogWarning("Invalid revenue values: Min {Min}, Max {Max}", newMinRevenue, newMaxRevenue);
                return false;
            }
            
            // Store old values for logging
            var oldMin = _contract.RevenueGoalMin;
            var oldMax = _contract.RevenueGoalMax;
            
            // Update contract
            _contract.RevenueGoalMin = newMinRevenue;
            _contract.RevenueGoalMax = newMaxRevenue;
            
            // Log the change
            await _logger.LogActivity(
                "ContractUpdate",
                $"Revenue goals updated from ${oldMin}-${oldMax} to ${newMinRevenue}-${newMaxRevenue}",
                $"Justification: {justification}"
            );
            
            _defaultLogger.LogInformation("Contract updated: Revenue goals changed to ${Min}-${Max}", 
                newMinRevenue, newMaxRevenue);
                
            return true;
        }
    }
}
```

## Governance Framework Setup

Create the Articles of Operations model and service:

```csharp
// File: WisdomGuidedAspire.ApiService/Models/ArticlesOfOperations.cs

namespace WisdomGuidedAspire.ApiService.Models
{
    public class ArticlesOfOperations
    {
        public Article[] Articles { get; } = new Article[]
        {
            new Article
            {
                Title = "Development Standards",
                Clauses = new string[]
                {
                    "All projects must use Semantic Kernel with Azure AI Foundry API and OpenAI (GPT-4)",
                    "Projects must be production-ready in 2-3 days, with Prompty files and a README for clients",
                    "Use .NET Aspire for orchestration and observability of all services"
                }
            },
            new Article
            {
                Title = "Sales Targets",
                Clauses = new string[]
                {
                    "Aim for 5-10 sales per project type in 30 days (e.g., 5 chatbots at $500, 3 summarizers at $750)",
                    "Adjust pricing if sales are slow (e.g., reduce by 10% after 5 days of no sales)"
                }
            },
            new Article
            {
                Title = "Optimization Schedule",
                Clauses = new string[]
                {
                    "Review API, agent performance, and prompts on Day 15 and after every 5 client deliveries",
                    "Update Prompty files and log all changes",
                    "Use .NET Aspire dashboard to monitor service health and performance metrics"
                }
            },
            new Article
            {
                Title = "Communication",
                Clauses = new string[]
                {
                    "Daily updates via logs in the Logs/ folder",
                    "Use the Recalibration Prompt Template if revenue goals are not met by Day 15",
                    "Record all service calls in OpenTelemetry for later review"
                }
            }
        };
    }

    public class Article
    {
        public string Title { get; set; }
        public string[] Clauses { get; set; }
    }
}
```

Create a governance service:

```csharp
// File: WisdomGuidedAspire.ApiService/Services/GovernanceService.cs

using System.Threading.Tasks;
using WisdomGuidedAspire.ApiService.Models;

namespace WisdomGuidedAspire.ApiService.Services
{
    public class GovernanceService
    {
        private readonly ArticlesOfOperations _articles;
        private readonly WisdomLogger _logger;
        private readonly ILogger<GovernanceService> _defaultLogger;

        public GovernanceService(WisdomLogger logger, ILogger<GovernanceService> defaultLogger)
        {
            _articles = new ArticlesOfOperations();
            _logger = logger;
            _defaultLogger = defaultLogger;
        }

        public ArticlesOfOperations GetArticles()
        {
            return _articles;
        }

        public async Task LogComplianceCheck(string articleTitle, bool compliant, string evidence)
        {
            _defaultLogger.LogInformation("Compliance check for {Article}: {Compliant}", articleTitle, compliant);
            
            await _logger.LogActivity(
                "ComplianceCheck",
                $"Article: {articleTitle}, Compliant: {compliant}",
                $"Evidence: {evidence}"
            );
        }

        public async Task<bool> ProposeArticleAmendment(string articleTitle, string existingClause, 
            string proposedClause, string justification)
        {
            _defaultLogger.LogInformation("Article amendment proposed for {Article}", articleTitle);
            
            await _logger.LogActivity(
                "ArticleAmendment",
                $"Article: {articleTitle}, From: \"{existingClause}\", To: \"{proposedClause}\"",
                $"Justification: {justification}"
            );
            
            // In a real implementation, you would update the clause here
            // For now, we just log the proposal
            
            return true;
        }
    }
}
```

## Create a Logger System

Create a logger service optimized for .NET Aspire:

```csharp
// File: WisdomGuidedAspire.ApiService/Services/WisdomLogger.cs

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace WisdomGuidedAspire.ApiService.Services
{
    public class WisdomLogger
    {
        private readonly string _logDirectory;
        private readonly ILogger<WisdomLogger> _logger;
        private static readonly ActivitySource _activitySource = new("WisdomGuidedAspire.Logging");

        public WisdomLogger(string logDirectory, ILogger<WisdomLogger> logger)
        {
            _logDirectory = logDirectory;
            _logger = logger;
            Directory.CreateDirectory(_logDirectory);
        }

        public async Task LogActivity(string action, string details, string outcome)
        {
            using var activity = _activitySource.StartActivity("LogActivity");
            activity?.SetTag("action", action);
            
            string logDate = DateTime.Now.ToString("yyyy-MM-dd");
            string logFileName = $"Log_{logDate}_{action.Replace(" ", "")}.txt";
            string logPath = Path.Combine(_logDirectory, logFileName);

            string logEntry = $"Date: {DateTime.Now}\nAction: {action}\nDetails: {details}\nOutcome: {outcome}\n\n";

            // Log to file
            await File.AppendAllTextAsync(logPath, logEntry);
            
            // Also log to .NET Aspire's logging system
            _logger.LogInformation("Activity logged: {Action}, Details: {Details}", action, details);
            
            // Add trace data for observability
            activity?.SetTag("details", details);
            activity?.SetTag("outcome", outcome);
            activity?.SetTag("logFile", logPath);
        }

        public async Task<string[]> GetLogs(string date)
        {
            using var activity = _activitySource.StartActivity("GetLogs");
            activity?.SetTag("date", date);
            
            string searchPattern = $"Log_{date}_*.txt";
            string[] logFiles = Directory.GetFiles(_logDirectory, searchPattern);
            
            _logger.LogInformation("Retrieving {Count} logs for date {Date}", logFiles.Length, date);
            
            string[] logs = new string[logFiles.Length];
            
            for (int i = 0; i < logFiles.Length; i++)
            {
                logs[i] = await File.ReadAllTextAsync(logFiles[i]);
            }
            
            activity?.SetTag("logCount", logs.Length);
            
            return logs;
        }
    }
}
```

## Deployment and Monitoring

.NET Aspire provides excellent built-in tools for monitoring and managing your application. Let's set up a comprehensive monitoring and deployment strategy:

Register all services in the ApiService's Program.cs file:

```csharp
// File: WisdomGuidedAspire.ApiService/Program.cs

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using System.IO;
using WisdomGuidedAspire.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (from .NET Aspire)
builder.AddServiceDefaults();

// Add controllers and API Explorer
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Semantic Kernel with Azure OpenAI
var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(
        deploymentName: builder.Configuration["AzureOpenAI:DeploymentName"],
        endpoint: builder.Configuration["AzureOpenAI:Endpoint"],
        apiKey: builder.Configuration["AzureOpenAI:ApiKey"])
    .Build();

// Add Redis cache for distributed caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["ConnectionStrings:cache"];
});

// Register services
builder.Services.AddSingleton(kernel);
builder.Services.AddSingleton<AgentService>();
builder.Services.AddSingleton<WisdomLogger>(provider => 
    new WisdomLogger(
        Path.Combine(Directory.GetCurrentDirectory(), "Logs"),
        provider.GetRequiredService<ILogger<WisdomLogger>>()
    )
);
builder.Services.AddSingleton<ContractService>();
builder.Services.AddSingleton<GovernanceService>();

// Build the app
var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Add health check endpoint
app.MapHealthChecks("/health");

app.Run();
```

Create a controller for the API endpoints:

```csharp
// File: WisdomGuidedAspire.ApiService/Controllers/AgentController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel.Agents;
using System.Threading.Tasks;
using WisdomGuidedAspire.ApiService.Services;
using WisdomGuidedAspire.ApiService.Models;

namespace WisdomGuidedAspire.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgentController : ControllerBase
    {
        private readonly AgentService _agentService;
        private readonly ContractService _contractService;
        private readonly GovernanceService _governanceService;
        private readonly WisdomLogger _logger;
        private readonly ILogger<AgentController> _defaultLogger;

        public AgentController(
            AgentService agentService,
            ContractService contractService,
            GovernanceService governanceService,
            WisdomLogger logger,
            ILogger<AgentController> defaultLogger)
        {
            _agentService = agentService;
            _contractService = contractService;
            _governanceService = governanceService;
            _logger = logger;
            _defaultLogger = defaultLogger;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            _defaultLogger.LogInformation("Chat request received: {FirstChars}...", 
                request.Message.Substring(0, Math.Min(20, request.Message.Length)));
            
            try
            {
                var agentChat = await _agentService.CreateMultiAgentChatbotSystem();
                var response = await _agentService.ProcessUserQuery(agentChat, request.Message);
                
                await _logger.LogActivity(
                    "AgentChat", 
                    $"Processed user query: {request.Message}", 
                    $"Response: {response.Substring(0, Math.Min(100, response.Length))}..."
                );

                return Ok(new { response });
            }
            catch (Exception ex)
            {
                _defaultLogger.LogError(ex, "Error processing chat request");
                return StatusCode(500, new { error = "An error occurred processing your request" });
            }
        }

        [HttpGet("contract")]
        public IActionResult GetContract()
        {
            _defaultLogger.LogInformation("Contract information requested");
            var contract = _contractService.GetContract();
            return Ok(contract);
        }

        [HttpGet("articles")]
        public IActionResult GetArticlesOfOperations()
        {
            _defaultLogger.LogInformation("Articles of operations requested");
            var articles = _governanceService.GetArticles();
            return Ok(articles);
        }
        
        [HttpGet("logs/{date}")]
        public async Task<IActionResult> GetLogs(string date)
        {
            try
            {
                _defaultLogger.LogInformation("Logs requested for date: {Date}", date);
                var logs = await _logger.GetLogs(date);
                return Ok(new { count = logs.Length, logs });
            }
            catch (Exception ex)