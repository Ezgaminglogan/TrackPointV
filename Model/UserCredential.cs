using System;

namespace TrackPointV.Model
{
    public class UserCredential
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsAuthenticated { get; set; }
        public DateTime? LastLogin { get; set; }
    }
} 