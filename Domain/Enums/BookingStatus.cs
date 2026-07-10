using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum BookingStatus
    {
        PendingPayment,              // 0  booking created, awaiting payment
        WaitingForContract,          // 1  payment completed, waiting for admin to upload contract
        WaitingForSignatures,        // 2  contract uploaded by admin, waiting for signatures
        WaitingForStudentSignature,  // 3  landlord signed, waiting for student signature
        WaitingForLandlordSignature, // 4  student signed, waiting for landlord signature
        WaitingForAdminApproval,     // 5  both parties signed, waiting for admin final decision
        Approved,                    // 6  admin approved → escrow released to landlord
        Rejected,                    // 7  admin rejected → escrow refunded to student
        Cancelled                    // 8  cancelled by student or landlord before approval
    }
}
