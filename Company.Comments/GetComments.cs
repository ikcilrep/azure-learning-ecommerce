using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Company.PostComment;

public static class GetComments
{
    [FunctionName("GetComments")]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "comments/{id}")]
        HttpRequest req,
        [CosmosDB(
            databaseName: "Ecommerce",
            containerName: "CommentsToProduct",
            Connection = "CosmosDbConnection",
            Id = "{id}",
            PartitionKey = "{id}")]
        ProductComments productComments,
        [CosmosDB(
            databaseName: "Ecommerce",
            containerName: "Comments",
            Connection = "CosmosDbConnection")]
        CosmosClient client,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request");
        var commentsContainer = client.GetDatabase("Ecommerce").GetContainer("Comments");
        var idsWithPartitionKey = productComments.CommentIds
            .Select(id => id.ToString())
            .Select(id => (id, new PartitionKey(id)))
            .ToImmutableList();
        var commentsResponse = await commentsContainer.ReadManyItemsAsync<Comment>(idsWithPartitionKey);
        return new OkObjectResult(commentsResponse.Resource);
    }
}