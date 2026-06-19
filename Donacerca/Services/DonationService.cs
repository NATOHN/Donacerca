using Donacerca.DTOs;
using Donacerca.Models;
using Google.Cloud.Firestore;

namespace Donacerca.Services;

public class DonationService
{
    private readonly FirebaseService _firebase;

    public DonationService(FirebaseService firebase)
    {
        _firebase = firebase;
    }

    public async Task<List<DonationPost>> GetAvailableAsync(string? categoryId = null, string? zone = null)
    {
        var query = _firebase.GetCollection("donationPosts")
            .WhereEqualTo("Status", "disponible")
            .WhereEqualTo("IsActive", true) as Query;

        if (categoryId != null) query = query.WhereEqualTo("CategoryId", categoryId);
        if (zone != null) query = query.WhereEqualTo("Zone", zone);

        var snap = await query.GetSnapshotAsync();
        return snap.Documents.Select(MapPost).ToList();
    }

    public async Task<List<DonationPost>> GetByDonorAsync(string donorId)
    {
        var snap = await _firebase.GetCollection("donationPosts")
            .WhereEqualTo("DonorId", donorId)
            .GetSnapshotAsync();
        return snap.Documents.Select(MapPost).ToList();
    }

    public async Task<DonationPost?> GetByIdAsync(string id)
    {
        var doc = await _firebase.GetCollection("donationPosts").Document(id).GetSnapshotAsync();
        return doc.Exists ? MapPost(doc) : null;
    }

    public async Task<DonationPost> CreateAsync(CreateDonationPostDto dto, string donorId, string donorName)
    {
        var post = new DonationPost
        {
            DonorId = donorId,
            DonorName = donorName,
            CategoryId = dto.CategoryId,
            ItemName = dto.ItemName,
            Description = dto.Description,
            ItemCondition = dto.ItemCondition,
            Zone = dto.Zone,
            PhotoUrls = dto.PhotoUrls
        };
        await _firebase.GetCollection("donationPosts").Document(post.Id).SetAsync(ToDict(post));
        return post;
    }

    public async Task<DonationPost?> UpdateAsync(string id, UpdateDonationPostDto dto, string requestingUserId)
    {
        var docRef = _firebase.GetCollection("donationPosts").Document(id);
        var snap = await docRef.GetSnapshotAsync();
        if (!snap.Exists) return null;

        var post = MapPost(snap);

        // Solo el dueño puede editar
        if (post.DonorId != requestingUserId)
            throw new UnauthorizedAccessException("No tienes permiso para editar esta publicación");

        // No editar si ya tiene receptor seleccionado
        if (post.SelectedReceiverId != null)
            throw new InvalidOperationException("No se puede editar una publicación con receptor ya seleccionado");

        var updates = new Dictionary<string, object>();
        if (dto.CategoryId != null) updates["CategoryId"] = dto.CategoryId;
        if (dto.ItemName != null) updates["ItemName"] = dto.ItemName;
        if (dto.Description != null) updates["Description"] = dto.Description;
        if (dto.ItemCondition != null) updates["ItemCondition"] = dto.ItemCondition;
        if (dto.Zone != null) updates["Zone"] = dto.Zone;
        if (dto.PhotoUrls != null) updates["PhotoUrls"] = dto.PhotoUrls;

        await docRef.UpdateAsync(updates);
        var updated = await docRef.GetSnapshotAsync();
        return MapPost(updated);
    }

    public async Task DeactivateAsync(string id, string requestingUserId, bool isAdmin = false)
    {
        var docRef = _firebase.GetCollection("donationPosts").Document(id);
        var snap = await docRef.GetSnapshotAsync();
        if (!snap.Exists) throw new KeyNotFoundException("Publicación no encontrada");

        var post = MapPost(snap);
        if (!isAdmin && post.DonorId != requestingUserId)
            throw new UnauthorizedAccessException("No tienes permiso");

        await docRef.UpdateAsync(new Dictionary<string, object> { { "IsActive", false } });
    }

    public async Task<List<DonationPost>> GetAllActiveAsync()
    {
        var snap = await _firebase.GetCollection("donationPosts")
            .WhereEqualTo("IsActive", true)
            .GetSnapshotAsync();
        return snap.Documents.Select(MapPost).ToList();
    }

    private static DonationPost MapPost(DocumentSnapshot doc)
    {
        var d = doc.ToDictionary();

        // ReservedAt — puede ser string vacío o Timestamp
        DateTime? reservedAt = null;
        if (d.ContainsKey("ReservedAt") && d["ReservedAt"] is Timestamp rts)
            reservedAt = rts.ToDateTime();

        // ClosedAt — puede ser string vacío o Timestamp
        DateTime? closedAt = null;
        if (d.ContainsKey("ClosedAt") && d["ClosedAt"] is Timestamp cts)
            closedAt = cts.ToDateTime();

        return new DonationPost
        {
            Id = d["Id"].ToString()!,
            DonorId = d["DonorId"].ToString()!,
            DonorName = d["DonorName"].ToString()!,
            CategoryId = d["CategoryId"].ToString()!,
            ItemName = d["ItemName"].ToString()!,
            Description = d["Description"].ToString()!,
            ItemCondition = d["ItemCondition"].ToString()!,
            Zone = d["Zone"].ToString()!,
            PhotoUrls = d.ContainsKey("PhotoUrls")
                ? ((List<object>)d["PhotoUrls"]).Select(x => x.ToString()!).ToList()
                : new(),
            Status = d["Status"].ToString()!,
            SelectedReceiverId = d.ContainsKey("SelectedReceiverId")
                ? d["SelectedReceiverId"]?.ToString()
                : null,
            IsActive = (bool)d["IsActive"],
            CreatedAt = ((Timestamp)d["CreatedAt"]).ToDateTime(),
            ReservedAt = reservedAt,
            ClosedAt = closedAt
        };
    }

    private static Dictionary<string, object> ToDict(DonationPost p)
    {
        var dict = new Dictionary<string, object>
        {
            { "Id", p.Id },
            { "DonorId", p.DonorId },
            { "DonorName", p.DonorName },
            { "CategoryId", p.CategoryId },
            { "ItemName", p.ItemName },
            { "Description", p.Description },
            { "ItemCondition", p.ItemCondition },
            { "Zone", p.Zone },
            { "PhotoUrls", p.PhotoUrls },
            { "Status", p.Status },
            { "SelectedReceiverId", p.SelectedReceiverId ?? (object)string.Empty },
            { "IsActive", p.IsActive },
            { "CreatedAt", p.CreatedAt }
        };

        // Solo guardar si tienen valor real
        if (p.ReservedAt.HasValue) dict["ReservedAt"] = p.ReservedAt.Value;
        if (p.ClosedAt.HasValue) dict["ClosedAt"] = p.ClosedAt.Value;

        return dict;
    }
}