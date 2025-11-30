namespace Application.Users.Exceptions;

public abstract class UserException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public class UserNotFoundException(string identifier)
    : UserException($"User not found: {identifier}");

public class UnauthorizedUserAccessException()
    : UserException("User is not authorized to perform this action");

public class InvalidRoleException(string role)
    : UserException($"Invalid role: {role}");

public class UnhandledUserException(Exception? innerException)
    : UserException("Unexpected error occurred during user operation", innerException);
    
public class UserBlockedException(string email)
    : UserException($"User {email} is blocked and cannot perform this action");

public class UserBlockFailedException(string errors)
    : UserException($"Failed to block user: {errors}");

public class UserRoleChangeFailedException(string errors)
    : UserException($"Failed to change user role: {errors}");

public class UserUnblockFailedException(string errors)
    : UserException($"Failed to unblock user: {errors}");
    
public class UserUpdateFailedException(string errors)
    : UserException($"Failed to update user profile: {errors}");
public class UserCannotBeDeletedDueToCartException(Guid userId)
    : UserException($"User {userId} cannot be deleted because user has an active shopping cart");

public class UserCannotBeDeletedDueToOrdersException(Guid userId)
    : UserException($"User {userId} cannot be deleted because user has existing orders");

public class UserCannotBeDeletedDueToReviewsException(Guid userId)
    : UserException($"User {userId} cannot be deleted because user has written product reviews");
    
public class UserDeleteFailedException(string errors)
    : UserException($"Failed to delete user: {errors}");