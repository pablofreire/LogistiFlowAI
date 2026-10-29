namespace LogistiFlow.Fulfillment.Domain.Enums;

/// <summary>Actions a warehouse operator performs on a job. Each maps to one target status.</summary>
public enum FulfillmentJobAction
{
    /// <summary>Picking is done and the package is sealed: <c>Picking</c> → <c>Packed</c>.</summary>
    Pack = 0,

    /// <summary>The package left the dock: <c>Packed</c> → <c>Shipped</c>.</summary>
    Ship = 1,

    /// <summary>Something blocks the job (damaged item, wrong bin): <c>Picking</c> or <c>Packed</c> → <c>Failed</c>.</summary>
    ReportIssue = 2,

    /// <summary>Reprocess a failed job from the start: <c>Failed</c> → <c>Picking</c>.</summary>
    Reprocess = 3,
}
