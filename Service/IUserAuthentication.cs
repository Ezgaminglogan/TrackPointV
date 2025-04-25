namespace TrackPointV.Service
{
    public interface IUserAuthentication
    {
        bool ValidateUser(string username, string password);
        bool RegisterUser(string username, string password);
        void UpdateLastLogin(string username);
        bool UserExists(string username);

        // Async versions
        Task<bool> ValidateUserAsync(string username, string password);
        Task<bool> RegisterUserAsync(string username, string password);
        Task UpdateLastLoginAsync(string username);
        Task<bool> UserExistsAsync(string username);

        // Session management
        bool IsUserLoggedIn(string username);
        Task<bool> IsUserLoggedInAsync(string username);
        void Login(string username);
        Task LogoutAsync(string username);
        void Logout(string username);
        
        // Google Authentication
        Task<bool> AddOrUpdateGoogleUserAsync(string email, string displayName);
        Task<bool> IsGoogleUserAsync(string email);
    }
}
