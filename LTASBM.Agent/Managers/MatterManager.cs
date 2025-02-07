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
    public class MatterManager
    {
        private const int VALID_MATTER_NUMBER_LENGTH = 11;     
        private readonly IObjectManager _objectManager;
        private readonly DataHandler _dataHandler;
        private readonly IInstanceSettingsBundle _instanceSettings;
        private readonly int _billingManagementDatabase;
        private readonly LTASBMHelper _ltasHelper;
        private readonly ILTASLogger _logger;

        public MatterManager(
            IAPILog relativityLogger, 
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
            _ltasHelper = new LTASBMHelper(helper, relativityLogger.ForContext<MatterManager>());
            _logger = LoggerFactory.CreateLogger<MatterManager>(helper.GetDBContext(-1), helper, relativityLogger);
        }
        
        public async Task ProcessMatterRoutinesAsync()
        {           
            try
            {
                _logger.LogInformation("Starting matter routines processing");
                _logger.LogDebug("Retrieving EDDS matters");

                var eddsMatters = _dataHandler.EddsMatters();
                _logger.LogInformation("Retrieved {Count} EDDS matters", eddsMatters.Count);

                _logger.LogDebug("Retrieving billing matters");
                var billingMatters = _dataHandler.BillingMatters();
                _logger.LogInformation("Retrieved {Count} billing matters", billingMatters.Count);

                await ProcessAllMatterOperationsAsync(eddsMatters, billingMatters);

                _logger.LogInformation("Completed matter routines processing");
            }
            catch (Exception ex)
            {                
                _ltasHelper.Logger.LogError(ex, "Error In ProcessMatterRoutine");
                _logger.LogError(ex, "Error In ProcessMatterRoutine");
            }
        }        

        private async Task ProcessAllMatterOperationsAsync(List<EddsMatters> eddsMatters, List<BillingMatters> billingMatters)
        {
            _logger.LogDebug("Starting matter operations processing");

            var invalidMatters = GetInvalidMatters(eddsMatters);
            _logger.LogInformation("Found {Count} invalid matters", invalidMatters.Count());

            await NotifyInvalidMattersAsync(invalidMatters);

            var duplicateMatter = GetDuplicateMatters(eddsMatters);
            _logger.LogInformation("Found {Count} duplicate matters", duplicateMatter.Count());
            await NotifiyDuplicateMattersAsync(duplicateMatter);

            var newMatters = GetNewMattersForBilling(eddsMatters, billingMatters);
            _logger.LogInformation("Found {Count} new matters for billing", newMatters.Count());
            await ProcessNewMattesAsync(newMatters);

            _logger.LogDebug("Completed matter operations processing");
        }

        private IEnumerable<EddsMatters> GetNewMattersForBilling(List<EddsMatters> eddsMatters, List<BillingMatters> billingMatters)
            => eddsMatters
                .Where(edds => !billingMatters
                .Any(billing => billing.BillingEddsMatterArtifactId == edds.EddsMatterArtifactId)
                && edds.EddsMatterNumber.Length >= VALID_MATTER_NUMBER_LENGTH);
        
        //TODO: Remove
        //private IEnumerable<EddsMatters> GetInvalidMatters(List<EddsMatters> eddsMatters)
        //    => eddsMatters.Where(c => c.EddsMatterNumber.Length < VALID_MATTER_NUMBER_LENGTH);

        private IEnumerable<EddsMatters> GetInvalidMatters(List<EddsMatters> eddsMatters)
        {
            var invalidMatters = eddsMatters.Where(c => c.EddsMatterNumber.Length < VALID_MATTER_NUMBER_LENGTH);
            foreach (var matter in invalidMatters)
            {
                _logger.LogWarning("Invalid matter number found: {MatterNumber} for matter {MatterName} (ArtifactId: {ArtifactId})",
                    matter.EddsMatterNumber, matter.EddsMatterName, matter.EddsMatterArtifactId);
            }
            return invalidMatters;
        }

        //TODO: Remove
        //private IEnumerable<EddsMatters> GetDuplicateMatters(List<EddsMatters> eddsMatters)
        //    => eddsMatters
        //        .GroupBy(c => c.EddsMatterNumber)
        //        .Where(g => g.Count() > 1)
        //        .SelectMany(g => g)
        //        .ToList();

        private IEnumerable<EddsMatters> GetDuplicateMatters(List<EddsMatters> eddsMatters)
        {
            var duplicates = eddsMatters
                .GroupBy(c => c.EddsMatterNumber)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g)
                .ToList();

            foreach (var matter in duplicates)
            {
                _logger.LogWarning("Duplicate matter found: {MatterNumber} - {MatterName} (ArtifactId: {ArtifactId})",
                    matter.EddsMatterNumber, matter.EddsMatterName, matter.EddsMatterArtifactId);
            }
            return duplicates;
        }

        //TODO: Remove
        //private async Task NotifyInvalidMattersAsync(IEnumerable<EddsMatters> invalidMatters)
        //{
        //    if (invalidMatters.Any())
        //    {
        //        foreach (var matter in invalidMatters)
        //        {
        //            var emailBody = new StringBuilder();
        //            emailBody = MessageHandler.InvalidMatterEmailBody(emailBody, matter);
        //            await MessageHandler.Email.SentInvalidMatterNumberAsync(_instanceSettings, emailBody, matter.EddsMatterCreatedByEmail);
        //        }
        //    }
        //}

        private async Task NotifyInvalidMattersAsync(IEnumerable<EddsMatters> invalidMatters)
        {
            if (invalidMatters.Any())
            {
                _logger.LogInformation("Preparing to send invalid matter notifications for {Count} matters", invalidMatters.Count());
                foreach (var matter in invalidMatters)
                {
                    try
                    {
                        _logger.LogDebug("Sending invalid matter notification for {MatterNumber} to {Email}",
                            matter.EddsMatterNumber, matter.EddsMatterCreatedByEmail);

                        var emailBody = new StringBuilder();
                        emailBody = MessageHandler.InvalidMatterEmailBody(emailBody, matter);
                        await MessageHandler.Email.SentInvalidMatterNumberAsync(_instanceSettings, emailBody, matter.EddsMatterCreatedByEmail);

                        _logger.LogDebug("Successfully sent invalid matter notification for {MatterNumber}", matter.EddsMatterNumber);
                    }
                    catch (Exception ex)
                    {
                        _ltasHelper.Logger.LogError(ex, "Failed to send invalid matter notification for matter {MatterNumber}", matter.EddsMatterNumber);
                        _logger.LogError(ex, "Failed to send invalid matter notification for matter {MatterNumber}", matter.EddsMatterNumber);
                    }
                }
            }
        }

        //TODO: Remove
        //private async Task NotifiyDuplicateMattersAsync(IEnumerable<EddsMatters> duplicateMatters)
        //{
        //    if(duplicateMatters.Any()) 
        //    {
        //        var emailBody = new StringBuilder();
        //        MessageHandler.DuplicateMattersEmailBody(emailBody, duplicateMatters.ToList());
        //        await MessageHandler.Email.SendInternalNotificationAsync(_instanceSettings, emailBody, "Duplicate Matters Found");
        //    }
        //}

        private async Task NotifiyDuplicateMattersAsync(IEnumerable<EddsMatters> duplicateMatters)
        {
            if (duplicateMatters.Any())
            {
                _logger.LogInformation("Preparing to send duplicate matters notification for {Count} matters", duplicateMatters.Count());
                try
                {
                    var emailBody = new StringBuilder();
                    MessageHandler.DuplicateMattersEmailBody(emailBody, duplicateMatters.ToList());
                    await MessageHandler.Email.SendInternalNotificationAsync(_instanceSettings, emailBody, "Duplicate Matters Found");

                    _logger.LogInformation("Successfully sent duplicate matters notification");
                }
                catch (Exception ex)
                {
                    _ltasHelper.Logger.LogError(ex, "Failed to send duplicate matters notification");
                    _logger.LogError(ex, "Failed to send duplicate matters notification");
                }
            }
        }

        //TODO: Remove
        //private async Task ProcessNewMattesAsync(IEnumerable<EddsMatters> newMatters)
        //{
        //    if (!newMatters.Any()) return;
        //    await NotifyNewMattersAsync(newMatters);
        //    await CreateNewMattersInBillingAsync(newMatters);
        //}

        private async Task ProcessNewMattesAsync(IEnumerable<EddsMatters> newMatters)
        {
            if (!newMatters.Any())
            {
                _logger.LogInformation("No new matters to process");
                return;
            }

            _logger.LogInformation("Beginning processing of {Count} new matters", newMatters.Count());
            await NotifyNewMattersAsync(newMatters);
            await CreateNewMattersInBillingAsync(newMatters);
        }

        //TODO: Remove
        //private async Task NotifyNewMattersAsync(IEnumerable<EddsMatters> newMatters)
        //{ 
        //    var emailBody = new StringBuilder();
        //    MessageHandler.NewMattersEmailBody(emailBody, newMatters.ToList());
        //    await MessageHandler.Email.SendNewMattersReportingAsync(_instanceSettings, emailBody);
        //}

        private async Task NotifyNewMattersAsync(IEnumerable<EddsMatters> newMatters)
        {
            _logger.LogDebug("Preparing new matters notification email");
            try
            {
                var emailBody = new StringBuilder();
                MessageHandler.NewMattersEmailBody(emailBody, newMatters.ToList());
                await MessageHandler.Email.SendNewMattersReportingAsync(_instanceSettings, emailBody);

                _logger.LogInformation("Successfully sent new matters notification");
            }
            catch (Exception ex)
            {
                _ltasHelper.Logger.LogError(ex, "Failed to send new matters notification email");
                _logger.LogError(ex, "Failed to send new matters notification email");
            }
        }

        //TODO: Remove
        //private async Task CreateNewMattersInBillingAsync(IEnumerable<EddsMatters> newMatters)
        //{
        //    foreach(var matter in newMatters)
        //    {
        //        try
        //        {
        //            _ltasHelper.Logger.LogInformation("Attempting to create matter: {matterDetails}",
        //                new { matter.EddsMatterArtifactId, matter.EddsMatterNumber, matter.EddsMatterName });
                    
        //            int qryClientArtifactIDResult = await _ltasHelper.LookupClientArtifactID(_objectManager, _billingManagementDatabase, matter.EddsMatterClientEDDSArtifactID);

        //            if (qryClientArtifactIDResult == 0)
        //            {
        //                _ltasHelper.Logger.LogError($"Error getting Client ArtifactID for Matter Creation: {new { matter.EddsMatterArtifactId, matter.EddsMatterNumber, matter.EddsMatterName }}.");
        //            }

        //            var result = await ObjectHandler.CreateNewMatterAsync(
        //                    _objectManager,
        //                    _billingManagementDatabase,
        //                    matter.EddsMatterNumber,
        //                    matter.EddsMatterName,
        //                    matter.EddsMatterArtifactId,
        //                    qryClientArtifactIDResult,
        //                    _ltasHelper.Logger,
        //                    _ltasHelper.Helper);

        //            if (result == null)
        //            {
        //                _ltasHelper.Logger.LogError($"CreateNewMatter returned null for matter {matter.EddsMatterNumber}");
        //            }

        //        }
        //        catch (Exception ex)
        //        {
        //            _ltasHelper.Logger.LogError(ex, "Error creating matter: {}. Error:{}",
        //                    new { matter.EddsMatterArtifactId, matter.EddsMatterNumber, matter.EddsMatterName }, ex.Message);
        //        }
        //    }            
        //}

        private async Task CreateNewMattersInBillingAsync(IEnumerable<EddsMatters> newMatters)
        {
            foreach (var matter in newMatters)
            {
                try
                {
                    _logger.LogInformation("Attempting to create matter: {@MatterDetails}",
                        new { matter.EddsMatterArtifactId, matter.EddsMatterNumber, matter.EddsMatterName });

                    _logger.LogDebug("Looking up client artifact ID for matter {MatterNumber}", matter.EddsMatterNumber);
                    int qryClientArtifactIDResult = await _ltasHelper.LookupClientArtifactID(_objectManager,
                        _billingManagementDatabase, matter.EddsMatterClientEDDSArtifactID);

                    if (qryClientArtifactIDResult == 0)
                    {
                        _logger.LogError("Failed to get Client ArtifactID for Matter Creation: {@MatterDetails}",
                            new { matter.EddsMatterArtifactId, matter.EddsMatterNumber, matter.EddsMatterName });
                        continue;
                    }

                    _logger.LogDebug("Creating new matter in billing system: {MatterNumber}", matter.EddsMatterNumber);
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
                        _logger.LogError("CreateNewMatter returned null result for matter {MatterNumber}",
                            matter.EddsMatterNumber);
                    }
                    else
                    {
                        _logger.LogInformation("Successfully created matter in billing system: {MatterNumber}",
                            matter.EddsMatterNumber);
                    }
                }
                catch (Exception ex)
                {
                    _ltasHelper.Logger.LogError(ex, "Error creating matter: {@MatterDetails}",
                        new { matter.EddsMatterArtifactId, matter.EddsMatterNumber, matter.EddsMatterName });
                    _logger.LogError(ex, "Error creating matter: {@MatterDetails}",
                        new { matter.EddsMatterArtifactId, matter.EddsMatterNumber, matter.EddsMatterName });
                }
            }
        }
    }
}