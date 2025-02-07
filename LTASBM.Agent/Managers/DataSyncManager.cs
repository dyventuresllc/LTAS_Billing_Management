using LTASBM.Agent.Handlers;
using LTASBM.Agent.Logging;
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
        private readonly ILTASLogger _logger;
        public DataSyncManager(
            IAPILog relativityLogger,
            IHelper helper,
            IObjectManager objectManager,
            IInstanceSettingsBundle instanceSettings,
            DataHandler dataHandler,
            int billingManagementDatabase)
        {
            _dataHandler = dataHandler ?? throw new ArgumentNullException(nameof(dataHandler));
            _objectManager = objectManager ?? throw new ArgumentNullException(nameof(objectManager));
            _instanceSettings = instanceSettings ?? throw new ArgumentNullException(nameof(instanceSettings));
            _ltasHelper = new LTASBMHelper(helper, relativityLogger.ForContext<DataSyncManager>());
            _billingManagementDatabase = billingManagementDatabase;
            _logger = LoggerFactory.CreateLogger<DataSyncManager>(helper.GetDBContext(-1), helper, relativityLogger);
            _logger.LogInformation("DataSyncManager initialized");
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
                var eddsUsers = _dataHandler.EDDSUsers();
                var billingUsers = _dataHandler.BillingUsers();

                _logger.LogInformation("Retrieved all data for sync. Processing sync operations");
                await ProccessAllDataSyncOperationsAsync(
                    eddsClients, billingClients,
                    eddsMatters, billingMatters,
                    eddsWorkspaces, billingWorkspaces,
                    eddsUsers, billingUsers);
            }
            catch (Exception ex)
            {
                _ltasHelper.Logger.LogError(ex, "Error In DataSyncRoutine");
                _logger.LogError(ex, "Error In DataSyncRoutine");
                throw;
            }
        }

        private async Task ProccessAllDataSyncOperationsAsync(
            List<EddsClients> eddsclients, List<BillingClients> billingClients,
            List<EddsMatters> eddsMatters, List<BillingMatters> billingMatters,
            List<EddsWorkspaces> eddsWorkspaces, List<BillingWorkspaces> billingWorkspaces,
            List<EddsUsers> eddsUsers, List<BillingUsers> billingUsers)
        {
            _logger.LogInformation("Beginning all data sync operations");

            var clientNameUpdates = GetClientNameUpdates(eddsclients, billingClients);
            _logger.LogInformation("Found {count} client name updates", clientNameUpdates.Count());
            await NotifyClientNameUpdatesAsync(clientNameUpdates);

            var clientNumberUpdates = GetClientNumberUpdates(eddsclients, billingClients);
            _logger.LogInformation("Found {count} client number updates", clientNumberUpdates.Count());
            await NotifyClientNumberUpdatesAsync(clientNumberUpdates);

            var matterNameUpdates = GetMatterNameUpdates(eddsMatters, billingMatters);
            _logger.LogInformation("Found {count} matter name updates", matterNameUpdates.Count());
            await NotifyMatterNameUpdatesAsync(matterNameUpdates);

            var matterNumberUpdates = GetMatterNumberUpdates(eddsMatters, billingMatters);
            _logger.LogInformation("Found {count} matter number updates", matterNumberUpdates.Count());
            await NotifyMatterNumberUpdatesAsync(matterNumberUpdates);

            var workspaceNameUpdates = GetWorkspaceNameUpdates(eddsWorkspaces, billingWorkspaces);
            _logger.LogInformation("Found {count} workspace name updates", workspaceNameUpdates.Count());
            await NotifyWorkspaceNameUpdatesAsync(workspaceNameUpdates);

            var workspaceCreatedByUpdates = GetWorkspaceCreatedByUpdates(eddsWorkspaces, billingWorkspaces);
            _logger.LogInformation("Found {count} workspace created by updates", workspaceCreatedByUpdates.Count());
            await NotifyWorkspaceCreatedByUpdatesAsync(workspaceCreatedByUpdates);

            var workspaceCreatedOnUpdates = GetWorkspaceCreatedOnUpdates(eddsWorkspaces, billingWorkspaces);
            _logger.LogInformation("Found {count} workspace created on updates", workspaceCreatedOnUpdates.Count());
            await NotifyWorkspaceCreatedOnUpdatesAsync(workspaceCreatedOnUpdates);

            var workspaceCaseTeamUpdates = GetWorkspaceCaseTeamUpdates(eddsWorkspaces, billingWorkspaces);
            _logger.LogInformation("Found {count} workspace case team updates", workspaceCaseTeamUpdates.Count());
            await NotifyWorkspaceCaseTeamUpdatesAsync(workspaceCaseTeamUpdates);

            var workspaceAnalystUpdates = GetWorkspaceAnalystUpdates(eddsWorkspaces, billingWorkspaces);
            _logger.LogInformation("Found {count} workspace analyst updates", workspaceAnalystUpdates.Count());
            await NotifyWorkspaceAnalystUpdatesAsync(workspaceAnalystUpdates);

            var workspaceStatusUpdates = await GetWorkspaceStatusUpdates(eddsWorkspaces, billingWorkspaces);
            _logger.LogInformation("Found {count} workspace status updates", workspaceStatusUpdates.Count());
            await NotifyWorkspaceStatusUpdateAsync(workspaceStatusUpdates);

            var workspaceMatterUpdates = GetWorkspaceMatterMismatch(eddsWorkspaces, billingWorkspaces);
            _logger.LogInformation("Found {count} workspace matter mismatches", workspaceMatterUpdates.Count());
            await NotifyWorkspaceMatterUpdatesAsync(workspaceMatterUpdates, billingWorkspaces, eddsMatters, billingMatters);            
            
            var billingRecipientsNewUser = GetNewUsersForBilling(eddsUsers, billingUsers);
            _logger.LogInformation("Found {count} new users to create", billingRecipientsNewUser.Count());
            await CreateNewUserAsync(billingRecipientsNewUser);

            var matterClientUpdates = GetMatterClientMismatch(eddsMatters, billingMatters);
            _logger.LogInformation("Found {count} matter client mismatches", matterClientUpdates.Count());
            await NotifyMatterClientMismatchAsync(matterClientUpdates);

            var userReportEmailAddressUpdates = GetUserEmailMismatches(eddsUsers, billingUsers);
            _logger.LogInformation("Found {count} user email mismatches", userReportEmailAddressUpdates.Count());
            await UpdateUserEmailMismatchAsync(userReportEmailAddressUpdates);

            var userReportFirstNameUpdates = GetUserFirstNameMismatches(eddsUsers, billingUsers);
            _logger.LogInformation("Found {count} user first name mismatches", userReportFirstNameUpdates.Count());
            await UpdateUserEmailFirstNameMismatchAsync(userReportFirstNameUpdates);

            var userReportLastnameUpdates = GetUserLastNameMismatches(eddsUsers, billingUsers);
            _logger.LogInformation("Found {count} user last name mismatches", userReportLastnameUpdates.Count());
            await UpdateUserEmailLastNameMismatch(userReportLastnameUpdates);

            _logger.LogInformation("Completed all data sync operations");
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

        private Task<IEnumerable<(int BillingWorkspaceArtifactId, string CurrentStatus, string NewStatus, int NewStatusChoiceArtifactId)>> GetWorkspaceStatusUpdates(
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

            return Task.FromResult<IEnumerable<(int, string, string, int)>>(updates);
        }

        private IEnumerable<EddsUsers> GetNewUsersForBilling(
            List<EddsUsers> eddsUsers,
            List<BillingUsers> billingUsers)
            => eddsUsers
            .Where(edds =>
            !billingUsers.Any(billing =>
                billing.BillingUserEddsArtifactId == edds.EddsUserArtifactId))
            .ToList();

        private IEnumerable<(int BillingMatterArtifactId, int CurrentClientArtifactId, int NewClientArtifactId)> GetMatterClientMismatch(
            List<EddsMatters> eddsMatters,
            List<BillingMatters> billingMatters)
            => billingMatters
                .Where(b => b.BillingMatterEDDSClientArtifactID != 0)  // Filter out zero client IDs
                .Join(
                    eddsMatters,
                    billing => billing.BillingEddsMatterArtifactId,
                    edds => edds.EddsMatterArtifactId,
                    (billing, edds) => new
                    {
                        billing.BillingMatterArtficatId,
                        CurrentClientId = billing.BillingMatterEDDSClientArtifactID,
                        NewClientEddsArtifactId = edds.EddsMatterClientEDDSArtifactID
                    })
                .Where(result =>
                    result.CurrentClientId != result.NewClientEddsArtifactId &&
                    result.CurrentClientId != 0 &&    // Additional checks to match SQL
                    result.NewClientEddsArtifactId != 0)
                .Select(result => (
                    result.BillingMatterArtficatId,
                    result.CurrentClientId,
                    result.NewClientEddsArtifactId))
                .ToList();

        private IEnumerable<(int BillingWorkspaceArtifactId, int CurrentMatterArtifactId, int NewMatterArtifactId)> GetWorkspaceMatterMismatch(
            List<EddsWorkspaces> eddsWorkspaces,
            List<BillingWorkspaces> billingWorkspaces)
            => billingWorkspaces
                .Where(b => b.BillingWorkspaceMatterArtifactId != 0)  // Filter out zero matter IDs
                .Join(
                    eddsWorkspaces,
                    billing => billing.BillingWorkspaceEddsArtifactId,  // Join on EDDS ArtifactId
                    edds => edds.EddsWorkspaceArtifactId,
                    (billing, edds) => new
                    {
                        billing.BillingWorkspaceArtifactId,            // This is what we want to return
                        CurrentMatterEDDSId = billing.BillingWorkspaceMatterEddsArtifactId,  // Current matter ID in billing
                        NewMatterEDDSArtifactId = edds.EddsWorkspaceMatterArtifactId    // New matter ID from EDDS
                    })
                .Where(result =>
                    result.CurrentMatterEDDSId != result.NewMatterEDDSArtifactId &&  // Only where they don't match
                    result.CurrentMatterEDDSId != 0 &&
                    result.NewMatterEDDSArtifactId != 0)
                .Select(result => (
                    result.BillingWorkspaceArtifactId,     // Return billing workspace ID
                    result.CurrentMatterEDDSId,                // Current matter ID in billing
                    result.NewMatterEDDSArtifactId))           // New matter ID from EDDS
                .ToList();

        private IEnumerable<(int BillingUserId, string CurrentEmail, string NewEmail)> GetUserEmailMismatches(
           List<EddsUsers> eddsUsers,
           List<BillingUsers> billingUsers)
        {
            return billingUsers
                .Join(
                    eddsUsers,
                    billing => billing.BillingUserEddsArtifactId,
                    edds => edds.EddsUserArtifactId,
                    (billing, edds) => new {
                        billing.BillingUserArtifactId,
                        CurrentEmail = billing.BillingUserEmailAddress,
                        NewEmail = edds.EddsUserEmailAddress
                    })
                .Where(x => !string.Equals(x.CurrentEmail, x.NewEmail, StringComparison.OrdinalIgnoreCase))
                .Select(x => (x.BillingUserArtifactId, x.CurrentEmail, x.NewEmail))
                .ToList();
        }

        private IEnumerable<(int BillingUserId, string CurrentFirstName, string NewFirstName)> GetUserFirstNameMismatches(
            List<EddsUsers> eddsUsers,
            List<BillingUsers> billingUsers)
        {
            return billingUsers
                .Join(
                    eddsUsers,
                    billing => billing.BillingUserEddsArtifactId,
                    edds => edds.EddsUserArtifactId,
                    (billing, edds) => new {
                        billing.BillingUserArtifactId,
                        CurrentFirstName = billing.BillingUserFirstName,
                        NewFirstName = edds.EddsUserFirstName
                    })
                .Where(x => !string.Equals(x.CurrentFirstName, x.NewFirstName, StringComparison.OrdinalIgnoreCase))
                .Select(x => (x.BillingUserArtifactId, x.CurrentFirstName, x.NewFirstName))
                .ToList();
        }

        private IEnumerable<(int BillingUserId, string CurrentLastName, string NewLastName)> GetUserLastNameMismatches(
            List<EddsUsers> eddsUsers,
            List<BillingUsers> billingUsers)
        {
            return billingUsers
                .Join(
                    eddsUsers,
                    billing => billing.BillingUserEddsArtifactId,
                    edds => edds.EddsUserArtifactId,
                    (billing, edds) => new {
                        billing.BillingUserArtifactId,
                        CurrentLastName = billing.BillingUserLastName,
                        NewLastName = edds.EddsUserLastName
                    })
                .Where(x => !string.Equals(x.CurrentLastName, x.NewLastName, StringComparison.OrdinalIgnoreCase))
                .Select(x => (x.BillingUserArtifactId, x.CurrentLastName, x.NewLastName))
                .ToList();
        }

        //TODO: Remove
        //private async Task NotifyClientNameUpdatesAsync(IEnumerable<(int BillingClientArtifactId, string EddsClientName)> clientNameUpdates)
        //{
        //    if (!clientNameUpdates.Any()) return;

        //    var emailBody = new StringBuilder();
        //    emailBody = MessageHandler.DataSyncNotificationEmailBody(emailBody, clientNameUpdates, "Client", "Client Name");
        //    await MessageHandler.Email.SendInternalNotificationAsync(
        //        _instanceSettings,
        //        emailBody,
        //        "Client Name Updates Required");

        //    await UpdateObjectValueTypeAsync(clientNameUpdates, UpdateType.ClientName);
        //}

        private async Task NotifyClientNameUpdatesAsync(IEnumerable<(int BillingClientArtifactId, string EddsClientName)> clientNameUpdates)
        {
            if (!clientNameUpdates.Any())
            {
                _logger.LogInformation("No client name updates required");
                return;
            }

            _logger.LogInformation("Processing {count} client name updates", clientNameUpdates.Count());
            var emailBody = new StringBuilder();
            emailBody = MessageHandler.DataSyncNotificationEmailBody(emailBody, clientNameUpdates, "Client", "Client Name");

            await MessageHandler.Email.SendInternalNotificationAsync(
                _instanceSettings,
                emailBody,
                "Client Name Updates Required");

            _logger.LogInformation("Starting client name field updates");
            await UpdateObjectValueTypeAsync(clientNameUpdates, UpdateType.ClientName);
        }


        //TODO: Remove
        //private async Task NotifyClientNumberUpdatesAsync(IEnumerable<(int BillingClientArtifactId, string EddsClientName)> clientNumberUpdates)
        //{
        //    if (!clientNumberUpdates.Any()) return;

        //    var emailBody = new StringBuilder();
        //    emailBody = MessageHandler.DataSyncNotificationEmailBody(emailBody, clientNumberUpdates, "Client", UpdateType.ClientNumber.ToString());
        //    await MessageHandler.Email.SendInternalNotificationAsync(
        //        _instanceSettings,
        //        emailBody,
        //        "Client Number Updates Required");

        //    await UpdateObjectValueTypeAsync(clientNumberUpdates, UpdateType.ClientNumber);
        //}

        private async Task NotifyClientNumberUpdatesAsync(IEnumerable<(int BillingClientArtifactId, string EddsClientName)> clientNumberUpdates)
        {
            if (!clientNumberUpdates.Any())
            {
                _logger.LogInformation("No client number updates required");
                return;
            }

            _logger.LogInformation("Processing {count} client number updates", clientNumberUpdates.Count());
            var emailBody = new StringBuilder();
            emailBody = MessageHandler.DataSyncNotificationEmailBody(emailBody, clientNumberUpdates, "Client", UpdateType.ClientNumber.ToString());

            await MessageHandler.Email.SendInternalNotificationAsync(
                _instanceSettings,
                emailBody,
                "Client Number Updates Required");

            _logger.LogInformation("Starting client number field updates");
            await UpdateObjectValueTypeAsync(clientNumberUpdates, UpdateType.ClientNumber);
        }

        //TODO: Remove
        //private async Task NotifyMatterNameUpdatesAsync(IEnumerable<(int BillingMatterArtifactID, string EddsMatterName)> matterNameUpdates)
        //{
        //    if (matterNameUpdates.Any())
        //    {
        //        var emailBody = new StringBuilder();
        //        emailBody = MessageHandler.DataSyncNotificationEmailBody(emailBody, matterNameUpdates, "Matter", UpdateType.MatterName.ToString());
        //        await MessageHandler.Email.SendInternalNotificationAsync(
        //            _instanceSettings,
        //            emailBody,
        //            "Matter Name Updates Required");

        //        await UpdateObjectValueTypeAsync(matterNameUpdates, UpdateType.MatterName);
        //    }
        //}

        private async Task NotifyMatterNameUpdatesAsync(IEnumerable<(int BillingMatterArtifactID, string EddsMatterName)> matterNameUpdates)
        {
            if (!matterNameUpdates.Any())
            {
                _logger.LogInformation("No matter name updates required");
                return;
            }

            _logger.LogInformation("Processing {count} matter name updates", matterNameUpdates.Count());
            var emailBody = new StringBuilder();
            emailBody = MessageHandler.DataSyncNotificationEmailBody(emailBody, matterNameUpdates, "Matter", UpdateType.MatterName.ToString());

            await MessageHandler.Email.SendInternalNotificationAsync(
                _instanceSettings,
                emailBody,
                "Matter Name Updates Required");

            _logger.LogInformation("Starting matter name field updates");
            await UpdateObjectValueTypeAsync(matterNameUpdates, UpdateType.MatterName);
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
      
        private async Task NotifyWorkspaceMatterUpdatesAsync(IEnumerable<(int BillingWorkspaceArtifactId, int CurrentMatterArtifactId, int NewMatterArtifactId)> workspaceMatterMismatches,
            List<BillingWorkspaces> billingWorkspaces, List<EddsMatters> eddsMatters, List<BillingMatters> billingMatters)
        {
            try
            {                
                if (!workspaceMatterMismatches.Any())
                {
                    _logger.LogInformation("No workspace matter updates required");
                    return;
                }

                _logger.LogInformation("Processing {count} workspace matter updates", workspaceMatterMismatches.Count());

                var emailBody = new StringBuilder();
                emailBody.AppendLine("The following workspaces changes to the Workspace Matter ArtifactID mismatches:");
                emailBody.AppendLine("<br><br>");
                emailBody.AppendLine("<table border='1'>");
                emailBody.AppendLine("<tr>");
                emailBody.AppendLine("<th>Workspace ArtifactID</th>");
                emailBody.AppendLine("<th>Workspace Name</th>");
                emailBody.AppendLine("<th>Current Matter EDDS ArtifactID</th>");
                emailBody.AppendLine("<th>Current Mattter Name</th>");
                emailBody.AppendLine("<th>New Matter EDDS ArtifactID</th>");
                emailBody.AppendLine("<th>New Mattter Name</th>");
                emailBody.AppendLine("</tr>");

                foreach (var (BillingWorkspaceArtifactId, CurrentMatterArtifactId, NewMatterArtifactId) in workspaceMatterMismatches)
                {
                    var workspaceName = _ltasHelper.GetWorkspaceNameByBillingWorkspaceArtifactID(
                        BillingWorkspaceArtifactId,
                        billingWorkspaces);

                    var currentEddsMatterName = _ltasHelper.GetMatterNameByEDDSMatterArtifactId(
                        CurrentMatterArtifactId,
                        eddsMatters);

                    var newEddsMatterName = _ltasHelper.GetMatterNameByEDDSMatterArtifactId(
                        NewMatterArtifactId,
                        eddsMatters);

                    emailBody.AppendLine("<tr>");
                    emailBody.AppendLine($"<td>{BillingWorkspaceArtifactId}</td>");
                    emailBody.AppendLine($"<td>{workspaceName}</td>");
                    emailBody.AppendLine($"<td>{CurrentMatterArtifactId}</td>");
                    emailBody.AppendLine($"<td>{currentEddsMatterName}</td>");
                    emailBody.AppendLine($"<td>{NewMatterArtifactId}</td>");
                    emailBody.AppendLine($"<td>{newEddsMatterName}</td>");
                    emailBody.AppendLine("</tr>");
                }

                emailBody.AppendLine("</table>");

                await MessageHandler.Email.SendInternalNotificationAsync(
                    _instanceSettings,
                    emailBody,
                    "Workspace Matter Object mismatches");
                _logger.LogInformation("Starting workspace matter field updates");

                foreach (var (BillingWorkspaceArtifactId, _, NewMatterArtifactId) in workspaceMatterMismatches)
                {
                    _logger.LogInformation("Updating workspace {workspaceId} with new matter {matterId}",
                        BillingWorkspaceArtifactId, NewMatterArtifactId);

                    await ObjectHandler.UpdateFieldValueAsync(
                        _objectManager,
                        _billingManagementDatabase,
                        BillingWorkspaceArtifactId,
                        _ltasHelper.WorkspaceMatterNumberField,
                        new Relativity.Services.Objects.DataContracts.RelativityObject
                        {
                            ArtifactID = _ltasHelper.GetMatterArifactIdByEddsMatterArtifactId(NewMatterArtifactId, billingMatters)
                        },
                        _ltasHelper.Logger
                        );
                }
            }
            catch (Exception ex)
            {
                _ltasHelper.Logger.LogError(ex, "Error handling workspace matter mismatch notification");
                _logger.LogError(ex, "Error handling workspace matter mismatch notification");
                throw;
            }
        }

        private async Task NotifyWorkspaceNameUpdatesAsync(IEnumerable<(int BillingWorkspaceArtifactId, string EddsWorkspaceName)> workspaceNameUpdates)
        {
            if (workspaceNameUpdates.Any())
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

        private async Task UpdateUserEmailMismatchAsync(IEnumerable<(int BillingUserId, string CurrentEmail, string NewEmail)> userEmailMismatch)
        {
            if (!userEmailMismatch.Any())
            {
                _logger.LogInformation("No user email updates required");
                return;
            }
            _logger.LogInformation("Processing {count} user email updates", userEmailMismatch.Count());

            foreach (var (BillingUserId, _, NewEmail) in userEmailMismatch)
            {
                _logger.LogInformation("Updating user {userId} email to {newEmail}", BillingUserId, NewEmail);

                await ObjectHandler.UpdateFieldValueAsync(
                    _objectManager,
                    _billingManagementDatabase,
                    BillingUserId,
                    _ltasHelper.UserEmailAddressField,
                    NewEmail,
                    _ltasHelper.Logger);
            }
        }

        private async Task UpdateUserEmailFirstNameMismatchAsync(IEnumerable<(int BillingUserId, string CurrentFirstName, string NewFirstName)> userFirstNameMismatch)
        {
            if(!userFirstNameMismatch.Any()) return;

            StringBuilder sb = new StringBuilder();
            sb.Append($"{userFirstNameMismatch.Count()} - User billing first Names to update");
            await MessageHandler.Email.SendInternalNotificationAsync(_instanceSettings,sb, "debug - firstname user count");

            foreach (var (BillingUserId, _, NewFirstName) in userFirstNameMismatch)
            {
                await ObjectHandler.UpdateFieldValueAsync(
                    _objectManager,
                    _billingManagementDatabase,
                    BillingUserId,
                    _ltasHelper.UserFirstNameField,
                    NewFirstName,
                    _ltasHelper.Logger);
            }
        }

        private async Task UpdateUserEmailLastNameMismatch(IEnumerable<(int BillingUserId, string CurrentLastName, string NewLastName)> userLastNameMismatch) 
        {
            if (!userLastNameMismatch.Any()) return;

            foreach (var (BillingUserId, _, NewLastName) in userLastNameMismatch)
            {
                await ObjectHandler.UpdateFieldValueAsync(
                    _objectManager,
                    _billingManagementDatabase,
                    BillingUserId,
                    _ltasHelper.UserLastNameField,
                    NewLastName,
                    _ltasHelper.Logger);
            }
        }

        private async Task NotifyMatterClientMismatchAsync(
            IEnumerable<(int BillingMatterArtifactId, int CurrentClientArtifactId, int NewClientEddsArtifactId)> matterClientUpdates)
        {
            if (!matterClientUpdates.Any()) return;

            var emailBody = new StringBuilder();
            emailBody.AppendLine("The following matters have client mismatches between EDDS and Billing:");
            emailBody.AppendLine("<table border='1' style='border-collapse: collapse;'>");
            emailBody.AppendLine("<tr style='background-color: #f2f2f2;'>");
            emailBody.AppendLine("<th style='padding: 8px;'>Matter ArtifactID</th>");
            emailBody.AppendLine("<th style='padding: 8px;'>Current Client ArtifactID</th>");
            emailBody.AppendLine("<th style='padding: 8px;'>New Client EDDS ArtifactID</th>");
            emailBody.AppendLine("</tr>");

            foreach (var (BillingMatterArtifactId, CurrentClientArtifactId, NewClientEddsArtifactId) in matterClientUpdates)
            {
                emailBody.AppendLine("<tr>");
                emailBody.AppendLine($"<td style='padding: 8px;'>{BillingMatterArtifactId}</td>");
                emailBody.AppendLine($"<td style='padding: 8px;'>{CurrentClientArtifactId}</td>");
                emailBody.AppendLine($"<td style='padding: 8px;'>{NewClientEddsArtifactId}</td>");
                emailBody.AppendLine("</tr>");
            }

            emailBody.AppendLine("</table>");
    
            await MessageHandler.Email.SendInternalNotificationAsync(
                _instanceSettings,
                emailBody,
                "Matter Client Mismatch Updates");

            // Update the records
            try 
            {
                foreach(var (BillingMatterArtifactId, CurrentClientArtifactId, NewClientEddsArtifactId) in matterClientUpdates)
                {
                    await ObjectHandler.UpdateFieldValueAsync(
                        _objectManager,
                        _billingManagementDatabase,
                        BillingMatterArtifactId,
                        _ltasHelper.MatterClientObjectField,
                        new Relativity.Services.Objects.DataContracts.RelativityObjectRef
                        {
                            ArtifactID = await _ltasHelper.LookupClientArtifactID(_objectManager,_billingManagementDatabase, NewClientEddsArtifactId)
                        },
                        _ltasHelper.Logger);
                }
            }
            catch (Exception ex)
            {
                _ltasHelper.Logger.LogError(ex, "Error updating matter client references");
                throw;
            }
        }

        private async Task CreateNewUserAsync(IEnumerable<EddsUsers> newUsersToBillingReceipients)
        {
            foreach (var user in newUsersToBillingReceipients)
            {
                await ObjectHandler.CreateNewUserAsync(
                    _objectManager,
                    _billingManagementDatabase,
                    user.EddsUserFirstName,
                    user.EddsUserLastName,
                    user.EddsUserEmailAddress,
                    user.EddsUserArtifactId,
                    _ltasHelper.Logger,
                    _ltasHelper.Helper);
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
