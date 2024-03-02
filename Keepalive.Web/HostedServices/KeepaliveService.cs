
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
        private readonly Timer timer;
        private readonly AutoResetEvent autoResetEvent = new(false);
        private readonly IServiceProvider serviceProvider;
        private readonly TimeSpan timerInterval = TimeSpan.FromMinutes(1);
        private readonly ILogger<KeepaliveService> logger;

        public KeepaliveService(ServiceConfig serviceConfig, IServiceProvider serviceProvider, ILogger<KeepaliveService> logger)
        {
            this.serviceConfig = serviceConfig;
            this.serviceProvider = serviceProvider;
            this.logger = logger;
            timer = new Timer(async (object? stateInfo) => await TimerCallbackAsync(stateInfo), autoResetEvent, Timeout.InfiniteTimeSpan, timerInterval);
        }

        public override void Dispose()
        {
            timer.Dispose();
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

        private async Task TimerCallbackAsync(object? stateInfo)
        {
            ArgumentNullException.ThrowIfNull(stateInfo);
            var eventState = (AutoResetEvent)stateInfo;
            try
            {
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<KeepaliveContext>();
                await SendKeepaliveAsync(context);
            }
            catch (Exception ex)
            {
                logger.LogError("TimerCallbackAsync exception: {ex}", ex);
            }
            finally
            {
                logger.LogDebug("TimerCallbackAsync completed.");
                eventState.Set();
            }
        }

        private async Task SendKeepaliveAsync(KeepaliveContext context)
        {
            logger.LogDebug("SendKeepalive start.");
            var userQuery =
                from user in context.Users
                select user;

            await userQuery.ForEachAsync(async user =>
            {
                logger.LogInformation("Check {0} with miss count: {1}.", user.Name, user.MissCount);
                var latestRecord = (
                    from record in context.KeepaliveRecords
                    where record.UserId == user.Id
                    orderby record.CheckTimestamp descending
                    select record).FirstOrDefault();

                if (latestRecord is null || NeedCheck(user, latestRecord))
                {
                    if (user.MissCount >= user.MissThreshold)
                    {
                        await SendEmergencyEmailAsync(user);
                    }
                    else
                    {
                        user.MissCount += 1;
                        context.Users.Update(user);
                        await context.SaveChangesAsync();
                        await SendCheckEmailAsync(user, context);
                    }
                }
            });

            logger.LogDebug($"SendKeepalive completed.");
        }

        private static bool NeedCheck(User user, KeepaliveRecord record) => DateTimeOffset.UtcNow.AddDays(-user.CheckIntervalDays).ToUnixTimeSeconds() > record.CheckTimestamp;

        private async Task SendCheckEmailAsync(User user, KeepaliveContext context)
        {
            try
            {
                var record = new KeepaliveRecord { UserId = user.Id };
                context.KeepaliveRecords.Add(record);
                await context.SaveChangesAsync();

                using var scope = serviceProvider.CreateScope();
                var smtpCilent = scope.ServiceProvider.GetRequiredService<SmtpClient>();

                string bodyTemplate = """
                <html>
                <h1>Keepalive check</h1>
                <body>
                <p>Record Id: {0}.</p>
                <a href="{1}">Keepalive</a>
                </body>
                </html>
                """;
                var baseUri = new Uri(serviceConfig.Host);
                var mail = new MailMessage(from: serviceConfig.Email.EmailAddress, to: user.Email)
                {
                    Subject = $"[{user.MissCount}/3] Keepalive check",
                    Body = string.Format(bodyTemplate, record.Id, new Uri(baseUri, $"keepalive/{record.Id}").AbsoluteUri),
                    IsBodyHtml = true
                };

                await smtpCilent.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to send check email to {name}[{email}]. Exception: {ex}", user.Name, user.Email, ex);
            }

            logger.LogInformation("Send check email to user {name}[{email}].", user.Name, user.Email);
        }

        private async Task SendEmergencyEmailAsync(User user)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var smtpCilent = scope.ServiceProvider.GetRequiredService<SmtpClient>();
                var mail = new MailMessage(from: serviceConfig.Email.EmailAddress, to: user.EmergencyEmail)
                {
                    Subject = $"[Emergency] {user.Name} is missing",
                    Body = user.EmergencyEmailBody
                };

                await smtpCilent.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to send emergency email to {name}[{email}]. Exception: {ex}", user.Name, user.EmergencyEmail, ex);
            }

            logger.LogInformation("Send emerency email to user {name}[{email}].", user.Name, user.EmergencyEmail);
        }
    }
}