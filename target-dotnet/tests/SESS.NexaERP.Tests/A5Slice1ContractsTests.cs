using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Application.Purchase.A5;
using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Tests;

public sealed class A5Slice1ContractsTests
{
    [Fact]
    public void Registry_is_closed_and_maps_all_nineteen_existing_methods()
    {
        var expectedIds = new[]
        {
            "RFQ_CREATE", "RFQ_VENDOR_INVITE", "QUOTATION_REVISION_SUBMIT", "QUOTATION_TECHNICAL_VERIFY",
            "COMPARISON_CREATE", "COMPARISON_RECOMMEND", "COMPARISON_APPROVE", "COMPARISON_REJECT",
            "COMPARISON_REVISION_REQUEST", "COMPARISON_RESUBMIT", "PO_CREATE", "PO_SUBMIT", "PO_ISSUE",
            "PO_AMEND", "PO_REVISE_REJECTED", "PO_APPROVE", "PO_REJECT", "PO_CANCEL",
            "MATERIAL_FOLLOW_UP_TRANSITION"
        };
        var expectedMethods = new[]
        {
            nameof(IRev869BPurchaseService.CreateRfqAsync), nameof(IRev869BPurchaseService.InviteVendorAsync),
            nameof(IRev869BPurchaseService.SubmitQuotationRevisionAsync), nameof(IRev869BPurchaseService.VerifyTechnicalAsync),
            nameof(IRev869BPurchaseService.CreateComparisonAsync), nameof(IRev869BPurchaseService.RecommendAsync),
            nameof(IRev869BPurchaseService.ApproveAsync), nameof(IRev869BPurchaseService.RejectAsync),
            nameof(IRev869BPurchaseService.RequestRevisionAsync), nameof(IRev869BPurchaseService.ResubmitAsync),
            nameof(IRev869BPurchaseService.CreatePurchaseOrderAsync), nameof(IRev869BPurchaseService.SubmitPurchaseOrderAsync),
            nameof(IRev869BPurchaseService.IssuePurchaseOrderAsync), nameof(IRev869BPurchaseService.AmendPurchaseOrderAsync),
            nameof(IRev869BPurchaseService.ReviseRejectedPurchaseOrderAsync), nameof(IRev869BPurchaseService.ApprovePurchaseOrderAsync),
            nameof(IRev869BPurchaseService.RejectPurchaseOrderAsync), nameof(IRev869BPurchaseService.CancelPurchaseOrderAsync),
            nameof(IRev869BPurchaseService.TransitionMaterialFollowUpAsync)
        };

        Assert.Equal(19, A5PurchaseActionRegistry.Count);
        Assert.Equal(expectedIds, A5PurchaseActionRegistry.ActionIds.Select(x => x.ToString()));
        Assert.Equal(expectedMethods, A5PurchaseActionRegistry.ActionIds.Select(x => A5PurchaseActionRegistry.GetBinding(x).BusinessMethodName));
    }

    [Fact]
    public void Type_validation_runs_from_action_to_expected_type_only()
    {
        var shared = new A5ComparisonApprovalParameters("CMP-1", "remarks", 4, "idem");
        A5PurchaseActionRegistry.ValidateParameters(A5PurchaseActionId.COMPARISON_APPROVE, shared);
        A5PurchaseActionRegistry.ValidateParameters(A5PurchaseActionId.COMPARISON_REJECT, shared);
        A5PurchaseActionRegistry.ValidateParameters(A5PurchaseActionId.COMPARISON_REVISION_REQUEST, shared);
        A5PurchaseActionRegistry.ValidateParameters(A5PurchaseActionId.COMPARISON_RESUBMIT, shared);
        Assert.Throws<ArgumentException>(() =>
            A5PurchaseActionRegistry.ValidateParameters(A5PurchaseActionId.COMPARISON_CREATE, shared));

        var po = new A5PurchaseOrderApprovalParameters("PO-1", "remarks", 3, 2, "idem");
        A5PurchaseActionRegistry.ValidateParameters(A5PurchaseActionId.PO_APPROVE, po);
        A5PurchaseActionRegistry.ValidateParameters(A5PurchaseActionId.PO_REJECT, po);
    }

