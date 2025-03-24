using TrackPointV.Service;

namespace TrackPointV.View;

public partial class RegisterPage : ContentPage
{

    private readonly IUserAuthentication _userAuth = new UserAuthentication();
    private bool _passwordVisible = false;
    private bool _confirmPasswordVisible = false;

    public RegisterPage()
	{
		InitializeComponent();
        loginLabel.Loaded += (s, e) =>
        {
            loginLabel.ScaleTo(1.1, 300)
                .ContinueWith((task) => loginLabel.ScaleTo(1.0, 300));
        };
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

}