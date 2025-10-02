using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace StarEvents.Services
{
    public class DummyEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            //NOthing for now
            Console.WriteLine($"Sending Email to {email} with subject {subject}");
            return Task.CompletedTask;
        }
    }
}
