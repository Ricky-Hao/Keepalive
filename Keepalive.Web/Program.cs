using Keepalive.Configs;
using Keepalive.Database.Data;
using Keepalive.Web.Components;
using Keepalive.Web.HostedServices;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Keepalive.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        var serviceConfig = builder.Configuration.GetSection("Service").Get<ServiceConfig>() ?? throw new Exception($"Service Config is null.");
        builder.Services
            .AddSingleton<ServiceConfig>(serviceConfig)
            .AddDbContextFactory<KeepaliveContext>(options => options.UseSqlite($"Data Source={serviceConfig.Database}"))
            .AddDbContext<KeepaliveContext>(options => options.UseSqlite($"Data Source={serviceConfig.Database}"))
            .AddHostedService<KeepaliveService>()
            .AddRazorComponents()
            .AddInteractiveServerComponents();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        else
        {
            app.UseDeveloperExceptionPage();
        }

        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;

            var context = services.GetRequiredService<KeepaliveContext>();
            context.Database.EnsureCreated();
            KeepaliveInitialize.Initialize(context);
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}
