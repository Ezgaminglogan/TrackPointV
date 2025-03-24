using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using TrackPointV.Service;
using TrackPointV.View.DBView.CrudView;

namespace TrackPointV.View.DBView
{
    public partial class UserPage : ContentPage
    {
        private readonly UserService _userService;
        private ObservableCollection<User> _users;
        private string _searchText = string.Empty;

        public UserPage()
        {
            InitializeComponent();
            _userService = new UserService();
            _users = new ObservableCollection<User>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadUsersAsync();
            await UpdateStatisticsAsync();
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                
                _users.Clear();
                foreach (var user in users)
                {
                    if (string.IsNullOrEmpty(_searchText) || 
                        user.Username.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                    {
                        _users.Add(user);
                    }
                }

                usersCollection.ItemsSource = _users;
                noUsersLabel.IsVisible = _users.Count == 0;
                
                if (refreshView.IsRefreshing)
                    refreshView.IsRefreshing = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading users: {ex.Message}");
                await DisplayAlert("Error", "Failed to load users. Please try again.", "OK");
                
                if (refreshView.IsRefreshing)
                    refreshView.IsRefreshing = false;
            }
        }

        private async Task UpdateStatisticsAsync()
        {
            try
            {
                var allUsers = await _userService.GetAllUsersAsync();
                totalUsersLabel.Text = allUsers.Count.ToString();

                // Count users who logged in today
                var today = DateTime.Today;
                int activeToday = allUsers.Count(u => 
                    u.LastLoginDate.HasValue && 
                    u.LastLoginDate.Value.Date == today);
                
                activeUsersLabel.Text = activeToday.ToString();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating statistics: {ex.Message}");
            }
        }

        private async void refreshView_Refreshing(object sender, EventArgs e)
        {
            await LoadUsersAsync();
            await UpdateStatisticsAsync();
        }

        private async void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = e.NewTextValue ?? string.Empty;
            await LoadUsersAsync();
        }

        private void FilterButton_Clicked(object sender, EventArgs e)
        {
            // Implement filter functionality (e.g., show a popup with filter options)
            DisplayAlert("Filter", "Filter functionality will be implemented here", "OK");
        }

        private async void AddUserButton_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new UserDetailPage());
        }

        private async void EditUser_Clicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is int userId)
            {
                var user = await _userService.GetUserByIdAsync(userId);
                if (user != null)
                {
                    await Navigation.PushAsync(new UserDetailPage(user));
                }
            }
        }

        private async void DeleteUser_Clicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is int userId)
            {
                bool confirm = await DisplayAlert("Confirm Delete", 
                    "Are you sure you want to delete this user? This action cannot be undone.", 
                    "Delete", "Cancel");
                
                if (confirm)
                {
                    try
                    {
                        bool success = await _userService.DeleteUserAsync(userId);
                        if (success)
                        {
                            await LoadUsersAsync();
                            await UpdateStatisticsAsync();
                        }
                        else
                        {
                            await DisplayAlert("Error", "Failed to delete user. The user may have associated records.", "OK");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error deleting user: {ex.Message}");
                        await DisplayAlert("Error", "An error occurred while deleting the user.", "OK");
                    }
                }
            }
        }

        private void UsersCollection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Reset selection
            usersCollection.SelectedItem = null;
        }
    }
}