    [Fact]
    public void Every_action_parameter_contract_carries_the_business_idempotency_key()
    {
        foreach (var actionId in A5PurchaseActionRegistry.ActionIds)
        {
            var property = A5PurchaseActionRegistry.GetBinding(actionId).ParameterType.GetProperty("IdempotencyKey");
            Assert.NotNull(property);
            Assert.Equal(typeof(string), property.PropertyType);
        }
    }

    [Fact]
    public void Shared_vendor_registration_vocabulary_is_exact_and_ordinal()
    {
        var expected = new[] { "REGULAR", "COMPOSITION", "UNREGISTERED", "SEZ", "OVERSEAS", "DEEMED_EXPORT", "UIN" };
        Assert.Equal(expected, VendorRegistrationTypes.All.Select(x => x.ToCanonicalValue()));
        foreach (var value in expected) Assert.True(VendorRegistrationTypes.TryParseCanonical(value, out _));
        Assert.False(VendorRegistrationTypes.TryParseCanonical("regular", out _));
        Assert.False(VendorRegistrationTypes.TryParseCanonical(" REGULAR", out _));
        Assert.False(VendorRegistrationTypes.TryParseCanonical("REGULAR ", out _));
    }

    [Fact]
    public void Other_closed_canonical_tokens_preserve_exact_business_casing()
    {
        Assert.Equal(new[] { "EMAIL_RECEIVED", "PHYSICAL_RECEIVED" }, Enum.GetNames<A5SubmissionSource>());
        Assert.Equal(new[] { "InProgress", "Completed" }, Enum.GetNames<A5MaterialFollowUpTargetStatus>());
        var parameters = new A5MaterialFollowUpTransitionParameters(
            Guid.Parse("50000000-0000-0000-0000-000000000005"),
            A5MaterialFollowUpTargetStatus.Completed,
            "received",
            9,
            "idem-follow-up");
        using var document = JsonDocument.Parse(A5PurchaseCanonicalSerializer.Serialize(
            A5PurchaseActionId.MATERIAL_FOLLOW_UP_TRANSITION,
            parameters));
        Assert.Equal("Completed", document.RootElement.GetProperty("toStatus").GetString());
    }

    [Fact]
    public void Every_quotation_decimal_uses_minimum_plain_invariant_form()
    {
        using var document = SerializeQuotation(SampleQuotation());
        var root = document.RootElement;
        var line = root.GetProperty("lines")[0];
        Assert.Equal("1.5", line.GetProperty("quantity").GetRawText());
        Assert.Equal("2.5", line.GetProperty("unitRate").GetRawText());
        Assert.Equal("3", line.GetProperty("discountValue").GetRawText());
        Assert.Equal("4.04", line.GetProperty("packingForwarding").GetRawText());
        Assert.Equal("5", line.GetProperty("freight").GetRawText());
        Assert.Equal("6.6", line.GetProperty("insurance").GetRawText());
        Assert.Equal("7.7", line.GetProperty("otherCharges").GetRawText());
        Assert.Equal("8.8", line.GetProperty("roundOff").GetRawText());
        Assert.Equal("9.9", root.GetProperty("headerDiscountValue").GetRawText());
        Assert.Equal("0", A5PurchaseCanonicalSerializer.NormalizeDecimal(decimal.Negate(decimal.Zero)));
        Assert.Equal(A5PurchaseCanonicalSerializer.NormalizeDecimal(1.50m), A5PurchaseCanonicalSerializer.NormalizeDecimal(1.5m));
    }

