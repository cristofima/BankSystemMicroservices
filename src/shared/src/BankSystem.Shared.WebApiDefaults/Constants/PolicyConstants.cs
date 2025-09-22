using System.Diagnostics.CodeAnalysis;

namespace BankSystem.Shared.WebApiDefaults.Constants;

/// <summary>
/// Constants for authorization policy names used throughout the bank system.
/// Centralizes policy definitions to avoid hardcoded strings and improve maintainability.
/// </summary>
[ExcludeFromCodeCoverage]
public static class PolicyConstants
{
    /// <summary>
    /// Policy that allows access to customers and administrators
    /// </summary>
    public const string CustomerAccess = "CustomerAccess";

    /// <summary>
    /// Policy that allows access only to administrators
    /// </summary>
    public const string AdminAccess = "AdminAccess";

    /// <summary>
    /// Policy that allows access to managers and administrators
    /// </summary>
    public const string ManagerAccess = "ManagerAccess";

    /// <summary>
    /// Policy that allows access to tellers, managers, and administrators
    /// </summary>
    public const string TellerAccess = "TellerAccess";

    /// <summary>
    /// Gets all available policy names
    /// </summary>
    public static readonly string[] AllPolicies = 
    {
        CustomerAccess,
        AdminAccess,
        ManagerAccess,
        TellerAccess
    };

    /// <summary>
    /// Gets policies that allow administrative access
    /// </summary>
    public static readonly string[] AdministrativePolicies = 
    {
        AdminAccess,
        ManagerAccess
    };

    /// <summary>
    /// Gets policies that allow operational access
    /// </summary>
    public static readonly string[] OperationalPolicies = 
    {
        TellerAccess,
        ManagerAccess,
        AdminAccess
    };
}