using Microsoft.Extensions.AI;

// Make sure you're running:
// - Ollama with "all-minilm" available (e.g. `ollama pull all-minilm` then `ollama serve`)
// - Qdrant in Docker (e.g., `docker run -p 6333:6333 -p 6334:6334 -v qdrant_storage:/qdrant/storage:z -d qdrant/qdrant`)
// Of course, you could also use Aspire to orchestrate this.

IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator =
    new OllamaEmbeddingGenerator(new Uri("http://127.0.0.1:11434"), modelId: "all-minilm");

var manualPdfDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../data/product-manuals"));
foreach (var filePath in Directory.EnumerateFiles(manualPdfDir, "*.pdf"))
{
    var productId = int.Parse(Path.GetFileNameWithoutExtension(filePath));
    Console.WriteLine($"Ingesting manual for product {productId}...");

    // TODO: Parse, chunk, embed, and store the PDF
}
