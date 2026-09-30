using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SESS.NexaERP.Infrastructure.Email;

namespace SESS.NexaERP.Tests;

public sealed class EmailLiteConfigurationTests
{
    private static EmailLiteOptions Enabled() => new()
    {
        Enabled = true, From = "erp@example.invalid", AllowList = ["td@example.invalid"],
        Smtp = new() { Host = "mail.example.invalid", User = "erp@example.invalid" }
    };

    [Fact]
    public void MissingConfigurationLeavesDeliveryDisabledButValid()
    {
        using var services = new ServiceCollection().AddEmailLite(new ConfigurationBuilder().Build()).BuildServiceProvider();
        var options = services.GetRequiredService<IOptions<EmailLiteOptions>>().Value;
        Assert.False(options.Enabled);
        Assert.Equal(EmailRecipientDecision.Disabled, EmailRecipientPolicy.Evaluate(options, ["vendor@example.invalid"], []));
        Assert.True(new EmailLiteOptionsValidator().Validate(null, options).Succeeded);
    }

    [Fact]
    public void OperatorConfigurationBindsAndInvalidEnabledConfigurationIsRefused()
    {
        var values = new Dictionary<string,string?>
        {
            ["Email:Enabled"]="true", ["Email:Mode"]="TEST", ["Email:AllowList:0"]="td@example.invalid",
            ["Email:From"]="erp@example.invalid", ["Email:Smtp:Host"]="mail.example.invalid",
            ["Email:Smtp:Port"]="465", ["Email:Smtp:Security"]="SslOnConnect", ["Email:Smtp:User"]="erp@example.invalid",
            ["Email:HourlyLimit"]="50", ["Email:DigestDays"]="Mon-Sat", ["Email:DigestTimeIst"]="09:00",
            ["Email:FromNamePerCompany:SESS_PVT_LTD"]="Example company"
        };
        using var services = new ServiceCollection().AddEmailLite(new ConfigurationBuilder().AddInMemoryCollection(values).Build()).BuildServiceProvider();
        var options = services.GetRequiredService<IOptions<EmailLiteOptions>>().Value;
        Assert.Equal(EmailRecipientDecision.Allowed, EmailRecipientPolicy.Evaluate(options, ["td@example.invalid"], []));
        Assert.Equal("Example company", options.FromNamePerCompany["SESS_PVT_LTD"]);
        values["Email:Smtp:Security"]="None";
        using var invalid = new ServiceCollection().AddEmailLite(new ConfigurationBuilder().AddInMemoryCollection(values).Build()).BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() => invalid.GetRequiredService<IOptions<EmailLiteOptions>>().Value);
    }

    [Theory]
    [InlineData("None",465)]
    [InlineData("Auto",465)]
    [InlineData("StartTlsWhenAvailable",465)]
    [InlineData("SslOnConnect",0)]
    [InlineData("SslOnConnect",65536)]
    public void InvalidOrInsecureTransportConfigurationIsRejected(string security,int port)
    {
        var options=Enabled();options.Smtp.Security=security;options.Smtp.Port=port;
        Assert.True(new EmailLiteOptionsValidator().Validate(null,options).Failed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void HourlyLimitCannotDisableOrExceedTheR1Cap(int limit)
    {
        var options=Enabled();options.HourlyLimit=limit;
        Assert.True(new EmailLiteOptionsValidator().Validate(null,options).Failed);
    }

    [Fact]
    public void VendorPoSendingRequiresPurchaseCcAndTestModeRequiresAllowList()
    {
        var options=Enabled();options.VendorPoEmailEnabled=true;
        Assert.True(new EmailLiteOptionsValidator().Validate(null,options).Failed);
        options.PurchaseMailbox="purchase@example.invalid";
        Assert.True(new EmailLiteOptionsValidator().Validate(null,options).Succeeded);
        options.AllowList=[];
        Assert.True(new EmailLiteOptionsValidator().Validate(null,options).Failed);
        options.Mode="LIVE";
        Assert.True(new EmailLiteOptionsValidator().Validate(null,options).Succeeded);
    }

    [Fact]
    public void EveryToAndCcRecipientMustBeAllowListedWithoutRedirection()
    {
        var options=Enabled();
        Assert.Equal(EmailRecipientDecision.Allowed,EmailRecipientPolicy.Evaluate(options,[" TD@example.invalid "],["td@example.invalid"]));
        var to=new[]{"td@example.invalid"};var cc=new[]{"vendor@example.invalid"};
        Assert.Equal(EmailRecipientDecision.BlockedAllowList,EmailRecipientPolicy.Evaluate(options,to,cc));
        Assert.Equal("vendor@example.invalid",Assert.Single(cc));
        Assert.Equal(EmailRecipientDecision.BlockedAllowList,EmailRecipientPolicy.Evaluate(options,["vendor@example.invalid"],[]));
        options.AllowList=[];
        Assert.Equal(EmailRecipientDecision.InvalidConfiguration,EmailRecipientPolicy.Evaluate(options,to,[]));
        options.Mode="mistyped";
        Assert.Equal(EmailRecipientDecision.InvalidConfiguration,EmailRecipientPolicy.Evaluate(options,to,[]));
    }

    [Theory]
    [InlineData("not-an-address")]
    [InlineData("first@example.invalid,second@example.invalid")]
    [InlineData("Name <td@example.invalid>")]
    [InlineData("td@example.invalid\r\nBcc: other@example.invalid")]
    public void InvalidAddressesCannotUseTestOrLiveSendPaths(string address)
    {
        var options=Enabled();
        foreach(var mode in new[]{"TEST","LIVE"})
        {
            options.Mode=mode;
            Assert.Equal(EmailRecipientDecision.InvalidRecipient,EmailRecipientPolicy.Evaluate(options,[address],[]));
            Assert.Equal(EmailRecipientDecision.InvalidRecipient,EmailRecipientPolicy.Evaluate(options,["td@example.invalid"],[address]));
        }
        Assert.Equal(EmailRecipientDecision.InvalidRecipient,EmailRecipientPolicy.Evaluate(options,[],[]));
    }

    [Fact]
    public void DigestScheduleUsesConfiguredDaysAndRefusesInvalidCalendarInput()
    {
        Assert.True(EmailDigestSchedule.TryParseDays("Mon-Sat",out var days));
        Assert.Equal(6,days.Count);Assert.DoesNotContain(DayOfWeek.Sunday,days);
        Assert.True(EmailDigestSchedule.TryParseDays("Fri-Mon,Wed",out days));
        Assert.True(days.SetEquals(new[]{DayOfWeek.Friday,DayOfWeek.Saturday,DayOfWeek.Sunday,DayOfWeek.Monday,DayOfWeek.Wednesday}));
        foreach(var value in new[]{"", "Monday", "Mon,,Tue", "Mon-Noday"})
            Assert.False(EmailDigestSchedule.TryParseDays(value,out _));
        var options=Enabled();options.DigestTimeIst="25:00";
        Assert.True(new EmailLiteOptionsValidator().Validate(null,options).Failed);
    }
}