    [Fact]
    public void Canonical_parameters_emit_nulls_defaults_versions_dates_order_and_exact_idempotency_key()
    {
        var parameters = SampleQuotation() with
        {
            IdempotencyKey = "  exact-idempotency-key  ",
            Lines = [SampleQuotation().Lines[0], SampleQuotation().Lines[0] with { RequestForQuotationLineId = Guid.Parse("30000000-0000-0000-0000-000000000003") }],
            HeaderDiscountValue = 0m
        };
        using var document = SerializeQuotation(parameters);
        var root = document.RootElement;
        Assert.Equal(JsonValueKind.Null, root.GetProperty("lateAuthorizationRemarks").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("previousQuotationVersion").ValueKind);
        Assert.Equal("0", root.GetProperty("headerDiscountValue").GetRawText());
        Assert.Equal("  exact-idempotency-key  ", root.GetProperty("idempotencyKey").GetString());
        Assert.Equal("2026-08-23T01:38:09.0000000Z", root.GetProperty("receivedAt").GetString());
        Assert.Equal("2026-09-30", root.GetProperty("lines")[0].GetProperty("promisedDeliveryDate").GetString());
        Assert.Equal("20000000-0000-0000-0000-000000000002", root.GetProperty("lines")[0].GetProperty("requestForQuotationLineId").GetString());
        Assert.Equal("30000000-0000-0000-0000-000000000003", root.GetProperty("lines")[1].GetProperty("requestForQuotationLineId").GetString());

        var po = new A5PurchaseOrderApprovalParameters("PO-1", "remarks", 8, null, "idem-po");
        using var poDocument = JsonDocument.Parse(A5PurchaseCanonicalSerializer.Serialize(A5PurchaseActionId.PO_APPROVE, po));
        Assert.Equal(8u, poDocument.RootElement.GetProperty("version").GetUInt32());
        Assert.Equal(JsonValueKind.Null, poDocument.RootElement.GetProperty("expectedCurrentVersion").ValueKind);
    }

    [Fact]
    public void Noncanonical_or_extra_json_and_wrong_enum_case_are_rejected()
    {
        var bytes = A5PurchaseCanonicalSerializer.Serialize(A5PurchaseActionId.QUOTATION_REVISION_SUBMIT, SampleQuotation());
        var json = Encoding.UTF8.GetString(bytes);
        var extra = Encoding.UTF8.GetBytes(json[..^1] + "," + JsonSerializer.Serialize("unknown") + ":1}");
        Assert.Throws<JsonException>(() => A5PurchaseCanonicalSerializer.DeserializeCanonical(A5PurchaseActionId.QUOTATION_REVISION_SUBMIT, extra));

        var wrongCase = Encoding.UTF8.GetBytes(json.Replace("EMAIL_RECEIVED", "email_received", StringComparison.Ordinal));
        Assert.Throws<JsonException>(() => A5PurchaseCanonicalSerializer.DeserializeCanonical(A5PurchaseActionId.QUOTATION_REVISION_SUBMIT, wrongCase));

        var spaced = Encoding.UTF8.GetBytes(" " + json);
        Assert.Throws<JsonException>(() => A5PurchaseCanonicalSerializer.DeserializeCanonical(A5PurchaseActionId.QUOTATION_REVISION_SUBMIT, spaced));
    }

    [Theory]
    [InlineData("../quote.pdf")]
    [InlineData("folder/../quote.pdf")]
    [InlineData("https://host/quote.pdf")]
    [InlineData("/absolute/quote.pdf")]
    [InlineData("C:/quote.pdf")]
    [InlineData("folder\\quote.pdf")]
    [InlineData("folder/quote name.pdf")]
    public void Attachment_object_key_rejects_unsafe_forms(string objectKey)
    {
        var parameters = SampleQuotation() with { AttachmentObjectKey = objectKey };
        Assert.Throws<ArgumentException>(() =>
            A5PurchaseCanonicalSerializer.Serialize(A5PurchaseActionId.QUOTATION_REVISION_SUBMIT, parameters));
    }

    [Fact]
    public void Attachment_constraints_enforce_length_and_lowercase_sha256()
    {
        var tooLong = SampleQuotation() with { AttachmentObjectKey = new string('a', 501) };
        Assert.Throws<ArgumentException>(() =>
            A5PurchaseCanonicalSerializer.Serialize(A5PurchaseActionId.QUOTATION_REVISION_SUBMIT, tooLong));
        var uppercaseHash = SampleQuotation() with { AttachmentSha256 = new string('A', 64) };
        Assert.Throws<ArgumentException>(() =>
            A5PurchaseCanonicalSerializer.Serialize(A5PurchaseActionId.QUOTATION_REVISION_SUBMIT, uppercaseHash));
        var shortHash = SampleQuotation() with { AttachmentSha256 = new string('a', 63) };
        Assert.Throws<ArgumentException>(() =>
            A5PurchaseCanonicalSerializer.Serialize(A5PurchaseActionId.QUOTATION_REVISION_SUBMIT, shortHash));
    }

