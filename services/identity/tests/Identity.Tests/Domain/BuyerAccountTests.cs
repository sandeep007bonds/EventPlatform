namespace Identity.Tests.Domain;

public sealed class BuyerAccountTests
{
    [Fact]
    public void CreateFromVerification_StampsCreatedAndLastVerifiedAtTheSameTime()
    {
        var account = BuyerAccount.CreateFromVerification("+15550000000");

        account.PhoneNumber.ShouldBe("+15550000000");
        account.LastVerifiedAt.ShouldBe(account.CreatedAt);
    }

    [Fact]
    public void RecordVerification_UpdatesLastVerifiedAt_NotCreatedAt()
    {
        var account = BuyerAccount.CreateFromVerification("+15550000000");
        var createdAt = account.CreatedAt;
        var later = createdAt.AddDays(30);

        account.RecordVerification(later);

        account.CreatedAt.ShouldBe(createdAt);
        account.LastVerifiedAt.ShouldBe(later);
    }
}
