
WITH AuthorStats AS (
    SELECT
        Author,
        COUNT(*)     AS QuoteCount,
        MAX(CreatedAt) AS LatestAt
    FROM [Quotes]
    GROUP BY Author
),
RankedQuotes AS (
    SELECT
        Author,
        Text,
        CreatedAt,
        ROW_NUMBER() OVER (PARTITION BY Author ORDER BY CreatedAt DESC) AS rn
    FROM [Quotes]
)
SELECT
    s.Author,
    s.QuoteCount,

    r.Text      AS MostRecentQuote,
    s.LatestAt  AS MostRecentAt
FROM AuthorStats s
JOIN RankedQuotes r ON r.Author = s.Author AND r.rn = 1
ORDER BY s.QuoteCount DESC;