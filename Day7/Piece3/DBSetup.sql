CREATE TABLE [Quotes] (
    [Id]        INT              NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [Author]    NVARCHAR(100)    NOT NULL,
    [Text]      NVARCHAR(1000)   NOT NULL,
    [CreatedAt] DATETIMEOFFSET   NOT NULL,
    [OwnerId]   INT              NULL
);

CREATE TABLE [Users] (
    [Id]           INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [Email]        NVARCHAR(256) NOT NULL,
    [PasswordHash] NVARCHAR(MAX) NOT NULL
);

CREATE TABLE [RefreshTokens] (
    [Id]              INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [TokenHash]       NVARCHAR(256)  NOT NULL,
    [UserId]          INT            NOT NULL,
    [FamilyId]        NVARCHAR(50)   NOT NULL,
    [ExpiresAt]       DATETIMEOFFSET NOT NULL,
    [RevokedAt]       DATETIMEOFFSET NULL,
    [ReplacedByToken] NVARCHAR(MAX)  NULL,
    CONSTRAINT [FK_RefreshTokens_Users_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);
CREATE INDEX [IX_RefreshTokens_FamilyId]         ON [RefreshTokens] ([FamilyId]);

CREATE TABLE [Categories] (
    [Id]   INT          NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [Name] NVARCHAR(50) NOT NULL
);

CREATE TABLE [Tags] (
    [Id]   INT          NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [Name] NVARCHAR(50) NOT NULL
);

CREATE TABLE [QuoteCategories] (
    [QuoteId]    INT NOT NULL,
    [CategoryId] INT NOT NULL,
    CONSTRAINT [PK_QuoteCategories]              PRIMARY KEY ([QuoteId], [CategoryId]),
    CONSTRAINT [FK_QuoteCategories_Quotes]       FOREIGN KEY ([QuoteId])    REFERENCES [Quotes]     ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_QuoteCategories_Categories]   FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [QuoteTags] (
    [QuoteId] INT NOT NULL,
    [TagId]   INT NOT NULL,
    CONSTRAINT [PK_QuoteTags]        PRIMARY KEY ([QuoteId], [TagId]),
    CONSTRAINT [FK_QuoteTags_Quotes] FOREIGN KEY ([QuoteId]) REFERENCES [Quotes] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_QuoteTags_Tags]   FOREIGN KEY ([TagId])   REFERENCES [Tags]   ([Id]) ON DELETE CASCADE
);

-- Users (15 rows)
-- All passwords = "password"
INSERT INTO [Users] ([Email], [PasswordHash]) VALUES
('user1@email.com',  '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi'),
('user2@email.com',  '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi'),
('user3@email.com',  '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi'),
('user4@email.com',  '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi'),
('user5@email.com',  '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi'),
('user6@email.com',  '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi'),
('user7@email.com',  '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi'),
('user8@email.com',  '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi'),
('user9@email.com',  '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi'),
('user10@email.com', '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi'),
('user11@email.com', '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi'),
('user12@email.com', '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi'),
('user13@email.com', '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi'),
('user14@email.com', '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi'),
('user15@email.com', '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi');


