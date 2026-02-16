# SMTP Mail Troubleshooting Guide

## Overview
Your SMTP email sending is failing. This guide helps you diagnose and fix the issue.

---

## Quick Diagnostics

When your application starts, it now logs SMTP configuration status. Check the console output for messages like:
- ? SMTP Configuration loaded successfully
- ??  WARNING: SMTP configuration is incomplete!

---

## Common SMTP Issues & Solutions

### 1. **Authentication Failed (Error 535)**
**Symptoms:** "Authentication failed" or "535 5.7.8 Error"

**Cause:** Incorrect username/password for Gmail SMTP

**Solution for Gmail:**
- Generate an **App Password** (not your regular Gmail password):
  1. Go to https://myaccount.google.com/security
  2. Enable 2-Step Verification (if not already enabled)
  3. Go to "App passwords" (under 2-Step Verification)
  4. Select "Mail" and "Windows Computer"
  5. Copy the generated 16-character password
  6. Update `appsettings.json`:
     ```json
     "Password": "xxxx xxxx xxxx xxxx"  // Remove spaces if needed
     ```

### 2. **Connection Timeout**
**Symptoms:** "Connection timed out" or "Unable to connect"

**Cause:** SMTP server unreachable or firewall blocking port 587

**Solution:**
- Check your network connection
- Verify firewall allows outbound connections on port 587
- Test connectivity:
  ```powershell
  Test-NetConnection -ComputerName smtp.gmail.com -Port 587
  ```

### 3. **SSL/TLS Error**
**Symptoms:** "SSL connection error" or "TrustServerCertificate"

**Solution:**
- Verify `appsettings.json` has:
  ```json
  "EnableSsl": true,
  "Port": 587
  ```
- Do NOT use port 465 with standard `SmtpClient`

### 4. **Incomplete Configuration**
**Symptoms:** "SMTP configuration is incomplete"

**Solution:** Ensure all fields are set in `appsettings.json`:
```json
"SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromAddress": "your-email@gmail.com",
    "FromDisplayName": "Your Display Name"
}
```

### 5. **Recipient Email Rejected**
**Symptoms:** "Recipient mailbox rejected the message"

**Cause:** Invalid recipient email or mailbox policy

**Solution:**
- Verify recipient email address is correct
- Check if recipient's mailbox is full
- For Gmail, try sending to another Gmail account first

---

## Testing SMTP Configuration

### Use the Diagnostics API Endpoint

1. **Check SMTP Configuration:**
   ```bash
   curl -H "Authorization: Bearer YOUR_JWT_TOKEN" \
        https://localhost:7107/api/diagnostics/smtp-config
   ```

   Response shows all configuration details (password not exposed).

2. **Send a Test Email:**
   ```bash
   curl -X POST \
        -H "Authorization: Bearer YOUR_JWT_TOKEN" \
        "https://localhost:7107/api/diagnostics/send-test-email?toEmail=test@example.com"
   ```

   Response will show detailed error information if it fails.

### Manual Testing in C#

Create a simple test program:
```csharp
using System.Net;
using System.Net.Mail;

var smtp = new SmtpClient("smtp.gmail.com", 587)
{
    EnableSsl = true,
    UseDefaultCredentials = false,
    Credentials = new NetworkCredential("your-email@gmail.com", "your-app-password"),
    Timeout = 10000
};

try
{
    using var message = new MailMessage("your-email@gmail.com", "recipient@example.com")
    {
        Subject = "Test",
        Body = "Test email",
        IsBodyHtml = true
    };
    
    await smtp.SendMailAsync(message);
    Console.WriteLine("? Email sent successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"? Error: {ex.Message}");
}
```

---

## Logging & Debugging

### Check Application Logs

The `SmtpEmailSender` now includes detailed logging. Enable debug logging in `appsettings.json`:

```json
"Logging": {
    "LogLevel": {
        "Default": "Information",
        "Microsoft.AspNetCore": "Warning",
        "DT_I_Onboarding_Portal.Services.SmtpEmailSender": "Debug"
    }
}
```

### Log Messages

- **Info:** "Attempting to send email to {ToEmail} via {Host}:{Port}"
- **Info:** "Email sent successfully to {ToEmail}"
- **Error:** "SMTP Exception: {Message}"
- **Error:** "Recipient rejected: {StatusCode} {Message}"

---

## Step-by-Step Fix Checklist

- [ ] Verify Gmail account has 2-Step Verification enabled
- [ ] Generate a new App Password for Gmail
- [ ] Update `appsettings.json` with correct credentials
- [ ] Ensure `Host` is "smtp.gmail.com"
- [ ] Ensure `Port` is "587"
- [ ] Ensure `EnableSsl` is "true"
- [ ] Test network connectivity to smtp.gmail.com:587
- [ ] Check application startup logs for configuration warnings
- [ ] Send test email via diagnostics endpoint
- [ ] Check recipient inbox and spam folder
- [ ] Review application logs for detailed error messages

---

## API Endpoints (Admin Only)

### GET /api/diagnostics/smtp-config
Returns current SMTP configuration (password not exposed).

### POST /api/diagnostics/send-test-email?toEmail={email}
Sends a test email and returns detailed error information if it fails.

---

## Security Considerations

?? **Important:** 
- Never commit `appsettings.json` with real credentials to version control
- Use User Secrets or environment variables in production
- The diagnostics endpoints are **Admin-only** and require JWT authentication
- Never expose the actual password in logs or API responses

---

## Additional Resources

- **Gmail App Passwords:** https://support.google.com/accounts/answer/185833
- **SmtpClient Documentation:** https://docs.microsoft.com/dotnet/api/system.net.mail.smtpclient
- **Email Error Codes:** https://docs.microsoft.com/en-us/exchange/mail-flow/smtp-error-codes

---

## Still Not Working?

1. Run the application and check console output during startup
2. Call `/api/diagnostics/smtp-config` to verify configuration is loaded
3. Call `/api/diagnostics/send-test-email` with a test email address
4. Check application logs for detailed error messages
5. Review the response from the test email endpoint - it includes the exact error from the SMTP server

The improved error handling now captures and returns the exact SMTP error, making it much easier to diagnose the root cause.
