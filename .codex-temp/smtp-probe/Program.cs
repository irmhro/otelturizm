using MailKit.Net.Smtp;
using MailKit.Security;
using System.Security.Authentication;

async Task ProbeAsync(string host, int port, SecureSocketOptions options)
{
    using var client = new SmtpClient();
    client.Timeout = 30000;
    client.CheckCertificateRevocation = false;
    client.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;

    try
    {
        await client.ConnectAsync(host, port, options);
        await client.AuthenticateAsync("info@otelturizm.com", "cYUJ*6yozW$gFm)G");
        Console.WriteLine($"SMTP_OK {host}:{port} {options} Auth={client.IsAuthenticated} Secure={client.IsSecure}");
        await client.DisconnectAsync(true);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"SMTP_FAIL {host}:{port} {options} :: {ex.GetType().Name} :: {ex.Message}");
    }
}

await ProbeAsync("umay.muvhost.com", 587, SecureSocketOptions.StartTls);
await ProbeAsync("umay.muvhost.com", 587, SecureSocketOptions.Auto);
await ProbeAsync("umay.muvhost.com", 465, SecureSocketOptions.SslOnConnect);
