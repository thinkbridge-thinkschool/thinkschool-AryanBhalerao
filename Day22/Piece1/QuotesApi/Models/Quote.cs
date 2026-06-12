using System.ComponentModel.DataAnnotations;

namespace QuotesApi.Models;

public class Quote
{
    public const int AuthorMaxLength = 100;
    public const int TextMaxLength = 1000;

    public int Id { get; set; }

    [Required]
    [MaxLength(AuthorMaxLength)]
    public string Author { get; set; } = string.Empty;

    [Required]
    [MaxLength(TextMaxLength)]
    public string Text { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public int? OwnerId { get; set; }
    public User? Owner { get; set; }

    public ICollection<Tag> Tags { get; set; } = [];
    public ICollection<Category> Categories { get; set; } = [];
}
