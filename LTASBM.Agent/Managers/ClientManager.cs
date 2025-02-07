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
    public class ClientManager
    {
        private const int VALID_CLIENT_NUMBER_LENGTH = 5;        
        private readonly IObjectManager _objectManager;
        private readonly DataHandler _dataHandler;
        private readonly IInstanceSettingsBundle _instanceSettings;
        private readonly int _billingManagementDatabase;
        private readonly LTASBMHelper _ltasHelper;
        private readonly ILTASLogger _logger;

        public ClientManager(
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
            _ltasHelper = new LTASBMHelper(helper, logger.ForContext<ClientManager>());
            _logger = LoggerFactory.CreateLogger<ClientManager>(helper.GetDBContext(-1), helper, logger);
        }

        public async Task ProcessClientRoutinesAsync()
        {
            try
            {
                _logger.LogDebug("Fetching EDDS and Billing clients");
                var eddsClients = _dataHandler.EDDSClients();
                var billingClients = _dataHandler.BillingClients();

                
                _logger.LogInformation("Retrieved {EddsClientCount} EDDS clients and {BillingClientCount} billing clients",
                    eddsClients.Count, billingClients.Count);

                await ProcessAllClientOperationsAsync(eddsClients, billingClients);
                _logger.LogInformation("Successfully completed ProcessClientRoutinesAsync");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in ProcessClientRoutinesAsync");
                _ltasHelper.Logger.LogError(ex, "Error In ProcessClientRoutine");
            }
        }
                
        private async Task ProcessAllClientOperationsAsync(List<EddsClients> eddsClients, List<BillingClients> billingClients)
        {
            _logger.LogDebug("Starting ProcessAllClientOperationsAsync");
            var invalidClients = GetInvalidClients(eddsClients);
            _logger.LogInformation("Found {InvalidClientCount} clients with invalid client numbers", invalidClients.Count());
            await NotifyInvalidClientsAsync(invalidClients);

            var duplicateClients = GetDuplicateClients(eddsClients);
            _logger.LogInformation("Found {DuplicateClientCount} duplicate clients", duplicateClients.Count());
            await NotifyDuplicateClientsAsync(duplicateClients);

            var newClients = GetNewClientsForBilling(eddsClients, billingClients);
            _logger.LogInformation("Found {NewClientCount} new clients to process", newClients.Count());
            await ProcessNewClientsAsync(newClients);

            _logger.LogDebug("Completed ProcessAllClientOperationsAsync");
        }


        //TODO: Remove
        //private IEnumerable<EddsClients> GetInvalidClients(List<EddsClients> eddsClients)
        //    => eddsClients.Where(c => c.EddsClientNumber.Length != VALID_CLIENT_NUMBER_LENGTH);

        private IEnumerable<EddsClients> GetInvalidClients(List<EddsClients> eddsClients)
        {
            var invalidClients = eddsClients.Where(c => c.EddsClientNumber.Length != VALID_CLIENT_NUMBER_LENGTH);
            foreach (var client in invalidClients)
            {
                _logger.LogWarning("Invalid client number found: {ClientNumber} for client {ClientName} (ArtifactId: {ArtifactId})",
                    client.EddsClientNumber, client.EddsClientName, client.EddsClientArtifactId);
            }
            return invalidClients;
        }


        //TODO: Remove
        //private IEnumerable<EddsClients> GetDuplicateClients(List<EddsClients>eddsClients) 
        //    => eddsClients
        //        .GroupBy(c => c.EddsClientNumber)
        //        .Where(g => g.Count() > 1)
        //        .SelectMany(g => g);

        private IEnumerable<EddsClients> GetDuplicateClients(List<EddsClients> eddsClients)
        {
            var duplicates = eddsClients
                .GroupBy(c => c.EddsClientNumber)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g);

            foreach (var duplicate in duplicates)
            {
                _logger.LogWarning("Duplicate client found: {ClientNumber} - {ClientName} (ArtifactId: {ArtifactId})",
                    duplicate.EddsClientNumber, duplicate.EddsClientName, duplicate.EddsClientArtifactId);
            }
            return duplicates;
        }

        //TODO: Remove
        //private IEnumerable<EddsClients> GetNewClientsForBilling(List<EddsClients>eddsClients, List<BillingClients> billingClients) 
        //    => eddsClients.Where(edds =>
        //        !billingClients.Any(billing => billing.BillingEddsClientArtifactId == edds.EddsClientArtifactId)
        //        && edds.EddsClientNumber.Length == VALID_CLIENT_NUMBER_LENGTH);

        private IEnumerable<EddsClients> GetNewClientsForBilling(List<EddsClients> eddsClients, List<BillingClients> billingClients)
        {
            var newClients = eddsClients.Where(edds =>
                !billingClients.Any(billing => billing.BillingEddsClientArtifactId == edds.EddsClientArtifactId)
                && edds.EddsClientNumber.Length == VALID_CLIENT_NUMBER_LENGTH);

            foreach (var client in newClients)
            {
                _logger.LogInformation("New client identified for billing creation: {ClientNumber} - {ClientName} (ArtifactId: {ArtifactId})",
                    client.EddsClientNumber, client.EddsClientName, client.EddsClientArtifactId);
            }
            return newClients;
        }

        //TODO: Remove
        //private async Task NotifyInvalidClientsAsync(IEnumerable<EddsClients> invalidClients)
        //{
        //    foreach (var client in invalidClients)
        //    {
        //        var emailBody = new StringBuilder();
        //        MessageHandler.InvalidClientEmailBody(emailBody, client);
        //        await MessageHandler.Email.SentInvalidClientNumber(_instanceSettings, emailBody, client.EddsClientCreatedByEmail);
        //    }
        //}

        private async Task NotifyInvalidClientsAsync(IEnumerable<EddsClients> invalidClients)
        {
            foreach (var client in invalidClients)
            {
                _logger.LogInformation("Sending invalid client notification email to {Email} for client {ClientNumber}",
                    client.EddsClientCreatedByEmail, client.EddsClientNumber);

                try
                {
                    var emailBody = new StringBuilder();
                    MessageHandler.InvalidClientEmailBody(emailBody, client);
                    await MessageHandler.Email.SentInvalidClientNumber(_instanceSettings, emailBody, client.EddsClientCreatedByEmail);

                    _logger.LogDebug("Successfully sent invalid client notification for {ClientNumber}", client.EddsClientNumber);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send invalid client notification for client {ClientNumber}", client.EddsClientNumber);
                }
            }
        }

        //TODO: Remove
        //private async Task NotifyDuplicateClientsAsync (IEnumerable<EddsClients> duplicateClients)
        //{
        //    if(duplicateClients.Any()) 
        //    {
        //        var emailBody = new StringBuilder();
        //        MessageHandler.DuplicateClientEmailBody(emailBody, duplicateClients.ToList());
        //        await MessageHandler.Email.SendInternalNotificationAsync(_instanceSettings, emailBody, "Duplicate Clients Found");
        //    }
        //}

        private async Task NotifyDuplicateClientsAsync(IEnumerable<EddsClients> duplicateClients)
        {
            if (!duplicateClients.Any()) return;

            _logger.LogInformation("Preparing to send duplicate clients notification for {Count} clients", duplicateClients.Count());
            try
            {
                var emailBody = new StringBuilder();
                MessageHandler.DuplicateClientEmailBody(emailBody, duplicateClients.ToList());
                await MessageHandler.Email.SendInternalNotificationAsync(_instanceSettings, emailBody, "Duplicate Clients Found");

                _logger.LogInformation("Successfully sent duplicate clients notification");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send duplicate clients notification");
            }
        }

        //TODO: Remove
        //private async Task ProcessNewClientsAsync(IEnumerable<EddsClients> newClients) 
        //{
        //    if (!newClients.Any()) return;
        //    await NotifyNewClientsAsync(newClients);
        //    await CreateNewClientsInBillingAsync(newClients);
            
        //}

        private async Task ProcessNewClientsAsync(IEnumerable<EddsClients> newClients)
        {
            if (!newClients.Any())
            {
                _logger.LogInformation("No new clients to process");
                return;
            }

            _logger.LogInformation("Beginning processing of {Count} new clients", newClients.Count());
            await NotifyNewClientsAsync(newClients);
            await CreateNewClientsInBillingAsync(newClients);
        }

        //TODO: Remove
        //private async Task NotifyNewClientsAsync(IEnumerable<EddsClients> newClients) 
        //{
        //    var emailBody = new StringBuilder();
        //    MessageHandler.NewClientsEmailBody(emailBody, newClients.ToList());
        //    await MessageHandler.Email.SendNewClientsReportingAsync(_instanceSettings, emailBody);
        //}

        private async Task NotifyNewClientsAsync(IEnumerable<EddsClients> newClients)
        {
            _logger.LogDebug("Preparing new clients notification email");
            try
            {
                var emailBody = new StringBuilder();
                MessageHandler.NewClientsEmailBody(emailBody, newClients.ToList());
                await MessageHandler.Email.SendNewClientsReportingAsync(_instanceSettings, emailBody);

                _logger.LogInformation("Successfully sent new clients notification");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send new clients notification email");
            }
        }

        private async Task CreateNewClientsInBillingAsync(IEnumerable<EddsClients> newClients)
        {
            foreach (var client in newClients)
            {
                try
                {
                    _logger.LogInformation("Creating new client in billing system: {@ClientDetails}",
                     new { client.EddsClientArtifactId, client.EddsClientNumber, client.EddsClientName });

                    var result = await ObjectHandler.CreateNewClientAsync(
                        _objectManager,
                        _billingManagementDatabase,
                        client.EddsClientNumber,
                        client.EddsClientName,
                        client.EddsClientArtifactId,
                        _ltasHelper.Logger,
                        _ltasHelper.Helper);

                    if (result == null)
                    {
                        _ltasHelper.Logger.LogError("CreateNewClient returned null result for client {ClientNumber}",
                            client.EddsClientNumber);
                        _logger.LogError("Failed to create client in billing system: {ClientNumber} - Null result returned",
                        client.EddsClientNumber);
                    }
                    else 
                    {
                        _logger.LogInformation("Successfully created client in billing system: {ClientNumber}",
                        client.EddsClientNumber);
                    }
                }
                catch (Exception ex)
                {
                    _ltasHelper.Logger.LogError(ex,
                        "Error creating client: {@ClientDetails}",
                        new { client.EddsClientArtifactId, client.EddsClientNumber, client.EddsClientName });
                }
            }
        }

    }
}