    [Fact]
    public void Unsigned_plan_is_action_typed_defensive_and_hashes_its_canonical_bytes()
    {
        var plan = A5UnsignedPurchasePlan.Create(
            Guid.Parse("40000000-0000-0000-0000-000000000004"),
            A5PurchaseActionId.QUOTATION_REVISION_SUBMIT,
            SampleQuotation(),
            "ORG-1",
            "VQ-001");
        var unsigned = plan.GetUnsignedCanonicalBytesForFutureSigning();
        Assert.Equal(Convert.ToHexString(SHA256.HashData(unsigned)).ToLowerInvariant(), plan.PlanHash);
        Assert.DoesNotContain("expectedResourceVersion", Encoding.UTF8.GetString(unsigned), StringComparison.Ordinal);
        var first = plan.CanonicalParameters;
        first[0] ^= 0xff;
        Assert.NotEqual(first, plan.CanonicalParameters);
        Assert.Throws<ArgumentException>(() => A5UnsignedPurchasePlan.Create(
            Guid.NewGuid(), A5PurchaseActionId.PO_CREATE, SampleQuotation(), "ORG-1", "VQ-001"));
        Assert.Throws<ArgumentOutOfRangeException>(() => A5PurchaseActionRegistry.GetBinding((A5PurchaseActionId)999));
    }

    [Fact]
    public void Same_parameters_have_byte_identical_canonical_output_in_process()
    {
        var first = A5PurchaseCanonicalSerializer.Serialize(A5PurchaseActionId.QUOTATION_REVISION_SUBMIT, SampleQuotation());
        var second = A5PurchaseCanonicalSerializer.Serialize(A5PurchaseActionId.QUOTATION_REVISION_SUBMIT, SampleQuotation());
        Assert.Equal(first, second);
    }

    [Fact]
    public void A5_source_excludes_forbidden_runtime_dispatch_apis_and_endpoint_uses_shared_allowlist()
    {
        var root = FindRepositoryRoot();
        var a5Root = Path.Combine(root, "src", "SESS.NexaERP.Application", "Purchase", "A5");
        var source = string.Join('\n', Directory.GetFiles(a5Root, "*.cs").Select(File.ReadAllText));
        foreach (var forbidden in new[]
        {
            "System.Reflection", "Type.GetType", "Activator.CreateInstance", "MethodInfo.Invoke",
            "dynamic", "Expression.Compile", "Assembly.Load"
        })
            Assert.DoesNotContain(forbidden, source, StringComparison.Ordinal);

        var endpoint = File.ReadAllText(Path.Combine(root, "src", "SESS.NexaERP.Api", "Endpoints", "Rev869AConfigurationEndpoints.cs"));
        Assert.Contains("VendorRegistrationTypes.TryParseCanonical(request.VendorRegistrationType", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("NormalizeCode(request.VendorRegistrationType)", endpoint, StringComparison.Ordinal);
    }

    private static JsonDocument SerializeQuotation(A5QuotationRevisionSubmitParameters parameters) =>
        JsonDocument.Parse(A5PurchaseCanonicalSerializer.Serialize(A5PurchaseActionId.QUOTATION_REVISION_SUBMIT, parameters));

    private static A5QuotationRevisionSubmitParameters SampleQuotation() => new(
        Guid.Parse("10000000-0000-0000-0000-000000000001"),
        "VQ-001",
        "INR",
        "NET30",
        "DAP",
        "12M",
        false,
        null,
        A5SubmissionSource.EMAIL_RECEIVED,
        new DateTimeOffset(2026, 8, 23, 7, 8, 9, TimeSpan.FromHours(5.5)),
        "purchase/quotes/vq-001.pdf",
        new string('a', 64),
        "Vendor attestation",
        7,
        null,
        "idem-quotation-001",
        [new A5QuotationLineParameters(
            Guid.Parse("20000000-0000-0000-0000-000000000002"),
            1.50m, 2.500m, 3.000m, 4.040m, 5.0m, 6.600m, 7.7000m,
            new DateOnly(2026, 9, 30),
            "8471", "KA", "TN", VendorRegistrationType.REGULAR, 8.80000m)],
        9.9000m);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
