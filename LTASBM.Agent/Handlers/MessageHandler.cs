using LTASBM.Agent.Models;
using Relativity.API;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Linq;

namespace LTASBM.Agent.Handlers
{
    public class MessageHandler
    {
        public class SMTPSetting
        {
            public string Section { get; set; }
            public string Name { get; set; }
        }

        public static StringBuilder InvalidClientEmailBody(StringBuilder htmlBody, EddsClients clients)
        {
            htmlBody.AppendLine("<!DOCTYPE html>");
            htmlBody.AppendLine("<html>");
            htmlBody.AppendLine("<head>");
            htmlBody.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            htmlBody.AppendLine("<style>");
            htmlBody.AppendLine("@media screen and (max-width: 600px) {");
            htmlBody.AppendLine("  table { width: 100% !important; font-size: 14px; }");
            htmlBody.AppendLine("  img { max-width: 100% !important; height: auto !important; }");
            htmlBody.AppendLine("  .logo-image { width: 20px !important; height: 10px !important; }");  // Mobile size
            htmlBody.AppendLine("}");
            htmlBody.AppendLine("@media screen and (min-width: 601px) {");  // Desktop specific
            htmlBody.AppendLine("  .logo-image { width: 15px !important; height: 7px !important; }");  // Smaller for desktop
            htmlBody.AppendLine("}");
            htmlBody.AppendLine(".logo-image { vertical-align: middle; }");
            htmlBody.AppendLine(".email-table { max-width: 800px; width: 100%; margin: 0 auto; }");
            htmlBody.AppendLine("</style>");
            htmlBody.AppendLine("</head>");
            htmlBody.AppendLine("<body style='font-family: Arial, sans-serif;'>");
            htmlBody.AppendLine("<div class='email-table'>");
            // Email content
            htmlBody.AppendLine($"<p>Hi {clients.EddsClientCreatedByFirstName},</p>");
            htmlBody.AppendLine("<p>Please update the client, the client number should only be 5 digits [XXXXX], however there are a few exceptions.</p>");
            htmlBody.AppendLine("<p><em>Here are the following allowable exceptions:</em></p>");
            htmlBody.AppendLine("<ul>");
            htmlBody.AppendLine("\t<li><em>Review Vendor</em></li>");
            htmlBody.AppendLine("\t<li><em>Co-Counsel</em></li>");
            htmlBody.AppendLine("\t<li><em>Software</em></li>");
            htmlBody.AppendLine("</ul>");
            htmlBody.AppendLine("<p><img alt=\"\" src=\"https://i.ibb.co/jhZzxJq/Correct-Client-Input.png\" style=\"max-width: 350px; width: 100%; height: auto;\" /></p>");
            //htmlBody.AppendLine("<p><strong>Clients with Invalid Client Numbers</strong></p>");
            // Responsive table
            htmlBody.AppendLine("<table border=\"1\" bordercolor=\"#ccc\" cellpadding=\"5\" cellspacing=\"0\" style=\"border-collapse:collapse; max-width: 600px; width: 100%; margin: 0 auto;\">");
            htmlBody.AppendLine("\t<tbody>");
            htmlBody.AppendLine("\t\t<tr style=\"background-color: #f2f2f2;\">");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;\">Client Name</th>");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;\">Client Number</th>");
            htmlBody.AppendLine("\t\t</tr>");
            htmlBody.AppendLine("\t\t<tr>");
            htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;'>{clients.EddsClientName}</td>");
            htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;'>{clients.EddsClientNumber}</td>");
            htmlBody.AppendLine("\t\t</tr>");
            htmlBody.AppendLine("\t</tbody>");
            htmlBody.AppendLine("</table>");
            htmlBody.AppendLine("<p>&nbsp;</p>");
            // Client link
            htmlBody.AppendLine($"<p><a href=\"https://qe-us.relativity.one/Relativity/RelativityInternal.aspx?AppID=-1&ArtifactTypeID=5&ArtifactID={clients.EddsClientArtifactId}&Mode=Forms&FormMode=view&LayoutID=null&SelectedTab=null\">Click here to view the record</a></p>");                                               
            htmlBody.AppendLine("<p>If you have any questions, please reach out to Damien for assistance.</p>");
            // Footer with responsive logo
            htmlBody.AppendLine("<p>");
            htmlBody.AppendLine("<img class='logo-image' alt=\"\" src=\"https://i.ibb.co/H7g8BSz/LTASLogo-small.png\" /> ");
            htmlBody.AppendLine("<small><var><code><strong>LTAS Billing Automations</strong></code></var></small>");
            htmlBody.AppendLine("</p>");
            htmlBody.AppendLine("<p><small><var><code><strong>[FYI: this job runs once an hour]</strong></code></var></small></p>");
            htmlBody.AppendLine("</div>");
            htmlBody.AppendLine("</body></html>");

