using Donacerca.DTOs;
using Donacerca.Models;
using Google.Cloud.Firestore;

namespace Donacerca.Services;

public class DeliveryService
{
    private readonly FirebaseService _firebase;
    private readonly NotificationService _notificationService;

    public DeliveryService(FirebaseService firebase, NotificationService notificationService)
    {
        _firebase = firebase;
        _notificationService = notificationService;
    }

    public async Task<DeliveryRecord> CreateDeliveryAsync(CreateDeliveryDto dto, string donorId)
    {
        var postSnap = await _firebase.GetCollection("donationPosts").Document(dto.PostId).GetSnapshotAsync();
        if (!postSnap.Exists) throw new KeyNotFoundException("Publicación no encontrada");

        var postData = postSnap.ToDictionary();
        if (postData["DonorId"].ToString() != donorId)
            throw new UnauthorizedAccessException("Solo el donante puede registrar la entrega");

        if (postData["Status"].ToString() != "reservado")
            throw new InvalidOperationException("El artículo debe estar reservado para registrar una entrega");

        var record = new DeliveryRecord
        {
            PostId = dto.PostId,
            DonorId = donorId,
            ReceiverId = postData["SelectedReceiverId"].ToString()!,
            ItemName = postData["ItemName"].ToString()!,
            CategoryId = postData["CategoryId"].ToString()!,
            DeliveryDate = dto.DeliveryDate,
            DeliveryLocation = dto.DeliveryLocation
        };

        await _firebase.GetCollection("deliveryRecords").Document(record.Id).SetAsync(ToDict(record));
        return record;
    }

    public async Task ConfirmByDonorAsync(string postId, string donorId)
    {
        var record = await GetByPostIdAsync(postId)
            ?? throw new KeyNotFoundException("Registro de entrega no encontrado");

        if (record.DonorId != donorId)
            throw new UnauthorizedAccessException("Solo el donante puede confirmar esta entrega");

        await _firebase.GetCollection("deliveryRecords").Document(record.Id)
            .UpdateAsync(new Dictionary<string, object> { { "ConfirmedByDonor", true } });

        await _firebase.GetCollection("donationPosts").Document(postId)
            .UpdateAsync(new Dictionary<string, object> { { "Status", "entregado" } });
    }

    public async Task ConfirmByReceiverAsync(string postId, string receiverId)
    {
        var record = await GetByPostIdAsync(postId)
            ?? throw new KeyNotFoundException("Registro de entrega no encontrado");

        if (record.ReceiverId != receiverId)
            throw new UnauthorizedAccessException("Solo el receptor puede confirmar la recepción");

        if (!record.ConfirmedByDonor)
            throw new InvalidOperationException("El donante aún no ha confirmado la entrega");

        await _firebase.GetCollection("deliveryRecords").Document(record.Id)
            .UpdateAsync(new Dictionary<string, object>
            {
                { "ConfirmedByReceiver", true },
                { "CompletedAt", DateTime.UtcNow }
            });

        await _firebase.GetCollection("donationPosts").Document(postId)
            .UpdateAsync(new Dictionary<string, object>
            {
                { "Status", "entregado" },
                { "IsActive", false },
                { "ClosedAt", DateTime.UtcNow }
            });

        // Notificar al donante que la donación se completó exitosamente
        await _notificationService.CreateAsync(
            record.DonorId,
            "completed",
            $"🎉 ¡Donación completada! El receptor confirmó que recibió '{record.ItemName}' exitosamente.",
            postId
        );
    }

    public async Task<DeliveryRecord?> GetByPostIdAsync(string postId)
    {
        var snap = await _firebase.GetCollection("deliveryRecords")
            .WhereEqualTo("PostId", postId)
            .GetSnapshotAsync();
        return snap.Count > 0 ? MapRecord(snap.Documents[0]) : null;
    }

    public async Task<List<DeliveryRecord>> GetAllCompletedAsync()
    {
        var snap = await _firebase.GetCollection("deliveryRecords")
            .WhereEqualTo("ConfirmedByReceiver", true)
            .GetSnapshotAsync();

        var records = snap.Documents.Select(MapRecord).ToList();

        foreach (var record in records)
        {
            var donorDoc = await _firebase.GetCollection("users").Document(record.DonorId).GetSnapshotAsync();
            if (donorDoc.Exists)
                record.DonorName = donorDoc.ToDictionary()["FullName"].ToString()!;

            var receiverDoc = await _firebase.GetCollection("users").Document(record.ReceiverId).GetSnapshotAsync();
            if (receiverDoc.Exists)
                record.ReceiverName = receiverDoc.ToDictionary()["FullName"].ToString()!;
        }

        return records;
    }

    private static DeliveryRecord MapRecord(DocumentSnapshot doc)
    {
        var d = doc.ToDictionary();

        DateTime? completedAt = null;
        if (d.ContainsKey("CompletedAt") && d["CompletedAt"] is Timestamp ts)
            completedAt = ts.ToDateTime();

        return new DeliveryRecord
        {
            Id = d["Id"].ToString()!,
            PostId = d["PostId"].ToString()!,
            DonorId = d["DonorId"].ToString()!,
            ReceiverId = d["ReceiverId"].ToString()!,
            ItemName = d["ItemName"].ToString()!,
            CategoryId = d["CategoryId"].ToString()!,
            DeliveryDate = ((Timestamp)d["DeliveryDate"]).ToDateTime(),
            DeliveryLocation = d["DeliveryLocation"].ToString()!,
            ConfirmedByDonor = (bool)d["ConfirmedByDonor"],
            ConfirmedByReceiver = (bool)d["ConfirmedByReceiver"],
            CompletedAt = completedAt
        };
    }

    private static Dictionary<string, object> ToDict(DeliveryRecord r) => new()
    {
        { "Id", r.Id },
        { "PostId", r.PostId },
        { "DonorId", r.DonorId },
        { "ReceiverId", r.ReceiverId },
        { "ItemName", r.ItemName },
        { "CategoryId", r.CategoryId },
        { "DeliveryDate", r.DeliveryDate },
        { "DeliveryLocation", r.DeliveryLocation },
        { "ConfirmedByDonor", r.ConfirmedByDonor },
        { "ConfirmedByReceiver", r.ConfirmedByReceiver }
    };
}