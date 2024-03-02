using Keepalive.Database.Models;

namespace Keepalive.Database.Data
{
    public static class KeepaliveInitialize
    {
        public static void Initialize(KeepaliveContext context)
        {
            if (context.Users.Any())
            {
                return;
            }

            var users = new User[]
            {
                new User{Name="RickyHao", Email="a959695@live.com", CheckIntervalDays=7, EmergencyEmail="root@rickyhao.com", EmergencyEmailBody="TestBody", MissCount=0},
            };
            context.Users.AddRange(users);
            context.SaveChanges();
        }
    }
}