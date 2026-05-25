-- Q1: Authors who have quotes but NO tags on any of them
-- Operator: EXCEPT
SELECT DISTINCT Author AS "Authors with no quotes with Tags"
FROM   Quotes
EXCEPT
SELECT DISTINCT q.Author
FROM   Quotes      q
JOIN   QuoteTags  qt ON q.Id = qt.QuoteId;

-- Q2: Authors who appear in BOTH the 'classic' and 'modern' sets
-- Operator: INTERSECT
SELECT DISTINCT q.Author AS "Authors with both Classic and Modern quotes."
FROM   Quotes           q
JOIN   QuoteCategories qc ON q.Id          = qc.QuoteId
JOIN   Categories       c ON qc.CategoryId = c.Id
WHERE  c.Name = 'classic'
INTERSECT
SELECT DISTINCT q.Author
FROM   Quotes           q
JOIN   QuoteCategories qc ON q.Id          = qc.QuoteId
JOIN   Categories       c ON qc.CategoryId = c.Id
WHERE  c.Name = 'modern';

-- Q3: Combined distinct tag list across the 'classic' and 'modern' categories
-- Operator: UNION
SELECT DISTINCT t.Name AS "Distinct Tags"
FROM   Tags            t
JOIN   QuoteTags      qt ON t.Id        = qt.TagId
JOIN   QuoteCategories qc ON qt.QuoteId = qc.QuoteId
JOIN   Categories       c ON qc.CategoryId = c.Id
WHERE  c.Name = 'classic'
UNION
SELECT DISTINCT t.Name
FROM   Tags            t
JOIN   QuoteTags      qt ON t.Id        = qt.TagId
JOIN   QuoteCategories qc ON qt.QuoteId = qc.QuoteId
JOIN   Categories       c ON qc.CategoryId = c.Id
WHERE  c.Name = 'modern';
