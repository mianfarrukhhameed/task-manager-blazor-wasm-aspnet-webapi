/*
  Adds CreatedByUserId to TodoTask for resource-level authorization.
  Orphan rows are assigned to the seed admin user before NOT NULL is enforced.
*/
IF COL_LENGTH('dbo.TodoTask', 'CreatedByUserId') IS NULL
BEGIN
    ALTER TABLE [dbo].[TodoTask] ADD [CreatedByUserId] UNIQUEIDENTIFIER NULL;

    UPDATE [dbo].[TodoTask]
    SET [CreatedByUserId] = '1efb2983-09be-47a5-ac2c-bff124d542ec'
    WHERE [CreatedByUserId] IS NULL;

    ALTER TABLE [dbo].[TodoTask] ALTER COLUMN [CreatedByUserId] UNIQUEIDENTIFIER NOT NULL;

    CREATE INDEX [IX_TodoTask_CreatedByUserId] ON [dbo].[TodoTask]([CreatedByUserId]);
END
