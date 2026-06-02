# 09 — File Storage por Tenant (S3 / Azure Blob)

En un SaaS multi-tenant, los archivos de cada empresa deben estar aislados. El patrón estándar es organizar los archivos en rutas que incluyen el `tenant_id` (y `branch_id` si aplica), y generar URLs de acceso temporal (presigned URLs) en lugar de exponer el bucket públicamente.

---

## Estructura de paths en S3 / Blob Storage

```
s3://misaas-files/
├── tenants/
│   ├── {tenant_id}/
│   │   ├── logos/
│   │   │   └── logo.png
│   │   ├── documents/
│   │   │   ├── 2025/01/contrato.pdf
│   │   │   └── 2025/02/factura.pdf
│   │   └── exports/
│   │       └── report-2025-q1.xlsx
│   │
│   └── {tenant_id}/
│       ├── branches/
│       │   ├── {branch_id}/
│       │   │   ├── reports/
│       │   │   └── attachments/
│       │   └── {branch_id}/
│       └── shared/           ← archivos compartidos entre branches
│
└── system/
    └── templates/            ← plantillas del SaaS (no son de ningún tenant)
```

### Paths con tenant + branch

```csharp
public static class StoragePaths
{
    // Archivo de un tenant sin branch
    public static string TenantFile(long tenantId, string category, string filename) =>
        $"tenants/{tenantId}/{category}/{filename}";

    // Archivo de un branch específico
    public static string BranchFile(long tenantId, long branchId, string category, string filename) =>
        $"tenants/{tenantId}/branches/{branchId}/{category}/{filename}";

    // Logo del tenant
    public static string TenantLogo(long tenantId, string extension = "png") =>
        $"tenants/{tenantId}/logos/logo.{extension}";

    // Exportación con timestamp para unicidad
    public static string Export(long tenantId, long? branchId, string reportName) =>
        branchId.HasValue
            ? $"tenants/{tenantId}/branches/{branchId}/exports/{reportName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx"
            : $"tenants/{tenantId}/exports/{reportName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
}
```

---

## IStorageService — abstracción del proveedor

```csharp
// Common/Storage/IStorageService.cs
public interface IStorageService
{
    // Upload — devuelve la key del objeto almacenado
    Task<string> UploadAsync(
        string key,
        Stream content,
        string contentType,
        CancellationToken ct = default);

    // Presigned URL para descarga temporal
    Task<string> GetPresignedDownloadUrlAsync(
        string key,
        TimeSpan expiry,
        CancellationToken ct = default);

    // Presigned URL para upload directo desde el cliente (browser)
    Task<PresignedUploadDto> GetPresignedUploadUrlAsync(
        string key,
        string contentType,
        long   maxSizeBytes,
        TimeSpan expiry,
        CancellationToken ct = default);

    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
    Task<long> GetSizeAsync(string key, CancellationToken ct = default);
}

public sealed record PresignedUploadDto(
    string UploadUrl,
    Dictionary<string, string> Fields,   // campos adicionales para el form POST
    string FinalKey);                    // key que tendrá el archivo una vez subido
```

---

## Implementación AWS S3

```csharp
// Infrastructure/Storage/S3StorageService.cs
public sealed class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly string    _bucket;

    public S3StorageService(IAmazonS3 s3, IConfiguration config)
    {
        _s3     = s3;
        _bucket = config["Aws:S3:BucketName"]!;
    }

    public async Task<string> UploadAsync(
        string key, Stream content, string contentType, CancellationToken ct)
    {
        var request = new PutObjectRequest
        {
            BucketName  = _bucket,
            Key         = key,
            InputStream = content,
            ContentType = contentType,
            // No ACL público — acceso solo por presigned URL
        };
        await _s3.PutObjectAsync(request, ct);
        return key;
    }

    public async Task<string> GetPresignedDownloadUrlAsync(
        string key, TimeSpan expiry, CancellationToken ct)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key        = key,
            Verb       = HttpVerb.GET,
            Expires    = DateTime.UtcNow.Add(expiry)
        };
        return _s3.GetPreSignedURL(request);   // síncrono — no hay versión async
    }

    public async Task<PresignedUploadDto> GetPresignedUploadUrlAsync(
        string key, string contentType, long maxSizeBytes, TimeSpan expiry, CancellationToken ct)
    {
        // POST presignado con condiciones de tamaño
        var conditions = new List<object>
        {
            new Dictionary<string, string> { ["Content-Type"] = contentType },
            new List<object> { "content-length-range", 0, maxSizeBytes }
        };

        var fields = new Dictionary<string, object>
        {
            ["Content-Type"] = contentType
        };

        // AWS SDK v3 — S3PostUploadSignedPolicy
        var policy = S3PostUploadSignedPolicy.GetSignedPolicy(
            _bucket, key, DateTime.UtcNow.Add(expiry), conditions, fields,
            new Amazon.Runtime.ImmutableCredentials("accessKey", "secretKey", null));

        return new PresignedUploadDto(
            UploadUrl: $"https://{_bucket}.s3.amazonaws.com/",
            Fields:    policy.ToFormFields(),
            FinalKey:  key);
    }

    public async Task DeleteAsync(string key, CancellationToken ct)
    {
        await _s3.DeleteObjectAsync(_bucket, key, ct);
    }

    public async Task<long> GetSizeAsync(string key, CancellationToken ct)
    {
        var meta = await _s3.GetObjectMetadataAsync(_bucket, key, ct);
        return meta.ContentLength;
    }
}
```

