# Semantic Kernel Agent Framework Components for AI-Powered Travel Planner

## Core Libraries and Versions

### Semantic Kernel Libraries
1. **Microsoft.SemanticKernel**
   - **Version**: `1.24.1-alpha` (prerelease)
   - **Purpose**: Core library providing the `Kernel` class, plugins, and AI service integration
   - **Installation**: `dotnet add package Microsoft.SemanticKernel --version 1.24.1-alpha`

2. **Microsoft.SemanticKernel.Agents.Core**
   - **Version**: `1.24.1-alpha` (prerelease)
   - **Purpose**: Core agent framework, including `ChatCompletionAgent` and `AgentChat` classes
   - **Installation**: `dotnet add package Microsoft.SemanticKernel.Agents.Core --version 1.24.1-alpha`

### AI Service Connector
3. **Microsoft.SemanticKernel.Connectors.AzureOpenAI**
   - **Version**: `1.24.1-alpha` (prerelease)
   - **Purpose**: Azure OpenAI connector for chat completion services
   - **Installation**: `dotnet add package Microsoft.SemanticKernel.Connectors.AzureOpenAI --version 1.24.1-alpha`

### Authentication
4. **Azure.Identity**
   - **Version**: `1.12.0` (stable)
   - **Purpose**: Credential management for Azure services authentication
   - **Installation**: `dotnet add package Azure.Identity --version 1.12.0`

### Additional Required Packages
5. **Microsoft.Extensions.Configuration.UserSecrets**
   - **Version**: `8.0.0` (stable)
   - **Purpose**: Manages user secrets for storing API keys securely
   - **Installation**: `dotnet add package Microsoft.Extensions.Configuration.UserSecrets --version 8.0.0`

6. **Microsoft.SemanticKernel.Prompty**
   - **Purpose**: For using Prompty file templates
   - **Installation**: `dotnet add package Microsoft.SemanticKernel.Prompty --prerelease`

7. **Microsoft.SemanticKernel.Planners.Sequential**
   - **Purpose**: For planning and sequencing tasks
   - **Installation**: `dotnet add package Microsoft.SemanticKernel.Planners.Sequential --prerelease`

## Framework Architecture Components

### Core Components
1. **Kernel**: Central engine managing AI services and plugins
2. **ChatCompletionAgent**: Conversation agent using `ChatHistory` to maintain context
3. **Prompty Files**: YAML templates defining agent behavior
4. **Plugins**: Custom functions for specific tasks (e.g., flight search)

### Development Environment
1. **.NET SDK**: Required for C# development
2. **IDE**: Visual Studio 2022/2025 or JetBrains Rider
3. **Azure OpenAI Account**: For natural language processing capabilities

## Plugin System

### Example Plugins
1. **FlightPlugin**
   ```csharp
   public class FlightPlugin
   {
       [KernelFunction("SearchFlights")]
       public string SearchFlights(string origin, string destination, string date)
       {
           return $"Flights from {origin} to {destination} on {date}: $500";
       }
   }
   ```

2. **HotelPlugin**
   ```csharp
   public class HotelPlugin
   {
       [KernelFunction("SearchHotels")]
       public string SearchHotels(string destination, string checkIn, string checkOut)
       {
           return $"Hotels in {destination} from {checkIn} to {checkOut}: $300";
       }
   }
   ```

3. **WeatherPlugin**
   ```csharp
   public class WeatherPlugin
   {
       [KernelFunction("GetWeather")]
       public string GetWeather(string destination, string date)
       {
           return $"Weather in {destination} on {date}: Sunny, 25°C";
       }
   }
   ```

## Prompty File Templates

### Main Travel Planner Template
```yaml
name: TravelPlannerPrompt
description: A prompt for generating personalized travel itineraries
authors:
  - Your Name
model:
  api: chat
system:
  You are a travel expert helping users plan their trips. Your task is to create a detailed, personalized itinerary based on the user's destination, travel dates, budget, and preferences. Consider the budget carefully and suggest options that fit within it. Also, take into account any specific preferences such as activities, dining, or accessibility needs.
user:
  Plan a trip to {{destination}} from {{start_date}} to {{end_date}} with a budget of {{budget}}. Preferences: {{preferences}}
```

