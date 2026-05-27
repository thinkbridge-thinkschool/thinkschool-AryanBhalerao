-- DEADLOCK REPRO — Session 2 (lock order: Orders → Widgets, reversed)
-- Run immediately after session1.sql is already executing.
USE DeadlockLab;
GO

BEGIN TRANSACTION;
    UPDATE dbo.Orders  SET Qty   = Qty   + 1 WHERE OrderId  = 101;
    WAITFOR DELAY '00:00:10';
    UPDATE dbo.Widgets SET Stock = Stock - 1 WHERE WidgetId = 1;
COMMIT TRANSACTION;
GO
-- Will return Msg 1205 on the victim session (deadlock chosen by SQL Server)

-- ─────────────────────────────────────────────────────────────────────────────
-- FIXED — same lock order as Session 1 (Widgets → Orders)
BEGIN TRANSACTION;
    UPDATE dbo.Widgets SET Stock = Stock - 1 WHERE WidgetId = 1;
    UPDATE dbo.Orders  SET Qty   = Qty   + 1 WHERE OrderId  = 101;
COMMIT TRANSACTION;
GO
-- Will return (1 row(s) affected) twice, transaction commits cleanly