---

## Implementación Azure Blob Storage

```csharp
// Infrastructure/Storage/AzureBlobStorageService.cs
public sealed class AzureBlobStorageService : IStorageService
{
    private readonly BlobServiceClient _client;
    private readonly string            _container;

    public AzureBlobStorageService(BlobServiceClient client, IConfiguration config)
    {
        _client    = client;
        _container = config["Azure:Storage:ContainerName"]!;
    }

    public async Task<string> UploadAsync(
        string key, Stream content, string contentType, CancellationToken ct)
    {
        var blob = _client
            .GetBlobContainerClient(_container)
            .GetBlobClient(key);

        await blob.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, ct);
        return key;
    }

    public async Task<string> GetPresignedDownloadUrlAsync(
        string key, TimeSpan expiry, CancellationToken ct)
    {
        var blob = _client
            .GetBlobContainerClient(_container)
            .GetBlobClient(key);

        var sasUri = blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(expiry));
        return sasUri.ToString();
    }
}
```

---

## Flujo upload directo desde el cliente (browser)

El patrón recomendado para archivos grandes: el backend genera una presigned URL y el cliente sube directamente a S3, sin pasar por la API:

```
1. Cliente solicita presigned upload URL al API
   POST /api/files/upload-url
   { "filename": "reporte.pdf", "contentType": "application/pdf", "sizeBytes": 5242880 }

2. API valida (plan, cuota de storage) y genera presigned URL
   → { "uploadUrl": "https://bucket.s3.amazonaws.com/", "fields": {...}, "fileKey": "tenants/1/documents/..." }

3. Cliente sube directamente a S3 con el form POST
   POST https://bucket.s3.amazonaws.com/
   form-data: { key, Content-Type, policy, signature, file }

4. Cliente notifica al API que el upload terminó
   POST /api/files/confirm
   { "fileKey": "tenants/1/documents/..." }

5. API registra el archivo en la DB
```

```csharp
// Files.Application/UseCases/RequestUpload/RequestUploadHandler.cs
public async Task<RequestUploadResponse> Handle(
    RequestUploadRequest request, CancellationToken ct)
{
    // 1. Validar plan y cuota de storage
    var check = await _planService.CanUploadFileAsync(
        request.TenantId, request.SizeBytes, ct);
    if (!check.Allowed)
        return new RequestUploadLimitReachedFailure(check.Reason!);

    // 2. Sanitizar nombre de archivo
    var safeName = SanitizeFilename(request.Filename);
    var key = StoragePaths.BranchFile(
        request.TenantId, request.BranchId,
        "documents", $"{Guid.NewGuid():N}-{safeName}");

    // 3. Generar presigned URL (válida por 15 minutos)
    var presigned = await _storage.GetPresignedUploadUrlAsync(
        key,
        request.ContentType,
        request.SizeBytes,
        TimeSpan.FromMinutes(15),
        ct);

    return new RequestUploadSuccess(presigned);
}

private static string SanitizeFilename(string filename) =>
    Regex.Replace(Path.GetFileName(filename), @"[^a-zA-Z0-9.\-_]", "_");
```

---

## Confirmar el upload y registrar en la DB

```csharp
// Files.Domain/Entities/StoredFile.cs
public sealed class StoredFile
{
    public long     Id           { get; init; }
    public Guid     PublicId     { get; init; }
    public long     TenantId     { get; init; }
    public long?    BranchId     { get; init; }
    public string   Key          { get; init; } = string.Empty;   // ruta en S3
    public string   OriginalName { get; init; } = string.Empty;
    public string   ContentType  { get; init; } = string.Empty;
    public long     SizeBytes    { get; init; }
    public Guid     UploadedByPublicId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
```

