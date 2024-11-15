using LTASBM.Agent.Handlers;
using LTASBM.Agent.Models;
using Relativity.API;
using Relativity.Services.Objects;
using Relativity.Services.Objects.DataContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTASBM.Agent.Routines
{
    public class ClientRoutine
    {
        private readonly IAPILog _logger;
        private readonly IHelper _helper;

        public ClientRoutine(IAPILog logger, IHelper helper)
        {
            _logger = logger.ForContext<ClientRoutine>();
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
        }

        public async Task ProcessClientRoutines(int billingManagementDatabase, IObjectManager objectManager, DataHandler dataHandler, IInstanceSettingsBundle instanceSettings)
        {
            StringBuilder emailBody = new StringBuilder();

            try
            {
                var eddsClients = dataHandler.EDDSClients();
                var billingClients = dataHandler.BillingClients();

                ProcessInvalidClients(emailBody, eddsClients, instanceSettings);
                ProcessDuplicateClients(emailBody, eddsClients, instanceSettings);
                await ProcessNewClientsToBillingAsync(objectManager, billingManagementDatabase, emailBody, eddsClients, billingClients, instanceSettings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error In ProcessClientRoutine");
            }
        }

        private void ProcessInvalidClients(StringBuilder emailBody, List<EddsClients> eddsClients, IInstanceSettingsBundle instanceSettings)
        {

            //Handle invalid clients -- client number should be 5 digits only ever
            var invalidClients = eddsClients.Where(c => c.EddsClientNumber.Length != 5).ToList();

            if (invalidClients.Any())
            {
                foreach (var record in invalidClients)
                {
                    emailBody.Clear();
                    emailBody = MessageHandler.InvalidClientEmailBody(emailBody, record);
                    MessageHandler.Email.SentInvalidClientNumber(instanceSettings, emailBody, record.EddsClientCreatedByEmail);
                }
            }
        }
        private void ProcessDuplicateClients(StringBuilder emailBody, List<EddsClients> eddsClients, IInstanceSettingsBundle instanceSettings)
        {
            var duplicateClients = eddsClients
                .GroupBy(c => c.EddsClientNumber)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g)
                .ToList();

            if (duplicateClients.Any())
            {
                MessageHandler.DuplicateClientEmailBody(emailBody, duplicateClients);
                MessageHandler.Email.SendDebugEmail(instanceSettings, emailBody, "Duplicate Clients Found");
            }
        }
        private async Task ProcessNewClientsToBillingAsync(IObjectManager objectManager, int billingManagementDatabase, StringBuilder emailBody, List<EddsClients> eddsClients, List<BillingClients> billingClients, IInstanceSettingsBundle instanceSettings)
        {
            //Handle new clients that need to be added to billing system
            var missingInBilling = eddsClients
                .Where(edds => !billingClients
                .Any(billing => billing.BillingEddsClientArtifactId == edds.EddsClientArtifactId)
                && edds.EddsClientNumber.Length == 5)
                .ToList();

            if (missingInBilling.Any())
            {
                emailBody.Clear();
                emailBody = MessageHandler.NewClientsEmailBody(emailBody, missingInBilling);
                MessageHandler.Email.SendNewClientsReporting(instanceSettings, emailBody, "damienyoung@quinnemanuel.com");

                foreach (var record in missingInBilling)
                {
                    try
                    {
                        _logger.LogInformation("Attempting to create client: {clientDetails}",
                                new { record.EddsClientArtifactId, record.EddsClientNumber, record.EddsClientName });

                        CreateResult result = await ObjectHandler.CreateNewClient(
                            objectManager,
                            billingManagementDatabase,
                            record.EddsClientNumber,
                            record.EddsClientName,
                            record.EddsClientArtifactId,
                            _logger,
                            _helper);

                        if (result == null)
                        {
                            _logger.LogError("CreateNewClient returned null result for client {ClientNumber}", record.EddsClientNumber);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating client: {ClientDetails}. Error:{ErrorMessage}",
                            new { record.EddsClientArtifactId, record.EddsClientNumber, record.EddsClientName },
                            ex.Message);
                    }
                }
            }
        }
    }
}