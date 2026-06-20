using Donacerca.DTOs;
using Donacerca.Models;
using Google.Cloud.Firestore;

namespace Donacerca.Services;

public class CategoryService
{
    private readonly FirebaseService _firebase;

    public CategoryService(FirebaseService firebase)
    {
        _firebase = firebase;
    }

    public async Task<List<Category>> GetAllAsync()
    {
        var snap = await _firebase.GetCollection("categories").GetSnapshotAsync();
        var categories = snap.Documents.Select(MapCategory).ToList();

        // Contar artículos activos por categoría
        var postsSnap = await _firebase.GetCollection("donationPosts")
            .WhereEqualTo("IsActive", true)
            .WhereEqualTo("Status", "disponible")
            .GetSnapshotAsync();

        var countByCat = new Dictionary<string, int>();
        foreach (var doc in postsSnap.Documents)
        {
            var d = doc.ToDictionary();
            var catId = d.ContainsKey("CategoryId") ? d["CategoryId"].ToString()! : "";
            if (!string.IsNullOrEmpty(catId))
            {
                if (!countByCat.ContainsKey(catId)) countByCat[catId] = 0;
                countByCat[catId]++;
            }
        }

        foreach (var cat in categories)
        {
            cat.ActiveItemsCount = countByCat.ContainsKey(cat.Id) ? countByCat[cat.Id] : 0;
        }

        return categories;
    }

    public async Task<Category?> GetByIdAsync(string id)
    {
        var doc = await _firebase.GetCollection("categories").Document(id).GetSnapshotAsync();
        return doc.Exists ? MapCategory(doc) : null;
    }

    public async Task<Category> CreateAsync(CreateCategoryDto dto)
    {
        var category = new Category
        {
            Name = dto.Name,
            Description = dto.Description
        };
        await _firebase.GetCollection("categories").Document(category.Id).SetAsync(ToDict(category));
        return category;
    }

    public async Task<Category?> UpdateAsync(string id, UpdateCategoryDto dto)
    {
        var docRef = _firebase.GetCollection("categories").Document(id);
        var snap = await docRef.GetSnapshotAsync();
        if (!snap.Exists) return null;

        var updates = new Dictionary<string, object>();
        if (dto.Name != null) updates["Name"] = dto.Name;
        if (dto.Description != null) updates["Description"] = dto.Description;
        if (dto.IsActive.HasValue) updates["IsActive"] = dto.IsActive.Value;

        await docRef.UpdateAsync(updates);
        var updated = await docRef.GetSnapshotAsync();
        return MapCategory(updated);
    }

    private static Category MapCategory(DocumentSnapshot doc)
    {
        var d = doc.ToDictionary();
        return new Category
        {
            Id = d["Id"].ToString()!,
            Name = d["Name"].ToString()!,
            Description = d["Description"].ToString()!,
            IsActive = (bool)d["IsActive"],
            ActiveItemsCount = 0,
            CreatedAt = ((Google.Cloud.Firestore.Timestamp)d["CreatedAt"]).ToDateTime()
        };
    }

    private static Dictionary<string, object> ToDict(Category c) => new()
    {
        { "Id", c.Id },
        { "Name", c.Name },
        { "Description", c.Description },
        { "IsActive", c.IsActive },
        { "CreatedAt", c.CreatedAt }
    };
}