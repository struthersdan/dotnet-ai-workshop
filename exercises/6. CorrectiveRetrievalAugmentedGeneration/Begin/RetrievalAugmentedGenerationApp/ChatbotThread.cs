using Microsoft.Extensions.AI;
using Qdrant.Client;

namespace RetrievalAugmentedGenerationApp;

public class ChatbotThread(
    IChatClient chatClient,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    QdrantClient qdrantClient,
    Product currentProduct)
{
    private List<ChatMessage> _messages = [];

    public Task<(string Text, Citation? Citation)> AnswerAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public record Citation(int ProductId, int PageNumber, string Quote);
}
