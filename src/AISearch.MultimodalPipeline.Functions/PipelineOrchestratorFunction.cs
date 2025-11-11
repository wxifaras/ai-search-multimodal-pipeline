using System.IO;
using System.Threading.Tasks;
using AISearch.MultimodalPipeline.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AISearch.MultimodalPipeline.Functions;

public class PipelineOrchestratorFunction
{
    private readonly ILogger<PipelineOrchestratorFunction> _logger;
    private readonly ISearchPipelineOrchestrator _orchestrator;

    public PipelineOrchestratorFunction(
        ILogger<PipelineOrchestratorFunction> logger, 
        ISearchPipelineOrchestrator orchestrator)
    {
        _logger = logger;
        _orchestrator = orchestrator;
    }

    [Function(nameof(PipelineOrchestratorFunction))]
    public async Task Run([BlobTrigger("samples/{name}", Connection = "AzureStorage")] Stream stream, string name)
    {
        try
        {
            _logger.LogInformation("Blob trigger function processed blob\n Name:{name} \n Size: {length} Bytes", name, stream.Length);
            await _orchestrator.SetupPipelineAsync();
            _logger.LogInformation("Search pipeline setup completed successfully.");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "An error occurred while setting up the search pipeline.");
            throw;
        }
    }
}