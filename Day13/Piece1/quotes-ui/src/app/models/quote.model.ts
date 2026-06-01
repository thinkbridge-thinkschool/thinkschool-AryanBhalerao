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
  user: string;
  tags: string[];
  categories: string[];
}
