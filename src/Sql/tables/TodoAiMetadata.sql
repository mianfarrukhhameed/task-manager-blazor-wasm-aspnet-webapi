CREATE TABLE [dbo].[TodoAiMetadata]
(
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [TodoId] INT NOT NULL,
    [AiSummary] NVARCHAR(500) NULL,
    [AiSummaryModel] NVARCHAR(100) NULL,
    [AiPriority] NVARCHAR(10) NULL,
    [AiCategory] NVARCHAR(50) NULL,
    [AiType] NVARCHAR(50) NULL,
    [ConfidenceScore] FLOAT NULL,
    [CreatedAt] DATETIME NOT NULL,
    [UpdatedAt] DATETIME NULL,
    CONSTRAINT [FK_TodoAiMetadata_TodoTask] FOREIGN KEY ([TodoId]) REFERENCES [dbo].[TodoTask]([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_TodoAiMetadata_TodoId] ON [dbo].[TodoAiMetadata]([TodoId]);
