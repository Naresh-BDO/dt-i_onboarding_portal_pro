# SMTP Diagnostic Endpoint - Usage Guide

## Quick Start

Once your application is running, you can test SMTP connectivity by visiting:

```
http://localhost:7107/api/auth/diagnostics/smtp
```

Or via curl:

```bash
curl http://localhost:7107/api/auth/diagnostics/smtp
```

---

## What It Does

The diagnostic endpoint will:

1. **Test TCP connectivity** to your SMTP server
   - Verifies network can reach the server
   - Checks firewall isn't blocking the connection
   - Tests basic socket communication

2. **Test SMTP authentication** (if TCP passes)
   - Connects using your configured credentials
   - Verifies authentication works
   - Validates SSL/TLS configuration

3. **Returns detailed results** including:
   - Overall health status (`healthy: true/false`)
   - Human-readable summary
   - Individual TCP and SMTP test results with messages

---

## Response Examples

### ? Success Response

```json
{
  "healthy": true,
  "summary": "\nSMTP Connection Diagnostics - 2024-01-15 10:30:45\n...\nOverall: ? HEALTHY - Ready to send emails\n",
  "details": {
    "tcp": {
      "connected": true,
      "message": "? Successfully connected to smtp.gmail.com:587"
    },
    "smtp": {
      "connected": true,
      "message": "? SMTP connection successful to smtp.gmail.com:587"
    }
  }
}
```

### ? Network Blocked

```json
{
  "healthy": false,
  "summary": "\nSMTP Connection Diagnostics - 2024-01-15 10:30:45\n...\nTCP Test: ? FAIL\nMessage: ? Connection to smtp.gmail.com:587 timed out after 5000ms\n",
  "details": {
    "tcp": {
      "connected": false,
      "message": "? Connection to smtp.gmail.com:587 timed out after 5000ms"
    },
    "smtp": {
      "connected": false,
      "message": "Skipped - TCP connection failed"
    }
  }
}
```

### ? Authentication Failed

```json
{
  "healthy": false,
  "summary": "...\nSMTP: ? FAIL\nMessage: ? SMTP error: 535 5.7.8 Username and Password not accepted\n",
  "details": {
    "tcp": {
      "connected": true,
      "message": "? Successfully connected to smtp.gmail.com:587"
    },
    "smtp": {
      "connected": false,
      "message": "? SMTP error: 535 5.7.8 Username and Password not accepted (Code: 0)"
    }
  }
}
```

---

## Interpreting Results

### TCP Test Failed
- **Cause**: Network/firewall is blocking the connection
- **Solution**: 
  - Contact IT to whitelist `smtp.gmail.com:587` (or your SMTP server)
  - Or switch to a different SMTP server

### SMTP Test Failed (but TCP succeeded)
- **Cause 1**: Wrong credentials or App Password not set
  - **Solution**: Generate a Gmail App Password at https://myaccount.google.com/apppasswords
- **Cause 2**: SMTP server rejecting connection
  - **Solution**: Verify host, port, and SSL settings in `appsettings.json`

### Both Tests Succeeded
- **Status**: ? Ready to send emails!
- **Next**: Try creating a user account and verify email confirmation works

---

## Security Notes

?? **Important**: The diagnostic endpoint:
- **Only works from localhost** (127.0.0.1 or localhost)
- **Returns error if accessed from remote hosts** (for security)
- Should be **disabled in production**

To disable in production, remove or comment out this method in `AuthController.cs`.

---

## Troubleshooting

### Endpoint returns 403 Forbidden

```json
{
  "message": "SMTP diagnostics only available from localhost"
}
```

**Cause**: You're accessing from a non-localhost machine  
**Solution**: Access from your development machine or allow specific IPs in the endpoint

### No data returned / Timeout

**Cause**: Application might be slow or overloaded  
**Solution**: 
- Check application logs for errors
- Verify the endpoint URL is correct
- Try again after a moment

### Helpful Logs

Run your application and watch the console/debug output. You should see log entries like:

```
[INF] Starting full SMTP diagnostics
[INF] Testing TCP connection to smtp.gmail.com:587
[INF] ? Successfully connected to smtp.gmail.com:587
[INF] Testing SMTP connection to smtp.gmail.com:587 with SSL=True
[INF] ? SMTP connection successful to smtp.gmail.com:587
[INF] Diagnostics complete. TCP: PASS, SMTP: PASS
```

---

## Using the Diagnostics Class Programmatically

You can also use the `SmtpConnectionDiagnostics` class in your own code:

```csharp
// In any service or controller with IOptions<SmtpOptions> and ILogger injected
var diagnostics = new SmtpConnectionDiagnostics(_logger);

// Test just TCP
var tcpResult = await diagnostics.TestTcpConnectionAsync(
    options.Value.Host,
    options.Value.Port);

Console.WriteLine(tcpResult.Message);
// Output: "? Successfully connected to smtp.gmail.com:587"

// Test full SMTP with credentials
var suite = await diagnostics.RunFullDiagnosticsAsync(options.Value);

if (suite.IsHealthy)
    Console.WriteLine("Ready to send emails!");
else
    Console.WriteLine(suite.SmtpResult.Message);
```

---

## Files Created/Modified

- ? `SmtpConnectionDiagnostics.cs` - New diagnostic utility class
- ? `AuthController.cs` - Added `/api/auth/diagnostics/smtp` endpoint
- ? `SmtpEmailSender.cs` - Already has comprehensive logging

---

## Next Steps

1. Start your application: `dotnet run`
2. Visit `http://localhost:7107/api/auth/diagnostics/smtp`
3. Check the results
4. If tests pass ? try sending a test email
5. If tests fail ? follow the troubleshooting guide above

