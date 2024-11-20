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
    public class MatterManager
    {
        private const int VALID_MATTER_NUMBER_LENGTH = 11;     
        private readonly IObjectManager _objectManager;
        private readonly DataHandler _dataHandler;
        private readonly IInstanceSettingsBundle _instanceSettings;
        private readonly int _billingManagementDatabase;
        private readonly LTASBMHelper _ltasHelper;

        public MatterManager(
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
            _ltasHelper = new LTASBMHelper(helper, logger.ForContext<MatterManager>());
        }
        
        public async Task ProcessMatterRoutinesAsync()
        {           
            try
            {
                var eddsMatters = _dataHandler.EddsMatters();
                var billingMatters = _dataHandler.BillingMatters();
                
                await ProcessAllMatterOperationsAsync(eddsMatters, billingMatters);                
            }
            catch (Exception ex)
            {                
                _ltasHelper.Logger.LogError(ex, "Error In ProcessMatterRoutine");
            }
        }        
        private async Task ProcessAllMatterOperationsAsync(List<EddsMatters> eddsMatters, List<BillingMatters> billingMatters)
        {
            var invalidMatters = GetInvalidMatters(eddsMatters);
            await NotifyInvalidMattersAsync(invalidMatters);

            var duplicateMatter = GetDuplicateMatters(eddsMatters);
            await NotifiyDuplicateMattersAsync(duplicateMatter);

            var newMatters = GetNewMattersForBilling(eddsMatters, billingMatters);
            await ProcessNewMattesAsync(newMatters);
        }
        private IEnumerable<EddsMatters> GetNewMattersForBilling(List<EddsMatters> eddsMatters, List<BillingMatters> billingMatters)
            => eddsMatters
                .Where(edds => !billingMatters
                .Any(billing => billing.BillingEddsMatterArtifactId == edds.EddsMatterArtifactId)
                && edds.EddsMatterNumber.Length >= VALID_MATTER_NUMBER_LENGTH);
        private IEnumerable<EddsMatters> GetInvalidMatters(List<EddsMatters> eddsMatters)
            => eddsMatters.Where(c => c.EddsMatterNumber.Length < VALID_MATTER_NUMBER_LENGTH);
        private IEnumerable<EddsMatters> GetDuplicateMatters(List<EddsMatters> eddsMatters)
            => eddsMatters
                .GroupBy(c => c.EddsMatterNumber)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g)
                .ToList();
        private async Task NotifyInvalidMattersAsync(IEnumerable<EddsMatters> invalidMatters)
        {
            if (invalidMatters.Any())
            {
                foreach (var matter in invalidMatters)
                {
                    var emailBody = new StringBuilder();
                    emailBody = MessageHandler.InvalidMatterEmailBody(emailBody, matter);
                    await MessageHandler.Email.SentInvalidMatterNumberAsync(_instanceSettings, emailBody, matter.EddsMatterCreatedByEmail);
                }
            }
        }
        private async Task NotifiyDuplicateMattersAsync(IEnumerable<EddsMatters> duplicateMatters)
        {
            if(duplicateMatters.Any()) 
            {
                var emailBody = new StringBuilder();
                MessageHandler.DuplicateMattersEmailBody(emailBody, duplicateMatters.ToList());
                await MessageHandler.Email.SendInternalNotificationAsync(_instanceSettings, emailBody, "Duplicate Matters Found");
            }
        }
        private async Task ProcessNewMattesAsync(IEnumerable<EddsMatters> newMatters)
        {
            if (!newMatters.Any()) return;
            await NotifyNewMattersAsync(newMatters);
            await CreateNewMattersInBillingAsync(newMatters);
        }
        private async Task NotifyNewMattersAsync(IEnumerable<EddsMatters> newMatters)
        { 
            var emailBody = new StringBuilder();
            MessageHandler.NewMattersEmailBody(emailBody, newMatters.ToList());
            await MessageHandler.Email.SendNewMattersReportingAsync(_instanceSettings, emailBody);
        }
        private async Task CreateNewMattersInBillingAsync(IEnumerable<EddsMatters> newMatters)
        {
            foreach(var matter in newMatters)
            {
                try
                {
                    _ltasHelper.Logger.LogInformation("Attempting to create matter: {matterDetails}",
                        new { matter.EddsMatterArtifactId, matter.EddsMatterNumber, matter.EddsMatterName });
                    
                    int qryClientArtifactIDResult = await _ltasHelper.LookupClientArtifactID(_objectManager, _billingManagementDatabase, matter.EddsMatterNumber.Substring(0, 5).ToString());

                    if (qryClientArtifactIDResult == 0)
                    {
                        _ltasHelper.Logger.LogError($"Error getting Client ArtifactID for Matter Creation: {new { matter.EddsMatterArtifactId, matter.EddsMatterNumber, matter.EddsMatterName }}.");
                    }

                    var result = await ObjectHandler.CreateNewMatterAsync(
                            _objectManager,
                            _billingManagementDatabase,
                            matter.EddsMatterNumber,
                            matter.EddsMatterName,
                            matter.EddsMatterArtifactId,
                            qryClientArtifactIDResult,
                            _ltasHelper.Logger,
                            _ltasHelper.Helper);

                    if (result == null)
                    {
                        _ltasHelper.Logger.LogError($"CreateNewMatter returned null for matter {matter.EddsMatterNumber}");
                    }

                }
                catch (Exception ex)
                {
                    _ltasHelper.Logger.LogError(ex, "Error creating matter: {}. Error:{}",
                            new { matter.EddsMatterArtifactId, matter.EddsMatterNumber, matter.EddsMatterName }, ex.Message);
                }
            }            
        }
    }
}