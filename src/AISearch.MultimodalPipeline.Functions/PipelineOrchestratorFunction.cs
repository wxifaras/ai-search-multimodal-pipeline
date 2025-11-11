using AISearch.MultimodalPipeline.Functions.Services;
using Azure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Services;
using System.IO;
using System.Threading.Tasks;

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

            // Check if this is first run by checking if indexer exists
            bool isFirstRun = await _orchestrator.IsFirstRunAsync();

            if (isFirstRun)
            {
                _logger.LogInformation("First run detected. Setting up complete pipeline infrastructure...");
                await _orchestrator.SetupPipelineAsync();
                _logger.LogInformation("Search pipeline setup completed successfully.");
            }
            else
            {
                _logger.LogInformation("Infrastructure exists. Running indexer for new file: {name}", name);
                await _orchestrator.RunIndexerAsync();
                _logger.LogInformation("Indexer run completed for new file.");
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "An error occurred while setting up the search pipeline.");
            throw;
        }
    }
}