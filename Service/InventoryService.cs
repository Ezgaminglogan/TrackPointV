using System;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using TrackPointV.Model;

namespace TrackPointV.Service
{
    public class InventoryService
    {
        private readonly Connection _connection;

        public InventoryService()
        {
            _connection = new Connection();
        }

        public async Task<int> GetTotalInventoryCountAsync()
        {
            try
            {
                string query = "SELECT SUM(Stock) FROM [Product]";
                var result = await _connection.ExecuteScalarAsync(query);
                return result != DBNull.Value ? Convert.ToInt32(result) : 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get total inventory count", ex);
            }
        }

        public async Task<int> GetLowStockItemsCountAsync(int threshold = 10)
        {
            try
            {
                string query = "SELECT COUNT(*) FROM [Product] WHERE Stock <= @Threshold";
                var parameters = new[]
                {
                    _connection.CreateParameter("@Threshold", threshold)
                };
                var result = await _connection.ExecuteScalarAsync(query, CommandType.Text, parameters);
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get low stock items count", ex);
            }
        }

        public async Task<decimal> GetTodaySalesAmountAsync()
        {
            try
            {
                string query = "SELECT ISNULL(SUM(TotalAmount), 0) FROM [Sales] WHERE CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE)";
                var result = await _connection.ExecuteScalarAsync(query);
                return result != DBNull.Value ? Convert.ToDecimal(result) : 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get today's sales amount", ex);
            }
        }

        public async Task<int> GetTodayTransactionsCountAsync()
        {
            try
            {
                string query = "SELECT COUNT(*) FROM [Sales] WHERE CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE)";
                var result = await _connection.ExecuteScalarAsync(query);
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get today's transaction count", ex);
            }
        }

        // Add these methods to your existing InventoryService class

        public async Task<List<Product>> GetAllProductsAsync()
        {
            try
            {
                string query = "SELECT * FROM [Product] ORDER BY DateUploaded DESC";
                var dataTable = await _connection.ExecuteQueryAsync(query);
                
                List<Product> products = new List<Product>();
                
                foreach (DataRow row in dataTable.Rows)
                {
                    products.Add(new Product
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        Name = row["Name"].ToString(),
                        Price = Convert.ToDecimal(row["Price"]),
                        Description = row["Description"].ToString(),
                        Stock = Convert.ToInt32(row["Stock"]),
                        DateUploaded = Convert.ToDateTime(row["DateUploaded"])
                    });
                }
                
                return products;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get products", ex);
            }
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            try
            {
                string query = "SELECT * FROM [Product] WHERE Id = @Id";
                var parameters = new[]
                {
                    _connection.CreateParameter("@Id", id)
                };
                
                var dataTable = await _connection.ExecuteQueryAsync(query, CommandType.Text, parameters);
                
                if (dataTable.Rows.Count == 0)
                    return null;
                    
                DataRow row = dataTable.Rows[0];
                
                return new Product
                {
                    Id = Convert.ToInt32(row["Id"]),
                    Name = row["Name"].ToString(),
                    Price = Convert.ToDecimal(row["Price"]),
                    Description = row["Description"].ToString(),
                    Stock = Convert.ToInt32(row["Stock"]),
                    DateUploaded = Convert.ToDateTime(row["DateUploaded"])
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get product with ID {id}", ex);
            }
        }