### Follow-up Interaction Template
```yaml
name: FollowUpPrompt
description: A prompt for handling follow-up questions or modifications to the travel itinerary
authors:
  - Your Name
model:
  api: chat
system:
  You are a travel expert assisting with follow-up questions or modifications to the user's travel itinerary. Use the context from the previous interaction to provide accurate and helpful responses. If the user asks for changes, update the itinerary accordingly.
user:
  {{follow_up_question}}
```

### Error Handling Template
```yaml
name: ErrorHandlingPrompt
description: A prompt for handling errors or incomplete information in user requests
authors:
  - Your Name
model:
  api: chat
system:
  You are a travel expert. When you encounter errors or incomplete information in user requests, politely ask for clarification or suggest alternatives. Be helpful and guide the user toward providing the necessary details.
user:
  {{error_context}}
```

## Implementation Steps

### 1. Basic Setup
```csharp
// Initialize the Kernel with Azure OpenAI
var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(
        deploymentName: "gpt-4",
        endpoint: "https://your-endpoint.openai.azure.com/",
        credentials: new AzureCliCredential())
    .Build();

// Add plugins
kernel.Plugins.AddFromType<FlightPlugin>("FlightPlugin");
kernel.Plugins.AddFromType<HotelPlugin>("HotelPlugin");
kernel.Plugins.AddFromType<WeatherPlugin>("WeatherPlugin");
```

### 2. Agent Configuration
```csharp
// Load system message
string systemMessage = File.ReadAllText("system_message.txt");

// Create the ChatCompletionAgent
var travelAgent = new ChatCompletionAgent
{
    Name = "TravelGenie",
    Instructions = systemMessage,
    Kernel = kernel,
    Arguments = new KernelArguments(
        new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        })
};
```

### 3. Conversation Handling
```csharp
// Initialize chat history
var chat = new ChatHistory();

// Conversation loop
while (true)
{
    Console.Write("User: ");
    string userInput = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(userInput)) continue;
    if (userInput.ToLower() == "exit") break;

    chat.Add(new ChatMessageContent(AuthorRole.User, userInput));

    await foreach (var response in travelAgent.InvokeAsync(chat))
    {
        Console.WriteLine($"TravelGenie: {response.Content}");
        chat.Add(response);
    }
}
```

## Scalability and Performance Considerations

### Caching Implementation
```csharp
using Microsoft.Extensions.Caching.Memory;

var cache = new MemoryCache(new MemoryCacheOptions());
if (!cache.TryGetValue("flights", out string cachedFlights))
{
    cachedFlights = await new FlightPlugin("key").SearchFlightsAsync("JFK", "LAX", "2023-12-01");
    cache.Set("flights", cachedFlights, TimeSpan.FromMinutes(30));
}
```

### Testing Framework
```csharp
// Unit Test Example
using Xunit;

public class FlightPluginTests
{
    [Fact]
    public async Task SearchFlightsAsync_ReturnsFlightOptions()
    {
        var plugin = new FlightPlugin("test-api-key");
        var result = await plugin.SearchFlightsAsync("JFK", "LAX", "2023-12-01");
        Assert.NotEmpty(result);
    }
}
```

## Complete System Integration

### Console Application Example
```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Planning.Sequential;

var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(
        deploymentName: "your-deployment-name",
        endpoint: "your-azure-openai-endpoint",
        apiKey: "your-azure-openai-api-key"
    )
    .Build();

kernel.Plugins.AddFromType<FlightPlugin>("FlightPlugin", new FlightPlugin("your-flight-api-key"));
kernel.Plugins.AddFromType<HotelPlugin>("HotelPlugin", new HotelPlugin("your-hotel-api-key"));
kernel.Plugins.AddFromType<WeatherPlugin>("WeatherPlugin", new WeatherPlugin("your-weather-api-key"));

var travelPlannerFunction = kernel.CreateFunctionFromPromptyFile("travel_planner.prompty");

var settings = new OpenAIPromptExecutionSettings
{
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
};

// User interaction code
// ...
```
