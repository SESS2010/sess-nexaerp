using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Stores;

namespace SESS.NexaERP.Tests;

/// <summary>
/// G1 (26 Sep): the employee who recorded or finalised a GRN does not inspect that receipt. The
/// rule is applied in the QC finalise and correct path before anything is written; the refusal is a
/// 409 with this sentence. No fixture employee holds both a Stores role and QC_MANAGER, so the
/// end-to-end refusal is not witnessed; the normal path (different employees) runs in every flow.
/// </summary>
public sealed class QcGrnSegregationTests
{
    private static readonly Guid Receiver = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid Finaliser = Guid.Parse("00000000-0000-0000-0000-0000000000a2");
    private static readonly Guid Inspector = Guid.Parse("00000000-0000-0000-0000-0000000000a3");

    [Fact]
    public void The_receiver_or_the_finaliser_cannot_inspect_the_receipt()
    {
        var receipt = new GoodsReceipt { ReceivedByEmployeeId = Receiver, FinalizedByEmployeeId = Finaliser };

        Assert.True(EfQcWorkflowService.RecordedOrFinalisedGrn(receipt, Receiver));
        Assert.True(EfQcWorkflowService.RecordedOrFinalisedGrn(receipt, Finaliser));
        Assert.False(EfQcWorkflowService.RecordedOrFinalisedGrn(receipt, Inspector));
    }

    [Fact]
    public void A_receipt_not_yet_finalised_by_anyone_only_excludes_its_receiver()
    {
        var receipt = new GoodsReceipt { ReceivedByEmployeeId = Receiver, FinalizedByEmployeeId = null };

        Assert.True(EfQcWorkflowService.RecordedOrFinalisedGrn(receipt, Receiver));
        Assert.False(EfQcWorkflowService.RecordedOrFinalisedGrn(receipt, Inspector));
    }

    [Fact]
    public void The_refusal_names_the_rule() =>
        Assert.Equal("The employee who recorded or finalised this GRN cannot inspect it. Another QC inspector must.",
            EfQcWorkflowService.GrnInspectorRefusal);
}
