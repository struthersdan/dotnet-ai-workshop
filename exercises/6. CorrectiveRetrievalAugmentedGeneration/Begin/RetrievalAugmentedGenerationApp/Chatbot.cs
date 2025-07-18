using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;
using Qdrant.Client;

namespace RetrievalAugmentedGenerationApp;

public class Chatbot(
    IChatClient chatClient,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    QdrantClient qdrantClient)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var currentProduct = Helpers.GetCurrentProduct();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Assistant: Hi! You're looking at the {currentProduct.Model}. What do you want to know about it?");

        // TODO: Implement the chat loop here
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
