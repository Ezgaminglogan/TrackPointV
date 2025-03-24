using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;
using TrackPointV.Service;
using TrackPointV.View.DBView.CrudView;

namespace TrackPointV.View.DBView
{
    public partial class SalePage : ContentPage
    {
        private readonly SaleService _saleService;
        private ObservableCollection<SaleViewModel> _sales;
        private bool _isInitialized = false;
        private string _searchText = string.Empty;
        private DateTime? _filterStartDate;
        private DateTime? _filterEndDate;
        private DateTime _startDate = DateTime.Now.AddDays(-7);
        private DateTime _endDate = DateTime.Now;

        public SalePage()
        {
            InitializeComponent();
            _saleService = new SaleService();
            _sales = new ObservableCollection<SaleViewModel>();
            salesCollection.ItemsSource = _sales;
            
            // Update date range label
            UpdateDateRangeLabel();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // Always refresh data when page appears
            await LoadSalesDataAsync();
            if (!_isInitialized)
            {
                _isInitialized = true;
            }
        }

        private async Task LoadSalesDataAsync()
        {
            try
            {
                // Show loading indicator
                IsBusy = true;
                
                // Load sales data
                var sales = await _saleService.GetAllSalesAsync();
                
                // Clear existing collection
                _sales.Clear();
                
                // Filter sales if needed
                var filteredSales = sales;
                if (!string.IsNullOrWhiteSpace(_searchText))
                {
                    filteredSales = sales.Where(s => 
                        s.Id.ToString().Contains(_searchText) || 
                        s.Username.Contains(_searchText, StringComparison.OrdinalIgnoreCase)).ToList();
                }
                
                if (_filterStartDate.HasValue)
                {
                    filteredSales = filteredSales.Where(s => s.SaleDate >= _filterStartDate.Value).ToList();
                }
                
                if (_filterEndDate.HasValue)
                {
                    filteredSales = filteredSales.Where(s => s.SaleDate <= _filterEndDate.Value).ToList();
                }
                
                // Add filtered sales to collection
                foreach (var sale in filteredSales)
                {
                    _sales.Add(new SaleViewModel(sale));
                }
                
                // Update statistics
                UpdateStatistics(sales);
                
                // Update chart
                await UpdateChartDataAsync();
                DrawSalesChart();
                
                // Fix: Remove reference to noSalesLabel which doesn't exist
                // Instead, we can use the EmptyView of the CollectionView which is already set in XAML
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load sales: {ex.Message}", "OK");
                Debug.WriteLine($"Error loading sales: {ex}");
            }
            finally
            {
                // Hide loading indicator
                IsBusy = false;
            }
        }

        private void UpdateStatistics(List<Sale> sales)
        {
            // Total sales count
            totalSalesLabel.Text = sales.Count.ToString();
            
            // Total revenue
            decimal totalRevenue = sales.Sum(s => s.TotalAmount);
            totalRevenueLabel.Text = $"${totalRevenue:N2}";
            
            // Today's sales
            var today = DateTime.Today;
            int todaySales = sales.Count(s => s.SaleDate.Date == today);
            todaySalesLabel.Text = todaySales.ToString();
        }

        private void UpdateDateRangeLabel()
        {
            // Format the date range for display
            dateRangeLabel.Text = $"{_startDate:MMM d, yyyy} - {_endDate:MMM d, yyyy}";
        }

        private void SetChartDateRange(int days)
        {
            _startDate = DateTime.Now.AddDays(-days);
            _endDate = DateTime.Now;
            UpdateDateRangeLabel();
        }

