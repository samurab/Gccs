using Gccs.Infrastructure.Identity;
using Gccs.Infrastructure.Marketing;
using Gccs.Infrastructure.Notifications;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class AzureCommunicationEmailTransportTests
{
    private static readonly string SyntheticKey = Convert.ToBase64String(new byte[32]);
    private static readonly string[] Flows = ["invitation", "assignment", "demo"];

    public static IEnumerable<object[]> RejectedConfigurations()
    {
        foreach (var flow in Flows)
        {
            foreach (var endpoint in new[] { "http://acs.invalid", "HTTP://acs.invalid", "ftp://acs.invalid", "file:///tmp/acs", "/relative", "" })
            foreach (var managedIdentity in new[] { true, false })
                yield return [flow, managedIdentity, endpoint, ConnectionString(endpoint)];

            foreach (var endpoints in new[]
            {
                "endpoint=http://acs.invalid;endpoint=https://acs.invalid",
                "endpoint=https://acs.invalid;ENDPOINT=http://acs.invalid"
            })
                yield return [flow, false, "https://ignored.invalid", endpoints + ";accesskey=" + SyntheticKey];
        }
    }

    [Theory]
    [MemberData(nameof(RejectedConfigurations))]
    public async Task Senders_reject_insecure_or_ambiguous_endpoints_before_message_processing(
        string flow, bool managedIdentity, string endpoint, string connectionString)
    {
        var sender = CreateSender(flow, managedIdentity, endpoint, connectionString);

        Assert.False(sender.Configured);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(sender.Send);
        Assert.Contains("not configured", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(SyntheticKey, error.Message, StringComparison.Ordinal);
        if (endpoint.Length > 0) Assert.DoesNotContain(endpoint, error.Message, StringComparison.OrdinalIgnoreCase);
    }

    public static IEnumerable<object[]> SecureConfigurations() =>
        from flow in Flows
        from managedIdentity in new[] { true, false }
        from endpoint in new[] { "https://acs.invalid", "HTTPS://acs.invalid" }
        select new object[] { flow, managedIdentity, endpoint };

    [Theory]
    [MemberData(nameof(SecureConfigurations))]
    public void Senders_preserve_both_https_credential_modes_without_sending(
        string flow, bool managedIdentity, string endpoint) =>
        Assert.True(CreateSender(flow, managedIdentity, endpoint, ConnectionString(endpoint)).Configured);

    [Theory]
    [InlineData("invitation", true)]
    [InlineData("invitation", false)]
    [InlineData("assignment", true)]
    [InlineData("assignment", false)]
    [InlineData("demo", true)]
    [InlineData("demo", false)]
    public void Only_the_selected_credential_mode_determines_transport_security(string flow, bool managedIdentity)
    {
        var secure = CreateSender(flow, managedIdentity,
            managedIdentity ? "https://acs.invalid" : "http://unused.invalid",
            ConnectionString(managedIdentity ? "http://unused.invalid" : "https://acs.invalid"));
        var insecure = CreateSender(flow, managedIdentity,
            managedIdentity ? "http://acs.invalid" : "https://unused.invalid",
            ConnectionString(managedIdentity ? "https://unused.invalid" : "http://acs.invalid"));

        Assert.True(secure.Configured);
        Assert.False(insecure.Configured);
    }

    [Theory]
    [InlineData("invitation")]
    [InlineData("assignment")]
    [InlineData("demo")]
    public void Disabled_delivery_remains_unconfigured(string flow) =>
        Assert.False(CreateSender(flow, false, "http://acs.invalid", ConnectionString("http://acs.invalid"), false).Configured);

    [Theory]
    [InlineData("Development", true)]
    [InlineData("Development", false)]
    [InlineData("Staging", true)]
    [InlineData("Staging", false)]
    [InlineData("Production", true)]
    [InlineData("Production", false)]
    public void Enabled_invitation_startup_rejects_http_transport(string environment, bool managedIdentity)
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", "");
            builder.UseSetting("InvitationDelivery:Enabled", "true");
            builder.UseSetting("InvitationDelivery:Provider", "AzureCommunicationServices");
            builder.UseSetting("InvitationDelivery:PublicWebBaseUrl", "https://web.invalid");
            builder.UseSetting("InvitationDelivery:SenderAddress", "sender@example.invalid");
            builder.UseSetting("InvitationDelivery:UseManagedIdentity", managedIdentity.ToString());
            builder.UseSetting("InvitationDelivery:Endpoint", "http://acs.invalid");
            builder.UseSetting("InvitationDelivery:ConnectionString", ConnectionString("http://acs.invalid"));
            builder.ConfigureServices(services => services.RemoveAll<IHostedService>());
        });
        var error = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.Contains("invitation delivery requires", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(SyntheticKey, error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Enabled_demo_configuration_rejects_http_transport(bool development, bool managedIdentity)
    {
        var options = new DemoRequestOptions
        {
            Enabled = true,
            Provider = development ? DemoRequestOptions.DevelopmentRequesterEmailProvider : DemoRequestOptions.AzureCommunicationServicesProvider,
            UseManagedIdentity = managedIdentity,
            Endpoint = "http://acs.invalid",
            ConnectionString = ConnectionString("http://acs.invalid"),
            SenderAddress = "sender@example.invalid",
            RecipientAddress = "recipient@example.invalid",
            PublicWebBaseUrl = "https://web.invalid",
            FollowUpTokenSigningKey = new string('s', 32)
        };
        var error = Assert.Throws<InvalidOperationException>(() => DemoRequestOptions.ValidateEnabledConfiguration(options, development));
        Assert.DoesNotContain(SyntheticKey, error.Message, StringComparison.Ordinal);
    }

    private static string ConnectionString(string endpoint) => $"endpoint={endpoint};accesskey={SyntheticKey}";

    private static (bool Configured, Func<Task> Send) CreateSender(
        string flow, bool managedIdentity, string endpoint, string connectionString, bool enabled = true)
    {
        var options = Options.Create(new InvitationEmailOptions
        {
            Enabled = enabled, UseManagedIdentity = managedIdentity, Endpoint = endpoint,
            ConnectionString = connectionString, SenderAddress = "sender@example.invalid"
        });
        if (flow == "invitation")
        {
            var sender = new AzureCommunicationInvitationEmailSender(options);
            return (sender.IsConfigured, async () => { await sender.SendAsync(null!); });
        }
        if (flow == "assignment")
        {
            var sender = new AzureCommunicationAssignmentEmailSender(options);
            return (sender.IsConfigured, async () => { await sender.SendAsync(null!); });
        }
        var demo = new AzureCommunicationDemoRequestEmailSender(Options.Create(new DemoRequestOptions
        {
            Enabled = enabled, UseManagedIdentity = managedIdentity, Endpoint = endpoint,
            ConnectionString = connectionString, SenderAddress = "sender@example.invalid"
        }));
        return (demo.IsConfigured, async () => { await demo.DeliverAsync(null!); });
    }
}
