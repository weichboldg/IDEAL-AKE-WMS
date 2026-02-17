using AKEBDELight.Services;
using FluentAssertions;

namespace AKEBDELight.Tests.Services;

public class PasswordServiceTests
{
    private readonly PasswordService _sut = new();

    [Fact]
    public void HashAndVerify_Roundtrip_Succeeds()
    {
        var hash = _sut.HashPassword("test123");
        _sut.VerifyPassword(hash, "test123").Should().BeTrue();
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = _sut.HashPassword("test123");
        _sut.VerifyPassword(hash, "wrongpassword").Should().BeFalse();
    }

    [Fact]
    public void Hash_DifferentSalts_ProduceDifferentHashes()
    {
        var h1 = _sut.HashPassword("test");
        var h2 = _sut.HashPassword("test");
        h1.Should().NotBe(h2);
    }

    [Fact]
    public void Hash_ProducesBase64String()
    {
        var hash = _sut.HashPassword("test");
        var decoded = Convert.FromBase64String(hash);
        decoded.Length.Should().Be(48); // 16 bytes salt + 32 bytes hash
    }

    [Fact]
    public void Verify_InvalidBase64_ReturnsFalse()
    {
        // Wrong length after decode
        var shortHash = Convert.ToBase64String(new byte[20]);
        _sut.VerifyPassword(shortHash, "test").Should().BeFalse();
    }
}
