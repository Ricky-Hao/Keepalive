namespace Keepalive.Database.Models
{
    public class KeepaliveRecord
    {
        public Guid Id { get; set; }

        public required Guid UserId { get; set; }

        public long CreateTimestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public long CheckTimestamp { get; set; } = 0;
    }
}