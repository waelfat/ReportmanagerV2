using System;

namespace reportmangerv2.Services;

public interface IEmailSender
{
    Task SendEmailAsync(
        string to,// seperated by comma
        string subject,
        string htmlBody,
        string attachments = null,
        string cc = null // seperated by comma
    );


}
/*

 builder.Services.AddSingleton<IEmailService>(new MailKitEmailService(
    smtpServer: "mail.egyptpost.org",
    smtpPort: 587,
  // username: "EGYPTPOST\\87642",
  username:"87462",
    password: "FathySelim$1945"
));
*/
