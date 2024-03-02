
namespace Keepalive.Web.HostedServices
{
    using System.Runtime.ExceptionServices;
    using System.Threading;
    using Keepalive.Configs;
    using Keepalive.Database.Data;
    using Keepalive.Database.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Internal;
    using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
    using Microsoft.Extensions.Options;

    class KeepaliveService : BackgroundService
    {
        private readonly ServiceConfig serviceConfig;
        private readonly KeepaliveContext keepaliveContext;
        private readonly Timer timer;
        private readonly AutoResetEvent autoResetEvent = new (false);

        public KeepaliveService(ServiceConfig serviceConfig, IDbContextFactory<KeepaliveContext> dbContextFactory)
        {
            this.serviceConfig = serviceConfig;
            keepaliveContext = dbContextFactory.CreateDbContext();
            timer = new Timer(SendKeepalive, autoResetEvent, Timeout.InfiniteTimeSpan, TimeSpan.FromSeconds(10));
        }

        public override void Dispose()
        {
            timer.Dispose();
            keepaliveContext.Dispose();
            base.Dispose();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(10));
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

            userQuery.ForEachAsync(async uesr => {
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
            await Task.Yield();
            Console.WriteLine($"Send check email to {user.Email}");
        }
    }
}