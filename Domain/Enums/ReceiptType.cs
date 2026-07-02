using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum ReceiptType
    {
        PaymentReceived,
        EscrowHeld,
        EscrowReleased,
        RefundIssued,
        OwnerPayout
    }
}
