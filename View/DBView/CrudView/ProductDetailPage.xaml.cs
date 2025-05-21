using System;
using System.Threading.Tasks;
using TrackPointV.Service;
using System.IO;
using System.Net.Http;
using System.Text;
using Microsoft.Maui.Graphics;
using ServiceProduct = TrackPointV.Service.Product; // Alias for clarity

namespace TrackPointV.View.DBView.CrudView
{
    public partial class ProductDetailPage : ContentPage
    {
        private readonly InventoryService _inventoryService;
        private int? _productId;
        private bool _isViewMode;
        private ServiceProduct? _currentProduct; // Changed to ServiceProduct
        private readonly HttpClient _httpClient; // For API calls
        private string _productLookupUrl; // URL for online product lookup

        // Constructor for adding a new product
        public ProductDetailPage()
        {
            InitializeComponent();
            _inventoryService = new InventoryService();
            _httpClient = new HttpClient();
            _isViewMode = false;
            
            // Set up UI for Add mode
            pageTitle.Text = "Add New Product";
            pageSubtitle.Text = "Enter product details below";
            saveButton.Text = "Save Product";
            
            // Hide view-only section
            viewModeSection.IsVisible = false;
            barcodeDisplaySection.IsVisible = false;
        }

        // Constructor for editing or viewing an existing product
        public ProductDetailPage(int productId, bool isViewMode = false)
        {
            InitializeComponent();
            _inventoryService = new InventoryService();
            _httpClient = new HttpClient();
            _productId = productId;
            _isViewMode = isViewMode;
            
            if (isViewMode)
            {
                // Set up UI for View mode
                pageTitle.Text = "Product Details";
                pageSubtitle.Text = "View product information";
                
                // Hide edit form and show view-only section
                viewModeSection.IsVisible = true;
                barcodeDisplaySection.IsVisible = true;
                
                // Make form read-only
                nameEntry.IsReadOnly = true;
                skuEntry.IsReadOnly = true;
                barcodeEntry.IsReadOnly = true;
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
                barcodeDisplaySection.IsVisible = false;
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
                    skuEntry.Text = _currentProduct.SKU;
                    barcodeEntry.Text = _currentProduct.Barcode;
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
                            revenueValue.Text = $"₱{salesStats.TotalRevenue:N2}";
                        }
                        catch
                        {
                            // If statistics aren't available, keep defaults
                            salesCountValue.Text = "0";
                            revenueValue.Text = "₱0.00";
                        }
                        
                        // Generate and display barcode & QR code
                        if (!string.IsNullOrEmpty(_currentProduct.Barcode))
                        {
                            barcodeValueLabel.Text = _currentProduct.Barcode;
                            await GenerateAndDisplayCodesAsync(_currentProduct.Name, _currentProduct.Barcode);
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

        private async Task GenerateAndDisplayCodesAsync(string productName, string barcodeValue)
        {
            try
            {
                // Generate a regular barcode
                string barcodeUrl = $"https://barcodeapi.org/api/128/{Uri.EscapeDataString(barcodeValue)}";
                barcodeImage.Source = barcodeUrl;
                
                // Always use the product name for the search URL
                string searchUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(productName)}";
                _productLookupUrl = searchUrl;
                
                // Generate QR code that directly links to Google search
                string qrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=150x150&data={Uri.EscapeDataString(searchUrl)}";
                qrCodeImage.Source = qrCodeUrl;
                
                // Make the online lookup button visible
                openProductLinkButton.IsVisible = true;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Code Generation Error", $"Could not generate product codes: {ex.Message}", "OK");
            }
        }

        // Simplified barcode search that opens browser directly
        private async void SearchBarcode_Clicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(barcodeEntry.Text))
            {
                ShowValidationError("Please enter a barcode to search");
                return;
            }

            if (string.IsNullOrWhiteSpace(nameEntry.Text))
            {
                await DisplayAlert("Product Search", "Please enter a product name first to search online.", "OK");
                return;
            }

