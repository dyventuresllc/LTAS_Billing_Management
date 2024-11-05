using LTASBM.Agent.Handlers;
using LTASBM.Agent.Utilities;
using Relativity.API;
using Relativity.Services.Objects;
using System;
using System.Linq;
using System.Text;

namespace LTASBM.Agent.Routines
{
    public class ClientRoutine
    {
        private readonly IAPILog _logger;
        private readonly DataHandler _dataHandler;
        private readonly IInstanceSettingsBundle _instanceSettings;
        private readonly IServicesMgr _servicesMgr;

        public ClientRoutine(IAPILog logger, DataHandler dataHandler, IInstanceSettingsBundle instanceSettings, IServicesMgr servicesMgr)
        {
            _logger = logger;
            _dataHandler = dataHandler;
            _instanceSettings = instanceSettings;
            _servicesMgr = servicesMgr;
        }

        public async void ProcessClientRoutines(int billingManagementDatabase)
        {
            try 
            { 
                var eddsClients = _dataHandler.EDDSClients();
                var billingClients = _dataHandler.BillingClients();

                var invalidClients = eddsClients.Where(c => c.EddsClientNumber.Length != 5).ToList();

                if(invalidClients.Any()) 
                {
                    foreach (var c in invalidClients) 
                    {
                        StringBuilder emailBody = new StringBuilder();
                        emailBody = EmailsHtml.InvalidClientEmailBody(emailBody, c);
                        Emails.InvalidClientNumber(_instanceSettings, emailBody, c.EddsClientCreatedByEmail);
                    }                                  
                }

                var missingInBilling = eddsClients
                    .Where(edds => !billingClients
                    .Any(billing => billing.BillingEddsClientArtifactId == edds.EddsClientArtifactId)
                    && edds.EddsClientNumber.Length == 5)                    
                    .ToList();

                if (missingInBilling.Any())
                {
                    StringBuilder emailBody = new StringBuilder();
                    emailBody = EmailsHtml.NewClientsToBeCreated(emailBody, missingInBilling);
                    Emails.NewClientsToBeCreated(_instanceSettings, emailBody, "damienyoung@quinnemanuel.com");

                    foreach (var c in missingInBilling)
                    {                     
                        using (IObjectManager objectManager =_servicesMgr.CreateProxy<IObjectManager>(ExecutionIdentity.System))                         
                        {
                            var result = await Tasks.Tasks.CreateNewClient(objectManager, billingManagementDatabase, c.EddsClientNumber, c.EddsClientName, c.EddsClientArtifactId, _logger);
                            if (result == null && result.Object == null)
                            {
                                StringBuilder sb = new StringBuilder();
                                _logger.LogError($"Client - {{ArtifactID - {c.EddsClientArtifactId};Number - {c.EddsClientNumber};Name - {c.EddsClientName} not created in billing database, some error occurred.");
                                Emails.testemail(_instanceSettings, sb);
                            }
                            else 
                            {
                                StringBuilder sb = new StringBuilder();
                                sb.Append($"Client - {{ArtifactID - {c.EddsClientArtifactId};Number - {c.EddsClientNumber};Name - {c.EddsClientName} created in billing database, New object artifactid -{result.Object.ArtifactID}");
                                Emails.testemail(_instanceSettings, sb);
                            }
                        }                                              
                    }
                }
            }
            catch (Exception ex)
            {                
                string errorMessage = ex.InnerException != null ? string.Concat("---", ex.InnerException) : string.Concat("---", ex.Message);
                _logger.LogError(errorMessage);                
            }
        }
    }
}