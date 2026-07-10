-- Migration script to update old booking status strings to new enum values
-- Run this on your production database to fix the booking status conversion error

UPDATE Bookings 
SET BookingStatus = 'PendingPayment' 
WHERE BookingStatus = 'Pending';

UPDATE Bookings 
SET BookingStatus = 'PendingPayment' 
WHERE BookingStatus = 'PaymentProcessing';

UPDATE Bookings 
SET BookingStatus = 'WaitingForContract' 
WHERE BookingStatus = 'ContractSent';

UPDATE Bookings 
SET BookingStatus = 'WaitingForSignatures' 
WHERE BookingStatus = 'ContractUploaded';

UPDATE Bookings 
SET BookingStatus = 'WaitingForStudentSignature' 
WHERE BookingStatus = 'LandlordSigned';

UPDATE Bookings 
SET BookingStatus = 'WaitingForLandlordSignature' 
WHERE BookingStatus = 'StudentSigned';

UPDATE Bookings 
SET BookingStatus = 'WaitingForAdminApproval' 
WHERE BookingStatus = 'BothSigned';

UPDATE Bookings 
SET BookingStatus = 'Approved' 
WHERE BookingStatus = 'Completed';

UPDATE Bookings 
SET BookingStatus = 'Rejected' 
WHERE BookingStatus = 'Declined';

-- Verify the update
SELECT BookingStatus, COUNT(*) as Count 
FROM Bookings 
GROUP BY BookingStatus;
