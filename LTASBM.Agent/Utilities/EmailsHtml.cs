using LTASBM.Agent.Models;
using System.Collections.Generic;
using System.Text;

namespace LTASBM.Agent.Utilities
{
    static class EmailsHtml
    {
        public static StringBuilder InvalidClientEmailBody(StringBuilder htmlBody, EddsClients c) 
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
            htmlBody.AppendLine($"<p>Hi {c.EddsClientCreatedByFirstName},</p>");
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
            htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;'>{c.EddsClientName}</td>");
            htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;'>{c.EddsClientNumber}</td>");
            htmlBody.AppendLine("\t\t</tr>");
            htmlBody.AppendLine("\t</tbody>");
            htmlBody.AppendLine("</table>");
            htmlBody.AppendLine("<p>&nbsp;</p>");
            // Client link
            htmlBody.AppendLine($"<p><a href=\"https://qe-us.relativity.one/Relativity/RelativityInternal.aspx?AppID=-1&ArtifactTypeID=5&ArtifactID={c.EddsClientArtifactId}&Mode=Forms&FormMode=view&LayoutID=null&SelectedTab=null\">Click here to view the record</a></p>");
            htmlBody.AppendLine("<p>If you have any questions, please reach out to Damien for assistance.</p>");
            // Footer with responsive logo
            htmlBody.AppendLine("<p>");
            htmlBody.AppendLine("<img class='logo-image' alt=\"\" src=\"https://i.ibb.co/kgXTdQb/LTASLogo.png\" /> ");
            htmlBody.AppendLine("<small><var><code><strong>LTAS Billing Automations</strong></code></var></small>");
            htmlBody.AppendLine("</p>");
            htmlBody.AppendLine("<p><small><var><code><strong>[FYI: this job runs once an hour]</strong></code></var></small></p>");
            htmlBody.AppendLine("</div>");
            htmlBody.AppendLine("</body></html>");

            return htmlBody;
        }

        public static StringBuilder DataOutput(StringBuilder htmlBody, EddsClients c) 
        {

            return htmlBody;
        }

        public static StringBuilder NewClientsToBeCreated(StringBuilder htmlBody, List<EddsClients> newClients) 
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
            htmlBody.AppendLine(".logo-image { width: 30px; height: 15px; vertical-align: middle; }");
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

            foreach (var client in newClients)
            {
                htmlBody.AppendLine("\t\t<tr>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;'>{client.EddsClientName}</td>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;'>{client.EddsClientNumber}</td>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;'>{client.EddsClientArtifactId}</td>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;'>{client.EddsClientCreatedByFirstName}</td>");
                htmlBody.AppendLine($"\t\t\t<td style='padding: 8px;'><a href=\"https://qe-us.relativity.one/Relativity/RelativityInternal.aspx?AppID=-1&ArtifactTypeID=5&ArtifactID={client.EddsClientArtifactId}&Mode=Forms&FormMode=view&LayoutID=null&SelectedTab=null\">View</a></td>");
                htmlBody.AppendLine("\t\t</tr>");
            }

            htmlBody.AppendLine("\t</tbody>");
            htmlBody.AppendLine("</table>");
            htmlBody.AppendLine("<p>&nbsp;</p>");

            // Footer with responsive logo
            htmlBody.AppendLine("<p>");
            htmlBody.AppendLine("<img class='logo-image' alt=\"\" src=\"https://i.ibb.co/kgXTdQb/LTASLogo.png\" /> ");
            htmlBody.AppendLine("<small><var><code><strong>LTAS Billing Automations</strong></code></var></small>");
            htmlBody.AppendLine("</p>");

            htmlBody.AppendLine("<p><small><var><code><strong>[FYI: this job runs once an hour]</strong></code></var></small></p>");

            htmlBody.AppendLine("</div>");
            htmlBody.AppendLine("</body></html>");

            return htmlBody;
        }
    }
}
