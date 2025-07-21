using Microsoft.Extensions.AI;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace RetrievalAugmentedGenerationApp;

public class ChatbotThread(
    IChatClient chatClient,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    QdrantClient qdrantClient,
    Product currentProduct)
{
    private List<ChatMessage> _messages =
    [
        new ChatMessage(ChatRole.System, $"""
        You are a helpful assistant, here to help customer service staff answer questions they have received from customers.
        The support staff member is currently answering a question about this product:
        ProductId: ${currentProduct.ProductId}
        Brand: ${currentProduct.Brand}
        Model: ${currentProduct.Model}
        """),
];

    public async Task<(string Text, Citation? Citation)> AnswerAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        // For a simple version of RAG, we'll embed the user's message directly and
        // add the closest few manual chunks to context.
        var userMessageEmbedding = await embeddingGenerator.GenerateVectorAsync(userMessage);
        var closestChunks = await qdrantClient.SearchAsync(
            collectionName: "manuals",
            vector: userMessageEmbedding.ToArray(),
            filter: Qdrant.Client.Grpc.Conditions.Match("productId", currentProduct.ProductId),
            limit: 5);
        // Now ask the chatbot
        _messages.Add(new(ChatRole.User, $$"""
    Give an answer using ONLY information from the following product manual extracts.
    If the product manual doesn't contain the information, you should say so. Do not make up information beyond what is given.
    Whenever relevant, specify manualExtractId to cite the manual extract that your answer is based on.

    {{string.Join(Environment.NewLine, closestChunks.Select(c => $"<manual_extract id='{c.Id}'>{c.Payload["text"].StringValue}</manual_extract>"))}}

    User question: {{userMessage}}
    Respond as a JSON object in this format: {
        "ManualExtractId": numberOrNull,
        "ManualQuote": stringOrNull, // The relevant verbatim quote from the manual extract, up to 10 words
        "AnswerText": string
    }
    """));

        var response = await chatClient.GetResponseAsync<ChatBotAnswer>(_messages, cancellationToken: cancellationToken);
        _messages.AddMessages(response);

        return response.TryGetResult(out var answer)
     ? (answer.AnswerText, Citation: GetCitation(answer, closestChunks))
     : ("Sorry, there was a problem.", default);
    }

    public record Citation(int ProductId, int PageNumber, string Quote);
    private record ChatBotAnswer(int? ManualExtractId, string? ManualQuote, string AnswerText);
    
    private static Citation? GetCitation(ChatBotAnswer answer, IReadOnlyList<ScoredPoint> chunks)
{
    return answer.ManualExtractId is int id && chunks.FirstOrDefault(c => c.Id.Num == (ulong)id) is { } chunk
        ? new Citation((int)chunk.Payload["productId"].IntegerValue, (int)chunk.Payload["pageNumber"].IntegerValue, answer.ManualQuote ?? "")
        : null;
}
}

