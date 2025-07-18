using Microsoft.Extensions.AI;
using System.Numerics.Tensors;

namespace Embeddings;

public class SentenceSimilarity
{
    public async Task RunAsync()
    {
        // Note: First run "ollama pull all-minilm" then "ollama serve"
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator =
            new OllamaEmbeddingGenerator(new Uri("http://127.0.0.1:11434"), modelId: "all-minilm");

        // TODO: Add your code here
        var embedding = await embeddingGenerator.GenerateVectorAsync("Hello, world!");
        Console.WriteLine($"Embedding dimensions: {embedding.Span.Length}");
        foreach (var value in embedding.Span)
        {
            Console.Write("{0:0.00}, ", value);
        }

        var catVector = await embeddingGenerator.GenerateVectorAsync("cat");
        var dogVector = await embeddingGenerator.GenerateVectorAsync("dog");
        var kittenVector = await embeddingGenerator.GenerateVectorAsync("kitten");

        Console.WriteLine($"Cat-dog similarity: {TensorPrimitives.CosineSimilarity(catVector.Span, dogVector.Span):F2}");
        Console.WriteLine($"Cat-kitten similarity: {TensorPrimitives.CosineSimilarity(catVector.Span, kittenVector.Span):F2}");
        Console.WriteLine($"Dog-kitten similarity: {TensorPrimitives.CosineSimilarity(dogVector.Span, kittenVector.Span):F2}");

    }
}
