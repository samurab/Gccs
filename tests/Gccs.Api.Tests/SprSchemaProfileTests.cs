using Gccs.Application.Reports;
using Gccs.Infrastructure.Reports;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SprSchemaProfileTests
{
    [Fact]
    public async Task Current_profile_is_reviewed_published_effective_and_source_traceable()
    {
        var service = new SprSchemaProfileService(new InMemorySprSchemaProfileRepository());
        var profile = await service.GetCurrentPublishedAsync();

        Assert.Equal(SprSchemaProfileState.Published, profile.State);
        Assert.Equal("1.0", profile.Version);
        Assert.StartsWith("https://", profile.SourceUrl, StringComparison.Ordinal);
        Assert.Equal(64, profile.DefinitionSha256.Length);
        Assert.NotEqual(profile.Owner, profile.Reviewer);
        Assert.Contains(SprReportingPeriod.Final, profile.ReportingPeriods);
    }

    [Fact]
    public async Task Draft_only_or_invalid_published_profiles_fail_closed()
    {
        var draft = InMemorySprSchemaProfileRepository.PublishedProfile with { State = SprSchemaProfileState.Draft };
        await Assert.ThrowsAsync<SprSchemaProfileValidationException>(() =>
            new SprSchemaProfileService(new InMemorySprSchemaProfileRepository([draft])).GetCurrentPublishedAsync());

        var invalid = InMemorySprSchemaProfileRepository.PublishedProfile with { Reviewer = InMemorySprSchemaProfileRepository.PublishedProfile.Owner };
        await Assert.ThrowsAsync<SprSchemaProfileValidationException>(() =>
            new SprSchemaProfileService(new InMemorySprSchemaProfileRepository([invalid])).ListAsync());
    }
}
