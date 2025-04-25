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
            try
            {
                return Task.Run(async () => await ValidateUserAsync(username, password)).Result;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to validate user", ex);
            }
        }

        public bool RegisterUser(string username, string password)
        {
            try
            {
                return Task.Run(async () => await RegisterUserAsync(username, password)).Result;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to register user", ex);
            }
        }

        public void UpdateLastLogin(string username)
        {
            try
            {
                Task.Run(async () => await UpdateLastLoginAsync(username)).Wait();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to update last login", ex);
            }
        }

        public bool UserExists(string username)
        {
            try
            {
                return Task.Run(async () => await UserExistsAsync(username)).Result;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to check if user exists", ex);
            }
        }

        // Add these methods to the UserAuthentication class
        
        public bool IsUserLoggedIn(string username)
        {
            try
            {
                return Task.Run(async () => await IsUserLoggedInAsync(username)).Result;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to check user login status", ex);
            }
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

                // Check if this is a Google user (for logging purposes)
                bool isGoogleUser = await IsGoogleUserAsync(username);

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

                // If on a platform that supports SecureStorage, clean up any stored tokens
                try
                {
                    // Remove Google tokens if they exist - Remove() returns bool, not Task<bool>
                    SecureStorage.Default.Remove("id_token");
                    SecureStorage.Default.Remove("access_token");
                }
                catch (Exception tokenEx)
                {
                    // Just log the exception but don't fail the logout process
                    Console.WriteLine($"Failed to remove tokens: {tokenEx.Message}");
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
            try
            {
                // Call the async method and wait for it to complete
                Task.Run(async () => await LogoutAsync(username)).Wait();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to logout user", ex);
            }
        }

        // Google Authentication Methods
        public async Task<bool> AddOrUpdateGoogleUserAsync(string email, string displayName)
        {
            try
            {
                // Check if the Google user already exists
                bool userExists = await IsGoogleUserAsync(email);
                
                if (userExists)
                {
                    // Update the existing Google user
                    string updateQuery = @"
                        UPDATE [User]
                        SET DisplayName = @DisplayName, 
                            LastLoginDate = GETDATE(),
                            IsGoogleUser = 1
                        WHERE Username = @Email AND IsGoogleUser = 1";
                    
                    var updateParameters = new[]
                    {
                        _connection.CreateParameter("@Email", email),
                        _connection.CreateParameter("@DisplayName", displayName)
                    };

                    int updateResult = await _connection.ExecuteNonQueryAsync(updateQuery, CommandType.Text, updateParameters);
                    return updateResult > 0;
                }
                else
                {
                    // Create a new Google user
                    // The password is not used for Google authentication, but we set a random one
                    string randomPassword = Guid.NewGuid().ToString();
                    string hashedPassword = PasswordHashService.HashPassword(randomPassword);
                    
                    string insertQuery = @"
                        INSERT INTO [User] (Username, Password, DisplayName, IsGoogleUser, LastLoginDate)
                        VALUES (@Email, @Password, @DisplayName, 1, GETDATE())";
                    
                    var insertParameters = new[]
                    {
                        _connection.CreateParameter("@Email", email),
                        _connection.CreateParameter("@Password", hashedPassword),
                        _connection.CreateParameter("@DisplayName", displayName)
                    };

                    int insertResult = await _connection.ExecuteNonQueryAsync(insertQuery, CommandType.Text, insertParameters);
                    return insertResult > 0;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to add or update Google user", ex);
            }
        }

        public async Task<bool> IsGoogleUserAsync(string email)
        {
            try
            {
                string query = "SELECT COUNT(*) FROM [User] WHERE Username = @Email AND IsGoogleUser = 1";
                
                var parameters = new[]
                {
                    _connection.CreateParameter("@Email", email)
                };

                var result = await _connection.ExecuteScalarAsync(query, CommandType.Text, parameters);
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to check if Google user exists", ex);
            }
        }
    }
}
