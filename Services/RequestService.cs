using Donacerca.DTOs;
using Donacerca.Models;
using Google.Cloud.Firestore;

namespace Donacerca.Services;

public class RequestService
{
    private readonly FirebaseService _firebase;
    private readonly NotificationService _notificationService;

    public RequestService(FirebaseService firebase, NotificationService notificationService)
    {
        _firebase = firebase;
        _notificationService = notificationService;
    }

    public async Task<DonationRequest> CreateAsync(string postId, string receiverId, string receiverName)
    {
        var postSnap = await _firebase.GetCollection("donationPosts").Document(postId).GetSnapshotAsync();
        if (!postSnap.Exists) throw new KeyNotFoundException("Publicación no encontrada");

        var postData = postSnap.ToDictionary();
        if (postData["Status"].ToString() != "disponible")
            throw new InvalidOperationException("Este artículo ya no está disponible");

        // Evitar que el donante solicite su propia publicación
        var donorId = postData["DonorId"].ToString()!;
        if (donorId == receiverId)
            throw new InvalidOperationException("No puedes solicitar tu propia publicación");

        var existing = await _firebase.GetCollection("donationRequests")
            .WhereEqualTo("PostId", postId)
            .WhereEqualTo("ReceiverId", receiverId)
            .WhereEqualTo("Status", "pendiente")
            .GetSnapshotAsync();

        if (existing.Count > 0)
            throw new InvalidOperationException("Ya tienes una solicitud activa para este artículo");

        var request = new DonationRequest
        {
            PostId       = postId,
            ReceiverId   = receiverId,
            ReceiverName = receiverName
        };

        await _firebase.GetCollection("donationRequests").Document(request.Id).SetAsync(ToDict(request));

        var itemName = postData["ItemName"].ToString()!;
        await _notificationService.CreateAsync(
            donorId,
            "request",
            $"{receiverName} ha enviado una solicitud para tu publicación '{itemName}'.",
            postId
        );

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

    public async Task SelectReceiverAsync(string postId, string receiverId, string donorId)
    {
        var db = _firebase.GetFirestoreDb();

        var postSnapPre = await _firebase.GetCollection("donationPosts").Document(postId).GetSnapshotAsync();
        if (!postSnapPre.Exists) throw new KeyNotFoundException("Publicación no encontrada");
        var postDataPre = postSnapPre.ToDictionary();
        var itemName    = postDataPre["ItemName"].ToString()!;
        var donorName   = postDataPre["DonorName"].ToString()!;

        await db.RunTransactionAsync(async transaction =>
        {
            var postRef  = _firebase.GetCollection("donationPosts").Document(postId);
            var postSnap = await transaction.GetSnapshotAsync(postRef);

            if (!postSnap.Exists) throw new KeyNotFoundException("Publicación no encontrada");

            var postData = postSnap.ToDictionary();
            if (postData["DonorId"].ToString() != donorId)
                throw new UnauthorizedAccessException("Solo el donante puede seleccionar al receptor");

            if (postData["Status"].ToString() != "disponible")
                throw new InvalidOperationException("Esta publicación ya no está disponible");

            transaction.Update(postRef, new Dictionary<string, object>
            {
                { "Status",             "reservado"      },
                { "SelectedReceiverId", receiverId       },
                { "ReservedAt",         DateTime.UtcNow  }
            });

            var requestsSnap = await _firebase.GetCollection("donationRequests")
                .WhereEqualTo("PostId", postId)
                .WhereEqualTo("Status", "pendiente")
                .GetSnapshotAsync();

            foreach (var reqDoc in requestsSnap.Documents)
            {
                var reqRef = _firebase.GetCollection("donationRequests").Document(reqDoc.Id);
                var status = reqDoc.ToDictionary()["ReceiverId"].ToString() == receiverId
                    ? "aceptada"
                    : "rechazada";
                transaction.Update(reqRef, new Dictionary<string, object> { { "Status", status } });
            }
        });

        await _notificationService.CreateAsync(
            receiverId,
            "selected",
            $"¡Felicidades! {donorName} te ha seleccionado como receptor para '{itemName}'. Coordina la entrega.",
            postId
        );
    }

    private static DonationRequest MapRequest(DocumentSnapshot doc)
    {
        var d = doc.ToDictionary();
        return new DonationRequest
        {
            Id               = d["Id"].ToString()!,
            PostId           = d["PostId"].ToString()!,
            ReceiverId       = d["ReceiverId"].ToString()!,
            ReceiverName     = d["ReceiverName"].ToString()!,
            Status           = d["Status"].ToString()!,
            RequestTimestamp = ((Google.Cloud.Firestore.Timestamp)d["RequestTimestamp"]).ToDateTime()
        };
    }

    private static Dictionary<string, object> ToDict(DonationRequest r) => new()
    {
        { "Id",               r.Id               },
        { "PostId",           r.PostId           },
        { "ReceiverId",       r.ReceiverId       },
        { "ReceiverName",     r.ReceiverName     },
        { "Status",           r.Status           },
        { "RequestTimestamp", r.RequestTimestamp }
    };
}