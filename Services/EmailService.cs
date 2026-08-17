using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace ConstructionFinance.Services;

public class EmailSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
    public string From { get; set; } = "";
    public string FromName { get; set; } = "Fundament";
    // auto | starttls | ssl | none
    public string Security { get; set; } = "auto";
    public bool AllowInvalidCert { get; set; }

    public bool Enabled => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(From);
}

public class AppOptions
{
    public string BaseUrl { get; set; } = "";
    public bool RequireConfirmedEmail { get; set; } = true;

    // За nginx приложение не всегда видит правильную схему, поэтому берём
    // APP_BASE_URL, если он задан, и только иначе — адрес текущего запроса.
    public string ResolveBaseUrl(string navBaseUri)
        => (string.IsNullOrWhiteSpace(BaseUrl) ? navBaseUri : BaseUrl).TrimEnd('/');
}

public class EmailResult
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
}

public class EmailService
{
    private readonly EmailSettings _s;
    private readonly ILogger<EmailService> _log;

    public EmailService(EmailSettings s, ILogger<EmailService> log) { _s = s; _log = log; }

    public bool Enabled => _s.Enabled;
    public string From => _s.From;

    public async Task<EmailResult> Send(string to, string subject, string html, string text)
    {
        if (!_s.Enabled)
            return new EmailResult { Error = "Отправка почты не настроена: не заданы SMTP_HOST или SMTP_FROM" };

        try
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(_s.FromName, _s.From));
            msg.To.Add(MailboxAddress.Parse(to));
            msg.Subject = subject;
            msg.Body = new BodyBuilder { HtmlBody = html, TextBody = text }.ToMessageBody();

            var options = _s.Security.Trim().ToLowerInvariant() switch
            {
                "ssl" => SecureSocketOptions.SslOnConnect,
                "starttls" => SecureSocketOptions.StartTls,
                "none" => SecureSocketOptions.None,
                _ => SecureSocketOptions.Auto
            };

            using var client = new SmtpClient();
            if (_s.AllowInvalidCert)
                client.ServerCertificateValidationCallback = (a, b, c, d) => true;

            await client.ConnectAsync(_s.Host, _s.Port, options);
            if (!string.IsNullOrWhiteSpace(_s.User))
                await client.AuthenticateAsync(_s.User, _s.Password);
            await client.SendAsync(msg);
            await client.DisconnectAsync(true);

            _log.LogInformation("Письмо отправлено на {To}: {Subject}", to, subject);
            return new EmailResult { Ok = true };
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Не удалось отправить письмо на {To}", to);
            return new EmailResult { Error = ex.Message };
        }
    }

    public Task<EmailResult> SendConfirmation(string to, string link) => Send(
        to,
        "Подтверждение адреса — Fundament",
        Template(
            "Подтвердите адрес почты",
            "Вы зарегистрировались в Fundament — учёте строительных финансов. Чтобы начать пользоваться приложением, подтвердите адрес почты.",
            "Подтвердить адрес",
            link,
            "Ссылка действует 24 часа. Если вы не регистрировались в Fundament, просто удалите это письмо."),
        $"Подтвердите адрес почты для входа в Fundament:\n{link}\n\nСсылка действует 24 часа. Если вы не регистрировались, удалите это письмо.");

    public Task<EmailResult> SendPasswordReset(string to, string link) => Send(
        to,
        "Восстановление пароля — Fundament",
        Template(
            "Восстановление пароля",
            "Кто-то запросил сброс пароля для вашей учётной записи в Fundament. Если это были вы, задайте новый пароль по кнопке ниже.",
            "Задать новый пароль",
            link,
            "Ссылка действует 24 часа. Если вы не запрашивали сброс, просто удалите это письмо — пароль останется прежним."),
        $"Сброс пароля в Fundament:\n{link}\n\nСсылка действует 24 часа. Если вы не запрашивали сброс, удалите это письмо.");

    public Task<EmailResult> SendTest(string to) => Send(
        to,
        "Проверка почты — Fundament",
        Template(
            "Почта настроена",
            "Это тестовое письмо из админки Fundament. Если вы его видите, отправка писем с сервера работает.",
            "Открыть Fundament",
            "https://fundament-app.ru",
            "Письмо отправлено вручную из раздела «Админка»."),
        "Это тестовое письмо из админки Fundament. Отправка писем работает.");

    private static string Template(string title, string intro, string button, string link, string footer) => $@"
<!DOCTYPE html>
<html lang=""ru"">
<body style=""margin:0;padding:0;background:#f4f4f5;font-family:Inter,Arial,Helvetica,sans-serif;"">
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#f4f4f5;padding:24px 12px;"">
    <tr><td align=""center"">
      <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:520px;background:#ffffff;border-radius:10px;overflow:hidden;border:1px solid #e4e4e7;"">
        <tr><td style=""background:#c1440e;padding:20px 28px;color:#ffffff;font-size:20px;font-weight:700;letter-spacing:.3px;"">FUNDAMENT</td></tr>
        <tr><td style=""padding:28px;"">
          <h1 style=""margin:0 0 14px;font-size:20px;color:#18181b;"">{title}</h1>
          <p style=""margin:0 0 22px;font-size:15px;line-height:1.55;color:#3f3f46;"">{intro}</p>
          <table role=""presentation"" cellpadding=""0"" cellspacing=""0""><tr><td style=""border-radius:7px;background:#c1440e;"">
            <a href=""{link}"" style=""display:inline-block;padding:12px 26px;font-size:15px;font-weight:600;color:#ffffff;text-decoration:none;"">{button}</a>
          </td></tr></table>
          <p style=""margin:22px 0 0;font-size:13px;line-height:1.5;color:#71717a;"">Если кнопка не работает, скопируйте ссылку в адресную строку браузера:<br />
            <a href=""{link}"" style=""color:#c1440e;word-break:break-all;"">{link}</a>
          </p>
        </td></tr>
        <tr><td style=""padding:16px 28px 22px;border-top:1px solid #e4e4e7;font-size:12px;line-height:1.5;color:#a1a1aa;"">{footer}</td></tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";
}
