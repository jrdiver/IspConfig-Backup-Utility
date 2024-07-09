// using SendGrid's C# Library
// https://github.com/sendgrid/sendgrid-csharp

using System;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace SendgridConnector;

public class SendEmail
{
    private readonly string apiKey;
    public string FromEmail = string.Empty;
    public string FromName = string.Empty;
    public string ToEmail = string.Empty;
    public string ToName = string.Empty;
    public string Subject = string.Empty;
    public string Message = string.Empty;
    public Response Response;

    public SendEmail(string sendgridApiKey)
    {
        apiKey = sendgridApiKey;
    }

    public bool SendMessage()
    {
        Task result = Execute();
        result.GetAwaiter().GetResult();
        return Response.IsSuccessStatusCode;
    }

    private async Task Execute()
    {
        SendGridClient client = new(apiKey);
        EmailAddress from = new(FromEmail, FromName);
        EmailAddress to = new(ToEmail, ToName);
        SendGridMessage msg = MailHelper.CreateSingleEmail(from, to, Subject, "", Message);
        Task<Response> send = client.SendEmailAsync(msg);
        Response = await send;
    }
}
