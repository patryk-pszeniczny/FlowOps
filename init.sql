IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [CustomerDirectory] (
    [CustomerId] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [TaxId] nvarchar(32) NULL,
    [Email] nvarchar(256) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CustomerDirectory] PRIMARY KEY ([CustomerId])
);

CREATE TABLE [CustomerReports] (
    [CustomerId] uniqueidentifier NOT NULL,
    [ActiveSubscriptions] int NOT NULL,
    [TotalInvoiced] decimal(18,2) NOT NULL,
    [TotalPaid] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_CustomerReports] PRIMARY KEY ([CustomerId])
);

CREATE TABLE [Customers] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [TaxId] nvarchar(32) NULL,
    [Email] nvarchar(256) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
);

CREATE TABLE [IdempotencyKeys] (
    [Key] nvarchar(200) NOT NULL,
    [SubscriptionId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_IdempotencyKeys] PRIMARY KEY ([Key])
);

CREATE TABLE [InboxMessages] (
    [Consumer] nvarchar(200) NOT NULL,
    [EventId] uniqueidentifier NOT NULL,
    [ProcessedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_InboxMessages] PRIMARY KEY ([Consumer], [EventId])
);

CREATE TABLE [IntegrationEvents] (
    [Id] uniqueidentifier NOT NULL,
    [TypeName] nvarchar(256) NOT NULL,
    [OccurredAt] datetime2 NOT NULL,
    [Version] int NOT NULL,
    [PayLoadJson] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_IntegrationEvents] PRIMARY KEY ([Id])
);

CREATE TABLE [Subscriptions] (
    [Id] uniqueidentifier NOT NULL,
    [CustomerId] uniqueidentifier NOT NULL,
    [PlanCode] nvarchar(100) NOT NULL,
    [Status] nvarchar(24) NOT NULL,
    [ActivatedAt] datetime2 NULL,
    [ExpiresAt] datetime2 NULL,
    [CancelledAt] datetime2 NULL,
    [SuspendedAt] datetime2 NULL,
    [ResumedAt] datetime2 NULL,
    CONSTRAINT [PK_Subscriptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Subscriptions_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ActiveSubscriptionIds] (
    [CustomerId] uniqueidentifier NOT NULL,
    [SubscriptionId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_ActiveSubscriptionIds] PRIMARY KEY ([CustomerId], [SubscriptionId]),
    CONSTRAINT [FK_ActiveSubscriptionIds_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]),
    CONSTRAINT [FK_ActiveSubscriptionIds_Subscriptions_SubscriptionId] FOREIGN KEY ([SubscriptionId]) REFERENCES [Subscriptions] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_ActiveSubscriptionIds_SubscriptionId] ON [ActiveSubscriptionIds] ([SubscriptionId]);

CREATE INDEX [IX_InboxMessages_ProcessedAt] ON [InboxMessages] ([ProcessedAt]);

CREATE INDEX [IX_Subscriptions_CustomerId] ON [Subscriptions] ([CustomerId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251218231546_0001_InitialCreate', N'9.0.0');

COMMIT;
GO

