namespace Domain.Enums;

public static class NotificationTypes
{
    // ── Account & Verification ──
    public const string AccountActivated = "AccountActivated";
    public const string AccountDeactivated = "AccountDeactivated";
    public const string AccountDeleted = "AccountDeleted";
    public const string AccountReactivated = "AccountReactivated";
    public const string AccountStatusChanged = "AccountStatusChanged";
    public const string ProfileUpdated = "ProfileUpdated";
    public const string PasswordChanged = "PasswordChanged";
    public const string LandlordVerificationApproved = "LandlordVerificationApproved";
    public const string LandlordVerificationRejected = "LandlordVerificationRejected";
    public const string VerificationReviewed = "VerificationReviewed";
    public const string VerificationStatusChanged = "VerificationStatusChanged";
    public const string UniversityVerificationApproved = "UniversityVerificationApproved";
    public const string UniversityVerificationRejected = "UniversityVerificationRejected";
    public const string UniversityVerificationPending = "UniversityVerificationPending";
    public const string UniversityVerificationSubmitted = "UniversityVerificationSubmitted";
    public const string NationalIdUploaded = "NationalIdUploaded";
    public const string UnitDocumentationUploaded = "UnitDocumentationUploaded";
    public const string NewRegistration = "NewRegistration";

    // ── Booking ──
    public const string BookingCreated = "BookingCreated";
    public const string NewBooking = "NewBooking";
    public const string BookingCancelled = "BookingCancelled";
    public const string BookingApproved = "BookingApproved";
    public const string BookingRejected = "BookingRejected";
    public const string MultiRoomBookingCreated = "MultiRoomBookingCreated";

    // ── Payment ──
    public const string PaymentInitiated = "PaymentInitiated";
    public const string PaymentCompleted = "PaymentCompleted";
    public const string PaymentFailed = "PaymentFailed";
    public const string PaymentWorkflowCompleted = "PaymentWorkflowCompleted";

    // ── Contract ──
    public const string ContractReady = "ContractReady";
    public const string ContractUploaded = "ContractUploaded";
    public const string ContractGenerated = "ContractGenerated";
    public const string ContractApproved = "ContractApproved";
    public const string ContractRejected = "ContractRejected";
    public const string ContractSigned = "ContractSigned";
    public const string StudentSigned = "StudentSigned";
    public const string LandlordSigned = "LandlordSigned";

    // ── Escrow ──
    public const string EscrowReleased = "EscrowReleased";
    public const string EscrowRefunded = "EscrowRefunded";
    public const string RefundProcessed = "RefundProcessed";

    // ── Complaints ──
    public const string NewComplaintFiled = "NewComplaintFiled";
    public const string ComplaintStatusUpdated = "ComplaintStatusUpdated";
    public const string ComplaintDeleted = "ComplaintDeleted";

    // ── Reviews ──
    public const string NewReviewReceived = "NewReviewReceived";
    public const string ReviewDeleted = "ReviewDeleted";

    // ── Chat ──
    public const string NewMessageReceived = "NewMessageReceived";
    public const string NewMessage = "NewMessage";
    public const string NewConversation = "NewConversation";

    // ── Admin ──
    public const string PendingApproval = "PendingApproval";
    public const string ContractSignedPendingApproval = "ContractSignedPendingApproval";

    // ── Housing Units ──
    public const string NewHousingUnit = "NewHousingUnit";
    public const string HousingUnitUpdated = "HousingUnitUpdated";
    public const string HousingUnitDeleted = "HousingUnitDeleted";
}
