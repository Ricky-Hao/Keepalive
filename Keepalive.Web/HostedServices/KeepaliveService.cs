
namespace Keepalive.Web.HostedServices
{
    using System.Net.Mail;
    using System.Threading;

    using Keepalive.Database.Data;
    using Keepalive.Database.Models;
    using Keepalive.Web.Configs;

    using Microsoft.EntityFrameworkCore;

    class KeepaliveService : BackgroundService
    {
        private readonly ServiceConfig serviceConfig;
        private readonly KeepaliveContext keepaliveContext;
        private readonly Timer timer;
        private readonly AutoResetEvent autoResetEvent = new(false);
        private readonly IServiceProvider serviceProvider;
        private readonly TimeSpan timerInterval = TimeSpan.FromHours(1);
        private readonly ILogger<KeepaliveService> logger;

        public KeepaliveService(ServiceConfig serviceConfig, IDbContextFactory<KeepaliveContext> dbContextFactory, IServiceProvider serviceProvider, ILogger<KeepaliveService> logger)
        {
            this.serviceConfig = serviceConfig;
            this.serviceProvider = serviceProvider;
            this.logger = logger;
            keepaliveContext = dbContextFactory.CreateDbContext();
            timer = new Timer(SendKeepalive, autoResetEvent, Timeout.InfiniteTimeSpan, timerInterval);
        }

        public override void Dispose()
        {
            timer.Dispose();
            keepaliveContext.Dispose();
            base.Dispose();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer.Change(TimeSpan.Zero, timerInterval);
            while (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine("Keepalive Service is running.");
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }

            autoResetEvent.WaitOne();
        }

        private void SendKeepalive(object? stateInfo)
        {
            ArgumentNullException.ThrowIfNull(stateInfo);
            var eventState = (AutoResetEvent)stateInfo;
            Console.WriteLine("SendKeepalive");

            var userQuery =
                from user in keepaliveContext.Users
                select user;

            userQuery.ForEachAsync(async uesr =>
            {
                Console.WriteLine($"Check {uesr.Name}");
                var latestRecord = (
                    from record in keepaliveContext.KeepaliveRecords
                    where record.Id == uesr.Id
                    orderby record.CheckTimestamp descending
                    select record).FirstOrDefault();

                if (latestRecord is null || NeedCheck(uesr, latestRecord))
                    await SendCheckEmail(uesr);
            });

            eventState.Set();
        }

        private static bool NeedCheck(User user, KeepaliveRecord record) => DateTimeOffset.UtcNow.AddDays(-user.CheckIntervalDays).ToUnixTimeSeconds() > record.CheckTimestamp;

        private async Task SendCheckEmail(User user)
        {
            try
            {
                var record = new KeepaliveRecord { UserId = user.Id };
                keepaliveContext.KeepaliveRecords.Add(record);
                await keepaliveContext.SaveChangesAsync();

                using var scope = serviceProvider.CreateScope();
                var smtpCilent = scope.ServiceProvider.GetRequiredService<SmtpClient>();
                var mail = new MailMessage(from: serviceConfig.Email.EmailAddress, to: user.Email)
                {
                    Subject = $"[{user.MissCount + 1}/3] Keepalive check",
                    Body = $"Missing count {user.MissCount}, Record: {record.Id}"
                };

                await smtpCilent.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to send email to {name}[{email}]. Exception: {ex}", user.Name, user.Email, ex);
            }

            logger.LogInformation("Sent email to user {name}[{email}].", user.Name, user.Email);
        }
    }
}