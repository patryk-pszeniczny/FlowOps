
using FlowOps.Infrastructure.Sql;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FlowOps.Infrastructure.Idempotency
{
    public sealed class SqlIdempotencyStore : IIdempotencyStore
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        public SqlIdempotencyStore(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        public void Set(string key, Guid subscriptionId)
        {
            using var connection = _connectionFactory.Create();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;

            command.CommandText = @"
                IF NOT EXISTS (
                    SELECT 
                        1
                    FROM 
                        dbo.IdempotencyKeys
                    WHERE 
                        [Key] = @key)
                BEGIN
                    INSERT INTO dbo.IdempotencyKeys ([Key], SubscriptionId)
                    VALUES (@key, @sid)
                END";
            command.Parameters.Add(new SqlParameter("@key", DbType.String)
            {
                Value = key
            });
            command.Parameters.Add(new SqlParameter("@sid", DbType.Guid)
            {
                Value = subscriptionId
            });
            command.ExecuteNonQuery();

        }

        public bool TryGet(string key, out Guid subscriptionId)
        {
            using var connection = _connectionFactory.Create();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT 
                    SubscriptionId
                FROM
                    dbo.IdempotencyKeys
                WHERE
                    [Key] = @key";
            command.Parameters.Add(new SqlParameter("@key", DbType.String)
            {
                Value = key
            });

            var existingSubscriptionId = command.ExecuteScalar();
            connection.Close();
            if (existingSubscriptionId is Guid id)
            {
                subscriptionId = id;
                return true;
            }
            if(existingSubscriptionId is not null && Guid.TryParse(existingSubscriptionId.ToString(), out var parsedId))
            {
                subscriptionId = parsedId;
                return true;
            }
            subscriptionId = Guid.Empty;
            return false;
        }
    }
}
