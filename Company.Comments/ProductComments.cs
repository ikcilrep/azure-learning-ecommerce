using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Company.PostComment;

public class ProductComments
{
    [JsonProperty("id")]
    public string ProductId { get; set; }
    
    public List<Guid> CommentIds  { get; set; } 
}