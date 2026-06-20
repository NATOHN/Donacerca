namespace Donacerca.Services;

public class ExpiredPostsJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ExpiredPostsJob> _logger;

    public ExpiredPostsJob(IServiceProvider services, ILogger<ExpiredPostsJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckExpiredPosts();
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task CheckExpiredPosts()
    {
        using var scope = _services.CreateScope();
        var firebase = scope.ServiceProvider.GetRequiredService<FirebaseService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();

        var cutoff = DateTime.UtcNow.AddDays(-2);
        var snap = await firebase.GetCollection("donationPosts")
            .WhereEqualTo("Status", "disponible")
            .WhereEqualTo("IsActive", true)
            .GetSnapshotAsync();

        var expired = snap.Documents
            .Where(d =>
            {
                var data = d.ToDictionary();
                var createdAt = ((Google.Cloud.Firestore.Timestamp)data["CreatedAt"]).ToDateTime();
                return createdAt < cutoff;
            }).ToList();

        foreach (var doc in expired)
        {
            var data = doc.ToDictionary();
            var donorId = data["DonorId"].ToString()!;
            var itemName = data["ItemName"].ToString()!;
            var postId = data["Id"].ToString()!;

            await firebase.GetCollection("donationPosts").Document(doc.Id)
                .UpdateAsync(new Dictionary<string, object>
                {
                    { "Status", "vencido" },
                    { "IsActive", false }
                });

            await notificationService.CreateAsync(
                donorId,
                "expired",
                $"Tu publicación '{itemName}' ha vencido. Puedes renovarla o cerrarla desde Mis publicaciones.",
                postId
            );

            _logger.LogInformation("Publicación vencida: {Id}", doc.Id);
        }

        _logger.LogInformation("Job ejecutado. {Count} publicaciones vencidas.", expired.Count);
    }
}