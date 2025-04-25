using System.Threading.Tasks;
using TrackPointV.Service;
using TrackPointV.View;
using TrackPointV.View.DBView;

namespace TrackPointV
{
    public partial class MainPage : ContentPage
    {
        private readonly GoogleAuthWindowsService _googleAuthService;

        public MainPage(GoogleAuthWindowsService googleAuthService = null)
        {
            InitializeComponent();
            _googleAuthService = googleAuthService;
            
            registerLabel.Loaded += (s, e) =>
            {
                registerLabel.ScaleTo(1.1, 300)
                    .ContinueWith((task) => registerLabel.ScaleTo(1.0, 300));
            };
        }

        private readonly IUserAuthentication _userAuth = new UserAuthentication();

        private async void OnLogin_Clicked(object sender, EventArgs e)
        {
            try
            {
                // Show loading indicator and disable login button
                loginActivityIndicator.IsVisible = true;
                loginActivityIndicator.IsRunning = true;
                OnLogin.IsEnabled = false;
                
                if (string.IsNullOrWhiteSpace(usernameEntry.Text) || string.IsNullOrWhiteSpace(passwordEntry.Text))
                {
                    await DisplayAlert("Error", "Please enter both username and password", "OK");
                    
                    // Hide loading indicator and re-enable login button
                    loginActivityIndicator.IsVisible = false;
                    loginActivityIndicator.IsRunning = false;
                    OnLogin.IsEnabled = true;
                    return;
                }

                bool isValid = await _userAuth.ValidateUserAsync(usernameEntry.Text, passwordEntry.Text);
                if (isValid)
                {
                    // Store username in preferences
                    Preferences.Set("CurrentUser", usernameEntry.Text);
                    
                    await _userAuth.UpdateLastLoginAsync(usernameEntry.Text);
                    
                    // Navigate to main app page
                    await Shell.Current.GoToAsync("//DashboardPage");
                    
                    // Note: We don't need to hide the indicator here as we're navigating away
                }
                else
                {
                    await DisplayAlert("Error", "Invalid username or password", "OK");
                    
                    // Hide loading indicator and re-enable login button
                    loginActivityIndicator.IsVisible = false;
                    loginActivityIndicator.IsRunning = false;
                    OnLogin.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Login failed: " + ex.Message, "OK");
                
                // Hide loading indicator and re-enable login button
                loginActivityIndicator.IsVisible = false;
                loginActivityIndicator.IsRunning = false;
                OnLogin.IsEnabled = true;
            }
        }
        private void onPassword_Tapped(object sender, TappedEventArgs e)
        {

        }

        private async void OnRegister_Tapped(object sender, TappedEventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(RegisterPage));  // Changed to use absolute path
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Navigation failed: " + ex.Message, "OK");
            }
        }

        private bool _passwordVisible = false;

        private void OnPasswordVisibilityToggle_Clicked(object sender, EventArgs e)
        {
            _passwordVisible = !_passwordVisible;

            // Toggle the password visibility
            passwordEntry.IsPassword = !_passwordVisible;

            // Change the icon based on visibility state
            if (_passwordVisible)
            {
                passwordVisibilityIcon.Glyph = "\uf070"; // Eye slash icon (hidden)
            }
            else
            {
                passwordVisibilityIcon.Glyph = "\uf06e"; // Eye icon (visible)
            }
        }

        private async void OnGoogleSignIn_Clicked(object sender, EventArgs e)
        {
            if (_googleAuthService == null)
            {
                await DisplayAlert("Error", "Google Sign-In service is not available.", "OK");
                return;
            }

            try
            {
                // Show loading state
                googleSignInButton.IsEnabled = false;
                loginActivityIndicator.IsVisible = true;
                loginActivityIndicator.IsRunning = true;
                
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
                googleSignInButton.IsEnabled = true;
                loginActivityIndicator.IsVisible = false;
                loginActivityIndicator.IsRunning = false;
            }
        }
    }
}
