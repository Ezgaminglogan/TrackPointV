using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace TrackPointV.Service
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public bool IsGoogleUser { get; set; } = false;
        public string? DisplayName { get; set; }
    }

    public class UserService
    {
        private readonly Connection _connection;

        public UserService()
        {
            _connection = new Connection();
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var users = new List<User>();
            
            try
            {
                string query = "SELECT Id, Username, Password, CreatedDate, LastLoginDate, IsGoogleUser, DisplayName FROM [User]";
                var dataTable = await _connection.ExecuteQueryAsync(query, CommandType.Text);
                
                foreach (DataRow row in dataTable.Rows)
                {
                    users.Add(new User
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        Username = row["Username"].ToString() ?? string.Empty,
                        Password = row["Password"].ToString() ?? string.Empty,
                        CreatedDate = Convert.ToDateTime(row["CreatedDate"]),
                        LastLoginDate = row["LastLoginDate"] != DBNull.Value 
                            ? Convert.ToDateTime(row["LastLoginDate"]) 
                            : null,
                        IsGoogleUser = row["IsGoogleUser"] != DBNull.Value && Convert.ToBoolean(row["IsGoogleUser"]),
                        DisplayName = row["DisplayName"] != DBNull.Value ? row["DisplayName"].ToString() : null
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting users: {ex.Message}");
                throw;
            }
            
            return users;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            try
            {
                string query = "SELECT Id, Username, Password, CreatedDate, LastLoginDate, IsGoogleUser, DisplayName FROM [User] WHERE Id = @Id";
                var parameters = new[]
                {
                    _connection.CreateParameter("@Id", id)
                };
                
                var dataTable = await _connection.ExecuteQueryAsync(query, CommandType.Text, parameters);
                
                if (dataTable.Rows.Count > 0)
                {
                    var row = dataTable.Rows[0];
                    return new User
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        Username = row["Username"].ToString() ?? string.Empty,
                        Password = row["Password"].ToString() ?? string.Empty,
                        CreatedDate = Convert.ToDateTime(row["CreatedDate"]),
                        LastLoginDate = row["LastLoginDate"] != DBNull.Value 
                            ? Convert.ToDateTime(row["LastLoginDate"]) 
                            : null,
                        IsGoogleUser = row["IsGoogleUser"] != DBNull.Value && Convert.ToBoolean(row["IsGoogleUser"]),
                        DisplayName = row["DisplayName"] != DBNull.Value ? row["DisplayName"].ToString() : null
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting user by ID: {ex.Message}");
                throw;
            }
            
            return null;
        }

        public async Task<bool> AddUserAsync(User user)
        {
            try
            {
                // Hash the password before storing
                string hashedPassword = PasswordHashService.HashPassword(user.Password);
                
                string query = @"
                    INSERT INTO [User] (Username, Password, CreatedDate, IsGoogleUser, DisplayName)
                    VALUES (@Username, @Password, @CreatedDate, @IsGoogleUser, @DisplayName);
                ";
                
                var parameters = new[]
                {
                    _connection.CreateParameter("@Username", user.Username),
                    _connection.CreateParameter("@Password", hashedPassword),
                    _connection.CreateParameter("@CreatedDate", user.CreatedDate),
                    _connection.CreateParameter("@IsGoogleUser", user.IsGoogleUser),
                    _connection.CreateParameter("@DisplayName", (object?)user.DisplayName ?? DBNull.Value)
                };
                
                int rowsAffected = await _connection.ExecuteNonQueryAsync(query, CommandType.Text, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding user: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                // Only hash the password if it's not already hashed (not the placeholder)
                string passwordToUse = user.Password;
                if (user.Password != "••••••••" && !IsAlreadyHashed(user.Password))
                {
                    passwordToUse = PasswordHashService.HashPassword(user.Password);
                }
                
                string query = @"
                    UPDATE [User]
                    SET Username = @Username, 
                        Password = @Password,
                        IsGoogleUser = @IsGoogleUser,
                        DisplayName = @DisplayName
                    WHERE Id = @Id;
                ";
                
                var parameters = new[]
                {
                    _connection.CreateParameter("@Id", user.Id),
                    _connection.CreateParameter("@Username", user.Username),
                    _connection.CreateParameter("@Password", passwordToUse),
                    _connection.CreateParameter("@IsGoogleUser", user.IsGoogleUser),
                    _connection.CreateParameter("@DisplayName", (object?)user.DisplayName ?? DBNull.Value)
                };
                
                int rowsAffected = await _connection.ExecuteNonQueryAsync(query, CommandType.Text, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating user: {ex.Message}");
                return false;
            }
        }

        // Helper method to check if a password is already hashed
        private bool IsAlreadyHashed(string password)
        {
            // SHA-256 hash is 64 characters long and contains only hex characters
            if (password.Length != 64)
                return false;

            foreach (char c in password)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
                    return false;
            }
            
            return true;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            try
            {
                // First check if user has any sales records
                string checkQuery = "SELECT COUNT(*) FROM Sales WHERE UserId = @Id";
                var checkParameters = new[]
                {
                    _connection.CreateParameter("@Id", id)
                };
                
                var result = await _connection.ExecuteScalarAsync(checkQuery, CommandType.Text, checkParameters);
                int salesCount = Convert.ToInt32(result);
                
                if (salesCount > 0)
                {
                    // User has sales records, cannot delete
                    return false;
                }
                
                // No sales records, proceed with deletion
                string deleteQuery = "DELETE FROM [User] WHERE Id = @Id";
                var deleteParameters = new[]
                {
                    _connection.CreateParameter("@Id", id)
                };
                
                int rowsAffected = await _connection.ExecuteNonQueryAsync(deleteQuery, CommandType.Text, deleteParameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting user: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateLastLoginAsync(int userId)
        {
            try
            {
                string query = @"
                    UPDATE [User]
                    SET LastLoginDate = GETDATE()
                    WHERE Id = @Id;
                ";
                
                var parameters = new[]
                {
                    _connection.CreateParameter("@Id", userId)
                };
                
                int rowsAffected = await _connection.ExecuteNonQueryAsync(query, CommandType.Text, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating last login: {ex.Message}");
                return false;
            }
        }
        
        public async Task<bool> ValidateUserCredentialsAsync(string username, string password)
        {
            try
            {
                string query = "SELECT Password FROM [User] WHERE Username = @Username";
                var parameters = new[]
                {
                    _connection.CreateParameter("@Username", username)
                };
                
                var result = await _connection.ExecuteScalarAsync(query, CommandType.Text, parameters);
                
                if (result == null || result == DBNull.Value)
                    return false;
                
                string storedHash = result.ToString() ?? string.Empty;
                return PasswordHashService.VerifyPassword(password, storedHash);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating user credentials: {ex.Message}");
                return false;
            }
        }
    }
}