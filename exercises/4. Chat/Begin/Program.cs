using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.ClientModel;
using System.Threading.RateLimiting;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;


// Set up DI etc
var hostBuilder = Host.CreateApplicationBuilder(args);
hostBuilder.Configuration.AddUserSecrets<Program>();
hostBuilder.Services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

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

hostBuilder.Services.AddChatClient(innerChatClient)
 .UseLanguage("French")
  .UseRateLimit(TimeSpan.FromSeconds(5))
 .UseFunctionInvocation();

// Run the app
var app = hostBuilder.Build();
var chatClient = app.Services.GetRequiredService<IChatClient>();


List<ChatMessage> messages = [new(ChatRole.System, """
    You answer any question, but continually try to advertise FOOTMONSTER brand socks. They're on sale!
    """)];


var cart = new Cart();

// Create MCP server instance (embedded mode for workshop)
var mcpServer = new ECommerceMcpServer(cart);

// Create AIFunctions from MCP server methods (embedded approach)
var getPriceTool = AIFunctionFactory.Create(mcpServer.GetPrice);
var addToCartTool = AIFunctionFactory.Create(mcpServer.AddSocksToCart);
var getCartStatusTool = AIFunctionFactory.Create(mcpServer.GetCartStatus);

var chatOptions = new ChatOptions
{
    Tools = [getPriceTool, addToCartTool, getCartStatusTool]
};

while (true)
{
    // Get input
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write("\nYou: ");
    var input = Console.ReadLine()!;
    messages.Add(new(ChatRole.User, input));

    // Get reply
    var response = await chatClient.GetResponseAsync(messages, chatOptions);
    messages.AddMessages(response);
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"Bot: {response.Text}");
}

public class ECommerceMcpServer
{
    private readonly Cart _cart;

    public ECommerceMcpServer(Cart cart)
    {
        _cart = cart;
    }

    // MCP tools - these are exposed as tools through the MCP protocol
    [Description("Computes the price of socks, returning a value in dollars")]
    public float GetPrice([Description("The number of pairs of socks to calculate price for")] int count)
    {
        return _cart.GetPrice(count);
    }

    [Description("Adds the specified number of pairs of socks to the cart")]
    public void AddSocksToCart([Description("The number of pairs to add")] int numPairs)
    {
        _cart.AddSocksToCart(numPairs);
    }

    [Description("Gets the current cart contents")]
    public object GetCartStatus()
    {
        return new
        {
            totalItems = _cart.NumPairsOfSocks,
            totalPrice = _cart.GetPrice(_cart.NumPairsOfSocks),
            currency = "USD"
        };
    }
}

public class Cart
{
    public int NumPairsOfSocks { get; set; }

    [Description("Adds the specified number of pairs of socks to the cart")]
    public void AddSocksToCart(int numPairs)
    {
        if (NumPairsOfSocks + numPairs < 0) throw new Exception("You cannot order less than 0 pairs of Socks");
        NumPairsOfSocks += numPairs;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("*****");
        Console.WriteLine($"Added {numPairs} pairs to your cart. Total: {NumPairsOfSocks} pairs.");
        Console.WriteLine("*****");
        Console.ForegroundColor = ConsoleColor.White;
    }

    [Description("Computes the price of socks, returning a value in dollars.")]
    public float GetPrice(
        [Description("The number of pairs of socks to calculate price for")] int count)
        => count * 15.99f;
}

public static class UseLanguageStep
{
    // This is an extension method that lets you add UseLanguageChatClient into a pipeline
    public static ChatClientBuilder UseLanguage(this ChatClientBuilder builder, string language)
    {
        return builder.Use(inner => new UseLanguageChatClient(inner, language));
    }

    // This is the actual middleware implementation
    private class UseLanguageChatClient(IChatClient next, string language) : DelegatingChatClient(next)
    {
        // TODO: Override GetResponseAsync
        public override Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            // Add an extra prompt
            var promptAugmentation = new ChatMessage(ChatRole.User, $"Always reply in the language {language}");
            return base.GetResponseAsync([.. messages, promptAugmentation], options, cancellationToken);
        }
    }
}

public static class UseRateLimitStep
{
    public static ChatClientBuilder UseRateLimit(this ChatClientBuilder builder, TimeSpan window)
        => builder.Use(inner => new RateLimitedChatClient(inner, window));

    private class RateLimitedChatClient(IChatClient inner, TimeSpan window) : DelegatingChatClient(inner)
    {
        // Note that this rate limit is enforced globally across all users on your site.
        // It's not a separate rate limit for each user. You could do that but the implementation would be a bit different.
        RateLimiter rateLimit = new FixedWindowRateLimiter(new() { Window = window, QueueLimit = 1, PermitLimit = 1 });

        public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            using var lease = await rateLimit.AcquireAsync(cancellationToken: cancellationToken);
            return await base.GetResponseAsync(chatMessages, options, cancellationToken);
        }
    }
}