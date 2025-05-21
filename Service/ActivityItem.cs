using System;

namespace TrackPointV.Service
{
    public class ActivityItem
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string? Username { get; set; }
        public decimal? Amount { get; set; }
        public int? Quantity { get; set; }
        public decimal? Price { get; set; }
        public int? ProductId { get; set; }
        
        // Formatted display text for the UI
        public string DisplayText 
        { 
            get 
            {
                string text = $"{Action} {Type}: {ItemName}";
                if (!string.IsNullOrEmpty(Username))
                {
                    text += $" by {Username}";
                }
                return text;
            } 
        }
        
        // Formatted date for the UI
        public string FormattedDate => Date.ToString("MMM dd, yyyy h:mm tt");
        
        // Properties for enhanced UI display
        public bool HasAmount => Amount.HasValue && Amount.Value > 0;
        public bool HasQuantity => Quantity.HasValue && Quantity.Value > 0;
        public bool HasPrice => Price.HasValue && Price.Value > 0;
        public string FormattedAmount => HasAmount ? $"₱{Amount:N2}" : string.Empty;
        public string FormattedPrice => HasPrice ? $"₱{Price:N2}" : string.Empty;
        public string FormattedQuantity => HasQuantity ? $"{Quantity} {(Quantity == 1 ? "unit" : "units")}" : string.Empty;
    }
}