        private async Task UpdateChartDataAsync()
        {
            try
            {
                var chartData = new ChartData();
                
                // Get sales within the date range
                var salesInRange = _sales.Where(s => s.SaleDate >= _startDate && s.SaleDate <= _endDate)
                    .OrderBy(s => s.SaleDate)
                    .ToList();
                
                // Group by date
                var salesByDate = salesInRange
                    .GroupBy(s => s.SaleDate.Date)
                    .Select(g => new { 
                        Date = g.Key, 
                        Count = g.Count(), 
                        Revenue = (float)g.Sum(s => s.TotalAmount) 
                    })
                    .ToList();
                
                // Fill in missing dates
                for (var date = _startDate.Date; date <= _endDate.Date; date = date.AddDays(1))
                {
                    var existingSale = salesByDate.FirstOrDefault(s => s.Date == date);
                    
                    chartData.Dates.Add(date);
                    chartData.SalesCounts.Add(existingSale?.Count ?? 0);
                    chartData.Revenues.Add(existingSale?.Revenue ?? 0);
                }
                
                // Update chart
                salesChartView.Drawable = new SalesChart3DDrawable(chartData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating chart: {ex}");
            }
        }

        private void DrawSalesChart()
        {
            salesChartView.Invalidate();
        }

        private async void refreshView_Refreshing(object sender, EventArgs e)
        {
            await LoadSalesDataAsync();
            refreshView.IsRefreshing = false;
        }

        private async void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = e.NewTextValue;
            await LoadSalesDataAsync();
        }

        private async void FilterButton_Clicked(object sender, EventArgs e)
        {
            // Show date filter dialog
            string result = await DisplayActionSheet("Filter by Date", "Cancel", null, "Today", "Yesterday", "This Week", "This Month", "Custom Range", "Clear Filter");
            
            switch (result)
            {
                case "Today":
                    _filterStartDate = DateTime.Today;
                    _filterEndDate = DateTime.Today.AddDays(1).AddSeconds(-1);
                    break;
                case "Yesterday":
                    _filterStartDate = DateTime.Today.AddDays(-1);
                    _filterEndDate = DateTime.Today.AddSeconds(-1);
                    break;
                case "This Week":
                    _filterStartDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                    _filterEndDate = _filterStartDate.Value.AddDays(7).AddSeconds(-1);
                    break;
                case "This Month":
                    _filterStartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    _filterEndDate = _filterStartDate.Value.AddMonths(1).AddSeconds(-1);
                    break;
                case "Custom Range":
                    // Show custom date picker dialog
                    await ShowCustomDateFilterAsync();
                    return;
                case "Clear Filter":
                    _filterStartDate = null;
                    _filterEndDate = null;
                    break;
                default:
                    return;
            }
            
            await LoadSalesDataAsync();
        }

        private async Task ShowCustomDateFilterAsync()
        {
            // Create a simple date range picker dialog
            var startDatePicker = new DatePicker { Date = DateTime.Today.AddDays(-7) };
            var endDatePicker = new DatePicker { Date = DateTime.Today };
            
            var layout = new VerticalStackLayout
            {
                Spacing = 10,
                Padding = new Thickness(20),
                Children =
                {
                    new Label { Text = "Start Date:", FontAttributes = FontAttributes.Bold },
                    startDatePicker,
                    new Label { Text = "End Date:", FontAttributes = FontAttributes.Bold, Margin = new Thickness(0, 10, 0, 0) },
                    endDatePicker
                }
            };
            
            bool result = await DisplayAlert("Custom Date Range", "Select date range:", "Apply", "Cancel");
            if (result)
            {
                _filterStartDate = startDatePicker.Date;
                _filterEndDate = endDatePicker.Date.AddDays(1).AddSeconds(-1);
                await LoadSalesDataAsync();
            }
        }

        private async void DateRangeButton_Clicked(object sender, EventArgs e)
        {
            // Show custom date range picker
            await ShowCustomDateRangeForChartAsync();
        }

