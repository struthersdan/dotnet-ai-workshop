using System.Diagnostics;
using Microsoft.Extensions.AI;

namespace Embeddings;

public class FaissSemanticSearch
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

        var index = await LoadOrCreateIndexAsync("index_hnsw.bin", githubIssues);


        // TODO: Build an index using FAISS
        while (true)
        {
            Console.Write("\nQuery: ");
            var input = Console.ReadLine()!;
            if (input == "") break;

            var inputEmbedding = await EmbeddingGenerator.GenerateVectorAsync(input);
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

        var index = FaissNet.Index.Create(EmbeddingDimension, "IDMap2,HNSW32", FaissNet.MetricType.METRIC_INNER_PRODUCT);

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
