using LTASBM.Agent.Handlers;
using LTASBM.Agent.Models;
using LTASBM.Agent.Utilites;
using Newtonsoft.Json;
using Relativity.API;
using Relativity.Services.Objects;
using Relativity.Services.Objects.DataContracts;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTASBM.Agent.Managers
{
    public class ReportingManager
    {
        private readonly DataHandler _dataHandler;
        private readonly IInstanceSettingsBundle _instanceSettings;
        private readonly LTASBMHelper _ltasHelper;
        private readonly BillingAPIHandler _billingApi;
        private readonly IObjectManager _objectManager;
        private readonly int _billingManagementDatabase;
        private readonly LTASBMCostCodeHelper _costCodeHelper;

        public ReportingManager(
            IAPILog logger,
            IHelper helper,
            IObjectManager objectManager,
            DataHandler dataHandler,
            IInstanceSettingsBundle instanceSettings,
            int billingManagementDatabase,
            string instanceId,
            string token,
            string instanceUrl)
        {
            _dataHandler = dataHandler ?? throw new ArgumentNullException(nameof(dataHandler));
            _instanceSettings = instanceSettings ?? throw new ArgumentNullException(nameof(instanceSettings));
            _ltasHelper = new LTASBMHelper(helper, logger.ForContext<ReportingManager>());
            _billingApi = new BillingAPIHandler(logger, instanceId, token, instanceUrl);
            _objectManager = objectManager ?? throw new ArgumentNullException(nameof(objectManager));
            _billingManagementDatabase = billingManagementDatabase;
            _costCodeHelper = new LTASBMCostCodeHelper(_ltasHelper);
        }

        public async Task MonthlyProcessingOnlyReport()
        {
            var eddsWorkspaces = _dataHandler.EddsWorkspaces();
            await HandleProcessingOnlyAgeCheckAsync(eddsWorkspaces);
        }

        private IEnumerable<EddsWorkspaces> GetProcessingOnlyWorkspaces(List<EddsWorkspaces> eddsWorkspaces)
            => eddsWorkspaces
            .Where(w =>
            w.EddsWorkspaceStatusName.Equals("Processing Only", StringComparison.OrdinalIgnoreCase))
            .ToList();

        private async Task HandleProcessingOnlyAgeCheckAsync(List<EddsWorkspaces> eddsWorkspaces)
        {
            try
            {
                var processingOnlyWorkspaces = GetProcessingOnlyWorkspaces(eddsWorkspaces)
                    .OrderByDescending(w => (DateTime.Now - w.EddsWorkspaceCreatedOn).Days);

                if (!processingOnlyWorkspaces.Any()) return;

                var emailBody = new StringBuilder();

                // Add distinct creators list at top
                var creators = processingOnlyWorkspaces
                    .Select(w => w.EddsWorkspaceCreatedBy)
                    .Distinct()
                    .OrderBy(name => name);
                emailBody.AppendLine("Workspace Creators: " + string.Join("; ", creators));
                emailBody.AppendLine("<br>---------------<br>");


                emailBody.AppendLine("Processing Only Workspace Age Report:");
                emailBody.AppendLine("<br><br>");
                emailBody.AppendLine("<table border='1' style='border-collapse: collapse;'>");
                emailBody.AppendLine("<tr style='background-color: #f2f2f2;'>");
                emailBody.AppendLine("<th style='padding: 8px;'>Workspace Name</th>");
                emailBody.AppendLine("<th style='padding: 8px;'>Workspace ArtifactID</th>");
                emailBody.AppendLine("<th style='padding: 8px;'>Status</th>");
                emailBody.AppendLine("<th style='padding: 8px;'>Created By</th>");
                emailBody.AppendLine("<th style='padding: 8px;'>Created On</th>");
                emailBody.AppendLine("<th style='padding: 8px;'>Age (Days)</th>");
                emailBody.AppendLine("<th style='padding: 8px;'>Age (Months)</th>");
                emailBody.AppendLine("<th style='padding: 8px;'>Hosting Review (GB)</th>");
                emailBody.AppendLine("</tr>");

                foreach (var workspace in processingOnlyWorkspaces)
                {
                    var ageInDays = (DateTime.Now - workspace.EddsWorkspaceCreatedOn).Days;
                    var ageInMonths = ageInDays / 30.44; // Average days in a month
                    var billingMetrics = await _billingApi.GetMonthlyMetricsAsync(workspace.EddsWorkspaceArtifactId);
                    var billingData = JsonConvert.DeserializeObject<BillingResponse>(billingMetrics);
                    var hostingReview = billingData?.Results?.FirstOrDefault()?.PricedMetrics?.ReviewHosting?.BillableValue ?? 0;

                    string colorStyle = "";
                    if (ageInMonths > 10)
                        colorStyle = " color: red;";
                    else if (ageInMonths > 8)
                        colorStyle = " color: #FFD700;"; // Yellow

                    string storageStyle = "";
                    if (hostingReview > 30)
                        storageStyle = " color: red;";
                    else if (hostingReview > 20)
                        storageStyle = " color: #FFD700;"; // Yellow

                    emailBody.AppendLine("<tr>");
                    emailBody.AppendLine($"<td style='padding: 8px;{(ageInMonths > 10 ? " font-weight: bold;" : "")}'>{workspace.EddsWorkspaceName}</td>");
                    emailBody.AppendLine($"<td style='padding: 8px;'>{workspace.EddsWorkspaceArtifactId}</td>");
                    emailBody.AppendLine($"<td style='padding: 8px;'>{workspace.EddsWorkspaceStatusName}</td>");
                    emailBody.AppendLine($"<td style='padding: 8px;'>{workspace.EddsWorkspaceCreatedBy}</td>");
                    emailBody.AppendLine($"<td style='padding: 8px;'>{workspace.EddsWorkspaceCreatedOn:yyyy-MM-dd HH:mm}</td>");
                    emailBody.AppendLine($"<td style='padding: 8px;{colorStyle}'>{ageInDays:N0}</td>");
                    emailBody.AppendLine($"<td style='padding: 8px;{colorStyle}'>{ageInMonths:N1}</td>");
                    emailBody.AppendLine($"<td style='padding: 8px;{storageStyle}'>{hostingReview:N2}</td>");
                    emailBody.AppendLine("</tr>");
                }

                emailBody.AppendLine("</table>");
                emailBody.AppendLine("<p><small>* Indicators:</small></p>");
                emailBody.AppendLine("<ul>");
                emailBody.AppendLine("<li><small>Age:</small></li>");
                emailBody.AppendLine($"<li><small style='color: #FFD700;'>Yellow: 8-10 months</small></li>");
                emailBody.AppendLine($"<li><small style='color: red;'>Red: Over 10 months</small></li>");
                emailBody.AppendLine("<li><small>Storage:</small></li>");
                emailBody.AppendLine($"<li><small style='color: #FFD700;'>Yellow: Over 20 GB</small></li>");
                emailBody.AppendLine($"<li><small style='color: red;'>Red: Over 30 GB</small></li>");
                emailBody.AppendLine("</ul>");

                await MessageHandler.Email.SendInternalNotificationAsync(
                    _instanceSettings,
                    emailBody,
                    "Processing Only Workspaces Age Report");
            }
            catch (Exception ex)
            {
                _ltasHelper.Logger.LogError(ex, "Error handling Processing Only workspace age check");
                throw;
            }
        }


        public async Task SendInvoiceEmail()
        {
            int billingDetailsArtifactId = 0;
            int matterArtifactId = 0;

            try
            {
                var dateKey = DateTime.Now.ToString("yyyyMM");
                var dBContext = _ltasHelper.Helper.GetDBContext(_billingManagementDatabase);
                string environment = _billingApi.GetInstanceGeoCode();

                var billingDetails = await ObjectHandler.BillingDetailsDataForReporting(
                    _objectManager,
                    _billingManagementDatabase,
                    _ltasHelper.Logger,
                    _ltasHelper.Helper,
                    dateKey);

                foreach (var billingDetail in billingDetails.Objects)
                {                    
                    billingDetailsArtifactId = Convert.ToInt32(billingDetail.FieldValues
                        .FirstOrDefault(f => f.Field.Name == "ArtifactID")?.Value);

                    var targetBillingDetails = new[] { 1077462, 1077294, 1077537, 1077260, 1077459, 1072886, 1077294 };
                    if (!targetBillingDetails.Contains(billingDetailsArtifactId))
                    {
                        //_ltasHelper.Logger.LogError($"Skipping non-target billing matter: {billingDetailsArtifactId}");
                        continue;
                    }

                    var dt = CreateBillingDataTable();

                    matterArtifactId = _ltasHelper.GetBillingDetailsParentMatterArtifactID(
                        dBContext,
                        Convert.ToInt32(billingDetailsArtifactId));


                    


                    //// Skip internal cases
                    //var targetMatters = new[] { 1076589, 1070558, 1071565, 1071139, 1070571 };
                    //if (!targetMatters.Contains(matterArtifactID))
                    //{
                    //    _ltasHelper.Logger.LogError($"Skipping non-target matter: {matterArtifactID}");
                    //    continue;
                    //}

                    if (matterArtifactId == 0)
                    {
                        _ltasHelper.Logger.LogError($"Billing Detail ArtifactId: {billingDetailsArtifactId} did not return a Matter ArtifactId");
                        return;
                    }

                    var matterDetails = await ObjectHandler.MatterDetailsData(
                        _objectManager,
                        _billingManagementDatabase,
                        _ltasHelper.Logger,
                        _ltasHelper.Helper,
                        matterArtifactId);

                    foreach (var matterDetail in matterDetails.Objects)
                    {
                        var matterName = matterDetail.FieldValues
                            .FirstOrDefault(f => f.Field.Guids.Contains(_ltasHelper.MatterNameField))?.Value?.ToString();

                        var matterNumber = matterDetail.FieldValues
                            .FirstOrDefault(f => f.Field.Guids.Contains(_ltasHelper.MatterNumberField))?.Value?.ToString();

                        //skip internal cases
                        if (matterName?.Contains("Quinn Internal") ?? false)
                            continue;

                        var matterGuid = matterDetail.FieldValues
                            .FirstOrDefault(f => f.Field.Guids.Contains(_ltasHelper.MatterGUIDField))?.Value.ToString();


                        var mainContactInfo = matterDetail.FieldValues
                            .FirstOrDefault(f => f.Field.Guids.Contains(_ltasHelper.MatterMainContact))?.Value is RelativityObjectValue mainContact ?
                            await ObjectHandler.BillingUserLookup(_objectManager, _billingManagementDatabase, mainContact.ArtifactID, _ltasHelper.Logger, _ltasHelper.Helper) : null;

                        var mainContactFirstName = string.Empty;
                        var mainContactEmailAddress = string.Empty;

                        if (mainContactInfo?.Objects.FirstOrDefault() is RelativityObject mainContactObj)
                        {
                            mainContactFirstName = mainContactObj.FieldValues
                                .FirstOrDefault(f => f.Field.Guids.Contains(_ltasHelper.UserFirstNameField))?.Value?.ToString() ?? "";
                            
                            mainContactEmailAddress = mainContactObj.FieldValues
                                .FirstOrDefault(f => f.Field.Guids.Contains(_ltasHelper.UserEmailAddressField))?.Value?.ToString() ?? "";
                        }

                        var emailTo = matterDetail.FieldValues
                            .FirstOrDefault(f => f.Field.Guids.Contains(_ltasHelper.MatterEmailTo))?.Value;

                        var emailCc = matterDetail.FieldValues
                            .FirstOrDefault(f => f.Field.Guids.Contains(_ltasHelper.MatterEmailCC))?.Value;

                        var toEmailString = await GetEmailListFromFieldValue(emailTo);

                        var combinedToEmailsString = !string.IsNullOrWhiteSpace(mainContactEmailAddress)
                            ? mainContactEmailAddress + (!string.IsNullOrWhiteSpace(toEmailString) ? ";" + toEmailString : "")
                            : toEmailString;

                        var ccEmailString = await GetEmailListFromFieldValue(emailCc);

                        var workspaceCount = Convert.ToInt32(billingDetail.FieldValues
                            .FirstOrDefault(f => f.Field.Guids.Contains(_ltasHelper.DetailsWorkspaceCount))?.Value);

                        //Process cost codes for this matter
                        var costdt = ProcessCostCodesForMatter(dt,
                            billingDetail,
                            matterDetail,
                            environment);

                        //Process users
                        var dtUsers = CreateBillingUsersTable();
                        var billingUsers = billingDetail.FieldValues.FirstOrDefault(f => f.Field.Guids.Contains(_ltasHelper.DetailsUsers))?.Value;

                        var userList = billingUsers as List<RelativityObjectValue>;

                        var matterUserInfo = await ProcessUsersForMatter(userList);
                                                       
                        StringBuilder sb = new StringBuilder();

                        MessageHandler.SendInvoiceEmailBody(sb, costdt, mainContactFirstName, combinedToEmailsString, ccEmailString, workspaceCount);

                        await MessageHandler.Email.SendInternalNotificationWAttachmentAsync(
                            _instanceSettings,
                            sb,
                            "users?",
                            matterUserInfo,
                            $"UserReport_{matterNumber}.csv");

                        // Send email for this specific matter if we have data
                        if (dt.Rows.Count > 0)
                        {
                            //send email code here 
                            
                            _ltasHelper.Logger.LogInformation($"Email sent successfully for matter {matterName} ({matterArtifactId})");
                        }
                        else
                        {
                            _ltasHelper.Logger.LogInformation($"No billable items found for matter {matterName} ({matterArtifactId})");
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                _ltasHelper.Logger.LogError(ex, $"Error generating invoice email for Billing ArtifactId:{billingDetailsArtifactId}, Matter ArtifacId:{matterArtifactId}");
                throw;
            }
        }

        private DataTable ProcessCostCodesForMatter(
            DataTable dt,
            RelativityObject billingDetail,
            RelativityObject matterDetail,
            string environment)
        {
            foreach (var costCodeInfo in _costCodeHelper.GetAllCostCodes())
            {
                var fieldValue = billingDetail.FieldValues
                    .FirstOrDefault(f => f.Field.Guids.Contains(costCodeInfo.QuantityFieldGuid))?.Value;

                if (fieldValue == null || !decimal.TryParse(fieldValue.ToString(), out decimal quantity) || quantity <= 0)
                {
                    continue;
                }
                var row = dt.NewRow();

                row["CostCode"] = (int)costCodeInfo.Code;
                row["CostCodeDescription"] = costCodeInfo.Description;
                row["Quantity"] = quantity;
                row["StandardRate"] = costCodeInfo.GetRateForEnvironment(environment);


                // Handle override rate
                var overrideValue = matterDetail.FieldValues
                    .FirstOrDefault(f => f.Field.Guids.Contains(costCodeInfo.OverrideFieldGuid))?.Value;

                if (overrideValue != null &&
                    decimal.TryParse(overrideValue.ToString(), out decimal overrideRate) &&
                    overrideRate >= 0)
                {
                    row["HasOverride"] = true;
                    row["OverrideRate"] = overrideRate;
                    row["FinalRate"] = overrideRate;
                }
                else
                {
                    row["HasOverride"] = false;
                    row["OverrideRate"] = DBNull.Value;
                    row["FinalRate"] = costCodeInfo.GetRateForEnvironment(environment);
                }

                row["BilledAmount"] = quantity * Convert.ToDecimal(row["FinalRate"]);
                dt.Rows.Add(row);
            }

            return dt;
        }

        private async Task<DataTable> ProcessUsersForMatter(List<RelativityObjectValue> users)
        {
            var dt = CreateBillingUsersTable();

            foreach (var user in users)
            {
                var userDetails = await ObjectHandler.BillingUserLookup(
                    _objectManager, 
                    _billingManagementDatabase, 
                    user.ArtifactID,
                    _ltasHelper.Logger, 
                    _ltasHelper.Helper);

                foreach (var relativityUser in userDetails.Objects)
                {
                    var row = dt.NewRow();

                    row["userArtifactID"] = Convert.ToInt32(relativityUser.FieldValues
                        .FirstOrDefault(f => f.Field.Name == "ArtifactID")?.Value);

                    row["FirstName"] = relativityUser.FieldValues
                        .FirstOrDefault(f => f.Field.Guids.Contains(_ltasHelper.UserFirstNameField))?.Value;

                    row["LastName"] = relativityUser.FieldValues
                        .FirstOrDefault(f => f.Field.Guids.Contains(_ltasHelper.UserLastNameField))?.Value;

                    row["EmailAddress"] = relativityUser.FieldValues
                        .FirstOrDefault(f => f.Field.Guids.Contains(_ltasHelper.UserEmailAddressField))?.Value;

                    dt.Rows.Add(row);                    
                }
            }
            return dt;
        }

        //TODO: Remove
        //private MemoryStream CreateUsersCsvStream(DataTable userTable)
        //{
        //    // First create the CSV content as a string
        //    var sb = new StringBuilder();

        //    // Write headers
        //    sb.AppendLine(string.Join(",", userTable.Columns.Cast<DataColumn>().Select(column => column.ColumnName)));

        //    // Write rows
        //    foreach (DataRow row in userTable.Rows)
        //    {
        //        sb.AppendLine(string.Join(",", row.ItemArray.Select(field => field?.ToString() ?? "")));
        //    }

        //    // Convert to bytes
        //    byte[] byteArray = Encoding.UTF8.GetBytes(sb.ToString());

        //    // Create a readable MemoryStream
        //    return new MemoryStream(byteArray) { Position = 0 };
        //}

        private async Task SendBillingDetailsEmail(DataTable dt, string matterName)
        {
            var sortedDt = dt.AsEnumerable()
                .OrderBy(row => row.Field<string>("CostCode"))
                .ThenBy(row => row.Field<bool>("HasOverride"))
                .ThenBy(row => row.Field<string>("MatterGuid"))
                .CopyToDataTable();

            var emailBody = new StringBuilder();

            emailBody.AppendLine($"Billing Details for Matter: {matterName}");
            emailBody.AppendLine("<br><br>");
            emailBody.AppendLine("<table border='1' style='border-collapse: collapse; width: 100%;'>");

            // Add header row
            emailBody.AppendLine("<tr style='background-color: #f2f2f2;'>");
            foreach (DataColumn column in sortedDt.Columns)
            {
                emailBody.AppendLine($"<th style='padding: 8px; text-align: left;'>{FormatColumnHeader(column.ColumnName)}</th>");
            }
            emailBody.AppendLine("</tr>");

            // Add data rows
            foreach (DataRow row in sortedDt.Rows)
            {
                emailBody.AppendLine("<tr>");
                foreach (DataColumn column in sortedDt.Columns)
                {
                    string style = "padding: 8px;";
                    string value = FormatCellValue(row[column], column.ColumnName);

                    // Add special formatting for amounts and rates
                    if (column.ColumnName.Contains("Amount") || column.ColumnName.Contains("Rate"))
                    {
                        style += " text-align: right;";
                    }

                    emailBody.AppendLine($"<td style='{style}'>{value}</td>");
                }
                emailBody.AppendLine("</tr>");
            }

            emailBody.AppendLine("</table>");

            // Add summary section if needed
            if (sortedDt.Rows.Count > 0)
            {
                var totalBilled = sortedDt.Compute("SUM(BilledAmount)", string.Empty);
                emailBody.AppendLine("<br>");
                emailBody.AppendLine("<div style='text-align: right;'>");
                emailBody.AppendLine($"<strong>Total Billed Amount: {FormatCellValue(totalBilled, "BilledAmount")}</strong>");
                emailBody.AppendLine("</div>");
            }

            // Add timestamp
            emailBody.AppendLine("<br>");
            emailBody.AppendLine($"<small>Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</small>");

            await MessageHandler.Email.SendInternalNotificationAsync(
                _instanceSettings,
                emailBody,
                $"Billing Details Report - {DateTime.Now:yyyy-MM}");
        }

        private string FormatColumnHeader(string columnName)
        {
            // Split camel case and add spaces
            return string.Concat(columnName.Select(x => char.IsUpper(x) ? " " + x : x.ToString())).TrimStart();
        }

        private string FormatCellValue(object value, string columnName)
        {
            if (value == null || value == DBNull.Value)
                return "-";

            if (columnName.Contains("Amount") || columnName.Contains("Rate"))
                return decimal.TryParse(value.ToString(), out decimal amount)
                    ? amount.ToString("C2")
                    : value.ToString();

            if (columnName.Contains("Quantity"))
                return decimal.TryParse(value.ToString(), out decimal qty)
                    ? qty.ToString("N2")
                    : value.ToString();

            if (columnName == "HasOverride")
                return Convert.ToBoolean(value) ? "Yes" : "No";

            return value.ToString();
        }

        private DataTable CreateBillingDataTable()
        {
            var dt = new DataTable("BillingDetails");

            // Basic identification columns
            dt.Columns.Add("BillingDetailsArtifactID", typeof(int));
            dt.Columns.Add("MatterArtifactID", typeof(int));
            dt.Columns.Add("MatterName", typeof(string));
            dt.Columns.Add("MatterGuid", typeof(string));

            // Cost code related columns
            dt.Columns.Add("CostCode", typeof(string));
            dt.Columns.Add("CostCodeDescription", typeof(string));
            dt.Columns.Add("Quantity", typeof(decimal));
            dt.Columns.Add("StandardRate", typeof(decimal));

            // Override related columns
            dt.Columns.Add("HasOverride", typeof(bool));
            dt.Columns.Add("OverrideRate", typeof(decimal));
            dt.Columns.Add("FinalRate", typeof(decimal)); 

            // Billing amounts
            dt.Columns.Add("BilledAmount", typeof(decimal));
            return dt;
        }

        private DataTable CreateBillingUsersTable()
        {
            var dt = new DataTable("BillingUsersDetail");

            dt.Columns.Add("userArtifactID", typeof(int));
            dt.Columns.Add("FirstName", typeof(string));
            dt.Columns.Add("LastName", typeof(string));
            dt.Columns.Add("EmailAddress", typeof(string));
            return dt;
        }

        public async Task<string> GetEmailListFromFieldValue(object fieldValue)
        {
            try
            {
                var emailList = new List<string>();

                // Handle single object
                if (fieldValue is RelativityObjectValue singleObject)
                {
                    var email = await GetEmailFromObject(singleObject);
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        emailList.Add(email);
                    }
                }
                // Handle list of objects
                else if (fieldValue is List<RelativityObjectValue> objectList)
                {
                    foreach (var obj in objectList)
                    {
                        var email = await GetEmailFromObject(obj);
                        if (!string.IsNullOrWhiteSpace(email))
                        {
                            emailList.Add(email);
                        }
                    }
                }

                // Join all valid emails with semicolon
                return string.Join(";", emailList);
            }
            catch (Exception ex)
            {
                _ltasHelper.Logger.LogError(ex, "Error processing email addresses from field value");
                return string.Empty;
            }
        }

        private async Task<string> GetEmailFromObject(RelativityObjectValue obj)
        {
            try
            {
                var userInfo = await ObjectHandler.BillingUserLookup(
                    _objectManager,
                    _billingManagementDatabase,
                    obj.ArtifactID,
                    _ltasHelper.Logger,
                    _ltasHelper.Helper);

                if (userInfo?.Objects.FirstOrDefault() is RelativityObject userObj)
                {
                    return userObj.FieldValues
                        .FirstOrDefault(f => f.Field.Guids.Contains(_ltasHelper.UserEmailAddressField))
                        ?.Value?.ToString();
                }
            }
            catch (Exception ex)
            {
                _ltasHelper.Logger.LogError(ex, $"Error retrieving email for artifact ID: {obj.ArtifactID}");
            }

            return string.Empty;
        }
    }    
}
