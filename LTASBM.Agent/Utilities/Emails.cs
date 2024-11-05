using System.Net.Mail;
using System.Net;
using Relativity.API;
using System.Text;
using System;
using System.Collections.Generic;

namespace LTASBM.Agent.Utilities
{
    public class SMTPSetting
    {
        public string Section { get; set; }
        public string Name { get; set; }
    }
    public class Emails
    {
        static string smtpPasswordValue;
        static int smtpPortValue;
        static string smtpUserValue;
        static string smtpServerValue;
        static string smtpEnvironmentValue;
        private static void GetSMTPValue(string settingName, SMTPSetting smtpInstanceSettingSingle, IInstanceSettingsBundle instanceSettingsBundle)
        {
            switch (settingName)
            {
                case "SMTPPassword":
                    var singleSettingValuePass = instanceSettingsBundle.GetStringAsync(smtpInstanceSettingSingle.Section, smtpInstanceSettingSingle.Name);
                    smtpPasswordValue = singleSettingValuePass.Result;
                    break;

                case "SMTPPort":
                    int singleSettingValuePort = Convert.ToInt32(instanceSettingsBundle.GetUIntAsync(smtpInstanceSettingSingle.Section, smtpInstanceSettingSingle.Name).Result.Value);
                    smtpPortValue = singleSettingValuePort;
                    break;

                case "SMTPUserName":
                    var singleSettingValueUser = instanceSettingsBundle.GetStringAsync(smtpInstanceSettingSingle.Section, smtpInstanceSettingSingle.Name);
                    smtpUserValue = singleSettingValueUser.Result;
                    break;

                case "SMTPServer":
                    var singleSettingValueServer = instanceSettingsBundle.GetStringAsync(smtpInstanceSettingSingle.Section, smtpInstanceSettingSingle.Name);
                    smtpServerValue = singleSettingValueServer.Result;
                    break;
                case "RelativityInstanceURL":
                    var singleSettingValueEnvironment = instanceSettingsBundle.GetStringAsync(smtpInstanceSettingSingle.Section, smtpInstanceSettingSingle.Name);
                    smtpEnvironmentValue = singleSettingValueEnvironment.Result;
                    break;
            }
        }        
        public static void InvalidClientNumber(IInstanceSettingsBundle instanceSettingsBundle, StringBuilder htmlBody, string emailAddress)
        {
            SMTPSetting smtpPassword = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPPassword" };
            SMTPSetting smtpPort = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPPort" };
            SMTPSetting smtpServer = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPServer" };
            SMTPSetting smtpUser = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPUserName" };
            SMTPSetting smtpEnvironment = new SMTPSetting { Section = "Relativity.Core", Name = "RelativityInstanceURL" };

            List<SMTPSetting> smtpSettings = new List<SMTPSetting> { smtpPort, smtpServer, smtpUser, smtpPassword, smtpEnvironment };

            foreach (var smtpInstanceSettingSingle in smtpSettings)
            {                
                try
                {
                    GetSMTPValue(smtpInstanceSettingSingle.Name, smtpInstanceSettingSingle, instanceSettingsBundle);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            var emailMessage = new MailMessage
            {
                From = new MailAddress("noreply@relativity.one", "LTAS Billing Management"),
                Subject = $"{smtpEnvironmentValue.Split('-')[1].Split('.')[0].ToUpper()} - Invalid Client Number",
                Body = htmlBody.ToString(),
                IsBodyHtml = true
            };
            emailMessage.To.Add(emailAddress);
            emailMessage.CC.Add("ltasrelativity@quinnemanuel.com");
            emailMessage.ReplyToList.Add(new MailAddress("dmaienyoung@quinnemanuel.com", "Damien Young"));

            using (var smtpClient = new SmtpClient())
            {
                smtpClient.Host = smtpServerValue;
                smtpClient.Port = smtpPortValue;
                smtpClient.EnableSsl = true;
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtpClient.DeliveryFormat = SmtpDeliveryFormat.SevenBit;
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new NetworkCredential(smtpUserValue, smtpPasswordValue);
                smtpClient.Send(emailMessage);
            }
        }
        public static void NewClientsToBeCreated(IInstanceSettingsBundle instanceSettingsBundle, StringBuilder htmlBody, string emailAddress)
        {
            SMTPSetting smtpPassword = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPPassword" };
            SMTPSetting smtpPort = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPPort" };
            SMTPSetting smtpServer = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPServer" };
            SMTPSetting smtpUser = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPUserName" };
            SMTPSetting smtpEnvironment = new SMTPSetting { Section = "Relativity.Core", Name = "RelativityInstanceURL" };

            List<SMTPSetting> smtpSettings = new List<SMTPSetting> { smtpPort, smtpServer, smtpUser, smtpPassword, smtpEnvironment };

            foreach (var smtpInstanceSettingSingle in smtpSettings)
            {
                try
                {
                    GetSMTPValue(smtpInstanceSettingSingle.Name, smtpInstanceSettingSingle, instanceSettingsBundle);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            var emailMessage = new MailMessage
            {
                From = new MailAddress("noreply@relativity.one", "LTAS Billing Management"),
                Subject = $"{smtpEnvironmentValue.Split('-')[1].Split('.')[0].ToUpper()} - New Clients To Be Created",
                Body = htmlBody.ToString(),
                IsBodyHtml = true
            };
            emailMessage.To.Add(emailAddress);            
            emailMessage.ReplyToList.Add(new MailAddress("dmaienyoung@quinnemanuel.com", "Damien Young"));

            using (var smtpClient = new SmtpClient())
            {
                smtpClient.Host = smtpServerValue;
                smtpClient.Port = smtpPortValue;
                smtpClient.EnableSsl = true;
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtpClient.DeliveryFormat = SmtpDeliveryFormat.SevenBit;
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new NetworkCredential(smtpUserValue, smtpPasswordValue);
                smtpClient.Send(emailMessage);
            }
        }

        public static void testemail(IInstanceSettingsBundle instanceSettingsBundle, StringBuilder htmlBody)
        {
            SMTPSetting smtpPassword = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPPassword" };
            SMTPSetting smtpPort = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPPort" };
            SMTPSetting smtpServer = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPServer" };
            SMTPSetting smtpUser = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPUserName" };
            SMTPSetting smtpEnvironment = new SMTPSetting { Section = "Relativity.Core", Name = "RelativityInstanceURL" };

            List<SMTPSetting> smtpSettings = new List<SMTPSetting> { smtpPort, smtpServer, smtpUser, smtpPassword, smtpEnvironment };

            foreach (var smtpInstanceSettingSingle in smtpSettings)
            {
                try
                {
                    GetSMTPValue(smtpInstanceSettingSingle.Name, smtpInstanceSettingSingle, instanceSettingsBundle);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            var emailMessage = new MailMessage
            {
                From = new MailAddress("noreply@relativity.one", "LTAS Billing Management"),
                Subject = $"{smtpEnvironmentValue.Split('-')[1].Split('.')[0].ToUpper()} - debug message",
                Body = htmlBody.ToString(),
                IsBodyHtml = true
            };
            emailMessage.To.Add("damienyoung@quinnemanuel.com");
            emailMessage.ReplyToList.Add(new MailAddress("dmaienyoung@quinnemanuel.com", "Damien Young"));

            using (var smtpClient = new SmtpClient())
            {
                smtpClient.Host = smtpServerValue;
                smtpClient.Port = smtpPortValue;
                smtpClient.EnableSsl = true;
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtpClient.DeliveryFormat = SmtpDeliveryFormat.SevenBit;
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new NetworkCredential(smtpUserValue, smtpPasswordValue);
                smtpClient.Send(emailMessage);
            }
        }
    }  
}
