namespace Queue.Tests.Domain;

public sealed class QueueSettingsTests
{
    [Fact]
    public void Create_SetsSensiblePacingDefaults()
    {
        var settings = QueueSettings.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), enabled: true);

        settings.Enabled.ShouldBeTrue();
        settings.AdmissionRatePerInterval.ShouldBeGreaterThan(0);
        settings.IntervalSeconds.ShouldBeGreaterThan(0);
        settings.SessionTtlSeconds.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void UpdateTuning_WithValidValues_UpdatesThePacingKnobs()
    {
        var settings = QueueSettings.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), enabled: true);

        settings.UpdateTuning(admissionRatePerInterval: 100, intervalSeconds: 5, sessionTtlSeconds: 300);

        settings.AdmissionRatePerInterval.ShouldBe(100);
        settings.IntervalSeconds.ShouldBe(5);
        settings.SessionTtlSeconds.ShouldBe(300);
    }

    [Fact]
    public void UpdateTuning_NeverChangesEnabled()
    {
        var settings = QueueSettings.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), enabled: true);

        settings.UpdateTuning(50, 10, 600);

        settings.Enabled.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0, 10, 600)]
    [InlineData(50, 0, 600)]
    [InlineData(50, 10, 0)]
    [InlineData(-1, 10, 600)]
    public void UpdateTuning_WithANonPositiveValue_Throws(int rate, int interval, int ttl)
    {
        var settings = QueueSettings.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), enabled: true);

        Should.Throw<ArgumentOutOfRangeException>(() => settings.UpdateTuning(rate, interval, ttl));
    }
}