```csharp
// Files.Application/UseCases/ConfirmUpload/ConfirmUploadHandler.cs
public async Task<ConfirmUploadResponse> Handle(
    ConfirmUploadRequest request, CancellationToken ct)
{
    // Verificar que el archivo realmente existe en S3
    if (!await _storage.ExistsAsync(request.FileKey, ct))
        return new ConfirmUploadNotFoundFailure("El archivo no fue encontrado en storage.");

    var sizeBytes = await _storage.GetSizeAsync(request.FileKey, ct);

    // Registrar en la DB
    await _files.InsertAsync(
        tenantId:          request.TenantId,
        branchId:          request.BranchId,
        key:               request.FileKey,
        originalName:      request.OriginalName,
        contentType:       request.ContentType,
        sizeBytes:         sizeBytes,
        uploadedByPublicId: request.UploaderPublicId,
        ct:                ct);

    // Actualizar cuota de storage del tenant
    await _storage.AddUsageAsync(request.TenantId, sizeBytes, ct);

    return new ConfirmUploadSuccess(request.FileKey);
}
```

---

## Descarga con presigned URL

```csharp
// Files.Application/UseCases/GetFileDownloadUrl/GetFileDownloadUrlHandler.cs
public async Task<GetFileDownloadUrlResponse> Handle(
    GetFileDownloadUrlRequest request, CancellationToken ct)
{
    var file = await _files.GetByPublicIdAsync(
        request.FilePublicId, request.TenantId, ct);

    if (file is null)
        return new GetFileDownloadUrlNotFoundFailure("Archivo no encontrado.");

    // El branch_id en el path garantiza que un usuario de Branch A
    // no puede descargar archivos de Branch B aunque adivine el FilePublicId
    if (file.BranchId.HasValue && file.BranchId != request.BranchId)
        return new GetFileDownloadUrlForbiddenFailure("Sin acceso a este archivo.");

    // URL válida por 5 minutos
    var url = await _storage.GetPresignedDownloadUrlAsync(
        file.Key,
        TimeSpan.FromMinutes(5),
        ct);

    return new GetFileDownloadUrlSuccess(url, file.OriginalName);
}
```

---

## Política de bucket S3 — denegar acceso público

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "DenyPublicAccess",
      "Effect": "Deny",
      "Principal": "*",
      "Action": "s3:GetObject",
      "Resource": "arn:aws:s3:::misaas-files/*",
      "Condition": {
        "StringNotEquals": {
          "aws:PrincipalArn": "arn:aws:iam::ACCOUNT_ID:role/misaas-api-role"
        }
      }
    }
  ]
}
```

---

## Cleanup de archivos huérfanos

Un job periódico elimina archivos de S3 cuyo registro en la DB fue eliminado (o tenants suspendidos):

```csharp
// Background job mensual
public async Task CleanupOrphanedFilesAsync(CancellationToken ct)
{
    // Listar todas las keys de S3 y comparar con la DB
    // Solo eliminar keys que no tienen registro en dbo.stored_files
    // Y pertenecen a tenants con status Deleted
}
```

---

## Checklist

- [ ] Paths incluyen `tenant_id` (y `branch_id` si aplica) — nunca paths planos
- [ ] Bucket privado — acceso solo por presigned URLs
- [ ] Presigned URL de download válida 5 minutos (ajustar según caso de uso)
- [ ] Presigned URL de upload válida 15 minutos
- [ ] Validar plan/cuota ANTES de generar la presigned URL de upload
- [ ] Confirmar el upload registrando el archivo en la DB
- [ ] Sanitizar nombres de archivo — no permitir paths traversal (`../`)
- [ ] Archivos de un branch no accesibles desde otro branch
- [ ] Job de cleanup para archivos de tenants eliminados

---

## Glosario

| Término | Definición |
|---------|-----------|
| Presigned URL | URL temporal con firma criptográfica que otorga acceso a un objeto de S3 o Blob sin exponer credenciales |
| Bucket | Contenedor lógico de objetos en Amazon S3; equivalente al contenedor de Azure Blob Storage |
| Object key | Identificador único de un objeto dentro de un bucket de S3; actúa como ruta de archivo |
| Path traversal | Ataque que usa secuencias como `../` en nombres de archivo para escapar del directorio permitido |
| Content-Type | Cabecera HTTP que indica el tipo MIME del archivo (application/pdf, image/png, etc.) |
| Cuota de storage | Límite de almacenamiento asignado a un tenant según su plan de suscripción |
| IStorageService | Interfaz de abstracción que desacopla la lógica de negocio del proveedor de almacenamiento concreto |
| Archivo huérfano | Objeto en S3 o Blob Storage cuyo registro en la base de datos fue eliminado o cuyo tenant fue desactivado |
| SAS | Shared Access Signature; mecanismo de Azure Blob Storage equivalente a la presigned URL de S3 |
| Upload directo | Patrón donde el cliente sube el archivo directamente a S3 usando una presigned URL, sin pasar por la API |
| StoredFile | Entidad de dominio que registra los metadatos de un archivo subido (key, tenant, branch, tamaño, tipo) |
| Sanitización | Proceso de validar y limpiar el nombre de archivo para eliminar caracteres peligrosos antes de usarlo como key |

---

*Rogelio Arriaga Gonzalez*
