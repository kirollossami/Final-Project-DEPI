using System;

namespace Domain.Enums
{
    /// <summary>
    /// Represents the lifecycle status of a rental contract in manual upload workflow.
    /// </summary>
    public enum ContractStatus
    {
        WaitingForUpload,          // 0  payment completed, waiting for admin to upload contract
        WaitingForSignatures,      // 1  contract uploaded by admin, waiting for signatures
        WaitingForStudentSignature, // 2  landlord signed, waiting for student signature
        WaitingForLandlordSignature, // 3  student signed, waiting for landlord signature
        WaitingForAdminApproval,   // 4  both parties signed, waiting for admin final decision
        Approved,                  // 5  admin approved → escrow released to landlord
        Rejected,                  // 6  admin rejected → escrow refunded to student
        Archived                   // 7  booking completed or cancelled
    }
}
