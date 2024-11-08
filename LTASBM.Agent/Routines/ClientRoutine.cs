using LTASBM.Agent.Handlers;
using Relativity.API;
using Relativity.Services.Objects;
using Relativity.Services.Objects.DataContracts;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTASBM.Agent.Routines
{
    public class ClientRoutine
    {
        private readonly IAPILog _logger;

        public ClientRoutine(IAPILog logger)
        {
            _logger = logger.ForContext<ClientRoutine>();
        }

        public async Task ProcessClientRoutines(int billingManagementDatabase, IObjectManager objectManager, DataHandler dataHandler, IInstanceSettingsBundle instanceSettings)
        {
            StringBuilder emailBody = new StringBuilder();

            try
            {
                var eddsClients = dataHandler.EDDSClients();
                var billingClients = dataHandler.BillingClients();

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
                                _logger);
                            
                            if (result == null)
                            {
                                _logger.LogError("CreateNewClient returned null result for client {ClientNumber}", record.EddsClientNumber);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error creating client: {ClientDetails}. Error:{ErrorMessage}", 
                                new {record.EddsClientArtifactId, record.EddsClientNumber, record.EddsClientName }, 
                                ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error In ProcessClientRoutine");
            }            
        }
    }
}