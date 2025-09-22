using System.Diagnostics.CodeAnalysis;

namespace BankSystem.Shared.WebApiDefaults.Constants;

/// <summary>
/// Constants for role names used throughout the bank system.
/// Centralizes role definitions to avoid hardcoded strings and improve maintainability.
/// </summary>
[ExcludeFromCodeCoverage]
public static class RoleConstants
{
    /// <summary>
    /// The customer role - represents regular banking customers
    /// </summary>
    public const string Customer = "Customer";

    /// <summary>
    /// The admin role - represents system administrators with full access
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// The manager role - represents bank managers with elevated privileges
    /// </summary>
    public const string Manager = "Manager";

    /// <summary>
    /// The teller role - represents bank tellers with operational access
    /// </summary>
    public const string Teller = "Teller";

    /// <summary>
    /// Gets all available role names
    /// </summary>
    public static IReadOnlyList<string> AllRoles { get; } = [Customer, Admin, Manager, Teller];

    /// <summary>
    /// Gets administrative role names (Admin, Manager)
    /// </summary>
    public static IReadOnlyList<string> AdministrativeRoles { get; } = [Admin, Manager];

    /// <summary>
    /// Gets operational role names (Teller, Manager, Admin)
    /// </summary>
    public static IReadOnlyList<string> OperationalRoles { get; } = [Teller, Manager, Admin];

    /// <summary>
    /// Gets customer-facing role names (Customer, Admin)
    /// </summary>
    public static IReadOnlyList<string> CustomerFacingRoles { get; } = [Customer, Admin];
}
