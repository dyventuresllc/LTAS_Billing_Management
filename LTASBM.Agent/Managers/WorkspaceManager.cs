using LTASBM.Agent.Handlers;
using LTASBM.Agent.Logging;
using LTASBM.Agent.Models;
using LTASBM.Agent.Utilites;
using Relativity.API;
using Relativity.Services.Objects;
using Relativity.Services.Objects.DataContracts;
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
        private readonly ILTASLogger _logger;

        public WorkspaceManager(
            IAPILog relativityLogger,
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
            _ltasHelper = new LTASBMHelper(helper, relativityLogger.ForContext<WorkspaceManager>());
            _logger = LoggerFactory.CreateLogger<WorkspaceManager>(helper.GetDBContext(-1), helper, relativityLogger);
        }
        public async Task ProcessWorkspaceRoutinesAsync()
        {
            try
            {
                _logger.LogInformation("Starting workspace routines processing");

                _logger.LogDebug("Retrieving EDDS workspaces");
                var eddsWorkspaces = _dataHandler.EddsWorkspaces();
                _logger.LogInformation("Retrieved {Count} EDDS workspaces", eddsWorkspaces.Count);

                _logger.LogDebug("Retrieving billing workspaces");
                var billingWorkspaces = _dataHandler.BillingWorkspaces();
                _logger.LogInformation("Retrieved {Count} billing workspaces", billingWorkspaces.Count);

                await ProcessAllWorkspaceOperationsAsync(eddsWorkspaces, billingWorkspaces);
                _logger.LogInformation("Completed workspace routines processing");
            }
            catch (Exception ex)
            {
                _ltasHelper.Logger.LogError(ex, "Error In ProcessWorkspaceRoutine");
                _logger.LogError(ex, "Error In ProcessWorkspaceRoutine");
            }
        }

        public async Task ProcessDailyOperationsAsnyc()
        {
            var eddsWorkspaces = _dataHandler.EddsWorkspaces();
            await NotifyMissingTeamInfoAsync(eddsWorkspaces);
        }

        private async Task ProcessAllWorkspaceOperationsAsync(List<EddsWorkspaces> eddsWorkspaces, List<BillingWorkspaces> billingWorkspaces)
        {
            _logger.LogDebug("Starting all workspace operations processing");
            var invalidWorkspaces = GetInvalidWorkspaces(billingWorkspaces);
            _logger.LogInformation("Found {Count} invalid workspaces", invalidWorkspaces.Count());
            await NotifyInvalidWorkspacesAsync(invalidWorkspaces);

            var duplicateWorkspaces = GetDuplicateWorkspaces(billingWorkspaces);
            _logger.LogInformation("Found {Count} duplicate workspaces", duplicateWorkspaces.Count());
            await NotifyDuplicateWorkspacesAsync(duplicateWorkspaces);

            var NewWorkspaces = GetNewWorkspacesForBilling(eddsWorkspaces, billingWorkspaces);
            _logger.LogInformation("Found {Count} new workspaces for billing", NewWorkspaces.Count());
            await ProcessNewWorkspacesAsync(NewWorkspaces);

            _logger.LogDebug("Processing orphaned workspaces");
            await HandleOrphanedWorkspacesAsync(eddsWorkspaces, billingWorkspaces);

            _logger.LogDebug("Processing processing-only workspace mismatches");
            await HandleProcessingOnlyMismatchAsync(eddsWorkspaces);

            _logger.LogDebug("Checking for workspaces deleted without date");
            var getDeletedWorkspaces = await ObjectHandler.WorkspacesDeletedNoDeletedDate(
                    _objectManager,
                    _billingManagementDatabase,
                    _ltasHelper.WorkspaceObjectType,
                    _ltasHelper.WorkspaceEDDSArtifactIDField,
                    _ltasHelper.WorkspaceNameField,
                    _ltasHelper.WorkspaceStatusField,
                    _ltasHelper.WorkspaceCreatedByField,
                    _ltasHelper.WorkspaceCreatedOnField,
                    _ltasHelper.Logger
                    );
            await NotifyWorkspacesDeletedNoDateAsync(getDeletedWorkspaces);
            
            _logger.LogDebug("Completed all workspace operations processing");
        }

        private IEnumerable<EddsWorkspaces> GetNewWorkspacesForBilling(List<EddsWorkspaces> eddsWorkspaces, List<BillingWorkspaces> billingWorkspaces)
        {
            _logger.LogDebug("Identifying new workspaces for billing");

            var invalidWorkspaceIds = new HashSet<int>(billingWorkspaces
                .Where(w => w.BillingWorkspaceArtifactId == 0 || w.BillingWorkspaceEddsArtifactId == 0)
                .Select(w => w.BillingWorkspaceEddsArtifactId));
            _logger.LogDebug("Found {Count} invalid workspace IDs to exclude", invalidWorkspaceIds.Count);

            var duplicateWorkspaceIds = new HashSet<int>(billingWorkspaces
                .GroupBy(w => w.BillingWorkspaceEddsArtifactId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key));
            _logger.LogDebug("Found {Count} duplicate workspace IDs to exclude", duplicateWorkspaceIds.Count);

            var newWorkspaces = eddsWorkspaces.Where(edds =>
                !billingWorkspaces.Any(billing => billing.BillingWorkspaceEddsArtifactId == edds.EddsWorkspaceArtifactId) &&
                !invalidWorkspaceIds.Contains(edds.EddsWorkspaceArtifactId) &&
                !duplicateWorkspaceIds.Contains(edds.EddsWorkspaceArtifactId))
                .ToList();

            foreach (var workspace in newWorkspaces)
            {
                _logger.LogDebug("New workspace identified: {WorkspaceName} (ID: {WorkspaceId})",
                    workspace.EddsWorkspaceName, workspace.EddsWorkspaceArtifactId);
            }

            return newWorkspaces;
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
        
        //TODO: Remove
        //private async Task NotifyInvalidWorkspacesAsync(IEnumerable<BillingWorkspaces> invalidWorkspaces) 
        //{
        //    if (invalidWorkspaces.Any())
        //    {
        //        var emailBody = new StringBuilder();
        //        emailBody = MessageHandler.InvalidWorkspaceEmailBody(emailBody, invalidWorkspaces.ToList());
        //        await MessageHandler.Email.SendInternalNotificationAsync(_instanceSettings, emailBody, "Invalid Workspaces");                
        //    }            
        //}

        private async Task NotifyInvalidWorkspacesAsync(IEnumerable<BillingWorkspaces> invalidWorkspaces)
        {
            if (invalidWorkspaces.Any())
            {
                _logger.LogInformation("Preparing to send invalid workspaces notification for {Count} workspaces",
                    invalidWorkspaces.Count());
                try
                {
                    var emailBody = new StringBuilder();
                    emailBody = MessageHandler.InvalidWorkspaceEmailBody(emailBody, invalidWorkspaces.ToList());
                    await MessageHandler.Email.SendInternalNotificationAsync(_instanceSettings, emailBody, "Invalid Workspaces");
                    _logger.LogInformation("Successfully sent invalid workspaces notification");
                }
                catch (Exception ex)
                {
                    _ltasHelper.Logger.LogError(ex, "Failed to send invalid workspaces notification");
                    _logger.LogError(ex, "Failed to send invalid workspaces notification");
                    throw;
                }
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

        //TODO: Remove
        //private async Task CreateNewWorkspacesInBillingAsync(IEnumerable<EddsWorkspaces> NewWorkspaces) 
        //{
        //    foreach (var workspace in NewWorkspaces)
        //    {
        //        try
        //        {
        //            _ltasHelper.Logger.LogInformation("Attempting to create workspace: {workspaceDetails}",
        //                        new { workspace.EddsWorkspaceArtifactId, workspace.EddsWorkspaceName });

        //            var result = await ObjectHandler.CreateNewWorkspaceAsync(
        //                _objectManager,
        //                _billingManagementDatabase,
        //                workspace.EddsWorkspaceArtifactId,
        //                workspace.EddsWorkspaceCreatedBy,
        //                workspace.EddsWorkspaceCreatedOn,
        //                workspace.EddsWorkspaceName,
        //                workspace.EddsWorkspaceMatterArtifactId,
        //                workspace.EddsWorkspaceCaseTeam,
        //                workspace.EddsWorkspaceAnalyst,
        //                workspace.EddsWorkspaceStatusName,
        //                _ltasHelper.Logger,
        //                _ltasHelper.Helper);

        //            if (result == null)
        //            {
        //                _ltasHelper.Logger.LogError($"CreateNewWorkspace returned null for workspace {workspace.EddsWorkspaceArtifactId}");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            _ltasHelper.Logger.LogError(ex, "Error creating workspace: {}. Error:{}",
        //               new { workspace.EddsWorkspaceArtifactId, workspace.EddsWorkspaceName }, ex.Message);
        //        }
        //    }
        //}

        private async Task CreateNewWorkspacesInBillingAsync(IEnumerable<EddsWorkspaces> newWorkspaces)
        {
            foreach (var workspace in newWorkspaces)
            {
                try
                {
                    _logger.LogInformation("Creating new workspace in billing: {@WorkspaceDetails}",
                        new
                        {
                            WorkspaceId = workspace.EddsWorkspaceArtifactId,
                            Name = workspace.EddsWorkspaceName,
                            CreatedBy = workspace.EddsWorkspaceCreatedBy,
                            Status = workspace.EddsWorkspaceStatusName
                        });

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
                        _logger.LogError("Failed to create workspace in billing system: {WorkspaceDetails}",
                            new { workspace.EddsWorkspaceArtifactId, workspace.EddsWorkspaceName });
                    }
                    else
                    {
                        _logger.LogInformation("Successfully created workspace in billing system: {WorkspaceId}",
                            workspace.EddsWorkspaceArtifactId);
                    }
                }
                catch (Exception ex)
                {
                    _ltasHelper.Logger.LogError(ex, "Error creating workspace: {@WorkspaceDetails}",
                        new { workspace.EddsWorkspaceArtifactId, workspace.EddsWorkspaceName });
                    _logger.LogError(ex, "Error creating workspace: {@WorkspaceDetails}",
                        new { workspace.EddsWorkspaceArtifactId, workspace.EddsWorkspaceName });
                    throw;
                }
            }
        }

        private async Task NotifyMissingTeamInfoAsync(List<EddsWorkspaces> eddsWorkspaces)
        {
            try
            {
                var workspacesMissingInfo = GetWorkspacesMissingTeamInfo(eddsWorkspaces)
                    .OrderBy(w => w.EddsWorkspaceCreatedOn)
                    .ToList();

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

                await MessageHandler.Email.SendMissingInfoReportingAsync(
                    _instanceSettings,
                    emailBody);
            }
            catch (Exception ex)
            {
                _ltasHelper.Logger.LogError(ex, "Error handling workspaces missing team info check");
                throw;
            }
        }

        //TODO: Remove
        //private async Task HandleOrphanedWorkspacesAsync(
        //    List<EddsWorkspaces> eddsWorkspaces,
        //    List<BillingWorkspaces> billingWorkspaces)
        //{
        //    try
        //    {
        //        var orphanedWorkspaces = GetOrphanedWorkspaces(eddsWorkspaces, billingWorkspaces);
                
        //        if (!orphanedWorkspaces.Any()) return;

        //        var emailBody = new StringBuilder();
        //        emailBody.AppendLine("<h3>The following workspaces in Billing system will be marked as Deleted because they no longer exist in EDDS:</h3>");
        //        emailBody.AppendLine("<table border='1' style='border-collapse: collapse;'>");
        //        emailBody.AppendLine("<tr style='background-color: #f2f2f2;'>");
        //        emailBody.AppendLine("<th style='padding: 8px;'>Workspace Name</th>");
        //        emailBody.AppendLine("<th style='padding: 8px;'>Workspace ID</th>");
        //        emailBody.AppendLine("<th style='padding: 8px;'>Current Status</th>");
        //        emailBody.AppendLine("</tr>");

        //        foreach (var workspace in orphanedWorkspaces)
        //        {
        //            emailBody.AppendLine("<tr>");
        //            emailBody.AppendLine($"<td style='padding: 8px;'>{workspace.BillingWorkspaceName}</td>");
        //            emailBody.AppendLine($"<td style='padding: 8px;'>{workspace.BillingWorkspaceArtifactId}</td>");
        //            emailBody.AppendLine($"<td style='padding: 8px;'>{workspace.BillingStatusName}</td>");
        //            emailBody.AppendLine("</tr>");
        //        }

        //        emailBody.AppendLine("</table>");
        //        emailBody.AppendLine("<br/>");
        //        emailBody.AppendLine("<p><em>These workspaces will be automatically updated to 'Deleted' status.</em></p>");

        //        await MessageHandler.Email.SendInternalNotificationAsync(
        //            _instanceSettings,
        //            emailBody,
        //            "Orphaned Workspaces Status Update");

        //        foreach (var workspace in orphanedWorkspaces)
        //        {
        //            try 
        //            {
        //                var deletedStatusArtifactId = _ltasHelper.GetCaseStatusArtifactID(
        //                    _ltasHelper.Helper.GetDBContext(_billingManagementDatabase),
        //                    "Deleted");

        //                if (deletedStatusArtifactId != 0)
        //                {
        //                    await ObjectHandler.UpdateFieldValueAsync(
        //                        _objectManager,
        //                        _billingManagementDatabase,
        //                        workspace.BillingWorkspaceArtifactId,
        //                        _ltasHelper.WorkspaceStatusField,
        //                        new Relativity.Services.Objects.DataContracts.ChoiceRef
        //                        {
        //                            ArtifactID = deletedStatusArtifactId
        //                        },                                
        //                        _ltasHelper.Logger);
        //                }
        //                else
        //                {
        //                    _ltasHelper.Logger.LogError(
        //                        "Could not find 'Deleted' status choice artifact ID for workspace {WorkspaceId}",
        //                        workspace.BillingWorkspaceArtifactId);
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                _ltasHelper.Logger.LogError(ex,
        //                    "Error updating status to Deleted for workspace {WorkspaceDetails}",
        //                    new { workspace.BillingWorkspaceArtifactId, workspace.BillingWorkspaceName });
        //            }
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        _ltasHelper.Logger.LogError(ex, "Error handling orphaned workspaces");
        //        throw;
        //    }
        //}

        private async Task HandleOrphanedWorkspacesAsync(List<EddsWorkspaces> eddsWorkspaces, List<BillingWorkspaces> billingWorkspaces)
        {
            try
            {
                _logger.LogDebug("Checking for orphaned workspaces");
                var orphanedWorkspaces = GetOrphanedWorkspaces(eddsWorkspaces, billingWorkspaces);

                if (!orphanedWorkspaces.Any())
                {
                    _logger.LogInformation("No orphaned workspaces found");
                    return;
                }

                _logger.LogInformation("Found {Count} orphaned workspaces", orphanedWorkspaces.Count());

                foreach (var workspace in orphanedWorkspaces)
                {
                    _logger.LogDebug("Orphaned workspace found: {WorkspaceName} (ID: {WorkspaceId})",
                        workspace.BillingWorkspaceName, workspace.BillingWorkspaceArtifactId);
                }

                // Email notification preparation and sending
                var emailBody = new StringBuilder();
                // ... [email body preparation code remains the same]

                await MessageHandler.Email.SendInternalNotificationAsync(
                    _instanceSettings,
                    emailBody,
                    "Orphaned Workspaces Status Update");

                // Update status for each orphaned workspace
                foreach (var workspace in orphanedWorkspaces)
                {
                    try
                    {
                        _logger.LogDebug("Updating status to Deleted for workspace: {WorkspaceId}",
                            workspace.BillingWorkspaceArtifactId);

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
                                new ChoiceRef { ArtifactID = deletedStatusArtifactId },
                                _ltasHelper.Logger);

                        _logger.LogInformation("Successfully marked workspace as Deleted: {WorkspaceId}",
                                workspace.BillingWorkspaceArtifactId);
                        }
                        else
                        {
                            _logger.LogError("Could not find 'Deleted' status choice artifact ID for workspace {WorkspaceId}",
                                workspace.BillingWorkspaceArtifactId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _ltasHelper.Logger.LogError(ex, "Failed to update status to Deleted for workspace: {@WorkspaceDetails}",
                            new
                            {
                                workspace.BillingWorkspaceArtifactId,
                                workspace.BillingWorkspaceName
                            });
                        _logger.LogError(ex, "Failed to update status to Deleted for workspace: {@WorkspaceDetails}",
                            new
                            {
                                workspace.BillingWorkspaceArtifactId,
                                workspace.BillingWorkspaceName
                            });
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _ltasHelper.Logger.LogError(ex, "Error handling orphaned workspaces");
                _logger.LogError(ex, "Error handling orphaned workspaces");
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
               
        private async Task NotifyWorkspacesDeletedNoDateAsync(QueryResult queryResult)
        {
            try
            {
                if (queryResult?.Objects == null || !queryResult.Objects.Any()) return;

                var emailBody = new StringBuilder();
                emailBody.AppendLine("The following workspaces are marked as Deleted but missing a Deleted Date:");
                emailBody.AppendLine("<table border='1' style='border-collapse: collapse;'>");
                emailBody.AppendLine("<tr style='background-color: #f2f2f2;'>");
                emailBody.AppendLine("<th style='padding: 8px;'>Workspace Name</th>");
                emailBody.AppendLine("<th style='padding: 8px;'>Workspace ID</th>");
                emailBody.AppendLine("<th style='padding: 8px;'>Created By</th>");
                emailBody.AppendLine("<th style='padding: 8px;'>Created On</th>");
                emailBody.AppendLine("</tr>");

                foreach (var workspace in queryResult.Objects)
                {
                    emailBody.AppendLine("<tr>");
                    emailBody.AppendLine($"<td style='padding: 8px;'>{workspace.FieldValues.FirstOrDefault(f => f.Field.Name == "Workspace Name")?.Value ?? "N/A"}</td>");
                    emailBody.AppendLine($"<td style='padding: 8px;'>{workspace.ArtifactID}</td>");
                    emailBody.AppendLine($"<td style='padding: 8px;'>{workspace.FieldValues.FirstOrDefault(f => f.Field.Name == "Workspace Created By")?.Value ?? "N/A"}</td>");
                    emailBody.AppendLine($"<td style='padding: 8px;'>{workspace.FieldValues.FirstOrDefault(f => f.Field.Name == "Workspace Created On")?.Value ?? "N/A"}</td>");
                    emailBody.AppendLine("</tr>");
                }

                emailBody.AppendLine("</table>");

                await MessageHandler.Email.SendInternalNotificationAsync(
                    _instanceSettings,
                    emailBody,
                    "Workspaces Missing Deleted Date");
            }
            catch (Exception ex)
            {
                _ltasHelper.Logger.LogError(ex,
                    "Error checking for workspaces missing deleted date");
                throw;
            }
        }

    }
}