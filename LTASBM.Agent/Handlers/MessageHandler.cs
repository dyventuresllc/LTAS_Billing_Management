using LTASBM.Agent.Models;
using Relativity.API;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.IO;
using System.Data;

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

        public static StringBuilder InvalidWorkspaceEmailBody(StringBuilder htmlBody, List<BillingWorkspaces> billingWorkspaces)
        {
            htmlBody.AppendLine("<!DOCTYPE html>");
            htmlBody.AppendLine("<html>");
            htmlBody.AppendLine("<head>");
            htmlBody.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            htmlBody.AppendLine("<style>");
            htmlBody.AppendLine("@media screen and (max-width: 600px) {");
            htmlBody.AppendLine("  table { width: 100% !important; font-size: 14px; }");
            htmlBody.AppendLine("  img { max-width: 100% !important; height: auto !important; }");
            htmlBody.AppendLine("  th, td { display: block; width: 100%; box-sizing: border-box; }");
            htmlBody.AppendLine("  th { text-align: left; }");
            htmlBody.AppendLine("}");
            htmlBody.AppendLine(".logo-image { width: 20px; height: 10px; vertical-align: middle; }");
            htmlBody.AppendLine(".email-table { max-width: 800px; width: 100%; margin: 0 auto; }");
            htmlBody.AppendLine(".invalid-value { color: red; font-weight: bold; }");
            htmlBody.AppendLine("</style>");
            htmlBody.AppendLine("</head>");
            htmlBody.AppendLine("<body style='font-family: Arial, sans-serif;'>");
            htmlBody.AppendLine("<div class='email-table'>");

            htmlBody.AppendLine("<p>The following workspaces have an invalid Workspace ArtifactId or EDDS workspace ArtifactId:</p>");

            // Responsive table
            htmlBody.AppendLine("<table border=\"1\" bordercolor=\"#ccc\" cellpadding=\"5\" cellspacing=\"0\" style=\"border-collapse:collapse; max-width: 800px; width: 100%; margin: 0 auto;\">");
            htmlBody.AppendLine("\t<tbody>");
            htmlBody.AppendLine("\t\t<tr style=\"background-color: #f2f2f2;\">");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;\">Billing Workspace ArtifactId</th>");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;\">Billing Workspace EDDS ArtifactID</th>");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;white-space: nowrap;\">Workspace Name</th>");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;white-space: nowrap;\">Created By</th>");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;white-space: nowrap;\">Created On</th>");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;white-space: nowrap;\">Matter ArtifactId</th>");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;white-space: nowrap;\">Analyst</th>");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;white-space: nowrap;\">Case Team</th>");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;white-space: nowrap;\">Status</th>");
            htmlBody.AppendLine("\t\t</tr>");

            foreach (var record in billingWorkspaces)
            {
                htmlBody.AppendLine("\t\t<tr>");
                // Highlight invalid values in red
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;{(record.BillingWorkspaceArtifactId == 0 ? " color: red; font-weight: bold;" : "")}'>{record.BillingWorkspaceArtifactId}</td>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;{(record.BillingWorkspaceEddsArtifactId == 0 ? " color: red; font-weight: bold;" : "")}'>{record.BillingWorkspaceEddsArtifactId}</td>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;white-space: nowrap;'>{HttpUtility.HtmlEncode(record.BillingWorkspaceName)}</td>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;white-space: nowrap;'>{HttpUtility.HtmlEncode(record.BillingWorkspaceCreatedBy)}</td>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;white-space: nowrap;'>{record.BillingWorkspaceCreatedOn:yyyy-MM-dd HH:mm}</td>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;white-space: nowrap;'>{record.BillingWorkspaceMatterArtifactId}</td>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;white-space: nowrap;'>{HttpUtility.HtmlEncode(record.BillingWorkspaceAnalyst)}</td>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;white-space: nowrap;'>{HttpUtility.HtmlEncode(record.BillingWorkspaceCaseTeam)}</td>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;white-space: nowrap;'>{HttpUtility.HtmlEncode(record.BillingStatusName)}</td>");
                htmlBody.AppendLine("\t\t</tr>");
            }

            htmlBody.AppendLine("\t</tbody>");
            htmlBody.AppendLine("</table>");
            htmlBody.AppendLine("<p>&nbsp;</p>");

            // Footer with responsive logo
            htmlBody.AppendLine("<p style='margin-top: 20px;'>");
            htmlBody.AppendLine("<img class='logo-image' alt=\"LTAS Logo\" src=\"https://i.ibb.co/H7g8BSz/LTASLogo-small.png\" /> ");
            htmlBody.AppendLine("<span style='font-size: 12px; font-weight: bold;'>LTAS Billing Automations</span>");
            htmlBody.AppendLine("</p>");

            htmlBody.AppendLine("<p style='font-size: 12px; font-weight: bold; color: #666;'>[FYI: this job runs once an hour]</p>");

            htmlBody.AppendLine("</div>");
            htmlBody.AppendLine("</body></html>");

            return htmlBody;
        }

        public static StringBuilder DuplicateWorkspacesEmailBody(StringBuilder htmlBody, List<BillingWorkspaces> duplicateWorkspaces)
        {
            htmlBody.AppendLine("<html><body style='font-family: Arial, sans-serif;'>");
            htmlBody.AppendLine("<h3 style=\"color: #d9534f;\">Duplicate EDDS Workspace ArtifactIDs:</h3>");

            var groupedDuplicates = duplicateWorkspaces
                .GroupBy(w => w.BillingWorkspaceEddsArtifactId)
                .OrderBy(g => g.Key);

            foreach (var group in groupedDuplicates)
            {
                htmlBody.AppendLine("<div style='margin-bottom: 20px; background-color: #f9f9f9; padding: 15px; border-radius: 5px;'>");
                htmlBody.AppendLine($"<h3 style='color: #333; margin-top: 0;'>EDDS Workspace ArtifactId: {group.Key}</h3>");

                foreach (var workspace in group)
                {
                    htmlBody.AppendLine("<div style='margin-left: 20px; padding: 10px; background-color: white; border-left: 4px solid #5bc0de; margin-bottom: 10px;'>");
                    htmlBody.AppendLine($"<div>Billing Workspace ArtifactID: {workspace.BillingWorkspaceArtifactId}</div>");
                    htmlBody.AppendLine($"<div>Workspace Name: {workspace.BillingWorkspaceName}</div>");
                    htmlBody.AppendLine($"<div>Created By: {workspace.BillingWorkspaceCreatedBy}</div>");
                    htmlBody.AppendLine($"<div>Created On: {workspace.BillingWorkspaceCreatedOn:yyyy-MM-dd HH:mm}</div>");
                    htmlBody.AppendLine($"<div>Matter ArtifactId: {workspace.BillingWorkspaceMatterArtifactId}</div>");
                    htmlBody.AppendLine($"<div>Status: {workspace.BillingStatusName}</div>");
                    htmlBody.AppendLine("</div>");
                }
                htmlBody.AppendLine("</div>");
            }
            htmlBody.AppendLine("</body></html>");
            return htmlBody;
        }

        public static StringBuilder NewWorkspacesEmailBody(StringBuilder htmlBody, List<EddsWorkspaces> workspaces)
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

            htmlBody.AppendLine("<p>The following workspaces exist in EDDS but are missing from the Billing workspace, they will be created:</p>");

            // Responsive table
            htmlBody.AppendLine("<table border=\"1\" bordercolor=\"#ccc\" cellpadding=\"5\" cellspacing=\"0\" style=\"border-collapse:collapse; max-width: 600px; width: 100%; margin: 0 auto;\">");
            htmlBody.AppendLine("\t<tbody>");
            htmlBody.AppendLine("\t\t<tr style=\"background-color: #f2f2f2;\">");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;white-space: nowrap;\">Workspace Name</th>");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;white-space: nowrap;\">EDDS ArtifactId</th>");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;white-space: nowrap;\">Created By</th>");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;white-space: nowrap;\">Created On</th>");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;white-space: nowrap;\">Matter ArtifactId</th>");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;white-space: nowrap;\">Status</th>");
            htmlBody.AppendLine("\t\t\t<th style=\"padding: 8px;\">Link</th>");
            htmlBody.AppendLine("\t\t</tr>");

            foreach (var record in workspaces)
            {
                htmlBody.AppendLine("\t\t<tr>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;white-space: nowrap;'>{record.EddsWorkspaceName}</td>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;white-space: nowrap;'>{record.EddsWorkspaceArtifactId}</td>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;white-space: nowrap;'>{record.EddsWorkspaceCreatedBy}</td>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;white-space: nowrap;'>{record.EddsWorkspaceCreatedOn:yyyy-MM-dd HH:mm}</td>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;white-space: nowrap;'>{record.EddsWorkspaceMatterArtifactId}</td>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;white-space: nowrap;'>{record.EddsWorkspaceStatusName}</td>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;'><a href=\"https://qe-us.relativity.one/Relativity/RelativityInternal.aspx?AppID=-1&ArtifactTypeID=8&ArtifactID={record.EddsWorkspaceArtifactId}&Mode=Forms&FormMode=view&LayoutID=null&SelectedTab=null\">View</a></td>");
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

        public static StringBuilder DataSyncNotificationEmailBody(
            StringBuilder htmlBody,
            IEnumerable<(int BillingArtifactID, string EddsValue)> updates, string objectName, string fieldName)
        {
            htmlBody.AppendLine($"The following {objectName} names need to be updated in the Billing System:");
            htmlBody.AppendLine("<br><br>");            
            htmlBody.AppendLine("<table border='1'>");
            htmlBody.AppendLine($"<tr><th>Billing {objectName} ArtifactID</th><th>New {fieldName}</th></tr>");

            foreach (var (BillingArtifactID, EddsValue) in updates)
            {
                htmlBody.AppendLine("<tr>");
                htmlBody.AppendLine($"<td>{BillingArtifactID}</td>");
                htmlBody.AppendLine($"<td>{EddsValue}</td>");
                htmlBody.AppendLine("</tr>");
            }

            htmlBody.AppendLine("</table>");
            return htmlBody;
        }

        public static StringBuilder SendInvoiceEmailBody(StringBuilder htmlBody, DataTable costdt, 
            string contactFirstName, string emailToList, string emailCcList, int workspaceCount)
        {
            // Helper functions
            T GetFieldValue<T>(DataRow row, string fieldName, T defaultValue = default)
            {
                try
                {
                    if (row == null || row[fieldName] == DBNull.Value)
                        return defaultValue;
                    if (typeof(T) == typeof(bool))
                        return (T)(object)(row[fieldName] != DBNull.Value && Convert.ToBoolean(row[fieldName]));
                    if (typeof(T) == typeof(decimal))
                        return (T)(object)(row[fieldName] != DBNull.Value ? Convert.ToDecimal(row[fieldName]) : 0m);
                    return (T)Convert.ChangeType(row[fieldName], typeof(T));
                }
                catch (Exception)
                {
                    return defaultValue;
                }
            }

            int GetCostCodeNumber(DataRow row)
            {
                var costCode = GetFieldValue<string>(row, "CostCode", "");
                if (string.IsNullOrEmpty(costCode)) return 0;
                var numericPart = new string(costCode.Where(char.IsDigit).ToArray());
                return int.TryParse(numericPart, out int result) ? result : 0;
            }

            // Set workspace text based on count
            var workspaceText = workspaceCount == 1 ? "workspace" : "workspaces";

            // Group data by section for Monthly Recurring Fees
            var monthlyFeeRows = costdt.AsEnumerable()
                .Where(row =>
                {
                    var code = GetCostCodeNumber(row);
                    return code == 3200 ||                    // User Fees
                           (code >= 3230 && code <= 3232) ||  // Review Hosting
                           (code >= 3220 && code <= 3222);    // Repository Hosting
                })
                .ToList();

            // Get specific fee rows
            var userFeeRows = monthlyFeeRows.Where(row => GetCostCodeNumber(row) == 3200).ToList();
            var reviewHostingRows = monthlyFeeRows.Where(row =>
            {
                var code = GetCostCodeNumber(row);
                return code >= 3230 && code <= 3232;
            }).ToList();
            var repoHostingRows = monthlyFeeRows.Where(row =>
            {
                var code = GetCostCodeNumber(row);
                return code >= 3220 && code <= 3222;
            }).ToList();

            // All other charges are one-time monthly fees
            var oneTimeFeeRows = costdt.AsEnumerable()
                .Where(row =>
                {
                    var code = GetCostCodeNumber(row);
                    return !(code == 3200 ||                    // Not User Fees
                            (code >= 3230 && code <= 3232) ||   // Not Review Hosting
                            (code >= 3220 && code <= 3222));    // Not Repository Hosting
                })
                .OrderBy(row => GetFieldValue<string>(row, "CostCodeDescription"))
                .ToList();

            // Start HTML
            htmlBody.AppendLine(@"<!DOCTYPE html>
<html>
<head>
    <style>
        body { 
            font-family: Arial, sans-serif; 
            font-size: 13px; 
            margin: 20px;
            max-width: 1200px;
            margin: 0 auto;
        }
        .content-wrapper {
            padding: 20px;
        }
        table { 
            width: auto;
            min-width: 600px;
            max-width: 800px;
            border-collapse: collapse; 
            margin: 15px 0; 
        }
        th { 
            text-align: left;
            padding: 6px 12px;
            border-bottom: 1px solid #000;
            white-space: nowrap;
        }
        td { 
            padding: 6px 12px;
            border-bottom: 1px solid #ddd;
        }
        .amount-cell { 
            text-align: right;
            white-space: nowrap;
        }
        .total-row {
            font-weight: bold;
            border-top: 1px solid #000;
        }
        .section-header {
            font-weight: bold;
            text-decoration: underline;
            margin-top: 20px;
            padding-left: 12px;
        }
        .fee-section {
            margin: 20px 0;
            padding: 15px;
            border-radius: 4px;
        }
        .monthly-fees {
            background-color: #f8f9fa;
        }
        .one-time-fees {
            background-color: #fff8e1;
            margin-top: 30px;
        }
        .notes-section {
            margin: 15px 0;
            padding-left: 12px;
        }
    </style>
</head>
<body>
<div class='content-wrapper'>");

            // Add email headers
            htmlBody.AppendLine($@"<p>To: {HttpUtility.HtmlEncode(emailToList)}</p>");
            if (!string.IsNullOrWhiteSpace(emailCcList))
            {
                htmlBody.AppendLine($@"<p>CC: {HttpUtility.HtmlEncode(emailCcList)}</p>");
            }

            // Dear line
            htmlBody.AppendLine($@"<p>Dear {HttpUtility.HtmlEncode(!string.IsNullOrWhiteSpace(contactFirstName) ? contactFirstName : "_____")},</p>");

            // Introduction text
            htmlBody.AppendLine($@"<p>The following outlines the monthly recurring user and hosting fees for your Relativity {workspaceText}. These charges are updated regularly to reflect changes in data volume and the number of users added or removed.</p>");

            // Monthly Recurring Fees Section
            decimal totalMonthlyFees = 0;
            var hasMonthlyDiscounts = false;

            htmlBody.AppendLine(@"<div class='fee-section monthly-fees'>");
            htmlBody.AppendLine(@"<table>
        <thead>
            <tr>
                <th>Description</th>
                <th style='text-align: right;'>Quantity</th>
                <th style='text-align: right;'>Rate</th>
                <th style='text-align: right;'>Amount</th>
            </tr>
        </thead>
        <tbody>");

            // User Fees
            if (userFeeRows.Any())
            {
                var userCount = userFeeRows.Sum(row => GetFieldValue<decimal>(row, "Quantity"));
                var userRate = GetFieldValue<decimal>(userFeeRows.First(), "FinalRate");
                var userAmount = Math.Ceiling(Math.Round(userFeeRows.Sum(row => GetFieldValue<decimal>(row, "BilledAmount")), 2));
                var hasDiscount = GetFieldValue<bool>(userFeeRows.First(), "HasOverride");
                hasMonthlyDiscounts |= hasDiscount;
                totalMonthlyFees += userAmount;

                htmlBody.AppendLine($@"<tr>
            <td>User Fees</td>
            <td class='amount-cell'>{userCount}</td>
            <td class='amount-cell'>${userRate:N2}{(hasDiscount ? "*" : "")}</td>
            <td class='amount-cell'>${userAmount:N2}</td>
        </tr>");
            }

            // Review Hosting
            if (reviewHostingRows.Any())
            {
                var reviewGB = Math.Ceiling(reviewHostingRows.Sum(row => GetFieldValue<decimal>(row, "Quantity")));
                var reviewRate = GetFieldValue<decimal>(reviewHostingRows.First(), "FinalRate");
                var reviewAmount = Math.Ceiling(Math.Round(reviewHostingRows.Sum(row => GetFieldValue<decimal>(row, "BilledAmount")), 2));
                var hasDiscount = GetFieldValue<bool>(reviewHostingRows.First(), "HasOverride");
                hasMonthlyDiscounts |= hasDiscount;
                totalMonthlyFees += reviewAmount;

                htmlBody.AppendLine($@"<tr>
            <td>Review Hosting (Per GB)</td>
            <td class='amount-cell'>{reviewGB:N0}</td>
            <td class='amount-cell'>${reviewRate:N2}{(hasDiscount ? "*" : "")}</td>
            <td class='amount-cell'>${reviewAmount:N2}</td>
        </tr>");
            }

            // Repository Hosting
            if (repoHostingRows.Any())
            {
                var repoGB = Math.Ceiling(repoHostingRows.Sum(row => GetFieldValue<decimal>(row, "Quantity")));
                var repoRate = GetFieldValue<decimal>(repoHostingRows.First(), "FinalRate");
                var repoAmount = Math.Ceiling(Math.Round(repoHostingRows.Sum(row => GetFieldValue<decimal>(row, "BilledAmount")), 2));
                var hasDiscount = GetFieldValue<bool>(repoHostingRows.First(), "HasOverride");
                hasMonthlyDiscounts |= hasDiscount;
                totalMonthlyFees += repoAmount;

                htmlBody.AppendLine($@"<tr>
            <td>Repository (ECA) Hosting (Per GB)</td>
            <td class='amount-cell'>{repoGB:N0}</td>
            <td class='amount-cell'>${repoRate:N2}{(hasDiscount ? "*" : "")}</td>
            <td class='amount-cell'>${repoAmount:N2}</td>
        </tr>");
            }

            // Total Monthly Fees
            htmlBody.AppendLine($@"<tr class='total-row'>
        <td colspan='3' style='text-align: right;'>Total Monthly Fees:</td>
        <td class='amount-cell'>${totalMonthlyFees:N2}</td>
    </tr>");

            htmlBody.AppendLine("</tbody></table>");

            // Add notes within monthly fees section
            htmlBody.AppendLine(@"<div class='notes-section'>");
            if (userFeeRows.Any())
            {
                htmlBody.AppendLine($@"<p style='text-indent: 2em;'><strong>User Fees:</strong> If you wish to deactivate any user accounts, we must receive your request before close of business 
                    on {GetLastMondayOfMonth():MMMM d, yyyy} to avoid charges in {GetNextMonth():MMMM}. The attached user report lists the active users on this matter.</p>");
            }

            if (reviewHostingRows.Any() || repoHostingRows.Any())
            {
                htmlBody.AppendLine($@"<p style='text-indent: 2em;'><strong>Hosting Fees:</strong> Hosting fees will continue each month unless the database is deleted or archived. 
                    If this case is no longer active (or is expected to have a long period of inactivity), please contact us to get an estimate to archive. 
                    Please note that larger cases may take several weeks to archive during which time the fees will continue to accrue.</p>");

                //htmlBody.AppendLine($@"<p style='text-indent: 2em; margin: 0;'><strong>Hosting Fees:</strong> Hosting fees will continue each month unless the database is deleted or archived. 
                //    If this case is no longer active (or is expected to have a long period of inactivity), please contact us to get an estimate to archive.<br/>
                //    <span style='margin-left: 2em;'>Please note that larger cases may take several weeks to archive during which time the fees will continue to accrue.</span></p>");



                htmlBody.AppendLine($@"<p style='text-indent: 2em; margin: 0; max-width: 1000px;'><strong>Hosting Fees:</strong> Hosting fees will continue each month unless the database is deleted or archived. 
    If this case is no longer active (or is expected to have a long period of inactivity), please contact us to get an estimate to archive.<br/><span style='margin-left: 12em;'>Please note that larger cases may take several weeks to archive during which time the fees will continue to accrue.</span></p>");

            }
            htmlBody.AppendLine("</div></div>");

            // Initialize one-time discounts flag before the if block
            var hasOneTimeDiscounts = false;

            // One-Time Monthly Fees section
            if (oneTimeFeeRows.Any())
            {
                htmlBody.AppendLine("<br>");
                htmlBody.AppendLine(@"<div class='fee-section one-time-fees'>");
                htmlBody.AppendLine($@"<p>The following table outlines the one-time monthly fees associated with your Relativity {workspaceText}. 
        These charges cover processing, imaging, and translation fees (where applicable) to support the needs of your matter.</p>");

                htmlBody.AppendLine(@"<table>
            <thead>
                <tr>
                    <th>Description</th>
                    <th style='text-align: right;'>Quantity</th>
                    <th style='text-align: right;'>Rate</th>
                    <th style='text-align: right;'>Amount</th>
                </tr>
            </thead>
            <tbody>");

                foreach (var row in oneTimeFeeRows)
                {
                    var description = HttpUtility.HtmlEncode(GetFieldValue<string>(row, "CostCodeDescription"));
                    var quantity = GetFieldValue<decimal>(row, "Quantity");
                    var rate = GetFieldValue<decimal>(row, "FinalRate");
                    var amount = Math.Ceiling(Math.Round(GetFieldValue<decimal>(row, "BilledAmount"), 2));
                    var hasDiscount = GetFieldValue<bool>(row, "HasOverride");
                    hasOneTimeDiscounts |= hasDiscount;

                    htmlBody.AppendLine($@"<tr>
                <td>{description}</td>
                <td class='amount-cell'>{quantity:N0}</td>
                <td class='amount-cell'>${rate:N2}{(hasDiscount ? "*" : "")}</td>
                <td class='amount-cell'>${amount:N2}</td>
            </tr>");
                }

                var totalOneTime = Math.Ceiling(Math.Round(oneTimeFeeRows.Sum(row => GetFieldValue<decimal>(row, "BilledAmount")), 2));
                htmlBody.AppendLine($@"<tr class='total-row'>
            <td colspan='3' style='text-align: right;'>Total One-Time Monthly Fees:</td>
            <td class='amount-cell'>${totalOneTime:N2}</td>
        </tr>");

                htmlBody.AppendLine("</tbody></table>");
                htmlBody.AppendLine("</div>");
            }

            // Matter Total (outside of both sections)
            htmlBody.AppendLine(@"<div style='margin-top: 30px; padding-left: 12px;'>");
            var grandTotal = totalMonthlyFees;
            if (oneTimeFeeRows.Any())
            {
                grandTotal += Math.Ceiling(Math.Round(oneTimeFeeRows.Sum(row => GetFieldValue<decimal>(row, "BilledAmount")), 2));
            }
            htmlBody.AppendLine($@"<p><strong>Matter Total: ${grandTotal:N2}</strong></p>");

            // Add discount note if any discounts exist
            var hasAnyDiscounts = hasMonthlyDiscounts || (oneTimeFeeRows.Any() && hasOneTimeDiscounts);
            if (hasAnyDiscounts)
            {
                htmlBody.AppendLine(@"<div class='note' style='margin-top: 15px; font-size: 12px; color: #666;'>
            * Rate is discounted
        </div>");
            }

            // Thank you note
            htmlBody.AppendLine(@"<p>Thank you for choosing LTAS.</p>");            
            htmlBody.AppendLine("<p><img class='logo-image' alt=\"\" src=\"https://i.ibb.co/DgCVxtkw/ltas-smaller.png\"/></p>");
            htmlBody.AppendLine("</div></body></html>");
            
            return htmlBody;
        }

        private static DateTime GetLastMondayOfMonth()
        {
            var today = DateTime.Today;
            var lastDay = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

            while (lastDay.DayOfWeek != DayOfWeek.Monday)
            {
                lastDay = lastDay.AddDays(-1);
            }

            return lastDay;
        }

        private static DateTime GetNextMonth()
        {
            var today = DateTime.Today;
            return new DateTime(today.Year, today.Month, 1).AddMonths(1);
        }

        public class Email
        {
            static string smtpPasswordValue;
            static int smtpPortValue;
            static string smtpUserValue;
            static string smtpServerValue;
            static string smtpEnvironmentValue;
            static string adminEmailAddress;
            static string teamEmailAddresses;
            static string supportEmailAddress;
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
                    case "AdminEmailAddress":
                        var singleSettingValueAdmin = instanceSettingsBundle.GetStringAsync(smtpInstanceSettingSingle.Section, smtpInstanceSettingSingle.Name);
                        adminEmailAddress = singleSettingValueAdmin.Result;
                        break;
                    case "TeamEmailAddresses":
                        var singleSettingValueTeam = instanceSettingsBundle.GetStringAsync(smtpInstanceSettingSingle.Section, smtpInstanceSettingSingle.Name);
                        teamEmailAddresses = singleSettingValueTeam.Result;
                        break;
                    case "AnalystCaseTeamUpdates":
                        var singleSettingValueSupport = instanceSettingsBundle.GetStringAsync(smtpInstanceSettingSingle.Section, smtpInstanceSettingSingle.Name);
                        supportEmailAddress = singleSettingValueSupport.Result;
                        break;
                }
            }

            public static async Task SentInvalidClientNumber(IInstanceSettingsBundle instanceSettingsBundle, StringBuilder htmlBody, string emailAddress)
            {
                SMTPSetting smtpPassword = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPPassword" };
                SMTPSetting smtpPort = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPPort" };
                SMTPSetting smtpServer = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPServer" };
                SMTPSetting smtpUser = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPUserName" };
                SMTPSetting smtpEnvironment = new SMTPSetting { Section = "Relativity.Core", Name = "RelativityInstanceURL" };
                SMTPSetting adminEmailSetting = new SMTPSetting { Section = "LTAS Billing Management", Name = "AdminEmailAddress" };                

                List<SMTPSetting> smtpSettings = new List<SMTPSetting> { smtpPort, smtpServer, smtpUser, smtpPassword, smtpEnvironment, adminEmailSetting };

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

                emailMessage.To.Add(
                   emailAddress.IndexOf("kcura.com", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   emailAddress.IndexOf("relativity.com", StringComparison.OrdinalIgnoreCase) >= 0
                       ? adminEmailAddress
                       : emailAddress);

                emailMessage.CC.Add(adminEmailAddress);
                emailMessage.ReplyToList.Add(new MailAddress(adminEmailAddress));

                using (var smtpClient = new SmtpClient())
                {
                    smtpClient.Host = smtpServerValue;
                    smtpClient.Port = smtpPortValue;
                    smtpClient.EnableSsl = true;
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtpClient.DeliveryFormat = SmtpDeliveryFormat.SevenBit;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(smtpUserValue, smtpPasswordValue);
                    await smtpClient.SendMailAsync(emailMessage);
                }
            }

            public static async Task SendNewClientsReportingAsync(IInstanceSettingsBundle instanceSettingsBundle, StringBuilder htmlBody)
            {
                SMTPSetting smtpPassword = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPPassword" };
                SMTPSetting smtpPort = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPPort" };
                SMTPSetting smtpServer = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPServer" };
                SMTPSetting smtpUser = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPUserName" };
                SMTPSetting smtpEnvironment = new SMTPSetting { Section = "Relativity.Core", Name = "RelativityInstanceURL" };
                SMTPSetting adminEmailSetting = new SMTPSetting { Section = "LTAS Billing Management", Name = "AdminEmailAddress" };
                
                List<SMTPSetting> smtpSettings = new List<SMTPSetting> { smtpPort, smtpServer, smtpUser, smtpPassword, smtpEnvironment, adminEmailSetting };

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
                emailMessage.To.Add(adminEmailAddress);
                emailMessage.ReplyToList.Add(new MailAddress(adminEmailAddress));

                using (var smtpClient = new SmtpClient())
                {
                    smtpClient.Host = smtpServerValue;
                    smtpClient.Port = smtpPortValue;
                    smtpClient.EnableSsl = true;
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtpClient.DeliveryFormat = SmtpDeliveryFormat.SevenBit;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(smtpUserValue, smtpPasswordValue);
                    await smtpClient.SendMailAsync(emailMessage);
                }                
            }

            public static async Task SentInvalidMatterNumberAsync(IInstanceSettingsBundle instanceSettingsBundle, StringBuilder htmlBody, string emailAddress)
            {
                SMTPSetting smtpPassword = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPPassword" };
                SMTPSetting smtpPort = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPPort" };
                SMTPSetting smtpServer = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPServer" };
                SMTPSetting smtpUser = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPUserName" };
                SMTPSetting smtpEnvironment = new SMTPSetting { Section = "Relativity.Core", Name = "RelativityInstanceURL" };
                SMTPSetting adminEmailSetting = new SMTPSetting { Section = "LTAS Billing Management", Name = "AdminEmailAddress" };
                
                List<SMTPSetting> smtpSettings = new List<SMTPSetting> { smtpPort, smtpServer, smtpUser, smtpPassword, smtpEnvironment, adminEmailSetting };

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
                ? adminEmailAddress
                : emailAddress);

                emailMessage.CC.Add(adminEmailAddress);
                emailMessage.ReplyToList.Add(new MailAddress(adminEmailAddress));

                using (var smtpClient = new SmtpClient())
                {
                    smtpClient.Host = smtpServerValue;
                    smtpClient.Port = smtpPortValue;
                    smtpClient.EnableSsl = true;
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtpClient.DeliveryFormat = SmtpDeliveryFormat.SevenBit;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(smtpUserValue, smtpPasswordValue);
                    await smtpClient.SendMailAsync(emailMessage);
                }
            }

            public static async Task SendNewMattersReportingAsync(IInstanceSettingsBundle instanceSettingsBundle, StringBuilder htmlBody)
            {
                SMTPSetting smtpPassword = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPPassword" };
                SMTPSetting smtpPort = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPPort" };
                SMTPSetting smtpServer = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPServer" };
                SMTPSetting smtpUser = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPUserName" };
                SMTPSetting smtpEnvironment = new SMTPSetting { Section = "Relativity.Core", Name = "RelativityInstanceURL" };
                SMTPSetting adminEmailSetting = new SMTPSetting { Section = "LTAS Billing Management", Name = "AdminEmailAddress" };
                SMTPSetting teamEmailSetting = new SMTPSetting { Section = "LTAS Billing Management", Name = "TeamEmailAddresses" };
                List<SMTPSetting> smtpSettings = new List<SMTPSetting> { smtpPort, smtpServer, smtpUser, smtpPassword, smtpEnvironment, adminEmailSetting, teamEmailSetting };

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

                if (!string.IsNullOrWhiteSpace(teamEmailAddresses))
                {
                    var emails = teamEmailAddresses.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(email => email.Trim())
                        .Where(email => !string.IsNullOrWhiteSpace(email));

                    foreach (var email in emails)
                    {
                        emailMessage.To.Add(email);
                    }
                }

                emailMessage.ReplyToList.Add(new MailAddress(adminEmailAddress));

                using (var smtpClient = new SmtpClient())
                {
                    smtpClient.Host = smtpServerValue;
                    smtpClient.Port = smtpPortValue;
                    smtpClient.EnableSsl = true;
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtpClient.DeliveryFormat = SmtpDeliveryFormat.SevenBit;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(smtpUserValue, smtpPasswordValue);
                    await smtpClient.SendMailAsync(emailMessage);
                }
            }

            public static async Task SendInternalNotificationAsync(IInstanceSettingsBundle instanceSettingsBundle, StringBuilder htmlBody, string emailSubject)
            {
                SMTPSetting smtpPassword = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPPassword" };
                SMTPSetting smtpPort = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPPort" };
                SMTPSetting smtpServer = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPServer" };
                SMTPSetting smtpUser = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPUserName" };
                SMTPSetting smtpEnvironment = new SMTPSetting { Section = "Relativity.Core", Name = "RelativityInstanceURL" };
                SMTPSetting adminEmailSetting = new SMTPSetting { Section = "LTAS Billing Management", Name = "AdminEmailAddress" };
                
                List<SMTPSetting> smtpSettings = new List<SMTPSetting> { smtpPort, smtpServer, smtpUser, smtpPassword, smtpEnvironment, adminEmailSetting };

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
                emailMessage.To.Add(adminEmailAddress);
                emailMessage.ReplyToList.Add(new MailAddress(adminEmailAddress));

                using (var smtpClient = new SmtpClient())
                {
                    smtpClient.Host = smtpServerValue;
                    smtpClient.Port = smtpPortValue;
                    smtpClient.EnableSsl = true;
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtpClient.DeliveryFormat = SmtpDeliveryFormat.SevenBit;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(smtpUserValue, smtpPasswordValue);
                    await smtpClient.SendMailAsync(emailMessage);
                }
            }

            public static async Task SendInternalNotificationWAttachmentAsync(IInstanceSettingsBundle instanceSettingsBundle, StringBuilder htmlBody, string emailSubject, DataTable dt, string attachmentName)
            {
                SMTPSetting smtpPassword = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPPassword" };
                SMTPSetting smtpPort = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPPort" };
                SMTPSetting smtpServer = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPServer" };
                SMTPSetting smtpUser = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPUserName" };
                SMTPSetting smtpEnvironment = new SMTPSetting { Section = "Relativity.Core", Name = "RelativityInstanceURL" };
                SMTPSetting adminEmailSetting = new SMTPSetting { Section = "LTAS Billing Management", Name = "AdminEmailAddress" };

                List<SMTPSetting> smtpSettings = new List<SMTPSetting> { smtpPort, smtpServer, smtpUser, smtpPassword, smtpEnvironment, adminEmailSetting };

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
                    IsBodyHtml = true,                    
                };

                byte[] bytes;

                using (var stream = new MemoryStream())
                using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
                {
                    // Write headers
                    writer.WriteLine(string.Join(",", dt.Columns.Cast<DataColumn>().Select(column => column.ColumnName)));

                    // Write rows
                    foreach (DataRow row in dt.Rows)
                    {
                        writer.WriteLine(string.Join(",", row.ItemArray.Select(field => field?.ToString() ?? "")));
                    }

                    writer.Flush();
                    bytes = stream.ToArray();
                }

                using (var attachmentStream = new MemoryStream(bytes))
                {


                    var attachment = new System.Net.Mail.Attachment(attachmentStream, attachmentName, "text/csv");
                    emailMessage.Attachments.Add(attachment);
                    emailMessage.To.Add(adminEmailAddress);
                    emailMessage.ReplyToList.Add(new MailAddress(adminEmailAddress));

                    using (var smtpClient = new SmtpClient())
                    {
                        smtpClient.Host = smtpServerValue;
                        smtpClient.Port = smtpPortValue;
                        smtpClient.EnableSsl = true;
                        smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smtpClient.DeliveryFormat = SmtpDeliveryFormat.SevenBit;
                        smtpClient.UseDefaultCredentials = false;
                        smtpClient.Credentials = new NetworkCredential(smtpUserValue, smtpPasswordValue);
                        await smtpClient.SendMailAsync(emailMessage);
                    }
                }
            }

            public static async Task SendMissingInfoReportingAsync(IInstanceSettingsBundle instanceSettingsBundle, StringBuilder htmlBody)
            {
                SMTPSetting smtpPassword = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPPassword" };
                SMTPSetting smtpPort = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPPort" };
                SMTPSetting smtpServer = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPServer" };
                SMTPSetting smtpUser = new SMTPSetting { Section = "kCura.Notification", Name = "SMTPUserName" };
                SMTPSetting smtpEnvironment = new SMTPSetting { Section = "Relativity.Core", Name = "RelativityInstanceURL" };
                SMTPSetting adminEmailSetting = new SMTPSetting { Section = "LTAS Billing Management", Name = "AdminEmailAddress" };
                SMTPSetting reportingEmailSetting = new SMTPSetting { Section = "LTAS Billing Management", Name = "AnalystCaseTeamUpdates" };
                List<SMTPSetting> smtpSettings = new List<SMTPSetting> { smtpPort, smtpServer, smtpUser, smtpPassword, smtpEnvironment, adminEmailSetting, reportingEmailSetting };

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
                    Subject = $"{smtpEnvironmentValue.Split('-')[1].Split('.')[0].ToUpper()} - Workspaces Missing Team Information",
                    Body = htmlBody.ToString(),
                    IsBodyHtml = true
                };

               
                emailMessage.To.Add(supportEmailAddress);
                emailMessage.CC.Add(adminEmailAddress);
                emailMessage.ReplyToList.Add(new MailAddress(adminEmailAddress));

                using (var smtpClient = new SmtpClient())
                {
                    smtpClient.Host = smtpServerValue;
                    smtpClient.Port = smtpPortValue;
                    smtpClient.EnableSsl = true;
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtpClient.DeliveryFormat = SmtpDeliveryFormat.SevenBit;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(smtpUserValue, smtpPasswordValue);
                    await smtpClient.SendMailAsync(emailMessage);
                }
            }

        }
    }
}
