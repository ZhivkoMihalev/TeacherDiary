using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using TeacherDiary.Infrastructure.Services;
using Xunit;

namespace TeacherDiary.Tests.Services;

public class CurrentUserTests
{
    private static readonly Guid SomeId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static CurrentUser CreateSut(HttpContext? ctx)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(ctx!);
        return new CurrentUser(accessor.Object);
    }

    // Creates a CurrentUser with a non-null HttpContext but the given User (may be null).
    private static CurrentUser CreateSutWithMockedHttpContext(ClaimsPrincipal? user)
    {
        var mockCtx = new Mock<HttpContext>();
        mockCtx.Setup(x => x.User).Returns(user!);
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(mockCtx.Object);
        return new CurrentUser(accessor.Object);
    }

    private static DefaultHttpContext MakeContext(IEnumerable<Claim> claims, bool authenticated = true)
    {
        var identity = new ClaimsIdentity(claims, authenticated ? "Test" : null);
        return new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
    }

    // -----------------------------------------------------------------------
    // UserId
    // -----------------------------------------------------------------------

    [Fact]
    public void UserId_WhenHttpContextIsNull_ReturnsGuidEmpty()
    {
        var sut = CreateSut(null);

        Assert.Equal(Guid.Empty, sut.UserId);
    }

    [Fact]
    public void UserId_WhenNameIdentifierClaimPresent_ReturnsThatGuid()
    {
        var ctx = MakeContext([new Claim(ClaimTypes.NameIdentifier, SomeId.ToString())]);
        var sut = CreateSut(ctx);

        Assert.Equal(SomeId, sut.UserId);
    }

    [Fact]
    public void UserId_WhenOnlySubClaimPresent_ReturnsThatGuid()
    {
        // NameIdentifier is absent → ?? falls through to "sub"
        var ctx = MakeContext([new Claim("sub", SomeId.ToString())]);
        var sut = CreateSut(ctx);

        Assert.Equal(SomeId, sut.UserId);
    }

    [Fact]
    public void UserId_WhenNeitherClaimPresent_ReturnsGuidEmpty()
    {
        // Both sides of ?? are null → TryParse(null) → false → Guid.Empty
        var ctx = MakeContext([]);
        var sut = CreateSut(ctx);

        Assert.Equal(Guid.Empty, sut.UserId);
    }

    [Fact]
    public void UserId_WhenUserIsNull_ReturnsGuidEmpty()
    {
        // HttpContext non-null but User is null → ?.User null branch fires → TryParse(null) → Guid.Empty
        var sut = CreateSutWithMockedHttpContext(null);

        Assert.Equal(Guid.Empty, sut.UserId);
    }

    // -----------------------------------------------------------------------
    // OrganizationId
    // -----------------------------------------------------------------------

    [Fact]
    public void OrganizationId_WhenHttpContextIsNull_ReturnsGuidEmpty()
    {
        var sut = CreateSut(null);

        Assert.Equal(Guid.Empty, sut.OrganizationId);
    }

    [Fact]
    public void OrganizationId_WhenClaimPresent_ReturnsThatGuid()
    {
        var ctx = MakeContext([new Claim("organizationId", SomeId.ToString())]);
        var sut = CreateSut(ctx);

        Assert.Equal(SomeId, sut.OrganizationId);
    }

    [Fact]
    public void OrganizationId_WhenClaimAbsent_ReturnsGuidEmpty()
    {
        var ctx = MakeContext([]);
        var sut = CreateSut(ctx);

        Assert.Equal(Guid.Empty, sut.OrganizationId);
    }

    [Fact]
    public void OrganizationId_WhenUserIsNull_ReturnsGuidEmpty()
    {
        // HttpContext non-null but User is null → ?.User null branch fires → TryParse(null) → Guid.Empty
        var sut = CreateSutWithMockedHttpContext(null);

        Assert.Equal(Guid.Empty, sut.OrganizationId);
    }

    // -----------------------------------------------------------------------
    // IsInRole
    // -----------------------------------------------------------------------

    [Fact]
    public void IsInRole_WhenHttpContextIsNull_ReturnsFalse()
    {
        // HttpContext?.User → null → ?? false
        var sut = CreateSut(null);

        Assert.False(sut.IsInRole("Admin"));
    }

    [Fact]
    public void IsInRole_WhenUserIsInRole_ReturnsTrue()
    {
        var ctx = MakeContext([new Claim(ClaimTypes.Role, "Admin")]);
        var sut = CreateSut(ctx);

        Assert.True(sut.IsInRole("Admin"));
    }

    [Fact]
    public void IsInRole_WhenUserIsNotInRole_ReturnsFalse()
    {
        // IsInRole returns false (non-null bool?) → ?? false left is non-null → false
        var ctx = MakeContext([]);
        var sut = CreateSut(ctx);

        Assert.False(sut.IsInRole("Admin"));
    }

    [Fact]
    public void IsInRole_WhenUserIsNull_ReturnsFalse()
    {
        // HttpContext non-null but User is null → ?.User null branch → ?? false
        var sut = CreateSutWithMockedHttpContext(null);

        Assert.False(sut.IsInRole("Admin"));
    }

    // -----------------------------------------------------------------------
    // IsAuthenticated
    // -----------------------------------------------------------------------

    [Fact]
    public void IsAuthenticated_WhenHttpContextIsNull_ReturnsFalse()
    {
        // HttpContext?.User?.Identity?.IsAuthenticated → null → ?? false
        var sut = CreateSut(null);

        Assert.False(sut.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_WhenIdentityIsAuthenticated_ReturnsTrue()
    {
        var ctx = MakeContext([], authenticated: true);
        var sut = CreateSut(ctx);

        Assert.True(sut.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_WhenIdentityIsNotAuthenticated_ReturnsFalse()
    {
        // authenticationType = null → ClaimsIdentity.IsAuthenticated = false (non-null bool?) → ?? false left non-null → false
        var ctx = MakeContext([], authenticated: false);
        var sut = CreateSut(ctx);

        Assert.False(sut.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_WhenUserIsNull_ReturnsFalse()
    {
        // HttpContext non-null, User is null → ?.User null branch → null → ?? false
        var sut = CreateSutWithMockedHttpContext(null);

        Assert.False(sut.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_WhenIdentityIsNull_ReturnsFalse()
    {
        // ClaimsPrincipal with no identities → Identity returns null → ?.Identity null branch → ?? false
        var sut = CreateSutWithMockedHttpContext(new ClaimsPrincipal());

        Assert.False(sut.IsAuthenticated);
    }
}
