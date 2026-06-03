using QuotesApi.Models;
using QuotesApi.Repositories;
using QuotesApi.Services;

namespace QuotesApi.Commands;

public class CreateQuoteCommandHandler
{
    private readonly IQuoteRepository _repo;
    private readonly IClock _clock;

    public CreateQuoteCommandHandler(IQuoteRepository repo, IClock clock)
    {
        _repo = repo;
        _clock = clock;
    }

    public async Task<(int? QuoteId, Dictionary<string, string[]>? Errors)> HandleAsync(
        CreateQuoteCommand command, CancellationToken ct)
    {
        var errors = Validate(command);
        if (errors.Count > 0)
            return (null, errors);

        var quote = new Quote
        {
            Author = command.Author,
            Text = command.Text,
            OwnerId = command.OwnerId
        };

        var created = await _repo.CreateAsync(quote, ct);
        return (created.Id, null);
    }

    private static Dictionary<string, string[]> Validate(CreateQuoteCommand cmd)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(cmd.Author))
            errors["author"] = ["Author is required"];
        else if (cmd.Author.Length > Quote.AuthorMaxLength)
            errors["author"] = [$"Author must be {Quote.AuthorMaxLength} characters or fewer"];

        if (string.IsNullOrWhiteSpace(cmd.Text))
            errors["text"] = ["Text is required"];
        else if (cmd.Text.Length > Quote.TextMaxLength)
            errors["text"] = [$"Text must be {Quote.TextMaxLength} characters or fewer"];

        return errors;
    }
}
