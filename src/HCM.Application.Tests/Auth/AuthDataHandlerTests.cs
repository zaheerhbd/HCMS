using FluentAssertions;
using FluentValidation;
using HCM.Application.Auth.DataHandlers;
using HCM.Application.Tests.Helpers;
using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HCM.Application.Tests.Auth;

public class AuthDataHandlerTests
{
    private static User NewUser(string username = "jdoe", string password = "Password1!", int failedAttempts = 0, DateTime? lockoutEnd = null)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = $"{username}@hcms.local",
            FirstName = "Jane",
            LastName = "Doe",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IsActive = true,
            FailedLoginAttempts = failedAttempts,
            LockoutEnd = lockoutEnd,
            CreatedAt = DateTime.UtcNow,
            UserRoles = []
        };
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsAccessAndRefreshTokens()
    {
        await using var db = TestDbContextFactory.Create();
        var role = new Role { Id = 1, Name = RoleNames.CareCoordinator };
        var user = NewUser();
        user.UserRoles = [new UserRole { UserId = user.Id, User = user, RoleId = role.Id, Role = role }];
        db.Roles.Add(role);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sut = new AuthDataHandler(db, new FakeTokenService(), NullLogger<AuthDataHandler>.Instance);

        var result = await sut.LoginAsync(user.Username, "Password1!");

        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.Roles.Should().Contain(RoleNames.CareCoordinator);
        (await db.RefreshTokens.CountAsync(rt => rt.UserId == user.Id)).Should().Be(1);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsUnauthorizedAndIncrementsFailedAttempts()
    {
        await using var db = TestDbContextFactory.Create();
        var user = NewUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sut = new AuthDataHandler(db, new FakeTokenService(), NullLogger<AuthDataHandler>.Instance);

        var act = async () => await sut.LoginAsync(user.Username, "WrongPassword!");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        (await db.Users.FindAsync(user.Id))!.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task LoginAsync_AfterFiveFailedAttempts_LocksAccount()
    {
        await using var db = TestDbContextFactory.Create();
        var user = NewUser(failedAttempts: 4);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sut = new AuthDataHandler(db, new FakeTokenService(), NullLogger<AuthDataHandler>.Instance);

        // 5th failed attempt should trip the lockout
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.LoginAsync(user.Username, "WrongPassword!"));

        var reloaded = await db.Users.FindAsync(user.Id);
        reloaded!.FailedLoginAttempts.Should().Be(5);
        reloaded.LockoutEnd.Should().NotBeNull();
        reloaded.IsLockedOut.Should().BeTrue();
    }

    [Fact]
    public async Task LoginAsync_WhenAccountLocked_ThrowsEvenWithCorrectPassword()
    {
        await using var db = TestDbContextFactory.Create();
        var user = NewUser(failedAttempts: 5, lockoutEnd: DateTime.UtcNow.AddMinutes(10));
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sut = new AuthDataHandler(db, new FakeTokenService(), NullLogger<AuthDataHandler>.Instance);

        var act = async () => await sut.LoginAsync(user.Username, "Password1!");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task LoginAsync_SuccessfulLogin_ResetsFailedAttemptCounter()
    {
        await using var db = TestDbContextFactory.Create();
        var role = new Role { Id = 1, Name = RoleNames.Clinician };
        var user = NewUser(failedAttempts: 3);
        user.UserRoles = [new UserRole { UserId = user.Id, User = user, RoleId = role.Id, Role = role }];
        db.Roles.Add(role);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sut = new AuthDataHandler(db, new FakeTokenService(), NullLogger<AuthDataHandler>.Instance);
        await sut.LoginAsync(user.Username, "Password1!");

        (await db.Users.FindAsync(user.Id))!.FailedLoginAttempts.Should().Be(0);
    }

    [Theory]
    [InlineData("", "Password1!")]
    [InlineData("jdoe", "")]
    public async Task LoginAsync_WithMissingCredentials_ThrowsValidationException(string username, string password)
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new AuthDataHandler(db, new FakeTokenService(), NullLogger<AuthDataHandler>.Instance);

        var act = async () => await sut.LoginAsync(username, password);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
