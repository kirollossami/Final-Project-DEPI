using System;

namespace Domain.Enums
{
    /// <summary>
    /// Represents the lifecycle status of a rental contract.
    /// </summary>
    public enum ContractStatus
    {
        Generated,
        WaitingStudentSignature,
        WaitingOwnerSignature,
        UnderReview,
        Approved,
        Rejected,
        Archived
    }
}
