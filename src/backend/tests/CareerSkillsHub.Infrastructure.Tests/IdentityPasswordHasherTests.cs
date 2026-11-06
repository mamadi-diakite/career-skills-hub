namespace CareerSkillsHub.Infrastructure.Tests;

public sealed class IdentityPasswordHasherTests
{
    private const string Password = "CareerDemo123!";

    private readonly IdentityPasswordHasher _hasher = new();

    [Fact]
    public void Hash_does_not_contain_the_password_and_is_salted()
    {
        var hash = _hasher.Hash(Password);

        Assert.DoesNotContain(Password, hash);
        Assert.NotEqual(hash, _hasher.Hash(Password));
    }

    [Fact]
    public void Verify_accepts_the_right_password()
    {
        Assert.True(_hasher.Verify(_hasher.Hash(Password), Password));
    }

    [Theory]
    [InlineData("careerdemo123!")]
    [InlineData("CareerDemo123")]
    [InlineData("another password")]
    public void Verify_rejects_a_wrong_password(string password)
    {
        Assert.False(_hasher.Verify(_hasher.Hash(Password), password));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Hash_rejects_a_missing_password(string? password)
    {
        Assert.ThrowsAny<ArgumentException>(() => _hasher.Hash(password!));
    }

    [Theory]
    [InlineData(null, Password)]
    [InlineData("", Password)]
    [InlineData("hash", null)]
    [InlineData("hash", "")]
    public void Verify_rejects_missing_arguments(string? hash, string? password)
    {
        Assert.ThrowsAny<ArgumentException>(() => _hasher.Verify(hash!, password!));
    }
}
