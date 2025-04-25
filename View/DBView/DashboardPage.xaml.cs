using System;
using System.Timers;
using TrackPointV.Service;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;

namespace TrackPointV.View.DBView
{
    public partial class DashboardPage : ContentPage
    {
        private readonly IUserAuthentication _userAuth;
        private readonly InventoryService _inventoryService;
        private readonly SaleService _saleService;
        private readonly UserService _userService;

        private System.Timers.Timer? _timer;
        private string? _currentUsername;

        public DashboardPage(IUserAuthentication userAuth)
        {
            InitializeComponent();
            _userAuth = userAuth;
            _inventoryService = new InventoryService();
            _saleService = new SaleService();
            _userService = new UserService();


            // Initialize timer for updating date/time
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Start();

            // Ensure flyout is enabled
            Shell.SetFlyoutBehavior(Shell.Current, FlyoutBehavior.Flyout);

            // Set current date and time
            UpdateDateTime();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // Get current username from preferences
            _currentUsername = Preferences.Get("CurrentUser", "User");
            greetingLabel.Text = $"Welcome, {_currentUsername}!";

            // Update login information
            loginUserLabel.Text = $"Logged in User: {_currentUsername} by {_currentUsername}";
            loginTimeLabel.Text = DateTime.Now.ToString("MMM dd, yyyy h:mm tt");

            // Ensure flyout is enabled when page appears
            Shell.SetFlyoutBehavior(Shell.Current, FlyoutBehavior.Flyout);

            // Use the existing UpdateDashboardStatisticsAsync method
            await UpdateDashboardStatisticsAsync();
            
            // Load recent activities
            await LoadRecentActivitiesAsync();
        }

        private async void refreshView_Refreshing(object sender, EventArgs e)
        {
            // Reload dashboard data when pulled to refresh
            await Task.Delay(500); // Small delay for UX
            
            // Use UpdateDashboardStatisticsAsync instead of LoadDashboardData
            await UpdateDashboardStatisticsAsync();
            await LoadRecentActivitiesAsync();
            
            // End refreshing
            refreshView.IsRefreshing = false;
        }

