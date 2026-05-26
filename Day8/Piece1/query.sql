USE IndexDemo;
GO

-- SECTION 1: Before Indexes
SET STATISTICS IO ON;

-- Q1: single-row point lookup by OrderID
SELECT OrderID, CustomerID, TotalAmount
FROM   dbo.Orders
WHERE  OrderID = 42000;

-- Q2: customer order history (all orders for one customer)
SELECT OrderID, OrderDate, TotalAmount
FROM   dbo.Orders
WHERE  CustomerID = 7500;

-- Q3: 3-month date range
SELECT OrderID, CustomerID, TotalAmount
FROM   dbo.Orders
WHERE  OrderDate >= '2025-01-01' AND OrderDate < '2025-04-01';

-- Q4: INSERT on heap
INSERT INTO dbo.Orders
    (OrderID, CustomerID, OrderDate, TotalAmount, Status, ProductID, Region)
VALUES (100001, 5001, '2026-05-26', 149.99, 'Pending', 42, 'West');

SET STATISTICS IO OFF;
GO

-- Output
-- (1 row affected)
-- Table 'Orders'. Scan count 1, logical reads 716, physical reads 0, page server reads 0, read-ahead reads 0, page server read-ahead reads 0, lob logical reads 0, lob physical reads 0, lob page server reads 0, lob read-ahead reads 0, lob page server read-ahead reads 0.

-- (12 rows affected)
-- Table 'Orders'. Scan count 1, logical reads 716, physical reads 0, page server reads 0, read-ahead reads 0, page server read-ahead reads 0, lob logical reads 0, lob physical reads 0, lob page server reads 0, lob read-ahead reads 0, lob page server read-ahead reads 0.

-- (4895 rows affected)
-- Table 'Orders'. Scan count 1, logical reads 716, physical reads 0, page server reads 0, read-ahead reads 0, page server read-ahead reads 0, lob logical reads 0, lob physical reads 0, lob page server reads 0, lob read-ahead reads 0, lob page server read-ahead reads 0.
-- Table 'Orders'. Scan count 0, logical reads 1, physical reads 0, page server reads 0, read-ahead reads 0, page server read-ahead reads 0, lob logical reads 0, lob physical reads 0, lob page server reads 0, lob read-ahead reads 0, lob page server read-ahead reads 0.

-- (1 row affected)

-- Completion time: 2026-05-26T12:42:45.3651634+05:30

-- SECTION 2: Adding Indexes

-- 1. CLUSTERED index: physically reorders the table pages by OrderID.
--    There can only be one per table; NCI row locators will point here.
CREATE CLUSTERED INDEX CIX_Orders_OrderID
    ON dbo.Orders (OrderID);
GO

-- 2. NON-CLUSTERED on CustomerID.
--    INCLUDE pulls OrderDate + TotalAmount into the leaf so Q2 is
--    fully covered (no key lookup back to the clustered row needed).
CREATE NONCLUSTERED INDEX NIX_Orders_CustomerID
    ON dbo.Orders (CustomerID)
    INCLUDE (OrderDate, TotalAmount);
GO

-- 3. NON-CLUSTERED on OrderDate.
--    INCLUDE pulls CustomerID + TotalAmount into the leaf so Q3 is
--    fully covered; OrderID arrives free as the clustered key.
CREATE NONCLUSTERED INDEX NIX_Orders_OrderDate
    ON dbo.Orders (OrderDate)
    INCLUDE (CustomerID, TotalAmount);
GO

-- Output:
-- Commands completed successfully.

-- Completion time: 2026-05-26T12:44:03.5190295+05:30


-- SECTION 3: After Indexes
SET STATISTICS IO ON;

-- Q1: single-row point lookup by OrderID
-- Made faster using Index CIX_Orders_OrderID
SELECT OrderID, CustomerID, TotalAmount
FROM   dbo.Orders
WHERE  OrderID = 42000;

-- Q2: customer order history (all orders for one customer)
-- Made faster using Index NIX_Orders_CustomerID
SELECT OrderID, OrderDate, TotalAmount
FROM   dbo.Orders
WHERE  CustomerID = 7500;

-- Q3: 3-month date range
-- Made faster using Index NIX_Orders_OrderDate
SELECT OrderID, CustomerID, TotalAmount
FROM   dbo.Orders
WHERE  OrderDate >= '2025-01-01' AND OrderDate < '2025-04-01';

-- Q4: INSERT on heap
-- Made faster using Index CIX_Orders_OrderID, NIX_Orders_CustomerID and NIX_Orders_OrderDate.
INSERT INTO dbo.Orders
    (OrderID, CustomerID, OrderDate, TotalAmount, Status, ProductID, Region)
VALUES (100002, 5001, '2026-05-26', 149.99, 'Pending', 42, 'West');

SET STATISTICS IO OFF;
GO

-- Output:
-- (1 row affected)
-- Table 'Orders'. Scan count 1, logical reads 3, physical reads 0, page server reads 0, read-ahead reads 0, page server read-ahead reads 0, lob logical reads 0, lob physical reads 0, lob page server reads 0, lob read-ahead reads 0, lob page server read-ahead reads 0.

-- (12 rows affected)
-- Table 'Orders'. Scan count 1, logical reads 2, physical reads 0, page server reads 0, read-ahead reads 0, page server read-ahead reads 0, lob logical reads 0, lob physical reads 0, lob page server reads 0, lob read-ahead reads 0, lob page server read-ahead reads 0.

-- (4895 rows affected)
-- Table 'Orders'. Scan count 1, logical reads 21, physical reads 0, page server reads 0, read-ahead reads 0, page server read-ahead reads 0, lob logical reads 0, lob physical reads 0, lob page server reads 0, lob read-ahead reads 0, lob page server read-ahead reads 0.
-- Table 'Orders'. Scan count 0, logical reads 13, physical reads 0, page server reads 0, read-ahead reads 0, page server read-ahead reads 0, lob logical reads 0, lob physical reads 0, lob page server reads 0, lob read-ahead reads 0, lob page server read-ahead reads 0.

-- (1 row affected)

-- Completion time: 2026-05-26T12:44:48.0907321+05:30