            try
            {
                // Open browser directly with the product name search
                string searchUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(nameEntry.Text)}";
                await Launcher.OpenAsync(new Uri(searchUrl));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Could not open browser: {ex.Message}", "OK");
            }
        }

        private async void OpenProductLink_Clicked(object sender, EventArgs e)
        {
            // Always open a search with the current product name
            try
            {
                string searchUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(nameEntry.Text)}";
                await Launcher.OpenAsync(new Uri(searchUrl));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Could not open product search: {ex.Message}", "OK");
            }
        }

        private void GenerateBarcode_Clicked(object sender, EventArgs e)
        {
            // Generate a unique EAN-13 barcode
            string barcode = GenerateEan13Barcode();
            barcodeEntry.Text = barcode;
        }

        /// <summary>
        /// Generates a valid EAN-13 barcode with proper check digit
        /// </summary>
        private string GenerateEan13Barcode()
        {
            // EAN-13 structure:
            // - First 2-3 digits: Country code (e.g., 608 for Philippines)
            // - Next digits: Manufacturer code + product code (total 9-10 digits)
            // - Last digit: Check digit calculated according to EAN-13 algorithm

            // For Philippines, use 608 as the country code
            StringBuilder ean = new StringBuilder("608"); 

            // Add timestamp-based portion to ensure uniqueness
            string timestamp = DateTime.Now.ToString("yyMMddHHmm"); // 10 digits from year, month, day, hour, minute
            ean.Append(timestamp);

            // At this point, we have 13 digits: 3 for country code + 10 for timestamp
            // Now calculate the check digit according to EAN-13 algorithm
            string barcodeWithoutCheck = ean.ToString();
            int checkDigit = CalculateEan13CheckDigit(barcodeWithoutCheck);
            
            // Return the complete EAN-13 (12 digits + check digit)
            return barcodeWithoutCheck.Substring(0, 12) + checkDigit.ToString();
        }

        /// <summary>
        /// Calculates the EAN-13 check digit for a 12-digit barcode
        /// </summary>
        private int CalculateEan13CheckDigit(string barcode)
        {
            // Ensure we have the first 12 digits of the barcode
            string code = barcode.Substring(0, 12);
            
            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                int digit = int.Parse(code[i].ToString());
                // EAN-13 algorithm: multiply by 1 for even positions, by 3 for odd positions
                sum += digit * (i % 2 == 0 ? 1 : 3);
            }
            
            // The check digit is the number needed to bring the sum to a multiple of 10
            int checkDigit = (10 - (sum % 10)) % 10; // The %10 at the end handles the case where sum % 10 = 0
            
            return checkDigit;
        }

        private void GenerateSku_Clicked(object sender, EventArgs e)
        {
            // Generate a unique SKU code
            string sku = GenerateSkuCode();
            skuEntry.Text = sku;
        }

        /// <summary>
        /// Generates a unique SKU code based on product information and date
        /// </summary>
        private string GenerateSkuCode()
        {
            // Format: [Category Code][Sequential Number][Year Code]
            
            // Category prefix (first 2 characters)
            string categoryPrefix = "PR"; // Default for "Product"
            
            // Try to determine a better category from product name if available
            if (!string.IsNullOrEmpty(nameEntry.Text))
            {
                string name = nameEntry.Text.ToUpper();
                if (name.Contains("ELEC") || name.Contains("GADGET") || name.Contains("PHONE"))
                    categoryPrefix = "EL";
                else if (name.Contains("CLOTH") || name.Contains("APPAREL") || name.Contains("WEAR"))
                    categoryPrefix = "CL";
                else if (name.Contains("FOOD") || name.Contains("DRINK") || name.Contains("GROCERY"))
                    categoryPrefix = "FD";
                else if (name.Contains("TOY") || name.Contains("GAME"))
                    categoryPrefix = "TY";
                else if (name.Contains("FURNITURE") || name.Contains("HOME"))
                    categoryPrefix = "HM";
                else if (name.Contains("BOOK") || name.Contains("STATIONERY"))
                    categoryPrefix = "BK";
                else if (name.Contains("ALCOHOL") || name.Contains("SANITIZER"))
                    categoryPrefix = "AL"; // For products like "Green Cross Isopropyl Alcohol"
            }
            
            // Sequential number (4 digits) - using last 4 digits of timestamp
            string sequentialNumber = DateTime.Now.ToString("mmss");
            
            // Year code (2 digits) - using year's last 2 digits
            string yearCode = DateTime.Now.ToString("yy");
            
            // Random suffix to ensure uniqueness (2 characters)
            string randomSuffix = new Random().Next(10, 99).ToString();
            
            // Combine all parts to create the SKU
            string sku = $"{categoryPrefix}{sequentialNumber}{yearCode}{randomSuffix}";
            
            return sku;
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
                
                // Make SKU optional if not provided, generate it automatically
                if (string.IsNullOrWhiteSpace(skuEntry.Text))
                {
                    skuEntry.Text = GenerateSkuCode();
                }
                
                // Make barcode optional if not provided, generate it automatically
                if (string.IsNullOrWhiteSpace(barcodeEntry.Text))
                {
                    barcodeEntry.Text = GenerateEan13Barcode();
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
                    SKU = skuEntry.Text,
                    Barcode = barcodeEntry.Text,
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
            validationGrid.IsVisible = true;
            validationLabel.Text = message;
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
            skuEntry.IsReadOnly = false;
            barcodeEntry.IsReadOnly = false;
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
            barcodeDisplaySection.IsVisible = false;
        }
    }
}