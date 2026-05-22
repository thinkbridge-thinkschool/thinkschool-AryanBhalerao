
# CIRun.md

[![Piece 7 Integration Tests](https://github.com/thinkbridge-thinkschool/thinkschool-AryanBhalerao/actions/workflows/day3piece7ci.yml/badge.svg)](https://github.com/thinkbridge-thinkschool/thinkschool-AryanBhalerao/actions/workflows/day3piece7ci.yml)

Tests - Status:
1. GetQuotes_WithSeededData_ReturnsAll - Passed
2. GetQuotes_EmptyDatabase_ReturnsEmptyList - Passed
3. GetQuotes_Pagination_RespectsPageSize - Passed
4. GetQuoteById_ExistingId_ReturnsQuote - Passed
5. GetQuoteById_NonExistentId_Returns404 - Passed 
6. GetQuoteById_SoftDeletedQuote_Returns404 - Passed
7. PostQuote_ValidTokenWithWriteScope_Returns201 - Passed
8. PostQuote_MissingScope_Returns403 - Passed
9. PostQuote_Unauthenticated_Returns401 - Passed
10. DeleteQuote_ByOwner_Returns204 - Passed
11. DeleteQuote_ByNonOwner_Returns403 - Passed