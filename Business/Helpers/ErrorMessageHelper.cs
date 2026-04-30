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
}

