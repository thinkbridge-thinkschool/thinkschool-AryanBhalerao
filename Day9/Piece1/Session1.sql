-- ============================================================
-- SESSION A — Run these scripts in your first SSMS window
-- Each block uses WAITFOR DELAY to give you time to switch
-- to Session B and execute the matching step there.
-- ============================================================


-- ============================================================
-- 1. DIRTY READ — Session A (writer)
-- ============================================================

-- ---------- REPRODUCE (READ UNCOMMITTED) ----------
USE IsolationDemo;
GO

BEGIN TRAN;
    -- Step A-1: update balance but do NOT commit yet
    UPDATE Accounts SET Balance = 9999.00 WHERE AccountID = 1;

    -- keep transaction open so Session B can read the dirty value
    WAITFOR DELAY '00:00:15';

    -- Step A-3: roll back — 9999.00 never really happened
ROLLBACK;
GO

-- ---------- PREVENT (READ COMMITTED) ----------
USE IsolationDemo;
GO

SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
BEGIN TRAN;
    -- Step A-1: update balance but do NOT commit yet
    UPDATE Accounts SET Balance = 9999.00 WHERE AccountID = 1;

    -- Session B (reader) is now BLOCKED — it cannot read the dirty value
    WAITFOR DELAY '00:00:15';

    -- Step A-3: roll back — 9999.00 never happened
ROLLBACK;
GO


-- ============================================================
-- 2. NON-REPEATABLE READ — Session A (double reader)
-- ============================================================

-- ---------- REPRODUCE (READ COMMITTED) ----------
USE IsolationDemo;
GO

SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
BEGIN TRAN;
    -- Step A-1: first read
    SELECT AccountID, Name, Balance
    FROM   Accounts
    WHERE  AccountID = 2;
    -- Returns 2000.00

    -- gap — switch to Session B and run its step now
    WAITFOR DELAY '00:00:15';

    -- Step A-3: second read — value has changed!
    SELECT AccountID, Name, Balance
    FROM   Accounts
    WHERE  AccountID = 2;
    -- Returns 2500.00 — non-repeatable!
COMMIT;
GO

-- ---------- PREVENT (REPEATABLE READ) ----------
USE IsolationDemo;
GO

SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;
BEGIN TRAN;
    -- Step A-1: first read — acquires and holds a shared lock
    SELECT AccountID, Name, Balance
    FROM   Accounts
    WHERE  AccountID = 2;
    -- Returns 2000.00

    WAITFOR DELAY '00:00:15';

    -- Step A-3: second read — Session B was blocked, value is still 2000.00
    SELECT AccountID, Name, Balance
    FROM   Accounts
    WHERE  AccountID = 2;
COMMIT;
GO


-- ============================================================
-- 3. PHANTOM READ — Session A (range reader)
-- ============================================================

-- ---------- REPRODUCE (REPEATABLE READ) ----------
USE IsolationDemo;
GO

SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;
BEGIN TRAN;
    -- Step A-1: first range read
    SELECT AccountID, Name, Balance
    FROM   Accounts
    WHERE  Balance > 500.00;
    -- Returns 2 rows: Alice (1000) and Bob (2000)

    -- gap — switch to Session B and run its step now
    WAITFOR DELAY '00:00:15';

    -- Step A-3: second range read — phantom row appears!
    SELECT AccountID, Name, Balance
    FROM   Accounts
    WHERE  Balance > 500.00;
    -- Returns 3 rows: Alice, Bob, and Dave (phantom!)
COMMIT;
GO

-- ---------- PREVENT (SERIALIZABLE) ----------
USE IsolationDemo;
GO

SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN TRAN;
    -- Step A-1: first range read — acquires a key-range lock on Balance > 500
    SELECT AccountID, Name, Balance
    FROM   Accounts
    WHERE  Balance > 500.00;
    -- Returns 2 rows

    WAITFOR DELAY '00:00:15';

    -- Step A-3: second range read — Session B was blocked, still 2 rows
    SELECT AccountID, Name, Balance
    FROM   Accounts
    WHERE  Balance > 500.00;
COMMIT;
GO
