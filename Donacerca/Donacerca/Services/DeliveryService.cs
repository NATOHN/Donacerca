using Donacerca.DTOs;
using Donacerca.Models;
using Google.Cloud.Firestore;

namespace Donacerca.Services;

public class DeliveryService
{
    private readonly FirebaseService _firebase;

    public DeliveryService(FirebaseService firebase)
    {
        _firebase = firebase;
    }

    // El donante registra los datos de entrega
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

    // El donante confirma que entregó
    public async Task ConfirmByDonorAsync(string postId, string donorId)
    {
        var record = await GetByPostIdAsync(postId)
            ?? throw new KeyNotFoundException("Registro de entrega no encontrado");

        if (record.DonorId != donorId)
            throw new UnauthorizedAccessException("Solo el donante puede confirmar esta entrega");

        await _firebase.GetCollection("deliveryRecords").Document(record.Id)
            .UpdateAsync(new Dictionary<string, object> { { "ConfirmedByDonor", true } });

        // Actualizar estado del post a "entregado"
        await _firebase.GetCollection("donationPosts").Document(postId)
            .UpdateAsync(new Dictionary<string, object> { { "Status", "entregado" } });
    }

    // El receptor confirma que recibió (solo si el donante ya confirmó)
    public async Task ConfirmByReceiverAsync(string postId, string receiverId)
    {
        var record = await GetByPostIdAsync(postId)
            ?? throw new KeyNotFoundException("Registro de entrega no encontrado");

        if (record.ReceiverId != receiverId)
            throw new UnauthorizedAccessException("Solo el receptor puede confirmar la recepción");

        if (!record.ConfirmedByDonor)
            throw new InvalidOperationException("El donante aún no ha confirmado la entrega");

        // Cierre inmutable: confirmación doble completa
        await _firebase.GetCollection("deliveryRecords").Document(record.Id)
            .UpdateAsync(new Dictionary<string, object>
            {
                { "ConfirmedByReceiver", true },
                { "CompletedAt", DateTime.UtcNow }
            });

        // Cerrar la publicación definitivamente
        await _firebase.GetCollection("donationPosts").Document(postId)
            .UpdateAsync(new Dictionary<string, object>
            {
                { "Status", "entregado" },
                { "IsActive", false },
                { "ClosedAt", DateTime.UtcNow }
            });
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
        return snap.Documents.Select(MapRecord).ToList();
    }

    private static DeliveryRecord MapRecord(DocumentSnapshot doc)
    {
        var d = doc.ToDictionary();
        return new DeliveryRecord
        {
            Id = d["Id"].ToString()!,
            PostId = d["PostId"].ToString()!,
            DonorId = d["DonorId"].ToString()!,
            ReceiverId = d["ReceiverId"].ToString()!,
            ItemName = d["ItemName"].ToString()!,
            CategoryId = d["CategoryId"].ToString()!,
            DeliveryDate = ((Google.Cloud.Firestore.Timestamp)d["DeliveryDate"]).ToDateTime(),
            DeliveryLocation = d["DeliveryLocation"].ToString()!,
            ConfirmedByDonor = (bool)d["ConfirmedByDonor"],
            ConfirmedByReceiver = (bool)d["ConfirmedByReceiver"],
            CompletedAt = d.ContainsKey("CompletedAt") && d["CompletedAt"] != null
                ? ((Google.Cloud.Firestore.Timestamp)d["CompletedAt"]).ToDateTime() : null
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
        { "ConfirmedByReceiver", r.ConfirmedByReceiver },
        { "CompletedAt", r.CompletedAt ?? (object)string.Empty }
    };
}