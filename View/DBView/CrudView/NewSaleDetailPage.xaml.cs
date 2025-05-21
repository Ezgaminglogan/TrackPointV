using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;
using TrackPointV.Service;

namespace TrackPointV.View.DBView.CrudView
{
    // Adding Product class definition to avoid reference issues
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public int Stock { get; set; }
        public DateTime DateUploaded { get; set; }
    }

    public partial class NewSaleDetailPage : ContentPage
    {
        private readonly SaleService _saleService;
        private readonly InventoryService _productService;
        private readonly UserService _userService;
        private ObservableCollection<SaleItemViewModel> _cartItems;
        private List<Product> _availableProducts;
        private List<User> _users;
        private int _selectedUserId;
        private decimal _totalAmount = 0;

        public NewSaleDetailPage()
        {
            InitializeComponent();
            
            _saleService = new SaleService();
            _productService = new InventoryService();
            _userService = new UserService();
            _cartItems = new ObservableCollection<SaleItemViewModel>();
            
            cartItemsCollection.ItemsSource = _cartItems;
            
            // Set current date
            saleDate.Date = DateTime.Now;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Show loading indicator
                loadingIndicator.IsRunning = true;
                contentLayout.IsVisible = false;

                // Load products
                var products = await _productService.GetAllProductsAsync();
                _availableProducts = products.Select(p => new Product
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Description = p.Description,
                    Stock = p.Stock,
                    DateUploaded = p.DateUploaded
                }).ToList();
                
                productPicker.ItemsSource = _availableProducts;
                
                // Load users
                var allUsers = await _userService.GetAllUsersAsync();
                // Filter out any username containing admin (case-insensitive)
                _users = allUsers
                    .Where(u => !u.Username.Contains("admin", StringComparison.OrdinalIgnoreCase) || u.Username.Contains("admins", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                userPicker.ItemsSource = _users;
                
                // Update UI
                UpdateTotalAmount();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load data: {ex.Message}", "OK");
                Debug.WriteLine($"Error loading data: {ex}");
            }
            finally
            {
                // Hide loading indicator
                loadingIndicator.IsRunning = false;
                contentLayout.IsVisible = true;
            }
        }

        private void UpdateTotalAmount()
        {
            _totalAmount = 0;
            foreach (var item in _cartItems)
            {
                _totalAmount += item.TotalPrice;
            }
            
            totalAmountLabel.Text = $"{_totalAmount.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-PH"))}";
            
            // Enable/disable save button based on cart items
            saveButton.IsEnabled = _cartItems.Count > 0 && _selectedUserId > 0;
        }

        private void ProductPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (productPicker.SelectedItem is Product selectedProduct)
            {
                priceEntry.Text = selectedProduct.Price.ToString("N2");
                
                // Highlight stock status based on quantity
                if (selectedProduct.Stock <= 0)
                {
                    stockLabel.Text = "Out of Stock!";
                    stockLabel.TextColor = Color.FromArgb("#ef4444"); // DangerColor
                    addToCartButton.IsEnabled = false;
                }
                else if (selectedProduct.Stock <= 5)
                {
                    stockLabel.Text = $"Low Stock: {selectedProduct.Stock}";
                    stockLabel.TextColor = Color.FromArgb("#f59e0b"); // AccentColor/Warning
                    addToCartButton.IsEnabled = true;
                }
                else
                {
                    stockLabel.Text = $"Available: {selectedProduct.Stock}";
                    stockLabel.TextColor = Color.FromArgb("#14b8a6"); // SecondaryColor
                    addToCartButton.IsEnabled = true;
                }
                
                // Set max quantity
                quantityStepper.Maximum = selectedProduct.Stock;
                quantityStepper.Value = 1;
                quantityLabel.Text = "1";
                
                // Update total price label
                totalPriceLabel.Text = $"${selectedProduct.Price:N2}";
                
                // Update visual feedback
                selectedProductLabel.Text = $"Selected: {selectedProduct.Name}";
                selectedProductLabel.TextColor = Color.FromArgb("#14b8a6"); // SecondaryColor
                productPickerBorder.Background = Color.FromArgb("#f0fdfa"); // Light teal background
            }
            else
            {
                selectedProductLabel.Text = "No product selected";
                selectedProductLabel.TextColor = Color.FromArgb("#94a3b8"); // TextLightColor
                productPickerBorder.Background = Color.FromArgb("#F8FAFC"); // Default background
                
                priceEntry.Text = string.Empty;
                stockLabel.Text = "Available: 0";
                stockLabel.TextColor = Color.FromArgb("#64748b"); // TextMediumColor
                quantityStepper.Value = 1;
                quantityLabel.Text = "1";
                totalPriceLabel.Text = "$0.00";
                addToCartButton.IsEnabled = false;
            }
        }

        private void UserPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (userPicker.SelectedItem is User selectedUser)
            {
                _selectedUserId = selectedUser.Id;
                
                // Update visual feedback
                selectedUserLabel.Text = $"Selected: {selectedUser.Username}";
                selectedUserLabel.TextColor = Color.FromArgb("#14b8a6"); // SecondaryColor
                userPickerBorder.Background = Color.FromArgb("#f0fdfa"); // Light teal background
                
                UpdateTotalAmount(); // Update save button state
            }
            else
            {
                selectedUserLabel.Text = "No user selected";
                selectedUserLabel.TextColor = Color.FromArgb("#94a3b8"); // TextLightColor
                userPickerBorder.Background = Color.FromArgb("#F8FAFC"); // Default background
            }
        }

        private void QuantityStepper_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (productPicker.SelectedItem is Product selectedProduct)
            {
                decimal totalPrice = selectedProduct.Price * (decimal)e.NewValue;
                totalPriceLabel.Text = $"{totalPrice.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-PH"))}";
                
                // Update quantity label to match stepper value
                quantityLabel.Text = e.NewValue.ToString();
            }
        }
        
        // Add these new methods to handle the + and - buttons
        private void IncrementQuantity_Clicked(object sender, EventArgs e)
        {
            if (productPicker.SelectedItem is Product selectedProduct)
            {
                int currentQuantity = (int)quantityStepper.Value;
                if (currentQuantity < selectedProduct.Stock)
                {
                    quantityStepper.Value++;
                    // The QuantityStepper_ValueChanged event will update the UI
                }
            }
        }
        
        private void DecrementQuantity_Clicked(object sender, EventArgs e)
        {
            if (quantityStepper.Value > 1)
            {
                quantityStepper.Value--;
                // The QuantityStepper_ValueChanged event will update the UI
            }
        }

        private void AddToCartButton_Clicked(object sender, EventArgs e)
        {
            if (productPicker.SelectedItem is Product selectedProduct)
            {
                int quantity = (int)quantityStepper.Value;
                
                // Check if product already in cart
                var existingItem = _cartItems.FirstOrDefault(i => i.ProductId == selectedProduct.Id);
                if (existingItem != null)
                {
                    // Update quantity
                    int newQuantity = existingItem.Quantity + quantity;
                    if (newQuantity <= selectedProduct.Stock)
                    {
                        existingItem.Quantity = newQuantity;
                        existingItem.TotalPrice = selectedProduct.Price * newQuantity;
                    }
                    else
                    {
                        DisplayAlert("Error", "Not enough stock available", "OK");
                        return;
                    }
                }
                else
                {
                    // Add new item
                    _cartItems.Add(new SaleItemViewModel
                    {
                        ProductId = selectedProduct.Id,
                        ProductName = selectedProduct.Name,
                        PricePerUnit = selectedProduct.Price,
                        Quantity = quantity,
                        TotalPrice = selectedProduct.Price * quantity
                    });
                }
                
                // Show confirmation
                DisplayAlert("Success", $"Added {quantity} {selectedProduct.Name} to cart", "OK");
                
                // Reset product selection
                productPicker.SelectedItem = null;
                priceEntry.Text = string.Empty;
                stockLabel.Text = "Available: 0";
                stockLabel.TextColor = Color.FromArgb("#64748b"); // TextMediumColor
                quantityStepper.Value = 1;
                quantityLabel.Text = "1";
                totalPriceLabel.Text = "₱0.00";
                addToCartButton.IsEnabled = false;
                selectedProductLabel.Text = "No product selected";
                selectedProductLabel.TextColor = Color.FromArgb("#94a3b8"); // TextLightColor
                productPickerBorder.Background = Color.FromArgb("#F8FAFC"); // Default background
                
                // Update total
                UpdateTotalAmount();
            }
        }

        private void RemoveCartItem_Clicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is SaleItemViewModel item)
            {
                _cartItems.Remove(item);
                UpdateTotalAmount();
            }
        }

        private async void SaveButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                // Validate
                if (_cartItems.Count == 0)
                {
                    await DisplayAlert("Error", "Please add at least one item to the cart", "OK");
                    return;
                }
                
                if (_selectedUserId <= 0)
                {
                    await DisplayAlert("Error", "Please select a user", "OK");
                    return;
                }
                
                // Confirm the transaction
                bool confirm = await DisplayAlert("Confirm Sale", 
                    $"Complete this sale for ₱{_totalAmount:N2}?\n\nThis will reduce product stock accordingly.", 
                    "Complete Sale", "Cancel");
                
                if (!confirm)
                    return;
                
                // Show loading indicator
                saveButton.IsEnabled = false;
                loadingIndicator.IsRunning = true;
                contentLayout.IsVisible = false;
                
                // Create sale
                var sale = new Sale
                {
                    UserId = _selectedUserId,
                    SaleDate = saleDate.Date,
                    TotalAmount = _totalAmount,
                    Items = _cartItems.Select(i => new SaleItem
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        PricePerUnit = i.PricePerUnit
                    }).ToList()
                };
                
                // Save to database
                bool success = await _saleService.CreateSaleAsync(sale);
                
                if (success)
                {
                    // Build stock update summary for confirmation
                    string stockUpdateSummary = "Stock updated for:\n";
                    
                    // Update product stock
                    foreach (var item in _cartItems)
                    {
                        var product = _availableProducts.FirstOrDefault(p => p.Id == item.ProductId);
                        if (product != null)
                        {
                            // Update stock in database
                            int oldStock = product.Stock;
                            product.Stock -= item.Quantity;
                            await _productService.UpdateProductAsync(new Service.Product
                            {
                                Id = product.Id,
                                Name = product.Name,
                                Price = product.Price,
                                Description = product.Description,
                                Stock = product.Stock,
                                DateUploaded = product.DateUploaded
                            });
                            
                            // Add to summary
                            stockUpdateSummary += $"• {product.Name}: {oldStock} → {product.Stock}\n";
                        }
                    }
                    
                    await DisplayAlert("Success", $"Sale #{sale.Id} created successfully!\n\n{stockUpdateSummary}", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Error", "Failed to create sale", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to save sale: {ex.Message}", "OK");
                Debug.WriteLine($"Error saving sale: {ex}");
            }
            finally
            {
                // Hide loading indicator
                loadingIndicator.IsRunning = false;
                contentLayout.IsVisible = true;
                saveButton.IsEnabled = true;
            }
        }

        private async void CancelButton_Clicked(object sender, EventArgs e)
        {
            if (_cartItems.Count > 0)
            {
                bool confirm = await DisplayAlert("Confirm", "Are you sure you want to cancel this sale?", "Yes", "No");
                if (confirm)
                {
                    await Navigation.PopAsync();
                }
            }
            else
            {
                await Navigation.PopAsync();
            }
        }
    }

    public class SaleItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal PricePerUnit { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        
        public string PriceDisplay => $"₱{PricePerUnit:N2}";
        public string TotalPriceDisplay => $"₱{TotalPrice:N2}";
    }
}