using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using System.ClientModel;
using Microsoft.Extensions.Configuration;
using Qdrant.Client;
using RetrievalAugmentedGenerationApp;
using System.Text.Json;
using Evaluation;

// ------ GET SERVICES ------

var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

// For GitHub Models or Azure OpenAI:
IChatClient innerChatClient = new AzureOpenAIClient(new Uri(config["AI:Endpoint"]!), new ApiKeyCredential(config["AI:Key"]!))
    .GetChatClient("gpt-4o-mini").AsIChatClient();

// Or for OpenAI Platform:
// var aiConfig = config.GetRequiredSection("AI");
// var innerChatClient = new OpenAI.Chat.ChatClient("gpt-4o-mini", aiConfig["Key"]!).AsIChatClient();

// Or for Ollama:
// IChatClient innerChatClient = new OllamaChatClient(new Uri("http://127.0.0.1:11434"), "llama3.1");

var chatClient = new ChatClientBuilder(innerChatClient)
    .UseFunctionInvocation()
    .UseRetryOnRateLimit()
    .Build();

// There's nothing to stop you from using a different LLM for evaluation vs the one that actually powers the chatbot
// In fact, really you *should* use the best LLM you can for scoring, even when testing out a smaller model for the chatbot
// In this case we'll use the same for both, since you might only have access to one of them.
var evaluationChatClient = new ChatClientBuilder(innerChatClient)
    .UseRetryOnRateLimit()
    .Build();

var embeddingGenerator = new OllamaEmbeddingGenerator(new Uri("http://127.0.0.1:11434"), modelId: "all-minilm");

var qdrantClient = new QdrantClient("127.0.0.1");

// ------ LOAD TEST DATA ------

var products = Helpers.GetAllProducts().ToDictionary(p => p.ProductId, p => p);
var evalQuestions = JsonSerializer.Deserialize<EvalQuestion[]>(File.ReadAllText(Path.Combine(Helpers.DataDir, "evalquestions.json")))!;

// ------ RUN EVALUATION LOOP ------

// TODO: Implement evaluation here
var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 2 };
var outputLock = new object();
var runningAverageCount = 0;
var runningAverageContextRelevance = 0.0; // If low, the context isn't helping (need to improve the "retrieval" phase)
var runningAverageAnswerGroundedness = 0.0; // If low, we're probably hallucinating (even if the answer is true)
var runningAverageAnswerCorrectness = 0.0; // If low, then it's wrong (even if grounded in some context)

await Parallel.ForEachAsync(evalQuestions, parallelOptions, async (evalQuestion, cancellationToken) =>
{
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($"Asking question {evalQuestion.QuestionId}...");
    var thread = new ChatbotThread(chatClient, embeddingGenerator, qdrantClient, products[evalQuestion.ProductId]);
    var answer = await thread.AnswerAsync(evalQuestion.Question, cancellationToken);

    // Assess the quality of the answer
// Note that ideally, "relevance" should be based on *all* the context we supply to the LLM, not just the citation it selects
var response = await evaluationChatClient.GetResponseAsync<EvaluationResponse>($$"""
    There is an AI assistant that helps customer support staff to answer questions about products.
    You are evaluating the quality of the answer given by the AI assistant for the following question.

    <question>{{evalQuestion.Question}}</question>
    <truth>{{evalQuestion.Answer}}</truth>
    <context>{{answer.Citation?.Quote}}</context>
    <answer_given>{{answer.Text}}</answer_given>

    You are to provide three scores:

    1. Score the relevance of <context> to <question>.
       Ignore <truth> when scoring this. Does <context> contain information that may answer <question>?
    2. Score the groundedness of <answer_given> in <context>
       Ignore <truth> when scoring this. Does <answer_given> take its main claim from <context> alone?
    2. Score the correctness of <answer_given> based on <truth>.
       Does <answer_given> contain the facts from <truth>?

    Each score comes with a short justification, and must be one of the following labels:
     * Awful: it's completely unrelated to the target or contradicts it
     * Poor: it misses essential information from the target
     * Good: it includes the main information from the target, but misses smaller details
     * Perfect: it includes all important information from the target and does not contradict it

    Respond as JSON object of the form {
      "ContextRelevance": { "Justification": string, "ScoreLabel": string },
      "AnswerGroundedness": { "Justification": string, "ScoreLabel": string },
      "AnswerCorrectness": { "Justification": string, "ScoreLabel": string },
    }
    """);
if (response.TryGetResult(out var score) && score.Populated)
{
    lock (outputLock)
    {
        runningAverageCount++;
        runningAverageContextRelevance += score.ContextRelevance!.ScoreNumber;
        runningAverageAnswerGroundedness += score.AnswerGroundedness!.ScoreNumber;
        runningAverageAnswerCorrectness += score.AnswerCorrectness!.ScoreNumber;

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(JsonSerializer.Serialize(score, new JsonSerializerOptions { WriteIndented = true }));

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Average: Context relevance {(runningAverageContextRelevance / runningAverageCount):F2}, Groundedness {(runningAverageAnswerGroundedness / runningAverageCount):F2}, Correctness {(runningAverageAnswerCorrectness / runningAverageCount):F2} after {runningAverageCount} questions");
    }
}
});

class EvaluationResponse
{
    public ScoreResponse? ContextRelevance { get; set; }
    public ScoreResponse? AnswerGroundedness { get; set; }
    public ScoreResponse? AnswerCorrectness { get; set; }

    public bool Populated => ContextRelevance is not null && AnswerGroundedness is not null && AnswerCorrectness is not null;
}

record ScoreResponse(string? Justification, ScoreLabel ScoreLabel)
{
    public double ScoreNumber => ScoreLabel switch
    {
        ScoreLabel.Awful => 0,
        ScoreLabel.Poor => 0.3,
        ScoreLabel.Good => 0.7,
        ScoreLabel.Perfect => 1,
        _ => throw new InvalidOperationException("Invalid score label")
    };
}

enum ScoreLabel { Awful, Poor, Good, Perfect }