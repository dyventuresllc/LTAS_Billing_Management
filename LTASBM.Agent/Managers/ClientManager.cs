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
    public class ClientManager
    {
        private const int VALID_CLIENT_NUMBER_LENGTH = 5;        
        private readonly IObjectManager _objectManager;
        private readonly DataHandler _dataHandler;
        private readonly IInstanceSettingsBundle _instanceSettings;
        private readonly int _billingManagementDatabase;
        private readonly LTASBMHelper _ltasHelper;

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
        }

        public async Task ProcessClientRoutinesAsync()
        {
            try
            {
                var eddsClients = _dataHandler.EDDSClients();
                var billingClients = _dataHandler.BillingClients();

                await ProcessAllClientOperationsAsync(eddsClients, billingClients);                
            }
            catch (Exception ex)
            {
                _ltasHelper.Logger.LogError(ex, "Error In ProcessClientRoutine");
            }
        }
                
        private async Task ProcessAllClientOperationsAsync(List<EddsClients> eddsClients, List<BillingClients> billingClients)
        {
            var invalidClients = GetInvalidClients(eddsClients);
            await NotifyInvalidClientsAsync(invalidClients);

            var duplicateClients = GetDuplicateClients(eddsClients);
            await NotifyDuplicateClientsAsync(duplicateClients);

            var newClients = GetNewClientsForBilling(eddsClients, billingClients);
            await ProcessNewClientsAsync(newClients);
        }
        private IEnumerable<EddsClients> GetInvalidClients(List<EddsClients> eddsClients)
            => eddsClients.Where(c => c.EddsClientNumber.Length != VALID_CLIENT_NUMBER_LENGTH);
        private IEnumerable<EddsClients> GetDuplicateClients(List<EddsClients>eddsClients) 
            => eddsClients
                .GroupBy(c => c.EddsClientNumber)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g);
        private IEnumerable<EddsClients> GetNewClientsForBilling(List<EddsClients>eddsClients, List<BillingClients> billingClients) 
            => eddsClients.Where(edds =>
                !billingClients.Any(billing => billing.BillingEddsClientArtifactId == edds.EddsClientArtifactId)
                && edds.EddsClientNumber.Length == VALID_CLIENT_NUMBER_LENGTH);
        private async Task NotifyInvalidClientsAsync(IEnumerable<EddsClients> invalidClients)
        {
            foreach (var client in invalidClients)
            {
                var emailBody = new StringBuilder();
                MessageHandler.InvalidClientEmailBody(emailBody, client);
                await MessageHandler.Email.SentInvalidClientNumber(_instanceSettings, emailBody, client.EddsClientCreatedByEmail);
            }
        }
        private async Task NotifyDuplicateClientsAsync (IEnumerable<EddsClients> duplicateClients)
        {
            if(duplicateClients.Any()) 
            {
                var emailBody = new StringBuilder();
                MessageHandler.DuplicateClientEmailBody(emailBody, duplicateClients.ToList());
                await MessageHandler.Email.SendInternalNotificationAsync(_instanceSettings, emailBody, "Duplicate Clients Found");
            }
        }
        private async Task ProcessNewClientsAsync(IEnumerable<EddsClients> newClients) 
        {
            if (!newClients.Any()) return;
            await NotifyNewClientsAsync(newClients);
            await CreateNewClientsInBillingAsync(newClients);
            
        }
        private async Task NotifyNewClientsAsync(IEnumerable<EddsClients> newClients) 
        {
            var emailBody = new StringBuilder();
            MessageHandler.NewClientsEmailBody(emailBody, newClients.ToList());
            await MessageHandler.Email.SendNewClientsReportingAsync(_instanceSettings, emailBody);
        }
        private async Task CreateNewClientsInBillingAsync(IEnumerable<EddsClients> newClients)
        {
            foreach (var client in newClients)
            {
                try
                {
                    _ltasHelper.Logger.LogInformation("Attempting to create client: {@ClientDetails}",
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