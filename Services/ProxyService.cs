

using System.Net;
using System.Net.Sockets;

namespace Kosync.Services;

public class ProxyService
{
    private readonly ILogger<ProxyService>? _logger;

    private bool _proxiesLoaded = false;


    private IPNetwork[] _trustedProxies = [];
    /// <summary>
    /// List of configured trusted proxies
    /// </summary>
    public IPNetwork[] TrustedProxies
    {
        get
        {
            LoadProxies();
            return _trustedProxies;
        }
    }


    public ProxyService(ILogger<ProxyService>? logger = null)
    {
        _logger = logger;
    }

    public bool IsTrustedProxy(IPAddress? address)
    {
        if (address is null)
        {
            return false;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        LoadProxies();

        foreach (var network in _trustedProxies)
        {
            if (network.Contains(address))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsTrustedProxy(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return false;
        }

        if (IPAddress.TryParse(address, out IPAddress? ip))
        {
            return IsTrustedProxy(ip);
        }

        return false;
    }

    private void LoadProxies()
    {
        if (_proxiesLoaded) { return; }

        _proxiesLoaded = true;

        string? proxies = Environment.GetEnvironmentVariable("TRUSTED_PROXIES");

        if (string.IsNullOrEmpty(proxies))
        {
            LogInfo("No trusted proxies set.");

            return;
        }

        string[] tempProxies = proxies.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        List<IPNetwork> validProxies = [];

        foreach (string proxy in tempProxies)
        {
            if (TryParseProxy(proxy, out IPNetwork network))
            {
                validProxies.Add(network);
            }
            else
            {
                LogWarning($"Invalid trusted proxy - {proxy}");
            }
        }

        _trustedProxies = validProxies.ToArray();

        if (_trustedProxies.Length == 0)
        {
            LogWarning("No valid trusted proxies set.");
        }
        else
        {
            string tempString = "Trusted proxies: " + string.Join(", ", _trustedProxies.Select(p => p.ToString()));

            LogInfo(tempString);
        }
    }

    private static bool TryParseProxy(string value, out IPNetwork network)
    {
        if (IPNetwork.TryParse(value, out network))
        {
            return true;
        }

        if (IPAddress.TryParse(value, out IPAddress? ip))
        {
            if (ip.IsIPv4MappedToIPv6)
            {
                ip = ip.MapToIPv4();
            }

            int prefixLength = ip.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
            network = new IPNetwork(ip, prefixLength);
            return true;
        }

        network = default;
        return false;
    }

    private void LogWarning(string text)
    {
        Log(LogLevel.Warning, text);
    }

    private void LogInfo(string text)
    {
        Log(LogLevel.Information, text);
    }

    private void Log(LogLevel level, string text)
    {
        text = $"[{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}] {text}";
        _logger?.Log(level, text);
    }
}