            return htmlBody;
        }        
        public static StringBuilder NewClientsEmailBody(StringBuilder htmlBody, List<EddsClients> clients)
        {
            htmlBody.AppendLine("<!DOCTYPE html>");
            htmlBody.AppendLine("<html>");
            htmlBody.AppendLine("<head>");
            htmlBody.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            htmlBody.AppendLine("<style>");
            htmlBody.AppendLine("@media screen and (max-width: 600px) {");
            htmlBody.AppendLine("  table { width: 100% !important; font-size: 14px; }");
            htmlBody.AppendLine("  img { max-width: 100% !important; height: auto !important; }");
            htmlBody.AppendLine("}");
            htmlBody.AppendLine(".logo-image { width: 20px; height: 10px; vertical-align: middle; }");
            htmlBody.AppendLine(".email-table { max-width: 800px; width: 100%; margin: 0 auto; }");
            htmlBody.AppendLine("</style>");
            htmlBody.AppendLine("</head>");
            htmlBody.AppendLine("<body style='font-family: Arial, sans-serif;'>");
            htmlBody.AppendLine("<div class='email-table'>");

            htmlBody.AppendLine("<p>The following clients exist in EDDS but are missing from the Billing workspace:</p>");

            // Responsive table
            htmlBody.AppendLine("<table border=\"1\" bordercolor=\"#ccc\" cellpadding=\"5\" cellspacing=\"0\" style=\"border-collapse:collapse; max-width: 600px; width: 100%; margin: 0 auto;\">");
            htmlBody.AppendLine("\t<tbody>");
            htmlBody.AppendLine("\t\t<tr style=\"background-color: #f2f2f2;\">");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;\">Client Name</th>");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;\">Client Number</th>");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;\">Client EDDS ArtifactId</th>");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;\">Created By</th>");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;\">Link</th>");
            htmlBody.AppendLine("\t\t</tr>");

            foreach (var record in clients)
            {
                htmlBody.AppendLine("\t\t<tr>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;'>{record.EddsClientName}</td>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;'>{record.EddsClientNumber}</td>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;'>{record.EddsClientArtifactId}</td>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;'>{record.EddsClientCreatedByFirstName}</td>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;'><a href=\"https://qe-us.relativity.one/Relativity/RelativityInternal.aspx?AppID=-1&ArtifactTypeID=5&ArtifactID={record.EddsClientArtifactId}&Mode=Forms&FormMode=view&LayoutID=null&SelectedTab=null\">View</a></td>");
                htmlBody.AppendLine("\t\t</tr>");
            }

            htmlBody.AppendLine("\t</tbody>");
            htmlBody.AppendLine("</table>");
            htmlBody.AppendLine("<p>&nbsp;</p>");

            // Footer with responsive logo
            htmlBody.AppendLine("<p>");
            htmlBody.AppendLine("<img class='logo-image' alt=\"\" src=\"https://i.ibb.co/H7g8BSz/LTASLogo-small.png\" /> ");
            htmlBody.AppendLine("<small><var><code><strong>LTAS Billing Automations</strong></code></var></small>");
            htmlBody.AppendLine("</p>");

            htmlBody.AppendLine("<p><small><var><code><strong>[FYI: this job runs once an hour]</strong></code></var></small></p>");

            htmlBody.AppendLine("</div>");
            htmlBody.AppendLine("</body></html>");

            return htmlBody;
        }
        public static StringBuilder InvalidMatterEmailBody(StringBuilder htmlBody, EddsMatters matters )
        {
            htmlBody.AppendLine("<!DOCTYPE html>");
            htmlBody.AppendLine("<html>");
            htmlBody.AppendLine("<head>");
            htmlBody.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            htmlBody.AppendLine("<style>");
            htmlBody.AppendLine("@media screen and (max-width: 600px) {");
            htmlBody.AppendLine("  table { width: 100% !important; font-size: 14px; }");
            htmlBody.AppendLine("  img { max-width: 100% !important; height: auto !important; }");
            htmlBody.AppendLine("  .logo-image { width: 20px !important; height: 10px !important; }");  // Mobile size
            htmlBody.AppendLine("}");
            htmlBody.AppendLine("@media screen and (min-width: 601px) {");  // Desktop specific
            htmlBody.AppendLine("  .logo-image { width: 15px !important; height: 7px !important; }");  // Smaller for desktop
            htmlBody.AppendLine("}");
            htmlBody.AppendLine(".logo-image { vertical-align: middle; }");
            htmlBody.AppendLine(".email-table { max-width: 800px; width: 100%; margin: 0 auto; }");
            htmlBody.AppendLine("</style>");
            htmlBody.AppendLine("</head>");
            htmlBody.AppendLine("<body style='font-family: Arial, sans-serif;'>");
            htmlBody.AppendLine("<div class='email-table'>");
            // Email content
            htmlBody.AppendLine($"<p>Hi {matters.EddsMatterCreatedByFirstName},</p>");
            htmlBody.AppendLine("<p>Please update the matter, the matter number should be at least 11 digits [XXXXX-XXXXXX].</p>");            
            htmlBody.AppendLine("<p><img alt=\"\" src=\"https://i.ibb.co/br139Fg/Correct-Matter-Input.png\" style=\"max-width: 350px; width: 100%; height: auto;\" /></p>");            
            // Responsive table
            htmlBody.AppendLine("<table border=\"1\" bordercolor=\"#ccc\" cellpadding=\"5\" cellspacing=\"0\" style=\"border-collapse:collapse; max-width: 600px; width: 100%; margin: 0 auto;\">");
            htmlBody.AppendLine("\t<tbody>");
            htmlBody.AppendLine("\t\t<tr style=\"background-color: #f2f2f2;\">");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;\">Matter Name</th>");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;\">Matter Number</th>");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;\">Matter ArtifactID</th>");
            htmlBody.AppendLine("\t\t</tr>");
            htmlBody.AppendLine("\t\t<tr>");
            htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;'>{matters.EddsMatterName}</td>");
            htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;'>{matters.EddsMatterNumber}</td>");
            htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;'>{matters.EddsMatterArtifactId}</td>");
            htmlBody.AppendLine("\t\t</tr>");
            htmlBody.AppendLine("\t</tbody>");
            htmlBody.AppendLine("</table>");
            htmlBody.AppendLine("<p>&nbsp;</p>");
            // Client link                     
            htmlBody.AppendLine($"<p><a href=\"https://qe-us.relativity.one/Relativity/RelativityInternal.aspx?AppID=-1&ArtifactTypeID=6&ArtifactID={matters.EddsMatterArtifactId}&Mode=Forms&FormMode=view&LayoutID=null&SelectedTab=null\">Click here to view the record</a></p>");
            htmlBody.AppendLine("<p>If you have any questions, please reach out to Damien for assistance.</p>");
            // Footer with responsive logo
            htmlBody.AppendLine("<p>");
            htmlBody.AppendLine("<img class='logo-image' alt=\"\" src=\"https://i.ibb.co/H7g8BSz/LTASLogo-small.png\" /> ");
            htmlBody.AppendLine("<small><var><code><strong>LTAS Billing Automations</strong></code></var></small>");
            htmlBody.AppendLine("</p>");
            htmlBody.AppendLine("<p><small><var><code><strong>[FYI: this job runs once an hour]</strong></code></var></small></p>");
            htmlBody.AppendLine("</div>");
            htmlBody.AppendLine("</body></html>");

            return htmlBody;
        }
        public static StringBuilder NewMattersEmailBody(StringBuilder htmlBody, List<EddsMatters> matters)
        {
            htmlBody.AppendLine("<!DOCTYPE html>");
            htmlBody.AppendLine("<html>");
            htmlBody.AppendLine("<head>");
            htmlBody.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            htmlBody.AppendLine("<style>");
            htmlBody.AppendLine("@media screen and (max-width: 600px) {");
            htmlBody.AppendLine("  table { width: 100% !important; font-size: 14px; }");
            htmlBody.AppendLine("  img { max-width: 100% !important; height: auto !important; }");
            htmlBody.AppendLine("}");
            htmlBody.AppendLine(".logo-image { width: 20px; height: 10px; vertical-align: middle; }");
            htmlBody.AppendLine(".email-table { max-width: 800px; width: 100%; margin: 0 auto; }");
            htmlBody.AppendLine("</style>");
            htmlBody.AppendLine("</head>");
            htmlBody.AppendLine("<body style='font-family: Arial, sans-serif;'>");
            htmlBody.AppendLine("<div class='email-table'>");

            htmlBody.AppendLine("<p>The following matters exist in EDDS but are missing from the Billing workspace, they will be created:</p>");
            htmlBody.AppendLine("<p>These may require setup on the billing page:</p>");
            htmlBody.AppendLine("<div style=\"width: 300px;\"><img src=\"https://i.ibb.co/V20Yp5j/billingsample-optimized.png\" style=\"width: 100%;\" /></div>");
            htmlBody.AppendLine("<br><br>");
            htmlBody.AppendLine("<br><br>");
            htmlBody.AppendLine("<br><br>");
            // Responsive table
            htmlBody.AppendLine("<table border=\"1\" bordercolor=\"#ccc\" cellpadding=\"5\" cellspacing=\"0\" style=\"border-collapse:collapse; max-width: 600px; width: 100%; margin: 0 auto;\">");
            htmlBody.AppendLine("\t<tbody>");
            htmlBody.AppendLine("\t\t<tr style=\"background-color: #f2f2f2;\">");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;white-space: nowrap;\">Matter Name</th>");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;white-space: nowrap;\">Matter Number</th>");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;white-space: nowrap;\">Matter EDDS ArtifactId</th>");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;white-space: nowrap;\">Created By</th>");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;\">Link</th>");
            htmlBody.AppendLine("\t\t</tr>");

            foreach (var record in matters)
            {
                htmlBody.AppendLine("\t\t<tr>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;white-space: nowrap;'>{record.EddsMatterName}</td>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;white-space: nowrap;'>{record.EddsMatterNumber}</td>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;white-space: nowrap;'>{record.EddsMatterArtifactId}</td>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;white-space: nowrap;'>{record.EddsMatterCreatedByFirstName}</td>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;'><a href=\"https://qe-us.relativity.one/Relativity/RelativityInternal.aspx?AppID=-1&ArtifactTypeID=5&ArtifactID={record.EddsMatterArtifactId}&Mode=Forms&FormMode=view&LayoutID=null&SelectedTab=null\">View</a></td>");
                htmlBody.AppendLine("\t\t</tr>");                           
            }

            htmlBody.AppendLine("\t</tbody>");
            htmlBody.AppendLine("</table>");
            htmlBody.AppendLine("<p>&nbsp;</p>");

            // Footer with responsive logo
            htmlBody.AppendLine("<p>");
            htmlBody.AppendLine("<img class='logo-image' alt=\"\" src=\"https://i.ibb.co/H7g8BSz/LTASLogo-small.png\" /> ");
            htmlBody.AppendLine("<small><var><code><strong>LTAS Billing Automations</strong></code></var></small>");
            htmlBody.AppendLine("</p>");
            htmlBody.AppendLine("<p><small><var><code><strong>[FYI: this job runs once an hour]</strong></code></var></small></p>");
            htmlBody.AppendLine("</div>");
            htmlBody.AppendLine("</body></html>");

            return htmlBody;
        }
        public static StringBuilder DuplicateMattersEmailBody(StringBuilder htmlBody, List<EddsMatters> duplicateMatters)
        {
            htmlBody.AppendLine("<html><body style='font-family: Arial, sans-serif;'>");
            htmlBody.AppendLine("<h3 style=\"color: #d9534f;\">Duplicate matter numbers:</h3>");

            var groupedDuplicates = duplicateMatters
                .GroupBy(c => c.EddsMatterNumber)
                .OrderBy(g => g.Key);

            foreach (var group in groupedDuplicates)
            {
                htmlBody.AppendLine("<div style='margin-bottom: 20px; background-color: #f9f9f9; padding: 15px; border-radius: 5px;'>");
                htmlBody.AppendLine($"<h3 style='color: #333; margin-top: 0;'>Matter Number: {group.Key}</h3>");

                foreach (var client in group)
                {
                    htmlBody.AppendLine("<div style='margin-left: 20px; padding: 10px; background-color: white; border-left: 4px solid #5bc0de; margin-bottom: 10px;'>");
                    htmlBody.AppendLine($"<div>ArtifactID:{client.EddsMatterArtifactId}</div>");
                    htmlBody.AppendLine($"<div>Name:{client.EddsMatterName}</div>");
                    htmlBody.AppendLine($"<div>Created By:{client.EddsMatterCreatedByFirstName}</div>");
                    htmlBody.AppendLine("</div>");
                }
                htmlBody.AppendLine("</div>");
            }
            htmlBody.AppendLine("</body></html>");            
            return htmlBody;
        }
        public static StringBuilder DuplicateClientEmailBody(StringBuilder htmlBody, List<EddsClients> duplicateClients)
        {
            htmlBody.AppendLine("<html><body style='font-family: Arial, sans-serif;'>");
            htmlBody.AppendLine("<h3 style=\"color: #d9534f;\">Duplicate client numbers:</h3>");

            var groupedDuplicates = duplicateClients
                .GroupBy(c => c.EddsClientNumber)
                .OrderBy(g => g.Key);

            foreach (var group in groupedDuplicates)
            {
                htmlBody.AppendLine("<div style='margin-bottom: 20px; background-color: #f9f9f9; padding: 15px; border-radius: 5px;'>");
                htmlBody.AppendLine($"<h3 style='color: #333; margin-top: 0;'>Client Number: {group.Key}</h3>");

                foreach (var client in group)
                {
                    htmlBody.AppendLine("<div style='margin-left: 20px; padding: 10px; background-color: white; border-left: 4px solid #5bc0de; margin-bottom: 10px;'>");
                    htmlBody.AppendLine($"<div>ArtifactID:{client.EddsClientArtifactId}</div>");
                    htmlBody.AppendLine($"<div>Name:{client.EddsClientName}</div>");
                    htmlBody.AppendLine($"<div>Created By:{client.EddsClientCreatedByFirstName}</div>");
                    htmlBody.AppendLine("</div>");
                }
                htmlBody.AppendLine("</div>");
            }
            htmlBody.AppendLine("</body></html>");
            return htmlBody;
        }
        public static StringBuilder DataOutput(StringBuilder htmlBody, EddsClients clients)
        {

            return htmlBody;
        }
        public class Email
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
            public static void SentInvalidClientNumber(IInstanceSettingsBundle instanceSettingsBundle, StringBuilder htmlBody, string emailAddress)
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

                emailMessage.To.Add(emailAddress.Contains("relativity.serviceaccount@kcura.com")
                ? "damienyoung@quinnemanuel.com"
                : emailAddress);

                emailMessage.CC.Add("damienyoung@quinnemanuel.com");
                emailMessage.ReplyToList.Add(new MailAddress("damienyoung@quinnemanuel.com", "Damien Young"));

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
            public static void SendNewClientsReporting(IInstanceSettingsBundle instanceSettingsBundle, StringBuilder htmlBody, string emailAddress)
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
                emailMessage.ReplyToList.Add(new MailAddress("damienyoung@quinnemanuel.com", "Damien Young"));

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
            public static void SentInvalidMatterNumber(IInstanceSettingsBundle instanceSettingsBundle, StringBuilder htmlBody, string emailAddress)
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
                    Subject = $"{smtpEnvironmentValue.Split('-')[1].Split('.')[0].ToUpper()} - Invalid Matter Number",
                    Body = htmlBody.ToString(),
                    IsBodyHtml = true
                };

                emailMessage.To.Add(emailAddress.Contains("relativity.serviceaccount@kcura.com")
                ? "damienyoung@quinnemanuel.com"
                : emailAddress);

                emailMessage.CC.Add("damienyoung@quinnemanuel.com");
                emailMessage.ReplyToList.Add(new MailAddress("damienyoung@quinnemanuel.com", "Damien Young"));

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
            public static void SendNewMattersReporting(IInstanceSettingsBundle instanceSettingsBundle, StringBuilder htmlBody)
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
                    Subject = $"{smtpEnvironmentValue.Split('-')[1].Split('.')[0].ToUpper()} - New Matters To Be Created",
                    Body = htmlBody.ToString(),
                    IsBodyHtml = true
                };                
                emailMessage.To.Add("damienyoung@quinnemanuel.com");
                emailMessage.To.Add("calaustin@quinnemanuel.com");
                emailMessage.ReplyToList.Add(new MailAddress("damienyoung@quinnemanuel.com", "Damien Young"));

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
            public static void SendDebugEmail(IInstanceSettingsBundle instanceSettingsBundle, StringBuilder htmlBody, string emailSubject)
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
                    Subject = $"{smtpEnvironmentValue.Split('-')[1].Split('.')[0].ToUpper()} - {emailSubject}",
                    Body = htmlBody.ToString(),
                    IsBodyHtml = true
                };
                emailMessage.To.Add("damienyoung@quinnemanuel.com");

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
}
