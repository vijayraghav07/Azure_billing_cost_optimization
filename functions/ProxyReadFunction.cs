using Microsoft.Azure.WebJobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

public static class ProxyReadFunction
{
    [FunctionName("GetBillingRecord")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "billing/{id}")] HttpRequest req,
        string id,
        ILogger log)
    {
        var cosmosResult = await TryGetFromCosmosAsync(id);
        if (cosmosResult != null)
        {
            return new OkObjectResult(cosmosResult);
        }

        var parquetResult = await TryGetFromAdlsAsync(id);
        return parquetResult != null
            ? new OkObjectResult(parquetResult)
            : new NotFoundResult();
    }

    private static async Task<object> TryGetFromCosmosAsync(string id) => null;

    private static async Task<object> TryGetFromAdlsAsync(string id) => null;
}