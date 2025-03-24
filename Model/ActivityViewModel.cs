using System;

namespace TrackPointV.Model
{
    public class ActivityViewModel
    {
        public string? ActivityType { get; set; }
        public int ReferenceId { get; set; }
        public string? Username { get; set; }
        public string? Details { get; set; }
        public DateTime ActivityDate { get; set; }
    }
}