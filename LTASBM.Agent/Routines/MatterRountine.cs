using LTASBM.Agent.Handlers;
using LTASBM.Agent.Models;
using LTASBM.Agent.Utilites;
using Relativity.API;
using Relativity.Services.Objects;
using Relativity.Services.Objects.DataContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LTASBM.Agent.Routines
{
    public class MatterRoutine
    {
        private readonly IAPILog _logger;
        private readonly LTASBMHelper _ltasHelper;

        public MatterRoutine(IAPILog logger, IHelper helper)
        {
            _logger = logger.ForContext<MatterRoutine>();
            _ltasHelper = new LTASBMHelper(helper, logger);
        }
        
        public async Task ProcessMatterRoutines(int billingManagementDatabase, IObjectManager objectManager, DataHandler dataHandler, IInstanceSettingsBundle instanceSettings)
        {
            StringBuilder emailBody = new StringBuilder();
         
            try
            {
                var eddsMatters = dataHandler.EddsMatters();
                var billingMatters = dataHandler.BillingMatters();

                ProccessInvalidMatters(emailBody, eddsMatters, instanceSettings);
                ProcessDuplicateMatters(emailBody, eddsMatters, instanceSettings);
                await ProcessNewMattersToBillingAsync(objectManager, billingManagementDatabase, emailBody, eddsMatters, billingMatters, instanceSettings);
            }
            catch (Exception ex)
            {                
                _logger.LogError(ex, "Error In ProcessMatterRoutine");
            }
        }
        private void ProccessInvalidMatters(StringBuilder emailBody, List<EddsMatters> eddsMatters, IInstanceSettingsBundle instanceSettings)
        {
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
        }
        private void ProcessDuplicateMatters(StringBuilder emailBody,List<EddsMatters> eddsMatters, IInstanceSettingsBundle instanceSettings)
        {
            var duplicateMatters = eddsMatters
                .GroupBy(c => c.EddsMatterNumber)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g)
                .ToList();

            if (duplicateMatters.Any())
            {
                MessageHandler.DuplicateMattersEmailBody(emailBody, duplicateMatters);
                MessageHandler.Email.SendDebugEmail(instanceSettings, emailBody, "Duplicate Matters Found");
            }
        }
        private async Task ProcessNewMattersToBillingAsync(IObjectManager objectManager, int billingManagementDatabase, StringBuilder emailBody, List<EddsMatters> eddsMatters, List<BillingMatters> billingMatters, IInstanceSettingsBundle instanceSettings)
        {
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
                MessageHandler.Email.SendNewMattersReporting(instanceSettings, emailBody);

                foreach (var record in missingInBilling)
                {
                    try
                    {
                        _logger.LogInformation("Attempting to create matter: {matterDetails}",
                                new { record.EddsMatterArtifactId, record.EddsMatterNumber, record.EddsMatterName });

                        int qryClientArtifactIDResult = await _ltasHelper.LookupClientArtifactID(objectManager, billingManagementDatabase, record.EddsMatterNumber.Substring(0, 5).ToString());                            

                        if (qryClientArtifactIDResult == 0)
                        {
                            _logger.LogError($"Error getting Client ArtifactID for Matter Creation: {new { record.EddsMatterArtifactId, record.EddsMatterNumber, record.EddsMatterName }}.");
                        }

                        CreateResult result = await ObjectHandler.CreateNewMatter(
                            objectManager,
                            billingManagementDatabase,
                            record.EddsMatterNumber,
                            record.EddsMatterName,
                            record.EddsMatterArtifactId,
                            qryClientArtifactIDResult,
                            _logger,
                            _ltasHelper.Helper);

                        if (result == null)
                        {
                            _logger.LogError($"CreateNewMatter returned null for matter {record.EddsMatterNumber}");
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
    }
}