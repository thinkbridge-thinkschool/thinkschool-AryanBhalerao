SELECT
    Author,
    Text,
    CreatedAt,

    -- Ordinal position of this quote within the author's timeline
    ROW_NUMBER() OVER (PARTITION BY Author ORDER BY CreatedAt)          AS QuoteNumber,

    -- Rank by recency across ALL quotes (ties share the same rank)
    RANK()       OVER (ORDER BY CreatedAt DESC)                         AS GlobalRecencyRank,

    -- Days since the author's previous quote (NULL for their first)
    DATEDIFF(
        day,
        LAG(CreatedAt) OVER (PARTITION BY Author ORDER BY CreatedAt),
        CreatedAt
    )                                                                    AS DaysSincePrevious,

    -- Next quote date for this author (NULL for their latest)
    LEAD(CreatedAt) OVER (PARTITION BY Author ORDER BY CreatedAt)       AS NextQuoteAt,

    -- Running total of quotes published so far, across all authors, ordered by time
    SUM(1) OVER (ORDER BY CreatedAt ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
                                                                         AS RunningQuoteTotal

FROM [Quotes]
ORDER BY Author, CreatedAt;