namespace Keepalive.Configs
{
    public class ServiceConfig
    {
        public required string Database { get; set; }
        public SmtpConfig Email { get; set; } = new();

        public void Validate()
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(Database);
            Email.Validate();
        }
    }
}