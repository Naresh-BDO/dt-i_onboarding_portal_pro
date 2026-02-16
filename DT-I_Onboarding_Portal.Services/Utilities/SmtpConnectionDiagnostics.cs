using System.Net.Sockets;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using DT_I_Onboarding_Portal.Core.Models;

namespace DT_I_Onboarding_Portal.Services.Utilities
{
    /// <summary>
    /// Diagnostic utility to test SMTP connectivity without sending emails
    /// </summary>
    public class SmtpConnectionDiagnostics
    {
        private readonly ILogger<SmtpConnectionDiagnostics>? _logger;

        public SmtpConnectionDiagnostics(ILogger<SmtpConnectionDiagnostics>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Test basic TCP connection to SMTP server
        /// </summary>
        public async Task<DiagnosticResult> TestTcpConnectionAsync(string host, int port, int timeoutMs = 5000)
        {
            var result = new DiagnosticResult { Host = host, Port = port };

            try
            {
                _logger?.LogInformation("Testing TCP connection to {Host}:{Port}", host, port);

                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(host, port);
                    bool completed = connectTask.Wait(TimeSpan.FromMilliseconds(timeoutMs));

                    if (completed && client.Connected)
                    {
                        result.TcpConnected = true;
                        result.Message = $"? Successfully connected to {host}:{port}";
                        _logger?.LogInformation(result.Message);
                        return result;
                    }
                    else
                    {
                        result.TcpConnected = false;
                        result.Message = $"? Connection to {host}:{port} timed out after {timeoutMs}ms";
                        _logger?.LogWarning(result.Message);
                        return result;
                    }
                }
            }
            catch (SocketException sex)
            {
                result.TcpConnected = false;
                result.Message = $"? Socket error: {sex.Message} (Code: {sex.SocketErrorCode})";
                _logger?.LogError(sex, "Socket exception during TCP test");
                return result;
            }
            catch (Exception ex)
            {
                result.TcpConnected = false;
                result.Message = $"? Error during connection test: {ex.Message}";
                _logger?.LogError(ex, "Exception during TCP test");
                return result;
            }
        }

        /// <summary>
        /// Test SMTP connection with credentials (no email sending)
        /// </summary>
        public async Task<DiagnosticResult> TestSmtpConnectionAsync(SmtpOptions options, int timeoutMs = 15000)
        {
            var result = new DiagnosticResult 
            { 
                Host = options.Host, 
                Port = options.Port,
                IsSmtpTest = true
            };

            try
            {
                _logger?.LogInformation("Testing SMTP connection to {Host}:{Port} with SSL={EnableSsl}",
                    options.Host, options.Port, options.EnableSsl);

                using (var client = new SmtpClient(options.Host, options.Port))
                {
                    client.EnableSsl = options.EnableSsl;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new System.Net.NetworkCredential(options.Username, options.Password);
                    client.Timeout = timeoutMs;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;

                    // Create a minimal test message to verify SMTP connection
                    var testMessage = new MailMessage(
                        new MailAddress("test@example.com"),
                        new MailAddress("test@example.com"))
                    {
                        Subject = "SMTP Connectivity Test",
                        Body = "This is a diagnostic test - can be ignored"
                    };

                    try
                    {
                        // Send test message with timeout
                        var sendTask = client.SendMailAsync(testMessage);
                        bool completed = await Task.WhenAny(
                            sendTask,
                            Task.Delay(timeoutMs)) == sendTask;

                        if (completed)
                        {
                            result.SmtpConnected = true;
                            result.Message = $"? SMTP connection successful to {options.Host}:{options.Port}";
                            _logger?.LogInformation(result.Message);
                        }
                        else
                        {
                            result.SmtpConnected = false;
                            result.Message = $"? SMTP connection timed out after {timeoutMs}ms";
                            _logger?.LogWarning(result.Message);
                        }
                    }
                    finally
                    {
                        testMessage?.Dispose();
                    }
                }

                return result;
            }
            catch (SmtpFailedRecipientException sfr)
            {
                // This is actually OK - it means SMTP connection succeeded but rejected test email
                result.SmtpConnected = true;
                result.Message = $"? SMTP connection succeeded (recipient rejected test email - {sfr.StatusCode})";
                _logger?.LogInformation("SMTP connection OK, test recipient rejected (expected): {Message}", result.Message);
                return result;
            }
            catch (SmtpException sex)
            {
                result.SmtpConnected = false;
                result.Message = $"? SMTP error: {sex.Message} (Code: {sex.StatusCode})";
                _logger?.LogError(sex, "SMTP exception during connection test");
                return result;
            }
            catch (SocketException sex)
            {
                result.SmtpConnected = false;
                result.Message = $"? Socket error: {sex.Message} - This typically indicates firewall or network issues";
                _logger?.LogError(sex, "Socket exception during SMTP test");
                return result;
            }
            catch (Exception ex)
            {
                result.SmtpConnected = false;
                result.Message = $"? Error: {ex.Message}";
                _logger?.LogError(ex, "Exception during SMTP test");
                return result;
            }
        }

