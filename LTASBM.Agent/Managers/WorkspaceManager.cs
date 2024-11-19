using LTASBM.Agent.Handlers;
using LTASBM.Agent.Models;
using LTASBM.Agent.Utilites;
using Relativity.API;
using Relativity.Services.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTASBM.Agent.Managers
{
    public class WorkspaceManager
    {
        private readonly IObjectManager _objectManager;
        private readonly DataHandler _dataHandler;
        private readonly IInstanceSettingsBundle _instanceSettings;
        private readonly int _billingManagementDatabase;
        private readonly LTASBMHelper _ltasHelper;

        public WorkspaceManager(
            IAPILog logger,
            IHelper helper,
            IObjectManager objectManager,
            DataHandler dataHandler,
            IInstanceSettingsBundle instanceSettings,
            int billingManagementDatabase)
        {
            _objectManager = objectManager ?? throw new ArgumentNullException(nameof(objectManager));
            _dataHandler = dataHandler ?? throw new ArgumentNullException(nameof(dataHandler));
            _instanceSettings = instanceSettings ?? throw new ArgumentNullException(nameof(instanceSettings));
            _billingManagementDatabase = billingManagementDatabase;
            _ltasHelper = new LTASBMHelper(helper, logger.ForContext<WorkspaceManager>());
        }
        public async Task ProcessWorkspaceRoutinesAsync()
        {
            try
            {
                var eddsWorkspaces = _dataHandler.EddsWorkspaces();
                var billingWorkspaces = _dataHandler.BillingWorkspaces();

                await ProcessAllWorkspaceOperationsAsync(eddsWorkspaces, billingWorkspaces);
            }
            catch (Exception ex)
            {
                _ltasHelper.Logger.LogError(ex, "Error In ProcessWorkspaceRoutine");
            }
        }
        public async Task ProcessMonthlyReportingJobs()
        {
            var eddsWorkspaces = _dataHandler.EddsWorkspaces();
            await HandleProcessingOnlyAgeCheckAsync(eddsWorkspaces);
        }

        public async Task ProcessDailyOperationsAsnyc()
        {
            var eddsWorkspaces = _dataHandler.EddsWorkspaces();
            await NotifyMissingTeamInfoAsync(eddsWorkspaces);
        }
        private async Task ProcessAllWorkspaceOperationsAsync(List<EddsWorkspaces> eddsWorkspaces, List<BillingWorkspaces> billingWorkspaces)
        {
            var invalidWorkspaces = GetInvalidWorkspaces(billingWorkspaces);
            await NotifyInvalidWorkspacesAsync(invalidWorkspaces);

            var duplicateWorkspaces = GetDuplicateWorkspaces(billingWorkspaces);
            await NotifyDuplicateWorkspacesAsync(duplicateWorkspaces);

            var NewWorkspaces = GetNewWorkspacesForBilling(eddsWorkspaces, billingWorkspaces);
            await ProcessNewWorkspacesAsync(NewWorkspaces);
            
            await HandleOrphanedWorkspacesAsync(eddsWorkspaces, billingWorkspaces);

            await HandleProcessingOnlyMismatchAsync(eddsWorkspaces);            
        }
        private IEnumerable<EddsWorkspaces> GetNewWorkspacesForBilling(List<EddsWorkspaces> eddsWorkspaces, List<BillingWorkspaces> billingWorkspaces)
        {
            var invalidWorkspaceIds = new HashSet<int>(billingWorkspaces
                .Where(w => w.BillingWorkspaceArtifactId == 0 || w.BillingWorkspaceEddsArtifactId == 0)
                .Select(w => w.BillingWorkspaceEddsArtifactId));

            var duplicateWorkspaceIds = new HashSet<int>(billingWorkspaces
                .GroupBy(w => w.BillingWorkspaceEddsArtifactId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key));

            return eddsWorkspaces.Where(edds =>
                !billingWorkspaces.Any(billing => billing.BillingWorkspaceEddsArtifactId == edds.EddsWorkspaceArtifactId) &&
                !invalidWorkspaceIds.Contains(edds.EddsWorkspaceArtifactId) &&
                !duplicateWorkspaceIds.Contains(edds.EddsWorkspaceArtifactId))
                .ToList();
        }
        private IEnumerable<BillingWorkspaces> GetInvalidWorkspaces(List<BillingWorkspaces> billingWorkspaces) 
            => billingWorkspaces.Where(w => w.BillingWorkspaceArtifactId == 0 || w.BillingWorkspaceEddsArtifactId == 0).ToList();
        private IEnumerable<BillingWorkspaces> GetDuplicateWorkspaces(List<BillingWorkspaces> billingWorkspaces)
            => billingWorkspaces.GroupBy(w => w.BillingWorkspaceEddsArtifactId).Where(g => g.Count() > 1).SelectMany(g => g).ToList();
        private IEnumerable<EddsWorkspaces> GetWorkspacesMissingTeamInfo(List<EddsWorkspaces> eddsWorkspaces)
            => eddsWorkspaces
            .Where(w =>
            (string.IsNullOrWhiteSpace(w.EddsWorkspaceAnalyst) ||
            string.IsNullOrWhiteSpace(w.EddsWorkspaceCaseTeam)) &&
            !new[] { "Template" }
                .Contains(w.EddsWorkspaceStatusName, StringComparer.OrdinalIgnoreCase))
            .ToList();
        private IEnumerable<BillingWorkspaces> GetOrphanedWorkspaces(
            List<EddsWorkspaces> eddsWorkspaces,
            List<BillingWorkspaces> billingWorkspaces)
            => billingWorkspaces
                .Where(billing =>
                    !eddsWorkspaces.Any(edds => edds.EddsWorkspaceArtifactId == billing.BillingWorkspaceEddsArtifactId) &&
                    billing.BillingStatusName != "Deleted")
                .ToList();
        private IEnumerable<EddsWorkspaces> GetProcessingOnlyWorkspaces(List<EddsWorkspaces> eddsWorkspaces)
            => eddsWorkspaces
            .Where(w =>
            w.EddsWorkspaceStatusName.Equals("Processing Only", StringComparison.OrdinalIgnoreCase))
            .ToList();
        private IEnumerable<EddsWorkspaces> GetProcessingOnlyNameMismatch(List<EddsWorkspaces> eddsWorkspaces)
        {
            var nonQEInternalWorkspaces = eddsWorkspaces
                .Where(w => w.EddsMatterName.IndexOf("QE INTERNAL", StringComparison.OrdinalIgnoreCase) == -1);

            var poWorkspaces = nonQEInternalWorkspaces
                .Where(w =>
                    w.EddsWorkspaceName.IndexOf("PO", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    !w.EddsWorkspaceStatusName.Equals("Processing Only", StringComparison.OrdinalIgnoreCase));

            return poWorkspaces.ToList();
        }
      
        private async Task NotifyInvalidWorkspacesAsync(IEnumerable<BillingWorkspaces> invalidWorkspaces) 
        {
            if (invalidWorkspaces.Any())
            {
                var emailBody = new StringBuilder();
                emailBody = MessageHandler.InvalidWorkspaceEmailBody(emailBody, invalidWorkspaces.ToList());
                await MessageHandler.Email.SendInternalNotificationAsync(_instanceSettings, emailBody, "Invalid Workspaces");                
            }            
        }
        private async Task NotifyDuplicateWorkspacesAsync(IEnumerable<BillingWorkspaces> duplicateWorkspaces) 
        {
            if(duplicateWorkspaces.Any()) 
            {
                var emailBody = new StringBuilder();
                MessageHandler.DuplicateWorkspacesEmailBody(emailBody, duplicateWorkspaces.ToList());
                await MessageHandler.Email.SendInternalNotificationAsync(_instanceSettings, emailBody, "Duplicate Workspaces Found");
            }            
        }
        private async Task ProcessNewWorkspacesAsync(IEnumerable<EddsWorkspaces> newWorksapces) 
        {
            if(!newWorksapces.Any()) return;
            await NotifyNewWorkspacesAsync(newWorksapces);
            await CreateNewWorkspacesInBillingAsync(newWorksapces);
        }
        private async Task NotifyNewWorkspacesAsync(IEnumerable<EddsWorkspaces> newWorkspaces) 
        {
            var emailBody = new StringBuilder();
            MessageHandler.NewWorkspacesEmailBody(emailBody, newWorkspaces.ToList());
            await MessageHandler.Email.SendInternalNotificationAsync(_instanceSettings, emailBody, "New Workspaces");
        }
        private async Task CreateNewWorkspacesInBillingAsync(IEnumerable<EddsWorkspaces> NewWorkspaces) 
        {
            foreach (var workspace in NewWorkspaces)
            {
                try
                {
                    _ltasHelper.Logger.LogInformation("Attempting to create workspace: {workspaceDetails}",
                                new { workspace.EddsWorkspaceArtifactId, workspace.EddsWorkspaceName });

                    var result = await ObjectHandler.CreateNewWorkspaceAsync(
                        _objectManager,
                        _billingManagementDatabase,
                        workspace.EddsWorkspaceArtifactId,
                        workspace.EddsWorkspaceCreatedBy,
                        workspace.EddsWorkspaceCreatedOn,
                        workspace.EddsWorkspaceName,
                        workspace.EddsWorkspaceMatterArtifactId,
                        workspace.EddsWorkspaceCaseTeam,
                        workspace.EddsWorkspaceAnalyst,
                        workspace.EddsWorkspaceStatusName,
                        _ltasHelper.Logger,
                        _ltasHelper.Helper);

                    if (result == null)
                    {
                        _ltasHelper.Logger.LogError($"CreateNewWorkspace returned null for workspace {workspace.EddsWorkspaceArtifactId}");
                    }
                }
                catch (Exception ex)
                {
                    _ltasHelper.Logger.LogError(ex, "Error creating workspace: {}. Error:{}",
                       new { workspace.EddsWorkspaceArtifactId, workspace.EddsWorkspaceName }, ex.Message);
                }
            }
        }
        private async Task NotifyMissingTeamInfoAsync(List<EddsWorkspaces> eddsWorkspaces)
        {
            try
            {
                var workspacesMissingInfo = GetWorkspacesMissingTeamInfo(eddsWorkspaces);

                if (!workspacesMissingInfo.Any()) return;

                var emailBody = new StringBuilder();
                emailBody.AppendLine("The following workspaces in EDDS are missing team information:");
                emailBody.AppendLine("<br><br>");
                emailBody.AppendLine("<table border='1'>");
                emailBody.AppendLine("<tr>");
                emailBody.AppendLine("<th>Workspace Name</th>");
                emailBody.AppendLine("<th>Workspace ID</th>");
                emailBody.AppendLine("<th>Missing LTAS Analyst</th>");
                emailBody.AppendLine("<th>Missing Case Team</th>");
                emailBody.AppendLine("<th>Created By</th>");
                emailBody.AppendLine("<th>Created On</th>");                
                emailBody.AppendLine("</tr>");

                foreach (var workspace in workspacesMissingInfo)
                {
                    emailBody.AppendLine("<tr>");
                    emailBody.AppendLine($"<td>{workspace.EddsWorkspaceName}</td>");
                    emailBody.AppendLine($"<td>{workspace.EddsWorkspaceArtifactId}</td>");
                    emailBody.AppendLine($"<td>{(string.IsNullOrWhiteSpace(workspace.EddsWorkspaceAnalyst) ? "Yes" : "No")}</td>");
                    emailBody.AppendLine($"<td>{(string.IsNullOrWhiteSpace(workspace.EddsWorkspaceCaseTeam) ? "Yes" : "No")}</td>");
                    emailBody.AppendLine($"<td>{workspace.EddsWorkspaceCreatedBy}</td>");
                    emailBody.AppendLine($"<td>{workspace.EddsWorkspaceCreatedOn:yyyy-MM-dd HH:mm}</td>");                    
                    emailBody.AppendLine("</tr>");
                }

                emailBody.AppendLine("</table>");

                await MessageHandler.Email.SendInternalNotificationAsync(
                    _instanceSettings,
                    emailBody,
                    "Workspaces Missing Team Information");
            }
            catch (Exception ex)
            {
                _ltasHelper.Logger.LogError(ex, "Error handling workspaces missing team info check");
                throw;
            }
        }
        private async Task HandleOrphanedWorkspacesAsync(
            List<EddsWorkspaces> eddsWorkspaces,
            List<BillingWorkspaces> billingWorkspaces)
        {
            try
            {
                var orphanedWorkspaces = GetOrphanedWorkspaces(eddsWorkspaces, billingWorkspaces);
                
                if (!orphanedWorkspaces.Any()) return;

                var emailBody = new StringBuilder();
                emailBody.AppendLine("<h3>The following workspaces in Billing system will be marked as Deleted because they no longer exist in EDDS:</h3>");
                emailBody.AppendLine("<table border='1' style='border-collapse: collapse;'>");
                emailBody.AppendLine("<tr style='background-color: #f2f2f2;'>");
                emailBody.AppendLine("<th style='padding: 8px;'>Workspace Name</th>");
                emailBody.AppendLine("<th style='padding: 8px;'>Workspace ID</th>");
                emailBody.AppendLine("<th style='padding: 8px;'>Current Status</th>");
                emailBody.AppendLine("</tr>");

                foreach (var workspace in orphanedWorkspaces)
                {
                    emailBody.AppendLine("<tr>");
                    emailBody.AppendLine($"<td style='padding: 8px;'>{workspace.BillingWorkspaceName}</td>");
                    emailBody.AppendLine($"<td style='padding: 8px;'>{workspace.BillingWorkspaceArtifactId}</td>");
                    emailBody.AppendLine($"<td style='padding: 8px;'>{workspace.BillingStatusName}</td>");
                    emailBody.AppendLine("</tr>");
                }

                emailBody.AppendLine("</table>");
                emailBody.AppendLine("<br/>");
                emailBody.AppendLine("<p><em>These workspaces will be automatically updated to 'Deleted' status.</em></p>");

                await MessageHandler.Email.SendInternalNotificationAsync(
                    _instanceSettings,
                    emailBody,
                    "Orphaned Workspaces Status Update");

                foreach (var workspace in orphanedWorkspaces)
                {
                    try 
                    {
                        var deletedStatusArtifactId = _ltasHelper.GetCaseStatusArtifactID(
                            _ltasHelper.Helper.GetDBContext(_billingManagementDatabase),
                            "Deleted");

                        if (deletedStatusArtifactId != 0)
                        {
                            await ObjectHandler.UpdateFieldValueAsync(
                                _objectManager,
                                _billingManagementDatabase,
                                workspace.BillingWorkspaceArtifactId,
                                _ltasHelper.WorkspaceStatusField,
                                new Relativity.Services.Objects.DataContracts.ChoiceRef
                                {
                                    ArtifactID = deletedStatusArtifactId
                                },
                                true,
                                _ltasHelper.Logger);
                        }
                        else
                        {
                            _ltasHelper.Logger.LogError(
                                "Could not find 'Deleted' status choice artifact ID for workspace {WorkspaceId}",
                                workspace.BillingWorkspaceArtifactId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _ltasHelper.Logger.LogError(ex,
                            "Error updating status to Deleted for workspace {WorkspaceDetails}",
                            new { workspace.BillingWorkspaceArtifactId, workspace.BillingWorkspaceName });
                    }
                }

            }
            catch (Exception ex)
            {
                _ltasHelper.Logger.LogError(ex, "Error handling orphaned workspaces");
                throw;
            }
        }
        private async Task HandleProcessingOnlyMismatchAsync(List<EddsWorkspaces> eddsWorkspaces)
        {
            try
            {
                var mismatchedWorkspaces = GetProcessingOnlyNameMismatch(eddsWorkspaces);

                if (!mismatchedWorkspaces.Any()) return;

                var emailBody = new StringBuilder();
                emailBody.AppendLine("The following workspaces have 'PO' in their name but are not marked as Processing Only:");
                emailBody.AppendLine("<br><br>");
                emailBody.AppendLine("<table border='1'>");
                emailBody.AppendLine("<tr>");
                emailBody.AppendLine("<th>Workspace Name</th>");
                emailBody.AppendLine("<th>Current Status</th>");
                emailBody.AppendLine("<th>Created By</th>");
                emailBody.AppendLine("<th>Created On</th>");                
                emailBody.AppendLine("</tr>");

                foreach (var workspace in mismatchedWorkspaces)
                {
                    emailBody.AppendLine("<tr>");
                    emailBody.AppendLine($"<td>{workspace.EddsWorkspaceName}</td>");
                    emailBody.AppendLine($"<td>{workspace.EddsWorkspaceStatusName}</td>");
                    emailBody.AppendLine($"<td>{workspace.EddsWorkspaceCreatedBy}</td>");
                    emailBody.AppendLine($"<td>{workspace.EddsWorkspaceCreatedOn:yyyy-MM-dd HH:mm}</td>");                    
                    emailBody.AppendLine("</tr>");
                }

                emailBody.AppendLine("</table>");

                await MessageHandler.Email.SendInternalNotificationAsync(
                    _instanceSettings,
                    emailBody,
                    "Workspaces with PO Name/Status Mismatch");
            }
            catch (Exception ex)
            {
                _ltasHelper.Logger.LogError(ex, "Error handling Processing Only name/status mismatch check");
                throw;
            }
        }
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
                emailBody.AppendLine("</tr>");

                foreach (var workspace in processingOnlyWorkspaces)
                {
                    var ageInDays = (DateTime.Now - workspace.EddsWorkspaceCreatedOn).Days;
                    var ageInMonths = ageInDays / 30.44; // Average days in a month

                    string colorStyle = "";
                    if (ageInMonths > 10)
                        colorStyle = " color: red;";
                    else if (ageInMonths > 8)
                        colorStyle = " color: #FFD700;"; // Yellow

                    emailBody.AppendLine("<tr>");
                    emailBody.AppendLine($"<td style='padding: 8px;{(ageInMonths > 10 ? " font-weight: bold;" : "")}'>{workspace.EddsWorkspaceName}</td>");
                    emailBody.AppendLine($"<td style='padding: 8px;'>{workspace.EddsWorkspaceArtifactId}</td>");
                    emailBody.AppendLine($"<td style='padding: 8px;'>{workspace.EddsWorkspaceStatusName}</td>");
                    emailBody.AppendLine($"<td style='padding: 8px;'>{workspace.EddsWorkspaceCreatedBy}</td>");
                    emailBody.AppendLine($"<td style='padding: 8px;'>{workspace.EddsWorkspaceCreatedOn:yyyy-MM-dd HH:mm}</td>");
                    emailBody.AppendLine($"<td style='padding: 8px;{colorStyle}'>{ageInDays:N0}</td>");
                    emailBody.AppendLine($"<td style='padding: 8px;{colorStyle}'>{ageInMonths:N1}</td>");
                    emailBody.AppendLine("</tr>");
                }

                emailBody.AppendLine("</table>");
                emailBody.AppendLine("<p><small>* Age indicators:</small></p>");
                emailBody.AppendLine("<ul>");                
                emailBody.AppendLine($"<li><small style='color: #FFD700;'>Yellow: 8-10 months</small></li>");
                emailBody.AppendLine($"<li><small style='color: red;'>Red: Over 10 months</small></li>");
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
    }
}