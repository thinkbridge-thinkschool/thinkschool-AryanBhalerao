-- DB Setup: Day 10 Piece 2
-- Instance : .\SQLEXPRESS
-- Database : QueryProjectionsDemo
-- Run with : sqlcmd -S .\SQLEXPRESS -E -i DBSetup.sql

-- 1. Create database
IF DB_ID('QueryProjectionsDemo') IS NULL
    CREATE DATABASE QueryProjectionsDemo;
GO

USE QueryProjectionsDemo;
GO

-- 2. Create table (7 columns — Description/ImageUrl make the full-entity row heavy)
IF OBJECT_ID('Products', 'U') IS NULL
BEGIN
    CREATE TABLE Products (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        Name        NVARCHAR(200)  NOT NULL,
        Price       DECIMAL(18,2)  NOT NULL,
        Stock       INT            NOT NULL,
        Description NVARCHAR(MAX)  NOT NULL,
        CreatedAt   DATETIME2      NOT NULL,
        ImageUrl    NVARCHAR(500)  NOT NULL
    );
END
GO

-- 3. Seed 10,000 rows (skips if already populated)
IF NOT EXISTS (SELECT 1 FROM Products)
BEGIN
    DECLARE @i INT = 1;
    WHILE @i <= 10000
    BEGIN
        INSERT INTO Products (Name, Price, Stock, Description, CreatedAt, ImageUrl)
        VALUES (
            'Product-' + CAST(@i AS NVARCHAR),
            ROUND(1.0 + (@i % 999), 2),
            @i % 500,
            'Full description for product ' + CAST(@i AS NVARCHAR) + '. Contains marketing copy, specs, and legal text.',
            DATEADD(day, -(@i % 365), GETUTCDATE()),
            'https://cdn.example.com/products/' + CAST(@i AS NVARCHAR) + '.jpg'
        );
        SET @i = @i + 1;
    END
END
GO

-- 4. Verify
SELECT COUNT(*) AS TotalRows FROM Products;  -- expected: 10000
GO
