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
        // Revisar cada 24 horas
        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckExpiredPosts();
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task CheckExpiredPosts()
    {
        using var scope = _services.CreateScope();
        var firebase = scope.ServiceProvider.GetRequiredService<FirebaseService>();

        var cutoff = DateTime.UtcNow.AddDays(-30);
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
            await firebase.GetCollection("donationPosts").Document(doc.Id)
                .UpdateAsync(new Dictionary<string, object>
                {
                    { "Status", "vencido" },
                    { "IsActive", false }
                });
            _logger.LogInformation("Publicación vencida: {Id}", doc.Id);
        }

        _logger.LogInformation("Job de vencimiento ejecutado. {Count} publicaciones vencidas.", expired.Count);
    }
}