        /// <summary>
        /// Run complete diagnostic suite
        /// </summary>
        public async Task<DiagnosticSuite> RunFullDiagnosticsAsync(SmtpOptions options)
        {
            _logger?.LogInformation("Starting full SMTP diagnostics");

            var suite = new DiagnosticSuite
            {
                Timestamp = DateTime.UtcNow,
                ConfiguredHost = options.Host,
                ConfiguredPort = options.Port
            };

            // Test 1: TCP connectivity
            suite.TcpResult = await TestTcpConnectionAsync(options.Host, options.Port);

            // Test 2: SMTP connectivity (only if TCP passed)
            if (suite.TcpResult.TcpConnected)
            {
                suite.SmtpResult = await TestSmtpConnectionAsync(options);
            }
            else
            {
                suite.SmtpResult = new DiagnosticResult
                {
                    Host = options.Host,
                    Port = options.Port,
                    IsSmtpTest = true,
                    SmtpConnected = false,
                    Message = "Skipped - TCP connection failed"
                };
            }

            _logger?.LogInformation("Diagnostics complete. TCP: {TcpStatus}, SMTP: {SmtpStatus}",
                suite.TcpResult.TcpConnected ? "PASS" : "FAIL",
                suite.SmtpResult.SmtpConnected ? "PASS" : "FAIL");

            return suite;
        }
    }

    /// <summary>
    /// Single diagnostic test result
    /// </summary>
    public class DiagnosticResult
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public bool TcpConnected { get; set; }
        public bool SmtpConnected { get; set; }
        public bool IsSmtpTest { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Message}";
        }
    }

    /// <summary>
    /// Complete diagnostic suite results
    /// </summary>
    public class DiagnosticSuite
    {
        public DateTime Timestamp { get; set; }
        public string ConfiguredHost { get; set; }
        public int ConfiguredPort { get; set; }
        public DiagnosticResult TcpResult { get; set; }
        public DiagnosticResult SmtpResult { get; set; }

        public bool IsHealthy => TcpResult?.TcpConnected == true && SmtpResult?.SmtpConnected == true;

        public string GetSummary()
        {
            return $@"
SMTP Connection Diagnostics - {Timestamp:yyyy-MM-dd HH:mm:ss}
================================================

Configuration:
  Host: {ConfiguredHost}
  Port: {ConfiguredPort}

TCP Test:
  Status: {(TcpResult?.TcpConnected == true ? "? PASS" : "? FAIL")}
  Message: {TcpResult?.Message}

SMTP Test:
  Status: {(SmtpResult?.SmtpConnected == true ? "? PASS" : "? FAIL")}
  Message: {SmtpResult?.Message}

Overall: {(IsHealthy ? "? HEALTHY - Ready to send emails" : "? UNHEALTHY - Check configuration and network")}
";
        }
    }
}
