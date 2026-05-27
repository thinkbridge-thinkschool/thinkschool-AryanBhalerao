-- DEADLOCK REPRO — Session 1 (lock order: Widgets → Orders)
-- Run first, then immediately run session2.sql in a second tab.
USE DeadlockLab;
GO

BEGIN TRANSACTION;
    UPDATE dbo.Widgets SET Stock = Stock - 1 WHERE WidgetId = 1;
    WAITFOR DELAY '00:00:10';
    UPDATE dbo.Orders  SET Qty   = Qty   + 1 WHERE OrderId  = 101;
COMMIT TRANSACTION;
GO
-- Will return Msg 1205 on the victim session (deadlock chosen by SQL Server)

-- ─────────────────────────────────────────────────────────────────────────────
-- FIXED — same lock order as Session 2 (Widgets → Orders)
BEGIN TRANSACTION;
    UPDATE dbo.Widgets SET Stock = Stock - 1 WHERE WidgetId = 1;
    UPDATE dbo.Orders  SET Qty   = Qty   + 1 WHERE OrderId  = 101;
COMMIT TRANSACTION;
GO
-- Will return (1 row(s) affected) twice, transaction commits cleanly

-- ─────────────────────────────────────────────────────────────────────────────
-- Read deadlock graph from SQL Server Error Log (trace flag 1222)
EXEC xp_readerrorlog 0, 1, N'deadlock';
-- Will return deadlock XML entries with process IDs and resource descriptors

-- Read from Extended Events ring buffer
SELECT CAST(target_data AS XML) AS DeadlockGraph
FROM   sys.dm_xe_session_targets t
JOIN   sys.dm_xe_sessions         s ON s.address = t.event_session_address
WHERE  s.name = 'DeadlockCapture';
-- Will return xml_deadlock_report event with full deadlock graph XML

-- Live waits at the moment both sessions are blocked
SELECT
    wt.session_id,
    wt.wait_type,
    wt.wait_duration_ms,
    wt.blocking_session_id,
    wt.resource_description
FROM sys.dm_os_waiting_tasks wt
WHERE wt.blocking_session_id IS NOT NULL;
-- Will return two rows showing each session blocking the other
