namespace EventPlatform.Messaging;

/// <summary>Maps the shared dead-letter table into a service's model.</summary>
public static class DeadLetterModelBuilderExtensions
{
    /// <summary>Adds the <c>dead_letters</c> table to the model.</summary>
    /// <remarks>
    /// Call this from any service that <i>subscribes</i> to anything, whether or not it publishes.
    /// </remarks>
    /// <param name="modelBuilder">The model builder.</param>
    /// <returns>The same <paramref name="modelBuilder"/> for chaining.</returns>
    public static ModelBuilder ApplyDeadLetters(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        var entity = modelBuilder.Entity<DeadLetterMessage>();
        entity.ToTable("dead_letters");
        entity.HasKey(m => m.Id);
        entity.Property(m => m.Topic).HasMaxLength(200).IsRequired();
        entity.Property(m => m.Payload).IsRequired();
        entity.HasIndex(m => m.MessageId);
        entity.HasIndex(m => m.CorrelationId);

        // The operator's actual question is "what is still broken", so that is the indexed one.
        entity.HasIndex(m => m.ResolvedAt);

        return modelBuilder;
    }
}
