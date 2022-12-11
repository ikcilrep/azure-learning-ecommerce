using System;
using Newtonsoft.Json;

namespace Company.PostComment;

public class Comment
{
    [JsonProperty("id")]
    public Guid Id { get; set; }

    public string Text { get; set; }

    public string AuthorEmail { get; set; }

    public string ProductId { get; set; }

    public DateTime CreatedAt { get; set; }
}