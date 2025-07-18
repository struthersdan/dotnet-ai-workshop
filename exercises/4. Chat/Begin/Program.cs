using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.ClientModel;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

// Set up DI etc
var hostBuilder = Host.CreateApplicationBuilder(args);
hostBuilder.Configuration.AddUserSecrets<Program>();
hostBuilder.Services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));

// Register an IChatClient

// For GitHub Models or Azure OpenAI:
var innerChatClient = new AzureOpenAIClient(
    new Uri(hostBuilder.Configuration["AI:Endpoint"]!),
    new ApiKeyCredential(hostBuilder.Configuration["AI:Key"]!))
    .GetChatClient("gpt-4o-mini").AsIChatClient();

// Or for OpenAI Platform:
// var innerChatClient = new OpenAI.Chat.ChatClient("gpt-4o-mini", hostBuilder.Configuration["AI:Key"]!).AsIChatClient();

// Or for Ollama:
//var innerChatClient = new OllamaChatClient(new Uri("http://localhost:11434"), "llama3.1");

hostBuilder.Services.AddChatClient(innerChatClient);

// Run the app
var app = hostBuilder.Build();
var chatClient = app.Services.GetRequiredService<IChatClient>();

var stories = await GetTopStories(20);

// Categorize them all at once
var response = await chatClient.GetResponseAsync<CategorizedHNStory[]>(
    $"For each of the following news stories, decide on a suitable category: {JsonSerializer.Serialize(stories)}");

// Display results
if (response.TryGetResult(out var categorized))
{
    foreach (var group in categorized.GroupBy(s => s.Category))
    {
        Console.WriteLine(group.Key);
        foreach (var story in group)
        {
            Console.WriteLine($" * [{story.Id}] {story.Title}");
        }
        Console.WriteLine();
    }
}

static async Task<HNStory[]> GetTopStories(int count)
{
    const string baseUrl = "https://hacker-news.firebaseio.com/v0";
    using var client = new HttpClient();
    var storyIds = await client.GetFromJsonAsync<int[]>($"{baseUrl}/topstories.json");
    var resultTasks = storyIds!.Take(count).Select(id => client.GetFromJsonAsync<HNStory>($"{baseUrl}/item/{id}.json")).ToArray();
    return (await Task.WhenAll(resultTasks))!;
}

record HNStory(int Id, string Title);
enum Category { AI, ProgrammingLanguages, Startups, History, Business, Society }

record CategorizedHNStory(int Id, string Title, Category Category);