-- Quotes (20 rows)
INSERT INTO [Quotes] ([Author], [Text], [CreatedAt], [OwnerId]) VALUES
('Mark Twain',           'The secret of getting ahead is getting started.',                                            '2025-06-01 09:00:00 +00:00', 1),
('Albert Einstein',      'Life is like riding a bicycle. To keep your balance, you must keep moving.',                '2025-06-15 11:20:00 +00:00', 2),
('Maya Angelou',         'You will face many defeats in life, but never let yourself be defeated.',                   '2025-07-04 08:45:00 +00:00', 2),
('Winston Churchill',    'Success is not final, failure is not fatal: it is the courage to continue that counts.',    '2025-07-20 14:30:00 +00:00', 3),
('Oscar Wilde',          'Be yourself; everyone else is already taken.',                                              '2025-08-02 10:10:00 +00:00', 4),
('Steve Jobs',           'The only way to do great work is to love what you do.',                                     '2025-08-18 16:00:00 +00:00', 4),
('Abraham Lincoln',      'Whatever you are, be a good one.',                                                          '2025-09-05 09:30:00 +00:00', 5),
('Friedrich Nietzsche',  'That which does not kill us makes us stronger.',                                            '2025-09-21 13:15:00 +00:00', 6),
('Mahatma Gandhi',       'Be the change you wish to see in the world.',                                               '2025-10-08 07:50:00 +00:00', 7),
('Aristotle',            'We are what we repeatedly do. Excellence, then, is not an act, but a habit.',              '2025-10-25 12:00:00 +00:00', 8),
('Benjamin Franklin',    'Tell me and I forget. Teach me and I remember. Involve me and I learn.',                   '2025-11-03 15:40:00 +00:00', 9),
('Ralph Waldo Emerson',  'Do not go where the path may lead; go instead where there is no path and leave a trail.',  '2025-11-19 10:25:00 +00:00', 10),
('Henry Ford',           'Whether you think you can or you think you cannot, you are right.',                         '2025-12-01 08:00:00 +00:00', 11),
('Socrates',             'The only true wisdom is in knowing you know nothing.',                                      '2025-12-14 17:30:00 +00:00', 12),
('Confucius',            'It does not matter how slowly you go as long as you do not stop.',                         '2026-01-06 09:45:00 +00:00', 13),
('Theodore Roosevelt',   'Believe you can and you are halfway there.',                                                '2026-01-22 11:00:00 +00:00', 14),
('Nelson Mandela',       'It always seems impossible until it is done.',                                              '2026-02-10 14:20:00 +00:00', 15),
('Lao Tzu',              'A journey of a thousand miles begins with a single step.',                                  '2026-03-03 08:30:00 +00:00', NULL),
('William Shakespeare',  'To be, or not to be, that is the question.',                                               '2026-03-28 10:00:00 +00:00', NULL),
('Mark Twain',           'If you tell the truth, you don''t have to remember anything.',                              '2026-04-15 13:10:00 +00:00', 1);


-- RefreshTokens (15 rows)
-- Active = not expired, not revoked
-- Expired = ExpiresAt in past
-- Revoked = RevokedAt set
INSERT INTO [RefreshTokens] ([TokenHash], [UserId], [FamilyId], [ExpiresAt], [RevokedAt], [ReplacedByToken]) VALUES
-- Active tokens
('a3f8c1d2e4b5967f0a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3', 1,  'fam-1a2b3c4d-0001', '2026-08-01 00:00:00 +00:00', NULL, NULL),
('b4e9d2e3f5c6078a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4', 2,  'fam-2b3c4d5e-0002', '2026-08-15 00:00:00 +00:00', NULL, NULL),
('c5fa e3f4a6d7189b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5', 3,  'fam-3c4d5e6f-0003', '2026-09-01 00:00:00 +00:00', NULL, NULL),
('d6ab f4a5b7e8290c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6', 5,  'fam-4d5e6f7a-0005', '2026-09-20 00:00:00 +00:00', NULL, NULL),
('e7bc a5b6c8f9301d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7', 8,  'fam-5e6f7a8b-0008', '2026-10-05 00:00:00 +00:00', NULL, NULL),
('f8cd b6c7d9a0412e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8', 12, 'fam-6f7a8b9c-0012', '2026-10-18 00:00:00 +00:00', NULL, NULL),

-- Expired tokens (ExpiresAt in the past)
('09de c7d8e0b1523f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9', 4,  'fam-7a8b9c0d-0004', '2025-12-01 00:00:00 +00:00', NULL, NULL),
('10ef d8e9f1c2634a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0', 6,  'fam-8b9c0d1e-0006', '2026-01-15 00:00:00 +00:00', NULL, NULL),
('21f0 e9f0a2d3745b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1', 9,  'fam-9c0d1e2f-0009', '2026-02-10 00:00:00 +00:00', NULL, NULL),
('3201 f0a1b3e4856c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b2', 13, 'fam-0d1e2f3a-0013', '2026-03-05 00:00:00 +00:00', NULL, NULL),

