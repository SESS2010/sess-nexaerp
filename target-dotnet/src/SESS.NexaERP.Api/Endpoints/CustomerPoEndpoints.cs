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
        string CustomerName, string? CustomerCode, string? CompanyCode, string? ServiceMode, string? SalesType,
        string? Description, decimal? TotalAmountWithGst, string WorkStatus, string? InvoiceNumber,
        DateOnly? InvoiceDate, string? PaymentStatus, string? FiscalYear, int LineCount, string? PoFileName, uint Version);

    public sealed record CustomerPoLineDto(
        int SlNo, string Description, DateOnly? DueDate, decimal? Quantity, string? Uom,
        decimal? Rate, decimal? DiscountPercent, decimal? Amount);

    public sealed record CustomerPoDetail(
        Guid Id, string PoRecordNumber, string CustomerPoNumber, DateOnly? CustomerPoDate,
        string? QuoteNumber, DateOnly? QuoteDate, string CustomerName, string? CustomerCode,
        string? CompanyCode, string? ServiceMode, string? SalesType, string? Description,
        decimal? TotalAmountWithGst, string WorkStatus, string? InvoiceNumber, DateOnly? InvoiceDate,
        DateOnly? FinalInvoiceDate, string? PaymentStatus, string? PaymentTerms, string? ModeOfDelivery,
        string? FiscalYear, string? Remarks,
        string? DeliveryTerms,
        decimal? TaxableValue, decimal? CgstPercent, decimal? CgstAmount, decimal? SgstPercent, decimal? SgstAmount,
        decimal? IgstPercent, decimal? IgstAmount, decimal? RoundOff, string? AmountInWords,
        string? PoFileName, string? InvoiceFileName, IReadOnlyList<CustomerPoLineDto> Lines,
        string CreatedBy, DateTimeOffset CreatedAt, uint Version);

    public sealed record UpsertCustomerPoRequest(
        string? PoRecordNumber, string CustomerPoNumber, DateOnly? CustomerPoDate,
        string? QuoteNumber, DateOnly? QuoteDate, string? CustomerCode, string? CustomerName,
        string? ServiceMode, string? SalesType, string? Description,
        decimal? TotalAmountWithGst, string? WorkStatus, string? InvoiceNumber, DateOnly? InvoiceDate,
        DateOnly? FinalInvoiceDate, string? PaymentStatus, string? PaymentTerms, string? ModeOfDelivery,
        string? FiscalYear, string? Remarks,
        string? DeliveryTerms,
        decimal? CgstPercent, decimal? SgstPercent, decimal? IgstPercent,
        IReadOnlyList<CustomerPoLineDto>? Lines, uint? Version);

    public sealed record AddCustomerPoOptionRequest(string Kind, string Value);

    public static IEndpointRouteBuilder MapCustomerPoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var sales = endpoints.MapGroup("/api/v1/sales").WithTags("Sales").RequireAuthorization();

        sales.MapGet("/customer-pos", async (NexaErpDbContext db, ICurrentUser currentUser, int? page, int? pageSize, string? search,
            string? workStatus, string? salesType, string? serviceMode, string? fiscalYear,
            CancellationToken ct) =>
        {
            var paging = MasterEndpointHelpers.NormalizePaging(page, pageSize);
            // Scoped to the company chosen at login; legacy rows with no company stay visible.
            var organizationId = currentUser.OrganizationId?.Trim();
            var query = db.CustomerPurchaseOrders.AsNoTracking()
                .Where(po => po.CompanyId == null || (po.Company != null && po.Company.Code == organizationId));
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToUpperInvariant();
                query = query.Where(po => po.PoRecordNumber.ToUpper().Contains(term)
                    || po.CustomerPoNumber.ToUpper().Contains(term)
                    || po.CustomerName.ToUpper().Contains(term)
                    || (po.InvoiceNumber != null && po.InvoiceNumber.ToUpper().Contains(term))
                    || (po.QuoteNumber != null && po.QuoteNumber.ToUpper().Contains(term)));
            }
            if (!string.IsNullOrWhiteSpace(workStatus)) query = query.Where(po => po.WorkStatus == workStatus.Trim());
            if (!string.IsNullOrWhiteSpace(salesType)) query = query.Where(po => po.SalesType == salesType.Trim());
            if (!string.IsNullOrWhiteSpace(serviceMode)) query = query.Where(po => po.ServiceMode == serviceMode.Trim());
            if (!string.IsNullOrWhiteSpace(fiscalYear)) query = query.Where(po => po.FiscalYear == fiscalYear.Trim());
            var total = await query.CountAsync(ct);
            var rows = await query
                .OrderBy(po => po.PoRecordNumber)
                .Skip(paging.Skip).Take(paging.PageSize)
                .Select(po => new CustomerPoSummary(po.Id, po.PoRecordNumber, po.CustomerPoNumber, po.CustomerPoDate,
                    po.CustomerName, po.Customer != null ? po.Customer.CustomerCode : null,
                    po.Company != null ? po.Company.Code : null, po.ServiceMode, po.SalesType,
                    po.Description, po.TotalAmountWithGst, po.WorkStatus, po.InvoiceNumber,
                    po.InvoiceDate, po.PaymentStatus, po.FiscalYear, po.Lines.Count, po.PoFileName, po.Version))
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

        sales.MapGet("/customer-pos/{poRecordNumber}", async (string poRecordNumber, NexaErpDbContext db, CancellationToken ct) =>
        {
            var entity = await db.CustomerPurchaseOrders.AsNoTracking()
                .Include(po => po.Customer).Include(po => po.Company)
                .Include(po => po.Lines.OrderBy(line => line.SlNo))
                .SingleOrDefaultAsync(po => po.PoRecordNumber == Normalize(poRecordNumber), ct);
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

            var entity = new CustomerPurchaseOrder { PoRecordNumber = recordNumber, CreatedBy = user.LoginId };
            Apply(entity, request, validation, user.LoginId);
            db.CustomerPurchaseOrders.Add(entity);
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("Sales", "Create", nameof(CustomerPurchaseOrder), entity.Id.ToString(), null,
                new { entity.PoRecordNumber, entity.CustomerPoNumber, entity.CustomerName, entity.TotalAmountWithGst, LineCount = entity.Lines.Count }, ct);
            return Results.Created($"/api/v1/sales/customer-pos/{entity.PoRecordNumber}", new { entity.PoRecordNumber });
        }).RequirePagePermission(Page, PagePermissionActions.Create);

        sales.MapPut("/customer-pos/{poRecordNumber}", async (string poRecordNumber, UpsertCustomerPoRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser user, CancellationToken ct) =>
        {
            var entity = await db.CustomerPurchaseOrders
                .Include(po => po.Lines)
                .SingleOrDefaultAsync(po => po.PoRecordNumber == Normalize(poRecordNumber), ct);
            if (entity is null) return Results.NotFound(new { message = "Customer PO not found." });
            if (request.Version is null || request.Version.Value != entity.Version)
                return Results.Conflict(new { message = "Stale record version. Refresh and retry." });

            var validation = await ValidateAsync(request, user, db, ct);
            if (validation.Error is not null) return Results.BadRequest(new { message = validation.Error });

            var before = new { entity.CustomerPoNumber, entity.CustomerName, entity.TotalAmountWithGst, entity.WorkStatus, entity.InvoiceNumber, entity.PaymentStatus, LineCount = entity.Lines.Count };
            db.CustomerPurchaseOrderLines.RemoveRange(entity.Lines);
            entity.Lines.Clear();
            Apply(entity, request, validation, user.LoginId);
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            entity.UpdatedBy = user.LoginId;
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("Sales", "Update", nameof(CustomerPurchaseOrder), entity.Id.ToString(), before,
                new { entity.CustomerPoNumber, entity.CustomerName, entity.TotalAmountWithGst, entity.WorkStatus, LineCount = entity.Lines.Count }, ct);

            var saved = await db.CustomerPurchaseOrders.AsNoTracking()
                .Include(po => po.Customer).Include(po => po.Company)
                .Include(po => po.Lines.OrderBy(line => line.SlNo))
                .SingleAsync(po => po.Id == entity.Id, ct);
            return Results.Ok(ToDetail(saved));
        }).RequirePagePermission(Page, PagePermissionActions.Update);

        // PO copy upload/download (PDF).
        sales.MapPost("/customer-pos/{poRecordNumber}/file", async (string poRecordNumber, HttpRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser user, CancellationToken ct) =>
        {
            var entity = await db.CustomerPurchaseOrders.SingleOrDefaultAsync(po => po.PoRecordNumber == Normalize(poRecordNumber), ct);
            if (entity is null) return Results.NotFound(new { message = "Customer PO not found." });
            if (!request.HasFormContentType) return Results.BadRequest(new { message = "Multipart form data with a 'file' field is required." });
            var form = await request.ReadFormAsync(ct);
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
                FileName = Path.GetFileName(file.FileName),
                ContentType = "application/pdf",
                SizeBytes = file.Length,
                Content = buffer.ToArray(),
                CreatedBy = user.LoginId
            };
            db.CustomerPoFiles.Add(stored);
            if (entity.PoFileId is { } oldId)
            {
                await db.CustomerPoFiles.Where(x => x.Id == oldId).ExecuteDeleteAsync(ct);
            }
            entity.PoFileId = stored.Id;
            entity.PoFileName = stored.FileName;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            entity.UpdatedBy = user.LoginId;
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("Sales", "UploadPoCopy", nameof(CustomerPurchaseOrder), entity.Id.ToString(), null,
                new { stored.FileName, stored.SizeBytes }, ct);
            return Results.Ok(new { entity.PoRecordNumber, entity.PoFileName });
        })
        .DisableAntiforgery()
        .RequirePagePermission(Page, PagePermissionActions.UploadAttachment);

        sales.MapGet("/customer-pos/{poRecordNumber}/file", async (string poRecordNumber, NexaErpDbContext db, CancellationToken ct) =>
        {
            var entity = await db.CustomerPurchaseOrders.AsNoTracking()
                .SingleOrDefaultAsync(po => po.PoRecordNumber == Normalize(poRecordNumber), ct);
            if (entity?.PoFileId is not { } fileId) return Results.NotFound(new { message = "No PO copy uploaded." });
            var stored = await db.CustomerPoFiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == fileId, ct);
            return stored is null
                ? Results.NotFound(new { message = "No PO copy uploaded." })
                : Results.File(stored.Content, stored.ContentType, stored.FileName);
        }).RequirePagePermission(Page, PagePermissionActions.View);

        // Invoice copy upload/download (PDF).
        sales.MapPost("/customer-pos/{poRecordNumber}/invoice-file", async (string poRecordNumber, HttpRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser user, CancellationToken ct) =>
        {
            var entity = await db.CustomerPurchaseOrders.SingleOrDefaultAsync(po => po.PoRecordNumber == Normalize(poRecordNumber), ct);
            if (entity is null) return Results.NotFound(new { message = "Customer PO not found." });
            if (!request.HasFormContentType) return Results.BadRequest(new { message = "Multipart form data with a 'file' field is required." });
            var form = await request.ReadFormAsync(ct);
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
                FileName = Path.GetFileName(file.FileName),
                ContentType = "application/pdf",
                SizeBytes = file.Length,
                Content = buffer.ToArray(),
                CreatedBy = user.LoginId
            };
            db.CustomerPoFiles.Add(stored);
            if (entity.InvoiceFileId is { } oldId)
            {
                await db.CustomerPoFiles.Where(x => x.Id == oldId).ExecuteDeleteAsync(ct);
            }
            entity.InvoiceFileId = stored.Id;
            entity.InvoiceFileName = stored.FileName;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            entity.UpdatedBy = user.LoginId;
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("Sales", "UploadInvoiceCopy", nameof(CustomerPurchaseOrder), entity.Id.ToString(), null,
                new { stored.FileName, stored.SizeBytes }, ct);
            return Results.Ok(new { entity.PoRecordNumber, entity.InvoiceFileName });
        })
        .DisableAntiforgery()
        .RequirePagePermission(Page, PagePermissionActions.UploadAttachment);

        sales.MapGet("/customer-pos/{poRecordNumber}/invoice-file", async (string poRecordNumber, NexaErpDbContext db, CancellationToken ct) =>
        {
            var entity = await db.CustomerPurchaseOrders.AsNoTracking()
                .SingleOrDefaultAsync(po => po.PoRecordNumber == Normalize(poRecordNumber), ct);
            if (entity?.InvoiceFileId is not { } fileId) return Results.NotFound(new { message = "No invoice copy uploaded." });
            var stored = await db.CustomerPoFiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == fileId, ct);
            return stored is null
                ? Results.NotFound(new { message = "No invoice copy uploaded." })
                : Results.File(stored.Content, stored.ContentType, stored.FileName);
        }).RequirePagePermission(Page, PagePermissionActions.View);

        return endpoints;
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();

    private static CustomerPoDetail ToDetail(CustomerPurchaseOrder po) => new(
        po.Id, po.PoRecordNumber, po.CustomerPoNumber, po.CustomerPoDate,
        po.QuoteNumber, po.QuoteDate, po.CustomerName, po.Customer?.CustomerCode,
        po.Company?.Code, po.ServiceMode, po.SalesType, po.Description,
        po.TotalAmountWithGst, po.WorkStatus, po.InvoiceNumber, po.InvoiceDate, po.FinalInvoiceDate,
        po.PaymentStatus, po.PaymentTerms, po.ModeOfDelivery, po.FiscalYear, po.Remarks,
        po.DeliveryTerms,
        po.TaxableValue, po.CgstPercent, po.CgstAmount, po.SgstPercent, po.SgstAmount,
        po.IgstPercent, po.IgstAmount, po.RoundOff, po.AmountInWords,
        po.PoFileName, po.InvoiceFileName,
        po.Lines.OrderBy(line => line.SlNo)
            .Select(line => new CustomerPoLineDto(line.SlNo, line.Description, line.DueDate, line.Quantity, line.Uom, line.Rate, line.DiscountPercent, line.Amount))
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

    private sealed record ValidationResult(string? Error, Guid? CustomerId, string? CustomerName, Guid? CompanyId);

    private static async Task<ValidationResult> ValidateAsync(UpsertCustomerPoRequest request, ICurrentUser currentUser, NexaErpDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerPoNumber))
            return new ValidationResult("Customer PO number is required.", null, null, null);

        Guid? customerId = null;
        string? customerName = request.CustomerName?.Trim();
        if (!string.IsNullOrWhiteSpace(request.CustomerCode))
        {
            var code = Normalize(request.CustomerCode);
            var customer = await db.Customers.AsNoTracking()
                .Where(c => c.CustomerCode == code).Select(c => new { c.Id, c.Name }).SingleOrDefaultAsync(ct);
            if (customer is null) return new ValidationResult($"Unknown customer code {code}.", null, null, null);
            customerId = customer.Id;
            customerName = customer.Name;
        }
        if (string.IsNullOrWhiteSpace(customerName))
            return new ValidationResult("Select a customer or enter the customer name.", null, null, null);

        // Company always comes from the login session, never from the request.
        Guid? companyId = null;
        if (!string.IsNullOrWhiteSpace(currentUser.OrganizationId))
        {
            var companyCode = currentUser.OrganizationId.Trim();
            companyId = await db.Companies.AsNoTracking()
                .Where(c => c.Code == companyCode).Select(c => (Guid?)c.Id).SingleOrDefaultAsync(ct);
        }

        if (!string.IsNullOrWhiteSpace(request.WorkStatus) && !CustomerPoWorkStatuses.All.Contains(request.WorkStatus.Trim()))
            return new ValidationResult($"Work status must be one of: {string.Join(", ", CustomerPoWorkStatuses.All)}.", null, null, null);

        if (request.Lines is not null)
        {
            foreach (var line in request.Lines)
            {
                if (string.IsNullOrWhiteSpace(line.Description))
                    return new ValidationResult($"Line {line.SlNo}: description is required.", null, null, null);
                if (line.Quantity is < 0 || line.Rate is < 0 || line.DiscountPercent is < 0 or > 100)
                    return new ValidationResult($"Line {line.SlNo}: quantity/rate must be non-negative and discount within 0–100%.", null, null, null);
            }
        }

        return new ValidationResult(null, customerId, customerName, companyId);
    }

    private static void Apply(CustomerPurchaseOrder entity, UpsertCustomerPoRequest request, ValidationResult validation, string loginId)
    {
        entity.CustomerPoNumber = request.CustomerPoNumber.Trim();
        entity.CustomerPoDate = request.CustomerPoDate;
        entity.QuoteNumber = request.QuoteNumber?.Trim();
        entity.QuoteDate = request.QuoteDate;
        entity.CustomerId = validation.CustomerId;
        entity.CustomerName = validation.CustomerName!;
        entity.CompanyId = validation.CompanyId;
        entity.ServiceMode = request.ServiceMode?.Trim();
        entity.SalesType = request.SalesType?.Trim();
        entity.Description = request.Description?.Trim();
        entity.WorkStatus = string.IsNullOrWhiteSpace(request.WorkStatus) ? CustomerPoWorkStatuses.NotCompleted : request.WorkStatus.Trim();
        entity.InvoiceNumber = request.InvoiceNumber?.Trim();
        entity.InvoiceDate = request.InvoiceDate;
        entity.FinalInvoiceDate = request.FinalInvoiceDate;
        entity.PaymentStatus = request.PaymentStatus?.Trim();
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
                CustomerPurchaseOrderId = entity.Id,
                SlNo = slNo,
                Description = line.Description.Trim(),
                DueDate = line.DueDate,
                Quantity = line.Quantity,
                Uom = line.Uom?.Trim(),
                Rate = line.Rate,
                DiscountPercent = line.DiscountPercent,
                Amount = amount,
                CreatedBy = loginId
            });
        }

        if (entity.Lines.Count > 0)
        {
            var taxable = entity.Lines.Sum(line => line.Amount ?? 0m);
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
            entity.TaxableValue = null;
            entity.CgstAmount = null;
            entity.SgstAmount = null;
            entity.IgstAmount = null;
            entity.RoundOff = null;
            entity.TotalAmountWithGst = request.TotalAmountWithGst;
        }

        entity.AmountInWords = entity.TotalAmountWithGst is { } totalValue && totalValue > 0
            ? IndianAmountInWords(totalValue)
            : null;
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
