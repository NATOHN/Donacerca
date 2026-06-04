using Donacerca.DTOs;
using Donacerca.Models;
using Google.Cloud.Firestore;

namespace Donacerca.Services;

public class RequestService
{
    private readonly FirebaseService _firebase;

    public RequestService(FirebaseService firebase)
    {
        _firebase = firebase;
    }

    public async Task<DonationRequest> CreateAsync(string postId, string receiverId, string receiverName)
    {
        // Validar que la publicación existe y está disponible
        var postSnap = await _firebase.GetCollection("donationPosts").Document(postId).GetSnapshotAsync();
        if (!postSnap.Exists) throw new KeyNotFoundException("Publicación no encontrada");

        var postData = postSnap.ToDictionary();
        if (postData["Status"].ToString() != "disponible")
            throw new InvalidOperationException("Este artículo ya no está disponible");

        // Validar que el receptor no tenga ya una solicitud activa para este post
        var existing = await _firebase.GetCollection("donationRequests")
            .WhereEqualTo("PostId", postId)
            .WhereEqualTo("ReceiverId", receiverId)
            .WhereEqualTo("Status", "pendiente")
            .GetSnapshotAsync();

        if (existing.Count > 0)
            throw new InvalidOperationException("Ya tienes una solicitud activa para este artículo");

        var request = new DonationRequest
        {
            PostId = postId,
            ReceiverId = receiverId,
            ReceiverName = receiverName
        };

        await _firebase.GetCollection("donationRequests").Document(request.Id).SetAsync(ToDict(request));
        return request;
    }

    public async Task<List<DonationRequest>> GetByPostAsync(string postId)
    {
        var snap = await _firebase.GetCollection("donationRequests")
            .WhereEqualTo("PostId", postId)
            .GetSnapshotAsync();
        return snap.Documents.Select(MapRequest).ToList();
    }

    public async Task<List<DonationRequest>> GetByReceiverAsync(string receiverId)
    {
        var snap = await _firebase.GetCollection("donationRequests")
            .WhereEqualTo("ReceiverId", receiverId)
            .GetSnapshotAsync();
        return snap.Documents.Select(MapRequest).ToList();
    }

    // El donante selecciona a un receptor (transacción atómica)
    public async Task SelectReceiverAsync(string postId, string receiverId, string donorId)
    {
        var db = _firebase.GetFirestoreDb();

        await db.RunTransactionAsync(async transaction =>
        {
            var postRef = _firebase.GetCollection("donationPosts").Document(postId);
            var postSnap = await transaction.GetSnapshotAsync(postRef);

            if (!postSnap.Exists) throw new KeyNotFoundException("Publicación no encontrada");

            var postData = postSnap.ToDictionary();
            if (postData["DonorId"].ToString() != donorId)
                throw new UnauthorizedAccessException("Solo el donante puede seleccionar al receptor");

            if (postData["Status"].ToString() != "disponible")
                throw new InvalidOperationException("Esta publicación ya no está disponible");

            // Reservar el post para el receptor seleccionado
            transaction.Update(postRef, new Dictionary<string, object>
            {
                { "Status", "reservado" },
                { "SelectedReceiverId", receiverId },
                { "ReservedAt", DateTime.UtcNow }
            });

            // Rechazar todas las demás solicitudes pendientes
            var requestsSnap = await _firebase.GetCollection("donationRequests")
                .WhereEqualTo("PostId", postId)
                .WhereEqualTo("Status", "pendiente")
                .GetSnapshotAsync();

            foreach (var reqDoc in requestsSnap.Documents)
            {
                var reqRef = _firebase.GetCollection("donationRequests").Document(reqDoc.Id);
                var status = reqDoc.ToDictionary()["ReceiverId"].ToString() == receiverId ? "aceptada" : "rechazada";
                transaction.Update(reqRef, new Dictionary<string, object> { { "Status", status } });
            }
        });
    }

    private static DonationRequest MapRequest(DocumentSnapshot doc)
    {
        var d = doc.ToDictionary();
        return new DonationRequest
        {
            Id = d["Id"].ToString()!,
            PostId = d["PostId"].ToString()!,
            ReceiverId = d["ReceiverId"].ToString()!,
            ReceiverName = d["ReceiverName"].ToString()!,
            Status = d["Status"].ToString()!,
            RequestTimestamp = ((Google.Cloud.Firestore.Timestamp)d["RequestTimestamp"]).ToDateTime()
        };
    }

    private static Dictionary<string, object> ToDict(DonationRequest r) => new()
    {
        { "Id", r.Id },
        { "PostId", r.PostId },
        { "ReceiverId", r.ReceiverId },
        { "ReceiverName", r.ReceiverName },
        { "Status", r.Status },
        { "RequestTimestamp", r.RequestTimestamp }
    };
}