using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum BookingStatus
    {
        Pending,
        PaymentPending,
        PaymentProcessing,
        PendingContract,
        ContractGenerated,
        WaitingStudentSignature,
        WaitingOwnerSignature,
        UnderAdminReview,
        Approved,
        Active,
        Completed,
        Rejected,
        Cancelled,
        SuccessfullyConfirmed
    }
}
