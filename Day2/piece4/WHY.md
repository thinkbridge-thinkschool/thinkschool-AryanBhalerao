# Why Rich Domain Model vs. Anemic Entity

## What the rich model bought us

### 1. Rules live exactly once, colocated with the data

The anemic `Quote` duplicated every constraint: `[MaxLength(100)]` on the entity, `[MaxLength(100)]` on the DTO, and manual `Validator.TryValidateObject` in the endpoint. Three places owned the same rule. The rich model puts invariants inside `Quote.Create()`—one place, always enforced, regardless of caller.

### 2. Compile-time immutability of Text

With public setters, `quote.Text = ""` was legal C# anywhere in the codebase. Private setters make mutation a compile error. The invariant "Text cannot change after creation" is now enforced by the type system, not by documentation or code review discipline.

### 3. Soft delete is a domain operation, not a scattered assignment

`quote.SoftDelete()` captures intent. Before, the delete semantics (remove row) were buried in the repository. Now the entity owns what "deletion" means; the repository just persists the state change.

### 4. `DomainResult<T>` makes failure explicit at the call site

The factory returns either a quote or an error. Callers cannot ignore validation failure—the type forces a branch. The old approach could be called with a bad DTO and would crash at the DB layer instead.

## The bug the anemic model would have shipped

Consider a scenario where the Author limit needs raising from 100 to 200 characters, the anemic model required the same change in three places: the `[MaxLength(100)]` annotation on `Quote`, the identical annotation on `CreateQuoteDto`, and any other DTOs or view models that happened to mirror the field. A developer updates the DB column and the entity annotation but misses the DTO. The API now silently rejects valid authors between 101-200 characters with a cryptic 400—a regression that unit tests won't catch because they validate the DTO and the entity in isolation. The rich model has exactly one place: the `author.Length > 200` guard inside `Quote.Create()`. The DTO carries no annotations and there is nothing to forget.
