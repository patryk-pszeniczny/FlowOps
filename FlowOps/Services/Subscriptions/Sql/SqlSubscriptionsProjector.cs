
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Events;
using FlowOps.Infrastructure.Sql;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FlowOps.Services.Subscriptions.Sql
{
    public sealed class SqlSubscriptionsProjector : IHostedService
    {
        private readonly IEventBus _bus;
        private readonly ISqlConnectionFactory _connection;
        private readonly ILogger<SqlSubscriptionsProjector> _logger;
        public SqlSubscriptionsProjector(
            IEventBus bus,
            ISqlConnectionFactory connectionFactory,
            ILogger<SqlSubscriptionsProjector> logger)
        {
            _bus = bus;
            _connection = connectionFactory;
            _logger = logger;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _bus.Subscribe<SubscriptionActivatedEvent>(OnActivated);
            _bus.Subscribe<SubscriptionSuspendedEvent>(OnSuspended);
            _bus.Subscribe<SubscriptionResumedEvent>(OnResumed);
            _bus.Subscribe<SubscriptionCancelledEvent>(OnCancelled);

            _logger.LogInformation("SqlSubscriptionsProjector started (listening for subscription lifecycle events).");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
        private async Task OnActivated(SubscriptionActivatedEvent ev)
        {
            await using var connection = await _connection.CreateOpenAsync();
            await ExecAsync(connection, @"
            IF EXISTS (SELECT 1 FROM dbo.Subscriptions WHERE SubscriptionId = @sid)
            BEGIN
                UPDATE dbo.Subscriptions
                SET CustomerId = @cid,
                    PlanCode   = @plan,
                    ActivatedAt= COALESCE(ActivatedAt, @ts),
                    SuspendedAt= NULL,
                    ResumedAt  = NULL,
                    CancelledAt= NULL
                WHERE SubscriptionId = @sid;
            END
            ELSE
            BEGIN
                INSERT INTO dbo.Subscriptions (SubscriptionId, CustomerId, PlanCode, Status, ActivatedAt)
                VALUES (@sid, @cid, @plan, N'Active', @ts);
            END",
            p =>
            {
                p.Add(new SqlParameter("@sid", SqlDbType.UniqueIdentifier) { Value = ev.SubscriptionId });
                p.Add(new SqlParameter("@cid", SqlDbType.UniqueIdentifier) { Value = ev.CustomerId });
                p.Add(new SqlParameter("@plan", SqlDbType.NVarChar, 100) { Value = ev.PlanCode });
                p.Add(new SqlParameter("@ts", SqlDbType.DateTime2) { Value = ev.OccurredOn });
            });
        }
        private async Task OnSuspended(SubscriptionSuspendedEvent ev)
        {
            await using var connection = await _connection.CreateOpenAsync();
            await ExecAsync(connection, @"
            UPDATE dbo.Subscriptions
            SET Status      = N'Suspended',
                SuspendedAt = COALESCE(SuspendedAt, @ts)
            WHERE SubscriptionId = @sid;",
            p =>
            {
                p.Add(new SqlParameter("@sid", SqlDbType.UniqueIdentifier) { Value = ev.SubscriptionId });
                p.Add(new SqlParameter("@ts", SqlDbType.DateTime2) { Value = ev.OccurredOn });
            });
        }
        private async Task OnResumed(SubscriptionResumedEvent ev)
        {
            await using var connection = await _connection.CreateOpenAsync();
            await ExecAsync(connection, @"
            UPDATE dbo.Subscriptions
            SET Status     = N'Active',
                ResumedAt  = COALESCE(ResumedAt, @ts),
            WHERE SubscriptionId = @sid;",
            p =>
            {
                p.Add(new SqlParameter("@sid", SqlDbType.UniqueIdentifier) { Value = ev.SubscriptionId });
                p.Add(new SqlParameter("@ts", SqlDbType.DateTime2) { Value = ev.OccurredOn });
            });
        }
        private async Task OnCancelled(SubscriptionCancelledEvent ev)
        {
            await using var connection = await _connection.CreateOpenAsync();
            await ExecAsync(connection, @"
            UPDATE dbo.Subscriptions
            SET Status      = N'Cancelled',
                CancelledAt = @ts
            WHERE SubscriptionId = @sid;",
            p =>
            {
                p.Add(new SqlParameter("@sid", SqlDbType.UniqueIdentifier) { Value = ev.SubscriptionId });
                p.Add(new SqlParameter("@ts", SqlDbType.DateTime2) { Value = ev.OccurredOn });
            });
        }
        private static async Task<int> ExecAsync(SqlConnection conn, string sql, Action<SqlParameterCollection> bind, CancellationToken ct = default)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = sql;
            bind(cmd.Parameters);
            return await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}