-- Revoked tokens (part of rotated families)
('4312 a1b2c4f5967d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b2c3', 1,  'fam-1a2b3c4d-0001', '2026-08-01 00:00:00 +00:00', '2026-04-10 08:00:00 +00:00', 'a3f8c1d2e4b5967f0a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3'),
('5423 b2c3d5a6078e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b2c3d4', 7,  'fam-2e3f4a5b-0007', '2026-07-01 00:00:00 +00:00', '2026-05-01 12:00:00 +00:00', NULL),
('6534 c3d4e6b7189f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b2c3d4e5', 10, 'fam-3f4a5b6c-0010', '2026-07-15 00:00:00 +00:00', '2026-05-10 09:30:00 +00:00', NULL),
('7645 d4e5f7c8290a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b2c3d4e5f6', 11, 'fam-4a5b6c7d-0011', '2026-06-01 00:00:00 +00:00', '2026-05-15 14:00:00 +00:00', NULL),
('8756 e5f6a8d9301b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b2c3d4e5f6a7', 14, 'fam-5b6c7d8e-0014', '2026-06-20 00:00:00 +00:00', '2026-05-20 11:15:00 +00:00', NULL);

-- Inserting More Quotes
INSERT INTO [Quotes] ([Author], [Text], [CreatedAt], [OwnerId]) VALUES

-- Mark Twain (was 2, now 5)
('Mark Twain', 'Whenever you find yourself on the side of the majority, it is time to pause and reflect.',                          '2026-04-20 09:00:00 +00:00', 2),
('Mark Twain', 'Twenty years from now you will be more disappointed by the things you did not do than by the ones you did.',        '2026-04-21 10:00:00 +00:00', 3),
('Mark Twain', 'It ain''t what you don''t know that gets you into trouble. It''s what you know for sure that just ain''t so.',      '2026-04-22 11:00:00 +00:00', 4),

-- Albert Einstein (was 1, now 4)
('Albert Einstein', 'Imagination is more important than knowledge.',                                                                '2026-04-23 09:00:00 +00:00', 5),
('Albert Einstein', 'A person who never made a mistake never tried anything new.',                                                  '2026-04-24 10:00:00 +00:00', 6),
('Albert Einstein', 'The measure of intelligence is the ability to change.',                                                        '2026-04-25 11:00:00 +00:00', 7),

-- Maya Angelou (was 1, now 4)
('Maya Angelou', 'Nothing will work unless you do.',                                                                               '2026-04-26 09:00:00 +00:00', 8),
('Maya Angelou', 'Try to be a rainbow in someone''s cloud.',                                                                       '2026-04-27 10:00:00 +00:00', 9),
('Maya Angelou', 'I''ve learned that people will never forget how you made them feel.',                                            '2026-04-28 11:00:00 +00:00', 10),

-- Winston Churchill (was 1, now 4)
('Winston Churchill', 'If you''re going through hell, keep going.',                                                                '2026-04-29 09:00:00 +00:00', 11),
('Winston Churchill', 'We make a living by what we get, but we make a life by what we give.',                                      '2026-04-30 10:00:00 +00:00', 12),
('Winston Churchill', 'Attitude is a little thing that makes a big difference.',                                                    '2026-05-01 11:00:00 +00:00', 13),

-- Oscar Wilde (was 1, now 4)
('Oscar Wilde', 'To live is the rarest thing in the world. Most people exist, that is all.',                                       '2026-05-02 09:00:00 +00:00', 14),
('Oscar Wilde', 'I can resist everything except temptation.',                                                                      '2026-05-03 10:00:00 +00:00', 15),
('Oscar Wilde', 'Always forgive your enemies; nothing annoys them so much.',                                                       '2026-05-04 11:00:00 +00:00', 1),

-- Steve Jobs (was 1, now 4)
('Steve Jobs', 'Innovation distinguishes between a leader and a follower.',                                                        '2026-05-05 09:00:00 +00:00', 2),
('Steve Jobs', 'Stay hungry, stay foolish.',                                                                                       '2026-05-06 10:00:00 +00:00', 3),
('Steve Jobs', 'Design is not just what it looks like. Design is how it works.',                                                   '2026-05-07 11:00:00 +00:00', 4),

-- Abraham Lincoln (was 1, now 4)
('Abraham Lincoln', 'Give me six hours to chop down a tree and I will spend the first four sharpening the axe.',                  '2026-05-08 09:00:00 +00:00', 5),
('Abraham Lincoln', 'In the end, it''s not the years in your life that count. It''s the life in your years.',                     '2026-05-09 10:00:00 +00:00', 6),
('Abraham Lincoln', 'Nearly all men can stand adversity, but to test a man''s character, give him power.',                        '2026-05-10 11:00:00 +00:00', 7),

