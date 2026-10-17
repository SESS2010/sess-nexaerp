using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Domain.Foundation;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public sealed record EnsureOnboardingPrimaryDepartmentRequest(string EmployeeCode,string OrganizationId,string DepartmentCode,
    DateOnly EffectiveFrom,uint EmployeeVersion,Guid CompanyAssignmentId,uint CompanyAssignmentVersion,
    string ApprovalReference,string RosterSha256,string Remarks,string IdempotencyKey);

public static partial class Rev869AConfigurationEndpoints
{
    private static async Task<IResult> ReadOnboardingPrimaryDepartment(string employeeCode,NexaErpDbContext db,
        ICurrentUser user,CancellationToken ct)
    {
        try { _=user.RequireRole("view","IT_MANAGER","TECHNICAL_DIRECTOR","MANAGING_DIRECTOR"); }
        catch(UnauthorizedAccessException){return Results.Forbid();}
        var code=employeeCode?.Trim().ToUpperInvariant();
        if(string.IsNullOrWhiteSpace(code) || code.Length>64) return Results.BadRequest(new {message="Employee code is required."});
        var company=await db.Companies.AsNoTracking().SingleOrDefaultAsync(x=>x.Code==user.OrganizationId && x.IsActive && x.Status=="ACTIVE",ct);
        if(company is null) return Results.Forbid();
        var employee=await db.Employees.AsNoTracking().Where(x=>x.EmployeeCode==code).Select(x=>new {
            x.Id,x.EmployeeCode,x.EmployeeName,x.Status,x.LoginEnabled,x.Version,x.DepartmentId,x.DesignationId,
            DepartmentCode=db.Departments.Where(d=>d.Id==x.DepartmentId).Select(d=>d.Code).FirstOrDefault(),
            DesignationCode=db.Designations.Where(d=>d.Id==x.DesignationId).Select(d=>d.Code).FirstOrDefault() }).SingleOrDefaultAsync(ct);
        if(employee is null) return Results.NotFound();
        var assignments=await db.EmployeeCompanyAssignments.AsNoTracking().Where(x=>x.EmployeeId==employee.Id && x.CompanyId==company.Id)
            .OrderBy(x=>x.EffectiveFrom).Select(x=>new {x.Id,x.Version,x.Status,x.IsActive,x.EffectiveFrom,x.EffectiveTo}).ToListAsync(ct);
        var ids=assignments.Select(x=>x.Id).ToArray();
        var departments=await (from a in db.EmployeeDepartmentAssignments.AsNoTracking()
            join d in db.Departments on a.DepartmentId equals d.Id
            join designation in db.Designations on a.DesignationId equals designation.Id
            where a.CompanyId==company.Id && ids.Contains(a.EmployeeCompanyAssignmentId)
            orderby a.EffectiveFrom
            select new {a.Id,a.EmployeeCompanyAssignmentId,a.DepartmentId,DepartmentCode=d.Code,a.DesignationId,DesignationCode=designation.Code,
                a.AssignmentType,a.IsPrimary,a.IsActive,a.Status,a.EffectiveFrom,a.EffectiveTo,a.Version}).ToListAsync(ct);
        return Results.Ok(new {OrganizationId=company.Code,Employee=employee,CompanyAssignments=assignments,DepartmentAssignments=departments});
    }

