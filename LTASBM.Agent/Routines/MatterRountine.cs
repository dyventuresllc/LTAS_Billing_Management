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
    public class MatterRoutine
    {
        private readonly IAPILog _logger;

        public MatterRoutine(IAPILog logger)
        {
            _logger = logger.ForContext<MatterRoutine>();
        }
        
        public async Task ProcessMatterRoutines(int billingManagementDatabase, IObjectManager objectManager, DataHandler dataHandler, IInstanceSettingsBundle instanceSettings)
        {
            StringBuilder emailBody = new StringBuilder();
         
            try
            {
                var eddsMatters = dataHandler.EddsMatters();
                var billingMatters = dataHandler.BillingMatters();
               
                //Handle invalid matter --  number should be 11 digits at min
                var invalidMatters = eddsMatters.Where(c => c.EddsMatterNumber.Length < 11).ToList();
                
                if (invalidMatters.Any())
                {
                    foreach (var record in invalidMatters)
                    {
                        emailBody.Clear();
                        emailBody = MessageHandler.InvalidMatterEmailBody(emailBody, record);
                        MessageHandler.Email.SentInvalidMatterNumber(instanceSettings, emailBody, "damienyoung@quinnemanuel.com");//record.EddsMatterCreatedByEmail);
                    }
                }

                //Handle new matters that need to be added to billing system
                var missingInBilling = eddsMatters
                    .Where(edds => !billingMatters
                    .Any(billing => billing.BillingEddsMatterArtifactId == edds.EddsMatterArtifactId)
                    && edds.EddsMatterNumber.Length >= 11)
                    .ToList();

                if (missingInBilling.Any())
                {
                    emailBody.Clear();
                    emailBody = MessageHandler.NewMattersEmailBody(emailBody, missingInBilling);
                    MessageHandler.Email.SendNewMattersReporting(instanceSettings, emailBody, "damienyoung@quinnemanuel.com");

                    foreach (var record in missingInBilling)
                    {
                        try
                        {
                            _logger.LogInformation("Attempting to create matter: {matterDetails}", 
                                    new { record.EddsMatterArtifactId, record.EddsMatterNumber, record.EddsMatterName });
                            
                            int qryClientArtifactIDResult = await ObjectHandler.LookupClientArtifactID(objectManager, billingManagementDatabase, record.EddsMatterNumber.Substring(1,5).ToString() , _logger);

                            CreateResult result = await ObjectHandler.CreateNewMatter(
                                objectManager,
                                billingManagementDatabase,
                                record.EddsMatterNumber,
                                record.EddsMatterName,
                                record.EddsMatterArtifactId,
                                qryClientArtifactIDResult,
                                _logger);

                            if (result == null)
                            {
                                _logger.LogError("CreateNewMatter returned null for matter {MatterNumber}", record.EddsMatterNumber);
                            }
                        }
                        catch (Exception ex) 
                        {
                            _logger.LogError(ex, "Error creating matter: {}. Error:{}",
                                new { record.EddsMatterArtifactId, record.EddsMatterNumber, record.EddsMatterName }, ex.Message);
                        }
                    }
                }                    
            }
            catch (Exception ex)
            {                
                _logger.LogError(ex, "Error In ProcessMatterRoutine");
            }
        }
    }
}