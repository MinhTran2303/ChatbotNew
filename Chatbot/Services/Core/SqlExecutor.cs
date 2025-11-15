using Microsoft.Data.SqlClient;
using Dapper;

namespace Chatbot.Services.Core
{
    public class SqlExecutor
    {
        private readonly IConfiguration _config;

        public SqlExecutor(IConfiguration config)
        {
            _config = config;
        }

        public async Task<IEnumerable<dynamic>> ExecuteQueryAsync(string connectionName, string query)
        {
            var connString = _config.GetConnectionString(connectionName);
            using var conn = new SqlConnection(connString);
            return await conn.QueryAsync(query);
        }
    }
}
