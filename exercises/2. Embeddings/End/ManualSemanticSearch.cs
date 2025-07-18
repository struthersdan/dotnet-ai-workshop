using System.Numerics.Tensors;
using Microsoft.Extensions.AI;

namespace Embeddings;

public class ManualSemanticSearch
{
    public async Task RunAsync()
    {
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator =
            new OllamaEmbeddingGenerator(new Uri("http://127.0.0.1:11434"), modelId: "all-minilm");

        // Create embeddings for all the test data
        var titlesWithEmbeddings = await embeddingGenerator.GenerateAndZipAsync(TestData.DocumentTitles.Values);
        Console.WriteLine($"Got {titlesWithEmbeddings.Length} title-embedding pairs");

        while (true)
        {
            Console.Write("\nQuery: ");
            var input = Console.ReadLine()!;
            if (input == "") break;

            var inputEmbedding = await embeddingGenerator.GenerateVectorAsync(input);

            var closest =
                from candidate in titlesWithEmbeddings
                let similarity = TensorPrimitives.CosineSimilarity(
                    candidate.Embedding.Vector.Span, inputEmbedding.Span)
                orderby similarity descending
                select new { candidate.Value, Similarity = similarity };

            foreach (var result in closest.Take(3))
            {
                Console.WriteLine($"({result.Similarity:F2}): {result.Value}");
            }
        }
    }
}
