using Account.Domain.ValueObjects;
using BankSystem.Shared.Domain.Common;

namespace Account.Domain.Entities;

/// <summary>
/// Represents a bank customer entity
/// </summary>
public class Customer : Entity<Guid>
{
    private readonly List<Account> _accounts = [];

    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public EmailAddress EmailAddress { get; private set; } = null!;
    public PhoneNumber? PhoneNumber { get; private set; }
    public DateTime DateOfBirth { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>
    /// Navigation property for customer accounts
    /// </summary>
    public IReadOnlyCollection<Account> Accounts => _accounts.AsReadOnly();

    /// <summary>
    /// Gets the customer's full name
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Gets the customer's age in years
    /// </summary>
    public int Age => DateTime.UtcNow.Year - DateOfBirth.Year - 
                      (DateTime.UtcNow.DayOfYear < DateOfBirth.DayOfYear ? 1 : 0);

    /// <summary>
    /// Private constructor for Entity Framework
    /// </summary>
    private Customer() { }

    /// <summary>
    /// Creates a new customer instance
    /// </summary>
    /// <param name="firstName">Customer's first name</param>
    /// <param name="lastName">Customer's last name</param>
    /// <param name="emailAddress">Customer's email address</param>
    /// <param name="dateOfBirth">Customer's date of birth</param>
    /// <param name="phoneNumber">Customer's phone number (optional)</param>
    /// <returns>New customer instance</returns>
    public static Customer Create(
        string firstName,
        string lastName,
        EmailAddress emailAddress,
        DateTime dateOfBirth,
        PhoneNumber? phoneNumber = null)
    {
        ValidateCreationParameters(firstName, lastName, emailAddress, dateOfBirth);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            EmailAddress = emailAddress,
            PhoneNumber = phoneNumber,
            DateOfBirth = dateOfBirth.Date,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        return customer;
    }

    /// <summary>
    /// Updates customer information
    /// </summary>
    /// <param name="firstName">New first name</param>
    /// <param name="lastName">New last name</param>
    /// <param name="phoneNumber">New phone number</param>
    public void UpdateInformation(string firstName, string lastName, PhoneNumber? phoneNumber = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty", nameof(lastName));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PhoneNumber = phoneNumber;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates customer's email address
    /// </summary>
    /// <param name="newEmailAddress">New email address</param>
    public void UpdateEmailAddress(EmailAddress newEmailAddress)
    {
        EmailAddress = newEmailAddress ?? throw new ArgumentNullException(nameof(newEmailAddress));
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the customer
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reactivates the customer
    /// </summary>
    public void Reactivate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the customer can open a new account
    /// </summary>
    /// <param name="maxAccountsPerCustomer">Maximum allowed accounts per customer</param>
    /// <returns>True if customer can open a new account</returns>
    public bool CanOpenNewAccount(int maxAccountsPerCustomer = 5)
    {
        return IsActive && _accounts.Count(a => a.IsActive) < maxAccountsPerCustomer;
    }

    /// <summary>
    /// Adds an account to the customer
    /// </summary>
    /// <param name="account">Account to add</param>
    internal void AddAccount(Account account)
    {
        if (account == null)
            throw new ArgumentNullException(nameof(account));

        if (account.CustomerId != Id)
            throw new InvalidOperationException("Account does not belong to this customer");

        _accounts.Add(account);
    }

    private static void ValidateCreationParameters(
        string firstName,
        string lastName,
        EmailAddress emailAddress,
        DateTime dateOfBirth)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty", nameof(lastName));

        if (emailAddress == null)
            throw new ArgumentNullException(nameof(emailAddress));

        if (dateOfBirth > DateTime.UtcNow.Date)
            throw new ArgumentException("Date of birth cannot be in the future", nameof(dateOfBirth));

        var age = DateTime.UtcNow.Year - dateOfBirth.Year;
        if (age < 18)
            throw new ArgumentException("Customer must be at least 18 years old", nameof(dateOfBirth));
    }
}
