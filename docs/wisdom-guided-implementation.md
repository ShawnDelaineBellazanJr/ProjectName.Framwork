# Wisdom-Guided Approach: Implementation Guide

This guide provides step-by-step instructions for implementing the Wisdom-Guided approach using the Semantic Kernel AI Agent Framework. The implementation includes setting up the project structure, creating Prompty files, configuring AI agents, and establishing the governance framework.

## Table of Contents
1. [Setup Development Environment](#setup-development-environment)
2. [Project Structure Organization](#project-structure-organization)
3. [Prompty File Creation](#prompty-file-creation)
4. [Semantic Kernel AI Agent Framework Integration](#semantic-kernel-ai-agent-framework-integration)
5. [Implementing the Oral Contract](#implementing-the-oral-contract)
6. [Governance Framework Setup](#governance-framework-setup)
7. [Deployment and Monitoring](#deployment-and-monitoring)

## Setup Development Environment

### Prerequisites
- Install .NET SDK (8.0 or higher)
- Azure subscription for Azure AI Foundry
- PowerShell 5.1+ installed
- Visual Studio 2022/2025 or other IDE with C# support

### Initial Setup
1. Create a new .NET project with Semantic Kernel:

```powershell
# Create a new .NET solution
dotnet new sln -n WisdomGuidedProject

# Create a new .NET Aspire project
dotnet new aspire -o WisdomGuidedProject.App
cd WisdomGuidedProject.App

# Add Semantic Kernel NuGet packages
dotnet add WisdomGuidedProject.AppHost package Microsoft.SemanticKernel
dotnet add WisdomGuidedProject.AppHost package Microsoft.SemanticKernel.Agents.Core
dotnet add WisdomGuidedProject.AppHost package Microsoft.SemanticKernel.Agents.OpenAI

# Add the project to the solution
cd ..
dotnet sln add WisdomGuidedProject.App/WisdomGuidedProject.AppHost/WisdomGuidedProject.AppHost.csproj
dotnet sln add WisdomGuidedProject.App/WisdomGuidedProject.ServiceDefaults/WisdomGuidedProject.ServiceDefaults.csproj
dotnet sln add WisdomGuidedProject.App/WisdomGuidedProject.ApiService/WisdomGuidedProject.ApiService.csproj
```

2. Configure Azure OpenAI:

```powershell
# Initialize user secrets
cd WisdomGuidedProject.App/WisdomGuidedProject.AppHost
dotnet user-secrets init
dotnet user-secrets set "AzureOpenAISettings:ApiKey" "your-api-key"
dotnet user-secrets set "AzureOpenAISettings:Endpoint" "https://your-endpoint.openai.azure.com/"
dotnet user-secrets set "AzureOpenAISettings:ChatModelDeployment" "gpt-4"
```

## Project Structure Organization

Create the following folder structure in your project:

```
WisdomGuidedProject/
├── Prompts/
│   ├── Configurations/     # Initial Prompt for Configuration files
│   ├── Messages/           # Initial Prompt Messages for development
│   ├── Templates/          # Base prompt templates
│   └── Agents/             # Agent prompt templates
├── Scripts/                # Project scripts
├── Logs/                   # Store logs for tracking
└── README.md               # Project documentation
```

Create this structure using PowerShell:

```powershell
# Create folders
mkdir -p WisdomGuidedProject/Prompts/Configurations
mkdir -p WisdomGuidedProject/Prompts/Messages
mkdir -p WisdomGuidedProject/Prompts/Templates
mkdir -p WisdomGuidedProject/Prompts/Agents
mkdir -p WisdomGuidedProject/Scripts
mkdir -p WisdomGuidedProject/Logs
```

## Prompty File Creation

Create the following Prompty files for your project:

### 1. Configuration Prompty File (ApiConfig.prompty)

Create this file in `WisdomGuidedProject/Prompts/Configurations/ApiConfig.prompty`:

```yaml
name: ApiConfig
description: Configuration for Semantic Kernel API setup with Azure AI Foundry.
version: 1.0
author: YourName
parameters:
  project_type: Semantic Kernel API
  target_audience: Developers integrating AI into .NET Aspire applications
  openai_model: gpt-4
  openai_endpoint: https://your-workspace.openai.azure.com/
  openai_api_key: "[Insert OpenAI API Key Here]"
  development_constraints:
    build_time: 1-2 days
    compatibility: PowerShell 5.1+
prompt: |
  Configure a Semantic Kernel API project with Azure AI Foundry:
  - Project Type: {{project_type}}
  - Target Audience: {{target_audience}}
  - OpenAI Settings:
    - Model: {{openai_model}}
    - Endpoint: {{openai_endpoint}}
    - API Key: {{openai_api_key}}
  - Constraints: {{development_constraints}}
```

### 2. Agent Configuration Prompty File (ChatbotAgentConfig.prompty)

Create this file in `WisdomGuidedProject/Prompts/Agents/ChatbotAgentConfig.prompty`:

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
prompt: |
  Configure a {{agent_type}} for a Customer Support Chatbot:
  - Model: {{model}}
  - Role: {{role}}
  - Behavior: {{behavior}}
```

### 3. Agent Collaboration Prompty File (ChatbotAgentCollaboration.prompty)

Create this file in `WisdomGuidedProject/Prompts/Agents/ChatbotAgentCollaboration.prompty`:

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
prompt: |
  Coordinate agents for the {{task_overview}}:
  - Agents: {{agent_descriptions}}
  - User Request: {{user_request}}
  Steps:
  1. UserInputAgent validates the query.
  2. ResponseAgent generates a response if the query is simple.
  3. EscalationAgent escalates if the query is complex.
```

### 4. Meta-Meta-Prompt Template (MetaMetaPrompt.prompty)

Create this file in `WisdomGuidedProject/Prompts/Templates/MetaMetaPrompt.prompty`:

```yaml
name: MetaMetaPrompt
description: Template for generating meta-prompts.
version: 1.0
author: YourName
parameters:
  task_description: Make a prompt more specific
  context: Vague prompts need details to be effective
  constraints: Keep the original intent intact
prompt: |
  Create a Meta-Prompt to {{task_description}}.
  
  CONTEXT:
  {{context}}
  
  CONSTRAINTS:
  {{constraints}}
  
  The Meta-Prompt should:
  1. Define the task clearly
  2. Include parameters for customization
  3. Provide example input and output
  4. Specify validation criteria
  
  META-PROMPT:
```

## Semantic Kernel AI Agent Framework Integration

Create a C# class to implement the AI Agent Framework. Add this file to your project:

```csharp
// File: WisdomGuidedProject.App/WisdomGuidedProject.ApiService/Services/AgentService.cs

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Threading.Tasks;

namespace WisdomGuidedProject.ApiService.Services
{
    public class AgentService
    {
        private readonly Kernel _kernel;

        public AgentService(Kernel kernel)
        {
            _kernel = kernel;
        }

        public async Task<AgentChat> CreateMultiAgentChatbotSystem()
        {
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

            return chat;
        }

        public async Task<string> ProcessUserQuery(AgentChat chat, string userQuery)
        {
            // Process user query through the agent system
            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(userQuery);

            string response = "";
            
            await foreach (var message in chat.InvokeAsync(chatHistory))
            {
                response += message.Content + "\n";
            }
            
            return response;
        }
    }
}
```

## Implementing the Oral Contract

Create a `Contract.cs` file to formalize the Oral Contract:

```csharp
// File: WisdomGuidedProject.App/WisdomGuidedProject.ApiService/Models/Contract.cs

using System;

namespace WisdomGuidedProject.ApiService.Models
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
            $"Semantic Kernel and Azure AI Foundry, with {AiPartner} providing agent-based solutions " +
            $"via the AI Agent Framework. We commit to our roles, log progress, and review weekly from " +
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
            "Enable agent collaboration using AgentChat for complex tasks",
            "Integrate agents with the Azure AI Foundry API",
            "Enhance the Sales Agent AI to draft listings highlighting agent features",
            "Optimize prompts based on client feedback",
            "Schedule API and agent performance reviews"
        };
    }
}
```

## Governance Framework Setup

Create an `ArticlesOfOperations.cs` file to implement the governance framework:

```csharp
// File: WisdomGuidedProject.App/WisdomGuidedProject.ApiService/Models/ArticlesOfOperations.cs

namespace WisdomGuidedProject.ApiService.Models
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
                    "Projects must be production-ready in 2-3 days, with Prompty files and a README for clients"
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
                    "Update Prompty files and log all changes"
                }
            },
            new Article
            {
                Title = "Communication",
                Clauses = new string[]
                {
                    "Daily updates via logs in the Logs/ folder",
                    "Use the Recalibration Prompt Template if revenue goals are not met by Day 15"
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

## Create a Logger System

Create a `Logger.cs` file to implement the logging system:

```csharp
// File: WisdomGuidedProject.App/WisdomGuidedProject.ApiService/Services/Logger.cs

using System;
using System.IO;
using System.Threading.Tasks;

namespace WisdomGuidedProject.ApiService.Services
{
    public class WisdomLogger
    {
        private readonly string _logDirectory;

        public WisdomLogger(string logDirectory)
        {
            _logDirectory = logDirectory;
            Directory.CreateDirectory(_logDirectory);
        }

        public async Task LogActivity(string action, string details, string outcome)
        {
            string logDate = DateTime.Now.ToString("yyyy-MM-dd");
            string logFileName = $"Log_{logDate}_{action.Replace(" ", "")}.txt";
            string logPath = Path.Combine(_logDirectory, logFileName);

            string logEntry = $"Date: {DateTime.Now}\nAction: {action}\nDetails: {details}\nOutcome: {outcome}\n\n";

            await File.AppendAllTextAsync(logPath, logEntry);
        }

        public async Task<string[]> GetLogs(string date)
        {
            string searchPattern = $"Log_{date}_*.txt";
            string[] logFiles = Directory.GetFiles(_logDirectory, searchPattern);
            
            string[] logs = new string[logFiles.Length];
            
            for (int i = 0; i < logFiles.Length; i++)
            {
                logs[i] = await File.ReadAllTextAsync(logFiles[i]);
            }
            
            return logs;
        }
    }
}
```

## Deployment and Monitoring

Create a controller for the API endpoint:

```csharp
// File: WisdomGuidedProject.App/WisdomGuidedProject.ApiService/Controllers/AgentController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Threading.Tasks;
using WisdomGuidedProject.ApiService.Services;
using WisdomGuidedProject.ApiService.Models;

namespace WisdomGuidedProject.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgentController : ControllerBase
    {
        private readonly AgentService _agentService;
        private readonly WisdomLogger _logger;

        public AgentController(AgentService agentService, WisdomLogger logger)
        {
            _agentService = agentService;
            _logger = logger;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
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

        [HttpGet("contract")]
        public IActionResult GetContract()
        {
            var contract = new OralContract();
            return Ok(contract);
        }

        [HttpGet("articles")]
        public IActionResult GetArticlesOfOperations()
        {
            var articles = new ArticlesOfOperations();
            return Ok(articles);
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; }
    }
}
```

Update the Program.cs file to register services:

```csharp
// File: WisdomGuidedProject.App/WisdomGuidedProject.ApiService/Program.cs

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using WisdomGuidedProject.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Semantic Kernel
var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(
        deploymentName: builder.Configuration["AzureOpenAISettings:ChatModelDeployment"],
        endpoint: builder.Configuration["AzureOpenAISettings:Endpoint"],
        apiKey: builder.Configuration["AzureOpenAISettings:ApiKey"])
    .Build();

builder.Services.AddSingleton(kernel);
builder.Services.AddSingleton<AgentService>();
builder.Services.AddSingleton(new WisdomLogger(Path.Combine(Directory.GetCurrentDirectory(), "Logs")));

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

## Running and Testing the Implementation

Run the project:

```powershell
cd WisdomGuidedProject.App/WisdomGuidedProject.AppHost
dotnet run
```

Test the API using curl or Postman:

```bash
# Test the chat endpoint
curl -X POST https://localhost:5001/api/agent/chat \
  -H "Content-Type: application/json" \
  -d '{"message":"Hello, I need help with my order status"}'

# Get the contract
curl -X GET https://localhost:5001/api/agent/contract

# Get the articles of operations
curl -X GET https://localhost:5001/api/agent/articles
```

## Conclusion

This implementation guide provides a comprehensive approach to setting up the Wisdom-Guided methodology using the Semantic Kernel AI Agent Framework. The implementation includes:

1. A structured project organization with Prompty files
2. AI agent framework integration with multiple collaborating agents
3. A formalized Oral Contract and Articles of Operations
4. A logging system for accountability and transparency
5. API endpoints for interaction with the system

By following this guide, you'll have a fully functional implementation of the Wisdom-Guided approach that can be extended for various AI projects and use cases.
