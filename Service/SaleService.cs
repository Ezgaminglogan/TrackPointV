using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace TrackPointV.Service
{
    public class Sale
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public decimal TotalAmount { get; set; }
        public List<SaleItem> Items { get; set; } = new List<SaleItem>();
    }

    public class SaleItem
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal TotalPrice => Quantity * PricePerUnit;
    }

    public class SaleService
    {
        private readonly Connection _connection;

        public SaleService()
        {
            _connection = new Connection();
        }

        public async Task<List<Sale>> GetAllSalesAsync()
        {
            var sales = new List<Sale>();
            
            try
            {
                string query = @"
                    SELECT s.Id, s.UserId, u.Username, s.SaleDate, s.TotalAmount 
                    FROM Sales s
                    JOIN [User] u ON s.UserId = u.Id
                    ORDER BY s.SaleDate DESC";
                
                var dataTable = await _connection.ExecuteQueryAsync(query, CommandType.Text);
                
                foreach (DataRow row in dataTable.Rows)
                {
                    sales.Add(new Sale
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        UserId = Convert.ToInt32(row["UserId"]),
                        Username = row["Username"].ToString() ?? string.Empty,
                        SaleDate = Convert.ToDateTime(row["SaleDate"]),
                        TotalAmount = Convert.ToDecimal(row["TotalAmount"])
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting sales: {ex.Message}");
                throw;
            }
            
            return sales;
        }

        public async Task<List<Sale>> GetSalesInRangeAsync(DateTime startDate, DateTime endDate)
        {
            var sales = new List<Sale>();
            
            try
            {
                string query = @"
                    SELECT s.Id, s.UserId, u.Username, s.SaleDate, s.TotalAmount 
                    FROM Sales s
                    JOIN [User] u ON s.UserId = u.Id
                    WHERE s.SaleDate BETWEEN @StartDate AND @EndDate
                    ORDER BY s.SaleDate DESC";
                
                var parameters = new[]
                {
                    _connection.CreateParameter("@StartDate", startDate),
                    _connection.CreateParameter("@EndDate", endDate)
                };
                
                var dataTable = await _connection.ExecuteQueryAsync(query, CommandType.Text, parameters);
                
                foreach (DataRow row in dataTable.Rows)
                {
                    sales.Add(new Sale
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        UserId = Convert.ToInt32(row["UserId"]),
                        Username = row["Username"].ToString() ?? string.Empty,
                        SaleDate = Convert.ToDateTime(row["SaleDate"]),
                        TotalAmount = Convert.ToDecimal(row["TotalAmount"])
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting sales in range: {ex.Message}");
                throw;
            }
            
            return sales;
        }

        public async Task<Sale?> GetSaleByIdAsync(int id)
        {
            try
            {
                string query = @"
                    SELECT s.Id, s.UserId, u.Username, s.SaleDate, s.TotalAmount 
                    FROM Sales s
                    JOIN [User] u ON s.UserId = u.Id
                    WHERE s.Id = @Id";
                
                var parameters = new[]
                {
                    _connection.CreateParameter("@Id", id)
                };
                
                var dataTable = await _connection.ExecuteQueryAsync(query, CommandType.Text, parameters);
                
                if (dataTable.Rows.Count > 0)
                {
                    var row = dataTable.Rows[0];
                    var sale = new Sale
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        UserId = Convert.ToInt32(row["UserId"]),
                        Username = row["Username"].ToString() ?? string.Empty,
                        SaleDate = Convert.ToDateTime(row["SaleDate"]),
                        TotalAmount = Convert.ToDecimal(row["TotalAmount"])
                    };
                    
                    // Get sale items
                    sale.Items = await GetSaleItemsAsync(sale.Id);
                    
                    return sale;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting sale by ID: {ex.Message}");
                throw;
            }
            
            return null;
        }

        public async Task<List<SaleItem>> GetSaleItemsAsync(int saleId)
        {
            var items = new List<SaleItem>();
            
            try
            {
                string query = @"
                    SELECT si.Id, si.SaleId, si.ProductId, p.Name AS ProductName, 
                           si.Quantity, si.PricePerUnit
                    FROM SaleItem si
                    JOIN Product p ON si.ProductId = p.Id
                    WHERE si.SaleId = @SaleId";
                
                var parameters = new[]
                {
                    _connection.CreateParameter("@SaleId", saleId)
                };
                
                var dataTable = await _connection.ExecuteQueryAsync(query, CommandType.Text, parameters);
                
                foreach (DataRow row in dataTable.Rows)
                {
                    items.Add(new SaleItem
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        SaleId = Convert.ToInt32(row["SaleId"]),
                        ProductId = Convert.ToInt32(row["ProductId"]),
                        ProductName = row["ProductName"].ToString() ?? string.Empty,
                        Quantity = Convert.ToInt32(row["Quantity"]),
                        PricePerUnit = Convert.ToDecimal(row["PricePerUnit"])
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting sale items: {ex.Message}");
                throw;
            }
            
            return items;
        }

        public async Task<bool> AddSaleAsync(Sale sale)
        {
            SqlConnection? connection = null;
            SqlTransaction? transaction = null;
            
            try
            {
                // Open connection and start transaction
                connection = await _connection.OpenConnectionAsync();
                transaction = connection.BeginTransaction();
                
                // Insert sale record
                string saleQuery = @"
                    INSERT INTO Sales (UserId, SaleDate, TotalAmount)
                    VALUES (@UserId, @SaleDate, @TotalAmount);
                    SELECT SCOPE_IDENTITY();
                ";
                
                var saleParameters = new[]
                {
                    _connection.CreateParameter("@UserId", sale.UserId),
                    _connection.CreateParameter("@SaleDate", sale.SaleDate),
                    _connection.CreateParameter("@TotalAmount", sale.TotalAmount)
                };
                
                // Execute scalar with manual transaction
                using (SqlCommand cmd = new SqlCommand(saleQuery, connection, transaction))
                {
                    cmd.Parameters.AddRange(saleParameters);
                    var saleIdResult = await cmd.ExecuteScalarAsync();
                    int saleId = Convert.ToInt32(saleIdResult);
                    
                    // Insert sale items
                    foreach (var item in sale.Items)
                    {
                        string itemQuery = @"
                            INSERT INTO SaleItem (SaleId, ProductId, Quantity, PricePerUnit)
                            VALUES (@SaleId, @ProductId, @Quantity, @PricePerUnit);
                        ";
                        
                        using (SqlCommand itemCmd = new SqlCommand(itemQuery, connection, transaction))
                        {
                            itemCmd.Parameters.AddWithValue("@SaleId", saleId);
                            itemCmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                            itemCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                            itemCmd.Parameters.AddWithValue("@PricePerUnit", item.PricePerUnit);
                            
                            await itemCmd.ExecuteNonQueryAsync();
                        }
                        
                        // Update product stock
                        await UpdateProductStockAsync(item.ProductId, -item.Quantity, connection, transaction);
                    }
                }
                
                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding sale: {ex.Message}");
                transaction?.Rollback();
                return false;
            }
            finally
            {
                await _connection.CloseConnectionAsync(connection);
            }
        }

        public async Task<bool> UpdateSaleAsync(Sale sale)
        {
            SqlConnection? connection = null;
            SqlTransaction? transaction = null;
            
            try
            {
                // Open connection and start transaction
                connection = await _connection.OpenConnectionAsync();
                transaction = connection.BeginTransaction();
                
                // Get original sale to restore stock
                var originalSale = await GetSaleByIdAsync(sale.Id);
                if (originalSale == null)
                    return false;
                
                // Restore original stock quantities
                foreach (var item in originalSale.Items)
                {
                    await UpdateProductStockAsync(item.ProductId, item.Quantity, connection, transaction);
                }
                
                // Update sale record
                string saleQuery = @"
                    UPDATE Sales
                    SET UserId = @UserId, 
                        SaleDate = @SaleDate, 
                        TotalAmount = @TotalAmount
                    WHERE Id = @Id;
                ";
                
                using (SqlCommand cmd = new SqlCommand(saleQuery, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@Id", sale.Id);
                    cmd.Parameters.AddWithValue("@UserId", sale.UserId);
                    cmd.Parameters.AddWithValue("@SaleDate", sale.SaleDate);
                    cmd.Parameters.AddWithValue("@TotalAmount", sale.TotalAmount);
                    
                    await cmd.ExecuteNonQueryAsync();
                }
                
                // Delete existing sale items
                string deleteItemsQuery = "DELETE FROM SaleItem WHERE SaleId = @SaleId";
                using (SqlCommand deleteCmd = new SqlCommand(deleteItemsQuery, connection, transaction))
                {
                    deleteCmd.Parameters.AddWithValue("@SaleId", sale.Id);
                    await deleteCmd.ExecuteNonQueryAsync();
                }
                
                // Insert updated sale items
                foreach (var item in sale.Items)
                {
                    string itemQuery = @"
                        INSERT INTO SaleItem (SaleId, ProductId, Quantity, PricePerUnit)
                        VALUES (@SaleId, @ProductId, @Quantity, @PricePerUnit);
                    ";
                    
                    using (SqlCommand itemCmd = new SqlCommand(itemQuery, connection, transaction))
                    {
                        itemCmd.Parameters.AddWithValue("@SaleId", sale.Id);
                        itemCmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                        itemCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                        itemCmd.Parameters.AddWithValue("@PricePerUnit", item.PricePerUnit);
                        
                        await itemCmd.ExecuteNonQueryAsync();
                    }
                    
                    // Update product stock with new quantities
                    await UpdateProductStockAsync(item.ProductId, -item.Quantity, connection, transaction);
                }
                
                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating sale: {ex.Message}");
                transaction?.Rollback();
                return false;
            }
            finally
            {
                await _connection.CloseConnectionAsync(connection);
            }
        }

        public async Task<bool> DeleteSaleAsync(int id)
        {
            SqlConnection? connection = null;
            SqlTransaction? transaction = null;
            
            try
            {
                // Open connection and start transaction
                connection = await _connection.OpenConnectionAsync();
                transaction = connection.BeginTransaction();
                
                // Get sale items to restore stock
                var saleItems = await GetSaleItemsAsync(id);
                
                // Restore stock quantities
                foreach (var item in saleItems)
                {
                    await UpdateProductStockAsync(item.ProductId, item.Quantity, connection, transaction);
                }
                
                // Delete sale items
                string deleteItemsQuery = "DELETE FROM SaleItem WHERE SaleId = @SaleId";
                using (SqlCommand deleteItemsCmd = new SqlCommand(deleteItemsQuery, connection, transaction))
                {
                    deleteItemsCmd.Parameters.AddWithValue("@SaleId", id);
                    await deleteItemsCmd.ExecuteNonQueryAsync();
                }
                
                // Delete sale
                string deleteSaleQuery = "DELETE FROM Sales WHERE Id = @Id";
                using (SqlCommand deleteSaleCmd = new SqlCommand(deleteSaleQuery, connection, transaction))
                {
                    deleteSaleCmd.Parameters.AddWithValue("@Id", id);
                    await deleteSaleCmd.ExecuteNonQueryAsync();
                }
                
                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting sale: {ex.Message}");
                transaction?.Rollback();
                return false;
            }
            finally
            {
                await _connection.CloseConnectionAsync(connection);
            }
        }

        // Helper method to update product stock
        private async Task<bool> UpdateProductStockAsync(int productId, int quantityChange, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                string query = @"
                    UPDATE Product
                    SET Stock = Stock + @QuantityChange
                    WHERE Id = @ProductId;
                ";
                
                using (SqlCommand cmd = new SqlCommand(query, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@ProductId", productId);
                    cmd.Parameters.AddWithValue("@QuantityChange", quantityChange);
                    
                    await cmd.ExecuteNonQueryAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating product stock: {ex.Message}");
                throw;
            }
        }
        public async Task<bool> CreateSaleAsync(Sale sale)
        {
            return await AddSaleAsync(sale);
        }
    }
}