using Embeddings;

public class FaissSemanticSearch
{
    public Task RunAsync()
    {
        // There are two versions of this exercise: one for Windows (with FaissNet) and one for Mac/Linux (with FaissMask).
        // See the instructions for the reasons behind this.
#if USE_FAISS_NET
        return new FaissSemanticSearch_Windows().RunAsync();
#else
        return new FaissSemanticSearch_MacLinux().RunAsync();
#endif
    }
}
