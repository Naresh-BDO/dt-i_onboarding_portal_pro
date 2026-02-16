
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using DT_I_Onboarding_Portal.Core.enums;
using DT_I_Onboarding_Portal.Core.Models;

namespace DT_I_Onboarding_Portal.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpOptions _options;
        private readonly ILogger<SmtpEmailSender>? _logger;

        public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender>? logger = null)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<SendEmailResult> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            // Basic configuration validation
            if (string.IsNullOrWhiteSpace(_options.Host) ||
                _options.Port <= 0 ||
                string.IsNullOrWhiteSpace(_options.FromAddress))
            {
                _logger?.LogError("SMTP configuration incomplete - Host: {Host}, Port: {Port}, FromAddress: {FromAddress}",
                    _options.Host, _options.Port, _options.FromAddress);

                return new SendEmailResult
                {
                    Success = false,
                    ErrorType = EmailErrorType.ConfigurationError,
                    ErrorMessage = "SMTP configuration is incomplete (Host/Port/FromAddress).",
                };
            }

            try
            {
                var from = new MailAddress(_options.FromAddress, _options.FromDisplayName, Encoding.UTF8);
                MailAddress to;
                try
                {
                    to = new MailAddress(toEmail);
                }
                catch (FormatException fe)
                {
                    _logger?.LogWarning(fe, "Invalid recipient email format: {Email}", toEmail);

                    return new SendEmailResult
                    {
                        Success = false,
                        ErrorType = EmailErrorType.InvalidRecipientAddress,
                        ErrorMessage = "Recipient email format is invalid.",
                        ProviderMessage = fe.Message
                    };
                }

                _logger?.LogInformation("Connecting to SMTP server {Host}:{Port} with SSL={EnableSsl}", 
                    _options.Host, _options.Port, _options.EnableSsl);

                using var client = new SmtpClient(_options.Host, _options.Port)
                {
                    EnableSsl = _options.EnableSsl,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_options.Username, _options.Password),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 15000
                };

                using var message = new MailMessage(from, to)
                {
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8,
                    Priority = MailPriority.Normal
                };

                await client.SendMailAsync(message);

                _logger?.LogInformation("Email sent successfully to {To}", toEmail);
                return new SendEmailResult { Success = true };
            }
            catch (SmtpFailedRecipientException sfr)
            {
                _logger?.LogError(sfr, "SMTP recipient rejected - Status: {StatusCode}", sfr.StatusCode);

                return new SendEmailResult
                {
                    Success = false,
                    ErrorType = EmailErrorType.RecipientRejected,
                    ErrorMessage = "Recipient mailbox rejected the message (invalid inbox or policy).",
                    ProviderMessage = $"{sfr.StatusCode}: {sfr.Message}"
                };
            }
            catch (SocketException soex)
            {
                _logger?.LogError(soex, "Socket exception connecting to SMTP server {Host}:{Port}. This usually indicates a network/firewall issue.",
                    _options.Host, _options.Port);

                return new SendEmailResult
                {
                    Success = false,
                    ErrorType = EmailErrorType.SmtpConnectionFailed,
                    ErrorMessage = "Failed to connect to SMTP server. Please check network connectivity and firewall settings.",
                    ProviderMessage = $"SocketException: {soex.Message}"
                };
            }
            catch (SmtpException se)
            {
                _logger?.LogError(se, "SMTP exception occurred - Status: {StatusCode}", se.StatusCode);

                var lower = se.Message.ToLowerInvariant();
                var type =
                    lower.Contains("authentication") || lower.Contains("535") || lower.Contains("auth")
                        ? EmailErrorType.AuthenticationFailed
                    : lower.Contains("timed out") || lower.Contains("timeout")
                        ? EmailErrorType.Timeout
                    : lower.Contains("unavailable") || lower.Contains("cannot connect") || lower.Contains("connection")
                        ? EmailErrorType.SmtpConnectionFailed
                    : EmailErrorType.SmtpSendFailed;

                return new SendEmailResult
                {
                    Success = false,
                    ErrorType = type,
                    ErrorMessage = "SMTP operation failed.",
                    ProviderMessage = se.Message
                };
            }
            catch (TaskCanceledException tce)
            {
                _logger?.LogError(tce, "SMTP send operation timed out after {Timeout}ms", 15000);

                return new SendEmailResult
                {
                    Success = false,
                    ErrorType = EmailErrorType.Timeout,
                    ErrorMessage = "SMTP send timed out.",
                    ProviderMessage = tce.Message
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error while sending email to {To}", toEmail);

                return new SendEmailResult
                {
                    Success = false,
                    ErrorType = EmailErrorType.Unknown,
                    ErrorMessage = "Unexpected error while sending email.",
                    ProviderMessage = ex.Message
                };
            }
        }
    }
}
