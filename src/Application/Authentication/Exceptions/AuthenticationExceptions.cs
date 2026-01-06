namespace Application.Authentication.Exceptions;

public abstract class AuthenticationException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public class UserAlreadyExistsException(string email)
    : AuthenticationException($"User with email {email} already exists");

public class InvalidCredentialsException()
    : AuthenticationException("Invalid email or password");

public class UserNotFoundException(string identifier)
    : AuthenticationException($"User not found: {identifier}");

public class UserBlockedException(string email)
    : AuthenticationException($"User {email} is blocked");

public class InvalidRoleException(string role)
    : AuthenticationException($"Invalid role: {role}");

public class UnhandledAuthenticationException(Exception? innerException)
    : AuthenticationException("Unexpected error occurred during authentication", innerException);
public class TokenRefreshFailedException(string errors)
    : AuthenticationException($"Failed to refresh token: {errors}");

public class UserCreationException(string errors)
    : AuthenticationException($"Failed to create user: {errors}");