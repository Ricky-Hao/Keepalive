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
            };
            context.Users.AddRange(users);
            context.SaveChanges();
        }
    }
}