        private async Task ShowCustomDateRangeForChartAsync()
        {
            // Create a simple date range picker dialog
            var startDatePicker = new DatePicker { Date = _startDate };
            var endDatePicker = new DatePicker { Date = _endDate };
            
            var layout = new VerticalStackLayout
            {
                Spacing = 16,
                Padding = new Thickness(20),
                Children =
                {
                    new Label { Text = "Select Date Range", FontAttributes = FontAttributes.Bold, FontSize = 18, TextColor = Color.FromArgb("#1e293b") },
                    new BoxView { Color = Color.FromArgb("#e2e8f0"), HeightRequest = 1, Margin = new Thickness(0, 4) },
                    new Label { Text = "Start Date:", FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#64748b") },
                    startDatePicker,
                    new Label { Text = "End Date:", FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#64748b"), Margin = new Thickness(0, 16, 0, 0) },
                    endDatePicker,
                    new BoxView { Color = Color.FromArgb("#e2e8f0"), HeightRequest = 1, Margin = new Thickness(0, 16, 0, 16) },
                    new Label { Text = "Preset Ranges:", FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#64748b") },
                    new HorizontalStackLayout
                    {
                        Spacing = 10,
                        Children =
                        {
                            new Button { Text = "Last 7 Days", BackgroundColor = Color.FromArgb("#f1f5f9"), TextColor = Color.FromArgb("#1e293b"), FontSize = 12, CornerRadius = 8, Padding = new Thickness(8, 4) },
                            new Button { Text = "Last 30 Days", BackgroundColor = Color.FromArgb("#f1f5f9"), TextColor = Color.FromArgb("#1e293b"), FontSize = 12, CornerRadius = 8, Padding = new Thickness(8, 4) },
                            new Button { Text = "This Month", BackgroundColor = Color.FromArgb("#f1f5f9"), TextColor = Color.FromArgb("#1e293b"), FontSize = 12, CornerRadius = 8, Padding = new Thickness(8, 4) }
                        }
                    }
                }
            };
            
            // Add event handlers for preset buttons
            if (layout.Children[8] is HorizontalStackLayout buttonStack)
            {
                if (buttonStack.Children[0] is Button btn7Days)
                {
                    btn7Days.Clicked += (s, e) => {
                        startDatePicker.Date = DateTime.Now.AddDays(-7);
                        endDatePicker.Date = DateTime.Now;
                    };
                }
                
                if (buttonStack.Children[1] is Button btn30Days)
                {
                    btn30Days.Clicked += (s, e) => {
                        startDatePicker.Date = DateTime.Now.AddDays(-30);
                        endDatePicker.Date = DateTime.Now;
                    };
                }
                
                if (buttonStack.Children[2] is Button btnThisMonth)
                {
                    btnThisMonth.Clicked += (s, e) => {
                        startDatePicker.Date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                        endDatePicker.Date = DateTime.Now;
                    };
                }
            }
            
            // Show custom dialog
            bool result = await Application.Current.MainPage.DisplayAlert("Custom Date Range", "Please select a date range for the chart:", "Apply", "Cancel");
            if (result)
            {
                _startDate = startDatePicker.Date;
                _endDate = endDatePicker.Date;
                
                // Ensure end date is not before start date
                if (_endDate < _startDate)
                {
                    _endDate = _startDate;
                }
                
                // Update date range label
                UpdateDateRangeLabel();
                
                // Update chart
                await UpdateChartDataAsync();
                DrawSalesChart();
            }
        }

        private async void SalesCollection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is SaleViewModel selectedSale)
            {
                // Clear selection
                salesCollection.SelectedItem = null;
                
                // Navigate to sale details
                // Fix: Use Id instead of SaleId and remove reference to User property
                await DisplayAlert("Sale Details", 
                    $"Sale #{selectedSale.Id}\nDate: {selectedSale.SaleDate}\nCustomer: {selectedSale.Username}\nAmount: ${selectedSale.TotalAmount:N2}", 
                    "OK");
            }
        }

        private async void NewOrderButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                // Show loading animation on button
                var button = (Button)sender;
                var originalText = button.Text;
                button.IsEnabled = false;
                button.Text = "Loading...";
                
                // Navigate to new sale page
                await Navigation.PushAsync(new NewSaleDetailPage());
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to open new sale page: {ex.Message}", "OK");
                Debug.WriteLine($"Error navigating to new sale page: {ex}");
            }
            finally
            {
                // Restore button state
                var button = (Button)sender;
                button.Text = "New Sale";
                button.IsEnabled = true;
            }
        }
    }

    // ViewModel for Sale to provide formatted display properties
    public class SaleViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public DateTime SaleDate { get; set; }
        public decimal TotalAmount { get; set; }
        
        // Formatted display properties
        public string SaleIdDisplay => $"Sale #{Id}";
        public string SaleDateDisplay => $"Date: {SaleDate:MMM d, yyyy h:mm tt}";
        public string UserDisplay => $"Customer: {Username}";
        public string TotalAmountDisplay => $"${TotalAmount:N2}";
        
        public SaleViewModel(Sale sale)
        {
            Id = sale.Id;
            UserId = sale.UserId;
            Username = sale.Username;
            SaleDate = sale.SaleDate;
            TotalAmount = sale.TotalAmount;
        }
    }

    // Chart data class
    public class ChartData
    {
        public List<DateTime> Dates { get; set; } = new List<DateTime>();
        public List<int> SalesCounts { get; set; } = new List<int>();
        public List<float> Revenues { get; set; } = new List<float>();
    }

    // Enhanced 3D Bar Chart Implementation
    public class SalesChart3DDrawable : IDrawable
    {
        private readonly ChartData _data;
        private readonly Color _salesBarColor = Color.FromArgb("#6366f1");  // PrimaryColor 
        private readonly Color _salesBarShadowColor = Color.FromArgb("#4338ca");  // Darker PrimaryColor
        private readonly Color _revenueBarColor = Color.FromArgb("#14b8a6"); // SecondaryColor
        private readonly Color _revenueBarShadowColor = Color.FromArgb("#0f766e"); // Darker SecondaryColor
        private readonly Color _gridLineColor = Color.FromArgb("#e2e8f0");    // BorderColor
        private readonly Color _textColor = Color.FromArgb("#64748b");        // TextMediumColor
        private readonly float _barWidth = 12;  // Width of each bar
        private readonly float _barGap = 8;    // Gap between sales and revenue bars
        private readonly float _barDepth = 5;  // Depth effect for 3D bars

        public SalesChart3DDrawable(ChartData data)
        {
            _data = data;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (_data.Dates.Count == 0)
                return;

            float padding = 16;
            float chartWidth = dirtyRect.Width - (padding * 2);
            float chartHeight = dirtyRect.Height - (padding * 2);
            float xStart = padding;
            float yStart = dirtyRect.Height - padding;
            
            // Background grid
            DrawGrid(canvas, xStart, yStart, chartWidth, chartHeight);
            
            // Draw X-axis labels (dates)
            DrawDateLabels(canvas, xStart, yStart, chartWidth);
            
            // Draw bars
            DrawBars(canvas, xStart, yStart, chartWidth, chartHeight);
        }
        
        private void DrawGrid(ICanvas canvas, float xStart, float yStart, float chartWidth, float chartHeight)
        {
            // Main horizontal lines for grid
            canvas.StrokeColor = _gridLineColor;
            canvas.StrokeSize = 1;
            
            int hGridLines = 4;
            for (int i = 0; i <= hGridLines; i++)
            {
                float y = yStart - ((float)i / hGridLines * chartHeight);
                canvas.DrawLine(xStart, y, xStart + chartWidth, y);
            }
        }
        
        private void DrawDateLabels(ICanvas canvas, float xStart, float yStart, float chartWidth)
        {
            int numDates = _data.Dates.Count;
            if (numDates <= 0) return;
            
            float dateStep = chartWidth / (numDates > 1 ? numDates : 1);
            float totalBarWidth = (_barWidth * 2) + _barGap;
            
            // Only show labels for some dates if there are too many
            int labelFrequency = numDates > 10 ? numDates / 6 : 1;
            
            canvas.FontColor = _textColor;
            canvas.FontSize = 11;
            
            for (int i = 0; i < numDates; i++)
            {
                // Show fewer labels if there are many dates
                if (i % labelFrequency == 0 || i == numDates - 1)
                {
                    float x = xStart + (i * dateStep) + (totalBarWidth / 2);
                    string dateLabel = _data.Dates[i].ToString("MM/dd");
                    canvas.DrawString(dateLabel, x, yStart + 15, HorizontalAlignment.Center);
                }
            }
        }
        
        private void DrawBars(ICanvas canvas, float xStart, float yStart, float chartWidth, float chartHeight)
        {
            int numDates = _data.Dates.Count;
            if (numDates <= 0) return;
            
            float dateStep = chartWidth / (numDates > 1 ? numDates : 1);
            
            // Find max values for scaling
            int maxSales = _data.SalesCounts.Count > 0 ? _data.SalesCounts.Max() : 1;
            float maxRevenue = _data.Revenues.Count > 0 ? _data.Revenues.Max() : 1;
            
            // Draw the bars for each date
            for (int i = 0; i < numDates; i++)
            {
                float x = xStart + (i * dateStep);
                
                // Draw Sales Count Bar (3D effect)
                if (i < _data.SalesCounts.Count)
                {
                    float salesValue = (float)_data.SalesCounts[i] / maxSales;
                    float barHeight = salesValue * chartHeight;
                    
                    // Only draw bars with non-zero values
                    if (barHeight > 0)
                    {
                        // Draw 3D side of bar
                        PathF side = new PathF();
                        side.MoveTo(x + _barWidth, yStart);
                        side.LineTo(x + _barWidth, yStart - barHeight);
                        side.LineTo(x + _barWidth + _barDepth, yStart - barHeight + _barDepth);
                        side.LineTo(x + _barWidth + _barDepth, yStart + _barDepth);
                        side.Close();
                        
                        canvas.FillColor = _salesBarShadowColor;
                        canvas.FillPath(side);
                        
                        // Draw 3D top of bar
                        PathF top = new PathF();
                        top.MoveTo(x, yStart - barHeight);
                        top.LineTo(x + _barWidth, yStart - barHeight);
                        top.LineTo(x + _barWidth + _barDepth, yStart - barHeight + _barDepth);
                        top.LineTo(x + _barDepth, yStart - barHeight + _barDepth);
                        top.Close();
                        
                        canvas.FillColor = _salesBarShadowColor.WithAlpha(0.7f);
                        canvas.FillPath(top);
                        
                        // Draw main front of bar
                        canvas.FillColor = _salesBarColor;
                        canvas.FillRectangle(x, yStart - barHeight, _barWidth, barHeight);
                        
                        // Draw value on top of bar if it's big enough
                        if (barHeight > 25)
                        {
                            canvas.FontColor = Colors.White;
                            canvas.FontSize = 10;
                            canvas.DrawString(_data.SalesCounts[i].ToString(), 
                                x + (_barWidth / 2), 
                                yStart - barHeight - 10,
                                HorizontalAlignment.Center);
                        }
                    }
                }
                
                // Draw Revenue Bar (3D effect)
                if (i < _data.Revenues.Count)
                {
                    float revenueValue = _data.Revenues[i] / maxRevenue;
                    float barHeight = revenueValue * chartHeight;
                    float revX = x + _barWidth + _barGap;
                    
                    // Only draw bars with non-zero values
                    if (barHeight > 0)
                    {
                        // Draw 3D side of bar
                        PathF side = new PathF();
                        side.MoveTo(revX + _barWidth, yStart);
                        side.LineTo(revX + _barWidth, yStart - barHeight);
                        side.LineTo(revX + _barWidth + _barDepth, yStart - barHeight + _barDepth);
                        side.LineTo(revX + _barWidth + _barDepth, yStart + _barDepth);
                        side.Close();
                        
                        canvas.FillColor = _revenueBarShadowColor;
                        canvas.FillPath(side);
                        
                        // Draw 3D top of bar
                        PathF top = new PathF();
                        top.MoveTo(revX, yStart - barHeight);
                        top.LineTo(revX + _barWidth, yStart - barHeight);
                        top.LineTo(revX + _barWidth + _barDepth, yStart - barHeight + _barDepth);
                        top.LineTo(revX + _barDepth, yStart - barHeight + _barDepth);
                        top.Close();
                        
                        canvas.FillColor = _revenueBarShadowColor.WithAlpha(0.7f);
                        canvas.FillPath(top);
                        
                        // Draw main front of bar
                        canvas.FillColor = _revenueBarColor;
                        canvas.FillRectangle(revX, yStart - barHeight, _barWidth, barHeight);
                        
                        // Draw value on top of bar if it's big enough
                        if (barHeight > 25)
                        {
                            canvas.FontColor = Colors.White;
                            canvas.FontSize = 10;
                            canvas.DrawString($"${_data.Revenues[i]:N0}", 
                                revX + (_barWidth / 2), 
                                yStart - barHeight - 10,
                                HorizontalAlignment.Center);
                        }
                    }
                }
            }
        }
    }
}