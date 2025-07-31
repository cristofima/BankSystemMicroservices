using System.ComponentModel.DataAnnotations;

namespace BankSystem.ApiGateway.Configuration;

/// <summary>
/// Configuration options for YARP (Yet Another Reverse Proxy) routing and load balancing.
/// Supports microservices routing with health checks and load balancing strategies.
/// </summary>
public class YarpOptions
{
    public const string SectionName = "ReverseProxy";

    /// <summary>
    /// Route configurations for different microservices
    /// </summary>
    public Dictionary<string, RouteConfig> Routes { get; set; } = new();

    /// <summary>
    /// Cluster configurations for service endpoints and load balancing
    /// </summary>
    public Dictionary<string, ClusterConfig> Clusters { get; set; } = new();

    public class RouteConfig
    {
        [Required]
        public string ClusterId { get; set; } = string.Empty;

        [Required]
        public string Match { get; set; } = string.Empty;

        public Dictionary<string, string> Metadata { get; set; } = new();
        
        public TransformConfig[]? Transforms { get; set; }
    }

    public class ClusterConfig
    {
        public Dictionary<string, DestinationConfig> Destinations { get; set; } = new();
        
        public LoadBalancingPolicyConfig? LoadBalancingPolicy { get; set; }
        
        public HealthCheckConfig? HealthCheck { get; set; }
        
        public HttpClientConfig? HttpClient { get; set; }
    }

    public class DestinationConfig
    {
        [Required]
        public string Address { get; set; } = string.Empty;
        
        public HealthConfig? Health { get; set; }
        
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    public class LoadBalancingPolicyConfig
    {
        public string Name { get; set; } = "RoundRobin";
    }

    public class HealthCheckConfig
    {
        public bool Enabled { get; set; } = true;
        
        public string Path { get; set; } = "/health";
        
        public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);
        
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
        
        public string Policy { get; set; } = "ConsecutiveFailures";
    }

    public class HealthConfig
    {
        public string? State { get; set; }
    }

    public class HttpClientConfig
    {
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
        
        public int MaxConnectionsPerServer { get; set; } = 100;
        
        public bool DangerousAcceptAnyServerCertificate { get; set; } = false;
    }

    public class TransformConfig
    {
        public string? PathPattern { get; set; }
        public string? RequestHeader { get; set; }
        public string? ResponseHeader { get; set; }
        public string? Set { get; set; }
        public string? Append { get; set; }
    }
}
