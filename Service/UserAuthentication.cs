using Microsoft.Data.SqlClient;
using System.Data;

namespace TrackPointV.Service
{
    public class UserAuthentication : IUserAuthentication
    {
        private readonly Connection _connection;

        public UserAuthentication()
        {
            _connection = new Connection();
        }

        public async Task<bool> ValidateUserAsync(string username, string password)
        {
            string hashedPassword = PasswordHashService.HashPassword(password);
            string query = "SELECT COUNT(*) FROM [User] WHERE Username = @Username AND Password = @Password";
            
            var parameters = new[]
            {
                _connection.CreateParameter("@Username", username),
                _connection.CreateParameter("@Password", hashedPassword)
            };

            try
            {
                var result = await _connection.ExecuteScalarAsync(query, CommandType.Text, parameters);
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to validate user", ex);
            }
        }

        public async Task<bool> RegisterUserAsync(string username, string password)
        {
            try
            {
                if (await UserExistsAsync(username))
                    return false;

                string hashedPassword = PasswordHashService.HashPassword(password);
                string query = "INSERT INTO [User] (Username, Password) VALUES (@Username, @Password)";
                
                var parameters = new[]
                {
                    _connection.CreateParameter("@Username", username),
                    _connection.CreateParameter("@Password", hashedPassword)
                };

                int result = await _connection.ExecuteNonQueryAsync(query, CommandType.Text, parameters);
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to register user", ex);
            }
        }

        public async Task UpdateLastLoginAsync(string username)
        {
            try
            {
                string query = "UPDATE [User] SET LastLoginDate = GETDATE() WHERE Username = @Username";
                
                var parameters = new[]
                {
                    _connection.CreateParameter("@Username", username)
                };

                await _connection.ExecuteNonQueryAsync(query, CommandType.Text, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to update last login", ex);
            }
        }

        public async Task<bool> UserExistsAsync(string username)
        {
            try
            {
                string query = "SELECT COUNT(*) FROM [User] WHERE Username = @Username";
                
                var parameters = new[]
                {
                    _connection.CreateParameter("@Username", username)
                };

                var result = await _connection.ExecuteScalarAsync(query, CommandType.Text, parameters);
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to check if user exists", ex);
            }
        }

        // Optional: Keep sync methods that internally use async methods
        public bool ValidateUser(string username, string password)
        {
            return ValidateUserAsync(username, password).GetAwaiter().GetResult();
        }

        public bool RegisterUser(string username, string password)
        {
            return RegisterUserAsync(username, password).GetAwaiter().GetResult();
        }

        public void UpdateLastLogin(string username)
        {
            UpdateLastLoginAsync(username).GetAwaiter().GetResult();
        }

        public bool UserExists(string username)
        {
            return UserExistsAsync(username).GetAwaiter().GetResult();
        }

        // Add these methods to the UserAuthentication class
        
        public bool IsUserLoggedIn(string username)
        {
            return IsUserLoggedInAsync(username).GetAwaiter().GetResult();
        }
        
        public async Task<bool> IsUserLoggedInAsync(string username)
        {
            try
            {
                string query = @"SELECT COUNT(*) FROM [User] 
                                WHERE Username = @Username 
                                AND LastLoginDate IS NOT NULL 
                                AND (LastLogoutDate IS NULL OR LastLoginDate > LastLogoutDate)";
                
                var parameters = new[]
                {
                    _connection.CreateParameter("@Username", username)
                };

                // Change this line
                var result = await _connection.ExecuteScalarAsync(query, CommandType.Text, parameters);
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to check user login status", ex);
            }
        }
        
        public void Login(string username)
        {
            try
            {
                string query = "UPDATE [User] SET LastLoginDate = GETDATE() WHERE Username = @Username";
                
                var parameters = new[]
                {
                    _connection.CreateParameter("@Username", username)
                };
        
                _connection.ExecuteNonQuery(query, CommandType.Text, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to update login status", ex);
            }
        }

        public async Task LogoutAsync(string username)
        {
            try
            {
                if (string.IsNullOrEmpty(username))
                {
                    throw new ArgumentException("Username cannot be empty or null");
                }

                // First check if the user exists
                bool userExists = await UserExistsAsync(username);
                if (!userExists)
                {
                    throw new Exception($"User '{username}' not found");
                }

                // Update the logout date
                string query = "UPDATE [User] SET LastLoginDate = GETDATE() WHERE Username = @Username";
                
                var parameters = new[]
                {
                    _connection.CreateParameter("@Username", username)
                };

                int rowsAffected = await _connection.ExecuteNonQueryAsync(query, CommandType.Text, parameters);
                
                // If no rows were affected, something went wrong
                if (rowsAffected == 0)
                {
                    throw new Exception($"Failed to update logout status for user '{username}'");
                }
            }
            catch (SqlException sqlEx)
            {
                // Log the specific SQL error
                Console.WriteLine($"SQL Error: {sqlEx.Message}");
                throw new Exception("Database error during logout", sqlEx);
            }
            catch (Exception ex)
            {
                // Rethrow with more context
                if (ex.Message.Contains("Failed to logout user"))
                {
                    throw; // Don't wrap the exception again if it's already wrapped
                }
                throw new Exception("Failed to logout user", ex);
            }
        }

        // Add the sync version for completeness
        public void Logout(string username)
        {
            LogoutAsync(username).GetAwaiter().GetResult();
        }
    }
}
