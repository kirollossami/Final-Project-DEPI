using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Helpers;

public static class ErrorMessageHelper
{
    public static string InvalidCredentials => "Invalid email or password.";
    public static string UserAlreadyExists => "User already exists.";
    public static string UserNotFound => "User not found.";
    public static string UserIsDeleted => "User account is deleted.";
    public static string StudentNotFound => "Student not found.";
    public static string PasswordsDoNotMatch => "New password and confirm password do not match.";
    public static string CurrentPasswordAndNewPasswordAreRequired => "Current password and new password are required.";
    public static string CurrentPasswordIncorrect => "Current password is incorrect.";
    public static string EmailAlreadyExists => "Email already exists.";
    public static string AccountInactive => "Account is inactive.";
    public static string LoginSuccess => "Login successful.";
    public static string RegistrationSuccess => "Registration successful.";
    public static string InvalidToken => "Invalid token.";
    public static string TokenRefreshed => "Token refreshed successfully.";
    public static string UserHasActiveBookings => "Cannot deactivate user with active bookings. Resolve the following bookings first: {0}";
    public static string VerificationNotPending => "Verification can only be reviewed when status is Pending.";
    public static string InvalidVerificationTransition => "Verification status can only be set to Approved or Rejected.";
    public static string EmailConfirmationSent => "Email confirmation token generated. Use the token to confirm your email.";
    public static string EmailAlreadyConfirmed => "Email is already confirmed.";
    public static string EmailConfirmationSuccess => "Email confirmed successfully.";
    public static string EmailConfirmationFailed => "Email confirmation failed. Invalid or expired token.";
    public static string ForgotPasswordGeneric => "If the email exists, a password reset link has been sent.";
    public static string PasswordResetSuccess => "Password has been reset successfully.";
    public static string PasswordResetFailed => "Password reset failed. Invalid or expired token.";
}

