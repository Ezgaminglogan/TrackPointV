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
        private DateTime _startDate;
        private DateTime _endDate;

        public SalePage()
        {
            InitializeComponent();
            _saleService = new SaleService();
            _sales = new ObservableCollection<SaleViewModel>();
            salesCollection.ItemsSource = _sales;
            
            // Set default chart period
            chartPeriodPicker.SelectedIndex = 0;
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

        private void SetChartDateRange(int days)
        {
            _startDate = DateTime.Now.AddDays(-days);
            _endDate = DateTime.Now;
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
                salesChartView.Drawable = new SalesChartDrawable(chartData);
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

        private async void ChartPeriodPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (chartPeriodPicker.SelectedIndex)
            {
                case 0: // Last 7 days
                    SetChartDateRange(7);
                    break;
                case 1: // Last 30 days
                    SetChartDateRange(30);
                    break;
                case 2: // Last 90 days
                    SetChartDateRange(90);
                    break;
                case 3: // This year
                    _startDate = new DateTime(DateTime.Now.Year, 1, 1);
                    _endDate = DateTime.Now;
                    break;
            }
            
            await UpdateChartDataAsync();
            DrawSalesChart();
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

    // Chart drawable class
    public class SalesChartDrawable : IDrawable
    {
        private readonly ChartData _data;
        private readonly Color _salesLineColor = Color.FromArgb("#6366f1");  // PrimaryColor 
        private readonly Color _revenueLineColor = Color.FromArgb("#14b8a6"); // SecondaryColor
        private readonly Color _gridLineColor = Color.FromArgb("#e2e8f0");    // BorderColor
        private readonly Color _textColor = Color.FromArgb("#64748b");        // TextMediumColor

        public SalesChartDrawable(ChartData data)
        {
            _data = data;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (_data.Dates.Count == 0)
                return;

            float padding = 40;
            float chartWidth = dirtyRect.Width - (padding * 2);
            float chartHeight = dirtyRect.Height - (padding * 2);
            float xStart = padding;
            float yStart = dirtyRect.Height - padding;

            // Draw grid lines
            canvas.StrokeColor = _gridLineColor;
            canvas.StrokeSize = 1;

            // Horizontal grid lines
            int hGridLines = 5;
            for (int i = 0; i <= hGridLines; i++)
            {
                float y = yStart - ((float)i / hGridLines * chartHeight);
                canvas.DrawLine(xStart, y, xStart + chartWidth, y);
                
                // Draw y-axis labels (sales count)
                int maxSales = _data.SalesCounts.Count > 0 ? _data.SalesCounts.Max() : 0;
                int salesValue = (int)((float)i / hGridLines * maxSales);
                canvas.DrawString(salesValue.ToString(), xStart - 25, y - 10, HorizontalAlignment.Center);
            }

            // Vertical grid lines
            int numPoints = _data.Dates.Count;
            float xStep = chartWidth / (numPoints - 1 > 0 ? numPoints - 1 : 1);
            
            for (int i = 0; i < numPoints; i++)
            {
                float x = xStart + (i * xStep);
                canvas.DrawLine(x, yStart, x, yStart - chartHeight);
                
                // Draw x-axis labels (dates)
                string dateLabel = _data.Dates[i].ToString("MM/dd");
                canvas.DrawString(dateLabel, x, yStart + 15, HorizontalAlignment.Center);
            }

            // Draw sales count line
            if (_data.SalesCounts.Count > 0)
            {
                int maxSales = _data.SalesCounts.Max();
                if (maxSales > 0)
                {
                    canvas.StrokeColor = _salesLineColor;
                    canvas.StrokeSize = 3;
                    
                    PathF path = new PathF();
                    for (int i = 0; i < numPoints; i++)
                    {
                        float x = xStart + (i * xStep);
                        float normalizedValue = (float)_data.SalesCounts[i] / maxSales;
                        float y = yStart - (normalizedValue * chartHeight);
                        
                        if (i == 0)
                            path.MoveTo(x, y);
                        else
                            path.LineTo(x, y);
                    }
                    
                    canvas.DrawPath(path);
                    
                    // Draw points on the line
                    for (int i = 0; i < numPoints; i++)
                    {
                        float x = xStart + (i * xStep);
                        float normalizedValue = (float)_data.SalesCounts[i] / maxSales;
                        float y = yStart - (normalizedValue * chartHeight);
                        
                        canvas.FillColor = _salesLineColor;
                        canvas.FillCircle(x, y, 5);
                    }
                }
            }

            // Draw revenue line
            if (_data.Revenues.Count > 0)
            {
                float maxRevenue = _data.Revenues.Max();
                if (maxRevenue > 0)
                {
                    canvas.StrokeColor = _revenueLineColor;
                    canvas.StrokeSize = 3;
                    
                    PathF path = new PathF();
                    for (int i = 0; i < numPoints; i++)
                    {
                        float x = xStart + (i * xStep);
                        float normalizedValue = _data.Revenues[i] / maxRevenue;
                        float y = yStart - (normalizedValue * chartHeight);
                        
                        if (i == 0)
                            path.MoveTo(x, y);
                        else
                            path.LineTo(x, y);
                    }
                    
                    canvas.DrawPath(path);
                    
                    // Draw points on the line
                    for (int i = 0; i < numPoints; i++)
                    {
                        float x = xStart + (i * xStep);
                        float normalizedValue = _data.Revenues[i] / maxRevenue;
                        float y = yStart - (normalizedValue * chartHeight);
                        
                        canvas.FillColor = _revenueLineColor;
                        canvas.FillCircle(x, y, 5);
                    }
                    
                    // Draw revenue scale on right side
                    canvas.FontColor = _revenueLineColor;
                    for (int i = 0; i <= hGridLines; i++)
                    {
                        float y = yStart - ((float)i / hGridLines * chartHeight);
                        float revenueValue = (float)i / hGridLines * maxRevenue;
                        canvas.DrawString($"${revenueValue:N0}", xStart + chartWidth + 25, y - 10, HorizontalAlignment.Center);
                    }
                }
            }
        }
    }
}