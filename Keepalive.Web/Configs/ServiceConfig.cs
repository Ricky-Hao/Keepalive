namespace Keepalive.Web.Configs
{
    public class ServiceConfig
    {
        public required string Database { get; set; }
        public required SmtpConfig Email { get; set; }

        public void Validate()
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(Database);
            Email.Validate();
        }
    }
}