
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Events;
using FlowOps.Infrastructure.Sql;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FlowOps.Services.Reporting.Sql
{
    public sealed class SqlReportingProjector : IHostedService
    {
        private readonly IEventBus _bus;
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILogger<SqlReportingProjector> _logger;
        public SqlReportingProjector(
            IEventBus bus,
            ISqlConnectionFactory connectionFactory,
            ILogger<SqlReportingProjector> logger)
        {
            _bus = bus;
            _connectionFactory = connectionFactory;
            _logger = logger;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _bus.Subscribe<SubscriptionActivatedEvent>(OnActivaed);

            _logger.LogInformation("SqlReportingProjector started (subscribed to reporing events).");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
        private async Task OnActivaed(SubscriptionActivatedEvent ev)
        {
            await EnsureCustomerRowAsync(ev.CustomerId);
            await using var connection = await _connectionFactory.CreateOpenAsync();
            await ExecAsync(connection,
                "UPDATE dbo.CustomerReports SET ActiveSubscriptions = ActiveSubscriptions + 1 WHERE CustomerId = @cid;",
                p => p.Add(new SqlParameter("@cid", SqlDbType.UniqueIdentifier)
                {
                    Value = ev.CustomerId
                }));
            await ExecAsync(connection,
                "IF NOT EXISTS (SELECT 1 FROM dbo.ActiveSubscriptionIds WHERE CustomerId=@cid AND SubscriptionId=@sid)" +
                "INSERT INFO dbo.ActiveSubscriptionsIds(CustomerId, SubscriptionId) VALUES(@cid, @sid);",
                p =>
                {
                    p.Add(new SqlParameter("@cid", SqlDbType.UniqueIdentifier)
                    {
                        Value = ev.CustomerId
                    });
                    p.Add(new SqlParameter("@sid", SqlDbType.UniqueIdentifier)
                    {
                        Value = ev.SubscriptionId
                    });
                });
        }
        private async Task OnCancelled(SubscriptionCancelledEvent ev)
        {
            await EnsureCustomerRowAsync(ev.CustomerId);
            await using var connection = await _connectionFactory.CreateOpenAsync();
            await ExecAsync(connection,
                "UPDATE dbo.CustomerReports SET ActiveSubscriptions = CASE WHEN ActiveSubscriptions > 0 THEN ActiveSubscriptions-1 ELSE 0 END WHERE CustomerId = @cid;",
                p => p.Add(new SqlParameter("@cid", SqlDbType.UniqueIdentifier)
                {
                    Value = ev.CustomerId
                }));
            await ExecAsync(connection,
                "DELETE FROM dbo.ActiveSubscriptionIds WHERE CustomerId=@cid AND SubscriptionId=@sid;",
                p =>
                {
                    p.Add(new SqlParameter("@cid", SqlDbType.UniqueIdentifier)
                    {
                        Value = ev.CustomerId
                    });
                    p.Add(new SqlParameter("@sid", SqlDbType.UniqueIdentifier)
                    {
                        Value = ev.SubscriptionId
                    });
                });
        }
        private async Task OnSuspended(SubscriptionSuspendedEvent ev)
        {
            await EnsureCustomerRowAsync(ev.CustomerId);
            await using var conn = await _connectionFactory.CreateOpenAsync();
            await ExecAsync(conn,
                "UPDATE dbo.CustomerReports SET ActiveSubscriptions = CASE WHEN ActiveSubscriptions>0 THEN ActiveSubscriptions-1 ELSE 0 END WHERE CustomerId=@cid;",
                p => p.Add(new SqlParameter("@cid", SqlDbType.UniqueIdentifier) { Value = ev.CustomerId }));
            await ExecAsync(conn,
                "DELETE FROM dbo.ActiveSubscriptionIds WHERE CustomerId=@cid AND SubscriptionId=@sid;",
                p =>
                {
                    p.Add(new SqlParameter("@cid", SqlDbType.UniqueIdentifier) { Value = ev.CustomerId });
                    p.Add(new SqlParameter("@sid", SqlDbType.UniqueIdentifier) { Value = ev.SubscriptionId });
                });
        }

        private async Task OnResumed(SubscriptionResumedEvent ev)
        {
            await EnsureCustomerRowAsync(ev.CustomerId);
            await using var conn = await _connectionFactory.CreateOpenAsync();
            await ExecAsync(conn,
                "UPDATE dbo.CustomerReports SET ActiveSubscriptions = ActiveSubscriptions + 1 WHERE CustomerId=@cid;",
                p => p.Add(new SqlParameter("@cid", SqlDbType.UniqueIdentifier) { Value = ev.CustomerId }));
            await ExecAsync(conn, @"
IF NOT EXISTS (SELECT 1 FROM dbo.ActiveSubscriptionIds WHERE CustomerId=@cid AND SubscriptionId=@sid)
    INSERT INTO dbo.ActiveSubscriptionIds(CustomerId, SubscriptionId) VALUES(@cid, @sid);",
                p =>
                {
                    p.Add(new SqlParameter("@cid", SqlDbType.UniqueIdentifier) { Value = ev.CustomerId });
                    p.Add(new SqlParameter("@sid", SqlDbType.UniqueIdentifier) { Value = ev.SubscriptionId });
                });
        }

        private async Task OnInvoiced(InvoiceIssuedEvent ev)
        {
            await EnsureCustomerRowAsync(ev.CustomerId);
            await using var conn = await _connectionFactory.CreateOpenAsync();
            await ExecAsync(conn,
                "UPDATE dbo.CustomerReports SET TotalInvoiced = TotalInvoiced + @amount WHERE CustomerId=@cid;",
                p =>
                {
                    var a = new SqlParameter("@amount", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = ev.Amount };
                    p.Add(a);
                    p.Add(new SqlParameter("@cid", SqlDbType.UniqueIdentifier) { Value = ev.CustomerId });
                });
        }

        private async Task OnPaid(InvoicePaidEvent ev)
        {
            await EnsureCustomerRowAsync(ev.CustomerId);
            await using var conn = await _connectionFactory.CreateOpenAsync();
            await ExecAsync(conn,
                "UPDATE dbo.CustomerReports SET TotalPaid = TotalPaid + @amount WHERE CustomerId=@cid;",
                p =>
                {
                    var a = new SqlParameter("@amount", SqlDbType.Decimal) 
                    {
                        Precision = 18, 
                        Scale = 2, 
                        Value = ev.Amount 
                    };
                    p.Add(a);
                    p.Add(new SqlParameter("@cid", SqlDbType.UniqueIdentifier) { Value = ev.CustomerId });
                });
        }

        private async Task EnsureCustomerRowAsync(Guid customerId, CancellationToken ct = default)
        {
            await using var connection = await _connectionFactory.CreateOpenAsync(ct);
            await ExecAsync(connection, @"
                IF NOT EXISTS (SELECT 1 FROM dbo.CustomerReports WHERE CustomerId=@cid)
                INSERT INTO dbo.CustomerReports(CustomerId, ActiveSubscriptions, TotalInvoiced, TotalPaid)
                VALUES(@cid, 0, 0, 0);",
            p => p.Add(new SqlParameter("@cid", SqlDbType.UniqueIdentifier)
            {
                Value = customerId
            }), 
            ct
            );
        }
        private static async Task<int> ExecAsync(SqlConnection connection, string sql, Action<SqlParameterCollection> bind, CancellationToken ct = default)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            bind(command.Parameters);
            return await command.ExecuteNonQueryAsync(ct);
        }
    }
}
