# CIRun.md

[![Piece 5 Unit Tests](https://github.com/thinkbridge-thinkschool/thinkschool-AryanBhalerao/actions/workflows/day3piece5ci.yml/badge.svg)](https://github.com/thinkbridge-thinkschool/thinkschool-AryanBhalerao/actions/workflows/day3piece5ci.yml)

## Test List

### CollectionTests (14 tests)
1. Create_ValidName_ReturnsCollectionWithNameAndOwnerId
2. Create_NullOrWhitespaceName_ThrowsArgumentException(name: null)
3. Create_NullOrWhitespaceName_ThrowsArgumentException(name: "")
4. Create_NullOrWhitespaceName_ThrowsArgumentException(name: "  ")
5. Create_NameTooShort_ThrowsArgumentException(name: "a")
6. Create_NameTooShort_ThrowsArgumentException(name: "ab")
7. Create_Name81Chars_ThrowsArgumentException
8. Create_NameExactly3Chars_ReturnsCollection
9. Create_NameExactly80Chars_ReturnsCollection
10. AddItem_NewQuoteId_AddsToItems
11. AddItem_DuplicateQuoteId_ThrowsInvalidOperationException
12. AddItem_WhenAt50Items_ThrowsInvalidOperationException
13. RemoveItem_ExistingQuoteId_RemovesFromItems
14. RemoveItem_NonExistentQuoteId_ThrowsInvalidOperationException

### DomainResultTests (4 tests)
15. Ok_WithValue_IsSuccessIsTrue
16. Ok_WithValue_ErrorIsNull
17. Fail_WithMessage_IsSuccessIsFalse
18. Fail_WithMessage_ValueIsNull

### QuoteOwnerHandlerTests (3 tests)
19. HandleRequirement_UserOwnsQuote_ContextSucceeds
20. HandleRequirement_UserDoesNotOwnQuote_ContextDoesNotSucceed
21. HandleRequirement_MissingNameIdentifierClaim_ContextDoesNotSucceed

### QuoteTests (14 tests)
22. Create_ValidAuthorAndText_ReturnsSuccessWithValues
23. Create_AuthorNullOrWhiteSpace_ReturnsFailure(author: null)
24. Create_AuthorNullOrWhiteSpace_ReturnsFailure(author: "")
25. Create_AuthorNullOrWhiteSpace_ReturnsFailure(author: "   ")
26. Create_Author201Chars_ReturnsFailure
27. Create_Author200Chars_ReturnsSuccess
28. Create_TextNullOrWhiteSpace_ReturnsFailure(text: null)
29. Create_TextNullOrWhiteSpace_ReturnsFailure(text: "")
30. Create_TextNullOrWhiteSpace_ReturnsFailure(text: "   ")
31. Create_Text1001Chars_ReturnsFailure
32. Create_Text1000Chars_ReturnsSuccess
33. Create_WithOwnerId_SetsOwnerIdOnQuote
34. Create_WithoutOwnerId_DefaultsToEmptyString
35. SoftDelete_OnActiveQuote_SetsIsDeletedToTrue

### RefreshTokenTests (9 tests)
36. Create_ValidInputs_SetsAllProperties
37. IsRevoked_NewToken_ReturnsFalse
38. Revoke_WhenCalled_SetsIsRevokedTrue
39. Revoke_WithSuccessorHash_SetsReplacedByToken
40. Revoke_WithoutSuccessorHash_ReplacedByTokenIsNull
41. IsActive_NotRevokedAndFutureExpiry_ReturnsTrue
42. IsActive_ExpiredToken_ReturnsFalse
43. IsActive_AfterRevoke_ReturnsFalse
44. Revoke_WithReplacedByToken_ExposesReuseDetectionSignal

