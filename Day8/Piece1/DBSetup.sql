CREATE DATABASE IndexDemo;
GO

USE IndexDemo;

-- Heap table: no indexes
CREATE TABLE dbo.Orders (
    OrderID     INT           NOT NULL,
    CustomerID  INT           NOT NULL,
    OrderDate   DATETIME      NOT NULL,
    TotalAmount DECIMAL(10,2) NOT NULL,
    Status      VARCHAR(20)   NOT NULL,
    ProductID   INT           NOT NULL,
    Region      VARCHAR(50)   NOT NULL
);
GO

-- Number generator: exceeds 100,000 combinations
WITH Nums AS (
    SELECT TOP (100000)
           ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM   sys.all_columns a
    CROSS JOIN sys.all_columns b
)
INSERT INTO dbo.Orders
    (OrderID, CustomerID, OrderDate, TotalAmount, Status, ProductID, Region)
SELECT
    CAST(n AS INT),
    ABS(CHECKSUM(NEWID()) % 10000) + 1,
    DATEADD(DAY, -(ABS(CHECKSUM(NEWID()) % 1825)), '2026-01-01'),
    CAST(ABS(CHECKSUM(NEWID()) % 9900) + 100 AS DECIMAL(10,2)),
    CASE ABS(CHECKSUM(NEWID()) % 4)
        WHEN 0 THEN 'Pending'
        WHEN 1 THEN 'Shipped'
        WHEN 2 THEN 'Delivered'
        ELSE        'Cancelled'
    END,
    ABS(CHECKSUM(NEWID()) % 500) + 1,
    CASE ABS(CHECKSUM(NEWID()) % 5)
        WHEN 0 THEN 'North'
        WHEN 1 THEN 'South'
        WHEN 2 THEN 'East'
        WHEN 3 THEN 'West'
        ELSE        'Central'
    END
FROM Nums;
GO

SELECT COUNT(*) AS total_rows FROM dbo.Orders;
GO

-- Output:
-- 	total_rows
-- 1	100000