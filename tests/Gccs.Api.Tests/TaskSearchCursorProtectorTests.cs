using Gccs.Api;
using Gccs.Application.Tasks;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class TaskSearchCursorProtectorTests
{
    [Fact]
    public void Cursor_expires_and_remains_bound_to_tenant_and_filters()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.Zero));
        var protector = new TaskSearchCursorProtector(new TaskSearchCursorOptions
        {
            CursorSigningKey = Convert.ToBase64String(new byte[32])
        }, clock);
        var tenantId = Guid.NewGuid();
        var position = new ComplianceTaskCursor(new DateOnly(2026, 9, 10), clock.GetUtcNow(), Guid.NewGuid());
        var protectedCursor = protector.Protect(tenantId, "filters", position);

        Assert.True(protector.TryUnprotect(protectedCursor, tenantId, "filters", out var decoded));
        Assert.Equal(position, decoded);
        Assert.False(protector.TryUnprotect(protectedCursor, Guid.NewGuid(), "filters", out _));
        Assert.False(protector.TryUnprotect(protectedCursor, tenantId, "different", out _));

        clock.Advance(TimeSpan.FromMinutes(16));
        Assert.False(protector.TryUnprotect(protectedCursor, tenantId, "filters", out _));
    }

    [Fact]
    public void Previous_key_accepts_in_flight_cursor_during_rotation()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.Zero));
        var oldKey = Convert.ToBase64String(Enumerable.Repeat((byte)1, 32).ToArray());
        var newKey = Convert.ToBase64String(Enumerable.Repeat((byte)2, 32).ToArray());
        var tenantId = Guid.NewGuid();
        var position = new ComplianceTaskCursor(null, clock.GetUtcNow(), Guid.NewGuid());
        var oldProtector = new TaskSearchCursorProtector(new TaskSearchCursorOptions { CursorSigningKey = oldKey }, clock);
        var protectedCursor = oldProtector.Protect(tenantId, "filters", position);
        var rotatedProtector = new TaskSearchCursorProtector(new TaskSearchCursorOptions
        {
            CursorSigningKey = newKey,
            PreviousCursorSigningKey = oldKey
        }, clock);

        Assert.True(rotatedProtector.TryUnprotect(protectedCursor, tenantId, "filters", out var decoded));
        Assert.Equal(position, decoded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-base64")]
    [InlineData("YQ==")]
    public void Signing_key_validation_fails_closed(string key)
    {
        var options = new TaskSearchCursorOptions { CursorSigningKey = key };
        Assert.Throws<InvalidOperationException>(() => TaskSearchCursorOptions.Validate(options));
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
    }
}
