using Google.Cloud.Firestore;

namespace Donacerca.Services;

public class ReportService
{
    private readonly FirebaseService _firebase;

    public ReportService(FirebaseService firebase)
    {
        _firebase = firebase;
    }

    public async Task<object> GetDashboardStatsAsync(
        string? categoryId = null,
        string? zone = null,
        DateTime? from = null,
        DateTime? to = null)
    {
        var postsSnap = await _firebase.GetCollection("donationPosts").GetSnapshotAsync();
        var requestsSnap = await _firebase.GetCollection("donationRequests").GetSnapshotAsync();
        var deliveriesSnap = await _firebase.GetCollection("deliveryRecords")
            .WhereEqualTo("ConfirmedByReceiver", true).GetSnapshotAsync();

        var posts = postsSnap.Documents
            .Select(d => d.ToDictionary())
            .Where(p =>
            {
                if (categoryId != null && p["CategoryId"].ToString() != categoryId) return false;
                if (zone != null && p["Zone"].ToString() != zone) return false;
                if (from.HasValue && p["CreatedAt"] is Timestamp ct && ct.ToDateTime() < from.Value) return false;
                if (to.HasValue && p["CreatedAt"] is Timestamp ct2 && ct2.ToDateTime() > to.Value) return false;
                return true;
            }).ToList();

        var active = posts.Count(p => p["Status"].ToString() == "disponible" && (bool)p["IsActive"]);
        var completed = deliveriesSnap.Count;
        var pending = requestsSnap.Documents.Count(d => d.ToDictionary()["Status"].ToString() == "pendiente");
        var total = posts.Count;

        var byStatus = posts
            .GroupBy(p => p["Status"].ToString())
            .ToDictionary(g => g.Key!, g => g.Count());

        var byCategory = posts
            .GroupBy(p => p["CategoryId"].ToString())
            .ToDictionary(g => g.Key!, g => g.Count());

        var byZone = posts
            .GroupBy(p => p["Zone"].ToString())
            .ToDictionary(g => g.Key!, g => g.Count());

        return new
        {
            ActivePosts = active,
            CompletedDonations = completed,
            PendingRequests = pending,
            TotalPosts = total,
            CompletionRate = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0,
            ByStatus = byStatus,
            ByCategory = byCategory,
            ByZone = byZone
        };
    }

    public async Task<object> GetTrendAsync(string period = "week")
    {
        var snap = await _firebase.GetCollection("deliveryRecords")
            .WhereEqualTo("ConfirmedByReceiver", true)
            .GetSnapshotAsync();

        // Solo incluir records donde CompletedAt es realmente un Timestamp
        var records = snap.Documents
            .Select(d => d.ToDictionary())
            .Where(d => d.ContainsKey("CompletedAt") && d["CompletedAt"] is Timestamp)
            .ToList();

        if (period == "week")
        {
            var trend = records
                .GroupBy(d => ((Timestamp)d["CompletedAt"]).ToDateTime().ToString("yyyy-MM-dd"))
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToList();
            return trend;
        }
        else
        {
            var trend = records
                .GroupBy(d => ((Timestamp)d["CompletedAt"]).ToDateTime().ToString("yyyy-MM"))
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .OrderBy(x => x.Month)
                .ToList();
            return trend;
        }
    }

}