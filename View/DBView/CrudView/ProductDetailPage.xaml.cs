using System;
using System.Threading.Tasks;
using TrackPointV.Service;
using ServiceProduct = TrackPointV.Service.Product; // Alias for clarity

namespace TrackPointV.View.DBView.CrudView
{
    public partial class ProductDetailPage : ContentPage
    {
        private readonly InventoryService _inventoryService;
        private int? _productId;
        private bool _isViewMode;
        private ServiceProduct? _currentProduct; // Changed to ServiceProduct

        // Constructor for adding a new product
        public ProductDetailPage()
        {
            InitializeComponent();
            _inventoryService = new InventoryService();
            _isViewMode = false;
            
            // Set up UI for Add mode
            pageTitle.Text = "Add New Product";
            pageSubtitle.Text = "Enter product details below";
            saveButton.Text = "Save Product";
            
            // Hide view-only section
            viewModeSection.IsVisible = false;
        }

        // Constructor for editing or viewing an existing product
        public ProductDetailPage(int productId, bool isViewMode = false)
        {
            InitializeComponent();
            _inventoryService = new InventoryService();
            _productId = productId;
            _isViewMode = isViewMode;
            
            if (isViewMode)
            {
                // Set up UI for View mode
                pageTitle.Text = "Product Details";
                pageSubtitle.Text = "View product information";
                
                // Hide edit form and show view-only section
                viewModeSection.IsVisible = true;
                
                // Make form read-only
                nameEntry.IsReadOnly = true;
                priceEntry.IsReadOnly = true;
                stockEntry.IsReadOnly = true;
                descriptionEditor.IsReadOnly = true;
                
                // Hide save/cancel buttons
                saveButton.IsVisible = false;
                cancelButton.IsVisible = false;
            }
            else
            {
                // Set up UI for Edit mode
                pageTitle.Text = "Edit Product";
                pageSubtitle.Text = "Update product details";
                saveButton.Text = "Update Product";
                
                // Show date uploaded field in edit mode
                dateUploadedLabel.IsVisible = true;
                dateUploadedEntry.IsVisible = true;
                
                // Hide view-only section
                viewModeSection.IsVisible = false;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // If editing or viewing an existing product, load its data
            if (_productId.HasValue)
            {
                await LoadProductDataAsync(_productId.Value);
            }
        }

        private async Task LoadProductDataAsync(int productId)
        {
            try
            {
                loadingIndicator.IsVisible = true;
                
                // Get product from database
                _currentProduct = await _inventoryService.GetProductByIdAsync(productId);
                
                if (_currentProduct != null)
                {
                    // Populate form fields
                    nameEntry.Text = _currentProduct.Name;
                    priceEntry.Text = _currentProduct.Price.ToString("F2");
                    stockEntry.Text = _currentProduct.Stock.ToString();
                    descriptionEditor.Text = _currentProduct.Description;
                    
                    if (dateUploadedEntry.IsVisible)
                    {
                        dateUploadedEntry.Text = _currentProduct.DateUploaded.ToString("MMM d, yyyy h:mm tt");
                    }
                    
                    // For view mode, populate statistics
                    if (_isViewMode)
                    {
                        dateAddedValue.Text = _currentProduct.DateUploaded.ToString("MMM d, yyyy");
                        lastUpdatedValue.Text = _currentProduct.DateUploaded.ToString("MMM d, yyyy"); // Assuming no separate update date
                        
                        // Get sales statistics (if you have this functionality)
                        try
                        {
                            var salesStats = await _inventoryService.GetProductSalesStatisticsAsync(productId);
                            salesCountValue.Text = salesStats.SalesCount.ToString();
                            revenueValue.Text = $"${salesStats.TotalRevenue:N2}";
                        }
                        catch
                        {
                            // If statistics aren't available, keep defaults
                            salesCountValue.Text = "0";
                            revenueValue.Text = "$0.00";
                        }
                    }
                }
                else
                {
                    await DisplayAlert("Error", "Product not found", "OK");
                    await Navigation.PopAsync();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load product: {ex.Message}", "OK");
                await Navigation.PopAsync();
            }
            finally
            {
                loadingIndicator.IsVisible = false;
            }
        }

        private async void SaveButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(nameEntry.Text))
                {
                    ShowValidationError("Product name is required");
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(priceEntry.Text) || !decimal.TryParse(priceEntry.Text, out decimal price) || price < 0)
                {
                    ShowValidationError("Please enter a valid price");
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(stockEntry.Text) || !int.TryParse(stockEntry.Text, out int stock) || stock < 0)
                {
                    ShowValidationError("Please enter a valid stock quantity");
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(descriptionEditor.Text))
                {
                    ShowValidationError("Product description is required");
                    return;
                }
                
                // Show loading indicator
                loadingIndicator.IsVisible = true;
                
                // Create product object
                var product = new ServiceProduct
                {
                    Name = nameEntry.Text,
                    Price = price,
                    Stock = stock,
                    Description = descriptionEditor.Text,
                    DateUploaded = DateTime.Now
                };
                
                // If editing, set ID and preserve original upload date
                if (_productId.HasValue && _currentProduct != null)
                {
                    product.Id = _productId.Value;
                    product.DateUploaded = _currentProduct.DateUploaded;
                }
                
                // Save to database
                if (_productId.HasValue)
                {
                    await _inventoryService.UpdateProductAsync(product);
                    await DisplayAlert("Success", "Product updated successfully", "OK");
                }
                else
                {
                    await _inventoryService.AddProductAsync(product);
                    await DisplayAlert("Success", "Product added successfully", "OK");
                }
                
                // Return to previous page
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                ShowValidationError($"Error: {ex.Message}");
                loadingIndicator.IsVisible = false;
            }
        }

        private void ShowValidationError(string message)
        {
            validationLabel.Text = message;
            validationLabel.IsVisible = true;
        }

        private async void CancelButton_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private void EditButton_Clicked(object sender, EventArgs e)
        {
            // Switch from view mode to edit mode
            _isViewMode = false;
            
            // Update UI
            pageTitle.Text = "Edit Product";
            pageSubtitle.Text = "Update product details";
            
            // Make form editable
            nameEntry.IsReadOnly = false;
            priceEntry.IsReadOnly = false;
            stockEntry.IsReadOnly = false;
            descriptionEditor.IsReadOnly = false;
            
            // Show save/cancel buttons
            saveButton.IsVisible = true;
            saveButton.Text = "Update Product";
            cancelButton.IsVisible = true;
            
            // Show date uploaded field
            dateUploadedLabel.IsVisible = true;
            dateUploadedEntry.IsVisible = true;
            
            // Hide view-only section
            viewModeSection.IsVisible = false;
        }
    }
}