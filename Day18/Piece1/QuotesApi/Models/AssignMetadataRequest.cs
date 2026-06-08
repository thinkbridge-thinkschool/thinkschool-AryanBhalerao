namespace QuotesApi.Models;

public class AssignMetadataRequest
{
    public List<string> Tags { get; set; } = [];
    public List<string> Categories { get; set; } = [];
}
