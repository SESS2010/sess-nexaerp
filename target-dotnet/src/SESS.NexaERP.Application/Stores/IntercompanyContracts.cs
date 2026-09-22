using System.Text.Json;
using SESS.NexaERP.Application.Common;

namespace SESS.NexaERP.Application.Stores;

public sealed record ProposeIntercompanyRouteRequest(
    string RouteCode,
    Guid BuyerCompanyId,
    Guid SellerSiteId,
    Guid BuyerSiteId,
    Guid SellerWarehouseId,
    Guid BuyerWarehouseId,
    Guid SellerGstRegistrationId,
    Guid BuyerGstRegistrationId,
    Guid SellerVendorId,
    Guid BuyerCustomerId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string Remarks,
    string IdempotencyKey);

public sealed record DecideIntercompanyRouteRequest(
    uint Version, string Remarks, string IdempotencyKey);

public sealed record IntercompanyRouteView(
    Guid Id, Guid CompanyId, string RouteCode, Guid BuyerCompanyId,
    Guid SellerSiteId, Guid BuyerSiteId, Guid SellerWarehouseId, Guid BuyerWarehouseId,
    Guid SellerGstRegistrationId, Guid BuyerGstRegistrationId,
    Guid SellerVendorId, Guid BuyerCustomerId,
    DateOnly EffectiveFrom, DateOnly? EffectiveTo,
    string Status, uint Version, JsonElement Definition, JsonElement History);

public interface IIntercompanyService
{
    Task<JsonElement> RouteOptionsAsync(CancellationToken ct);
    Task<JsonElement> PurchaseOptionsAsync(CancellationToken ct);
    Task<IntercompanyPurchaseView> PublishPurchaseAsync(PublishIntercompanyPurchaseRequest request, CancellationToken ct);
    Task<IntercompanyPurchaseView?> PurchaseAsync(Guid correlationId, CancellationToken ct);
    Task<PagedResponse<IntercompanyPurchaseView>> PurchasesAsync(int? page, int? pageSize, CancellationToken ct);
    Task<PagedResponse<IntercompanyRouteView>> RoutesAsync(int? page, int? pageSize, CancellationToken ct);
    Task<IntercompanyRouteView?> RouteAsync(Guid id, CancellationToken ct);
    Task<IntercompanyRouteView> ProposeRouteAsync(ProposeIntercompanyRouteRequest request, CancellationToken ct);
    Task<IntercompanyRouteView> DecideRouteAsync(Guid id, string decision, DecideIntercompanyRouteRequest request, CancellationToken ct);
}

public sealed record PublishIntercompanyPurchaseRequest(
    Guid RouteId, Guid PurchaseOrderId, uint Version, string Remarks, string IdempotencyKey);

public sealed record IntercompanyPurchaseView(
    Guid CorrelationId, Guid CompanyId, Guid BuyerCompanyId, Guid SellerCompanyId,
    Guid RouteId, Guid? PurchaseOrderId, uint PurchaseOrderVersion,
    string Eligibility, JsonElement CommercialOrder, DateTimeOffset PublishedAt);
