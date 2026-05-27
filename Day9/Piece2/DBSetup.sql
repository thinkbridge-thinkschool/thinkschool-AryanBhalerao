USE master;
GO

IF DB_ID('DeadlockLab') IS NOT NULL
BEGIN
    ALTER DATABASE DeadlockLab SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE DeadlockLab;
END
GO

CREATE DATABASE DeadlockLab;
GO

USE DeadlockLab;
GO

CREATE TABLE dbo.Widgets
(
    WidgetId   INT          NOT NULL PRIMARY KEY,
    Name       VARCHAR(50)  NOT NULL,
    Stock      INT          NOT NULL
);

CREATE TABLE dbo.Orders
(
    OrderId    INT          NOT NULL PRIMARY KEY,
    WidgetId   INT          NOT NULL,
    Qty        INT          NOT NULL
);
GO

INSERT INTO dbo.Widgets VALUES (1, 'Sprocket', 100), (2, 'Cog', 200);
INSERT INTO dbo.Orders  VALUES (101, 1, 5),          (102, 2, 3);
GO
-- Will return (2 row(s) affected) twice

-- Enable deadlock capture via trace flag
DBCC TRACEON(1222, -1);
-- Will return DBCC execution completed

DBCC TRACESTATUS(1222);
-- Will return TraceFlag 1222, Status 1, Global 1, Session 0

-- Or use Extended Events (ring-buffer based)
CREATE EVENT SESSION [DeadlockCapture] ON SERVER
ADD EVENT sqlserver.xml_deadlock_report
ADD TARGET  package0.ring_buffer (SET max_memory = 4096)
WITH (MAX_DISPATCH_LATENCY = 5 SECONDS);
GO

ALTER EVENT SESSION [DeadlockCapture] ON SERVER STATE = START;
GO
-- Will return Command(s) completed successfully

-- ─── Teardown ────────────────────────────────────────────────────────────────
ALTER EVENT SESSION [DeadlockCapture] ON SERVER STATE = STOP;
DROP  EVENT SESSION [DeadlockCapture] ON SERVER;
DBCC TRACEOFF(1222, -1);

USE master;
ALTER DATABASE DeadlockLab SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE DeadlockLab;
GO
-- Will return Command(s) completed successfully
