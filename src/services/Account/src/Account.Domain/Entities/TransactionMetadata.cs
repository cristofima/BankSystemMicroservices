namespace Account.Domain.Entities;

/// <summary>
/// Represents additional metadata for a transaction.
/// </summary>
public class TransactionMetadata
{
    public string? Channel { get; private set; }
    public string? Location { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? DeviceId { get; private set; }
    public string? CancellationReason { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Dictionary<string, string> AdditionalData { get; private set; } = new();

    private TransactionMetadata()
    { }

    public static TransactionMetadata Create() => new();

    public TransactionMetadata WithChannel(string channel)
    {
        Channel = channel;
        return this;
    }

    public TransactionMetadata WithLocation(string location)
    {
        Location = location;
        return this;
    }

    public TransactionMetadata WithIpAddress(string ipAddress)
    {
        IpAddress = ipAddress;
        return this;
    }

    public TransactionMetadata WithUserAgent(string userAgent)
    {
        UserAgent = userAgent;
        return this;
    }

    public TransactionMetadata WithDeviceId(string deviceId)
    {
        DeviceId = deviceId;
        return this;
    }

    public TransactionMetadata WithCancellationReason(string reason)
    {
        CancellationReason = reason;
        return this;
    }

    public TransactionMetadata WithError(string errorMessage)
    {
        ErrorMessage = errorMessage;
        return this;
    }

    public TransactionMetadata WithAdditionalData(string key, string value)
    {
        AdditionalData[key] = value;
        return this;
    }
}