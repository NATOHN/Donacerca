using Google.Cloud.Firestore;

namespace Donacerca.Services;

public class ReportService
{
    private readonly FirebaseService _firebase;

    public ReportService(FirebaseService firebase)
    {
        _firebase = firebase;
    }

    public async Task<object> GetDashboardStatsAsync()
    {
        var postsSnap = await _firebase.GetCollection("donationPosts").GetSnapshotAsync();
        var requestsSnap = await _firebase.GetCollection("donationRequests").GetSnapshotAsync();
        var deliveriesSnap = await _firebase.GetCollection("deliveryRecords")
            .WhereEqualTo("ConfirmedByReceiver", true).GetSnapshotAsync();

        var posts = postsSnap.Documents.Select(d => d.ToDictionary()).ToList();
        var active = posts.Count(p => p["Status"].ToString() == "disponible" && (bool)p["IsActive"]);
        var completed = deliveriesSnap.Count;
        var pending = requestsSnap.Documents.Count(d => d.ToDictionary()["Status"].ToString() == "pendiente");
        var total = posts.Count;

        // Distribución por estado
        var byStatus = posts
            .GroupBy(p => p["Status"].ToString())
            .ToDictionary(g => g.Key!, g => g.Count());

        // Por categoría
        var byCategory = posts
            .GroupBy(p => p["CategoryId"].ToString())
            .ToDictionary(g => g.Key!, g => g.Count());

        return new
        {
            ActivePosts = active,
            CompletedDonations = completed,
            PendingRequests = pending,
            TotalPosts = total,
            CompletionRate = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0,
            ByStatus = byStatus,
            ByCategory = byCategory
        };
    }

    public async Task<object> GetTrendAsync(string period = "week")
    {
        var snap = await _firebase.GetCollection("deliveryRecords")
            .WhereEqualTo("ConfirmedByReceiver", true)
            .GetSnapshotAsync();

        var records = snap.Documents
            .Select(d => d.ToDictionary())
            .Where(d => d.ContainsKey("CompletedAt") && d["CompletedAt"] != null)
            .ToList();

        if (period == "week")
        {
            var trend = records
                .GroupBy(d => ((Google.Cloud.Firestore.Timestamp)d["CompletedAt"]).ToDateTime().ToString("yyyy-MM-dd"))
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToList();
            return trend;
        }
        else
        {
            var trend = records
                .GroupBy(d => ((Google.Cloud.Firestore.Timestamp)d["CompletedAt"]).ToDateTime().ToString("yyyy-MM"))
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .OrderBy(x => x.Month)
                .ToList();
            return trend;
        }
    }
}