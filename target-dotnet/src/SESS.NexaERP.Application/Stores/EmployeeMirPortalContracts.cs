namespace SESS.NexaERP.Application.Stores;

public sealed record EmployeeMirCreate(string JobOrMachine, CreateMaterialIssueRequest Request);
public sealed record EmployeeMirUpdate(string JobOrMachine, UpdateMaterialIssueRequest Request);
public sealed record EmployeeMirItem(Guid Id, string ItemCode, string Name, Guid UomId, string UomCode)
{ public string? Barcode { get; init; } }
public sealed record EmployeeMirIssue(MaterialIssueView Issue, IReadOnlyList<EmployeeMirItem> Items, IReadOnlyDictionary<Guid,decimal> ReturnableQuantities);
public sealed record EmployeeMirReference(Guid Id, string Code, string Name);
public sealed record EmployeeMirSpareLine(Guid Id, Guid ItemId, string PoNumber, int LineNumber, string Description, decimal Quantity);
public sealed record EmployeeMirReferences(Guid DepartmentId, string DepartmentCode,
    IReadOnlyList<EmployeeMirReference> Jobs, IReadOnlyList<EmployeeMirReference> Customers);

public interface IEmployeeMirPortal
{
    Task<MaterialIssueRequestPage> ListAsync(string? number, string? status, int page, int pageSize, CancellationToken ct);
    Task<MaterialIssueRequestView?> GetAsync(Guid id, CancellationToken ct);
    Task<MaterialIssueRequestView> CreateAsync(EmployeeMirCreate command, CancellationToken ct);
    Task<MaterialIssueRequestView> UpdateAsync(Guid id, EmployeeMirUpdate command, CancellationToken ct);
    Task<MaterialIssueRequestView> SubmitAsync(Guid id, MaterialIssueTransitionRequest command, CancellationToken ct);
    Task<IReadOnlyList<EmployeeMirItem>> SearchItemsAsync(string? query, bool scan, CancellationToken ct);
    Task<IReadOnlyList<EmployeeMirSpareLine>> SpareLinesAsync(Guid customerId, CancellationToken ct);
    Task<EmployeeMirReferences> ReferencesAsync(CancellationToken ct);
    Task<IReadOnlyList<OutstandingEngineerCustodyView>> CustodyAsync(CancellationToken ct);
    Task<EmployeeMirIssue?> GetIssueAsync(Guid id, CancellationToken ct);
    Task<MaterialReturnPage> ReturnsAsync(int page, int pageSize, CancellationToken ct);
    Task<MaterialReturnView?> GetReturnAsync(Guid id, CancellationToken ct);
    Task<MaterialReturnView> DeclareReturnAsync(Guid issueId, CreateMaterialReturn command, CancellationToken ct);
}
