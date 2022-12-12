using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Company.PostComment;

public static class PostComment
{
    [FunctionName("PostComment")]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comments")]
        HttpRequest req, ILogger log,
        [CosmosDB(databaseName: "Ecommerce", containerName: "Comments",
            Connection =
                "CosmosDbConnection")]
        IAsyncCollector<Comment> commentsOut,
        [CosmosDB(databaseName: "Ecommerce", containerName: "CommentsToProduct",
            Connection =
                "CosmosDbConnection")]
        CosmosClient client)
    {
        log.LogInformation("C# HTTP trigger function processed a request");

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var comment = JsonConvert.DeserializeObject<Comment>(requestBody);
        log.LogInformation("Comment deserialized");
        if (comment != null)
        {
            comment.Id = Guid.NewGuid();
            comment.CreatedAt = DateTime.UtcNow;
        }
        else
            return new BadRequestResult();

        await commentsOut.AddAsync(comment);
        log.LogInformation("Comment added");
        await AddCommentToProduct(comment, client, log);

        return new OkObjectResult(commentsOut);
    }

    private static async Task AddCommentToProduct(Comment comment, CosmosClient client, ILogger log)
    {
        var commentsToProductContainer = client.GetDatabase("Ecommerce").GetContainer("CommentsToProduct");
        var productId = comment.ProductId;
        try
        {
            var partitionKey = new PartitionKey(productId);
            var productCommentsResponse = await commentsToProductContainer.ReadItemAsync<ProductComments>(productId,
                partitionKey);

            log.LogInformation("Read product comments successfully");
            await AddCommentToExistingProductToComments(comment, productCommentsResponse,
                commentsToProductContainer,
                productId, partitionKey);
        }
        catch (Exception e)
        {
            log.LogError("{ErrorMessage}", e.Message);
            await CreateProductToCommentsWithTheComment(comment, commentsToProductContainer);
        }
    }

    private static async Task AddCommentToExistingProductToComments(Comment comment,
        Response<ProductComments> productCommentsResponse,
        Container commentsToProductContainer, string productId, PartitionKey partitionKey)
    {
        var productComments = productCommentsResponse.Resource;
        productComments.CommentIds.Add(comment.Id);

        await commentsToProductContainer.PatchItemAsync<ProductComments>(productId, partitionKey,
            new[] { PatchOperation.Replace("/CommentIds", productComments.CommentIds) });
    }

    private static async Task CreateProductToCommentsWithTheComment(Comment comment,
        Container commentsToProductContainer)
    {
        var productComments = new ProductComments
        {
            ProductId = comment.ProductId,
            CommentIds = new List<Guid> { comment.Id }
        };
        var partitionKey = new PartitionKey(productComments.ProductId);
        await commentsToProductContainer.CreateItemAsync(productComments, partitionKey);
    }
}