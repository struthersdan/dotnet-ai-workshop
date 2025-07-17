using System.Numerics.Tensors;
using System.Runtime.Intrinsics;
using Microsoft.Extensions.AI;

namespace Embeddings;

public class SentenceSimilarity
{
    public async Task RunAsync()
    {
        // Note: First run "ollama pull all-minilm" then "ollama serve"
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator =
            new OllamaEmbeddingGenerator(new Uri("http://127.0.0.1:11434"), modelId: "all-minilm");

        // 1: Just generate an embedding
        var embedding = await embeddingGenerator.GenerateEmbeddingVectorAsync("Hello, world!");
        Console.WriteLine($"Embedding dimensions: {embedding.Span.Length}");
        foreach (var value in embedding.Span)
        {
            Console.Write("{0:0.00}, ", value);
        }

        // 2: Compute and compare embeddings
        var catVector = await embeddingGenerator.GenerateEmbeddingVectorAsync("cat");
        var dogVector = await embeddingGenerator.GenerateEmbeddingVectorAsync("dog");
        var kittenVector = await embeddingGenerator.GenerateEmbeddingVectorAsync("kitten");

        Console.WriteLine($"Cat-dog similarity: {TensorPrimitives.CosineSimilarity(catVector.Span, dogVector.Span):F2}");
        Console.WriteLine($"Cat-kitten similarity: {TensorPrimitives.CosineSimilarity(catVector.Span, kittenVector.Span):F2}");
        Console.WriteLine($"Dog-kitten similarity: {TensorPrimitives.CosineSimilarity(dogVector.Span, kittenVector.Span):F2}");

        Console.WriteLine($"Dog-kitten similarity (implemented manually): {DotProduct(dogVector.Span, kittenVector.Span):F2}");
        Console.WriteLine($"Dog-kitten similarity (SIMD): {DotProductSIMD(dogVector.Span, kittenVector.Span):F2}");
    }

    private static float DotProduct(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        var result = 0f;
        for (int i = 0; i < a.Length; i++)
        {
            result += a[i] * b[i];
        }
        return result;
    }

    private static unsafe float DotProductSIMD(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        var length = a.Length;
        var blockLength = Vector256<float>.Count;
        var result = Vector256<float>.Zero;

        fixed (float* aPtr = a)
        fixed (float* bPtr = b)
        {
            for (var pos = 0; pos < length; pos += blockLength)
            {
                var aVec = Vector256.Load(aPtr + pos);
                var bVec = Vector256.Load(bPtr + pos);
                var product = Vector256.Multiply(aVec, bVec);
                result = Vector256.Add(result, product);
            }
        }

        return Vector256.Sum(result);
    }
}
