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
    public class DataSyncManager
    {        
        private readonly IObjectManager _objectManager;
        private readonly IInstanceSettingsBundle _instanceSettings;
        private readonly int _billingManagementDatabase;
        private readonly LTASBMHelper _ltasHelper;    
        private readonly DataHandler _dataHandler;

        public DataSyncManager(
            IAPILog logger,
            IHelper helper,
            IObjectManager objectManager,
            IInstanceSettingsBundle instanceSettings,    
            DataHandler dataHandler,
            int billingManagementDatabase) 
        {           
            _dataHandler = dataHandler ?? throw new ArgumentNullException(nameof(dataHandler));
            _objectManager = objectManager ?? throw new ArgumentNullException(nameof(objectManager));
            _instanceSettings = instanceSettings ?? throw new ArgumentNullException(nameof(instanceSettings));
            _ltasHelper = new LTASBMHelper(helper, logger.ForContext<DataSyncManager>());
            _billingManagementDatabase = billingManagementDatabase;
        }

        public enum UpdateType
        {
            MatterName,
            MatterNumber,
            MatterClientObject,
            ClientName,
            ClientNumber,
            WorkspaceName,
            WorkspaceCreatedBy,
            WorkspaceCreatedOn,
            WorkspaceCaseTeam,
            WorkspaceAnalyst,
            WorkspaceStatus
        }

        public async Task ProcessDataSyncRoutinesAsync()
        {
            try
            {      
                var eddsClients = _dataHandler.EDDSClients();
                var billingClients = _dataHandler.BillingClients();
                var eddsMatters = _dataHandler.EddsMatters();
                var billingMatters = _dataHandler.BillingMatters();
                var eddsWorkspaces = _dataHandler.EddsWorkspaces();
                var billingWorkspaces = _dataHandler.BillingWorkspaces();

                await ProccessAllDataSyncOperationsAsync(
                    eddsClients, billingClients, 
                    eddsMatters, billingMatters, 
                    eddsWorkspaces, billingWorkspaces);
            }
            catch (Exception ex) 
            {
                _ltasHelper.Logger.LogError(ex, "Error In DataSyncRoutine");
            }      
        }

        private async Task ProccessAllDataSyncOperationsAsync(
            List<EddsClients>eddsclients, List<BillingClients>billingClients, 
            List<EddsMatters> eddsMatters, List<BillingMatters> billingMatters, 
            List<EddsWorkspaces> eddsWorkspaces, List<BillingWorkspaces> billingWorkspaces)
        {
            var clientNameUpdates = GetClientNameUpdates(eddsclients, billingClients);
            await NotifyClientNameUpdatesAsync(clientNameUpdates);

            var clientNumberUpdates = GetClientNumberUpdates(eddsclients, billingClients);
            await NotifyClientNumberUpdatesAsync(clientNumberUpdates);

            var matterNameUpdates = GetMatterNameUpdates(eddsMatters, billingMatters);
            await NotifyMatterNameUpdatesAsync(matterNameUpdates);

            var matterNumberUpdates = GetMatterNumberUpdates(eddsMatters, billingMatters);
            await NotifyMatterNumberUpdatesAsync(matterNumberUpdates);

            var matterClientObjectUpdates = GetMatterClientUpdates(eddsMatters, billingMatters, billingClients);
            await NotifyMatterClientObjectUpdatesAsync(matterClientObjectUpdates);

            var workspaceNameUpdates = GetWorkspaceNameUpdates(eddsWorkspaces, billingWorkspaces);
            await NotifyWorkspaceNameUpdatesAsync(workspaceNameUpdates);

            var workspaceCreatedByUpdates = GetWorkspaceCreatedByUpdates(eddsWorkspaces, billingWorkspaces);
            await NotifyWorkspaceCreatedByUpdatesAsync(workspaceCreatedByUpdates);

            var workspaceCreatedOnUpdates = GetWorkspaceCreatedOnUpdates(eddsWorkspaces, billingWorkspaces);
            await NotifyWorkspaceCreatedOnUpdatesAsync(workspaceCreatedOnUpdates);

            var workspaceCaseTeamUpdates = GetWorkspaceCaseTeamUpdates(eddsWorkspaces, billingWorkspaces);
            await NotifyWorkspaceCaseTeamUpdatesAsync(workspaceCaseTeamUpdates);

            var workspaceAnalystUpdates = GetWorkspaceAnalystUpdates(eddsWorkspaces, billingWorkspaces);
            await NotifyWorkspaceAnalystUpdatesAsync(workspaceAnalystUpdates);

            var workspaceStatusUpdates = await GetWorkspaceStatusUpdates(eddsWorkspaces, billingWorkspaces);            
            await NotifyWorkspaceStatusUpdateAsync(workspaceStatusUpdates);
        }

        private IEnumerable<(int BillingClientArtifactId, string EddsClientName)> GetClientNameUpdates(
            List<EddsClients> eddsClients,
            List<BillingClients> billingClients)
            => billingClients
           .Join(
               eddsClients,
               billing => billing.BillingEddsClientArtifactId,
               edds => edds.EddsClientArtifactId,
               (billing, edds) => new
               {
                   BillingClientArtifactId = billing.BillingClientArtifactID,
                   BillingClientName = billing.BillingEddsClientName,
                   edds.EddsClientName
               })
           .Where(result => result.BillingClientName != result.EddsClientName)
           .Select(result => (result.BillingClientArtifactId, result.EddsClientName))
           .ToList();
        
        private IEnumerable<(int BillingClientArtifactId, string EddsClientNumber)> GetClientNumberUpdates(
            List<EddsClients> eddsClients,
            List<BillingClients> billingClients)
            => billingClients
           .Join(
               eddsClients,
               billing => billing.BillingEddsClientArtifactId,
               edds => edds.EddsClientArtifactId,
               (billing, edds) => new
               {
                   BillingClientArtifactId = billing.BillingClientArtifactID,
                   BillingClientNumber = billing.BillingEddsClientNumber,
                   edds.EddsClientNumber
               })
           .Where(result => result.BillingClientNumber != result.EddsClientNumber)
           .Select(result => (result.BillingClientArtifactId, result.EddsClientNumber))
           .ToList();

        private IEnumerable<(int BillingMatterArtifactID, string EddsMatterName)> GetMatterNameUpdates(
            List<EddsMatters> eddsMatters,
            List<BillingMatters> billingMatters)
            => billingMatters
            .Join(
                eddsMatters,
                billing => billing.BillingEddsMatterArtifactId,
                edds => edds.EddsMatterArtifactId,
                (billing, edds) => new 
                {
                    BillingMatterArtifactId = billing.BillingMatterArtficatId,
                    BillingMatterName = billing.BillingEddsMatterName,
                    edds.EddsMatterName
                })
            .Where(result => result.BillingMatterName != result.EddsMatterName)
            .Select(result => (result.BillingMatterArtifactId, result.EddsMatterName))
            .ToList();

        private IEnumerable<(int BillingMatterArtifactID, string EddsMatterNumber)> GetMatterNumberUpdates(
            List<EddsMatters> eddsMatters,
            List<BillingMatters> billingMatters)
            => billingMatters
            .Join(
                eddsMatters,
                billing => billing.BillingEddsMatterArtifactId,
                edds => edds.EddsMatterArtifactId,
                (billing, edds) => new
                {
                    BillingMatterArtifactId = billing.BillingMatterArtficatId,
                    BillingMatterNumber = billing.BillingEddsMatterNumber,
                    edds.EddsMatterNumber
                })
            .Where(result => result.BillingMatterNumber != result.EddsMatterNumber)
            .Select(result => (result.BillingMatterArtifactId, result.EddsMatterNumber))
            .ToList();

        private IEnumerable<(int BillingMatterArtifactID, int NewClientArtifactId)> GetMatterClientUpdates(
            List<EddsMatters> eddsMatters,
            List<BillingMatters> billingMatters,
            List<BillingClients> billingClients)
            => billingMatters
            .Join(
                  eddsMatters,
                  billing => billing.BillingEddsMatterArtifactId,
                  edds => edds.EddsMatterArtifactId,
                  (billing, edds) => new
                  {
                      BillingMatterArtifactId = billing.BillingMatterArtficatId,
                      CurrentClientObjectValue = billing.BillingClientId,
                      // Get the client number from EDDS Matter to find corresponding billing client
                      EddsClientNumber = edds.EddsMatterNumber.Substring(0, 5)  // Assuming first 5 digits are client number
                  })
            .Join(
                  billingClients,
                  matter => matter.EddsClientNumber,
                  client => client.BillingEddsClientNumber,
                  (matter, client) => new
                  {
                      matter.BillingMatterArtifactId,
                      matter.CurrentClientObjectValue,
                      NewClientObjectValue = client.BillingClientArtifactID
                  })
            .Where(result => result.CurrentClientObjectValue != result.NewClientObjectValue)
            .Select(result => (
                result.BillingMatterArtifactId,
                result.NewClientObjectValue))
            .ToList();

        private IEnumerable<(int BillingWorkspaceArtifactId, string EddsWorkspaceName)> GetWorkspaceNameUpdates(
            List<EddsWorkspaces> eddsWorkspaces,
            List<BillingWorkspaces> billingWorkspaces)
            => billingWorkspaces
            .Join(
            eddsWorkspaces,
            billing => billing.BillingWorkspaceEddsArtifactId,
            edds => edds.EddsWorkspaceArtifactId,
            (billing, edds) => new
            {
                billing.BillingWorkspaceArtifactId,
                billing.BillingWorkspaceName,
                edds.EddsWorkspaceName
            })
        .Where(result => result.BillingWorkspaceName != result.EddsWorkspaceName)
        .Select(result => (result.BillingWorkspaceArtifactId, result.EddsWorkspaceName))
        .ToList();

        private IEnumerable<(int BillingWorkspaceArtifactId, string EddsWorkspaceCreatedBy)> GetWorkspaceCreatedByUpdates(
            List<EddsWorkspaces> eddsWorkspaces,
            List<BillingWorkspaces> billingWorkspaces)
            => billingWorkspaces
            .Join(
                eddsWorkspaces,
                billing => billing.BillingWorkspaceEddsArtifactId,
                edds => edds.EddsWorkspaceArtifactId,
                (billing, edds) => new
                {
                    billing.BillingWorkspaceArtifactId,
                    billing.BillingWorkspaceCreatedBy,
                    edds.EddsWorkspaceCreatedBy
                })
            .Where(result => result.BillingWorkspaceCreatedBy != result.EddsWorkspaceCreatedBy)
            .Select(result => (result.BillingWorkspaceArtifactId, result.EddsWorkspaceCreatedBy))
            .ToList();

        private IEnumerable<(int BillingWorkspaceArtifactId, DateTime EddsWorkspaceCreatedOn)> GetWorkspaceCreatedOnUpdates(
            List<EddsWorkspaces> eddsWorkspaces,
            List<BillingWorkspaces> billingWorkspaces)
            => billingWorkspaces
            .Join(
                eddsWorkspaces,
                billing => billing.BillingWorkspaceEddsArtifactId,
                edds => edds.EddsWorkspaceArtifactId,
                (billing, edds) => new
                {
                    billing.BillingWorkspaceArtifactId,
                    billing.BillingWorkspaceCreatedOn,
                    edds.EddsWorkspaceCreatedOn
                })
            .Where(result => result.BillingWorkspaceCreatedOn != result.EddsWorkspaceCreatedOn)
            .Select(result => (result.BillingWorkspaceArtifactId, result.EddsWorkspaceCreatedOn))
            .ToList();

        private IEnumerable<(int BillingWorkspaceArtifactId, string EddsWorkspaceCaseTeam)> GetWorkspaceCaseTeamUpdates(
            List<EddsWorkspaces> eddsWorkspaces,
            List<BillingWorkspaces> billingWorkspaces)
            => billingWorkspaces
            .Join(
                eddsWorkspaces,
                billing => billing.BillingWorkspaceEddsArtifactId,
                edds => edds.EddsWorkspaceArtifactId,
                (billing, edds) => new
                {
                    billing.BillingWorkspaceArtifactId,
                    billing.BillingWorkspaceCaseTeam,
                    edds.EddsWorkspaceCaseTeam
                })
            .Where(result => result.BillingWorkspaceCaseTeam != result.EddsWorkspaceCaseTeam)
            .Select(result => (result.BillingWorkspaceArtifactId, result.EddsWorkspaceCaseTeam))
            .ToList();

        private IEnumerable<(int BillingWorkspaceArtifactId, string EddsWorkspaceAnalyst)> GetWorkspaceAnalystUpdates(
            List<EddsWorkspaces> eddsWorkspaces,
            List<BillingWorkspaces> billingWorkspaces)
            => billingWorkspaces
            .Join(
                eddsWorkspaces,
                billing => billing.BillingWorkspaceEddsArtifactId,
                edds => edds.EddsWorkspaceArtifactId,
                (billing, edds) => new
                {
                    billing.BillingWorkspaceArtifactId,
                    billing.BillingWorkspaceAnalyst,
                    edds.EddsWorkspaceAnalyst
                })
            .Where(result => result.BillingWorkspaceAnalyst != result.EddsWorkspaceAnalyst)
            .Select(result => (result.BillingWorkspaceArtifactId, result.EddsWorkspaceAnalyst))
            .ToList();

        private async Task<IEnumerable<(int BillingWorkspaceArtifactId, string CurrentStatus, string NewStatus, int NewStatusChoiceArtifactId)>> GetWorkspaceStatusUpdates(
            List<EddsWorkspaces> eddsWorkspaces,
            List<BillingWorkspaces> billingWorkspaces)
        {
            var statusUpdates = billingWorkspaces
                .Join(
                    eddsWorkspaces,
                    billing => billing.BillingWorkspaceEddsArtifactId,
                    edds => edds.EddsWorkspaceArtifactId,
                    (billing, edds) => new
                    {
                        billing.BillingWorkspaceArtifactId,
                        CurrentStatus = billing.BillingStatusName,
                        NewStatus = edds.EddsWorkspaceStatusName
                    })
                .Where(result => result.CurrentStatus != result.NewStatus)
                .ToList();

            var updates = new List<(int, string, string, int)>();
            foreach (var status in statusUpdates)
            {
                var choiceArtifactId = _ltasHelper.GetCaseStatusArtifactID(
                    _ltasHelper.Helper.GetDBContext(_billingManagementDatabase),
                    status.NewStatus);

                if (choiceArtifactId != 0)
                {
                    updates.Add((
                        status.BillingWorkspaceArtifactId,
                        status.CurrentStatus,
                        status.NewStatus,
                        choiceArtifactId));
                }
                else
                {
                    _ltasHelper.Logger.LogError(
                        "Could not find choice artifact ID for status {StatusName} on workspace {WorkspaceId}",
                        status.NewStatus,
                        status.BillingWorkspaceArtifactId);
                }
            }

            return updates;
        }

        private async Task NotifyClientNameUpdatesAsync(IEnumerable<(int BillingClientArtifactId, string EddsClientName)> clientNameUpdates)
        {
            if (!clientNameUpdates.Any()) return;

            var emailBody = new StringBuilder();
            emailBody = MessageHandler.DataSyncNotificationEmailBody(emailBody, clientNameUpdates, "Client", "Client Name");
            await MessageHandler.Email.SendInternalNotificationAsync(
                _instanceSettings,
                emailBody,
                "Client Name Updates Required");

            await UpdateObjectValueTypeAsync(clientNameUpdates, UpdateType.ClientName);
        }
        private async Task NotifyClientNumberUpdatesAsync(IEnumerable<(int BillingClientArtifactId, string EddsClientName)> clientNumberUpdates)
        {
            if (!clientNumberUpdates.Any()) return;

            var emailBody = new StringBuilder();
            emailBody = MessageHandler.DataSyncNotificationEmailBody(emailBody, clientNumberUpdates, "Client", UpdateType.ClientNumber.ToString());
            await MessageHandler.Email.SendInternalNotificationAsync(
                _instanceSettings,
                emailBody,
                "Client Number Updates Required");

            await UpdateObjectValueTypeAsync(clientNumberUpdates, UpdateType.ClientNumber);
        }
        private async Task NotifyMatterNameUpdatesAsync(IEnumerable<(int BillingMatterArtifactID, string EddsMatterName)> matterNameUpdates)
        {          
            if (matterNameUpdates.Any())
            {
                var emailBody = new StringBuilder();
                emailBody = MessageHandler.DataSyncNotificationEmailBody(emailBody, matterNameUpdates, "Matter", UpdateType.MatterName.ToString());
                await MessageHandler.Email.SendInternalNotificationAsync(
                    _instanceSettings,
                    emailBody,
                    "Matter Name Updates Required");
                
                await UpdateObjectValueTypeAsync(matterNameUpdates, UpdateType.MatterName);
            }
        }
        private async Task NotifyMatterNumberUpdatesAsync(IEnumerable<(int BillingMatterArtifactID, string EddsNumberName)> matterNumberUpdates)
        {           
            if (matterNumberUpdates.Any())
            {
                var emailBody = new StringBuilder();
                emailBody = MessageHandler.DataSyncNotificationEmailBody(
                    emailBody, 
                    matterNumberUpdates, 
                    "Matter", 
                    UpdateType.MatterNumber.ToString());

                await MessageHandler.Email.SendInternalNotificationAsync(
                    _instanceSettings,
                    emailBody,
                    "Matter Number Updates Required");

                await UpdateObjectValueTypeAsync(matterNumberUpdates, UpdateType.MatterNumber);
            }
        }
        private async Task NotifyMatterClientObjectUpdatesAsync(IEnumerable<(int BillingMatterArtifactID, int NewClientArtifactId)> matterClientObjectUpdates)
        {
            if (matterClientObjectUpdates.Any()) 
            {
                var emailBody = new StringBuilder();
                var convertedMatterClientObjectUpdates = matterClientObjectUpdates
                    .Select(update => (update.BillingMatterArtifactID, update.NewClientArtifactId.ToString()))
                    .ToList();

                emailBody = MessageHandler.DataSyncNotificationEmailBody(
                    emailBody, convertedMatterClientObjectUpdates, "Matter", UpdateType.MatterClientObject.ToString());
            }                        
        }

        private async Task NotifyWorkspaceNameUpdatesAsync(IEnumerable<(int BillingWorkspaceArtifactId, string EddsWorkspaceName)> workspaceNameUpdates)
        { 
            if(workspaceNameUpdates.Any()) 
            {
                var emailBody = new StringBuilder();
                emailBody = MessageHandler.DataSyncNotificationEmailBody(
                    emailBody, 
                    workspaceNameUpdates, 
                    "Workspace", 
                    UpdateType.WorkspaceName.ToString());

                await MessageHandler.Email.SendInternalNotificationAsync(
                    _instanceSettings,
                    emailBody,
                    "Workspace Name Updates Required");

                await UpdateObjectValueTypeAsync(workspaceNameUpdates, UpdateType.WorkspaceName);
            }
        }
        private async Task NotifyWorkspaceCreatedByUpdatesAsync(IEnumerable<(int BillingWorkspaceArtifactId, string EddsWorkspaceCreatedBy)> workspaceCreatedByUpdates)
        {
            if (workspaceCreatedByUpdates.Any())
            {
                var emailBody = new StringBuilder();
                emailBody = MessageHandler.DataSyncNotificationEmailBody(
                    emailBody,
                    workspaceCreatedByUpdates,
                    "Workspace",
                    UpdateType.WorkspaceCreatedBy.ToString());

                await MessageHandler.Email.SendInternalNotificationAsync(
                    _instanceSettings,
                    emailBody,
                    "Workspace CreatedBy Updates Required");

                await UpdateObjectValueTypeAsync(workspaceCreatedByUpdates, UpdateType.WorkspaceCreatedBy);
            }
        }
        private async Task NotifyWorkspaceCreatedOnUpdatesAsync(IEnumerable<(int BillingWorkspaceArtifactId, DateTime EddsWorkspaceCreatedOn)> workspaceCreatedOnUpdates)
        {
            if (workspaceCreatedOnUpdates.Any())
            {
                var emailBody = new StringBuilder();

                var emailUpdates = workspaceCreatedOnUpdates
                    .Select(update => (
                        update.BillingWorkspaceArtifactId,
                        EddsValue: update.EddsWorkspaceCreatedOn.ToString("yyyy-MM-dd HH:mm:ss")))
                    .ToList();

                emailBody = MessageHandler.DataSyncNotificationEmailBody(
                    emailBody,
                    emailUpdates,
                    "Workspace",
                    UpdateType.WorkspaceCreatedOn.ToString());

                await MessageHandler.Email.SendInternalNotificationAsync(
                    _instanceSettings,
                    emailBody,
                    "Workspace CreatedOn Updates Required");
               
                await UpdateObjectDateTimeTypeAsync(workspaceCreatedOnUpdates, UpdateType.WorkspaceCreatedOn);
            }
        }
        private async Task NotifyWorkspaceCaseTeamUpdatesAsync(IEnumerable<(int BillingWorkspaceArtifactId, string EddsWorkspaceCaseTeam)> workspaceCaseTeamUpdates)
        {
            if (workspaceCaseTeamUpdates.Any())
            {
                var emailBody = new StringBuilder();
                emailBody = MessageHandler.DataSyncNotificationEmailBody(
                    emailBody,
                    workspaceCaseTeamUpdates,
                    "Workspace",
                    UpdateType.WorkspaceCaseTeam.ToString());

                await MessageHandler.Email.SendInternalNotificationAsync(
                    _instanceSettings,
                    emailBody,
                    "Workspace CaseTeam Updates Required");

                await UpdateObjectValueTypeAsync(workspaceCaseTeamUpdates, UpdateType.WorkspaceCaseTeam);
            }
        }
        private async Task NotifyWorkspaceAnalystUpdatesAsync(IEnumerable<(int BillingWorkspaceArtifactId, string EddsWorkspaceAnalyst)> workspaceAnalystUpdates)
        {
            if (workspaceAnalystUpdates.Any())
            {
                var emailBody = new StringBuilder();
                emailBody = MessageHandler.DataSyncNotificationEmailBody(
                    emailBody,
                    workspaceAnalystUpdates,
                    "Workspace",
                    UpdateType.WorkspaceAnalyst.ToString());

                await MessageHandler.Email.SendInternalNotificationAsync(
                    _instanceSettings,
                    emailBody,
                    "Workspace Analyst Updates Required");

                await UpdateObjectValueTypeAsync(workspaceAnalystUpdates, UpdateType.WorkspaceAnalyst);
            }
        }


        private async Task UpdateObjectValueTypeAsync(
            IEnumerable<(int BillingArtifactId, string EddsValue)> updates, 
            UpdateType updateType)
        {
            try 
            {
                foreach (var (BillingArtifactId, EddsValue) in updates)
                {
                    Guid fieldGuid;
                    switch(updateType)
                    {
                        case UpdateType.MatterName:
                            fieldGuid = _ltasHelper.MatterNameField;
                            break;
                        case UpdateType.MatterNumber:
                            fieldGuid = _ltasHelper.MatterNumberField;
                            break;
                        case UpdateType.ClientName:
                            fieldGuid = _ltasHelper.ClientNameField;
                            break;
                        case UpdateType.ClientNumber:
                            fieldGuid = _ltasHelper.ClientNumberField;
                            break;
                        case UpdateType.WorkspaceName:
                            fieldGuid= _ltasHelper.WorkspaceNameField;
                            break;
                        case UpdateType.WorkspaceCreatedBy:
                            fieldGuid = _ltasHelper.WorkspaceCreatedByField;
                            break;
                        case UpdateType.WorkspaceCreatedOn:
                            fieldGuid = _ltasHelper.WorkspaceCreatedOnField;
                            break;
                        case UpdateType.WorkspaceCaseTeam:
                            fieldGuid = _ltasHelper.WorkspaceCaseTeamField;
                            break;
                        case UpdateType.WorkspaceAnalyst:
                            fieldGuid = _ltasHelper.WorkspaceLtasAnalystField;
                            break;                        
                        default:
                            throw new ArgumentException($"{updateType} is not supported.");
                    }

                    await ObjectHandler.UpdateFieldValueAsync(
                        _objectManager,
                        _billingManagementDatabase,
                        BillingArtifactId,
                        fieldGuid,
                        EddsValue,                        
                        _ltasHelper.Logger);
                }
            }
            catch (Exception ex)
            {
                _ltasHelper.Logger.LogError(ex, "Error updating {UpdateType}", updateType);
                throw;
            }
        }

        private async Task UpdateObjectObjectTypeAsync(
            IEnumerable<(int BillingMatterArtifactID, int NewClientArtifactId)> updates,
            UpdateType updateType)
        {
            try 
            {
                foreach(var (BillingArtifactId, ObjectValue) in updates) 
                {
                    Guid fieldGuid;
                    switch(updateType) 
                    {
                        case UpdateType.MatterClientObject:
                            fieldGuid = _ltasHelper.MatterClientObjectField; 
                            break;
                        default:
                            throw new ArgumentException($"{updateType} is not supported.");
                    }
                    await ObjectHandler.UpdateFieldValueAsync(
                        _objectManager,
                        _billingManagementDatabase,
                        BillingArtifactId,
                        fieldGuid,
                        new Relativity.Services.Objects.DataContracts.RelativityObjectRef
                        {
                            ArtifactID = ObjectValue 
                        },
                        _ltasHelper.Logger);
                }
            }
            catch (Exception ex) 
            {
                _ltasHelper.Logger.LogError(ex, "Error updating {UpdateType}", updateType);
                throw;
            }
        }

        private async Task UpdateObjectDateTimeTypeAsync(
            IEnumerable<(int BillingArtifactId, DateTime EddsValue)> updates,
            UpdateType updateType)
        {
            try
            {
                foreach (var (BillingArtifactId, EddsValue) in updates)
                {
                    Guid fieldGuid;
                    switch (updateType)
                    {
                        case UpdateType.WorkspaceCreatedOn:
                            fieldGuid = _ltasHelper.WorkspaceCreatedOnField;
                            break;
                        default:
                            throw new ArgumentException($"{updateType} is not supported for DateTime updates.");
                    }

                    await ObjectHandler.UpdateFieldValueAsync(
                        _objectManager,
                        _billingManagementDatabase,
                        BillingArtifactId,
                        fieldGuid,
                        EddsValue,  
                        _ltasHelper.Logger);
                }
            }
            catch (Exception ex)
            {
                _ltasHelper.Logger.LogError(ex, "Error updating DateTime field {UpdateType}", updateType);
                throw;
            }
        }

        private async Task NotifyWorkspaceStatusUpdateAsync(
            IEnumerable<(int BillingWorkspaceArtifactId, string CurrentStatus, string NewStatus, int NewStatusChoiceArtifactId)> statusUpdates)
        {
            try
            {
                if (!statusUpdates.Any()) return;

                var emailBody = new StringBuilder();
                emailBody.AppendLine("The following workspaces require status updates:");
                emailBody.AppendLine("<br><br>");
                emailBody.AppendLine("<table border='1' style='border-collapse: collapse;'>");
                emailBody.AppendLine("<tr style='background-color: #f2f2f2;'>");
                emailBody.AppendLine("<th style='padding: 8px;'>Workspace ArtifactID</th>");
                emailBody.AppendLine("<th style='padding: 8px;'>Current Status</th>");
                emailBody.AppendLine("<th style='padding: 8px;'>New Status</th>");
                emailBody.AppendLine("</tr>");

                foreach (var (BillingWorkspaceArtifactId, CurrentStatus, NewStatus, NewStatusChoiceArtifactId) in statusUpdates)
                {
                    emailBody.AppendLine("<tr>");
                    emailBody.AppendLine($"<td style='padding: 8px;'>{BillingWorkspaceArtifactId}</td>");
                    emailBody.AppendLine($"<td style='padding: 8px;'>{CurrentStatus}</td>");
                    emailBody.AppendLine($"<td style='padding: 8px;'>{NewStatus}</td>");
                    emailBody.AppendLine("</tr>");
                }

                emailBody.AppendLine("</table>");
                await MessageHandler.Email.SendInternalNotificationAsync(
                    _instanceSettings,
                    emailBody,
                    "Workspace Status Updates");

                foreach (var (BillingWorkspaceArtifactId, _, _, NewStatusChoiceArtifactId) in statusUpdates)
                {
                    try
                    {
                        await ObjectHandler.UpdateFieldValueAsync(
                            _objectManager,
                            _billingManagementDatabase,
                            BillingWorkspaceArtifactId,
                            _ltasHelper.WorkspaceStatusField,
                            new Relativity.Services.Objects.DataContracts.ChoiceRef
                            {
                                ArtifactID = NewStatusChoiceArtifactId
                            },
                            true,
                            _ltasHelper.Logger);
                    }
                    catch (Exception ex)
                    {
                        _ltasHelper.Logger.LogError(ex,
                            "Error updating status for workspace {WorkspaceId} to status {NewStatus}",
                            BillingWorkspaceArtifactId,
                            NewStatusChoiceArtifactId);
                    }
                }
            }
            catch (Exception ex)
            {
                _ltasHelper.Logger.LogError(ex, "Error in UpdateWorkspaceStatusesAsync");
                throw;
            }
        }
    }
}