-- Friedrich Nietzsche (was 1, now 4)
('Friedrich Nietzsche', 'Without music, life would be a mistake.',                                                                 '2026-05-11 09:00:00 +00:00', 8),
('Friedrich Nietzsche', 'He who has a why to live can bear almost any how.',                                                       '2026-05-11 12:00:00 +00:00', 9),
('Friedrich Nietzsche', 'There are no facts, only interpretations.',                                                               '2026-05-12 09:00:00 +00:00', 10),

-- Mahatma Gandhi (was 1, now 4)
('Mahatma Gandhi', 'Strength does not come from physical capacity. It comes from an indomitable will.',                           '2026-05-12 12:00:00 +00:00', 11),
('Mahatma Gandhi', 'An eye for an eye only ends up making the whole world blind.',                                                 '2026-05-13 09:00:00 +00:00', 12),
('Mahatma Gandhi', 'The weak can never forgive. Forgiveness is the attribute of the strong.',                                     '2026-05-13 12:00:00 +00:00', 13),

-- Aristotle (was 1, now 4)
('Aristotle', 'Knowing yourself is the beginning of all wisdom.',                                                                  '2026-05-14 09:00:00 +00:00', 14),
('Aristotle', 'The more you know, the more you know you don''t know.',                                                            '2026-05-14 12:00:00 +00:00', 15),
('Aristotle', 'Happiness depends upon ourselves.',                                                                                 '2026-05-15 09:00:00 +00:00', 1),

-- Benjamin Franklin (was 1, now 4)
('Benjamin Franklin', 'An investment in knowledge pays the best interest.',                                                        '2026-05-15 12:00:00 +00:00', 2),
('Benjamin Franklin', 'By failing to prepare, you are preparing to fail.',                                                         '2026-05-16 09:00:00 +00:00', 3),
('Benjamin Franklin', 'Early to bed and early to rise makes a man healthy, wealthy, and wise.',                                    '2026-05-16 12:00:00 +00:00', 4),

-- Ralph Waldo Emerson (was 1, now 4)
('Ralph Waldo Emerson', 'What lies within us dwarfs what lies behind or before us.',                                               '2026-05-17 09:00:00 +00:00', 5),
('Ralph Waldo Emerson', 'The only way to have a friend is to be one.',                                                             '2026-05-17 12:00:00 +00:00', 6),
('Ralph Waldo Emerson', 'Life is a journey, not a destination.',                                                                   '2026-05-18 09:00:00 +00:00', 7),

-- Henry Ford (was 1, now 4)
('Henry Ford', 'Coming together is a beginning; staying together is progress; working together is success.',                       '2026-05-18 12:00:00 +00:00', 8),
('Henry Ford', 'Failure is simply the opportunity to begin again, this time more intelligently.',                                  '2026-05-19 09:00:00 +00:00', 9),
('Henry Ford', 'Anyone who stops learning is old, whether at twenty or eighty.',                                                   '2026-05-19 12:00:00 +00:00', 10),

-- Socrates (was 1, now 4)
('Socrates', 'An unexamined life is not worth living.',                                                                            '2026-05-20 09:00:00 +00:00', 11),
('Socrates', 'Education is the kindling of a flame, not the filling of a vessel.',                                                 '2026-05-20 10:00:00 +00:00', 12),
('Socrates', 'I know that I am intelligent, because I know that I know nothing.',                                                  '2026-05-20 11:00:00 +00:00', 13),

-- Confucius (was 1, now 4)
('Confucius', 'Everything has beauty, but not everyone sees it.',                                                                  '2026-05-20 12:00:00 +00:00', 14),
('Confucius', 'The man who asks a question is a fool for a minute; the man who does not ask is a fool for his life.',             '2026-05-20 13:00:00 +00:00', 15),
('Confucius', 'Life is really simple, but we insist on making it complicated.',                                                    '2026-05-20 14:00:00 +00:00', 1),

