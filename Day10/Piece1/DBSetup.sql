-- ============================================================
-- DB Setup — EF Core Change Tracker Demo
-- SQLite schema created automatically by EF Core (EnsureCreated)
-- ============================================================

-- Schema equivalent (what EF Core generates for SQLite):

CREATE TABLE IF NOT EXISTS "Quotes" (
    "Id"        INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "Author"    TEXT    NOT NULL,
    "Text"      TEXT    NOT NULL,
    "CreatedAt" TEXT    NOT NULL,
    "OwnerId"   INTEGER NULL
);

CREATE TABLE IF NOT EXISTS "Users" (
    "Id"           INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "Email"        TEXT    NOT NULL,
    "PasswordHash" TEXT    NOT NULL
);

CREATE TABLE IF NOT EXISTS "RefreshTokens" (
    "Id"              INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "TokenHash"       TEXT    NOT NULL,
    "UserId"          INTEGER NOT NULL,
    "FamilyId"        TEXT    NOT NULL,
    "ExpiresAt"       TEXT    NOT NULL,
    "RevokedAt"       TEXT    NULL,
    "ReplacedByToken" TEXT    NULL,
    FOREIGN KEY ("UserId") REFERENCES "Users" ("Id")
);

CREATE UNIQUE INDEX "IX_RefreshTokens_TokenHash" ON "RefreshTokens" ("TokenHash");
CREATE        INDEX "IX_RefreshTokens_FamilyId"  ON "RefreshTokens" ("FamilyId");

-- ============================================================
-- Seed: handled by the benchmark endpoint on first call
-- GET /benchmark/change-tracker  → seeds 10,000 quotes if needed
-- ============================================================

-- Manual seed example (SQLite — runs in a loop via recursive CTE):
-- INSERT INTO Quotes (Author, Text, CreatedAt)
-- WITH RECURSIVE gen(n) AS (
--     SELECT 1
--     UNION ALL
--     SELECT n + 1 FROM gen WHERE n < 10000
-- )
-- SELECT
--     'Author_' || n,
--     'Benchmark quote ' || n || '. The quick brown fox jumps over the lazy dog.',
--     datetime('now')
-- FROM gen;
