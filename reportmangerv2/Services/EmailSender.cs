using System;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;

namespace reportmangerv2.Services;

public class EmailSender : IEmailSender
{

    private readonly MailSettings _settings;
    private readonly string _reportsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Reports");

    public EmailSender(IOptions<MailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        string attachments = null,
        string cc = null
    )
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.AddRange(ParseAddresses(to));
        message.Subject = subject;

        if (!string.IsNullOrWhiteSpace(cc))
        {
            message.Cc.AddRange(ParseAddresses(cc));
        }

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };

        // Handle multiple attachments
        if (!string.IsNullOrWhiteSpace(attachments))
        {
            var files = attachments.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var file in files)
            {
                var filePath = Path.Combine(_reportsDirectory, file.Trim());
                if (File.Exists(filePath))
                {
                    bodyBuilder.Attachments.Add(filePath);
                }
            }
        }

        message.Body = bodyBuilder.ToMessageBody();
        /*

         using (var client = new SmtpClient())
                {

                    client.ServerCertificateValidationCallback = (s, c, h, e) => true; // For testing only
                   // await client.ConnectAsync(_smtpServer,_smtpPort,true,CancellationToken.None);
                    client.AuthenticationMechanisms.Remove("XOAUTH2");
                    client.Connect(_smtpServer, _smtpPort, SecureSocketOptions.StartTls);
                    try
                    {
                       //var ntlm = new SaslMechanismNtlm("EGYPTPOST\\87642","FathySelim$1945"); 
                     //  var ntlm = new SaslMechanismNtlm("EGYPTPOST\\87462","weal"); 
                        //sm02-dpp-8xg\post

                    client.Authenticate(_username,_password);
                   //client.Authenticate(ntlm);
                    client.Send(message);
                    }catch( Exception ex)
                    {
                     Console.WriteLine(ex.Message);

                    }
                    finally
                    {

                    client.Disconnect(true);
                    }

                }



        */
        using (var client = new SmtpClient())
        {
            try
            {
                //remove oath
                client.AuthenticationMechanisms.Remove("XOAUTH2");
                client.Connect(_settings.Server, _settings.Port, MailKit.Security.SecureSocketOptions.StartTls);
                client.Authenticate(_settings.Username, _settings.Password);
                client.Send(message);
                client.Disconnect(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
            finally
            {

                client.Disconnect(true);
            }
        }
        Console.WriteLine("Email Sent Successfully");
        
    

    }

    private static IEnumerable<MailboxAddress> ParseAddresses(string addresses)
    {
        return addresses
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(addr => new MailboxAddress("", addr.Trim()));
    }


}
