using System;
using System.Diagnostics;
using TrackPointV.Service;

namespace TrackPointV.View.DBView.CrudView
{
    public partial class UserDetailPage : ContentPage
    {
        private readonly UserService _userService;
        private User? _user;
        private bool _isEditMode;
        private bool _isPasswordVisible = false;
        private bool _isConfirmPasswordVisible = false;
        private bool _isGoogleUser = false;

        // Constructor for Add mode
        public UserDetailPage()
        {
            InitializeComponent();
            _userService = new UserService();
            _isEditMode = false;
            
            // Set up UI for Add mode
            headerLabel.Text = "ADD NEW USER";
            subHeaderLabel.Text = "Create a new user account";
            saveButton.Text = "Create User";
            
            // Hide read-only fields in Add mode
            createdDateLabel.IsVisible = false;
            createdDateEntry.IsVisible = false;
            lastLoginDateLabel.IsVisible = false;
            lastLoginDateEntry.IsVisible = false;
            
            // Show display name field in Add mode for potential Google users
            displayNameLabel.IsVisible = true;
            displayNameEntry.IsVisible = true;
        }

        // Constructor for Edit mode
        public UserDetailPage(User user)
        {
            InitializeComponent();
            _userService = new UserService();
            _user = user;
            _isEditMode = true;
            _isGoogleUser = user.IsGoogleUser;
            
            // Set up UI for Edit mode
            headerLabel.Text = "EDIT USER";
            subHeaderLabel.Text = $"Modify user account: {user.Username}";
            saveButton.Text = "Update User";
            
            // Populate fields with user data
            usernameEntry.Text = user.Username;
            
            // For Google users, show appropriate UI
            if (user.IsGoogleUser)
            {
                // Show Google indicator and display name
                googleUserIndicator.IsVisible = true;
                displayNameLabel.IsVisible = true;
                displayNameEntry.IsVisible = true;
                displayNameEntry.Text = user.DisplayName;
                
                // Hide password fields for Google users - they use Google auth
                passwordLabel.IsVisible = false;
                passwordEntry.IsVisible = false;
                confirmPasswordEntry.IsVisible = false;
                confirmPasswordLabel.IsVisible = false;
            }
            else
            {
                // For regular users, handle password fields
                passwordEntry.Text = ""; // Placeholder, we don't show actual password
                confirmPasswordEntry.Text = ""; // Placeholder
            }
            
            // Show read-only fields in Edit mode
            createdDateLabel.IsVisible = true;
            createdDateEntry.IsVisible = true;
            lastLoginDateLabel.IsVisible = true;
            lastLoginDateEntry.IsVisible = true;
            
            // Set read-only field values
            createdDateEntry.Text = user.CreatedDate.ToString("MMM dd, yyyy h:mm tt");
            lastLoginDateEntry.Text = user.LastLoginDate.HasValue 
                ? user.LastLoginDate.Value.ToString("MMM dd, yyyy h:mm tt")
                : "Never logged in";
        }

        private void TogglePassword_Clicked(object sender, EventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;
            passwordEntry.IsPassword = !_isPasswordVisible;
            
            // Change the eye icon based on visibility state
            if (sender is Button button)
            {
                button.Text = _isPasswordVisible ? "\uf070" : "\uf06e"; // eye-slash : eye
            }
        }

        private void ToggleConfirmPassword_Clicked(object sender, EventArgs e)
        {
            _isConfirmPasswordVisible = !_isConfirmPasswordVisible;
            confirmPasswordEntry.IsPassword = !_isConfirmPasswordVisible;
            
            // Change the eye icon based on visibility state
            if (sender is Button button)
            {
                button.Text = _isConfirmPasswordVisible ? "\uf070" : "\uf06e"; // eye-slash : eye
            }
        }

        private async void SaveButton_Clicked(object sender, EventArgs e)
        {
            if (!ValidateForm())
                return;

            try
            {
                saveButton.IsEnabled = false;
                
                if (_isEditMode && _user != null)
                {
                    // Update existing user
                    _user.Username = usernameEntry.Text.Trim();
                    
                    // Handle Google user fields
                    if (_user.IsGoogleUser)
                    {
                        _user.DisplayName = displayNameEntry.Text?.Trim();
                    }
                    // Only update password for regular users if it's been changed
                    else if (passwordEntry.Text != "")
                    {
                        _user.Password = passwordEntry.Text;
                    }
                    
                    bool success = await _userService.UpdateUserAsync(_user);
                    if (success)
                    {
                        await DisplayAlert("Success", "User updated successfully.", "OK");
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        validationLabel.Text = "Failed to update user. Please try again.";
                        validationLabel.IsVisible = true;
                    }
                }
                else
                {
                    // Create new user
                    var newUser = new User
                    {
                        Username = usernameEntry.Text.Trim(),
                        Password = passwordEntry.Text,
                        CreatedDate = DateTime.Now,
                        DisplayName = displayNameEntry.Text?.Trim(),
                        IsGoogleUser = false // New users created in UI are not Google users
                    };
                    
                    bool success = await _userService.AddUserAsync(newUser);
                    if (success)
                    {
                        await DisplayAlert("Success", "User created successfully.", "OK");
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        validationLabel.Text = "Failed to create user. Username may already exist.";
                        validationLabel.IsVisible = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving user: {ex.Message}");
                validationLabel.Text = "An error occurred. Please try again.";
                validationLabel.IsVisible = true;
            }
            finally
            {
                saveButton.IsEnabled = true;
            }
        }

        private void CancelButton_Clicked(object sender, EventArgs e)
        {
            Navigation.PopAsync();
        }

        private bool ValidateForm()
        {
            // Reset validation message
            validationLabel.IsVisible = false;
            
            // Validate username
            if (string.IsNullOrWhiteSpace(usernameEntry.Text))
            {
                validationLabel.Text = "Username is required.";
                validationLabel.IsVisible = true;
                return false;
            }
            
            // Skip password validation for Google users
            if (_isGoogleUser)
            {
                return true;
            }
            
            // In add mode, validate password
            if (!_isEditMode)
            {
                // Validate password
                if (string.IsNullOrWhiteSpace(passwordEntry.Text))
                {
                    validationLabel.Text = "Password is required.";
                    validationLabel.IsVisible = true;
                    return false;
                }
                
                // Validate password length
                if (passwordEntry.Text.Length < 6)
                {
                    validationLabel.Text = "Password must be at least 6 characters.";
                    validationLabel.IsVisible = true;
                    return false;
                }
                
                // Validate password match
                if (passwordEntry.Text != confirmPasswordEntry.Text)
                {
                    validationLabel.Text = "Passwords do not match.";
                    validationLabel.IsVisible = true;
                    return false;
                }
            }
            else if (passwordEntry.Text != "")
            {
                // In edit mode, only validate password if it's been changed
                
                // Validate password length
                if (passwordEntry.Text.Length < 6)
                {
                    validationLabel.Text = "Password must be at least 6 characters.";
                    validationLabel.IsVisible = true;
                    return false;
                }
                
                // Validate password match
                if (passwordEntry.Text != confirmPasswordEntry.Text)
                {
                    validationLabel.Text = "Passwords do not match.";
                    validationLabel.IsVisible = true;
                    return false;
                }
            }
            
            return true;
        }
    }
}