    private static async Task<IResult> EnsureOnboardingPrimaryDepartment(EnsureOnboardingPrimaryDepartmentRequest request,
        NexaErpDbContext db,ICurrentUser user,IAuditWriter audit,CancellationToken ct)
    {
        try { _=user.RequireRole("role-administration","IT_MANAGER","TECHNICAL_DIRECTOR","MANAGING_DIRECTOR"); }
        catch(UnauthorizedAccessException){return Results.Forbid();}
        if(string.IsNullOrWhiteSpace(request.EmployeeCode) || request.EmployeeCode.Length>64 ||
            string.IsNullOrWhiteSpace(request.DepartmentCode) || request.DepartmentCode.Length>64 ||
            string.IsNullOrWhiteSpace(request.OrganizationId) || request.OrganizationId.Length>64 ||
            request.EffectiveFrom==default || request.CompanyAssignmentId==Guid.Empty ||
            string.IsNullOrWhiteSpace(request.ApprovalReference) || request.ApprovalReference.Length>100 ||
            string.IsNullOrWhiteSpace(request.Remarks) || request.Remarks.Length>500 ||
            string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length>100 ||
            request.RosterSha256 is null || request.RosterSha256.Length!=64 || !request.RosterSha256.All(Uri.IsHexDigit))
            return Results.BadRequest(new {message="Employee, signed department/company, date, fresh versions, approval reference, roster SHA-256, reason and idempotency key are required."});
        var organization=request.OrganizationId.Trim().ToUpperInvariant();
        if(organization!=user.OrganizationId || !user.EmployeeId.HasValue) return Results.Forbid();
        await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        try
        {
            var company=await db.Companies.SingleOrDefaultAsync(x=>x.Code==organization && x.IsActive && x.Status=="ACTIVE",ct);
            if(company is null) return Results.Forbid();
            var code=request.EmployeeCode.Trim().ToUpperInvariant();
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"ONBOARD_PRIMARY:"+organization+":"+code},0))",ct);
            var employee=await db.Employees.FromSqlInterpolated($"SELECT * FROM advance.employees WHERE \"EmployeeCode\"={code} FOR UPDATE").SingleOrDefaultAsync(ct);
            if(employee is null) return Results.NotFound();
            if(employee.Id==user.EmployeeId.Value) return Results.Forbid();
            if(!string.Equals(employee.Status,"Active",StringComparison.OrdinalIgnoreCase))
                return Results.Conflict(new {message="Employee is inactive. Nothing changed."});
            var department=await db.Departments.AsNoTracking().SingleOrDefaultAsync(x=>x.Code==request.DepartmentCode.Trim().ToUpperInvariant() && x.IsActive,ct);
            if(department is null || department.Id!=employee.DepartmentId ||
                !await db.Designations.AsNoTracking().AnyAsync(x=>x.Id==employee.DesignationId && x.IsActive,ct))
                return Results.Conflict(new {message="Signed primary department differs from the active employee master. Reconcile first; onboarding cannot transfer an employee."});
            var parent=await db.EmployeeCompanyAssignments.FromSqlInterpolated($"SELECT * FROM advance.employee_company_assignments WHERE \"Id\"={request.CompanyAssignmentId} FOR UPDATE")
                .SingleOrDefaultAsync(ct);
            if(parent is null || parent.EmployeeId!=employee.Id || parent.CompanyId!=company.Id || !parent.IsActive || parent.Status!="ACTIVE" ||
                parent.EffectiveFrom>request.EffectiveFrom || parent.EffectiveTo.HasValue && parent.EffectiveTo.Value<request.EffectiveFrom)
                return Results.Conflict(new {message="A matching active company assignment covering the signed date is required. Nothing changed."});
            var rows=await db.EmployeeDepartmentAssignments.Where(x=>x.CompanyId==company.Id && x.EmployeeCompanyAssignmentId==parent.Id).ToListAsync(ct);
            bool Matches(EmployeeDepartmentAssignment row)=>row.DepartmentId==department.Id && row.DesignationId==employee.DesignationId &&
                row.AssignmentType=="PRIMARY" && row.IsPrimary && row.IsActive && row.Status=="ACTIVE" &&
                row.EffectiveFrom==request.EffectiveFrom && row.EffectiveTo==parent.EffectiveTo;
            if(rows.Count>1 || rows.Count==1 && !Matches(rows[0]))
                return Results.Conflict(new {message="A different, closed or ambiguous department assignment already exists. Onboarding cannot replace, reopen or transfer it."});
            var fingerprint=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request))));
            var correlation="RC26_ONBOARD_"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(organization+"\n"+code+"\n"+request.IdempotencyKey)));
            var receipt=await db.ControlledConfigurationHistories.AsNoTracking().SingleOrDefaultAsync(x=>x.CompanyId==company.Id && x.CorrelationId==correlation,ct);
            IResult Result(EmployeeDepartmentAssignment row,bool replayed)=>Results.Ok(new {employee.EmployeeCode,OrganizationId=company.Code,row.Id,
                DepartmentCode=department.Code,row.DepartmentId,row.DesignationId,row.EmployeeCompanyAssignmentId,row.AssignmentType,
                row.IsPrimary,row.IsActive,row.Status,row.EffectiveFrom,row.EffectiveTo,row.Version,Replayed=replayed,CorrelationId=correlation});
            if(receipt is not null)
            {
                using var saved=JsonDocument.Parse(receipt.AfterJson??"{}");
                if(!saved.RootElement.TryGetProperty("Fingerprint",out var stored) || stored.GetString()!=fingerprint || rows.Count!=1 || !Matches(rows[0]))
                    return Results.Conflict(new {message="Onboarding key/content or its read-back changed. Reconcile before retrying."});
                await transaction.CommitAsync(ct); return Result(rows[0],true);
            }
            if(employee.Version!=request.EmployeeVersion || parent.Version!=request.CompanyAssignmentVersion)
                return Results.Conflict(new {message="Employee or company-assignment version is stale. Read fresh state before applying."});
            if(rows.Count==1){await transaction.CommitAsync(ct);return Result(rows[0],true);}
            var entity=new EmployeeDepartmentAssignment {CompanyId=company.Id,EmployeeCompanyAssignmentId=parent.Id,DepartmentId=department.Id,
                DesignationId=employee.DesignationId,AssignmentType="PRIMARY",IsPrimary=true,IsActive=true,Status="ACTIVE",
                EffectiveFrom=request.EffectiveFrom,EffectiveTo=parent.EffectiveTo,CreatedBy=user.LoginId};
            db.EmployeeDepartmentAssignments.Add(entity);
            var evidence=new {Fingerprint=fingerprint,request.ApprovalReference,RosterSha256=request.RosterSha256.ToUpperInvariant(),employee.EmployeeCode,
                EmployeeId=employee.Id,OrganizationId=company.Code,DepartmentAssignmentId=entity.Id,entity.DepartmentId,entity.DesignationId,
                entity.EmployeeCompanyAssignmentId,entity.EffectiveFrom,entity.EffectiveTo,ActorEmployeeId=user.EmployeeId,
                user.ResolvedRoleAssignmentId,user.ResolvedRoleAssignmentType};
            AddHistory(db,company.Code,nameof(EmployeeDepartmentAssignment),entity.Id,"EnsurePrimaryDepartment",null,evidence,
                request.Remarks+" | "+request.ApprovalReference,user,company.Id);
            db.ControlledConfigurationHistories.Local.Single(x=>x.EntityId==entity.Id && x.Action=="EnsurePrimaryDepartment").CorrelationId=correlation;
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("Security","EnsureOnboardingPrimaryDepartment",nameof(EmployeeDepartmentAssignment),entity.Id.ToString(),null,evidence,ct);
            await transaction.CommitAsync(ct); return Result(entity,false);
        }
        catch(Exception error) when(error is PostgresException {SqlState:"23505" or "40001" or "40P01"} ||
            error is DbUpdateException {InnerException:PostgresException {SqlState:"23505" or "40001" or "40P01"}})
        {
            await transaction.RollbackAsync(ct);
            return Results.Conflict(new {message="Primary-department onboarding changed concurrently. Read and reconcile before retrying."});
        }
    }
}
