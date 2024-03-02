namespace Keepalive.Web.Configs
{
    public class SmtpConfig
    {
        public required string Host { get; set; }

        public int Port { get; set; }

        public bool SSLEnable { get; set; } = true;

        public required string Username { get; set; }

        public required string Password { get; set; }

        public required string EmailAddress { get; set; }

        public void Validate()
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(Host);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(Username);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(Password);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(EmailAddress);

            if (Port <= 0 || Port > 65535)
                throw new ArgumentNullException(nameof(Host));
        }
    }
}