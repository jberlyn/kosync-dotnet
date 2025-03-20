

using System.Net;

namespace Kosync.Services;

public class IPService
{
    private readonly HttpContext? _context;
    private readonly ILogger<IPService>? _logger;
    private readonly ProxyService _proxyService;

    private bool _ipLoaded = false;


    private bool _trustedProxy = false;
    /// <summary>
    /// Is this connection from a trusted proxy
    /// </summary>
    public bool TrustedProxy
    {
        get
        {
            LoadIP();
            return _trustedProxy;
        }
    }

    private string _clientIP = "";
    /// <summary>
    /// The IP address of the client
    /// </summary>
    public string ClientIP
    {
        get
        {
            LoadIP();
            return _clientIP;
        }
    }


    public IPService(IHttpContextAccessor accessor, ILogger<IPService>? logger, ProxyService proxyService)
    {
        _context = accessor.HttpContext;
        _logger = logger;
        _proxyService = proxyService;

        LoadIP();
    }

    private void LoadIP()
    {
        if (_ipLoaded) { return; }

        _ipLoaded = true;

        string? connectingIP = _context?.Connection.RemoteIpAddress?.ToString();
        if (connectingIP is null) { connectingIP = ""; }

        LogInfo(_context?.Request.Headers["X-Forwarded-For"]);
        if (_proxyService.TrustedProxies.Contains(connectingIP))
        {
            _trustedProxy = true;


            string? forwardedFor = _context?.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                forwardedFor = forwardedFor.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            }

            if (!string.IsNullOrEmpty(forwardedFor) &&
                IPAddress.TryParse(forwardedFor, out _))
            {
                _clientIP = forwardedFor;
            }
        }

        if (string.IsNullOrEmpty(_clientIP))
        {
            if (_trustedProxy)
            {
                LogWarning($"Trusted proxy [{connectingIP}] failed to forward client IP address.");
            }

            _clientIP = connectingIP;
        }

        if (string.IsNullOrEmpty(_clientIP))
        {
            LogWarning($"Unable to determine client IP address.");
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
