using System;
using System.Net.Mail;
using System.Net;
using Relativity.API;

namespace LTASBM.Agent.Utilities
{
    public class Emails
    {
        public static void FixClientEmail(IInstanceSettingsBundle instanceSettingsBundle, string emailTo, string analystFirstName)
        {
            var smtpServer = instanceSettingsBundle.GetStringAsync("kCura.Notification", "SMTPServer");
            var smtpUser = instanceSettingsBundle.GetStringAsync("kCura.Notification", "SMTPUserName");
            var smtpPassword = instanceSettingsBundle.GetStringAsync("kCura.Notification", "SMTPPassword");
            int smtpPort = Convert.ToInt32(instanceSettingsBundle.GetUIntAsync("kCura.Notification", "SMTPPort").Result.Value);

            var emailMessage = new MailMessage
            {
                From = new MailAddress("noreply@relativity.one", "LTAS Billing Management"),
                Subject = "LTASBM:[FIX] - Review Client You Recently Created",
                Body = $@"<h3>Hi ${analystFirstName},</h3>
                        <p>Please update the client the client number should only be 5 digits [XXXXX].&nbsp;</p>
                        <p><img alt="""" src=""https://i.ibb.co/jhZzxJq/Correct-Client-Input.png"" style=""width: 401px; height: 200px;"" /></p>
                        <p>If you have any questions, please reach out to Damien for assistance.</p>
                        <p>Thank You,</p>
                        <p><img alt="""" src=""https://i.ibb.co/kgXTdQb/LTASLogo.png"" style=""width: 40px; height: 30px;"" />&nbsp;<strong>LTAS Billing Automations</strong></p>",
                IsBodyHtml = true
            };
            emailMessage.To.Add("recipient@example.com");
            emailMessage.CC.Add("ltasrelativity@quinnemanuel.com");
            
            using (var smtpClient = new SmtpClient())
            {
                smtpClient.Host = smtpServer.ToString();
                smtpClient.Port = smtpPort;
                smtpClient.EnableSsl = true;
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtpClient.DeliveryFormat = SmtpDeliveryFormat.SevenBit;
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new NetworkCredential(smtpUser.ToString(), smtpPassword.ToString());

                smtpClient.Send(emailMessage);
            }
        }
    }
}
