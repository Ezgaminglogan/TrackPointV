using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using TrackPointV.Service;
using TrackPointV.View.DBView.CrudView;
using System.Diagnostics;

namespace TrackPointV.View.DBView
{
    public partial class ProductPage : ContentPage
    {
        private readonly InventoryService _inventoryService;
        private ObservableCollection<Product> _products;
        private ObservableCollection<Product> _featuredProducts;

        public ProductPage()
        {
            InitializeComponent();
            _inventoryService = new InventoryService();
            _products = new ObservableCollection<Product>();
            _featuredProducts = new ObservableCollection<Product>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // First set up empty bindings to prevent scroll issues
            if (featuredProductsCarousel != null)
            {
                featuredProductsCarousel.ItemsSource = new ObservableCollection<Product>();
            }
            
            // Load products
            await LoadProductsAsync();
            
            // Connect the indicator view to the carousel only if we have products
            if (_featuredProducts.Count > 0 && featuredProductsCarousel != null && indicatorView != null)
            {
                indicatorView.SetBinding(IndicatorView.PositionProperty,
                    new Binding("Position", source: featuredProductsCarousel));
                indicatorView.SetBinding(IndicatorView.ItemsSourceProperty,
                    new Binding("ItemsSource", source: featuredProductsCarousel));
            }
        }


        private async Task LoadProductsAsync()
        {
            try
            {
                if (refreshView != null)
                    refreshView.IsRefreshing = true;

                // Get products from database
                var serviceProducts = await _inventoryService.GetAllProductsAsync();
                
                // Clear collections safely
                MainThread.BeginInvokeOnMainThread(() => {
                    _products.Clear();
                    _featuredProducts.Clear();
                });
                
                if (serviceProducts == null)
                {
                    // Handle null response from service
                    await DisplayAlert("Warning", "No products data available", "OK");
                    serviceProducts = new List<TrackPointV.Service.Product>(); // Initialize empty list
                }
                
                // Create temporary collections to avoid UI updates during population
                var tempProducts = new List<Product>();
                var tempFeatured = new List<Product>();
                
                foreach (var serviceProduct in serviceProducts)
                {
                    // Convert from Service.Product to View.DBView.Product
                    tempProducts.Add(ConvertToViewProduct(serviceProduct));
                }
                
                // Check if there are any products before trying to take from the list
                if (serviceProducts.Any())
                {
                    var featured = serviceProducts.Take(Math.Min(5, serviceProducts.Count)).ToList();
                    foreach (var serviceProduct in featured)
                    {
                        // Convert from Service.Product to View.DBView.Product
                        tempFeatured.Add(ConvertToViewProduct(serviceProduct));
                    }
                }

                // Update UI on the main thread to avoid cross-thread issues
                await MainThread.InvokeOnMainThreadAsync(() => {
                    // Add all items at once to minimize collection change events
                    foreach (var product in tempProducts)
                    {
                        _products.Add(product);
                    }
                    
                    foreach (var product in tempFeatured)
                    {
                        _featuredProducts.Add(product);
                    }
                    
                    // Update UI controls
                    if (productsCollection != null)
                        productsCollection.ItemsSource = null; // Clear first
                        
                    if (productsCollection != null)
                        productsCollection.ItemsSource = _products;
                        
                    if (featuredProductsCarousel != null)
                    {
                        // Set to null first to avoid binding issues
                        featuredProductsCarousel.ItemsSource = null;
                        featuredProductsCarousel.ItemsSource = _featuredProducts;
                    }
                    
                    // Update statistics
                    if (totalProductsLabel != null)
                        totalProductsLabel.Text = _products.Count.ToString();
                        
                    // Safely calculate low stock count
                    int lowStockCount = 0;
                    if (_products != null && _products.Any())
                        lowStockCount = _products.Count(p => p.Stock <= 10);
                        
                    if (lowStockLabel != null)
                        lowStockLabel.Text = lowStockCount.ToString();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Product loading error details: {ex}");
                await DisplayAlert("Error", $"Failed to load products: {ex.Message}", "OK");
            }
            finally
            {
                if (refreshView != null)
                    refreshView.IsRefreshing = false;
            }
        }

        // Helper method to convert between product types
        private Product ConvertToViewProduct(TrackPointV.Service.Product serviceProduct)
        {
            return new Product
            {
                Id = serviceProduct.Id,
                Name = serviceProduct.Name,
                Price = serviceProduct.Price,
                Description = serviceProduct.Description,
                Stock = serviceProduct.Stock,
                DateUploaded = serviceProduct.DateUploaded
            };
        }

        private async void RefreshView_Refreshing(object sender, EventArgs e)
        {
            await LoadProductsAsync();
        }

        private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = e.NewTextValue?.ToLower() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                productsCollection.ItemsSource = _products;
            }
            else
            {
                var filteredProducts = _products.Where(p =>
                    (p.Name?.ToLower().Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.Description?.ToLower().Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();

                productsCollection.ItemsSource = filteredProducts;
            }
        }

        private async void Filter_Clicked(object sender, EventArgs e)
        {
            string[] options = new[] { "All Products", "Low Stock (≤10)", "Price: Low to High", "Price: High to Low", "Newest First" };
            string result = await DisplayActionSheet("Filter Products", "Cancel", null, options);

            if (result == null || result == "Cancel")
                return;

            IEnumerable<Product> filteredProducts = _products;

            switch (result)
            {
                case "Low Stock (≤10)":
                    filteredProducts = _products.Where(p => p.Stock <= 10);
                    break;
                case "Price: Low to High":
                    filteredProducts = _products.OrderBy(p => p.Price);
                    break;
                case "Price: High to Low":
                    filteredProducts = _products.OrderByDescending(p => p.Price);
                    break;
                case "Newest First":
                    filteredProducts = _products.OrderByDescending(p => p.DateUploaded);
                    break;
            }

            productsCollection.ItemsSource = filteredProducts.ToList();
        }

        private async void AddProduct_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ProductDetailPage());
        }

        private async void EditProduct_Clicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is int productId)
            {
                await Navigation.PushAsync(new ProductDetailPage(productId));
            }
        }

        private async void ViewProductDetails_Clicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is int productId)
            {
                await Navigation.PushAsync(new ProductDetailPage(productId, true));
            }
        }

        private async void DeleteProduct_Clicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is int productId)
            {
                bool confirm = await DisplayAlert("Delete Product", 
                    "Are you sure you want to delete this product? This action cannot be undone.", 
                    "Delete", "Cancel");

                if (confirm)
                {
                    try
                    {
                        await _inventoryService.DeleteProductAsync(productId);
                        await LoadProductsAsync();
                        await DisplayAlert("Success", "Product deleted successfully", "OK");
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Failed to delete product: {ex.Message}", "OK");
                    }
                }
            }
        }

        // Add the carousel methods INSIDE the class
        private void FeaturedProductsCarousel_CurrentItemChanged(object sender, CurrentItemChangedEventArgs e)
        {
            // You can add additional logic here if needed
            // For example, highlighting the current item or showing additional information
        }

        // Optionally, you can add a method to programmatically set the current item
        private void SetCarouselPosition(int index)
        {
            if (_featuredProducts.Count > 0 && index >= 0 && index < _featuredProducts.Count)
            {
                featuredProductsCarousel.Position = index;
            }
        }
    }

    // Simple Product class to match your database schema
    public class Product
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public int Stock { get; set; }
        public DateTime DateUploaded { get; set; }
    }
}