using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebAPI_FlowerShopSWP.Models;

public class FlowerVisibilityService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public FlowerVisibilityService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<FlowerEventShopsContext>();

                var currentTime = DateTime.UtcNow;
                var expiredFlowers = context.Flowers.Where(flower =>
                    flower.ListingDate.HasValue &&
                    currentTime > flower.ListingDate.Value.AddHours(24) &&
                    flower.IsVisible).ToList();

                foreach (var flower in expiredFlowers)
                {
                    flower.IsVisible = false;
                }

                await context.SaveChangesAsync();
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
