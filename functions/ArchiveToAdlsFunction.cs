using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Azure.Storage.Files.DataLake;
using Microsoft.Azure.Cosmos;
using Parquet;
using Parquet.Data;

public static class ArchiveToAdlsFunction
{
    [FunctionName("ArchiveToAdls")]
    public static async Task Run(
        [TimerTrigger("0 0 1 * * *")] TimerInfo myTimer,
        ILogger log)
    {
        string cosmosEndpoint = Environment.GetEnvironmentVariable("CosmosDbEndpoint");
        string cosmosKey = Environment.GetEnvironmentVariable("CosmosDbKey");
        string databaseId = "BillingDb";
        string containerId = "Records";

        CosmosClient cosmosClient = new(cosmosEndpoint, cosmosKey);
        var container = cosmosClient.GetContainer(databaseId, containerId);

        var query = new QueryDefinition("SELECT * FROM c WHERE c.recordDate < @cutoff")
            .WithParameter("@cutoff", DateTime.UtcNow.AddMonths(-3));

        using var iterator = container.GetItemQueryIterator<dynamic>(query);
        var serviceClient = new DataLakeServiceClient(
            new Uri(Environment.GetEnvironmentVariable("AdlsUri")),
            new Azure.Identity.DefaultAzureCredential());
        var fileSystemClient = serviceClient.GetFileSystemClient("billing-archive");

        while (iterator.HasMoreResults)
        {
            foreach (var item in await iterator.ReadNextAsync())
            {
                string year = DateTime.Parse(item.recordDate.ToString()).Year.ToString();
                string month = DateTime.Parse(item.recordDate.ToString()).Month.ToString("D2");
                string recordId = item.id;

                var stream = new MemoryStream();
                var schema = new SchemaElement[]
                {
                    new DataField<string>("id"),
                    new DataField<string>("content")
                };
                var table = new Parquet.Data.Rows.Table(schema);
                table.Add(new Row(recordId, item.ToString()));

                using (var writer = new ParquetWriter(new Schema(schema), stream))
                {
                    writer.CompressionMethod = CompressionMethod.Gzip;
                    using var rowGroupWriter = writer.CreateRowGroup();
                    foreach (var field in schema)
                    {
                        var data = table.GetColumn((DataField)field).Data;
                        rowGroupWriter.WriteColumn(new DataColumn((DataField)field, data));
                    }
                }
                stream.Position = 0;

                var dirClient = fileSystemClient.GetDirectoryClient($"{year}/{month}");
                var fileClient = dirClient.GetFileClient($"record_{recordId}.parquet");
                await fileClient.CreateAsync();
                await fileClient.AppendAsync(stream, offset: 0);
                await fileClient.FlushAsync(stream.Length);

                await container.DeleteItemAsync<dynamic>(recordId, new PartitionKey(item.partitionKey.ToString()));
                log.LogInformation($"Archived record {recordId} to ADLS.");
            }
        }
    }
}