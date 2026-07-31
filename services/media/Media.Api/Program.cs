var builder = WebApplication.CreateBuilder(args);

// Shared defaults: auth, OpenAPI/Scalar, JSON, OpenTelemetry, health checks — same as every
// other service, even though this one has no database and no MediatR pipeline.
builder.AddServiceDefaults(serviceName: "media");

builder.Services.AddSingleton(_ =>
    new BlobServiceClient(builder.Configuration.GetConnectionString("media-storage")));

var app = builder.Build();

app.UseServiceDefaults();
app.MapMediaEndpoints();

// Ensures the container exists on every startup — a no-op against a Terraform-created
// container in real Azure, and what makes Azurite "just work" locally with zero manual setup.
// One code path, not an environment branch.
using (var scope = app.Services.CreateScope())
{
    var blobServiceClient = scope.ServiceProvider.GetRequiredService<BlobServiceClient>();
    var containerClient = blobServiceClient.GetBlobContainerClient(MediaEndpoints.ContainerName);
    await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
}

await app.RunAsync();
