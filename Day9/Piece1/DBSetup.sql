-- ============================================================
-- DB Setup — Isolation Level Demo
-- ============================================================

-- Run once before any anomaly demo
CREATE DATABASE IsolationDemo;
GO

USE IsolationDemo;
GO

CREATE TABLE Accounts (
    AccountID INT            PRIMARY KEY,
    Name      NVARCHAR(100)  NOT NULL,
    Balance   DECIMAL(10, 2) NOT NULL
);
GO

INSERT INTO Accounts (AccountID, Name, Balance) VALUES
    (1, 'Alice', 1000.00),
    (2, 'Bob',   2000.00),
    (3, 'Carol',  500.00);
GO

-- ============================================================
-- Reset Between Demos
-- Run this to restore the table to its original state
-- ============================================================

USE IsolationDemo;
GO

DELETE FROM Accounts WHERE AccountID NOT IN (1, 2, 3);

UPDATE Accounts SET Balance = 1000.00 WHERE AccountID = 1;
UPDATE Accounts SET Balance = 2000.00 WHERE AccountID = 2;
UPDATE Accounts SET Balance =  500.00 WHERE AccountID = 3;
GO
