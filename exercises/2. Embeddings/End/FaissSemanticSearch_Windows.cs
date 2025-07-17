#if USE_FAISS_NET
using System.Diagnostics;
using Microsoft.Extensions.AI;
namespace Embeddings;

public class FaissSemanticSearch_Windows
{
    // The supplied test data contains 60,000 issues, but that may take too long to index
    // We'll work with a smaller set, but you can increase this if your machine can handle it
    private const int TestDataSetSize = 10000;

    // Keep in sync if you use a different model
    private const int EmbeddingDimension = 384;

    // Runs in process on CPU using a small embedding model
    // Alternatively use OllamaEmbeddingGenerator or OpenAIEmbeddingGenerator
    private IEmbeddingGenerator<string, Embedding<float>> EmbeddingGenerator { get; } =
        new LocalEmbeddingsGenerator(); 

    public async Task RunAsync()
    {
        var githubIssues = TestData.GitHubIssues.TakeLast(TestDataSetSize).ToDictionary(x => x.Number, x => x);
        var index = await LoadOrCreateIndexAsync("index.bin", githubIssues);

        // Search
        while (true)
        {
            Console.Write("\nQuery: ");
            var input = Console.ReadLine()!;
            if (input == "") break;

            var inputEmbedding = await EmbeddingGenerator.GenerateEmbeddingVectorAsync(input);
            var sw = new Stopwatch();
            sw.Start();
            var (resultDistances, resultIds) = index.SearchFlat(1, inputEmbedding.ToArray(), 3);
            sw.Stop();
            for (var i = 0; i < resultDistances.Length; i++)
            {
                var distance = resultDistances[i];
                var id = (int)resultIds[i];
                Console.WriteLine($"({distance:F2}): {githubIssues[id].Title}");
            }
            Console.WriteLine($"Search duration: {sw.ElapsedMilliseconds:F2}ms");
        }
    }

    private async Task<FaissNet.Index> LoadOrCreateIndexAsync(string filename, IDictionary<int, GitHubIssue> data)
    {
        if (File.Exists(filename))
        {
            var result = FaissNet.Index.Load(filename);
            Console.WriteLine($"Loaded index with {result.Count} entries");
            return result;
        }

        var index = FaissNet.Index.Create(EmbeddingDimension, "IDMap2,Flat", FaissNet.MetricType.METRIC_INNER_PRODUCT);

        // Build an index
        foreach (var issuesChunk in data.Chunk(1000))
        {
            Console.Write($"Embedding issues: {issuesChunk.First().Key} - {issuesChunk.Last().Key}");
            var embeddings = await EmbeddingGenerator.GenerateAsync(issuesChunk.Select(i => i.Value.Title));
            Console.WriteLine(" Inserting into index...");
            index.AddWithIds(
                embeddings.Select(e => e.Vector.ToArray()).ToArray(),
                issuesChunk.Select(i => (long)i.Key).ToArray());
        }

        index.Save(filename);
        return index;
    }
}

// Things to explore:
// - How's the performance of a flat index vs HNSW? How do each compare with the manually-implemented linear search?
//   (Answer: Flat is the same thing as manual linear search. HNSW takes longer to build but runs in sublinear time - vastly faster for large datasets,
//    but is approximate)
//   Try the index parameter: "IDMap2,HNSW32"
// - Can you find a faster or smaller semantic index? How does it differ on accuracy?
//   Hint: try FaissNet.Index.Create(EmbeddingDimension, "IDMap2,LSH", FaissNet.MetricType.METRIC_L2);
//   This gives a 548KiB index for 10000 issues and is very fast to build and search, versus 17.7MiB for HNSW32 (i.e., 32x size difference)
//   See https://github.com/facebookresearch/faiss/wiki/Faiss-indexes
//   and the hilariously complicated flowchart at https://github.com/facebookresearch/faiss/wiki/Guidelines-to-choose-an-index
//   In theory, managing indexing choices might be one of the benefits that a vector DB product could give you, though they still mostly
//   require you to make decisions yourself.
// - If you use OllamaEmbeddingGenerator and try out different embedding models, do the bigger ones produce higher-quality results?
//   You'll probably need to reduce TestDataSetSize otherwise it will take a long time to generate all the embeddings.

#endif // USE_FAISS_NET
