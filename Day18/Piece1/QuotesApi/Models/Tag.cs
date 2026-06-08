using System.ComponentModel.DataAnnotations;

namespace QuotesApi.Models;

public class Tag
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public ICollection<Quote> Quotes { get; set; } = [];
}
