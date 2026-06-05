// Matches QuoteReadModel record: GET /api/quotes?page=&size=
// and GET /api/quotes/{id}
export interface QuoteReadModel {
  id: number;
  authorName: string;   // mapped from Author column in Dapper query
  text: string;
  createdAt: string;    // DateTimeOffset serialised as ISO-8601 string
}

// Matches QuoteMetadataReadModel record: GET /api/quotes/with-metadata?page=&size=
export interface QuoteMetadataReadModel {
  quoteId: number;
  quote: string;
  author: string;
  user: string;
  createdAt: string;    // DateTimeOffset serialised as ISO-8601 string
  tags: string[];
  categories: string[];
}

// Matches AuthorWithQuotesDto record: GET /api/authors/with-quotes
// Used to derive collection-wide totals (total quotes = Σ quoteCount,
// total authors = list length) without a dedicated count endpoint.
export interface AuthorWithQuotes {
  name: string;
  quoteCount: number;
  quotes: { id: number; text: string; createdAt: string }[];
}