-- Theodore Roosevelt (was 1, now 4)
('Theodore Roosevelt', 'Do what you can, with what you have, where you are.',                                                      '2026-05-20 15:00:00 +00:00', 2),
('Theodore Roosevelt', 'It is hard to fail, but it is worse never to have tried to succeed.',                                      '2026-05-20 16:00:00 +00:00', 3),
('Theodore Roosevelt', 'The best prize life offers is the chance to work hard at work worth doing.',                               '2026-05-21 09:00:00 +00:00', 4),

-- Nelson Mandela (was 1, now 4)
('Nelson Mandela', 'Education is the most powerful weapon you can use to change the world.',                                       '2026-05-21 10:00:00 +00:00', 5),
('Nelson Mandela', 'Do not judge me by my successes; judge me by how many times I fell down and got back up.',                    '2026-05-21 11:00:00 +00:00', 6),
('Nelson Mandela', 'A winner is a dreamer who never gives up.',                                                                    '2026-05-21 12:00:00 +00:00', 7),

-- Lao Tzu (was 1, now 4)
('Lao Tzu', 'Nature does not hurry, yet everything is accomplished.',                                                              '2026-05-21 13:00:00 +00:00', 8),
('Lao Tzu', 'When I let go of what I am, I become what I might be.',                                                              '2026-05-21 14:00:00 +00:00', 9),
('Lao Tzu', 'The flame that burns twice as bright burns half as long.',                                                            '2026-05-21 15:00:00 +00:00', 10),

-- William Shakespeare (was 1, now 4)
('William Shakespeare', 'All the world''s a stage, and all the men and women merely players.',                                    '2026-05-21 16:00:00 +00:00', 11),
('William Shakespeare', 'We know what we are, but know not what we may be.',                                                      '2026-05-22 09:00:00 +00:00', 12),
('William Shakespeare', 'The course of true love never did run smooth.',                                                           '2026-05-22 10:00:00 +00:00', 13);


-- ============================================================
-- Categories  (Id 1 = classic, Id 2 = modern)
-- ============================================================
INSERT INTO [Categories] ([Name]) VALUES
('classic'),
('modern');


-- ============================================================
-- Tags  (Id 1-10)
-- ============================================================
INSERT INTO [Tags] ([Name]) VALUES
('wisdom'),       -- 1
('motivation'),   -- 2
('humor'),        -- 3
('philosophy'),   -- 4
('perseverance'), -- 5
('success'),      -- 6
('life'),         -- 7
('education'),    -- 8
('truth'),        -- 9
('change');       -- 10


-- ============================================================
-- QuoteCategories
-- Nietzsche, Twain, Franklin, Wilde appear in BOTH classic and modern.
-- Aristotle, Socrates, Shakespeare, Confucius, Lao Tzu, Lincoln,
--   Gandhi, Emerson → classic only.
-- Einstein, Angelou, Churchill, Jobs → modern only.
-- Theodore Roosevelt, Nelson Mandela, Henry Ford → NO category.
-- ============================================================

-- Classic (CategoryId = 1)
INSERT INTO [QuoteCategories] ([QuoteId], [CategoryId]) VALUES
(1,  1),   -- Twain      "The secret of getting ahead..."
(5,  1),   -- Wilde      "Be yourself..."
(7,  1),   -- Lincoln    "Whatever you are, be a good one."
(8,  1),   -- Nietzsche  "That which does not kill us..."
(9,  1),   -- Gandhi     "Be the change you wish to see..."
(10, 1),   -- Aristotle  "We are what we repeatedly do..."
(11, 1),   -- Franklin   "Tell me and I forget..."
(12, 1),   -- Emerson    "Do not go where the path may lead..."
(14, 1),   -- Socrates   "The only true wisdom..."
(15, 1),   -- Confucius  "It does not matter how slowly you go..."
(18, 1),   -- Lao Tzu   "A journey of a thousand miles..."
(19, 1),   -- Shakespeare "To be, or not to be..."
(20, 1),   -- Twain      "If you tell the truth..."
(39, 1),   -- Lincoln    "Give me six hours to chop down a tree..."
(45, 1),   -- Gandhi     "Strength does not come from physical capacity..."
(48, 1),   -- Aristotle  "Knowing yourself is the beginning of all wisdom."
(54, 1),   -- Emerson    "What lies within us..."
(60, 1),   -- Socrates   "An unexamined life is not worth living."
(63, 1),   -- Confucius  "Everything has beauty..."
(72, 1),   -- Lao Tzu   "Nature does not hurry..."
(75, 1);   -- Shakespeare "All the world's a stage..."

