# Quick Reference: SMTP Connection Error Solutions

## Error
```
SocketException: A connection attempt failed because the connected party did not 
properly respond after a period of time, or established connection failed because 
connected host has failed to respond.
```

---

## Quick Fixes (Try in Order)

### 1. **Test Network Access** (30 seconds)

**Option A: PowerShell Test (Recommended)**
```powershell
Test-NetConnection smtp.gmail.com -Port 587
# Expected: TcpTestSucceeded = True
```

**Option B: If PowerShell crashes, use Command Prompt**
```cmd
telnet smtp.gmail.com 587
# Expected: You should see a connection message (not "Could not open connection")
```

**Option C: Using .NET from your application**
```csharp
using System.Net.Sockets;

try
{
    using (var client = new TcpClient())
    {
        var task = client.ConnectAsync("smtp.gmail.com", 587);
        bool connected = task.Wait(TimeSpan.FromSeconds(5));
        
        if (connected)
            Console.WriteLine("? Connection successful to smtp.gmail.com:587");
        else
            Console.WriteLine("? Connection timed out");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"? Connection failed: {ex.Message}");
}
```

? Failed? ? **Your firewall is blocking port 587** ? Go to Fix #3

? Passed? ? Go to Fix #2

---

### 2. **Verify Gmail Credentials** (2 minutes)
- Go to: https://myaccount.google.com/apppasswords
- Generate an App Password (16 characters)
- Update `appsettings.json` with this password
- Ensure 2FA is enabled on your Google account

? Still failing? ? Go to Fix #3

? Working? ? Done! ?

---

### 3. **Ask Your IT/Network Admin**
Your network is blocking outbound SMTP connections. Request:
- Whitelist `smtp.gmail.com` on port 587 or 465
- OR get your corporate SMTP server details

---

## Configuration File Locations

| File | Location |
|------|----------|
| SMTP Settings | `DT-I_Onboarding_Portal.Server/appsettings.json` |
| Email Service | `DT-I_Onboarding_Portal.Services/Services/SmtpEmailSender.cs` |
| Detailed Guide | `SMTP_TROUBLESHOOTING_GUIDE.md` |

---

## Check Application Logs

After testing, run the application and look for logs mentioning:
- "Connecting to SMTP server"
- "Socket exception" or "Authentication failed"
- "Email sent successfully"

Logs appear in:
- Visual Studio Output window
- Console if running from command line
- Application Insights (if configured)

---

## Alternative: Use Corporate Mail Server

If Gmail doesn't work, contact IT for:
- SMTP server address (e.g., `mail.company.com`)
- Port (usually 25, 587, or 465)
- Username/password
- Whether SSL/TLS is required

Then update `appsettings.json` accordingly.

---

## Files That Were Updated

? `SmtpEmailSender.cs` - Added logging and SocketException handling
? Backward compatible - no breaking changes

---

## Next: Secure Your Credentials

**Never commit passwords to Git!** Use User Secrets:

```powershell
cd DT-I_Onboarding_Portal.Server
dotnet user-secrets init
dotnet user-secrets set "SmtpSettings:Password" "your-app-password"
```

Then remove passwords from `appsettings.json`.
