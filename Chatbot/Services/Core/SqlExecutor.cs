using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Chatbot.Services.Core
{
    public class SqlExecutor
    {
        private readonly IConfiguration _config;

        public SqlExecutor(IConfiguration config)
        {
            _config = config;
        }

        // Hàm cơ bản (không có tham số)
        public async Task<IEnumerable<dynamic>> ExecuteQueryAsync(string connectionName, string query)
        {
            var connStr = _config.GetConnectionString(connectionName);
            if (string.IsNullOrEmpty(connStr))
                throw new InvalidOperationException($"Không tìm thấy ConnectionString '{connectionName}' trong appsettings.json.");

            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();
            return await conn.QueryAsync(query);
        }

        // Hàm nâng cao (có parameter)
        public async Task<IEnumerable<dynamic>> ExecuteQueryAsync(string connectionName, string query, object parameters)
        {
            var connStr = _config.GetConnectionString(connectionName);
            if (string.IsNullOrEmpty(connStr))
                throw new InvalidOperationException($"Không tìm thấy ConnectionString '{connectionName}' trong appsettings.json.");

            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();
            return await conn.QueryAsync(query, parameters);
        }
    }
}
