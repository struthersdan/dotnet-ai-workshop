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

        // TODO: Build an index using FAISS
    }
}
