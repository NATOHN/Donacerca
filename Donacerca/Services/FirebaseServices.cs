using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Donacerca.Services;

public class FirebaseService
{
    // Este archivo sirve de puente de comunicacion entre la app y FB
    // TODAS LAS OPERACIONES PASAN POR AQUI SI O SI
    private readonly FirestoreDb _firestoreDb;

    public FirebaseService()
    {
        // Le vamos a FB donde esta el archivo con las credenciales
        // Usamos la ruta del folder para encontrarla
        var credentialPath = Path.Combine(AppContext.BaseDirectory, "Config", "firebase-credentials.json");
        
        // Una variable para que podamos utilizar el SDK de Google
        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialPath);
        
        // Ahora agregamos el id del proyecto para acceso a la Firebase Console
        _firestoreDb = FirestoreDb.Create("donacerca");
    }
    
    // Devuelve una referencia a una coleccion cualquiera que pidamos
    public CollectionReference GetCollection(string collectionName)
    {
        return _firestoreDb.Collection(collectionName);
    }
    
    public FirestoreDb GetFirestoreDb()
    {
        return _firestoreDb;
    }
}