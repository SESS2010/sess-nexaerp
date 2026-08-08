namespace SESS.NexaERP.Api.Endpoints;

public readonly record struct Paging(int Skip, int Take)
{
    public static Paging Normalize(int? page, int? pageSize)
    {
        var safePage = Math.Max(page ?? 1, 1);
        var safePageSize = Math.Clamp(pageSize ?? 50, 1, 200);
        return new Paging((safePage - 1) * safePageSize, safePageSize);
    }
}
