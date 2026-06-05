using Donacerca.Services;
using Google.Cloud.Firestore;

namespace Donacerca.Services;

// Guarda notificaciones en Firestore para que el frontend las lea
public class NotificationService
{
    private readonly FirebaseService _firebase;

    public NotificationService(FirebaseService firebase)
    {
        _firebase = firebase;
    }

    public async Task CreateAsync(string userId, string type, string message, string postId = "")
    {
        var notification = new Dictionary<string, object>
        {
            { "Id", Guid.NewGuid().ToString() },
            { "UserId", userId },
            { "Type", type },       // "selected", "delivered", "expired", etc.
            { "Message", message },
            { "PostId", postId },
            { "IsRead", false },
            { "CreatedAt", DateTime.UtcNow }
        };

        await _firebase.GetCollection("notifications")
            .Document(notification["Id"].ToString()!)
            .SetAsync(notification);
    }

    public async Task<List<object>> GetByUserAsync(string userId)
    {
        var snap = await _firebase.GetCollection("notifications")
            .WhereEqualTo("UserId", userId)
            .GetSnapshotAsync();

        return snap.Documents.Select(doc =>
        {
            var d = doc.ToDictionary();
            return (object)new
            {
                Id = d["Id"].ToString(),
                Type = d["Type"].ToString(),
                Message = d["Message"].ToString(),
                PostId = d["PostId"].ToString(),
                IsRead = (bool)d["IsRead"],
                CreatedAt = ((Timestamp)d["CreatedAt"]).ToDateTime()
            };
        }).ToList();
    }

    public async Task MarkAsReadAsync(string notificationId)
    {
        await _firebase.GetCollection("notifications")
            .Document(notificationId)
            .UpdateAsync(new Dictionary<string, object> { { "IsRead", true } });
    }
}