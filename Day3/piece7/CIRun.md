# CIRun.md

[![Piece 5 Unit Tests](https://github.com/thinkbridge-thinkschool/thinkschool-AryanBhalerao/actions/workflows/day3piece7ci.yml/badge.svg)](https://github.com/thinkbridge-thinkschool/thinkschool-AryanBhalerao/actions/workflows/day3piece7ci.yml)

## Test List

### QuotesEndpointTests (`QuotesApi.IntegrationTests`)

1. GetQuotes_WithSeededData_ReturnsAll
2. GetQuotes_EmptyDatabase_ReturnsEmptyList
3. GetQuotes_Pagination_RespectsPageSize
4. GetQuoteById_ExistingId_ReturnsQuote
5. GetQuoteById_NonExistentId_Returns404
6. GetQuoteById_SoftDeletedQuote_Returns404
7. PostQuote_ValidTokenWithWriteScope_Returns201
8. PostQuote_MissingScope_Returns403
9. PostQuote_Unauthenticated_Returns401
10. DeleteQuote_ByOwner_Returns204
11. DeleteQuote_ByNonOwner_Returns403
