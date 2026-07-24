namespace EventPlatform.Messaging;

/// <summary>Model-building helpers for the transactional outbox.</summary>
public static class OutboxModelBuilderExtensions
{
    /// <summary>Maps the <see cref="OutboxMessage"/> table. Call from a DbContext's OnModelCreating.</summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <returns>The same <paramref name="modelBuilder"/> for chaining.</returns>
    public static ModelBuilder ApplyOutbox(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        var entity = modelBuilder.Entity<OutboxMessage>();
        entity.ToTable("outbox");
        entity.HasKey(m => m.Id);
        entity.Property(m => m.Topic).HasMaxLength(200).IsRequired();
        entity.Property(m => m.Type).HasMaxLength(500).IsRequired();
        entity.Property(m => m.Payload).IsRequired();
        entity.HasIndex(m => m.PublishedAt);

        return modelBuilder;
    }
}
