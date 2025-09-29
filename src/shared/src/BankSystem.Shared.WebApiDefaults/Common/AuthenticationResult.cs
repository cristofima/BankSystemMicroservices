using BankSystem.Shared.WebApiDefaults.Configuration;

namespace BankSystem.Shared.WebApiDefaults.Common;

internal sealed record AuthenticationResult(
    bool IsSuccess,
    string ServiceName,
    AuthenticationMethod Method,
    string ErrorMessage
)
{
    public static AuthenticationResult Success(string serviceName, AuthenticationMethod method) =>
        new(true, serviceName, method, string.Empty);

    public static AuthenticationResult Failure(
        string errorMessage,
        AuthenticationMethod method = AuthenticationMethod.ApiKey,
        string serviceName = ""
    ) => new(false, serviceName, method, errorMessage);
}
