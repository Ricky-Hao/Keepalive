namespace Keepalive.Configs
{
    public class SmtpConfig
    {
        public string Host { get; set; } = null!;

        public int Port { get; set; }

        public string Username { get; set; } = null!;

        public string Password { get; set; } = null!;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Host))
                throw new ArgumentNullException(nameof(Host));

            if (Port <= 0 || Port > 65535)
                throw new ArgumentNullException(nameof(Host));

            if (string.IsNullOrWhiteSpace(Username))
                throw new ArgumentNullException(nameof(Username));

            if (string.IsNullOrWhiteSpace(Password))
                throw new ArgumentNullException(nameof(Password));
        }
    }
}