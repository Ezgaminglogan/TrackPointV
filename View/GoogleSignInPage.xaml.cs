using System.Web;
using TrackPointV.Service;

#if WINDOWS
using Microsoft.Web.WebView2.Core;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Maui.Platform;
#endif

namespace TrackPointV.View
{
    public partial class GoogleSignInPage : ContentPage
    {
        private readonly GoogleAuthWindowsService _authService;
        private readonly string _expectedState;
        private readonly TaskCompletionSource<GoogleUser> _authCompletionSource;
        private readonly string _authUrl;
#if WINDOWS
        private WebView2 _webView;
        private Microsoft.Maui.Controls.WebView _mauiWebView;
#endif

        public GoogleSignInPage(GoogleAuthWindowsService authService)
        {
            InitializeComponent();
            _authService = authService;
            _authCompletionSource = new TaskCompletionSource<GoogleUser>();

            var (authUrl, state) = _authService.GetAuthorizationUrl();
            _authUrl = authUrl;
            _expectedState = state;

            Loaded += OnPageLoaded;
        }

        private void OnPageLoaded(object sender, EventArgs e)
        {
            try
            {
#if WINDOWS
                // For Windows, use the MAUI WebView control instead
                _mauiWebView = new Microsoft.Maui.Controls.WebView
                {
                    Source = new UrlWebViewSource
                    {
                        Url = _authUrl
                    }
                };

                // Set the MAUI WebView as the content of the container
                webViewContainer.Content = _mauiWebView;

                // Hook up to the MAUI WebView's navigation events
                _mauiWebView.Navigated += OnMauiWebViewNavigated;
                _mauiWebView.Navigating += OnMauiWebViewNavigating;

                // Show the loading indicator when WebView is initializing
                loadingIndicator.IsVisible = true;
#else
                // For non-Windows platforms, show an error message
                DisplayAlert("Not Supported", "Google Sign-In with WebView is only supported on Windows.", "OK")
                    .ContinueWith(_ => Navigation.PopModalAsync());
#endif
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"Failed to initialize WebView: {ex.Message}", "OK")
                    .ContinueWith(_ => Navigation.PopModalAsync());
            }
        }

#if WINDOWS
        private void OnMauiWebViewNavigated(object sender, WebNavigatedEventArgs e)
        {
            // Hide loading indicator when navigation completes
            MainThread.BeginInvokeOnMainThread(() =>
            {
                loadingIndicator.IsVisible = false;
                loadingIndicator.IsRunning = false;
            });
        }

        private async void OnMauiWebViewNavigating(object sender, WebNavigatingEventArgs e)
        {
            // Handle redirect to localhost
            if (e.Url.StartsWith("http://localhost"))
            {
                e.Cancel = true;

                var uri = new Uri(e.Url);
                var queryParams = HttpUtility.ParseQueryString(uri.Query);

                var authCode = queryParams["code"];
                var state = queryParams["state"];

                // Create a flag to track if we've already completed the navigation
                bool isNavigating = true;

                if (string.IsNullOrEmpty(authCode))
                {
                    try
                    {
                        _authCompletionSource.TrySetException(new Exception("Authorization code not received"));
                    }
                    catch (Exception) { /* Ignore if already set */ }
                    
                    if (isNavigating)
                    {
                        isNavigating = false;
                        await Navigation.PopModalAsync();
                    }
                    return;
                }

                if (state != _expectedState)
                {
                    var errorMessage = "Authentication session expired or invalid. Please try again.";
                    await DisplayAlert("Authentication Error", errorMessage, "OK");
                    
                    try
                    {
                        _authCompletionSource.TrySetException(new Exception("State mismatch, possible CSRF attack"));
                    }
                    catch (Exception) { /* Ignore if already set */ }
                    
                    if (isNavigating)
                    {
                        isNavigating = false;
                        await Navigation.PopModalAsync();
                    }
                    return;
                }

                try
                {
                    // Show loading indicator during token exchange
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        loadingIndicator.IsVisible = true;
                        loadingIndicator.IsRunning = true;
                    });

                    var (idToken, accessToken) = await _authService.ExchangeCodeForTokensAsync(authCode);
                    var user = _authService.ParseIdToken(idToken);

                    // Store tokens securely
                    await SecureStorage.Default.SetAsync("id_token", idToken);
                    await SecureStorage.Default.SetAsync("access_token", accessToken);

                    // Use TrySetResult to avoid exceptions if already set
                    _authCompletionSource.TrySetResult(user);
                    
                    if (isNavigating)
                    {
                        isNavigating = false;
                        await Navigation.PopModalAsync();
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        _authCompletionSource.TrySetException(ex);
                    }
                    catch (Exception) { /* Ignore if already set */ }
                    
                    if (isNavigating)
                    {
                        isNavigating = false;
                        await Navigation.PopModalAsync();
                    }
                }
            }
        }
#endif

        private async void OnCloseButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                _authCompletionSource.TrySetCanceled();
            }
            catch (Exception) { /* Ignore if already set */ }
            
            await Navigation.PopModalAsync();
        }

        public Task<GoogleUser> AuthenticationCompletedTask => _authCompletionSource.Task;
    }
}