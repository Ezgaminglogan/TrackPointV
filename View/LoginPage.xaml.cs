using System.Threading.Tasks;
using TrackPointV.Service;
using TrackPointV.View;
using TrackPointV.View.DBView;
namespace TrackPointV
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
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
    }
}
