namespace BankSystem.Shared.Kernel.Common;

/// <summary>
/// Provides information about the current authenticated user.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// Gets the unique identifier of the user.
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// Gets the unique identifier of the customer associated with the user.
    /// </summary>
    Guid CustomerId { get; }

    /// <summary>
    /// Gets the username of the current user.
    /// </summary>
    string UserName { get; }
}
