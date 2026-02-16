# SMTP Connection Troubleshooting Guide

## Current Error
```
SocketException: A connection attempt failed because the connected party did not properly 
respond after a period of time, or established connection failed because connected host 
has failed to respond.
```

This indicates the connection to `smtp.gmail.com:587` is being **blocked by firewall/network**.

---

## Step 1: Test Network Connectivity

### PowerShell Commands to Run

```powershell
# Test Gmail SMTP port 587 (TLS - recommended for Gmail)
Test-NetConnection smtp.gmail.com -Port 587

# Test Gmail SMTP port 465 (SSL - alternative)
Test-NetConnection smtp.gmail.com -Port 465

# Test Gmail SMTP port 25 (unencrypted - rarely works)
Test-NetConnection smtp.gmail.com -Port 25

# Test basic DNS resolution
Resolve-DnsName smtp.gmail.com
```

**Expected output for working connection:**
```
ComputerName     : smtp.gmail.com
RemoteAddress    : 142.250.xxx.xxx
RemotePort       : 587
TcpTestSucceeded : True
```

**If all fail:** Your network/firewall is blocking outbound SMTP. Contact your IT/network admin.

---

## Step 2: Update appsettings.json

**CURRENT (May not work):**
```json
"SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "govindhannaresh@gmail.com",
    "Password": "jmfcfbjiyhzexaca",
    "FromAddress": "govindhannaresh@gmail.com",
    "FromDisplayName": "BDO RISE – Onboarding"
}
```

**Try Alternative Port (465 - SSL):**
```json
"SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": 465,
    "EnableSsl": true,
    "Username": "govindhannaresh@gmail.com",
    "Password": "your-gmail-app-password",
    "FromAddress": "govindhannaresh@gmail.com",
    "FromDisplayName": "BDO RISE – Onboarding"
}
```

---

## Step 3: Gmail Setup Verification

### Check Your Gmail Account

1. **Verify Gmail App Password is set:**
   - Go to: https://myaccount.google.com/apppasswords
   - Select "Mail" and "Windows Computer"
   - Generate a 16-character App Password
   - Use this password in `appsettings.json` (NOT your regular Gmail password)

2. **Enable "Less secure app access" (if using regular password):**
   - Go to: https://myaccount.google.com/lesssecureapps
   - Note: Google recommends App Passwords instead

3. **Verify 2-Factor Authentication is enabled** (required for App Passwords):
   - Go to: https://myaccount.google.com/security

---

## Step 4: Secure Configuration (Remove Credentials from appsettings.json)

### Use User Secrets for Development

```powershell
cd DT-I_Onboarding_Portal.Server
dotnet user-secrets init
dotnet user-secrets set "SmtpSettings:Username" "govindhannaresh@gmail.com"
dotnet user-secrets set "SmtpSettings:Password" "your-app-password-here"
```

### Update appsettings.json (remove sensitive data)

```json
{
    "ConnectionStrings": {
        "DefaultConnection": "Server=GN401;Database=Bdo;Trusted_Connection=True;TrustServerCertificate=True;"
    },
    "SmtpSettings": {
        "Host": "smtp.gmail.com",
        "Port": 587,
        "EnableSsl": true,
        "Username": "govindhannaresh@gmail.com",
        "Password": "placeholder-set-via-user-secrets",
        "FromAddress": "govindhannaresh@gmail.com",
        "FromDisplayName": "BDO RISE – Onboarding"
    },
    "Jwt": {
        "Key": "placeholder-set-via-user-secrets",
        "Issuer": "YourIssuer",
        "Audience": "YourAudience"
    },
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft.AspNetCore": "Warning"
        }
    },
    "AllowedHosts": "*"
}
```

---

## Step 5: Enable Logging to Debug Issues

### In Program.cs, ensure logging is configured:

```csharp
builder.Services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddConsole();
    config.AddDebug();
    config.SetMinimumLevel(LogLevel.Information);
});
```

### Check logs for detailed error messages when sending email

---

## Step 6: Test Email Sending

### Create a test endpoint in AuthController or use Unit Tests

```csharp
public class EmailSendingTests
{
    [Fact]
    public async Task TestEmailSending()
    {
        var options = Options.Create(new SmtpOptions
        {
            Host = "smtp.gmail.com",
            Port = 587,
            EnableSsl = true,
            Username = "your-email@gmail.com",
            Password = "your-app-password",
            FromAddress = "your-email@gmail.com",
            FromDisplayName = "BDO RISE"
        });

        var logger = new DebugLogger();
        var emailSender = new SmtpEmailSender(options, logger);

        var result = await emailSender.SendEmailAsync(
            "recipient@example.com",
            "Test Subject",
            "<h1>Test Email</h1>"
        );

        Assert.True(result.Success, result.ErrorMessage);
    }
}
```

---

## Common Issues & Solutions

| Issue | Cause | Solution |
|-------|-------|----------|
| SocketException | Firewall/network blocking SMTP | Contact IT/network admin, or use corporate mail server |
| Authentication Failed (535) | Wrong password or App Password not set | Generate Gmail App Password, ensure 2FA enabled |
| Connection Timeout (15s) | Server not responding | Try port 465 instead of 587, or check firewall |
| TLS/SSL Error | SSL version mismatch | Ensure `EnableSsl: true` for port 587 |

---

## Network Troubleshooting Flowchart

```
Is Test-NetConnection successful?
?? YES (TcpTestSucceeded: True)
?  ?? Is your Gmail password an App Password?
?  ?  ?? NO ? Generate App Password from myaccount.google.com/apppasswords
?  ?  ?? YES ? Try sending email, check detailed logs
?  ?? Email still fails?
?     ?? Check Gmail account settings and authentication logs
?
?? NO (TcpTestSucceeded: False)
   ?? Can you access Gmail via browser? (https://gmail.com)
   ?  ?? YES ? Your firewall is blocking SMTP (port 587)
   ?  ?  ?? Contact IT to whitelist smtp.gmail.com:587 or use port 465
   ?  ?? NO ? No internet connection or DNS issue
   ?     ?? Check network connectivity and DNS
```

---

## Alternative: Use Corporate/Internal Mail Server

If Gmail SMTP won't work in your network, ask your IT department for:
- Corporate SMTP server address
- SMTP port (usually 25, 587, or 465)
- Authentication credentials
- Whether SSL/TLS is required

Then update `appsettings.json` with those values.

---

## Files Updated

1. **SmtpEmailSender.cs** - Added comprehensive logging for all error scenarios
2. **ILogger injection** - Now optional; logger parameter added to constructor
3. **SocketException handling** - Specifically catches network/firewall issues

Check the **application logs** after each test to see detailed error information.
