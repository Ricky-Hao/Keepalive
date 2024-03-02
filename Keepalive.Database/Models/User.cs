using System.ComponentModel.DataAnnotations;

namespace Keepalive.Database.Models
{
    public class User
    {
        public Guid Id { get; set; }

        [Required]
        public required string Name { get; set; }

        [Required]
        public required string Email { get; set; }

        [Required]
        public required string EmergencyEmail { get; set; }

        [Required]
        public required string EmergencyEmailBody { get; set; }

        [Required]
        public required int CheckIntervalDays { get; set; } = 7;

        public int MissCount { get; set; } = 0;

        public int MissThreshold { get; set; } = 3;
    }
}