        // Enhanced method to load recent activities from SQL Server with additional details
        private async Task LoadRecentActivitiesAsync()
        {
            try
            {
                // Get recent activities using your existing Connection class
                var recentActivities = await _inventoryService.GetRecentActivitiesAsync(10);
                
                // Debug information to see what's coming back
                Debug.WriteLine($"Loaded {recentActivities?.Count ?? 0} recent activities");
                
                // Add product quantity for sale activities if not already included
                foreach (var activity in recentActivities)
                {
                    if (activity.Type.Equals("Sale", StringComparison.OrdinalIgnoreCase) && 
                        !activity.Quantity.HasValue)
                    {
                        // Try to parse quantity from the display text if available
                        // This is a fallback if the DB doesn't store quantity directly
                        string text = activity.DisplayText;
                        if (text.Contains("qty:"))
                        {
                            int startIndex = text.IndexOf("qty:") + 4;
                            int endIndex = text.IndexOf(" ", startIndex);
                            if (endIndex > startIndex)
                            {
                                string qtyStr = text.Substring(startIndex, endIndex - startIndex);
                                if (int.TryParse(qtyStr, out int qty))
                                {
                                    activity.Quantity = qty;
                                }
                            }
                        }
                        else
                        {
                            // Default to 1 if we can't determine quantity
                            activity.Quantity = 1;
                        }
                    }
                }
                
                // Update UI on the main thread
                await MainThread.InvokeOnMainThreadAsync(() => {
                    if (recentActivityList != null)
                    {
                        // Set to null first to avoid binding issues
                        recentActivityList.ItemsSource = null;
                        
                        if (recentActivities != null && recentActivities.Count > 0)
                        {
                            // Set the data source for the ListView
                            recentActivityList.ItemsSource = recentActivities;
                            
                            if (noActivityLabel != null)
                                noActivityLabel.IsVisible = false;
                                
                            recentActivityList.IsVisible = true;
                        }
                        else
                        {
                            // Show "No recent activity" message
                            if (noActivityLabel != null)
                                noActivityLabel.IsVisible = true;
                                
                            recentActivityList.IsVisible = false;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading recent activities: {ex}");
                await DisplayAlert("Error", $"Failed to load recent activities: {ex.Message}", "OK");
            }
        }

        // Enhanced method to update dashboard statistics with more detailed information
        private async Task UpdateDashboardStatisticsAsync()
        {
            try
            {
                // Get product count and low stock information
                var products = await _inventoryService.GetAllProductsAsync();
                int productCount = products.Count;
                
                // Update the total inventory label with actual product count
                totalInventoryLabel.Text = productCount.ToString("N0");
                
                // Calculate low stock items (items with stock <= 10)
                int lowStockCount = products.Count(p => p.Stock <= 10);
                lowStockLabel.Text = lowStockCount.ToString("N0");
                
                // Calculate and update low stock percentage
                double lowStockPercentage = productCount > 0 ? (double)lowStockCount / productCount * 100 : 0;
                lowStockPercentLabel.Text = $"{lowStockPercentage:N1}% of inventory";
                
                // Get today's sales
                DateTime today = DateTime.Today;
                DateTime tomorrow = today.AddDays(1);
                var todaySales = await _saleService.GetSalesInRangeAsync(today, tomorrow);
                
                // Calculate today's sales total amount and count
                decimal totalSalesAmount = todaySales.Sum(s => s.TotalAmount);
                int salesCount = todaySales.Count;
                
                // Update sales today display
                salesTodayAmountLabel.Text = $"${totalSalesAmount:N2}";
                salesTodayCountLabel.Text = $"{salesCount} {(salesCount == 1 ? "sale" : "sales")}";
                
                // Get all sales for total revenue (matching SalesPage)
                var allSales = await _saleService.GetAllSalesAsync();
                decimal totalRevenue = allSales.Sum(s => s.TotalAmount);
                
                // Update total revenue label
                totalRevenueLabel.Text = $"Total: ${totalRevenue:N2}";
                
                // Get user information
                var users = await _userService.GetAllUsersAsync();
                int userCount = users.Count;
                
                // Update user count display
                userCountLabel.Text = userCount.ToString("N0");
                
                // Find the most recent login
                var lastLogin = users
                    .Where(u => u.LastLoginDate.HasValue)
                    .OrderByDescending(u => u.LastLoginDate)
                    .FirstOrDefault();
                
                if (lastLogin != null && lastLogin.LastLoginDate.HasValue)
                {
                    // Format the last login display
                    DateTime lastLoginDate = lastLogin.LastLoginDate.Value;
                    if (lastLoginDate.Date == DateTime.Today)
                    {
                        lastLoginLabel.Text = $"Last login today at {lastLoginDate.ToString("h:mm tt")}";
                    }
                    else if (lastLoginDate.Date == DateTime.Today.AddDays(-1))
                    {
                        lastLoginLabel.Text = "Last login yesterday";
                    }
                    else
                    {
                        lastLoginLabel.Text = $"Last login on {lastLoginDate.ToString("MMM d")}";
                    }
                }
                else
                {
                    lastLoginLabel.Text = "No recent logins";
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load dashboard statistics: {ex.Message}", "OK");
                Debug.WriteLine($"Error loading dashboard statistics: {ex}");
            }
        }

        private void UpdateDateTime()
        {
            // Update the datetime on UI thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                DateTime now = DateTime.Now;
                dateTimeLabel.Text = now.ToString("dddd, MMMM d, yyyy - h:mm:ss tt");
            });
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            UpdateDateTime();
        }

        private async void btnLogout_Clicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");

            if (confirm)
            {
                try
                {
                    // Disable the logout button to prevent multiple clicks
                    if (btnLogout != null)
                    {
                        btnLogout.IsEnabled = false;
                    }
                    
                    if (!string.IsNullOrEmpty(_currentUsername))
                    {
                        // Store username before logout for navigation
                        string username = _currentUsername;
                        
                        // Check if this was a Google login
                        bool wasGoogleLogin = Preferences.Get("UserLogin", "") == "Google";
                        
                        // Call logout method and Clear Sessions
                        await _userAuth.LogoutAsync(username);
                        
                        // Clear all preference data
                        Preferences.Remove("CurrentUser");
                        Preferences.Remove("UserDisplayName");
                        Preferences.Remove("UserLogin");
                        Preferences.Clear();
                        
                        // Clear secure storage (for Google tokens)
                        try 
                        {
                            SecureStorage.Default.Remove("id_token");
                            SecureStorage.Default.Remove("access_token");
                        }
                        catch (Exception) 
                        {
                            // Ignore exceptions from SecureStorage - already handled in UserAuthentication
                        }

                        // Disable flyout before navigation
                        Shell.SetFlyoutBehavior(Shell.Current, FlyoutBehavior.Disabled);

                        // Navigate back to login page with absolute path and clear navigation stack
                        await Shell.Current.GoToAsync("//MainPage", true);
                        
                        // Replace the current page with a new AppShell using the recommended approach
                        if (Microsoft.Maui.Controls.Application.Current != null && Microsoft.Maui.Controls.Application.Current.Windows.Count > 0)
                        {
                            Microsoft.Maui.Controls.Application.Current.Windows[0].Page = new AppShell();
                        }
                    }
                    else
                    {
                        // Clear all preferences anyway to be safe
                        Preferences.Clear();
                        
                        // Try to clear secure storage too
                        try 
                        {
                            SecureStorage.Default.Remove("id_token");
                            SecureStorage.Default.Remove("access_token");
                        }
                        catch (Exception) 
                        {
                            // Ignore exceptions from SecureStorage
                        }
                        
                        // Disable flyout before navigation
                        Shell.SetFlyoutBehavior(Shell.Current, FlyoutBehavior.Disabled);

                        await Shell.Current.GoToAsync("//MainPage", true);
                        
                        // Replace the current page with a new AppShell using the recommended approach
                        if (Microsoft.Maui.Controls.Application.Current != null && Microsoft.Maui.Controls.Application.Current.Windows.Count > 0)
                        {
                            Microsoft.Maui.Controls.Application.Current.Windows[0].Page = new AppShell();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Make sure refreshView is not refreshing in case of error
                    if (refreshView != null)
                    {
                        refreshView.IsRefreshing = false;
                    }
                    
                    // Re-enable the logout button in case of error
                    if (btnLogout != null)
                    {
                        btnLogout.IsEnabled = true;
                    }
                    
                    // Show the error message
                    await DisplayAlert("Error", $"Logout failed: {ex.Message}", "OK");
                    
                    // Log the full exception details
                    Console.WriteLine($"Logout error: {ex}");
                }
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Stop timer when page is not visible
            _timer?.Stop();
        }
    }
}