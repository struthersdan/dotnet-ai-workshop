using System.Numerics.Tensors;
using Microsoft.Extensions.AI;

namespace Embeddings;

public class ManualSemanticSearch
{
    public async Task RunAsync()
    {
        // Note: First run "ollama pull all-minilm" then "ollama serve"
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator =
            new OllamaEmbeddingGenerator(new Uri("http://127.0.0.1:11434"), modelId: "all-minilm");

        // TODO: Add your code here
        var titlesWithEmbeddings = await embeddingGenerator.GenerateAndZipAsync(TestData.DocumentTitles.Values);
        Console.WriteLine($"Got {titlesWithEmbeddings.Length} title-embedding pairs");

        while (true)
        {
            Console.Write("\nQuery: ");
            var input = Console.ReadLine()!;
            if (input == "") break;

            // TODO: Compute embedding and search
            var inputEmbedding = await embeddingGenerator.GenerateVectorAsync(input);

            var closest =
                from candidate in titlesWithEmbeddings
                let similarity = DotProduct(
                    candidate.Embedding.Vector.Span, inputEmbedding.Span)
                orderby similarity descending
                select new { candidate.Value, Similarity = similarity };

            foreach (var result in closest.Take(3))
            {
                Console.WriteLine($"({result.Similarity:F2}): {result.Value}");
            }
        }
    }

    private static float DotProduct(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        // TODO: Implement this
        if (a.Length != b.Length)
        {
            throw new ArgumentException("Vectors must have the same length.");
        }
        float result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result += a[i] * b[i];
        }
        return result;
    }
}
