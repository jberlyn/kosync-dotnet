

using System.Net;

namespace Kosync.Services;

public class ProxyService
{
    private readonly ILogger<ProxyService>? _logger;

    private bool _proxiesLoaded = false;


    private string[] _trustedProxies = [];
    /// <summary>
    /// List of configured trusted proxies
    /// </summary>
    public string[] TrustedProxies
    {
        get
        {
            LoadProxies();
            return _trustedProxies;
        }
    }


    public ProxyService(ILogger<ProxyService> logger)
    {
        _logger = logger;
    }

    private void LoadProxies()
    {
        if (_proxiesLoaded) { return; }

        _proxiesLoaded = true;
        string? tempString;

        string? proxies = Environment.GetEnvironmentVariable("TRUSTED_PROXIES");

        if (string.IsNullOrEmpty(proxies))
        {
            LogInfo("No trusted proxies set.");

            return;
        }

        string[] tempProxies = proxies.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < tempProxies.Length; i++)
        {
            if (!IPAddress.TryParse(tempProxies[i], out _))
            {
                LogWarning($"Inavalid trusted proxy - {tempProxies[i]}");

                tempProxies[i] = "";
            }
        }

        _trustedProxies = tempProxies.Where(p => !string.IsNullOrEmpty(p)).ToArray();

        if (_trustedProxies.Length == 0)
        {
            LogWarning("No valid trusted proxies set.");
        }
        else
        {
            tempString = "";

            foreach (var prox in _trustedProxies)
            {
                if (!string.IsNullOrEmpty(tempString)) { tempString += ", "; }

                tempString += prox;
            }

            tempString = "Trusted proxies: " + tempString;

            LogInfo(tempString);
        }
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
