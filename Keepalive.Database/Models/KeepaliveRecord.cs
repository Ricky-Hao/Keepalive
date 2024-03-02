namespace Keepalive.Database.Models
{
    public class KeepaliveRecord
    {
        public Guid Id { get; set; }

        public required Guid UserId { get; set; }

        public double CreateTimestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public double CheckTimestamp { get; set; } = 0;
    }
}