using FluentAssertions;
using HCM.Application.Notifications.DataHandlers;
using HCM.Application.Tests.Helpers;
using Xunit;

namespace HCM.Application.Tests.Notifications;

public class NotificationDataHandlerTests
{
    [Fact]
    public async Task CreateAsync_PersistsUnreadNotification()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new NotificationDataHandler(db);
        var userId = Guid.NewGuid();

        await sut.CreateAsync(userId, "CaseAssigned", "Case assigned to you", "Case CASE-2026-00001 assigned");

        var unread = await sut.GetUnreadCountAsync(userId);
        unread.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetUnreadCountAsync_OnlyCountsCurrentUsersUnreadNotifications()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new NotificationDataHandler(db);
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        await sut.CreateAsync(userA, "CaseAssigned", "t", "m");
        await sut.CreateAsync(userB, "CaseAssigned", "t", "m");
        await sut.CreateAsync(userB, "CaseAssigned", "t", "m");

        (await sut.GetUnreadCountAsync(userA)).Count.Should().Be(1);
        (await sut.GetUnreadCountAsync(userB)).Count.Should().Be(2);
    }

    [Fact]
    public async Task MarkReadAsync_DecrementsUnreadCount()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new NotificationDataHandler(db);
        var userId = Guid.NewGuid();
        await sut.CreateAsync(userId, "CaseAssigned", "t", "m");
        var list = await sut.GetMyNotificationsAsync(userId);
        var notificationId = list.Items.Single().Id;

        await sut.MarkReadAsync(userId, notificationId);

        (await sut.GetUnreadCountAsync(userId)).Count.Should().Be(0);
    }

    [Fact]
    public async Task MarkReadAsync_ForAnotherUsersNotification_DoesNothing()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new NotificationDataHandler(db);
        var owner = Guid.NewGuid();
        var intruder = Guid.NewGuid();
        await sut.CreateAsync(owner, "CaseAssigned", "t", "m");
        var notificationId = (await sut.GetMyNotificationsAsync(owner)).Items.Single().Id;

        await sut.MarkReadAsync(intruder, notificationId);

        (await sut.GetUnreadCountAsync(owner)).Count.Should().Be(1);
    }

    [Fact]
    public async Task MarkAllReadAsync_ClearsAllUnreadForUser()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new NotificationDataHandler(db);
        var userId = Guid.NewGuid();
        await sut.CreateAsync(userId, "CaseAssigned", "t", "m1");
        await sut.CreateAsync(userId, "CaseEscalated", "t", "m2");

        await sut.MarkAllReadAsync(userId);

        (await sut.GetUnreadCountAsync(userId)).Count.Should().Be(0);
    }

    [Fact]
    public async Task GetMyNotificationsAsync_OrdersByNewestFirst()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new NotificationDataHandler(db);
        var userId = Guid.NewGuid();
        await sut.CreateAsync(userId, "CaseAssigned", "First", "m1");
        await sut.CreateAsync(userId, "CaseAssigned", "Second", "m2");

        var list = await sut.GetMyNotificationsAsync(userId);

        list.Items.First().Title.Should().Be("Second");
    }
}
