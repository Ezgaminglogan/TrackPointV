using Microsoft.Data.SqlClient;
using System.Data;

namespace TrackPointV.Service
{
    public class Connection
    {
        private readonly string ConnectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=E:\\Programming\\TrackPointV\\Database\\TrackPoints.mdf;Integrated Security=True";

        // Get a new SQL connection
        public SqlConnection GetConnection()
        {
            try
            {
                return new SqlConnection(ConnectionString);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to create database connection", ex);
            }
        }

        // Open a connection and return it
        public async Task<SqlConnection> OpenConnectionAsync()
        {
            try
            {
                SqlConnection connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();
                return connection;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to open database connection", ex);
            }
        }

        // Close a connection
        public async Task CloseConnectionAsync(SqlConnection? connection)
        {
            if (connection != null && connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }

        // Execute a non-query command (INSERT, UPDATE, DELETE)
        public async Task<int> ExecuteNonQueryAsync(string commandText, CommandType commandType = CommandType.Text, SqlParameter[]? parameters = null)
        {
            try
            {
                using SqlConnection connection = await OpenConnectionAsync();
                using SqlCommand command = new SqlCommand(commandText, connection);
                command.CommandType = commandType;
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }
                return await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to execute non-query command: {commandText}", ex);
            }
        }

        // Execute a query and return a DataTable
        public async Task<DataTable> ExecuteQueryAsync(string commandText, CommandType commandType = CommandType.Text, SqlParameter[]? parameters = null)
        {
            try
            {
                DataTable dataTable = new DataTable();
                using SqlConnection connection = await OpenConnectionAsync();
                using SqlCommand command = new SqlCommand(commandText, connection);
                command.CommandType = commandType;
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }
                using SqlDataAdapter adapter = new SqlDataAdapter(command);
                await Task.Run(() => adapter.Fill(dataTable));
                return dataTable;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to execute query: {commandText}", ex);
            }
        }

        // Execute a query and return a single value
        public async Task<object?> ExecuteScalarAsync(string commandText, CommandType commandType = CommandType.Text, SqlParameter[]? parameters = null)
        {
            try
            {
                using SqlConnection connection = await OpenConnectionAsync();
                using SqlCommand command = new SqlCommand(commandText, connection);
                command.CommandType = commandType;
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }
                return await command.ExecuteScalarAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to execute scalar query: {commandText}", ex);
            }
        }

        // Execute a query and return a SqlDataReader
        public async Task<SqlDataReader> ExecuteReaderAsync(string commandText, CommandType commandType = CommandType.Text, SqlParameter[]? parameters = null)
        {
            try
            {
                SqlConnection connection = await OpenConnectionAsync();
                SqlCommand command = new SqlCommand(commandText, connection);
                command.CommandType = commandType;
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }
                return await command.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to execute reader query: {commandText}", ex);
            }
        }

        // Create a parameter
        public SqlParameter CreateParameter(string name, object? value, SqlDbType dbType = SqlDbType.VarChar)
        {
            return new SqlParameter
            {
                ParameterName = name,
                Value = value ?? DBNull.Value,
                SqlDbType = dbType
            };
        }

        // Add this method to your Connection class
        public SqlParameter CreateParameter(string name, object value)
        {
            return new SqlParameter(name, value);
        }

        public int ExecuteNonQuery(string commandText, CommandType commandType = CommandType.Text, SqlParameter[]? parameters = null)
        {
            return ExecuteNonQueryAsync(commandText, commandType, parameters).GetAwaiter().GetResult();
        }
    }
}
