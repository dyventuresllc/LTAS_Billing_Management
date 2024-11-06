using LTASBM.Agent.Handlers;
using LTASBM.Agent.Utilities;
using Relativity.API;
using Relativity.Services.Objects.DataContracts;
using System;
using System.Linq;
using System.Text;

namespace LTASBM.Agent.Routines
{
    public class ClientRoutine
    {
        public async void ProcessClientRoutines(int billingManagementDatabase, IServicesMgr servicesMgr, DataHandler dataHandler, IInstanceSettingsBundle instanceSettings, IAPILog logger)
        {
            StringBuilder emailBody = new StringBuilder();
            ObjectHandler objectHandler = new ObjectHandler(servicesMgr, logger);

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
                        //HELP: I believe the result isnt running or logging the error properly I do recall i had an issue with invalid guid being used on the create but i fixed it and added a int field update so I'm taking that out now. 
                        //      but definetly maynot not logging correctly to catch the issue on the call for the create new client.
                        CreateResult result = await objectHandler.CreateNewClient(billingManagementDatabase, record.EddsClientNumber, record.EddsClientName, record.EddsClientArtifactId);
                        if (result != null && result.Object != null)
                        {
                            emailBody.Clear();

                            emailBody.Append($"Task result: Artifact ID: {result.Object?.ArtifactID}, " +
                                             $"Success: {result != null}, " +
                                             $"Object Created: {result.Object != null}");

                            emailBody.AppendLine($"Client - {{ArtifactID - {record.EddsClientArtifactId};Number - {record.EddsClientNumber};Name - {record.EddsClientName} created in billing database, New object artifactid -{result.Object.ArtifactID}");
                            Emails.DebugEmail(instanceSettings, emailBody);
                        }
                        else if (result != null && result.Object == null)
                        {
                            emailBody.Clear();
                            emailBody.Append($"{{CreateResult object: {Newtonsoft.Json.JsonConvert.SerializeObject(result)}");
                            logger.ForContext<ObjectHandler>().LogError($"CreateResult object: {Newtonsoft.Json.JsonConvert.SerializeObject(result)}");

                            var eventHandlerStatuses = result.EventHandlerStatuses;
                            if (eventHandlerStatuses != null)
                            {
                                foreach (var status in eventHandlerStatuses)
                                {
                                    if (status.Message != null)
                                    {
                                        logger.ForContext<ObjectHandler>().LogError($"EventHandlerStatus Message: {status.Message} - result.object null");
                                        emailBody.Append($"status - {status.Message}");
                                    }
                                }
                            }
                            Emails.DebugEmail(instanceSettings, emailBody);
                        }
                        else if (result == null)
                        {
                            emailBody.Clear();
                            var eventHandlerStatuses = result.EventHandlerStatuses;
                            if (eventHandlerStatuses != null)
                            {
                                foreach (var status in eventHandlerStatuses)
                                {
                                    if (status.Message != null)
                                    {
                                        logger.ForContext<ObjectHandler>().LogError($"EventHandlerStatus Message: {status.Message} - result null");
                                        emailBody.Append($"status - {status.Message}");
                                    }
                                }
                            }
                            Emails.DebugEmail(instanceSettings, emailBody);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = ex.InnerException != null ? string.Concat("---", ex.InnerException, "---", ex.StackTrace) : string.Concat("---", ex.Message, "---", ex.StackTrace);
                logger.ForContext(typeof(ClientRoutine))
                      .LogError($"Error Client Rountine: {errorMessage}");
            }
        }
    }
}