-- Modern (CategoryId = 2)
INSERT INTO [QuoteCategories] ([QuoteId], [CategoryId]) VALUES
(2,  2),   -- Einstein   "Life is like riding a bicycle..."
(3,  2),   -- Angelou    "You will face many defeats..."
(4,  2),   -- Churchill  "Success is not final..."
(6,  2),   -- Jobs       "The only way to do great work..."
(21, 2),   -- Twain      "Whenever you find yourself on the side of the majority..."
(22, 2),   -- Twain      "Twenty years from now..."
(24, 2),   -- Einstein   "Imagination is more important than knowledge."
(25, 2),   -- Einstein   "A person who never made a mistake..."
(27, 2),   -- Angelou    "Nothing will work unless you do."
(28, 2),   -- Angelou    "Try to be a rainbow..."
(30, 2),   -- Churchill  "If you're going through hell, keep going."
(31, 2),   -- Churchill  "We make a living by what we get..."
(33, 2),   -- Wilde      "To live is the rarest thing..."
(34, 2),   -- Wilde      "I can resist everything except temptation."
(36, 2),   -- Jobs       "Innovation distinguishes between a leader and a follower."
(37, 2),   -- Jobs       "Stay hungry, stay foolish."
(42, 2),   -- Nietzsche  "Without music, life would be a mistake."
(43, 2),   -- Nietzsche  "He who has a why to live..."
(51, 2),   -- Franklin   "An investment in knowledge pays the best interest."
(52, 2);   -- Franklin   "By failing to prepare, you are preparing to fail."

INSERT INTO [QuoteTags] ([QuoteId], [TagId]) VALUES
(1,  2),   -- Twain  "The secret of getting ahead..."  → motivation
(1,  6),   --                                          → success
(5,  7),   -- Wilde  "Be yourself..."                  → life
(5,  3),   --                                          → humor
(7,  1),   -- Lincoln "Whatever you are..."            → wisdom
(8,  4),   -- Nietzsche "That which does not kill us"  → philosophy
(8,  5),   --                                          → perseverance
(9,  10),  -- Gandhi  "Be the change..."               → change
(10, 1),   -- Aristotle "We are what we repeatedly do" → wisdom
(10, 4),   --                                          → philosophy
(11, 8),   -- Franklin "Tell me and I forget..."       → education
(11, 1),   --                                          → wisdom
(12, 7),   -- Emerson  "Do not go where the path..."   → life
(14, 1),   -- Socrates "The only true wisdom..."       → wisdom
(14, 4),   --                                          → philosophy
(15, 5),   -- Confucius "It does not matter how slowly" → perseverance
(18, 1),   -- Lao Tzu "A journey of a thousand miles"  → wisdom
(19, 7);   -- Shakespeare "To be, or not to be..."     → life

-- Modern tagged quotes
INSERT INTO [QuoteTags] ([QuoteId], [TagId]) VALUES
(2,  7),   -- Einstein  "Life is like riding a bicycle" → life
(3,  2),   -- Angelou   "You will face many defeats..."  → motivation
(3,  5),   --                                            → perseverance
(4,  5),   -- Churchill "Success is not final..."        → perseverance
(4,  6),   --                                            → success
(6,  2),   -- Jobs      "The only way to do great work"  → motivation
(6,  6),   --                                            → success
(21, 9),   -- Twain     "Whenever you find yourself..."  → truth
(24, 2),   -- Einstein  "Imagination is more important"  → motivation
(24, 8),   --                                            → education
(27, 2),   -- Angelou   "Nothing will work unless you do" → motivation
(33, 7),   -- Wilde     "To live is the rarest thing..."  → life
(33, 3),   --                                             → humor
(36, 10),  -- Jobs      "Innovation distinguishes..."     → change
(36, 6),   --                                             → success
(42, 7),   -- Nietzsche "Without music, life would be..." → life
(42, 4),   --                                             → philosophy
(51, 8),   -- Franklin  "An investment in knowledge..."   → education
(51, 6);   --                                             → success