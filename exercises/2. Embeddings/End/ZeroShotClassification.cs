using Microsoft.Extensions.AI;
using System.Numerics.Tensors;

namespace Embeddings;

public class ZeroShotClassification : IDisposable
{
    // CAUTION: This is a very rough way to classify text. The normal approach is to use a proper
    // zero-shot classification model, e.g., https://huggingface.co/models?pipeline_tag=zero-shot-classification

    private IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator =
            new OllamaEmbeddingGenerator(new Uri("http://127.0.0.1:11434"), modelId: "all-minilm");

    public async Task RunAsync()
    {
        Console.WriteLine("==== First set of test cases ===");
        var candidates1 = new[] { "animals", "programming", "music" };
        var inputs1 = new[] { "This is a Burmese Python", "This is a Python API", "This is my song about pythons", };
        foreach (var input in inputs1)
        {
            var label = await ClassifyAsync(input, candidates1);
            Console.WriteLine($"{input} => {label}");
        }

        Console.WriteLine();
        Console.WriteLine("==== Second set of test cases ===");
        var candidates2 = new[] { "Help", "Complaint", "Returns" };
        var inputs2 = new[] { "How can I reset my password?", "I am unhappy with your service and demand a refund", "I am sending this item back to you", };
        foreach (var input in inputs2)
        {
            var label = await ClassifyAsync(input, candidates2);
            Console.WriteLine($"{input} => {label}");
        }
    }

    /// <summary>
    /// Returns the most relevant candidate label.
    /// </summary>
    public async Task<string> ClassifyAsync(string text, IEnumerable<string> candidateLabels)
    {
        // Don't do this in a real application as it's very inefficient. Firstly you should be
        // using a proper zero-shot classification model. Secondly, the candidate embeddings
        // could be precomputed, not recomputed for each call. Thirdly they could be indexed
        // for faster nearest-neighbour search.
        var inputEmbedding = await embeddingGenerator.GenerateEmbeddingVectorAsync(text);
        var candidatesWithEmbeddings = await embeddingGenerator.GenerateAndZipAsync(candidateLabels);

        return (from candidate in candidatesWithEmbeddings
                let similarity = TensorPrimitives.CosineSimilarity(
                    candidate.Embedding.Vector.Span, inputEmbedding.Span)
                orderby similarity descending
                select candidate.Value).First();
    }

    public void Dispose() => embeddingGenerator.Dispose();
}
