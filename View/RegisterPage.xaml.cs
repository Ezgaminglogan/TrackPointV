using TrackPointV.Service;

namespace TrackPointV.View;

public partial class RegisterPage : ContentPage
{

    private readonly IUserAuthentication _userAuth = new UserAuthentication();
    private bool _passwordVisible = false;
    private bool _confirmPasswordVisible = false;
    private readonly GoogleAuthWindowsService _googleAuthService;

    public RegisterPage()
	{
		InitializeComponent();
        loginLabel.Loaded += (s, e) =>
        {
            loginLabel.ScaleTo(1.1, 300)
                .ContinueWith((task) => loginLabel.ScaleTo(1.0, 300));
        };
        
        // Get the Google auth service from the DI container
        _googleAuthService = IPlatformApplication.Current?.Services.GetService<GoogleAuthWindowsService>();
    }
    private async void OnRegister_Clicked(object sender, EventArgs e)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(usernameEntry.Text))
            {
                await DisplayAlert("Error", "Please enter a username", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(passwordEntry.Text))
            {
                await DisplayAlert("Error", "Please enter a password", "OK");
                return;
            }

            if (passwordEntry.Text != confirmPasswordEntry.Text)
            {
                await DisplayAlert("Error", "Passwords do not match", "OK");
                return;
            }

            // Register the user
            bool success = await _userAuth.RegisterUserAsync(usernameEntry.Text, passwordEntry.Text);

            if (success)
            {
                await DisplayAlert("Success", "Account created successfully", "OK");
                // Navigate back to login page
                await Shell.Current.GoToAsync("//MainPage");
            }
            else
            {
                await DisplayAlert("Error", "Username already exists", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Registration failed: {ex.Message}", "OK");
        }
    }


    private async void OnLogin_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("//MainPage");
    }

    private void OnPasswordVisibilityToggle_Clicked(object sender, EventArgs e)
    {
        _passwordVisible = !_passwordVisible;
        passwordEntry.IsPassword = !_passwordVisible;

        if (_passwordVisible)
        {
            passwordVisibilityIcon.Glyph = "\uf070"; // Eye slash icon (hidden)
        }
        else
        {
            passwordVisibilityIcon.Glyph = "\uf06e"; // Eye icon (visible)
        }
    }

    private void OnConfirmPasswordVisibilityToggle_Clicked(object sender, EventArgs e)
    {
        _confirmPasswordVisible = !_confirmPasswordVisible;
        confirmPasswordEntry.IsPassword = !_confirmPasswordVisible;

        if (_confirmPasswordVisible)
        {
            confirmPasswordVisibilityIcon.Glyph = "\uf070"; // Eye slash icon (hidden)
        }
        else
        {
            confirmPasswordVisibilityIcon.Glyph = "\uf06e"; // Eye icon (visible)
        }
    }

    private async void OnGoogleSignUp_Clicked(object sender, EventArgs e)
    {
        if (_googleAuthService == null)
        {
            await DisplayAlert("Error", "Google Sign-Up service is not available.", "OK");
            return;
        }

        try
        {
            // Show loading state
            googleSignUpButton.IsEnabled = false;
            registerActivityIndicator.IsVisible = true;
            registerActivityIndicator.IsRunning = true;
            
            // Launch the Google Sign-in page
            var signInPage = new GoogleSignInPage(_googleAuthService);
            await Navigation.PushModalAsync(signInPage);
            
            // Wait for authentication to complete
            var user = await signInPage.AuthenticationCompletedTask;
            
            if (user != null)
            {
                try
                {
                    // Store user info
                    Preferences.Set("CurrentUser", user.Email);
                    Preferences.Set("UserDisplayName", user.Name);
                    Preferences.Set("UserLogin", "Google");
                    
                    // Add the Google user to the database or update if exists
                    await _userAuth.AddOrUpdateGoogleUserAsync(user.Email, user.Name);
                    
                    // Update last login timestamp
                    await _userAuth.UpdateLastLoginAsync(user.Email);
                    
                    // Navigate to main app
                    await Shell.Current.GoToAsync("//DashboardPage");
                }
                catch (Exception dbEx)
                {
                    await DisplayAlert("Database Error", 
                        "Successfully authenticated with Google, but failed to update user database: " + dbEx.Message, 
                        "OK");
                    
                    // We can still proceed to dashboard even if DB update fails
                    await Shell.Current.GoToAsync("//DashboardPage");
                }
            }
        }
        catch (TaskCanceledException)
        {
            // User canceled the sign-in process
            // Just reset the UI
        }
        catch (Exception ex)
        {
            await DisplayAlert("Authentication Error", ex.Message, "OK");
        }
        finally
        {
            // Reset UI state
            googleSignUpButton.IsEnabled = true;
            registerActivityIndicator.IsVisible = false;
            registerActivityIndicator.IsRunning = false;
        }
    }

}