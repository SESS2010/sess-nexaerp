using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
    public void Closed_enum_deserialization_rejects_unknown_and_case_variant_tokens()
    {
        var followUp = new A5MaterialFollowUpTransitionParameters(
            Guid.Parse("50000000-0000-0000-0000-000000000005"),
            A5MaterialFollowUpTargetStatus.InProgress,
            "received",
            9,
            "idem-follow-up");
        var inProgress = A5PurchaseCanonicalSerializer.Serialize(
            A5PurchaseActionId.MATERIAL_FOLLOW_UP_TRANSITION, followUp);
        using (var document = JsonDocument.Parse(inProgress))
            Assert.Equal("InProgress", document.RootElement.GetProperty("toStatus").GetString());
        var completed = A5PurchaseCanonicalSerializer.Serialize(
            A5PurchaseActionId.MATERIAL_FOLLOW_UP_TRANSITION,
            followUp with { ToStatus = A5MaterialFollowUpTargetStatus.Completed });
        using (var document = JsonDocument.Parse(completed))
            Assert.Equal("Completed", document.RootElement.GetProperty("toStatus").GetString());

        var followUpJson = Encoding.UTF8.GetString(inProgress);
        foreach (var invalid in new[] { "Unknown", "inprogress", "INPROGRESS", "Inprogress" })
        {
            var bytes = Encoding.UTF8.GetBytes(followUpJson.Replace("InProgress", invalid, StringComparison.Ordinal));
            Assert.Throws<JsonException>(() => A5PurchaseCanonicalSerializer.DeserializeCanonical(
                A5PurchaseActionId.MATERIAL_FOLLOW_UP_TRANSITION, bytes));
        }

        var quotation = Encoding.UTF8.GetString(A5PurchaseCanonicalSerializer.Serialize(
            A5PurchaseActionId.QUOTATION_REVISION_SUBMIT, SampleQuotation()));
        foreach (var invalidVendor in new[] { "Regular", "regular" })
        {
            var bytes = Encoding.UTF8.GetBytes(quotation.Replace("REGULAR", invalidVendor, StringComparison.Ordinal));
            Assert.Throws<JsonException>(() => A5PurchaseCanonicalSerializer.DeserializeCanonical(
                A5PurchaseActionId.QUOTATION_REVISION_SUBMIT, bytes));
        }
        var invalidSource = Encoding.UTF8.GetBytes(
            quotation.Replace("EMAIL_RECEIVED", "Email_Received", StringComparison.Ordinal));
        Assert.Throws<JsonException>(() => A5PurchaseCanonicalSerializer.DeserializeCanonical(
            A5PurchaseActionId.QUOTATION_REVISION_SUBMIT, invalidSource));
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
    public void All_nine_decimals_use_plain_notation_and_normalize_scale_and_zero()
    {
        const decimal verySmall = 0.0000000000000000000000000001m;
        const decimal veryLarge = 79228162514264337593543950335m;

        Assert.All(QuotationDecimalTexts(WithAllQuotationDecimals(verySmall)), text =>
        {
            Assert.Equal("0.0000000000000000000000000001", text);
            Assert.DoesNotContain("e", text, StringComparison.OrdinalIgnoreCase);
        });
        Assert.All(QuotationDecimalTexts(WithAllQuotationDecimals(veryLarge)), text =>
        {
            Assert.Equal("79228162514264337593543950335", text);
            Assert.DoesNotContain("e", text, StringComparison.OrdinalIgnoreCase);
        });

        var scaled = A5PurchaseCanonicalSerializer.Serialize(
            A5PurchaseActionId.QUOTATION_REVISION_SUBMIT, WithAllQuotationDecimals(1.50m));
        var minimal = A5PurchaseCanonicalSerializer.Serialize(
            A5PurchaseActionId.QUOTATION_REVISION_SUBMIT, WithAllQuotationDecimals(1.5m));
        Assert.Equal(scaled, minimal);

        Assert.All(QuotationDecimalTexts(WithAllQuotationDecimals(decimal.Negate(decimal.Zero))), text =>
        {
            Assert.Equal("0", text);
            Assert.NotEqual("-0", text);
        });
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
    public void Every_action_emits_exactly_its_complete_version_contract()
    {
        AssertVersions(A5PurchaseActionId.RFQ_CREATE,
            new A5RfqCreateParameters(DateTimeOffset.UnixEpoch, "INR", false, null, "i", []));
        AssertVersions(A5PurchaseActionId.RFQ_VENDOR_INVITE,
            new A5RfqVendorInviteParameters("R", Guid.NewGuid(), "r", 11, "i"), ("rfqVersion", 11));
        AssertVersions(A5PurchaseActionId.QUOTATION_REVISION_SUBMIT, SampleQuotation(),
            ("invitationVersion", 7), ("previousQuotationVersion", null));
        AssertVersions(A5PurchaseActionId.QUOTATION_TECHNICAL_VERIFY,
            new A5QuotationTechnicalVerifyParameters("Q", Guid.NewGuid(), true, "{}", "r", 12, "i"),
            ("quotationVersion", 12));
        AssertVersions(A5PurchaseActionId.COMPARISON_CREATE,
            new A5ComparisonCreateParameters("R", 13, "i"), ("rfqVersion", 13));
        AssertVersions(A5PurchaseActionId.COMPARISON_RECOMMEND,
            new A5ComparisonRecommendParameters("C", Guid.NewGuid(), "r", null, 14, "i"), ("version", 14));

        var approval = new A5ComparisonApprovalParameters("C", "r", 15, "i");
        AssertVersions(A5PurchaseActionId.COMPARISON_APPROVE, approval, ("version", 15));
        AssertVersions(A5PurchaseActionId.COMPARISON_REJECT, approval, ("version", 15));
        AssertVersions(A5PurchaseActionId.COMPARISON_REVISION_REQUEST, approval, ("version", 15));
        AssertVersions(A5PurchaseActionId.COMPARISON_RESUBMIT, approval, ("version", 15));
        AssertVersions(A5PurchaseActionId.PO_CREATE,
            new A5PurchaseOrderCreateParameters("C", 16, "i"), ("comparisonVersion", 16));
        AssertVersions(A5PurchaseActionId.PO_SUBMIT,
            new A5PurchaseOrderSubmitParameters("P", "r", 17, "i"), ("version", 17));
        AssertVersions(A5PurchaseActionId.PO_ISSUE,
            new A5PurchaseOrderIssueParameters("P", "r", 18, "i"), ("version", 18));
        AssertVersions(A5PurchaseActionId.PO_AMEND,
            new A5PurchaseOrderAmendParameters("P", "r", "p", "d", "w", 19, "i"), ("version", 19));
        AssertVersions(A5PurchaseActionId.PO_REVISE_REJECTED,
            new A5PurchaseOrderReviseRejectedParameters("P", "r", "p", "d", "w", 20, "i"),
            ("rejectedVersion", 20));

        var poApproval = new A5PurchaseOrderApprovalParameters("P", "r", 21, 20, "i");
        AssertVersions(A5PurchaseActionId.PO_APPROVE, poApproval,
            ("version", 21), ("expectedCurrentVersion", 20));
        AssertVersions(A5PurchaseActionId.PO_REJECT, poApproval,
            ("version", 21), ("expectedCurrentVersion", 20));
        AssertVersions(A5PurchaseActionId.PO_CANCEL,
            new A5PurchaseOrderCancelParameters("P", "r", 22, "i"), ("version", 22));
        AssertVersions(A5PurchaseActionId.MATERIAL_FOLLOW_UP_TRANSITION,
            new A5MaterialFollowUpTransitionParameters(Guid.NewGuid(), A5MaterialFollowUpTargetStatus.InProgress, "r", 23, "i"),
            ("version", 23));

        using var previousVersion = SerializeQuotation(SampleQuotation() with { PreviousQuotationVersion = 6 });
        Assert.Equal(6u, previousVersion.RootElement.GetProperty("previousQuotationVersion").GetUInt32());
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

    [Fact]
    public void Canonical_parser_rejects_duplicate_properties()
    {
        var json = CanonicalApprovalJson();
        var duplicate = Encoding.UTF8.GetBytes(
            json[..^1] + "," + JsonSerializer.Serialize("version") + ":4}");
        Assert.Throws<JsonException>(() => A5PurchaseCanonicalSerializer.DeserializeCanonical(
            A5PurchaseActionId.COMPARISON_APPROVE, duplicate));
    }

    [Fact]
    public void Canonical_parser_rejects_comments()
    {
        var commented = Encoding.UTF8.GetBytes("/*comment*/" + CanonicalApprovalJson());
        Assert.Throws<JsonException>(() => A5PurchaseCanonicalSerializer.DeserializeCanonical(
            A5PurchaseActionId.COMPARISON_APPROVE, commented));
    }

    [Fact]
    public void Canonical_parser_rejects_trailing_commas()
    {
        var json = CanonicalApprovalJson();
        var trailing = Encoding.UTF8.GetBytes(json[..^1] + ",}");
        Assert.Throws<JsonException>(() => A5PurchaseCanonicalSerializer.DeserializeCanonical(
            A5PurchaseActionId.COMPARISON_APPROVE, trailing));
    }

    [Fact]
    public void Canonical_parser_rejects_missing_required_members()
    {
        var value = JsonNode.Parse(CanonicalApprovalJson())!.AsObject();
        Assert.True(value.Remove("remarks"));
        var missing = Encoding.UTF8.GetBytes(value.ToJsonString());
        Assert.Throws<JsonException>(() => A5PurchaseCanonicalSerializer.DeserializeCanonical(
            A5PurchaseActionId.COMPARISON_APPROVE, missing));
    }

    [Fact]
    public void Canonical_parser_rejects_null_non_nullable_collections()
    {
        var value = JsonNode.Parse(A5PurchaseCanonicalSerializer.Serialize(
            A5PurchaseActionId.QUOTATION_REVISION_SUBMIT, SampleQuotation()))!.AsObject();
        value["lines"] = null;
        var nullLines = Encoding.UTF8.GetBytes(value.ToJsonString());
        Assert.Throws<JsonException>(() => A5PurchaseCanonicalSerializer.DeserializeCanonical(
            A5PurchaseActionId.QUOTATION_REVISION_SUBMIT, nullLines));
    }

    [Fact]
    public void Canonical_parser_rejects_input_beyond_the_depth_limit()
    {
        var depth = A5PurchaseCanonicalSerializer.CanonicalMaxDepth + 1;
        var deeplyNested = Encoding.UTF8.GetBytes(new string('[', depth) + "0" + new string(']', depth));
        Assert.Throws<JsonException>(() => A5PurchaseCanonicalSerializer.DeserializeCanonical(
            A5PurchaseActionId.COMPARISON_APPROVE, deeplyNested));
    }

    [Fact]
    public void Uint_canonical_form_emits_zero_and_rejects_leading_zero_or_signs()
    {
        var parameters = new A5ComparisonApprovalParameters("CMP-1", "remarks", 0, "idem");
        var canonical = A5PurchaseCanonicalSerializer.Serialize(A5PurchaseActionId.COMPARISON_APPROVE, parameters);
        using (var document = JsonDocument.Parse(canonical))
            Assert.Equal("0", document.RootElement.GetProperty("version").GetRawText());

        var json = Encoding.UTF8.GetString(canonical);
        var marker = JsonSerializer.Serialize("version") + ":0";
        foreach (var invalidNumber in new[] { "00", "+0", "-0", "+1", "-1" })
        {
            var bytes = Encoding.UTF8.GetBytes(json.Replace(marker, JsonSerializer.Serialize("version") + ":" + invalidNumber, StringComparison.Ordinal));
            Assert.Throws<JsonException>(() => A5PurchaseCanonicalSerializer.DeserializeCanonical(
                A5PurchaseActionId.COMPARISON_APPROVE, bytes));
        }
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
        var longHash = SampleQuotation() with { AttachmentSha256 = new string('a', 65) };
        Assert.Throws<ArgumentException>(() =>
            A5PurchaseCanonicalSerializer.Serialize(A5PurchaseActionId.QUOTATION_REVISION_SUBMIT, longHash));
        var nonHexHash = SampleQuotation() with { AttachmentSha256 = new string('a', 63) + "g" };
        Assert.Throws<ArgumentException>(() =>
            A5PurchaseCanonicalSerializer.Serialize(A5PurchaseActionId.QUOTATION_REVISION_SUBMIT, nonHexHash));
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
        var unsigned = plan.GetUnsignedPlanBytesForCryptographicUse();
        Assert.Equal(Convert.ToHexString(SHA256.HashData(unsigned)).ToLowerInvariant(), plan.PlanHash);
        Assert.DoesNotContain("expectedResourceVersion", Encoding.UTF8.GetString(unsigned), StringComparison.Ordinal);
        var first = plan.GetCanonicalParameterBytesForCryptographicUse();
        first[0] ^= 0xff;
        Assert.NotEqual(first, plan.GetCanonicalParameterBytesForCryptographicUse());
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
    public void Same_parameters_have_byte_identical_output_across_explicit_cultures()
    {
        var cultures = new[]
        {
            new CultureInfo("en-US"),
            new CultureInfo("de-DE"),
            new CultureInfo("fr-FR"),
            new CultureInfo("ar-SA"),
            new CultureInfo("tr-TR"),
            new CultureInfo("hi-IN"),
            CultureInfo.InvariantCulture
        };
        var parameters = SampleQuotation();
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        byte[]? baseline = null;

        try
        {
            foreach (var culture in cultures)
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                var bytes = A5PurchaseCanonicalSerializer.Serialize(
                    A5PurchaseActionId.QUOTATION_REVISION_SUBMIT,
                    parameters);
                baseline ??= bytes;
                Assert.True(baseline.SequenceEqual(bytes),
                    $"Canonical bytes differ under {(culture.Equals(CultureInfo.InvariantCulture) ? "InvariantCulture" : culture.Name)}.");
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void Non_ascii_golden_vector_matches_exact_bytes_and_hash_under_every_culture()
    {
        const string expectedJson = """{"attachmentObjectKey":"purchase/quotes/vq-001.pdf","attachmentSha256":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","currencyCode":"INR","deliveryTerms":"درجة الحرارة 25°","headerDiscountValue":9.9,"idempotencyKey":"idem-quotation-001","invitationId":"10000000-0000-0000-0000-000000000001","invitationVersion":7,"lateAuthorizationRemarks":"مراجعة مطلوبة – தமிழ் \uD83D\uDE00","lines":[{"discountValue":3,"freight":5,"hsnSacCode":"8471","insurance":6.6,"otherCharges":7.7,"packingForwarding":4.04,"placeOfSupplyStateCode":"TN","promisedDeliveryDate":"2026-09-30","quantity":1.5,"requestForQuotationLineId":"20000000-0000-0000-0000-000000000002","roundOff":8.8,"supplierStateCode":"KA","unitRate":2.5,"vendorRegistrationType":"REGULAR"}],"paymentTerms":"Zahlung 30 Tage – Grüße","previousQuotationVersion":null,"receivedAt":"2026-08-23T01:38:09.0000000Z","requestLateAuthorization":true,"submissionSource":"EMAIL_RECEIVED","vendorAttestation":"أُقِرّ بالشروط °","vendorQuoteReference":"விலை-Ä-\uD83D\uDE00","warrantyTerms":"உத்தரவாதம் இரண்டு ஆண்டு"}""";
        const string expectedSha256 = "f64b00c4cf756c6ed09a64ab412625827d681023c2ac6cac0abd977a7802a074";
        var expectedBytes = Encoding.UTF8.GetBytes(expectedJson);
        var cultures = new[]
        {
            new CultureInfo("en-US"), new CultureInfo("de-DE"), new CultureInfo("fr-FR"),
            new CultureInfo("ar-SA"), new CultureInfo("tr-TR"), new CultureInfo("hi-IN"),
            CultureInfo.InvariantCulture
        };
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            foreach (var culture in cultures)
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                var actual = A5PurchaseCanonicalSerializer.Serialize(
                    A5PurchaseActionId.QUOTATION_REVISION_SUBMIT, GoldenNonAsciiQuotation());
                Assert.Equal(expectedBytes, actual);
                Assert.Equal(expectedSha256, Convert.ToHexString(SHA256.HashData(actual)).ToLowerInvariant());
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void Relaxed_escaping_golden_vector_matches_controls_html_and_lone_surrogate()
    {
        const string expectedJson = """{"comparisonNumber":"C<>&'\"","idempotencyKey":"idem","remarks":"controls:\b\t\n\f\r\u0000\u001F lone:\uFFFD","version":4}""";
        const string expectedSha256 = "a3b0da9514a1e8abc4e767d238f90fb60013516fc411d8258b876fecc7129ac3";
        var parameters = new A5ComparisonApprovalParameters(
            "C<>&'\"",
            "controls:\b\t\n\f\r\0\u001f lone:" + "\ud800",
            4,
            "idem");

        var actual = A5PurchaseCanonicalSerializer.Serialize(A5PurchaseActionId.COMPARISON_APPROVE, parameters);

        Assert.Equal(Encoding.UTF8.GetBytes(expectedJson), actual);
        Assert.Equal(expectedSha256, Convert.ToHexString(SHA256.HashData(actual)).ToLowerInvariant());
    }

    [Fact]
    public void Unsigned_plan_golden_vector_matches_non_ascii_organization_and_target()
    {
        const string expectedJson = "{\"actionId\":\"COMPARISON_APPROVE\",\"organization\":\"\u0b85\u0bae\u0bc8\u0baa\u0bcd\u0baa\u0bc1 <&\",\"parameters\":{\"comparisonNumber\":\"CMP-1\",\"idempotencyKey\":\"idem\",\"remarks\":\"Gr\u00fc\u00dfe\",\"version\":4},\"planId\":\"40000000-0000-0000-0000-000000000004\",\"target\":\"\u0647\u062f\u0641 \\uD83D\\uDE00\"}";
        const string expectedSha256 = "2e52d3bd59894c82d8fc9941ae8743608b2a6dc0c14ec0e0c3b57f38c1816410";
        var plan = A5UnsignedPurchasePlan.Create(
            Guid.Parse("40000000-0000-0000-0000-000000000004"),
            A5PurchaseActionId.COMPARISON_APPROVE,
            new A5ComparisonApprovalParameters("CMP-1", "Gr\u00fc\u00dfe", 4, "idem"),
            "\u0b85\u0bae\u0bc8\u0baa\u0bcd\u0baa\u0bc1 <&",
            "\u0647\u062f\u0641 \U0001f600");

        var actual = plan.GetUnsignedPlanBytesForCryptographicUse();

        Assert.Equal(Encoding.UTF8.GetBytes(expectedJson), actual);
        Assert.Equal(expectedSha256, Convert.ToHexString(SHA256.HashData(actual)).ToLowerInvariant());
        Assert.Equal(expectedSha256, plan.PlanHash);
    }

    [Fact]
    public void A5_syntax_trees_exclude_forbidden_runtime_dispatch_apis_and_endpoint_uses_shared_allowlist()
    {
        var root = FindRepositoryRoot();
        var a5Root = Path.Combine(root, "src", "SESS.NexaERP.Application", "Purchase", "A5");
        foreach (var path in Directory.GetFiles(a5Root, "*.cs"))
            Assert.Empty(FindForbiddenSyntax(File.ReadAllText(path)));

        var endpoint = File.ReadAllText(Path.Combine(root, "src", "SESS.NexaERP.Api", "Endpoints", "Rev869AConfigurationEndpoints.cs"));
        Assert.Contains("VendorRegistrationTypes.TryParseCanonical(request.VendorRegistrationType", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("NormalizeCode(request.VendorRegistrationType)", endpoint, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("using R = System.Reflection; class C { R.Assembly? Value; }")]
    [InlineData("class C { void M() { global::System.Type.GetType(\"C\"); } }")]
    [InlineData("class C { void M() { System.Activator\n.CreateInstance(typeof(C)); } }")]
    [InlineData("class C { dynamic Value = 1; }")]
    [InlineData("class C { void M(System.Reflection.MethodInfo m) { m.Invoke(this, null); } }")]
    [InlineData("class C { void M(System.Linq.Expressions.LambdaExpression e) { e.Compile(); } }")]
    [InlineData("class C { void M() { System.Reflection.Assembly.Load(Array.Empty<byte>()); } }")]
    public void Forbidden_dispatch_syntax_guard_rejects_aliases_qualification_and_line_breaks(string source)
    {
        Assert.NotEmpty(FindForbiddenSyntax(source));
    }

    private static void AssertVersions(
        A5PurchaseActionId actionId,
        IA5PurchaseActionParameters parameters,
        params (string Property, uint? Value)[] expected)
    {
        using var document = JsonDocument.Parse(A5PurchaseCanonicalSerializer.Serialize(actionId, parameters));
        var actualNames = document.RootElement.EnumerateObject()
            .Select(x => x.Name)
            .Where(x => x == "version" || x.EndsWith("Version", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expected.Select(x => x.Property).Order(StringComparer.Ordinal), actualNames);
        foreach (var item in expected)
        {
            var property = document.RootElement.GetProperty(item.Property);
            if (item.Value.HasValue) Assert.Equal(item.Value.Value, property.GetUInt32());
            else Assert.Equal(JsonValueKind.Null, property.ValueKind);
        }
    }

    private static string CanonicalApprovalJson() => Encoding.UTF8.GetString(
        A5PurchaseCanonicalSerializer.Serialize(
            A5PurchaseActionId.COMPARISON_APPROVE,
            new A5ComparisonApprovalParameters("CMP-1", "remarks", 4, "idem")));

    private static A5QuotationRevisionSubmitParameters WithAllQuotationDecimals(decimal value)
    {
        var sample = SampleQuotation();
        var line = sample.Lines[0] with
        {
            Quantity = value,
            UnitRate = value,
            DiscountValue = value,
            PackingForwarding = value,
            Freight = value,
            Insurance = value,
            OtherCharges = value,
            RoundOff = value
        };
        return sample with { Lines = [line], HeaderDiscountValue = value };
    }

    private static string[] QuotationDecimalTexts(A5QuotationRevisionSubmitParameters parameters)
    {
        using var document = SerializeQuotation(parameters);
        var root = document.RootElement;
        var line = root.GetProperty("lines")[0];
        return
        [
            line.GetProperty("quantity").GetRawText(),
            line.GetProperty("unitRate").GetRawText(),
            line.GetProperty("discountValue").GetRawText(),
            line.GetProperty("packingForwarding").GetRawText(),
            line.GetProperty("freight").GetRawText(),
            line.GetProperty("insurance").GetRawText(),
            line.GetProperty("otherCharges").GetRawText(),
            line.GetProperty("roundOff").GetRawText(),
            root.GetProperty("headerDiscountValue").GetRawText()
        ];
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

    private static A5QuotationRevisionSubmitParameters GoldenNonAsciiQuotation() => SampleQuotation() with
    {
        VendorQuoteReference = "விலை-Ä-😀",
        PaymentTerms = "Zahlung 30 Tage – Grüße",
        DeliveryTerms = "درجة الحرارة 25°",
        WarrantyTerms = "உத்தரவாதம் இரண்டு ஆண்டு",
        RequestLateAuthorization = true,
        LateAuthorizationRemarks = "مراجعة مطلوبة – தமிழ் 😀",
        VendorAttestation = "أُقِرّ بالشروط °"
    };

    private static IReadOnlyList<string> FindForbiddenSyntax(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        var root = tree.GetRoot();
        var findings = tree.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => "parse-error:" + diagnostic)
            .ToList();
        var aliases = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(usingDirective => usingDirective.Alias is not null)
            .ToDictionary(
                usingDirective => usingDirective.Alias!.Name.Identifier.ValueText,
                usingDirective => Compact(usingDirective.Name!),
                StringComparer.Ordinal);
        var importedNamespaces = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(usingDirective => usingDirective.Alias is null && !usingDirective.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
            .Select(usingDirective => Compact(usingDirective.Name!))
            .ToArray();

        string Expand(string value)
        {
            value = value.Replace("global::", string.Empty, StringComparison.Ordinal);
            var separator = value.IndexOf('.', StringComparison.Ordinal);
            var first = separator < 0 ? value : value[..separator];
            return aliases.TryGetValue(first, out var replacement)
                ? replacement + (separator < 0 ? string.Empty : value[separator..])
                : value;
        }

        string ResolveType(TypeSyntax type)
        {
            var value = Expand(Compact(type));
            if (value.Contains('.', StringComparison.Ordinal)) return value;
            var imported = importedNamespaces.FirstOrDefault(candidate =>
                candidate == "System" && value is "Type" or "Activator" ||
                candidate == "System.Reflection" && value is "Assembly" or "MethodInfo" ||
                candidate == "System.Linq.Expressions" && value.EndsWith("Expression", StringComparison.Ordinal));
            return imported is null ? value : imported + "." + value;
        }

        foreach (var name in root.DescendantNodes().OfType<NameSyntax>())
        {
            if (Expand(Compact(name)).StartsWith("System.Reflection", StringComparison.Ordinal))
                findings.Add("System.Reflection:" + name.GetLocation().GetLineSpan().StartLinePosition);
        }

        foreach (var dynamicName in root.DescendantNodes().OfType<IdentifierNameSyntax>()
                     .Where(name => name.Identifier.ValueText == "dynamic"))
            findings.Add("dynamic:" + dynamicName.GetLocation().GetLineSpan().StartLinePosition);

        var declaredTypes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var parameter in root.DescendantNodes().OfType<ParameterSyntax>().Where(parameter => parameter.Type is not null))
            declaredTypes[parameter.Identifier.ValueText] = ResolveType(parameter.Type!);
        foreach (var declaration in root.DescendantNodes().OfType<VariableDeclarationSyntax>())
        {
            var type = ResolveType(declaration.Type);
            foreach (var variable in declaration.Variables)
                declaredTypes[variable.Identifier.ValueText] = type;
        }

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax member) continue;
            var method = member.Name.Identifier.ValueText;
            var receiver = Expand(Compact(member.Expression));
            if (member.Expression is IdentifierNameSyntax identifier &&
                declaredTypes.TryGetValue(identifier.Identifier.ValueText, out var declaredType))
                receiver = declaredType;
            var simpleReceiver = receiver[(receiver.LastIndexOf(".", StringComparison.Ordinal) + 1)..];
            var forbidden = method switch
            {
                "GetType" => simpleReceiver == "Type",
                "CreateInstance" => simpleReceiver == "Activator",
                "Invoke" => simpleReceiver == "MethodInfo",
                "Compile" => simpleReceiver.EndsWith("Expression", StringComparison.Ordinal),
                "Load" => simpleReceiver == "Assembly",
                _ => false
            };
            if (forbidden)
                findings.Add(simpleReceiver + "." + method + ":" + invocation.GetLocation().GetLineSpan().StartLinePosition);
        }

        return findings;
    }

    private static string Compact(SyntaxNode node) =>
        string.Concat(node.DescendantTokens().Select(token => token.Text));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
