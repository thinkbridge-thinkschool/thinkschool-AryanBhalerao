-- DBSetup.sql
-- Run via: sqlcmd -S .\SQLEXPRESS -E -i DBSetup.sql

-- Create database if it does not exist
IF DB_ID('EFCoreDemo') IS NULL
    CREATE DATABASE EFCoreDemo;
GO

USE EFCoreDemo;
GO

-- Create table
IF OBJECT_ID('dbo.Products', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products (
        Id    INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
        Name  NVARCHAR(200) NOT NULL,
        Price DECIMAL(18,2) NOT NULL,
        Stock INT           NOT NULL
    );
END
GO

-- Seed 10 000 rows using a cross-join tally table
-- (avoids the default MAXRECURSION 100 limit of recursive CTEs)
IF NOT EXISTS (SELECT 1 FROM dbo.Products)
BEGIN
    WITH
        n2    AS (SELECT 1 AS x UNION ALL SELECT 1),
        n4    AS (SELECT 1 AS x FROM n2   CROSS JOIN n2   b),
        n16   AS (SELECT 1 AS x FROM n4   CROSS JOIN n4   b),
        n256  AS (SELECT 1 AS x FROM n16  CROSS JOIN n16  b),
        n65k  AS (SELECT 1 AS x FROM n256 CROSS JOIN n256 b),
        nums  AS (SELECT ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
                  FROM n65k)
    INSERT INTO dbo.Products (Name, Price, Stock)
    SELECT
        N'Product-' + CAST(n AS NVARCHAR(10)),
        CAST(1.0 + (n % 999) AS DECIMAL(18,2)),
        CAST(n % 500 AS INT)
    FROM nums
    WHERE n <= 10000;

    PRINT CAST(@@ROWCOUNT AS VARCHAR(10)) + ' rows inserted into Products.';
END
ELSE
    PRINT 'Products table already seeded, skipping.';
GO
