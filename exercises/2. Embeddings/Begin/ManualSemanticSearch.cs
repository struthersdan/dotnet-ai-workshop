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
    }
}
