using kCura.Vendor.Castle.Core.Logging;
using LTASBM.Agent.Handlers;
using LTASBM.Agent.Models;
using LTASBM.Agent.Utilites;
using Relativity.API;
using Relativity.Services.Objects;
using Relativity.Services.Objects.DataContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace LTASBM.Agent.Routines
{
    public class WorkspaceRoutine
    {
        private readonly IAPILog _logger;
        private readonly LTASBMHelper _ltasHelper;

        public WorkspaceRoutine(IAPILog logger, IHelper helper)
        {
            _logger = logger.ForContext<WorkspaceRoutine>();
            _ltasHelper = new LTASBMHelper(helper, logger);
        }
        
        public async Task ProcessWorkspaceRoutines(int billingManagementDatabase, IObjectManager objectManager, DataHandler dataHandler, IInstanceSettingsBundle instanceSettings)
        {
            StringBuilder emailBody = new StringBuilder();
            
            try
            {
                var eddsWorkspaces = dataHandler.EddsWorkspaces();
                var billingWorkspaces = dataHandler.BillingWorkspaces();
                InvalidWorkspaces(billingWorkspaces, instanceSettings, emailBody);
                DuplicateWorkspaces(billingWorkspaces, instanceSettings, emailBody);
                await ProcessNewWorkspacesToBillingAsync(objectManager, billingManagementDatabase, emailBody, eddsWorkspaces, billingWorkspaces, instanceSettings);
            }
            catch (Exception ex)
            {                
                _logger.LogError(ex, "Error In ProcessWorkspaceRoutine");
            }
        }
        
        private void InvalidWorkspaces (List<BillingWorkspaces> billingWorkspaces, IInstanceSettingsBundle instanceSettings, StringBuilder emailBody)
        {
            emailBody.Clear();
            var invalidWorkspaces = billingWorkspaces
                .Where(w => w.BillingWorkspaceArtifactId == 0 || w.BillingWorkspaceEddsArtifactId == 0)
                .ToList();                   

            if (invalidWorkspaces.Any()) 
            {
                emailBody = MessageHandler.InvalidWorkspaceEmailBody(emailBody, invalidWorkspaces);
                MessageHandler.Email.SendDebugEmail(instanceSettings, emailBody,"Invalid Workspaces");                
            }
        }

        private void DuplicateWorkspaces(List<BillingWorkspaces> billingWorkspaces, IInstanceSettingsBundle instanceSettings, StringBuilder emailBody)
        {
            emailBody.Clear();
            var duplicateWorkspaces = billingWorkspaces
                .GroupBy(w => w.BillingWorkspaceEddsArtifactId)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g)
                .ToList();

            if (duplicateWorkspaces.Any())
            {
                emailBody = MessageHandler.DuplicateWorkspacesEmailBody(emailBody, duplicateWorkspaces);
                MessageHandler.Email.SendDebugEmail(instanceSettings, emailBody, "Duplicate Workspaces Found");
            }
        }

        private async Task ProcessNewWorkspacesToBillingAsync(IObjectManager objectManager, int billingManagementDatabase, StringBuilder emailBody, List<EddsWorkspaces> eddsWorkspaces, List<BillingWorkspaces> billingWorkspaces, IInstanceSettingsBundle instanceSettings)
        {
            //Handle new workspaces that need to be added to billing system
            var missingInBilling = eddsWorkspaces
                .Where(edds => !billingWorkspaces
                .Any(billing => billing.BillingWorkspaceEddsArtifactId == edds.EddsWorkspaceArtifactId))
                .ToList();
            if (missingInBilling.Any()) 
            {
                emailBody.Clear();
                emailBody = MessageHandler.NewWorkspacesEmailBody(emailBody, missingInBilling);
                MessageHandler.Email.SendDebugEmail(instanceSettings, emailBody, "New Workspaces");

                foreach(var record in missingInBilling) 
                {
                    try
                    {
                        _logger.LogInformation("Attempting to create workspace: {workspaceDetails}",
                                    new { record.EddsWorkspaceArtifactId, record.EddsWorkspaceName });

                        //CreateResult result = await ObjectHandler.CreateNewWorkspace(
                        //    objectManager,
                        //    billingManagementDatabase,
                        //    record.EddsWorkspaceArtifactId,
                        //    record.EddsWorkspaceCreatedBy,
                        //    record.EddsWorkspaceCreatedOn,
                        //    record.EddsWorkspaceName,
                        //    record.EddsWorkspaceMatterArtifactId,
                        //    record.EddsWorkspaceCaseTeam,
                        //    record.EddsWorkspaceAnalyst,
                        //    record.EddsWorkspaceStatusName,
                        //    _logger,
                        //    _ltasHelper.Helper);

                        //if (result == null)
                        //{
                        //    _logger.LogError($"CreateNewWorkspace returned null for workspace {record.EddsWorkspaceArtifactId}");
                        //}
                    }
                    catch (Exception ex) 
                    {
                        _logger.LogError(ex, "Error creating workspace: {}. Error:{}",
                           new { record.EddsWorkspaceArtifactId, record.EddsWorkspaceName }, ex.Message);
                    }
                }
            }
        }
    }
}