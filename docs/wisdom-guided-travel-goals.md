# Travel Planner and Goal Tracking in Wisdom-Guided Approach

This document details how to implement an AI-powered Travel Planner component and a Goal Tracking system within the Wisdom-Guided approach. These components will help you build sellable AI products while tracking progress toward your $10,000-$20,000 revenue target within the 30-day timeframe.

## Table of Contents

1. [AI-Powered Travel Planner Implementation](#ai-powered-travel-planner-implementation)
   1. [Travel Planner Models](#travel-planner-models)
   2. [Travel Agent System](#travel-agent-system)
   3. [Travel Service Integrations](#travel-service-integrations)
   4. [Travel Planner Controller](#travel-planner-controller)
   5. [Travel Planner Prompty Files](#travel-planner-prompty-files)
   6. [Marketing the Travel Planner](#marketing-the-travel-planner)

2. [Goal Tracking System](#goal-tracking-system)
   1. [Goal Models](#goal-models)
   2. [Goal Service](#goal-service)
   3. [Goal Tracking Dashboard](#goal-tracking-dashboard)
   4. [Progress Notifications](#progress-notifications)
   5. [Goal Analytics](#goal-analytics)

## AI-Powered Travel Planner Implementation

### Travel Planner Models

Let's start by defining the data models for the travel planner:

```csharp
using System;
using System.Collections.Generic;

namespace WisdomGuidedAspire.ApiService.Models
{
    public class TravelRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Destination { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Budget { get; set; }
        public int TravelerCount { get; set; } = 1;
        public List<string> Preferences { get; set; } = new List<string>();
        public List<string> ExcludedActivities { get; set; } = new List<string>();
        public TravelRequestStatus Status { get; set; } = TravelRequestStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string UserId { get; set; }
        public string UserEmail { get; set; }
    }

    public enum TravelRequestStatus
    {
        Pending,
        Processing,
        Completed,
        Failed
    }

    public class TravelItinerary
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string RequestId { get; set; }
        public List<ItineraryDay> Days { get; set; } = new List<ItineraryDay>();
        public Transportation Transportation { get; set; }
        public Accommodation Accommodation { get; set; }
        public BudgetBreakdown BudgetBreakdown { get; set; }
        public List<string> Tips { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }
    }

    public class ItineraryDay
    {
        public int DayNumber { get; set; }
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public List<Activity> Activities { get; set; } = new List<Activity>();
        public List<MealRecommendation> Meals { get; set; } = new List<MealRecommendation>();
        public decimal EstimatedCost { get; set; }
    }

    public class Activity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Location { get; set; }
        public decimal EstimatedCost { get; set; }
        public string BookingUrl { get; set; }
        public string Category { get; set; }
    }

    public class MealRecommendation
    {
        public string Type { get; set; } // Breakfast, Lunch, Dinner
        public string VenueName { get; set; }
        public string Description { get; set; }
        public string Cuisine { get; set; }
        public decimal EstimatedCost { get; set; }
        public string Address { get; set; }
    }

    public class Transportation
    {
        public List<FlightOption> FlightOptions { get; set; } = new List<FlightOption>();
        public List<LocalTransportOption> LocalTransportOptions { get; set; } = new List<LocalTransportOption>();
        public decimal EstimatedTotalCost { get; set; }
    }

    public class FlightOption
    {
        public string Airline { get; set; }
        public string DepartureAirport { get; set; }
        public string ArrivalAirport { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public decimal Price { get; set; }
        public string BookingUrl { get; set; }
    }

    public class LocalTransportOption
    {
        public string Type { get; set; } // Taxi, Bus, Subway, etc.
        public string Description { get; set; }
        public decimal EstimatedCost { get; set; }
        public string Website { get; set; }
    }

    public class Accommodation
    {
        public List<HotelOption> HotelOptions { get; set; } = new List<HotelOption>();
        public decimal EstimatedTotalCost { get; set; }
    }

    public class HotelOption
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public int StarRating { get; set; }
        public List<string> Amenities { get; set; } = new List<string>();
        public decimal PricePerNight { get; set; }
        public decimal TotalPrice { get; set; }
        public string BookingUrl { get; set; }
    }

    public class BudgetBreakdown
    {
        public decimal Accommodation { get; set; }
        public decimal Transportation { get; set; }
        public decimal Meals { get; set; }
        public decimal Activities { get; set; }
        public decimal Miscellaneous { get; set; }
        public decimal TotalEstimatedCost { get; set; }
        public decimal RemainingBudget { get; set; }
    }
}
```

### Travel Agent System

Now, let's create a multi-agent system for the travel planner using the Semantic Kernel AI Agent Framework:

```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using WisdomGuidedAspire.ApiService.Models;

namespace WisdomGuidedAspire.ApiService.Services
{
    public class TravelPlannerService
    {
        private readonly AgentFactory _agentFactory;
        private readonly WisdomLogger _logger;
        private readonly IDistributedCache _cache;
        private readonly AgentTelemetry _telemetry;
        private readonly string _travelRequestsCacheKey = "TravelRequests";
        private readonly string _travelItinerariesCacheKey = "TravelItineraries";
        
        public TravelPlannerService(
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
        
        public async Task<AgentChat> CreateTravelPlannerAgentSystem()
        {
            // Define agent parameters
            var destinationExpertParams = new Dictionary<string, string>
            {
                ["role"] = "destination expert",
                ["expertise"] = "knowledge of global travel destinations, attractions, and local customs"
            };
            
            var accommodationExpertParams = new Dictionary<string, string>
            {
                ["role"] = "accommodation expert",
                ["expertise"] = "finding suitable hotels and lodging options within budget constraints"
            };
            
            var transportationExpertParams = new Dictionary<string, string>
            {
                ["role"] = "transportation expert",
                ["expertise"] = "flight options, local transportation, and travel logistics"
            };
            
            var activitiesExpertParams = new Dictionary<string, string>
            {
                ["role"] = "activities expert",
                ["expertise"] = "local activities, tours, attractions, and dining options"
            };
            
            var budgetExpertParams = new Dictionary<string, string>
            {
                ["role"] = "budget analyst",
                ["expertise"] = "optimizing travel expenses and ensuring the plan stays within budget"
            };
            
            // Create specialized travel agents
            var destinationExpert = await _agentFactory.CreateAgent("DestinationExpert", destinationExpertParams);
            var accommodationExpert = await _agentFactory.CreateAgent("AccommodationExpert", accommodationExpertParams);
            var transportationExpert = await _agentFactory.CreateAgent("TransportationExpert", transportationExpertParams);
            var activitiesExpert = await _agentFactory.CreateAgent("ActivitiesExpert", activitiesExpertParams);
            var budgetExpert = await _agentFactory.CreateAgent("BudgetExpert", budgetExpertParams);
            
            // Create the coordinating travel planner
            var travelPlanner = await _agentFactory.CreateAgent("TravelPlanner", new Dictionary<string, string>
            {
                ["role"] = "travel planner coordinator",
                ["expertise"] = "coordinating between different travel experts to create a cohesive travel plan"
            });
            
            // Create agent chat
            var chat = new AgentChat();
            chat.AddAgent(travelPlanner);
            chat.AddAgent(destinationExpert);
            chat.AddAgent(accommodationExpert);
            chat.AddAgent(transportationExpert);
            chat.AddAgent(activitiesExpert);
            chat.AddAgent(budgetExpert);
            
            _logger.LogInformation("Created travel planner agent system with 6 agents");
            
            return chat;
        }
        
        public async Task<TravelRequest> CreateTravelRequest(TravelRequest request)
        {
            // Get existing requests
            var requests = await GetAllTravelRequests();
            
            // Add new request
            requests.Add(request);
            
            // Save requests
            await SaveTravelRequests(requests);
            
            // Log the request
            await _logger.LogActivity(
                "TravelRequestCreated",
                $"Created travel request for {request.Destination} ({request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd})",
                $"Budget: ${request.Budget}, Travelers: {request.TravelerCount}, Preferences: {string.Join(", ", request.Preferences)}"
            );
            
            // Return the request
            return request;
        }
        
        public async Task<TravelItinerary> GenerateTravelItinerary(string requestId)
        {
            using var activity = _telemetry.StartAgentActivity(
                "TravelPlanner", 
                "GenerateItinerary", 
                new Dictionary<string, string> { ["requestId"] = requestId }
            );
            
            // Get the request
            var requests = await GetAllTravelRequests();
            var request = requests.Find(r => r.Id == requestId);
            
            if (request == null)
            {
                throw new ArgumentException($"Travel request with ID {requestId} not found");
            }
            
            // Update request status
            request.Status = TravelRequestStatus.Processing;
            await SaveTravelRequests(requests);
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Create the agent system
                var agentChat = await CreateTravelPlannerAgentSystem();
                
                // Format the travel request for the agents
                string travelQuery = $@"
                    Please create a travel itinerary for the following request:
                    
                    Destination: {request.Destination}
                    Dates: {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd} ({(request.EndDate - request.StartDate).TotalDays} days)
                    Budget: ${request.Budget}
                    Number of travelers: {request.TravelerCount}
                    Preferences: {string.Join(", ", request.Preferences)}
                    Activities to avoid: {string.Join(", ", request.ExcludedActivities)}
                    
                    Please provide a detailed itinerary including:
                    1. Day-by-day schedule with activities and estimated costs
                    2. Recommended accommodation options
                    3. Transportation options including flights
                    4. Budget breakdown
                    5. Local tips and recommendations
                ";
                
                // Create chat history
                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage(travelQuery);
                
                // Process the request through the agent system
                string agentResponse = "";
                
                await foreach (var response in agentChat.InvokeAsync(chatHistory))
                {
                    agentResponse += response.Content + "\n";
                }
                
                // Parse the response into a TravelItinerary object
                var itinerary = ParseAgentResponseToItinerary(agentResponse, requestId);
                
                // Get existing itineraries
                var itineraries = await GetAllTravelItineraries();
                
                // Add new itinerary
                itineraries.Add(itinerary);
                
                // Save itineraries
                await SaveTravelItineraries(itineraries);
                
                // Update request status
                request.Status = TravelRequestStatus.Completed;
                await SaveTravelRequests(requests);
                
                // Log the completion
                stopwatch.Stop();
                
                await _logger.LogActivity(
                    "TravelItineraryGenerated",
                    $"Generated travel itinerary for {request.Destination} ({request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd})",
                    $"Processing time: {stopwatch.ElapsedMilliseconds}ms, Total cost: ${itinerary.BudgetBreakdown.TotalEstimatedCost}"
                );
                
                // Return the itinerary
                return itinerary;
            }
            catch (Exception ex)
            {
                // Update request status
                request.Status = TravelRequestStatus.Failed;
                await SaveTravelRequests(requests);
                
                // Log the error
                _logger.LogError(ex, "Error generating travel itinerary for request {RequestId}", requestId);
                throw;
            }
        }
        
        private TravelItinerary ParseAgentResponseToItinerary(string agentResponse, string requestId)
        {
            // In a real implementation, you would parse the agent response into a structured itinerary
            // Here, we're creating a simple mock itinerary for demonstration purposes
            
            // Get the travel request
            var requests = GetAllTravelRequests().Result;
            var request = requests.Find(r => r.Id == requestId);
            
            if (request == null)
            {
                throw new ArgumentException($"Travel request with ID {requestId} not found");
            }
            
            int daysCount = (int)(request.EndDate - request.StartDate).TotalDays + 1;
            
            var itinerary = new TravelItinerary
            {
                RequestId = requestId,
                Days = new List<ItineraryDay>(),
                Transportation = new Transportation
                {
                    FlightOptions = new List<FlightOption>
                    {
                        new FlightOption
                        {
                            Airline = "Sample Airline",
                            DepartureAirport = "Origin Airport",
                            ArrivalAirport = $"{request.Destination} Airport",
                            DepartureTime = request.StartDate.AddHours(-3),
                            ArrivalTime = request.StartDate,
                            Price = request.Budget * 0.3m,
                            BookingUrl = "https://example.com/booking"
                        }
                    },
                    LocalTransportOptions = new List<LocalTransportOption>
                    {
                        new LocalTransportOption
                        {
                            Type = "Taxi",
                            Description = "Airport to hotel transfer",
                            EstimatedCost = 50,
                            Website = "https://example.com/taxi"
                        },
                        new LocalTransportOption
                        {
                            Type = "Public Transit",
                            Description = "Local transportation options",
                            EstimatedCost = 25 * daysCount,
                            Website = "https://example.com/transit"
                        }
                    },
                    EstimatedTotalCost = request.Budget * 0.3m + 50 + (25 * daysCount)
                },
                Accommodation = new Accommodation
                {
                    HotelOptions = new List<HotelOption>
                    {
                        new HotelOption
                        {
                            Name = "Sample Hotel",
                            Description = "Centrally located hotel with great amenities",
                            Address = $"{request.Destination} Central Street",
                            StarRating = 4,
                            Amenities = new List<string> { "Wi-Fi", "Breakfast", "Pool" },
                            PricePerNight = request.Budget * 0.1m / daysCount,
                            TotalPrice = request.Budget * 0.1m,
                            BookingUrl = "https://example.com/hotel"
                        }
                    },
                    EstimatedTotalCost = request.Budget * 0.1m
                },
                Tips = new List<string>
                {
                    "Best time to visit popular attractions is early morning",
                    "Local transportation passes can save money",
                    "Don't forget to try the local cuisine"
                }
            };
            
            // Create daily itinerary
            decimal dailyActivityBudget = (request.Budget * 0.4m) / daysCount;
            decimal dailyMealBudget = (request.Budget * 0.2m) / daysCount;
            
            for (int i = 0; i < daysCount; i++)
            {
                var day = new ItineraryDay
                {
                    DayNumber = i + 1,
                    Title = $"Day {i + 1}: Exploring {request.Destination}",
                    Date = request.StartDate.AddDays(i),
                    Activities = new List<Activity>
                    {
                        new Activity
                        {
                            Name = $"Morning Activity on Day {i + 1}",
                            Description = "Visit a popular attraction",
                            StartTime = new TimeSpan(9, 0, 0),
                            EndTime = new TimeSpan(12, 0, 0),
                            Location = $"{request.Destination} Main Attraction",
                            EstimatedCost = dailyActivityBudget * 0.4m,
                            BookingUrl = "https://example.com/activity1",
                            Category = "Sightseeing"
                        },
                        new Activity
                        {
                            Name = $"Afternoon Activity on Day {i + 1}",
                            Description = "Explore local culture",
                            StartTime = new TimeSpan(14, 0, 0),
                            EndTime = new TimeSpan(17, 0, 0),
                            Location = $"{request.Destination} Cultural Center",
                            EstimatedCost = dailyActivityBudget * 0.6m,
                            BookingUrl = "https://example.com/activity2",
                            Category = "Cultural"
                        }
                    },
                    Meals = new List<MealRecommendation>
                    {
                        new MealRecommendation
                        {
                            Type = "Breakfast",
                            VenueName = "Hotel Breakfast",
                            Description = "Included with hotel stay",
                            Cuisine = "Continental",
                            EstimatedCost = 0,
                            Address = $"{request.Destination} Central Street"
                        },
                        new MealRecommendation
                        {
                            Type = "Lunch",
                            VenueName = "Local Cafe",
                            Description = "Casual lunch spot with local favorites",
                            Cuisine = "Local",
                            EstimatedCost = dailyMealBudget * 0.3m,
                            Address = $"{request.Destination} Food District"
                        },
                        new MealRecommendation
                        {
                            Type = "Dinner",
                            VenueName = "Restaurant",
                            Description = "Popular dinner restaurant",
                            Cuisine = "International",
                            EstimatedCost = dailyMealBudget * 0.7m,
                            Address = $"{request.Destination} Dining Street"
                        }
                    },
                    EstimatedCost = dailyActivityBudget + dailyMealBudget
                };
                
                itinerary.Days.Add(day);
            }
            
            // Create budget breakdown
            itinerary.BudgetBreakdown = new BudgetBreakdown
            {
                Accommodation = itinerary.Accommodation.EstimatedTotalCost,
                Transportation = itinerary.Transportation.EstimatedTotalCost,
                Meals = dailyMealBudget * daysCount,
                Activities = dailyActivityBudget * daysCount,
                Miscellaneous = request.Budget * 0.05m,
                TotalEstimatedCost = itinerary.Accommodation.EstimatedTotalCost + 
                                     itinerary.Transportation.EstimatedTotalCost + 
                                     (dailyMealBudget * daysCount) + 
                                     (dailyActivityBudget * daysCount) + 
                                     (request.Budget * 0.05m),
                RemainingBudget = request.Budget - (
                                     itinerary.Accommodation.EstimatedTotalCost + 
                                     itinerary.Transportation.EstimatedTotalCost + 
                                     (dailyMealBudget * daysCount) + 
                                     (dailyActivityBudget * daysCount) + 
                                     (request.Budget * 0.05m)
                                 )
            };
            
            return itinerary;
        }
        
        public async Task<List<TravelRequest>> GetAllTravelRequests()
        {
            string requestsJson = await _cache.GetStringAsync(_travelRequestsCacheKey);
            
            if (string.IsNullOrEmpty(requestsJson))
            {
                return new List<TravelRequest>();
            }
            
            return System.Text.Json.JsonSerializer.Deserialize<List<TravelRequest>>(requestsJson);
        }
        
        private async Task SaveTravelRequests(List<TravelRequest> requests)
        {
            string requestsJson = System.Text.Json.JsonSerializer.Serialize(requests);
            
            await _cache.SetStringAsync(
                _travelRequestsCacheKey,
                requestsJson,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
                }
            );
        }
        
        public async Task<List<TravelItinerary>> GetAllTravelItineraries()
        {
            string itinerariesJson = await _cache.GetStringAsync(_travelItinerariesCacheKey);
            
            if (string.IsNullOrEmpty(itinerariesJson))
            {
                return new List<TravelItinerary>();
            }
            
            return System.Text.Json.JsonSerializer.Deserialize<List<TravelItinerary>>(itinerariesJson);
        }
        
        private async Task SaveTravelItineraries(List<TravelItinerary> itineraries)
        {
            string itinerariesJson = System.Text.Json.JsonSerializer.Serialize(itineraries);
            
            await _cache.SetStringAsync(
                _travelItinerariesCacheKey,
                itinerariesJson,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
                }
            );
        }
        
        public async Task<TravelRequest> GetTravelRequestById(string id)
        {
            var requests = await GetAllTravelRequests();
            return requests.Find(r => r.Id == id);
        }
        
        public async Task<TravelItinerary> GetTravelItineraryByRequestId(string requestId)
        {
            var itineraries = await GetAllTravelItineraries();
            return itineraries.Find(i => i.RequestId == requestId);
        }
    }
}
```

### Travel Service Integrations

In a real implementation, you would integrate with travel APIs for flights, hotels, and activities. For demonstration purposes, let's create a mock service that can be replaced with real integrations later:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WisdomGuidedAspire.ApiService.Models;

namespace WisdomGuidedAspire.ApiService.Services
{
    public class TravelApiIntegration
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TravelApiIntegration> _logger;
        
        public TravelApiIntegration(
            HttpClient httpClient,
            ILogger<TravelApiIntegration> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }
        
        public async Task<List<FlightOption>> SearchFlights(
            string origin,
            string destination,
            DateTime departureDate,
            DateTime returnDate,
            int passengers)
        {
            // In a real implementation, you would call a flight API like Skyscanner or Amadeus
            // Mock implementation for demonstration purposes
            
            _logger.LogInformation(
                "Searching flights from {Origin} to {Destination} ({DepartureDate} to {ReturnDate}) for {Passengers} passengers",
                origin, destination, departureDate, returnDate, passengers);
            
            // Generate mock flight options
            return new List<FlightOption>
            {
                new FlightOption
                {
                    Airline = "Sample Airline 1",
                    DepartureAirport = origin,
                    ArrivalAirport = destination,
                    DepartureTime = departureDate.AddHours(8),
                    ArrivalTime = departureDate.AddHours(10),
                    Price = 350,
                    BookingUrl = "https://example.com/flight1"
                },
                new FlightOption
                {
                    Airline = "Sample Airline 2",
                    DepartureAirport = origin,
                    ArrivalAirport = destination,
                    DepartureTime = departureDate.AddHours(12),
                    ArrivalTime = departureDate.AddHours(14),
                    Price = 450,
                    BookingUrl = "https://example.com/flight2"
                }
            };
        }
        
        public async Task<List<HotelOption>> SearchHotels(
            string destination,
            DateTime checkIn,
            DateTime checkOut,
            int guests,
            decimal maxPrice)
        {
            // In a real implementation, you would call a hotel API like Booking.com or Hotels.com
            // Mock implementation for demonstration purposes
            
            _logger.LogInformation(
                "Searching hotels in {Destination} ({CheckIn} to {CheckOut}) for {Guests} guests with max price {MaxPrice}",
                destination, checkIn, checkOut, guests, maxPrice);
            
            // Generate mock hotel options
            return new List<HotelOption>
            {
                new HotelOption
                {
                    Name = "City Center Hotel",
                    Description = "Located in the heart of the city",
                    Address = $"{destination} Main Street",
                    StarRating = 4,
                    Amenities = new List<string> { "Wi-Fi", "Breakfast", "Pool" },
                    PricePerNight = 150,
                    TotalPrice = 150 * (int)(checkOut - checkIn).TotalDays,
                    BookingUrl = "https://example.com/hotel1"
                },
                new HotelOption
                {
                    Name = "Budget Inn",
                    Description = "Affordable accommodation near attractions",
                    Address = $"{destination} Budget Street",
                    StarRating = 3,
                    Amenities = new List<string> { "Wi-Fi", "Breakfast" },
                    PricePerNight = 100,
                    TotalPrice = 100 * (int)(checkOut - checkIn).TotalDays,
                    BookingUrl = "https://example.com/hotel2"
                }
            };
        }
        
        public async Task<List<Activity>> SearchActivities(
            string destination,
            DateTime date,
            List<string> preferences,
            List<string> excluded)
        {
            // In a real implementation, you would call an activities API like TripAdvisor or Viator
            // Mock implementation for demonstration purposes
            
            _logger.LogInformation(
                "Searching activities in {Destination} on {Date} with preferences {Preferences}",
                destination, date, string.Join(", ", preferences));
            
            // Generate mock activity options
            return new List<Activity>
            {
                new Activity
                {
                    Name = "City Tour",
                    Description = "Explore the highlights of the city",
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(12, 0, 0),
                    Location = $"{destination} City Center",
                    EstimatedCost = 50,
                    BookingUrl = "https://example.com/activity1",
                    Category = "Sightseeing"
                },
                new Activity
                {
                    Name = "Local Experience",
                    Description = "Experience local culture and traditions",
                    StartTime = new TimeSpan(14, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0),
                    Location = $"{destination} Local District",
                    EstimatedCost = 35,
                    BookingUrl = "https://example.com/activity2",
                    Category = "Cultural"
                }
            };
        }
        
        public async Task<List<MealRecommendation>> GetRestaurantRecommendations(
            string destination,
            DateTime date,
            List<string> cuisinePreferences)
        {
            // In a real implementation, you would call a restaurant API like Yelp or TripAdvisor
            // Mock implementation for demonstration purposes
            
            _logger.LogInformation(
                "Searching restaurants in {Destination} on {Date} with preferences {Preferences}",
                destination, date, string.Join(", ", cuisinePreferences));
            
            // Generate mock restaurant recommendations
            return new List<MealRecommendation>
            {
                new MealRecommendation
                {
                    Type = "Lunch",
                    VenueName = "Local Cafe",
                    Description