-- ============================================================
-- SESSION B — Run these scripts in your second SSMS window
-- Execute each step while Session A is inside its WAITFOR DELAY.
-- ============================================================


-- ============================================================
-- 1. DIRTY READ — Session B (reader)
-- ============================================================

-- ---------- REPRODUCE (READ UNCOMMITTED) ----------
-- Step B-1: run while Session A is in the WAITFOR
USE IsolationDemo;
GO

SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

SELECT AccountID, Name, Balance
FROM   Accounts
WHERE  AccountID = 1;
-- Returns 9999.00 — dirty data that Session A will roll back!
GO

-- ---------- PREVENT (READ COMMITTED) ----------
-- Step B-1: run while Session A is in the WAITFOR
USE IsolationDemo;
GO

SET TRANSACTION ISOLATION LEVEL READ COMMITTED;   -- SQL Server default

SELECT AccountID, Name, Balance
FROM   Accounts
WHERE  AccountID = 1;
-- Waits for Session A to finish, then returns 1000.00
GO


-- ============================================================
-- 2. NON-REPEATABLE READ — Session B (updater)
-- ============================================================

-- ---------- REPRODUCE (READ COMMITTED) ----------
-- Step B-1: run while Session A is in the WAITFOR
USE IsolationDemo;
GO

UPDATE Accounts SET Balance = 2500.00 WHERE AccountID = 2;
-- auto-commits — slips in between Session A's two reads
GO

-- ---------- PREVENT (REPEATABLE READ) ----------
-- Step B-1: run while Session A is in the WAITFOR
USE IsolationDemo;
GO

-- This UPDATE blocks until Session A's REPEATABLE READ tran ends
UPDATE Accounts SET Balance = 2500.00 WHERE AccountID = 2;
GO


-- ============================================================
-- 3. PHANTOM READ — Session B (inserter)
-- ============================================================

-- ---------- REPRODUCE (REPEATABLE READ) ----------
-- Step B-1: run while Session A is in the WAITFOR
USE IsolationDemo;
GO

INSERT INTO Accounts (AccountID, Name, Balance) VALUES (4, 'Dave', 750.00);
-- auto-commits — Session A's lock did NOT cover the new key range
GO

-- ---------- PREVENT (SERIALIZABLE) ----------
-- Step B-1: run while Session A is in the WAITFOR
USE IsolationDemo;
GO

-- This INSERT blocks until Session A's SERIALIZABLE tran ends
INSERT INTO Accounts (AccountID, Name, Balance) VALUES (4, 'Dave', 750.00);
GO