        public async Task<bool> AddProductAsync(Product product)
        {
            try
            {
                string query = @"INSERT INTO [Product] (Name, Price, Description, Stock, DateUploaded) 
                                VALUES (@Name, @Price, @Description, @Stock, @DateUploaded)";

                var parameters = new[]
                {
                    _connection.CreateParameter("@Name", product.Name ?? string.Empty),
                    _connection.CreateParameter("@Price", product.Price),
                    _connection.CreateParameter("@Description", product.Description ?? string.Empty),
                    _connection.CreateParameter("@Stock", product.Stock),
                    _connection.CreateParameter("@DateUploaded", product.DateUploaded)
                };

                int rowsAffected = await _connection.ExecuteNonQueryAsync(query, CommandType.Text, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to add product", ex);
            }
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            try
            {
                string query = @"UPDATE [Product] 
                                SET Name = @Name, 
                                    Price = @Price, 
                                    Description = @Description, 
                                    Stock = @Stock 
                                WHERE Id = @Id";

                var parameters = new[]
                {
                    _connection.CreateParameter("@Id", product.Id),
                    _connection.CreateParameter("@Name", product.Name ?? string.Empty),
                    _connection.CreateParameter("@Price", product.Price),
                    _connection.CreateParameter("@Description", product.Description ?? string.Empty),
                    _connection.CreateParameter("@Stock", product.Stock)
                };

                int rowsAffected = await _connection.ExecuteNonQueryAsync(query, CommandType.Text, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update product with ID {product.Id}", ex);
            }
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            try
            {
                // First check if product is referenced in SaleItem
                string checkQuery = "SELECT COUNT(*) FROM [SaleItem] WHERE ProductId = @Id";
                var checkParameters = new[]
                {
                    _connection.CreateParameter("@Id", id)
                };
                
                var result = await _connection.ExecuteScalarAsync(checkQuery, CommandType.Text, checkParameters);
                int referenceCount = Convert.ToInt32(result);
                
                if (referenceCount > 0)
                {
                    throw new Exception("Cannot delete product because it is referenced in sales records");
                }
                
                // If no references, proceed with deletion
                string deleteQuery = "DELETE FROM [Product] WHERE Id = @Id";
                var deleteParameters = new[]
                {
                    _connection.CreateParameter("@Id", id)
                };
                
                int rowsAffected = await _connection.ExecuteNonQueryAsync(deleteQuery, CommandType.Text, deleteParameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete product with ID {id}", ex);
            }
        }

        public async Task<ProductSalesStatistics> GetProductSalesStatisticsAsync(int productId)
        {
            try
            {
                string query = @"SELECT 
                                    COUNT(*) AS SalesCount,
                                    ISNULL(SUM(si.Quantity * si.PricePerUnit), 0) AS TotalRevenue
                                FROM [SaleItem] si
                                WHERE si.ProductId = @ProductId";
                
                var parameters = new[]
                {
                    _connection.CreateParameter("@ProductId", productId)
                };
                
                var dataTable = await _connection.ExecuteQueryAsync(query, CommandType.Text, parameters);
                
                if (dataTable.Rows.Count == 0)
                {
                    return new ProductSalesStatistics { SalesCount = 0, TotalRevenue = 0 };
                }
                
                DataRow row = dataTable.Rows[0];
                
                return new ProductSalesStatistics
                {
                    SalesCount = Convert.ToInt32(row["SalesCount"]),
                    TotalRevenue = Convert.ToDecimal(row["TotalRevenue"])
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get sales statistics for product with ID {productId}", ex);
            }
        }

        // Add this method to get recent activities from your SQL Server database
        // Modify the GetRecentActivitiesAsync method to use existing tables
        // Add this method to your InventoryService class
        public async Task<List<ActivityItem>> GetRecentActivitiesAsync(int count = 10)
        {
            var activities = new List<ActivityItem>();
            
            try
            {
                // Query that combines recent product changes, sales, and user activities
                string query = @"
                    -- Get recent product additions/updates
                    SELECT 
                        p.Id AS ItemId,
                        'Product' AS ActivityType,
                        CASE 
                            WHEN p.DateUploaded > DATEADD(MINUTE, -1, GETDATE()) THEN 'Added'
                            ELSE 'Updated'
                        END AS Action,
                        p.Name AS ItemName,
                        p.DateUploaded AS ActivityDate,
                        NULL AS Username,
                        NULL AS Amount
                    FROM Product p
                    
                    UNION ALL
                    
                    -- Get recent sales
                    SELECT 
                        s.Id AS ItemId,
                        'Sale' AS ActivityType,
                        'Completed' AS Action,
                        CONCAT('Sale #', s.Id) AS ItemName,
                        s.SaleDate AS ActivityDate,
                        u.Username AS Username,
                        s.TotalAmount AS Amount
                    FROM Sales s
                    JOIN [User] u ON s.UserId = u.Id
                    
                    UNION ALL
                    
                    -- Get recent user logins
                    SELECT 
                        u.Id AS ItemId,
                        'User' AS ActivityType,
                        'Logged in' AS Action,
                        u.Username AS ItemName,
                        u.LastLoginDate AS ActivityDate,
                        u.Username AS Username,
                        NULL AS Amount
                    FROM [User] u
                    WHERE u.LastLoginDate IS NOT NULL
                    
                    ORDER BY ActivityDate DESC
                    OFFSET 0 ROWS
                    FETCH NEXT @Count ROWS ONLY";
                    
                var parameters = new[]
                {
                    _connection.CreateParameter("@Count", count)
                };
                
                var dataTable = await _connection.ExecuteQueryAsync(query, CommandType.Text, parameters);
                
                foreach (DataRow row in dataTable.Rows)
                {
                    var activity = new ActivityItem
                    {
                        Id = Convert.ToInt32(row["ItemId"]),
                        Type = row["ActivityType"].ToString() ?? string.Empty,
                        Action = row["Action"].ToString() ?? string.Empty,
                        ItemName = row["ItemName"].ToString() ?? string.Empty,
                        Date = Convert.ToDateTime(row["ActivityDate"]),
                        Username = row["Username"] != DBNull.Value ? row["Username"].ToString() : null,
                        Amount = row["Amount"] != DBNull.Value ? Convert.ToDecimal(row["Amount"]) : null
                    };
                    
                    activities.Add(activity);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting recent activities: {ex.Message}");
                // Return empty list instead of throwing to avoid crashing the UI
            }
            
            return activities;
        }


    }

    public class Product
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public int Stock { get; set; }
        public DateTime DateUploaded { get; set; }
    }

    public class ProductSalesStatistics
    {
        public int SalesCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
