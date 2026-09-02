using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Domain.Sales;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static class CustomerPoEndpoints
{
    private const string Page = "sales.customer-po";
    private const string RecordPrefix = "CPO-";
    private const long MaxPoFileBytes = 10 * 1024 * 1024;

    public sealed record CustomerPoSummary(
        Guid Id, string PoRecordNumber, string CustomerPoNumber, DateOnly? CustomerPoDate,
        string CustomerName, string CustomerCode, string CompanyCode, string? ServiceMode, string? SalesType,
        string? Description, decimal? TotalAmountWithGst, string WorkStatus,
        string? FiscalYear, int LineCount, string? PoFileName, int CurrentRevisionNumber, uint Version);

    public sealed record CustomerPoLineDto(
        int SlNo, string Description, DateOnly? DueDate, decimal? Quantity, string? Uom,
        decimal? Rate, decimal? DiscountPercent, decimal? Amount);

    public sealed record CustomerPoRevisionDto(
        int RevisionNumber, string ChangeReason, string CreatedBy, DateTimeOffset CreatedAt);

    public sealed record CustomerPoDetail(
        Guid Id, string PoRecordNumber, string CustomerPoNumber, DateOnly? CustomerPoDate,
        string? QuoteNumber, DateOnly? QuoteDate, string CustomerName, string CustomerCode,
        string CompanyCode, string? ServiceMode, string? SalesType, string? Description,
        decimal? TotalAmountWithGst, string WorkStatus, string? PaymentTerms, string? ModeOfDelivery,
        string? FiscalYear, string? Remarks, string? DeliveryTerms,
        decimal? TaxableValue, decimal? CgstPercent, decimal? CgstAmount, decimal? SgstPercent, decimal? SgstAmount,
        decimal? IgstPercent, decimal? IgstAmount, decimal? RoundOff, string? AmountInWords,
        string? PoFileName, int CurrentRevisionNumber, IReadOnlyList<CustomerPoLineDto> Lines,
        IReadOnlyList<CustomerPoRevisionDto> Revisions,
        string CreatedBy, DateTimeOffset CreatedAt, uint Version);

    public sealed record UpsertCustomerPoRequest(
        string? PoRecordNumber, string CustomerPoNumber, DateOnly? CustomerPoDate,
        string? QuoteNumber, DateOnly? QuoteDate, string CustomerCode,
        string? ServiceMode, string? SalesType, string? Description,
        decimal? TotalAmountWithGst, string? WorkStatus, string? PaymentTerms, string? ModeOfDelivery,
        string? FiscalYear, string? Remarks, string? DeliveryTerms,
        decimal? CgstPercent, decimal? SgstPercent, decimal? IgstPercent,
        IReadOnlyList<CustomerPoLineDto>? Lines, uint? Version, string? RevisionReason = null);

    public sealed record AddCustomerPoOptionRequest(string Kind, string Value);

    public static IEndpointRouteBuilder MapCustomerPoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var sales = endpoints.MapGroup("/api/v1/sales").WithTags("Sales").RequireAuthorization();

        sales.MapGet("/customer-pos", async (NexaErpDbContext db, ICurrentUser currentUser, int? page, int? pageSize, string? search,
            string? poRecordNumber, string? customerPoNumber,
            string? workStatus, string? salesType, string? serviceMode, string? fiscalYear,
            CancellationToken ct) =>
        {
            var paging = MasterEndpointHelpers.NormalizePaging(page, pageSize);
            // Company is mandatory and always resolved from the authenticated session.
            var organizationId = currentUser.OrganizationId?.Trim();
            var query = db.CustomerPurchaseOrders.AsNoTracking()
                .Where(po => po.Company != null && po.Company.Code == organizationId);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToUpperInvariant();
                query = query.Where(po => po.PoRecordNumber.ToUpper().Contains(term)
                    || po.CustomerPoNumber.ToUpper().Contains(term)
                    || (po.Customer != null && po.Customer.Name.ToUpper().Contains(term))
                    || (po.QuoteNumber != null && po.QuoteNumber.ToUpper().Contains(term)));
            }
            if (!string.IsNullOrWhiteSpace(poRecordNumber)) query = query.Where(po => po.PoRecordNumber == poRecordNumber.Trim().ToUpperInvariant());
            if (!string.IsNullOrWhiteSpace(customerPoNumber)) query = query.Where(po => po.CustomerPoNumber == customerPoNumber.Trim());
            if (!string.IsNullOrWhiteSpace(workStatus)) query = query.Where(po => po.WorkStatus == workStatus.Trim());
            if (!string.IsNullOrWhiteSpace(salesType)) query = query.Where(po => po.SalesType == salesType.Trim());
            if (!string.IsNullOrWhiteSpace(serviceMode)) query = query.Where(po => po.ServiceMode == serviceMode.Trim());
            if (!string.IsNullOrWhiteSpace(fiscalYear)) query = query.Where(po => po.FiscalYear == fiscalYear.Trim());
            var total = await query.CountAsync(ct);
            var rows = await query
                .OrderBy(po => po.PoRecordNumber)
                .Skip(paging.Skip).Take(paging.PageSize)
                .Select(po => new CustomerPoSummary(po.Id, po.PoRecordNumber, po.CustomerPoNumber, po.CustomerPoDate,
                    po.Customer!.Name, po.Customer.CustomerCode, po.Company!.Code, po.ServiceMode, po.SalesType,
                    po.Description, po.TotalAmountWithGst, po.WorkStatus, po.FiscalYear,
                    po.Lines.Count(line => line.RevisionNumber == po.CurrentRevisionNumber), po.PoFileName,
                    po.CurrentRevisionNumber, po.Version))
                .ToListAsync(ct);
            return Results.Ok(new PagedResponse<CustomerPoSummary>(total, paging.PageNumber, paging.PageSize, rows));
        }).RequirePagePermission(Page, PagePermissionActions.View);

        sales.MapGet("/customer-pos/lookups", async (NexaErpDbContext db, CancellationToken ct) =>
        {
            var fiscalYears = await db.CustomerPurchaseOrders.AsNoTracking()
                .Where(po => po.FiscalYear != null).Select(po => po.FiscalYear!).Distinct().OrderByDescending(x => x).ToListAsync(ct);
            var uoms = await db.Uoms.AsNoTracking().Where(u => u.IsActive)
                .Select(u => u.Code).OrderBy(u => u).ToListAsync(ct);
            var options = await db.CustomerPoOptions.AsNoTracking().Where(o => o.IsActive)
                .OrderBy(o => o.Value).Select(o => new { o.Kind, o.Value }).ToListAsync(ct);
            return Results.Ok(new
            {
                WorkStatuses = CustomerPoWorkStatuses.All,
                ServiceModes = options.Where(o => o.Kind == CustomerPoOptionKinds.ServiceMode).Select(o => o.Value).ToList(),
                SalesTypes = options.Where(o => o.Kind == CustomerPoOptionKinds.SalesType).Select(o => o.Value).ToList(),
                FiscalYears = fiscalYears,
                Uoms = uoms
            });
        }).RequirePagePermission(Page, PagePermissionActions.View);

        // Quick-add for the mode-of-service / sales-type dropdowns.
        sales.MapPost("/customer-pos/options", async (AddCustomerPoOptionRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser user, CancellationToken ct) =>
        {
            var kind = request.Kind?.Trim().ToUpperInvariant() ?? string.Empty;
            if (!CustomerPoOptionKinds.All.Contains(kind))
                return Results.BadRequest(new { message = $"Kind must be one of: {string.Join(", ", CustomerPoOptionKinds.All)}." });
            var value = request.Value?.Trim() ?? string.Empty;
            if (value.Length is 0 or > 60)
                return Results.BadRequest(new { message = "Value is required (max 60 characters)." });

            var existing = await db.CustomerPoOptions
                .SingleOrDefaultAsync(o => o.Kind == kind && o.Value.ToUpper() == value.ToUpper(), ct);
            if (existing is not null)
            {
                if (!existing.IsActive)
                {
                    existing.IsActive = true;
                    existing.UpdatedAt = DateTimeOffset.UtcNow;
                    existing.UpdatedBy = user.LoginId;
                    await db.SaveChangesAsync(ct);
                }
                return Results.Ok(new { existing.Kind, existing.Value });
            }

            var option = new CustomerPoOption { Kind = kind, Value = value, CreatedBy = user.LoginId };
            db.CustomerPoOptions.Add(option);
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("Sales", "AddOption", nameof(CustomerPoOption), option.Id.ToString(), null, new { option.Kind, option.Value }, ct);
            return Results.Ok(new { option.Kind, option.Value });
        }).RequirePagePermission(Page, PagePermissionActions.Create);

        sales.MapGet("/customer-pos/next-number", async (NexaErpDbContext db, CancellationToken ct) =>
        {
            return Results.Ok(new { PoRecordNumber = await NextRecordNumberAsync(db, ct) });
        }).RequirePagePermission(Page, PagePermissionActions.View);

        sales.MapGet("/customer-pos/{poRecordNumber}", async (string poRecordNumber, NexaErpDbContext db, ICurrentUser user, CancellationToken ct) =>
        {
            var entity = await db.CustomerPurchaseOrders.AsNoTracking()
                .Include(po => po.Customer).Include(po => po.Company)
                .Include(po => po.Lines.OrderBy(line => line.RevisionNumber).ThenBy(line => line.SlNo)).Include(po => po.Revisions.OrderBy(revision => revision.RevisionNumber))
                .SingleOrDefaultAsync(po => po.PoRecordNumber == Normalize(poRecordNumber) && po.Company != null && po.Company.Code == user.OrganizationId, ct);
            return entity is null ? Results.NotFound(new { message = "Customer PO not found." }) : Results.Ok(ToDetail(entity));
        }).RequirePagePermission(Page, PagePermissionActions.View);

        sales.MapPost("/customer-pos", async (UpsertCustomerPoRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser user, CancellationToken ct) =>
        {
            var validation = await ValidateAsync(request, user, db, ct);
            if (validation.Error is not null) return Results.BadRequest(new { message = validation.Error });

            var recordNumber = string.IsNullOrWhiteSpace(request.PoRecordNumber)
                ? await NextRecordNumberAsync(db, ct)
                : Normalize(request.PoRecordNumber);
            if (await db.CustomerPurchaseOrders.AnyAsync(po => po.PoRecordNumber == recordNumber, ct))
                return Results.Conflict(new { message = $"PO record number {recordNumber} already exists." });

            var entity = new CustomerPurchaseOrder { PoRecordNumber = recordNumber, CurrentRevisionNumber = 1, CreatedBy = user.LoginId };
            Apply(entity, request, validation, user.LoginId);
            AppendRevision(entity, "Initial intake", user.LoginId);
            db.CustomerPurchaseOrders.Add(entity);
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("Sales", "Create", nameof(CustomerPurchaseOrder), entity.Id.ToString(), null,
                new { entity.PoRecordNumber, entity.CustomerPoNumber, CustomerId = entity.CustomerId, entity.TotalAmountWithGst, Revision = entity.CurrentRevisionNumber, LineCount = entity.Lines.Count }, ct);
            return Results.Created($"/api/v1/sales/customer-pos/{entity.PoRecordNumber}", new { entity.PoRecordNumber, entity.Version, entity.CurrentRevisionNumber });
        }).RequirePagePermission(Page, PagePermissionActions.Create);

        sales.MapPut("/customer-pos/{poRecordNumber}", async (string poRecordNumber, UpsertCustomerPoRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser user, CancellationToken ct) =>
        {
            var entity = await db.CustomerPurchaseOrders
                .Include(po => po.Lines)
                .Include(po => po.Revisions)
                .SingleOrDefaultAsync(po => po.PoRecordNumber == Normalize(poRecordNumber)
                    && po.Company != null && po.Company.Code == user.OrganizationId, ct);
            if (entity is null) return Results.NotFound(new { message = "Customer PO not found." });
            if (request.Version is null || request.Version.Value != entity.Version)
                return Results.Conflict(new { message = "Stale record version. Refresh and retry." });

            var validation = await ValidateAsync(request, user, db, ct);
            if (validation.Error is not null) return Results.BadRequest(new { message = validation.Error });

            if (validation.CustomerId != entity.CustomerId)
                return Results.BadRequest(new { message = "Customer identity is immutable. Create a new intake record for a different customer." });
            if (string.IsNullOrWhiteSpace(request.RevisionReason))
                return Results.BadRequest(new { message = "Revision reason is required." });
            var before = new { entity.CustomerPoNumber, entity.TotalAmountWithGst, entity.WorkStatus, Revision = entity.CurrentRevisionNumber, LineCount = entity.Lines.Count(line => line.RevisionNumber == entity.CurrentRevisionNumber) };
            entity.CurrentRevisionNumber++;
            Apply(entity, request, validation, user.LoginId);
            AppendRevision(entity, request.RevisionReason.Trim(), user.LoginId);
            entity.Version++;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            entity.UpdatedBy = user.LoginId;
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("Sales", "Update", nameof(CustomerPurchaseOrder), entity.Id.ToString(), before,
                new { entity.CustomerPoNumber, entity.TotalAmountWithGst, entity.WorkStatus, Revision = entity.CurrentRevisionNumber, LineCount = entity.Lines.Count(line => line.RevisionNumber == entity.CurrentRevisionNumber) }, ct);

            var saved = await db.CustomerPurchaseOrders.AsNoTracking()
                .Include(po => po.Customer).Include(po => po.Company)
                .Include(po => po.Lines.OrderBy(line => line.RevisionNumber).ThenBy(line => line.SlNo)).Include(po => po.Revisions.OrderBy(revision => revision.RevisionNumber))
                .SingleAsync(po => po.Id == entity.Id, ct);
            return Results.Ok(ToDetail(saved));
        }).RequirePagePermission(Page, PagePermissionActions.Update);

        // PO copy upload/download (PDF). Uploading a replacement creates a new immutable revision.
        sales.MapPost("/customer-pos/{poRecordNumber}/file", async (string poRecordNumber, HttpRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser user, CancellationToken ct) =>
        {
            var entity = await db.CustomerPurchaseOrders
                .Include(po => po.Lines).Include(po => po.Revisions)
                .SingleOrDefaultAsync(po => po.PoRecordNumber == Normalize(poRecordNumber)
                    && po.Company != null && po.Company.Code == user.OrganizationId, ct);
            if (entity is null) return Results.NotFound(new { message = "Customer PO not found." });
            if (!request.HasFormContentType) return Results.BadRequest(new { message = "Multipart form data with 'file', 'version' and 'revisionReason' fields is required." });
            var form = await request.ReadFormAsync(ct);
            if (!uint.TryParse(form["version"], out var expectedVersion) || expectedVersion != entity.Version)
                return Results.Conflict(new { message = "Stale or missing record version. Refresh and retry." });
            var revisionReason = form["revisionReason"].ToString().Trim();
            if (string.IsNullOrWhiteSpace(revisionReason)) return Results.BadRequest(new { message = "Revision reason is required." });
            var file = form.Files.GetFile("file");
            if (file is null || file.Length == 0) return Results.BadRequest(new { message = "A PDF file is required." });
            if (file.Length > MaxPoFileBytes) return Results.BadRequest(new { message = "File exceeds the 10 MB limit." });
            if (!string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { message = "Only PDF files are accepted." });

            await using var stream = file.OpenReadStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);
            var stored = new CustomerPoFile
            {
                FileName = Path.GetFileName(file.FileName), ContentType = "application/pdf",
                SizeBytes = file.Length, Content = buffer.ToArray(), CreatedBy = user.LoginId
            };
            db.CustomerPoFiles.Add(stored);
            entity.CurrentRevisionNumber++;
            ClonePreviousRevisionLines(entity, user.LoginId);
            entity.PoFileId = stored.Id;
            entity.PoFileName = stored.FileName;
            AppendRevision(entity, revisionReason, user.LoginId);
            entity.Version++;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            entity.UpdatedBy = user.LoginId;
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("Sales", "UploadPoCopyRevision", nameof(CustomerPurchaseOrder), entity.Id.ToString(), null,
                new { stored.FileName, stored.SizeBytes, Revision = entity.CurrentRevisionNumber }, ct);
            return Results.Ok(new { entity.PoRecordNumber, entity.PoFileName, entity.CurrentRevisionNumber, entity.Version });
        })
        .DisableAntiforgery()
        .RequirePagePermission(Page, PagePermissionActions.UploadAttachment);

        sales.MapGet("/customer-pos/{poRecordNumber}/file", async (string poRecordNumber, NexaErpDbContext db, ICurrentUser user, CancellationToken ct) =>
        {
            var entity = await db.CustomerPurchaseOrders.AsNoTracking()
                .SingleOrDefaultAsync(po => po.PoRecordNumber == Normalize(poRecordNumber)
                    && po.Company != null && po.Company.Code == user.OrganizationId, ct);
            if (entity?.PoFileId is not { } fileId) return Results.NotFound(new { message = "No PO copy uploaded." });
            var stored = await db.CustomerPoFiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == fileId, ct);
            return stored is null
                ? Results.NotFound(new { message = "No PO copy uploaded." })
                : Results.File(stored.Content, stored.ContentType, stored.FileName);
        }).RequirePagePermission(Page, PagePermissionActions.View);

        return endpoints;
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();

    private static CustomerPoDetail ToDetail(CustomerPurchaseOrder po) => new(
        po.Id, po.PoRecordNumber, po.CustomerPoNumber, po.CustomerPoDate,
        po.QuoteNumber, po.QuoteDate, po.Customer!.Name, po.Customer.CustomerCode,
        po.Company!.Code, po.ServiceMode, po.SalesType, po.Description,
        po.TotalAmountWithGst, po.WorkStatus, po.PaymentTerms, po.ModeOfDelivery, po.FiscalYear, po.Remarks,
        po.DeliveryTerms, po.TaxableValue, po.CgstPercent, po.CgstAmount, po.SgstPercent, po.SgstAmount,
        po.IgstPercent, po.IgstAmount, po.RoundOff, po.AmountInWords, po.PoFileName, po.CurrentRevisionNumber,
        po.Lines.Where(line => line.RevisionNumber == po.CurrentRevisionNumber).OrderBy(line => line.SlNo)
            .Select(line => new CustomerPoLineDto(line.SlNo, line.Description, line.DueDate, line.Quantity, line.Uom, line.Rate, line.DiscountPercent, line.Amount))
            .ToList(),
        po.Revisions.OrderBy(revision => revision.RevisionNumber)
            .Select(revision => new CustomerPoRevisionDto(revision.RevisionNumber, revision.ChangeReason, revision.CreatedBy, revision.CreatedAt))
            .ToList(),
        po.CreatedBy, po.CreatedAt, po.Version);

    private static async Task<string> NextRecordNumberAsync(NexaErpDbContext db, CancellationToken ct)
    {
        var numbers = await db.CustomerPurchaseOrders.AsNoTracking()
            .Where(po => po.PoRecordNumber.StartsWith(RecordPrefix))
            .Select(po => po.PoRecordNumber).ToListAsync(ct);
        var max = 0;
        foreach (var number in numbers)
        {
            if (int.TryParse(number[RecordPrefix.Length..], out var value) && value > max) max = value;
        }
        return $"{RecordPrefix}{max + 1:00000}";
    }

    private sealed record ValidationResult(string? Error, Guid? CustomerId, Guid? CompanyId);

    private static async Task<ValidationResult> ValidateAsync(UpsertCustomerPoRequest request, ICurrentUser currentUser, NexaErpDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerPoNumber))
            return new ValidationResult("Customer PO number is required.", null, null);
        if (string.IsNullOrWhiteSpace(request.CustomerCode))
            return new ValidationResult("Customer code is required; free-text customer identity is not accepted.", null, null);

        var code = Normalize(request.CustomerCode);
        var customerId = await db.Customers.AsNoTracking()
            .Where(customer => customer.CustomerCode == code)
            .Select(customer => (Guid?)customer.Id).SingleOrDefaultAsync(ct);
        if (customerId is null) return new ValidationResult($"Unknown customer code {code}.", null, null);

        if (string.IsNullOrWhiteSpace(currentUser.OrganizationId))
            return new ValidationResult("An authenticated company session is required.", null, null);
        var companyCode = currentUser.OrganizationId.Trim();
        var companyId = await db.Companies.AsNoTracking()
            .Where(company => company.Code == companyCode && company.IsActive)
            .Select(company => (Guid?)company.Id).SingleOrDefaultAsync(ct);
        if (companyId is null) return new ValidationResult("The session company is not active or does not exist.", null, null);

        if (!string.IsNullOrWhiteSpace(request.WorkStatus) && !CustomerPoWorkStatuses.All.Contains(request.WorkStatus.Trim()))
            return new ValidationResult($"Work status must be one of: {string.Join(", ", CustomerPoWorkStatuses.All)}.", null, null);
        if (request.Lines is not null)
        {
            foreach (var line in request.Lines)
            {
                if (string.IsNullOrWhiteSpace(line.Description))
                    return new ValidationResult($"Line {line.SlNo}: description is required.", null, null);
                if (line.Quantity is < 0 || line.Rate is < 0 || line.DiscountPercent is < 0 or > 100)
                    return new ValidationResult($"Line {line.SlNo}: quantity/rate must be non-negative and discount within 0–100%.", null, null);
            }
        }
        return new ValidationResult(null, customerId, companyId);
    }

    private static void Apply(CustomerPurchaseOrder entity, UpsertCustomerPoRequest request, ValidationResult validation, string loginId)
    {
        entity.CustomerPoNumber = request.CustomerPoNumber.Trim();
        entity.CustomerPoDate = request.CustomerPoDate;
        entity.QuoteNumber = request.QuoteNumber?.Trim();
        entity.QuoteDate = request.QuoteDate;
        entity.CustomerId = validation.CustomerId!.Value;
        entity.CompanyId = validation.CompanyId!.Value;
        entity.ServiceMode = request.ServiceMode?.Trim();
        entity.SalesType = request.SalesType?.Trim();
        entity.Description = request.Description?.Trim();
        entity.WorkStatus = string.IsNullOrWhiteSpace(request.WorkStatus) ? CustomerPoWorkStatuses.NotCompleted : request.WorkStatus.Trim();
        entity.PaymentTerms = request.PaymentTerms?.Trim();
        entity.ModeOfDelivery = request.ModeOfDelivery?.Trim();
        entity.FiscalYear = string.IsNullOrWhiteSpace(request.FiscalYear) ? FiscalYearOf(request.CustomerPoDate) : request.FiscalYear.Trim();
        entity.Remarks = request.Remarks?.Trim();
        entity.DeliveryTerms = request.DeliveryTerms?.Trim();
        entity.CgstPercent = request.CgstPercent;
        entity.SgstPercent = request.SgstPercent;
        entity.IgstPercent = request.IgstPercent;

        var lines = (request.Lines ?? []).OrderBy(line => line.SlNo).ToList();
        var slNo = 0;
        foreach (var line in lines)
        {
            slNo++;
            var amount = line.Amount;
            if (line.Quantity is { } qty && line.Rate is { } rate)
            {
                var gross = qty * rate;
                var discount = line.DiscountPercent is { } disc ? gross * disc / 100m : 0m;
                amount = Math.Round(gross - discount, 2, MidpointRounding.AwayFromZero);
            }
            entity.Lines.Add(new CustomerPurchaseOrderLine
            {
                CustomerPurchaseOrderId = entity.Id, RevisionNumber = entity.CurrentRevisionNumber, SlNo = slNo,
                Description = line.Description.Trim(), DueDate = line.DueDate, Quantity = line.Quantity,
                Uom = line.Uom?.Trim(), Rate = line.Rate, DiscountPercent = line.DiscountPercent,
                Amount = amount, CreatedBy = loginId
            });
        }

        var currentLines = entity.Lines.Where(line => line.RevisionNumber == entity.CurrentRevisionNumber).ToList();
        if (currentLines.Count > 0)
        {
            var taxable = currentLines.Sum(line => line.Amount ?? 0m);
            entity.TaxableValue = Math.Round(taxable, 2, MidpointRounding.AwayFromZero);
            entity.CgstAmount = request.CgstPercent is { } cgst ? Math.Round(taxable * cgst / 100m, 2, MidpointRounding.AwayFromZero) : null;
            entity.SgstAmount = request.SgstPercent is { } sgst ? Math.Round(taxable * sgst / 100m, 2, MidpointRounding.AwayFromZero) : null;
            entity.IgstAmount = request.IgstPercent is { } igst ? Math.Round(taxable * igst / 100m, 2, MidpointRounding.AwayFromZero) : null;
            var beforeRounding = taxable + (entity.CgstAmount ?? 0m) + (entity.SgstAmount ?? 0m) + (entity.IgstAmount ?? 0m);
            var rounded = Math.Round(beforeRounding, 0, MidpointRounding.AwayFromZero);
            entity.RoundOff = Math.Round(rounded - beforeRounding, 2, MidpointRounding.AwayFromZero);
            entity.TotalAmountWithGst = rounded;
        }
        else
        {
            entity.TaxableValue = null; entity.CgstAmount = null; entity.SgstAmount = null;
            entity.IgstAmount = null; entity.RoundOff = null; entity.TotalAmountWithGst = request.TotalAmountWithGst;
        }
        entity.AmountInWords = entity.TotalAmountWithGst is { } totalValue && totalValue > 0 ? IndianAmountInWords(totalValue) : null;
    }

    private static void ClonePreviousRevisionLines(CustomerPurchaseOrder entity, string loginId)
    {
        var previousRevision = entity.CurrentRevisionNumber - 1;
        foreach (var line in entity.Lines.Where(line => line.RevisionNumber == previousRevision).ToList())
        {
            entity.Lines.Add(new CustomerPurchaseOrderLine
            {
                CustomerPurchaseOrderId = entity.Id, RevisionNumber = entity.CurrentRevisionNumber, SlNo = line.SlNo,
                Description = line.Description, DueDate = line.DueDate, Quantity = line.Quantity, Uom = line.Uom,
                Rate = line.Rate, DiscountPercent = line.DiscountPercent, Amount = line.Amount, CreatedBy = loginId
            });
        }
    }

    private static void AppendRevision(CustomerPurchaseOrder entity, string changeReason, string loginId)
    {
        var lines = entity.Lines.Where(line => line.RevisionNumber == entity.CurrentRevisionNumber).OrderBy(line => line.SlNo)
            .Select(line => new { line.SlNo, line.Description, line.DueDate, line.Quantity, line.Uom, line.Rate, line.DiscountPercent, line.Amount })
            .ToList();
        var snapshot = JsonSerializer.Serialize(new
        {
            entity.PoRecordNumber, entity.CustomerId, entity.CompanyId, entity.CustomerPoNumber, entity.CustomerPoDate,
            entity.QuoteNumber, entity.QuoteDate, entity.ServiceMode, entity.SalesType, entity.Description,
            entity.TotalAmountWithGst, entity.WorkStatus, entity.PaymentTerms, entity.ModeOfDelivery, entity.FiscalYear,
            entity.Remarks, entity.DeliveryTerms, entity.TaxableValue, entity.CgstPercent, entity.CgstAmount,
            entity.SgstPercent, entity.SgstAmount, entity.IgstPercent, entity.IgstAmount, entity.RoundOff,
            entity.AmountInWords, entity.PoFileId, entity.PoFileName, Lines = lines
        });
        entity.Revisions.Add(new CustomerPurchaseOrderRevision
        {
            CustomerPurchaseOrderId = entity.Id, RevisionNumber = entity.CurrentRevisionNumber,
            ChangeReason = changeReason, SnapshotJson = snapshot, CreatedBy = loginId
        });
    }

    private static string? FiscalYearOf(DateOnly? poDate)
    {
        if (poDate is not { } date) return null;
        var startYear = date.Month >= 4 ? date.Year : date.Year - 1;
        return $"{startYear}-{(startYear + 1) % 100:00}";
    }

    /// <summary>"INR Two Lakh Sixty Five Thousand Four Hundred Forty Three Only" (Indian numbering).</summary>
    internal static string IndianAmountInWords(decimal amount)
    {
        var rupees = (long)Math.Floor(Math.Abs(amount));
        var paise = (int)Math.Round((Math.Abs(amount) - rupees) * 100m, 0, MidpointRounding.AwayFromZero);
        var words = rupees == 0 ? "Zero" : Convert(rupees);
        var result = $"INR {words}";
        if (paise > 0) result += $" and {Convert(paise)} Paise";
        return result + " Only";

        static string Convert(long value)
        {
            string[] ones = ["", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
                "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen"];
            string[] tens = ["", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"];

            string TwoDigits(long n) => n < 20 ? ones[n] : $"{tens[n / 10]}{(n % 10 > 0 ? " " + ones[n % 10] : "")}";
            string ThreeDigits(long n) =>
                n >= 100 ? $"{ones[n / 100]} Hundred{(n % 100 > 0 ? " " + TwoDigits(n % 100) : "")}" : TwoDigits(n);

            var parts = new List<string>();
            if (value >= 10000000) { parts.Add($"{Convert(value / 10000000)} Crore"); value %= 10000000; }
            if (value >= 100000) { parts.Add($"{TwoDigits(value / 100000)} Lakh"); value %= 100000; }
            if (value >= 1000) { parts.Add($"{TwoDigits(value / 1000)} Thousand"); value %= 1000; }
            if (value > 0) parts.Add(ThreeDigits(value));
            return string.Join(" ", parts);
        }
    }
}
