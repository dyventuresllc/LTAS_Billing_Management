using LTASBM.Agent.Handlers;
using LTASBM.Agent.Logging;
using LTASBM.Agent.Models;
using LTASBM.Agent.Models.Metadata;
using LTASBM.Agent.Utilites;
using Newtonsoft.Json;
using Relativity.API;
using Relativity.Services.Exceptions;
using Relativity.Services.Objects;
using Relativity.Services.Objects.DataContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace LTASBM.Agent.Managers
{
    public class BillingManager
    {
        private readonly IObjectManager _objectManager;
        private readonly LTASBMHelper _ltasHelper;
        private readonly IInstanceSettingsBundle _instanceSettings;
        private readonly int _billingManagementDatabase;
        private readonly DataHandler _dataHandler;
        private readonly BillingAPIHandler _billingApi;
        private readonly ILTASLogger _logger;
        private readonly IAPILog _relativityLogger;
        private readonly IHelper _helper;
        //private readonly MetadataFields _metadataFields;
        

        public string FieldGuidValuePublishedDocumentSizeId { get; private set; }
        public string FieldGuidValueWorkspaceArtifactId { get; private set; }

        public BillingManager(
            IAPILog relativityLogger,
            IHelper helper,
            IObjectManager objectManager,
            DataHandler dataHandler,
            IInstanceSettingsBundle instanceSettings,
            int billingManagementDatabase,
            string instanceId,
            string token,
            string instanceUrl)
        {
            _instanceSettings = instanceSettings ?? throw new ArgumentNullException(nameof(instanceSettings));
            _dataHandler = dataHandler ?? throw new ArgumentNullException(nameof(dataHandler));
            _objectManager = objectManager ?? throw new ArgumentNullException(nameof(objectManager));
            _ltasHelper = new LTASBMHelper(helper, relativityLogger.ForContext<BillingManager>());
            _billingManagementDatabase = billingManagementDatabase;
            _billingApi = new BillingAPIHandler(relativityLogger, instanceId, token, instanceUrl);
            _logger = LoggerFactory.CreateLogger<BillingManager>(helper.GetDBContext(-1), helper, relativityLogger);
            //_metadataFields = new MetadataFields();
            _helper = helper;
            _relativityLogger = relativityLogger;
        }

        public async Task ProcessBillingMetricsAsync()
        {
            try
            {
                _logger.LogInformation("Starting ProcessBillingMetricsAsync");

                // Initial data fetching
                var matters = _dataHandler.BillingMatters();
                var billingworkspaces = _dataHandler.BillingWorkspaces();
                _logger.LogInformation($"Retrieved {matters?.Count ?? 0} matters and {billingworkspaces?.Count ?? 0} billing workspaces");

                // Clear temp tables
                await _dataHandler.TruncateBillingDetailsTempWorkspace();
                await _dataHandler.TruncateBillingDetailsTempUsers();
                _logger.LogInformation("Temporary tables truncated successfully");

                // Get metadata fields
                var (fieldMetadata, fieldsFound) = await GetAndSetMetadataFields();
                if (!fieldsFound)
                {
                    _logger.LogError("Required metadata fields missing - stopping Billing API calls");
                    return;
                }

                // Date setup
                var dateKey = DateTime.Now.ToString("yyyyMM");
                var (startDate, endDate) = GetMonthDateRange(DateTime.Now);
                _logger.LogInformation($"Processing for date range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}, DateKey: {dateKey}");

                // Get billing workspaces
                var billingWorkspaces = await ObjectHandler.WorkspacesForBilling(
                    _objectManager,
                    _billingManagementDatabase,
                    _ltasHelper.WorkspaceObjectType,
                    _ltasHelper.WorkspaceEDDSArtifactIDField,
                    _ltasHelper.WorkspaceMatterNumberField,
                    _ltasHelper.Logger);

                // Generate usage report
                var usageReportData = await GenerateReport(startDate, endDate, fieldMetadata);
                if (usageReportData == null || billingWorkspaces?.Objects == null || !billingWorkspaces.Objects.Any())
                {
                    await SendCriticalDataMissingNotification(usageReportData, billingWorkspaces);
                    return;
                }

                _logger.LogInformation($"Retrieved {usageReportData.Count} usage report records");
                await ProcessWorkspaceMetrics(billingWorkspaces, dateKey, usageReportData);

                // Get summary data
                var summaryMetrics = _dataHandler.BillingSummaryMetrics();
                var summaryWorkspaces = _dataHandler.BillingSummaryWorkspaces();
                var summaryUsers = _dataHandler.BillingSummaryUsers();
                var existingDetails = _dataHandler.BillingDetails();
                var existingDetailsOrgCount = existingDetails.Count;

                _logger.LogInformation($"Retrieved summaries - Metrics: {summaryMetrics?.Count ?? 0}, " +
                    $"Workspaces: {summaryWorkspaces?.Count ?? 0}, Users: {summaryUsers?.Count ?? 0}, " +
                    $"Details: {existingDetails?.Count ?? 0}");

                // Delete current datekey records
                _logger.LogInformation($"Deleting records for datekey: {dateKey}");
                var deletedRecordsCount = await _dataHandler.DeleteBillingDetailsCurrentDateKey(dateKey);
                _logger.LogInformation($"Deleted {deletedRecordsCount} records");

                // Wait for deletion confirmation
                await ConfirmDeletion(dateKey, deletedRecordsCount, maxRetries: 4);
                existingDetails = _dataHandler.BillingDetails();
                _logger.LogInformation($"Details after deletion - {existingDetails?.Count ?? 0} records");

                // Calculate and create new records
                var (recordsToUpdate, recordsToInsert) = SeparateRecords(summaryMetrics, existingDetails);
                if (recordsToInsert.Count > 0)
                {
                    int expectedTotalRecords = existingDetailsOrgCount - deletedRecordsCount + recordsToInsert.Count;
                    _logger.LogInformation($"Expected records after creation: {expectedTotalRecords} " +
                        $"(Original: {existingDetailsOrgCount} - Deleted: {deletedRecordsCount} + New: {recordsToInsert.Count})");

                    await CreateBillingDetails(recordsToInsert);
                    await VerifyRecordCreation(expectedTotalRecords, maxRetries: 4);
                }

                // Final verification and processing
                existingDetails = _dataHandler.BillingDetails();
                _logger.LogInformation($"Final record count: {existingDetails.Count}");

                var billingOverrides = _dataHandler.BillingOverrides();
                _logger.LogInformation($"Retrieved {billingOverrides?.Count ?? 0} billing overrides");

                await UpdateActiveBilling(summaryMetrics, usageReportData);
                await ProcessSummaryMetrics(summaryMetrics, summaryWorkspaces, summaryUsers,
                    existingDetails, billingworkspaces, billingOverrides);

                _logger.LogInformation("ProcessBillingMetricsAsync completed successfully");
            }
            catch (Exception ex)
            {
                string errorMessage = ex.InnerException != null
                    ? String.Concat(ex.InnerException.Message, "---", ex.StackTrace)
                    : String.Concat(ex.Message, "---", ex.StackTrace);
                _ltasHelper.Logger.ForContext<BillingManager>().LogError($"{errorMessage}");
                _logger.LogError($"ProcessBillingMetricsAsync failed: {errorMessage}", ex);
                throw;
            }
        }

        private async Task ConfirmDeletion(string dateKey, int deletedRecordsCount, int maxRetries = 4)
        {
            if (deletedRecordsCount == 0)
            {
                _logger.LogInformation("No records deleted, confirmation not needed");
                return;
            }

            int currentRetry = 0;
            bool deletionConfirmed = false;

            while (!deletionConfirmed && currentRetry < maxRetries)
            {
                var remainingRecords = await _dataHandler.GetRemainingRecordsCount(dateKey);
                _logger.LogInformation($"Deletion check attempt {currentRetry + 1}: {remainingRecords} records remaining");

                if (remainingRecords == 0)
                {
                    deletionConfirmed = true;
                    _logger.LogInformation("Deletion confirmed");
                }
                else
                {
                    currentRetry++;
                    if (currentRetry < maxRetries)
                    {
                        _logger.LogWarning($"{remainingRecords} records still remain. Waiting 30 seconds...");
                        await Task.Delay(15000);
                    }
                }
            }

            if (!deletionConfirmed)
            {
                _logger.LogError($"Deletion confirmation failed after {maxRetries} attempts");
            }
        }

        private async Task VerifyRecordCreation(int expectedTotalRecords, int maxRetries = 4)
        {
            int currentRetry = 0;
            bool recordsVerified = false;

            while (!recordsVerified && currentRetry < maxRetries)
            {
                var existingDetails = _dataHandler.BillingDetails();
                _logger.LogInformation($"Record verification attempt {currentRetry + 1}: {existingDetails.Count} of {expectedTotalRecords} records found");

                if (existingDetails.Count >= expectedTotalRecords)
                {
                    recordsVerified = true;
                    _logger.LogInformation("Record creation verified");
                }
                else
                {
                    currentRetry++;
                    if (currentRetry < maxRetries)
                    {
                        _logger.LogWarning($"Missing {expectedTotalRecords - existingDetails.Count} records. Waiting 30 seconds...");
                        await Task.Delay(60000);
                    }
                }
            }

            if (!recordsVerified)
            {
                _logger.LogError($"Record creation verification failed after {maxRetries} attempts");
            }
        }

        private async Task SendCriticalDataMissingNotification(List<SecondaryData> usageReportData, QueryResult billingWorkspaces)
        {
            _logger.LogError($"Critical data missing: Usage report data null: {usageReportData == null}, " +
                $"Billing Workspaces null: {billingWorkspaces?.Objects == null}, " +
                $"Billing Workspaces empty: {billingWorkspaces?.Objects?.Any() == false}");

            StringBuilder sb = new StringBuilder()
                .AppendLine("Critical data collection failure - process cannot continue")
                .AppendLine("<br>")
                .AppendLine("Usage Report Data and Billing Workspaces are null or empty");

            await MessageHandler.Email.SendInternalNotificationAsync(
                _instanceSettings,
                sb,
                "Critical Data Collection Failure"
            );
        }
        private async Task<(MetadataFields Fields, bool AllFieldsFound)> GetAndSetMetadataFields()
        {
            try
            {
                _logger.LogInformation("Starting GetAndSetMetadataFields");
                var responseMetadataFields = await _billingApi.GetReportMetadataAsync();
                if (string.IsNullOrEmpty(responseMetadataFields))
                {
                    _logger.LogError("GetReportMetadataAsync returned null or empty response");
                    return (null, false);
                }

                var metadata = JsonConvert.DeserializeObject<MetadataResponse>(responseMetadataFields);
                if (metadata?.Origins == null)
                {
                    _logger.LogError("Metadata response or Origins is null");
                    return (null, false);
                }

                var metadataFields = new MetadataFields();
                var validator = new MetadataValidator(metadataFields, _relativityLogger, _helper);
                var allFieldsFound = validator.PopulateMetadataFields(metadata);

                if (!allFieldsFound)
                {
                    _logger.LogError("Not all required fields were found or are valid");
                    // Here you could add your email notification logic
                    return (metadataFields, false);
                }

                _logger.LogInformation("Successfully validated and set all required field IDs");
                return (metadataFields, true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAndSetMetadataFields: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        //TODO: Remove
        //private async Task<List<ProcessingData>> GenerateAndDownloadReport(DateTime startDate, DateTime endDate)
        //{
        //    var reportGuid = Guid.NewGuid();
        //    var reportResponse = await _billingApi.GetUsageReportAsync(
        //        $"Processing - {reportGuid}",
        //        startDate,
        //        endDate,
        //        FieldGuidValueWorkspaceArtifactId,
        //        FieldGuidValuePublishedDocumentSizeId);

        //    if (await _billingApi.WaitForReportCompletion(reportResponse.Id))
        //    {
        //        return await _billingApi.DownloadReportAsync(reportResponse.Id, _instanceSettings);
        //    }

        //    return null;
        //}

        //private async Task<(List<SecondaryData> Primary, List<SecondaryData> Secondary)> GenerateAndDownloadAllReports(DateTime startDate, DateTime endDate)
        //{
        //    try
        //    {
        //        _logger.LogInformation($"Generating both primary and secondary reports for {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

        //        // Validate metadata fields
        //        if (PrimaryMetadataFields == null)
        //        {
        //            _logger.LogError("PrimaryMetadataFields is null");
        //            return (null, null);
        //        }

        //        if (SecondaryMetadataFields == null)
        //        {
        //            _logger.LogError("SecondaryMetadataFields is null");
        //            return (null, null);
        //        }

        //        _logger.LogInformation("Validating primary field IDs...");
        //        if (string.IsNullOrEmpty(PrimaryMetadataFields.WorkspaceArtifactId))
        //        {
        //            _logger.LogError("PrimaryMetadataFields.WorkspaceArtifactId is null or empty");
        //            return (null, null);
        //        }

        //        if (string.IsNullOrEmpty(PrimaryMetadataFields.PublishedDocumentSizeId))
        //        {
        //            _logger.LogError("PrimaryMetadataFields.PublishedDocumentSizeId is null or empty");
        //            return (null, null);
        //        }

        //        _logger.LogInformation("Validating secondary field IDs...");
        //        if (string.IsNullOrEmpty(SecondaryMetadataFields.WorkspaceArtifactId))
        //        {
        //            _logger.LogError("SecondaryMetadataFields.WorkspaceArtifactId is null or empty");
        //            return (null, null);
        //        }

        //        // Primary Report
        //        _logger.LogInformation("Generating primary report...");
        //        var primaryReportGuid = Guid.NewGuid();
        //        var primaryReport = await _billingApi.GetUsageReportAsync(
        //            $"Processing Primary - {primaryReportGuid}",
        //            startDate,
        //            endDate,
        //            PrimaryMetadataFields.WorkspaceArtifactId,
        //            PrimaryMetadataFields.PublishedDocumentSizeId);

        //        if (primaryReport == null)
        //        {
        //            _logger.LogError("Primary report generation failed - GetUsageReportAsync returned null");
        //            return (null, null);
        //        }

        //        if (string.IsNullOrEmpty(primaryReport.Id))
        //        {
        //            _logger.LogError("Primary report generation failed - report ID is null or empty");
        //            return (null, null);
        //        }

        //        // Secondary Report
        //        _logger.LogInformation("Generating secondary report...");
        //        var secondaryReportGuid = Guid.NewGuid();
        //        var validFields = new List<string>
        //{
        //    SecondaryMetadataFields.WorkspaceArtifactId,
        //    SecondaryMetadataFields.PublishedDocumentSizeId,
        //};

        //        // Only add non-null fields
        //        if (!string.IsNullOrEmpty(SecondaryMetadataFields.LinkedTotalFileSizeId))
        //            validFields.Add(SecondaryMetadataFields.LinkedTotalFileSizeId);
        //        if (!string.IsNullOrEmpty(SecondaryMetadataFields.PeakWorkspaceHostedSizeId))
        //            validFields.Add(SecondaryMetadataFields.PeakWorkspaceHostedSizeId);
        //        if (!string.IsNullOrEmpty(SecondaryMetadataFields.TranslateDocumentUnitsId))
        //            validFields.Add(SecondaryMetadataFields.TranslateDocumentUnitsId);

        //        _logger.LogInformation($"Secondary report using {validFields.Count} valid fields");

        //        var secondaryReport = await _billingApi.GetUsageReportAsync(
        //            $"Processing Secondary - {secondaryReportGuid}",
        //            startDate,
        //            endDate,
        //            validFields.ToArray());

        //        if (secondaryReport == null)
        //        {
        //            _logger.LogError("Secondary report generation failed - GetUsageReportAsync returned null");
        //            return (null, null);
        //        }

        //        if (string.IsNullOrEmpty(secondaryReport.Id))
        //        {
        //            _logger.LogError("Secondary report generation failed - report ID is null or empty");
        //            return (null, null);
        //        }

        //        var primaryRequiredColumns = new[]
        //        {
        //            "Instance Name",
        //            "Workspace Name",
        //            "Client Name",
        //            "Matter Name",
        //            "Workspace Utilization Capture Date-Time",
        //            "Processing Metrics Capture Date-Time",
        //            "Workspace ArtifactID",
        //            "Published Document Size [GB]"
        //        };

        //        var secondaryRequiredColumns = new[]
        //        {
        //            "Instance Name",
        //            "Workspace Name",
        //            "Workspace ArtifactID",
        //            "Published Document Size [GB]",
        //            "Linked Total File Size [GB]",
        //            "Peak Workspace Hosted Size [GB]",
        //            "Translate Document Units"
        //        };


        //        List<SecondaryData> primaryData = null;
        //        List<SecondaryData> secondaryData = null;

        //        // Download primary report
        //        _logger.LogInformation($"Waiting for primary report completion (ID: {primaryReport.Id})");
        //        if (await _billingApi.WaitForReportCompletion(primaryReport.Id))
        //        {
        //            _logger.LogInformation("Primary report completed, downloading...");
        //            primaryData = await _billingApi.DownloadReportAsync(primaryReport.Id, _instanceSettings, primaryRequiredColumns, "Primary");
        //            _logger.LogInformation($"Downloaded primary report with {primaryData?.Count ?? 0} records");
        //        }
        //        else
        //        {
        //            _logger.LogError("Primary report failed to complete");
        //        }

        //        // Download secondary report
        //        _logger.LogInformation($"Waiting for secondary report completion (ID: {secondaryReport.Id})");
        //        if (await _billingApi.WaitForReportCompletion(secondaryReport.Id))
        //        {
        //            _logger.LogInformation("Secondary report completed, downloading...");
        //            secondaryData = await _billingApi.DownloadReportAsync(secondaryReport.Id, _instanceSettings, secondaryRequiredColumns, "Secondary");
        //            _logger.LogInformation($"Downloaded secondary report with {secondaryData?.Count ?? 0} records");
        //        }
        //        else
        //        {
        //            _logger.LogError("Secondary report failed to complete");
        //        }

        //        _logger.LogInformation("GenerateAndDownloadAllReports completed");
        //        return (primaryData, secondaryData);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Error in GenerateAndDownloadAllReports: {ex.Message}");
        //        _logger.LogError($"Stack trace: {ex.StackTrace}");
        //        if (ex.InnerException != null)
        //        {
        //            _logger.LogError($"Inner exception: {ex.InnerException.Message}");
        //            _logger.LogError($"Inner stack trace: {ex.InnerException.StackTrace}");
        //        }
        //        throw;
        //    }
        //}

        //TODO: Remove
        //private async Task ProcessWorkspaceMetrics(QueryResult billingWorkspaces, string dateKey, List<ProcessingData> processingData)
        //{
        //    var (startDate, endDate) = GetMonthDateRange(DateTime.Now);
        //    var failures = new List<(int WorkspaceId, string Error)>();
        //    List<BillingReportUsers> usersPerCase = new List<BillingReportUsers>();
        //    int pageCountPerCase = 0;
        //    StringBuilder sb = new StringBuilder();


        //    foreach (var workspace in billingWorkspaces.Objects)
        //    {
        //        int matterArtifactId = 0;
        //        int workspaceEddsArtifactId = 0;

        //        try
        //        {
        //            (matterArtifactId, workspaceEddsArtifactId) = ExtractWorkspaceIds(workspace);
        //            var billingMetrics = await GetWorkspaceBillingMetrics(workspaceEddsArtifactId);

        //            try
        //            {
        //                var workspaceUsers = await Task.Run(() => _dataHandler.GetUsersForCase(workspaceEddsArtifactId, matterArtifactId));
        //                if (workspaceUsers.Any())
        //                {
        //                    await Task.Run(() => _dataHandler.InsertIntoBillingUserInfo(workspaceUsers));
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                failures.Add((workspaceEddsArtifactId, $"Failed to process users: {ex.Message}"));
        //                _ltasHelper.Logger.LogError(ex, $"Error processing users for workspace {workspaceEddsArtifactId}");
        //                sb.Clear();
        //                sb.AppendLine($"Error processing users:<br>{ex.Message}<br><br>Stack Trace: {ex.StackTrace}");
        //                await MessageHandler.Email.SendInternalNotificationAsync(_instanceSettings, sb, $"Billing data failure for workspace: {workspaceEddsArtifactId}");
        //            }

        //            try
        //            {
        //                pageCountPerCase = _dataHandler.GetWorkspaceImageCount(startDate, endDate, workspaceEddsArtifactId);
        //                if (billingMetrics != null)
        //                {
        //                    var newMetrics = CreateBillingMetrics(workspaceEddsArtifactId, matterArtifactId, dateKey, billingMetrics, processingData, pageCountPerCase);
        //                    await SaveMetricsToDatabase(new List<TempBilingDetailsWorkspace> { newMetrics });
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                failures.Add((workspaceEddsArtifactId, $"Failed to process metrics: {ex.Message}"));
        //                _ltasHelper.Logger.LogError(ex, $"Error processing metrics for workspace {workspaceEddsArtifactId}");
        //                sb.Clear();
        //                sb.AppendLine($"Error processing metrics:<br>{ex.Message}<br><br>Stack Trace: {ex.StackTrace}");
        //                await MessageHandler.Email.SendInternalNotificationAsync(_instanceSettings, sb, $"Billing data failure for workspace: {workspaceEddsArtifactId}");
        //            }

        //        }
        //        catch (Exception ex)
        //        {
        //            failures.Add((workspaceEddsArtifactId, $"Failed to process workspace: {ex.Message}"));
        //            _ltasHelper.Logger.LogError(ex, $"Error processing workspace {workspaceEddsArtifactId}");
        //            sb.Clear();
        //            sb.AppendLine("Some other error processing billing metrics check the logs");
        //            sb.AppendLine("<br>");
        //            sb.AppendLine($"workspace: {workspaceEddsArtifactId}");
        //            await MessageHandler.Email.SendInternalNotificationAsync(_instanceSettings, sb, "Billing data failure check logs");
        //        }
        //    }

        //    if (failures.Any())
        //    {
        //        var failureMessage = string.Join("\n", failures.Select(f => $"Workspace {f.WorkspaceId}: {f.Error}"));
        //        _ltasHelper.Logger.LogError($"Failed to process some workspaces:\n{failureMessage}");
        //    }
        //}

        //TODO: Remove
        //private async Task<List<SecondaryData>> GenerateReport(DateTime startDate, DateTime endDate, MetadataFields metadataFields)
        //{
        //    try
        //    {
        //        _logger.LogInformation($"Generating report for {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

        //        var fieldIds = new[]
        //        {
        //            metadataFields.WorkspaceArtifactId,
        //            metadataFields.PublishedDocumentSizeId,
        //            metadataFields.PeakWorkspaceHostedSizeId,
        //            metadataFields.LinkedTotalFileSizeId,
        //            metadataFields.TranslateDocumentUnitsId,
        //            metadataFields.AirForPrivilegeDocumentsId,
        //            metadataFields.AirForReviewDocumentsId
        //        }.Where(id => !string.IsNullOrEmpty(id)).ToArray();

        //        var report = await _billingApi.GetUsageReportAsync(
        //            $"Usage Report - {Guid.NewGuid()}",
        //            startDate,
        //            endDate,
        //            fieldIds);

        //        if (!await _billingApi.WaitForReportCompletion(report?.Id))
        //        {
        //            _logger.LogError("Report failed to complete");
        //            return null;
        //        }

        //        return await _billingApi.DownloadReportAsync(report.Id, _instanceSettings);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Error in GenerateReport: {ex.Message}");
        //        throw;
        //    }
        //}

        private async Task<List<SecondaryData>> GenerateReport(DateTime startDate, DateTime endDate, MetadataFields metadataFields)
        {
            try
            {
                _logger.LogInformation($"Generating report for {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

                var fieldIds = new[]
                {
                    metadataFields.WorkspaceArtifactId,
                    metadataFields.PublishedDocumentSizeId,
                    metadataFields.PeakWorkspaceHostedSizeId,
                    metadataFields.LinkedTotalFileSizeId,
                    metadataFields.TranslateDocumentUnitsId,
                    metadataFields.AirForPrivilegeDocumentsId,
                    metadataFields.AirForReviewDocumentsId,
                    metadataFields.WorkspaceTypeId
                }.Where(id => !string.IsNullOrEmpty(id)).ToArray();

                _logger.LogInformation($"Field IDs for report:" +
                    $"\nWorkspaceTypeId: {metadataFields.WorkspaceTypeId}" +
                    $"\nWorkspaceArtifactId: {metadataFields.WorkspaceArtifactId}" +
                    $"\nPublishedDocumentSizeId: {metadataFields.PublishedDocumentSizeId}" +
                    $"\nPeakWorkspaceHostedSizeId: {metadataFields.PeakWorkspaceHostedSizeId}" +
                    $"\nLinkedTotalFileSizeId: {metadataFields.LinkedTotalFileSizeId}" +
                    $"\nTranslateDocumentUnitsId: {metadataFields.TranslateDocumentUnitsId}" +
                    $"\nAirForPrivilegeDocumentsId: {metadataFields.AirForPrivilegeDocumentsId}" +
                    $"\nAirForReviewDocumentsId: {metadataFields.AirForReviewDocumentsId}" +
                    $"\nTotal valid fields: {fieldIds.Length}");

                if (fieldIds.Length == 0)
                {
                    _logger.LogError("No valid field IDs found for report generation");
                    return null;
                }

                var reportName = $"Usage Report - {Guid.NewGuid()}";
                _logger.LogInformation($"Getting usage report '{reportName}' with {fieldIds.Length} fields");

                var report = await _billingApi.GetUsageReportAsync(
                    reportName,
                    startDate,
                    endDate,
                    fieldIds);

                if (report == null)
                {
                    _logger.LogError("Failed to get usage report - report is null");
                    return null;
                }

                _logger.LogInformation($"Waiting for report completion. Report ID: {report.Id}");
                if (!await _billingApi.WaitForReportCompletion(report.Id))
                {
                    _logger.LogError($"Report {report.Id} failed to complete");
                    return null;
                }

                _logger.LogInformation($"Report {report.Id} completed successfully. Starting download...");
                var data = await _billingApi.DownloadReportAsync(report.Id, _instanceSettings);

                if (data == null)
                {
                    _logger.LogError($"Download failed for report {report.Id} - no data returned");
                    return null;
                }

                _logger.LogInformation($"Download complete for report {report.Id}. Records retrieved: {data.Count}");

                if (data.Count == 0)
                {
                    _logger.LogWarning($"Report {report.Id} downloaded successfully but contains no records");
                }                

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GenerateReport: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        //TODO: Remove
        //private async Task ProcessWorkspaceMetrics(QueryResult billingWorkspaces, string dateKey,
        //    List<SecondaryData> primaryData, List<SecondaryData> secondaryData)
        //{
        //    try
        //    {
        //        _logger.LogInformation("Starting ProcessWorkspaceMetrics");
        //        var (startDate, endDate) = GetMonthDateRange(DateTime.Now);
        //        var failures = new List<(int WorkspaceId, string Error)>();
        //        StringBuilder sb = new StringBuilder();

        //        // Create lookup dictionaries for efficient data access
        //        var primaryDataLookup = primaryData?.ToDictionary(p => p.WorkspaceArtifactId, p => p)
        //            ?? new Dictionary<int, SecondaryData>();
        //        var secondaryDataLookup = secondaryData?.ToDictionary(s => s.WorkspaceArtifactId, s => s)
        //            ?? new Dictionary<int, SecondaryData>();

        //        _logger.LogInformation($"Processing {billingWorkspaces.Objects.Count} workspaces");
        //        _logger.LogInformation($"Have {primaryDataLookup.Count} primary metrics and {secondaryDataLookup.Count} secondary metrics");

        //        foreach (var workspace in billingWorkspaces.Objects)
        //        {
        //            int matterArtifactId = 0;
        //            int workspaceEddsArtifactId = 0;

        //            try
        //            {
        //                (matterArtifactId, workspaceEddsArtifactId) = ExtractWorkspaceIds(workspace);
        //                _logger.LogInformation($"Processing workspace {workspaceEddsArtifactId} for matter {matterArtifactId}");

        //                var billingMetrics = await GetWorkspaceBillingMetrics(workspaceEddsArtifactId);

        //                try
        //                {
        //                    var workspaceUsers = await Task.Run(() =>
        //                        _dataHandler.GetUsersForCase(workspaceEddsArtifactId, matterArtifactId));
        //                    if (workspaceUsers.Any())
        //                    {
        //                        await Task.Run(() => _dataHandler.InsertIntoBillingUserInfo(workspaceUsers));
        //                        _logger.LogInformation($"Inserted {workspaceUsers.Count} users for workspace {workspaceEddsArtifactId}");
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    failures.Add((workspaceEddsArtifactId, $"Failed to process users: {ex.Message}"));
        //                    _logger.LogError($"Error processing users for workspace {workspaceEddsArtifactId}: {ex.Message}", ex);
        //                    await SendErrorNotification(workspaceEddsArtifactId, "users", ex);
        //                }

        //                try
        //                {
        //                    var pageCountPerCase = _dataHandler.GetWorkspaceImageCount(startDate, endDate, workspaceEddsArtifactId);

        //                    if (billingMetrics != null)
        //                    {
        //                        // Get both primary and secondary data for the workspace
        //                        primaryDataLookup.TryGetValue(workspaceEddsArtifactId, out var primaryMetrics);
        //                        secondaryDataLookup.TryGetValue(workspaceEddsArtifactId, out var secondaryMetrics);

        //                        _logger.LogInformation($"Found primary data: {primaryMetrics != null}, secondary data: {secondaryMetrics != null} for workspace {workspaceEddsArtifactId}");

        //                        var newMetrics = CreateBillingMetrics(
        //                            workspaceEddsArtifactId,
        //                            matterArtifactId,
        //                            dateKey,
        //                            billingMetrics,
        //                            primaryMetrics,
        //                            secondaryMetrics,
        //                            pageCountPerCase);

        //                        await SaveMetricsToDatabase(new List<TempBilingDetailsWorkspace> { newMetrics });
        //                        _logger.LogInformation($"Saved metrics for workspace {workspaceEddsArtifactId}");
        //                    }
        //                    else
        //                    {
        //                        _logger.LogWarning($"No billing metrics returned for workspace {workspaceEddsArtifactId}");
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    failures.Add((workspaceEddsArtifactId, $"Failed to process metrics: {ex.Message}"));
        //                    _logger.LogError($"Error processing metrics for workspace {workspaceEddsArtifactId}: {ex.Message}", ex);
        //                    await SendErrorNotification(workspaceEddsArtifactId, "metrics", ex);
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                failures.Add((workspaceEddsArtifactId, $"Failed to process workspace: {ex.Message}"));
        //                _logger.LogError($"Error processing workspace {workspaceEddsArtifactId}: {ex.Message}", ex);
        //                await SendErrorNotification(workspaceEddsArtifactId, "workspace", ex);
        //            }
        //        }

        //        if (failures.Any())
        //        {
        //            var failureMessage = string.Join("\n", failures.Select(f => $"Workspace {f.WorkspaceId}: {f.Error}"));
        //            _logger.LogError($"Failed to process some workspaces:\n{failureMessage}");
        //        }

        //        _logger.LogInformation("Completed ProcessWorkspaceMetrics");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Critical error in ProcessWorkspaceMetrics: {ex.Message}", ex);
        //        throw;
        //    }
        //}

        private async Task ProcessWorkspaceMetrics(QueryResult billingWorkspaces, string dateKey, List<SecondaryData> data)
        {
            try
            {
                _logger.LogInformation("Starting ProcessWorkspaceMetrics");
                var (startDate, endDate) = GetMonthDateRange(DateTime.Now);
                var failures = new List<(int WorkspaceId, string Error)>();

                // Create lookup dictionary for efficient data access
                var dataLookup = data?.ToDictionary(d => d.WorkspaceArtifactId, d => d)
                    ?? new Dictionary<int, SecondaryData>();                               

                foreach (var workspace in billingWorkspaces.Objects)
                {
                    int matterArtifactId = 0;
                    int workspaceEddsArtifactId = 0;

                    try
                    {
                        (matterArtifactId, workspaceEddsArtifactId) = ExtractWorkspaceIds(workspace);
                        _logger.LogInformation($"Processing workspace {workspaceEddsArtifactId} for matter {matterArtifactId}");

                        // Process users
                        try
                        {
                            var workspaceUsers = await Task.Run(() =>
                                _dataHandler.GetUsersForCase(workspaceEddsArtifactId, matterArtifactId));
                            if (workspaceUsers.Any())
                            {
                                await Task.Run(() => _dataHandler.InsertIntoBillingUserInfo(workspaceUsers));
                                _logger.LogInformation($"Inserted {workspaceUsers.Count} users for workspace {workspaceEddsArtifactId}");
                            }
                        }
                        catch (Exception ex)
                        {
                            failures.Add((workspaceEddsArtifactId, $"Failed to process users: {ex.Message}"));
                            _logger.LogError($"Error processing users for workspace {workspaceEddsArtifactId}: {ex.Message}", ex);
                            await SendErrorNotification(workspaceEddsArtifactId, "users", ex);
                        }

                        // Process metrics
                        try
                        {
                            var pageCountPerCase = _dataHandler.GetWorkspaceImageCount(startDate, endDate, workspaceEddsArtifactId);

                            if (dataLookup.TryGetValue(workspaceEddsArtifactId, out var metrics))
                            {
                                _logger.LogInformation($"Found metrics data for workspace {workspaceEddsArtifactId}");

                                var newMetrics = CreateBillingMetrics(
                                    workspaceEddsArtifactId,
                                    matterArtifactId,
                                    dateKey,
                                    metrics,
                                    pageCountPerCase);

                                await SaveMetricsToDatabase(new List<TempBilingDetailsWorkspace> { newMetrics });
                                _logger.LogInformation($"Saved metrics for workspace {workspaceEddsArtifactId}");
                            }
                            else
                            {
                                _logger.LogWarning($"No metrics data found for workspace {workspaceEddsArtifactId}");
                            }
                        }
                        catch (Exception ex)
                        {
                            failures.Add((workspaceEddsArtifactId, $"Failed to process metrics: {ex.Message}"));
                            _logger.LogError($"Error processing metrics for workspace {workspaceEddsArtifactId}: {ex.Message}", ex);
                            await SendErrorNotification(workspaceEddsArtifactId, "metrics", ex);
                        }
                    }
                    catch (Exception ex)
                    {
                        failures.Add((workspaceEddsArtifactId, $"Failed to process workspace: {ex.Message}"));
                        _logger.LogError($"Error processing workspace {workspaceEddsArtifactId}: {ex.Message}", ex);
                        await SendErrorNotification(workspaceEddsArtifactId, "workspace", ex);
                    }
                }

                if (failures.Any())
                {
                    var failureMessage = string.Join("\n", failures.Select(f => $"Workspace {f.WorkspaceId}: {f.Error}"));
                    _logger.LogError($"Failed to process some workspaces:\n{failureMessage}");
                }

                _logger.LogInformation("Completed ProcessWorkspaceMetrics");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Critical error in ProcessWorkspaceMetrics: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        private async Task SendErrorNotification(int workspaceId, string errorType, Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Error processing {errorType}:<br>{ex.Message}<br><br>Stack Trace: {ex.StackTrace}");
            await MessageHandler.Email.SendInternalNotificationAsync(
                _instanceSettings,
                sb,
                $"Billing data failure for workspace: {workspaceId}");
        }

        private async Task ProcessSummaryMetrics(
            List<BillingSummaryMetrics> summaryMetrics, 
            List<BillingSummaryWorkspaces> summaryWorkspaces, 
            List<BillingSummaryUsers> summaryUsers, 
            List<BillingDetails> existingdetails, 
            List<BillingWorkspaces> billingWorkspaces, 
            List<BillingOverrides> billingOverrides)
        {
            var (recordsToUpdate, _) = SeparateRecords(summaryMetrics, existingdetails);
         
            var workspaceMapping = await PrepareWorkspaceMapping(summaryWorkspaces, billingWorkspaces);
            await UpdateBillingDetails(recordsToUpdate, summaryUsers, summaryWorkspaces, workspaceMapping, billingOverrides);

            //TODO: Remove
            //if (recordsToUpdate.Count > 0)
            //{
            //    var workspaceMapping = await PrepareWorkspaceMapping(summaryWorkspaces, billingWorkspaces);
            //    await UpdateBillingDetails(recordsToUpdate, summaryUsers, summaryWorkspaces, workspaceMapping, billingOverrides);
            //}
        }

        //TODO: Remove
        //private (DateTime FirstDay, DateTime LastDay) GetMonthDateRange(DateTime currentDate)
        //{
        //    var firstDay = new DateTime(currentDate.Year, currentDate.Month, 1);
        //    var lastDay = firstDay.AddMonths(1).AddDays(-1);
        //    return (firstDay, lastDay);
        //}

        private (DateTime FirstDay, DateTime LastDay) GetMonthDateRange(DateTime currentDate)
        {
            // On the first day of the month, use previous month
            if (currentDate.Day == 1)
            {
                var previousMonth = currentDate.AddMonths(-1);
                return (
                    new DateTime(previousMonth.Year, previousMonth.Month, 1),
                    new DateTime(previousMonth.Year, previousMonth.Month, DateTime.DaysInMonth(previousMonth.Year, previousMonth.Month))
                );
            }

            // For all other days, use current month
            return (
                new DateTime(currentDate.Year, currentDate.Month, 1),
                new DateTime(currentDate.Year, currentDate.Month, DateTime.DaysInMonth(currentDate.Year, currentDate.Month))
            );
        }
        private (int matterArtifactId, int workspaceEddsArtifactId) ExtractWorkspaceIds(RelativityObject workspace)
        {
            var matterValue = workspace.FieldValues
                .FirstOrDefault(f => f.Field.Name == "Workspace Matter Object")?.Value as RelativityObjectValue;
            var workspaceEddsValue = workspace.FieldValues
                .FirstOrDefault(f => f.Field.Name == "EDDS Workspace ArtifactID")?.Value;

            return (matterValue.ArtifactID, int.Parse(workspaceEddsValue.ToString()));
        }

        //OBSOLETE -- leaving for now but may not be needed at all
        //private async Task<BillingResult> GetWorkspaceBillingMetrics(int workspaceEddsArtifactId)
        //{
        //    var responseBillingReviewData = await _billingApi.GetMonthlyMetricsAsync(workspaceEddsArtifactId);
        //    var billingReviewData = JsonConvert.DeserializeObject<BillingResponse>(responseBillingReviewData);
        //    return billingReviewData?.Results?.FirstOrDefault();
        //}

        //TODO: Remove
        //private TempBilingDetailsWorkspace CreateBillingMetrics(int workspaceEddsArtifactId, int matterArtifactId, string dateKey, BillingResult result, List<ProcessingData> processingData, int pageCount)
        //{
        //    decimal publishedSize = 0;

        //    if (processingData == null)
        //    {
        //        _ltasHelper.Logger.LogError($"Processing data is null for workspace {workspaceEddsArtifactId}");
        //    }
        //    else
        //    {
        //        publishedSize = processingData.FirstOrDefault(p => p.WorkspaceArtifactId == workspaceEddsArtifactId)?.PublishedDocumentSizeGB ?? 0;
        //    }

        //    //var publishedSize = processingData
        //    //    .FirstOrDefault(p => p.WorkspaceArtifactId == workspaceEddsArtifactId)
        //    //    ?.PublishedDocumentSizeGB ?? 0;

        //    decimal processingRepository = 0;
        //    decimal processingReview = 0;

        //    if (result.Workspace.WorkspaceStatus == "ECA")
        //    {
        //        processingRepository = publishedSize;
        //    }
        //    else
        //    {
        //        processingReview = publishedSize;
        //    }

        //    return new TempBilingDetailsWorkspace
        //    {
        //        ArtifactIdWorkspaceEDDS = workspaceEddsArtifactId,
        //        ArtifactIdMatter = matterArtifactId,
        //        DateKey = dateKey,
        //        HostingReview = result.PricedMetrics.ReviewHosting?.BillableValue ?? 0,
        //        HostingRepository = result.PricedMetrics.RepositoryHosting?.BillableValue ?? 0,
        //        ProcessingRepository = processingRepository,
        //        ProcessingReview = processingReview,
        //        PageCountUnits = pageCount,
        //        ColdStorage = result.PricedMetrics.StorageCold?.BillableValue ?? 0,
        //        TranslationUnits = (int)(result.PricedMetrics.TranslateUnits?.BillableValue ?? 0),
        //        AirReviewUnits = (int)(result.PricedMetrics.AirReviewUnits?.BillableValue ?? 0),
        //        AirPriviilegeUnits = (int)(result.PricedMetrics.AirPrivilegeUnits?.BillableValue ?? 0)
        //    };
        //}

        private TempBilingDetailsWorkspace CreateBillingMetrics(
            int workspaceEddsArtifactId,
            int matterArtifactId,
            string dateKey,
            SecondaryData data,
            int pageCount)
        {
            try
            {
                _logger.LogInformation($"Creating billing metrics for workspace {workspaceEddsArtifactId}, matter {matterArtifactId}");

                if (data == null)
                {
                    _logger.LogError($"No metrics data available for workspace {workspaceEddsArtifactId}");
                    return null;
                }

                var billingDetails = new TempBilingDetailsWorkspace
                {
                    ArtifactIdWorkspaceEDDS = workspaceEddsArtifactId,
                    ArtifactIdMatter = matterArtifactId,
                    DateKey = dateKey,
                    PageCountUnits = pageCount,
                    TranslationUnits = data.TranslateDocumentUnits,
                    AirReviewUnits = data.AirForReviewDocuments,
                    AirPriviilegeUnits = data.AirForPrivilegeDocuments
                };

                _logger.LogInformation($"Processing {data.WorkspaceType} workspace with:" +
                    $"\n  Peak Workspace Hosted Size: {data.PeakWorkspaceHostedSizeGB}" +
                    $"\n  Linked Total File Size: {data.LinkedTotalFileSizeGB}" +
                    $"\n  Published Document Size: {data.PublishedDocumentSizeGB}");

                // Storage and processing calculations based on workspace type
                switch (data.WorkspaceType?.ToUpper())
                {
                    case "REPOSITORY":
                        // For ECA workspaces:
                        // - Review Hosting uses Linked Total File Size
                        // - Repository Hosting is the difference between Peak and Linked
                        billingDetails.HostingReview = data.LinkedTotalFileSizeGB;
                        billingDetails.HostingRepository = data.PeakWorkspaceHostedSizeGB - data.LinkedTotalFileSizeGB;
                        billingDetails.ProcessingRepository = data.PublishedDocumentSizeGB;
                        billingDetails.ColdStorage = 0;

                        _logger.LogInformation($"ECA workspace calculations:" +
                            $"\n  Review Hosting = Linked Total File Size: {billingDetails.HostingReview}" +
                            $"\n  Repository Hosting = Peak - Linked: {data.PeakWorkspaceHostedSizeGB} - {data.LinkedTotalFileSizeGB} = {billingDetails.HostingRepository}" +
                            $"\n  Processing Repository = Published Size: {billingDetails.ProcessingRepository}");
                        break;

                    case "COLD STORAGE":
                        // For Cold Storage workspaces:
                        // - Only Cold Storage is set, using Peak Workspace Hosted Size
                        billingDetails.HostingReview = 0;
                        billingDetails.HostingRepository = 0;
                        billingDetails.ProcessingReview = 0;
                        billingDetails.ProcessingRepository = 0;
                        billingDetails.ColdStorage = data.PeakWorkspaceHostedSizeGB;

                        _logger.LogInformation($"Cold Storage workspace calculations:" +
                            $"\n  Cold Storage = Peak Workspace Hosted Size: {billingDetails.ColdStorage}");
                        break;

                    case "REVIEW":
                    default:
                        // For Review workspaces:
                        // - Review Hosting uses Peak Workspace Hosted Size
                        // - No Repository Hosting or Cold Storage
                        billingDetails.HostingReview = data.PeakWorkspaceHostedSizeGB;
                        billingDetails.HostingRepository = 0;
                        billingDetails.ProcessingReview = data.PublishedDocumentSizeGB;
                        billingDetails.ColdStorage = 0;

                        _logger.LogInformation($"Review workspace calculations:" +
                            $"\n  Review Hosting = Peak Workspace Hosted Size: {billingDetails.HostingReview}" +
                            $"\n  Processing Review = Published Size: {billingDetails.ProcessingReview}");
                        break;
                }

                _logger.LogInformation($"Final metrics for workspace {workspaceEddsArtifactId}:" +
                    $"\nWorkspace Type: {data.WorkspaceType}" +
                    $"\nHosting Review: {billingDetails.HostingReview:N2} GB" +
                    $"\nHosting Repository: {billingDetails.HostingRepository:N2} GB" +
                    $"\nProcessing Repository: {billingDetails.ProcessingRepository:N2} GB" +
                    $"\nProcessing Review: {billingDetails.ProcessingReview:N2} GB" +
                    $"\nCold Storage: {billingDetails.ColdStorage:N2} GB" +
                    $"\nAIR Review Units: {billingDetails.AirReviewUnits}" +
                    $"\nAIR Privilege Units: {billingDetails.AirPriviilegeUnits}" +
                    $"\nTranslation Units: {billingDetails.TranslationUnits}" +
                    $"\nPage Count Units: {billingDetails.PageCountUnits}");

                return billingDetails;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating billing metrics for workspace {workspaceEddsArtifactId}: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        private async Task SaveMetricsToDatabase(List<TempBilingDetailsWorkspace> insert)
        {
            if (insert.Any())
            {
                var insertValues = string.Join(",", insert.Select(b =>
                    $"({b.ArtifactIdWorkspaceEDDS}, {b.ArtifactIdMatter}, '{b.DateKey}', " +
                    $"{b.HostingReview}, {b.HostingRepository}, {b.ProcessingReview},{b.ProcessingRepository}, {b.TranslationUnits}, " +
                    $"{b.AirPriviilegeUnits}, {b.AirReviewUnits}, {b.PageCountUnits}, {b.ColdStorage})"));

                string insertSql = $@"INSERT INTO eddsdbo.BillingDetailsTempWorkspace
                        (WID, MID, DateKey, RVWH, RPYH, RVWP, RPYP, TU, APU, ARU, PU, CS)
                        VALUES {insertValues}";

                await _dataHandler.InsertIntoBillingDetailsWorkspace(insertSql);
            }
        }

        private (List<(BillingSummaryMetrics Metric, int BillingDetailsArtifactId)> ToUpdate, List<BillingSummaryMetrics> ToInsert) SeparateRecords(List<BillingSummaryMetrics> metrics, List<BillingDetails> existing)
        {
            var toUpdate = new List<(BillingSummaryMetrics Metric, int BillingDetailsArtifactId)>();
            var toInsert = new List<BillingSummaryMetrics>();

            foreach (var metric in metrics)
            {
                var existingRecord = existing.FirstOrDefault(d =>
                    d.MatterArtifactId == metric.MatterArtifactId &&
                    d.DateKey == metric.DateKey);

                if (existingRecord != null)
                {
                    toUpdate.Add((metric, existingRecord.BillingDetailsArtifactId));
                }
                else
                {
                    toInsert.Add(metric);
                }
            }

            return (toUpdate, toInsert);
        }

        private async Task<Dictionary<int, int>> PrepareWorkspaceMapping(List<BillingSummaryWorkspaces> workspaces, List<BillingWorkspaces> billingWorkspaces)
        {
            var workspaceArtifactIdMap = new Dictionary<int, int>();
            var sb = new StringBuilder();
            var hasDuplicates = false;

            foreach (var workspace in workspaces)
            {
                var artifactId = _ltasHelper.GetWorkspaceArtifactIdFromEddsArtifactId(workspace.WorkspaceEddsArtifactId, billingWorkspaces);
                if (!workspaceArtifactIdMap.ContainsKey(workspace.WorkspaceEddsArtifactId))
                {
                    workspaceArtifactIdMap.Add(workspace.WorkspaceEddsArtifactId, artifactId);
                }
                else
                {
                    hasDuplicates = true;
                    string duplicateMessage = $"Duplicate workspace EDDS ID found: {workspace.WorkspaceEddsArtifactId} for Matter {workspace.MatterArtifactId}";
                    _ltasHelper.Logger.LogError(duplicateMessage);

                    sb.AppendLine(duplicateMessage);
                    sb.AppendLine("<br>");
                    sb.AppendLine($"Existing mapping: EDDS ID {workspace.WorkspaceEddsArtifactId} -> Artifact ID {workspaceArtifactIdMap[workspace.WorkspaceEddsArtifactId]}");
                    sb.AppendLine("<br>");
                    sb.AppendLine($"Attempted new mapping: EDDS ID {workspace.WorkspaceEddsArtifactId} -> Artifact ID {artifactId}");
                    sb.AppendLine("<br>");
                }
            }

            if (hasDuplicates)
            {
                await MessageHandler.Email.SendInternalNotificationAsync(
                    _instanceSettings,
                    sb,
                    "Duplicate Workspaces Found in Billing Processing"
                );
            }

            return workspaceArtifactIdMap;
        }

        private async Task CreateBillingDetails(List<BillingSummaryMetrics> recordsToInsert)
        {
            _logger.LogInformation($"Billing Details Records: {recordsToInsert?.Count ?? 0} to be created");
            int successfulCreations = 0;
            int failedCreations = 0;

            foreach (var metric in recordsToInsert)
            {
                var createRequest = new CreateRequest
                {
                    ObjectType = new ObjectTypeRef { Guid = _ltasHelper.DetailsObjectType },
                    FieldValues = new List<FieldRefValuePair>
                    {
                        new FieldRefValuePair
                        {
                            Field = new FieldRef
                            {
                                Guid = _ltasHelper.DetailsYearMonth
                            },
                            Value = metric.DateKey
                        }
                    },
                    ParentObject = new RelativityObjectRef { ArtifactID = metric.MatterArtifactId }
                };

                var result = ObjectHandler.CreateBillingDetails(_objectManager, _billingManagementDatabase, createRequest, _ltasHelper.Logger);
                if (result == null)
                {
                    failedCreations++;
                    _ltasHelper.Logger.LogError($"Failed to create billing details for matter {metric.MatterArtifactId} with date key {metric.DateKey}");
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine($"Failed to create billing details record for matter: {metric.MatterArtifactId}");
                    sb.AppendLine("<br>");
                    sb.AppendLine($"Date Key: {metric.DateKey}");
                    await MessageHandler.Email.SendInternalNotificationAsync(
                        _instanceSettings,
                        sb,
                        $"Billing Details Creation Failure for Matter: {metric.MatterArtifactId}"
                    );
                }
                else
                {
                    successfulCreations++;
                }
            }

            _logger.LogInformation($"Billing Details Records creation complete. Successfully created: {successfulCreations}, Failed: {failedCreations} out of {recordsToInsert?.Count ?? 0} total records");
        }

        //TODO: Remove
        //private async Task UpdateBillingDetails(List<(BillingSummaryMetrics Metric, int BillingDetailsArtifactId)> recordsToUpdate, List<BillingSummaryUsers> users, List<BillingSummaryWorkspaces> workspaces, Dictionary<int, int> workspaceLookup, List<BillingOverrides> overrides)
        //{
        //    _logger.LogInformation($"Billing Details Records: {recordsToUpdate?.Count ?? 0} to be upated");
        //    int updatedCount = 0;

        //    foreach (var metric in recordsToUpdate)
        //    {
        //        UpdateResult result = null;
        //        List<FieldRefValuePair> fields = new List<FieldRefValuePair>();
        //        UpdateRequest updateRequest = new UpdateRequest();

        //        var relatedWorkspaces = workspaces
        //            .Where(w => w.MatterArtifactId == metric.Metric.MatterArtifactId && w.DateKey == metric.Metric.DateKey)
        //            .Select(w => {
        //                if (!workspaceLookup.ContainsKey(w.WorkspaceEddsArtifactId))
        //                {
        //                    _ltasHelper.Logger.LogError($"Unexpected: No mapping found for workspace {w.WorkspaceEddsArtifactId}");
        //                }
        //                return new RelativityObjectRef { ArtifactID = workspaceLookup[w.WorkspaceEddsArtifactId] };
        //            })
        //            .ToList();

        //        var relatedUsers = users
        //            .Where(u => u.MatterArtifactId == metric.Metric.MatterArtifactId && u.DateKey == metric.Metric.DateKey)
        //            .Select(u => new RelativityObjectRef { ArtifactID = u.UserArtifactId })
        //            .ToList();

        //        var matteroverrides = overrides.FirstOrDefault(o => o.MatterArtifactsId == metric.Metric.MatterArtifactId);

        //        Guid reviewHostingGuid = matteroverrides != null ?
        //            _ltasHelper.DetermineHostingReviewGuid(metric.Metric.SumHostingReview, matteroverrides) :
        //            _ltasHelper.DetermineHostingReviewGuid(metric.Metric.SumHostingReview, null);


        //        Guid repositoryHostingGuid = matteroverrides != null ?
        //            _ltasHelper.DetermineHostingRepositoryGuid(metric.Metric.SumHostingRepository, matteroverrides) :
        //            _ltasHelper.DetermineHostingRepositoryGuid(metric.Metric.SumHostingRepository, null);

        //        Guid reviewProcessingGuid = matteroverrides != null ?
        //            _ltasHelper.DetermineProcessingReview(metric.Metric.SumProcessingReview, matteroverrides) :
        //            _ltasHelper.DetermineProcessingReview(metric.Metric.SumProcessingReview, null);

        //        Guid repositoryProcessingGuid = matteroverrides != null ?
        //            _ltasHelper.DetermineProcesingRepository(metric.Metric.SumProcessingRepository, matteroverrides) :
        //            _ltasHelper.DetermineProcesingRepository(metric.Metric.SumProcessingRepository, null);

        //        if (metric.Metric.SumHostingReview > 0)
        //        {
        //            fields.Add(new FieldRefValuePair
        //            {
        //                Field = new FieldRef
        //                {
        //                    Guid = reviewHostingGuid
        //                },
        //                Value = metric.Metric.SumHostingReview
        //            });
        //        }

        //        if (metric.Metric.SumHostingRepository > 0)
        //        {
        //            fields.Add(new FieldRefValuePair
        //            {   //Repository Hosting
        //                Field = new FieldRef
        //                {
        //                    Guid = repositoryHostingGuid
        //                },
        //                Value = metric.Metric.SumHostingRepository
        //            });
        //        }

        //        if (metric.Metric.SumProcessingReview > 0)
        //        {
        //            fields.Add(new FieldRefValuePair
        //            {
        //                //Review Processing
        //                Field = new FieldRef
        //                {
        //                    Guid = reviewProcessingGuid
        //                },
        //                Value = metric.Metric.SumProcessingReview
        //            });
        //        }

        //        if (metric.Metric.SumProcessingRepository > 0)
        //        {
        //            fields.Add(new FieldRefValuePair
        //            {
        //                //Repository Processing
        //                Field = new FieldRef
        //                {
        //                    Guid = repositoryProcessingGuid
        //                },
        //                Value = metric.Metric.SumProcessingRepository
        //            });
        //        }

        //        if (metric.Metric.SumColdStorage > 0)
        //        {
        //            fields.Add(new FieldRefValuePair
        //            {
        //                //ColdStorage
        //                Field = new FieldRef
        //                {
        //                    Guid = _ltasHelper.DetailsCS3205
        //                },
        //                Value = metric.Metric.SumColdStorage
        //            });

        //            fields.Add(new FieldRefValuePair
        //            {
        //                //ColdStorage Override
        //                Field = new FieldRef
        //                {
        //                    Guid = _ltasHelper.DetailsCS3205_Override
        //                },
        //                Value = matteroverrides?.CS_O.HasValue == true ? 1 : 0
        //            });
        //        }

        //        if (metric.Metric.SumTranslationUnits > 0)
        //        {
        //            fields.Add(new FieldRefValuePair
        //            {
        //                //Translations
        //                Field = new FieldRef
        //                {
        //                    Guid = _ltasHelper.DetailsTU3270
        //                },
        //                Value = metric.Metric.SumTranslationUnits
        //            });

        //            fields.Add(new FieldRefValuePair
        //            {
        //                //Translations Override
        //                Field = new FieldRef
        //                {
        //                    Guid = _ltasHelper.DetailsTU3270_Override
        //                },
        //                Value = matteroverrides?.TU_O.HasValue == true ? 1 : 0
        //            });
        //        }

        //        if (metric.Metric.SumPageCountUnits > 0)
        //        {
        //            fields.Add(new FieldRefValuePair
        //            {
        //                //Page Count
        //                Field = new FieldRef
        //                {
        //                    Guid = _ltasHelper.DetailsPU3203
        //                },
        //                Value = metric.Metric.SumPageCountUnits
        //            });

        //            fields.Add(new FieldRefValuePair
        //            {
        //                //Page Count Override
        //                Field = new FieldRef
        //                {
        //                    Guid = _ltasHelper.DetailsPU3203_Override
        //                },
        //                Value = matteroverrides?.PU_O.HasValue == true ? 1 : 0
        //            });
        //        }

        //        if (metric.Metric.SumAirReviewUnits > 0)
        //        {
        //            fields.Add(new FieldRefValuePair
        //            {
        //                //Air for Review
        //                Field = new FieldRef
        //                {
        //                    Guid = _ltasHelper.DetailsARU3201
        //                },
        //                Value = metric.Metric.SumAirReviewUnits
        //            });

        //            fields.Add(new FieldRefValuePair
        //            {
        //                //Translations
        //                Field = new FieldRef
        //                {
        //                    Guid = _ltasHelper.DetailsARU3201_Override
        //                },
        //                Value = matteroverrides?.ARU_O.HasValue == true ? 1 : 0
        //            });
        //        }

        //        if (metric.Metric.SumAirPriviilegeUnits > 0)
        //        {
        //            fields.Add(new FieldRefValuePair
        //            {
        //                //Air for Privilege
        //                Field = new FieldRef
        //                {
        //                    Guid = _ltasHelper.DetailsAPU3202
        //                },
        //                Value = metric.Metric.SumAirPriviilegeUnits
        //            });

        //            fields.Add(new FieldRefValuePair
        //            {
        //                //Translations
        //                Field = new FieldRef
        //                {
        //                    Guid = _ltasHelper.DetailsAPU3202_Override
        //                },
        //                Value = matteroverrides?.APU_O.HasValue == true ? 1 : 0
        //            });
        //        }

        //        if (relatedUsers.Any() || relatedWorkspaces.Any())
        //        {
        //            if (relatedUsers.Any())
        //            {
        //                fields.Add(new FieldRefValuePair
        //                {
        //                    //User Count
        //                    Field = new FieldRef
        //                    {
        //                        Guid = _ltasHelper.DetailsUU3200
        //                    },
        //                    Value = relatedUsers.Count
        //                });

        //                fields.Add(new FieldRefValuePair
        //                {
        //                    //User Count Override
        //                    Field = new FieldRef
        //                    {
        //                        Guid = _ltasHelper.DetailsUU3200_Override
        //                    },
        //                    Value = matteroverrides?.U_O.HasValue == true ? 1 : 0
        //                });

        //                fields.Add(new FieldRefValuePair
        //                {
        //                    //list of users
        //                    Field = new FieldRef
        //                    {
        //                        Guid = _ltasHelper.DetailsUsers
        //                    },
        //                    Value = relatedUsers
        //                });
        //            }

        //            if (relatedWorkspaces.Any())
        //            {
        //                fields.Add(new FieldRefValuePair
        //                {
        //                    //Workspace Count
        //                    Field = new FieldRef
        //                    {
        //                        Guid = _ltasHelper.DetailsWorkspaceCount
        //                    },
        //                    Value = relatedWorkspaces.Count
        //                });

        //                fields.Add(new FieldRefValuePair
        //                {
        //                    //Workspace Count
        //                    Field = new FieldRef
        //                    {
        //                        Guid = _ltasHelper.DetailsWorkspaces
        //                    },
        //                    Value = relatedWorkspaces
        //                });
        //            }

        //            updateRequest.Object = new RelativityObjectRef { ArtifactID = metric.BillingDetailsArtifactId };
        //            updateRequest.FieldValues = fields;

        //            result = await TryUpdateWithRetry(() =>
        //                ObjectHandler.UpdateBillingDetailsWUpdateOptions(_objectManager, _billingManagementDatabase, updateRequest, _ltasHelper.Logger));

        //        }
        //        else
        //        {
        //            updateRequest.Object = new RelativityObjectRef { ArtifactID = metric.BillingDetailsArtifactId };
        //            updateRequest.FieldValues = fields;

        //            result = await TryUpdateWithRetry(() =>
        //                ObjectHandler.UpdateBillingDetails(_objectManager, _billingManagementDatabase, updateRequest, _ltasHelper.Logger));
        //        }


        //        if (result == null)
        //        {
        //            _ltasHelper.Logger.LogError($"Failed to update billing details for matter {metric.Metric.MatterArtifactId} with date key {metric.Metric.DateKey}");

        //            StringBuilder sb = new StringBuilder();
        //            sb.AppendLine($"Failed to update billing details record for matter: {metric.Metric.MatterArtifactId}, billing details artifactid: {metric.BillingDetailsArtifactId}");
        //            sb.AppendLine("<br>");
        //            sb.AppendLine($"Date Key: {metric.Metric.DateKey}");

        //            await MessageHandler.Email.SendInternalNotificationAsync(
        //                _instanceSettings,
        //                sb,
        //                $"Billing Details Update Failure for Matter: {metric.Metric.MatterArtifactId} , billing details artifactid:  {metric.BillingDetailsArtifactId}"
        //            );
        //        }
        //        else
        //        {
        //            updatedCount++;
        //        }
        //    }            
        //    _logger.LogInformation($"Billing Details update complete - {updatedCount} of {recordsToUpdate?.Count ?? 0} records updated successfully");
        //}


        //TODO: Remove
        //private async Task<UpdateResult> TryUpdateWithRetry(Func<Task<UpdateResult>> updateAction, int maxRetries = 3)
        //{
        //    for (int i = 0; i < maxRetries; i++)
        //    {
        //        try
        //        {
        //            if (i > 0) // Skip delay on first attempt
        //            {
        //                await Task.Delay(i * 5000); // 5 second delay multiplied by retry count
        //            }

        //            return await updateAction();
        //        }
        //        catch (Exception ex)
        //        {
        //            if (i < maxRetries - 1)
        //            {
        //                _ltasHelper.Logger.LogWarning($"Update attempt {i + 1} failed, retrying after delay... Error: {ex.Message}");
        //                continue;
        //            }
        //            throw;
        //        }
        //    }
        //    return null;
        //}

        private async Task UpdateBillingDetails(List<(BillingSummaryMetrics Metric, int BillingDetailsArtifactId)> recordsToUpdate,
            List<BillingSummaryUsers> users, List<BillingSummaryWorkspaces> workspaces, Dictionary<int, int> workspaceLookup,
            List<BillingOverrides> overrides)
        {
            _logger.LogInformation($"Billing Details Records: {recordsToUpdate?.Count ?? 0} to be updated");
            int updatedCount = 0;

            foreach (var metric in recordsToUpdate)
            {
                UpdateResult result = null;
                StringBuilder errorDetails = new StringBuilder();
                List<FieldRefValuePair> fields = new List<FieldRefValuePair>();
                UpdateRequest updateRequest = new UpdateRequest();

                var relatedWorkspaces = workspaces
                    .Where(w => w.MatterArtifactId == metric.Metric.MatterArtifactId && w.DateKey == metric.Metric.DateKey)
                    .Select(w => {
                        if (!workspaceLookup.ContainsKey(w.WorkspaceEddsArtifactId))
                        {
                            _ltasHelper.Logger.LogError($"Unexpected: No mapping found for workspace {w.WorkspaceEddsArtifactId}");
                        }
                        return new RelativityObjectRef { ArtifactID = workspaceLookup[w.WorkspaceEddsArtifactId] };
                    })
                    .ToList();

                var relatedUsers = users
                    .Where(u => u.MatterArtifactId == metric.Metric.MatterArtifactId && u.DateKey == metric.Metric.DateKey)
                    .Select(u => new RelativityObjectRef { ArtifactID = u.UserArtifactId })
                    .ToList();

                var matteroverrides = overrides.FirstOrDefault(o => o.MatterArtifactsId == metric.Metric.MatterArtifactId);

                Guid reviewHostingGuid = matteroverrides != null ?
                    _ltasHelper.DetermineHostingReviewGuid(metric.Metric.SumHostingReview, matteroverrides) :
                    _ltasHelper.DetermineHostingReviewGuid(metric.Metric.SumHostingReview, null);

                Guid repositoryHostingGuid = matteroverrides != null ?
                    _ltasHelper.DetermineHostingRepositoryGuid(metric.Metric.SumHostingRepository, matteroverrides) :
                    _ltasHelper.DetermineHostingRepositoryGuid(metric.Metric.SumHostingRepository, null);

                Guid reviewProcessingGuid = matteroverrides != null ?
                    _ltasHelper.DetermineProcessingReview(metric.Metric.SumProcessingReview, matteroverrides) :
                    _ltasHelper.DetermineProcessingReview(metric.Metric.SumProcessingReview, null);

                Guid repositoryProcessingGuid = matteroverrides != null ?
                    _ltasHelper.DetermineProcesingRepository(metric.Metric.SumProcessingRepository, matteroverrides) :
                    _ltasHelper.DetermineProcesingRepository(metric.Metric.SumProcessingRepository, null);

                // Add Review Hosting field if applicable
                if (metric.Metric.SumHostingReview > 0)
                {
                    fields.Add(new FieldRefValuePair
                    {
                        Field = new FieldRef { Guid = reviewHostingGuid },
                        Value = metric.Metric.SumHostingReview
                    });
                }

                // Add Repository Hosting field if applicable
                if (metric.Metric.SumHostingRepository > 0)
                {
                    fields.Add(new FieldRefValuePair
                    {
                        Field = new FieldRef { Guid = repositoryHostingGuid },
                        Value = metric.Metric.SumHostingRepository
                    });
                }

                // Add Review Processing field if applicable
                if (metric.Metric.SumProcessingReview > 0)
                {
                    fields.Add(new FieldRefValuePair
                    {
                        Field = new FieldRef { Guid = reviewProcessingGuid },
                        Value = metric.Metric.SumProcessingReview
                    });
                }

                // Add Repository Processing field if applicable
                if (metric.Metric.SumProcessingRepository > 0)
                {
                    fields.Add(new FieldRefValuePair
                    {
                        Field = new FieldRef { Guid = repositoryProcessingGuid },
                        Value = metric.Metric.SumProcessingRepository
                    });
                }

                // Add Cold Storage fields if applicable
                if (metric.Metric.SumColdStorage > 0)
                {
                    fields.Add(new FieldRefValuePair
                    {
                        Field = new FieldRef { Guid = _ltasHelper.DetailsCS3205 },
                        Value = metric.Metric.SumColdStorage
                    });
                    fields.Add(new FieldRefValuePair
                    {
                        Field = new FieldRef { Guid = _ltasHelper.DetailsCS3205Override },
                        Value = matteroverrides?.CS_O.HasValue == true ? 1 : 0
                    });
                }

                // Add Translation fields if applicable
                if (metric.Metric.SumTranslationUnits > 0)
                {
                    fields.Add(new FieldRefValuePair
                    {
                        Field = new FieldRef { Guid = _ltasHelper.DetailsTU3270 },
                        Value = metric.Metric.SumTranslationUnits
                    });
                    fields.Add(new FieldRefValuePair
                    {
                        Field = new FieldRef { Guid = _ltasHelper.DetailsTU3270Override },
                        Value = matteroverrides?.TU_O.HasValue == true ? 1 : 0
                    });
                }

                // Add Page Count fields if applicable
                if (metric.Metric.SumPageCountUnits > 0)
                {
                    fields.Add(new FieldRefValuePair
                    {
                        Field = new FieldRef { Guid = _ltasHelper.DetailsPU3203 },
                        Value = metric.Metric.SumPageCountUnits
                    });
                    fields.Add(new FieldRefValuePair
                    {
                        Field = new FieldRef { Guid = _ltasHelper.DetailsPU3203Override },
                        Value = matteroverrides?.PU_O.HasValue == true ? 1 : 0
                    });
                }

                // Add AIR Review fields if applicable
                if (metric.Metric.SumAirReviewUnits > 0)
                {
                    fields.Add(new FieldRefValuePair
                    {
                        Field = new FieldRef { Guid = _ltasHelper.DetailsARU3201 },
                        Value = metric.Metric.SumAirReviewUnits
                    });
                    fields.Add(new FieldRefValuePair
                    {
                        Field = new FieldRef { Guid = _ltasHelper.DetailsARU3201Override },
                        Value = matteroverrides?.ARU_O.HasValue == true ? 1 : 0
                    });
                }

                // Add AIR Privilege fields if applicable
                if (metric.Metric.SumAirPriviilegeUnits > 0)
                {
                    fields.Add(new FieldRefValuePair
                    {
                        Field = new FieldRef { Guid = _ltasHelper.DetailsAPU3202 },
                        Value = metric.Metric.SumAirPriviilegeUnits
                    });
                    fields.Add(new FieldRefValuePair
                    {
                        Field = new FieldRef { Guid = _ltasHelper.DetailsAPU3202Override },
                        Value = matteroverrides?.APU_O.HasValue == true ? 1 : 0
                    });
                }

                // Add User and Workspace related fields
                if (relatedUsers.Any() || relatedWorkspaces.Any())
                {
                    if (relatedUsers.Any())
                    {
                        fields.Add(new FieldRefValuePair
                        {
                            Field = new FieldRef { Guid = _ltasHelper.DetailsUU3200 },
                            Value = relatedUsers.Count
                        });
                        fields.Add(new FieldRefValuePair
                        {
                            Field = new FieldRef { Guid = _ltasHelper.DetailsUU3200Override },
                            Value = matteroverrides?.U_O.HasValue == true ? 1 : 0
                        });
                        fields.Add(new FieldRefValuePair
                        {
                            Field = new FieldRef { Guid = _ltasHelper.DetailsUsers },
                            Value = relatedUsers
                        });
                    }

                    if (relatedWorkspaces.Any())
                    {
                        fields.Add(new FieldRefValuePair
                        {
                            Field = new FieldRef { Guid = _ltasHelper.DetailsWorkspaceCount },
                            Value = relatedWorkspaces.Count
                        });
                        fields.Add(new FieldRefValuePair
                        {
                            Field = new FieldRef { Guid = _ltasHelper.DetailsWorkspaces },
                            Value = relatedWorkspaces
                        });
                    }

                    updateRequest.Object = new RelativityObjectRef { ArtifactID = metric.BillingDetailsArtifactId };
                    updateRequest.FieldValues = fields;
                    var (resultWithUpdate, updateError) = await TryUpdateWithRetry(() =>
                        ObjectHandler.UpdateBillingDetailsWUpdateOptions(_objectManager, _billingManagementDatabase, updateRequest, _ltasHelper.Logger));
                    result = resultWithUpdate;
                    if (!string.IsNullOrEmpty(updateError))
                        errorDetails.AppendLine(updateError);
                }
                else
                {
                    updateRequest.Object = new RelativityObjectRef { ArtifactID = metric.BillingDetailsArtifactId };
                    updateRequest.FieldValues = fields;
                    var (resultNoUpdate, noUpdateError) = await TryUpdateWithRetry(() =>
                        ObjectHandler.UpdateBillingDetails(_objectManager, _billingManagementDatabase, updateRequest, _ltasHelper.Logger));
                    result = resultNoUpdate;
                    if (!string.IsNullOrEmpty(noUpdateError))
                        errorDetails.AppendLine(noUpdateError);
                }

                if (result == null)
                {
                    _ltasHelper.Logger.LogError($"Failed to update billing details for matter {metric.Metric.MatterArtifactId} with date key {metric.Metric.DateKey}");
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine($"Failed to update billing details record for matter: {metric.Metric.MatterArtifactId}, billing details artifactid: {metric.BillingDetailsArtifactId}");
                    sb.AppendLine("<br>");
                    sb.AppendLine($"Date Key: {metric.Metric.DateKey}");

                    if (errorDetails.Length > 0)
                    {
                        sb.AppendLine("<br>");
                        sb.AppendLine("Error Details:");
                        sb.AppendLine(errorDetails.ToString());
                    }

                    await MessageHandler.Email.SendInternalNotificationAsync(
                        _instanceSettings,
                        sb,
                        $"Billing Details Update Failure for Matter: {metric.Metric.MatterArtifactId}, Billing Details ArtifactId: {metric.BillingDetailsArtifactId}"
                    );
                }
                else
                {
                    updatedCount++;
                }
            }

            _logger.LogInformation($"Billing Details update complete - {updatedCount} of {recordsToUpdate?.Count ?? 0} records updated successfully");
        }


        private async Task<(UpdateResult Result, string ErrorDetails)> TryUpdateWithRetry(Func<Task<UpdateResult>> updateAction, int maxRetries = 3)
        {
            StringBuilder errorDetails = new StringBuilder();

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    if (i > 0)
                    {
                        await Task.Delay(i * 5000);
                    }
                    return (await updateAction(), null);
                }
                catch (Exception ex)
                {
                    string error = $"Attempt {i + 1} failed: {ex.Message}";
                    if (ex.InnerException != null)
                        error += $"\nInner error: {ex.InnerException.Message}";

                    errorDetails.AppendLine(error);

                    if (i < maxRetries - 1)
                    {
                        _ltasHelper.Logger.LogWarning($"Update attempt {i + 1} failed, retrying after delay... Error: {ex.Message}");
                        continue;
                    }
                    return (null, errorDetails.ToString());
                }
            }
            return (null, "Max retries exceeded with no specific error details");
        }

        private async Task UpdateActiveBilling(List<BillingSummaryMetrics> summaryMetrics, List<SecondaryData> secondaryData)
        {
            try
            {
                _logger.LogInformation("Starting UpdateActiveBilling");
                var currentDateKey = DateTime.Now.ToString("yyyyMM");
                var dBContext = _ltasHelper.Helper.GetDBContext(_billingManagementDatabase);

                // Get all matters and their current status
                var matterActiveStatus = await ObjectHandler.MatterBillingStatus(
                    _objectManager,
                    _billingManagementDatabase,
                    _ltasHelper.Logger,
                    _ltasHelper.Helper);

                if (matterActiveStatus?.Objects == null)
                {
                    _logger.LogError("No matter status records found");
                    return;
                }                

                // Create lookup for quick workspace data access
                var secondaryDataByWorkspace = secondaryData?
                    .ToDictionary(d => d.WorkspaceArtifactId, d => d)                
                    ?? new Dictionary<int, SecondaryData>();

                var dbContext = _helper.GetDBContext(_billingManagementDatabase);

                // Filter to only check currently dormant matters
                var dormantMatters = matterActiveStatus.Objects
                    .Where(m => GetMatterStatus(m, dbContext) == "Dormant")
                    .ToList();

                _logger.LogInformation($"Found {dormantMatters.Count} dormant matters to check for activity");

                // Identify which dormant matters need to be activated
                var mattersToActivate = new List<RelativityObjectRef>();
                foreach (var matter in dormantMatters)
                {
                    var matterArtifactId = (int)matter.FieldValues
                        .FirstOrDefault(f => f.Field.Name == "ArtifactID")?.Value;

                    // Check if matter has any activity in current period
                    var hasActivity = summaryMetrics
                        .Where(m => m.DateKey == currentDateKey && m.MatterArtifactId == matterArtifactId)
                        .Any(m => HasAnyActivity(m, secondaryDataByWorkspace.TryGetValue(m.MatterArtifactId, out var data) ? data : null));

                    if (hasActivity)
                    {
                        mattersToActivate.Add(new RelativityObjectRef { ArtifactID = matterArtifactId });
                        //DEBUG ONLY
                        //_logger.LogInformation($"Matter {matterArtifactId} will be activated based on current activity");
                    }
                }

                // Perform mass update for matters requiring activation
                if (mattersToActivate.Any())
                {
                    var activeStatusId = _ltasHelper.GetBillingActiveStatusArtifactID(dBContext, "Active");
                    var fieldValues = new List<FieldRefValuePair>
                    {
                        new FieldRefValuePair
                        {
                            Field = new FieldRef { Name = "Active Billing" },
                            Value = new ChoiceRef { ArtifactID = activeStatusId }
                        }
                    };

                    var result = await MassUpdate(
                        _ltasHelper.Helper,
                        _billingManagementDatabase,
                        mattersToActivate,
                        fieldValues,
                        FieldUpdateBehavior.Replace);

                    _logger.LogInformation($"Mass update completed - {(result?.Success == true ? mattersToActivate.Count : 0)} of {mattersToActivate.Count} matters activated");
                }
                else
                {
                    _logger.LogInformation("No dormant matters require activation");
                }

                _logger.LogInformation($"UpdateActiveBilling complete - Checked {dormantMatters.Count} dormant matters, " +
                    $"Activated {mattersToActivate.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdateActiveBilling: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        private string GetMatterStatus(RelativityObject matter, IDBContext dbContext)
        {
            var statusField = matter.FieldValues
                .FirstOrDefault(f => f.Field.Name == "Active Billing")?.Value as ChoiceRef;

            if (statusField?.ArtifactID == _ltasHelper.GetBillingActiveStatusArtifactID(dbContext, "Active"))
                return "Active";
            else
                return "Dormant";
        }

        private async Task<MassUpdateResult> MassUpdate(
            IHelper helper,
            int workspaceID,
            IReadOnlyList<RelativityObjectRef> relativityObjectRefs,
            IEnumerable<FieldRefValuePair> fieldRefValuePairs,
            FieldUpdateBehavior behavior)
        {
            using (IObjectManager objectManager = helper.GetServicesManager().CreateProxy<IObjectManager>(ExecutionIdentity.CurrentUser))
            {
                try
                {
                    var updateRequest = new MassUpdateByObjectIdentifiersRequest
                    {
                        Objects = relativityObjectRefs,
                        FieldValues = fieldRefValuePairs
                    };

                    var updateOptions = new MassUpdateOptions
                    {
                        UpdateBehavior = behavior
                    };

                    return await objectManager.UpdateAsync(workspaceID, updateRequest, updateOptions);
                }
                catch (ValidationException exception)
                {
                    _logger.LogError($"Mass update validation error: {exception.Message}");
                    if (exception.InnerException != null)
                    {
                        _logger.LogError($"Inner exception: {exception.InnerException.Message}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Mass update error: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                    }
                }
            }

            return null;
        }


        private bool HasAnyActivity(BillingSummaryMetrics metrics, SecondaryData secondaryData)
        {
            if (metrics == null) return false;

            bool summaryActivity = metrics.SumHostingReview > 0
                || metrics.SumHostingRepository > 0
                || metrics.SumProcessingReview > 0
                || metrics.SumProcessingRepository > 0
                || metrics.SumColdStorage > 0
                || metrics.SumTranslationUnits > 0
                || metrics.SumPageCountUnits > 0
                || metrics.SumAirReviewUnits > 0
                || metrics.SumAirPriviilegeUnits > 0;

            if (summaryActivity) return true;

            if (secondaryData != null)
            {
                bool secondaryActivity = secondaryData.PublishedDocumentSizeGB > 0
                    || secondaryData.LinkedTotalFileSizeGB > 0
                    || secondaryData.PeakWorkspaceHostedSizeGB > 0
                    || secondaryData.TranslateDocumentUnits > 0;

                return secondaryActivity;
            }

            return false;
        }
       
    }
}