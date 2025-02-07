using kCura.Agent;
using System;
using System.Runtime.InteropServices;
using Relativity.API;
using LTASBM.Agent.Handlers;
using Relativity.Services.Objects;
using System.Threading.Tasks;
using LTASBM.Agent.Managers;
using System.Data.SqlClient;
using LTASBM.Agent.Logging;

namespace LTASBM.Agent
{
    [kCura.Agent.CustomAttributes.Name("LTAS Billing Management")]
    [Guid("fe5d6dca-598c-483f-942b-64af159f0982")]

    public class LTASBillingWorker : AgentBase
    {
        private ILTASLogger _logger;
        public override string Name => "LTAS Billing Management Worker";        
        public override void Execute()
        {
            IAPILog relativityLogger = Helper.GetLoggerFactory().GetLogger().ForContext<LTASBillingWorker>();
            _logger = LoggerFactory.CreateLogger<LTASBillingWorker>(Helper.GetDBContext(-1), Helper, relativityLogger);

            _logger.LogInformation("Starting LTAS Billing Management Agent");
            RaiseMessage("Starting LTAS Billing Management...", 10);

            try
            {
                var eddsDbContext = Helper.GetDBContext(-1);
                _logger.LogDebug("Successfully connected to EDDS database");

                var instanceSettingManager = Helper.GetInstanceSettingBundle();
                var billingDatabaseId = instanceSettingManager.GetInt("LTAS Billing Management", "Management Database").Value;
                _logger.LogInformation("Retrieved billing database ID: {BillingDatabaseId}", billingDatabaseId);

                var billingDbContext = Helper.GetDBContext(billingDatabaseId);
                _logger.LogDebug("Successfully connected to billing database");

                using (var objectManager = Helper.GetServicesManager().CreateProxy<IObjectManager>(ExecutionIdentity.System))
                {
                    _logger.LogDebug("Created object manager proxy with system execution identity");
                    var dataHandler = new DataHandler(eddsDbContext, billingDbContext,Helper);

                    int[] jobIds = { 1, 2, 3, 4 };

                    foreach (int jobId in jobIds)
                    {
                        try
                        {
                            _logger.LogInformation("Starting job cycle for Job {JobId}", jobId);
                            RaiseMessage($"Checking job {jobId}...", 10);

                            if (ShouldExecuteJob(eddsDbContext, jobId))
                            {
                                _logger.LogInformation("Job {JobId} is scheduled for execution", jobId);
                                RaiseMessage($"Executing job {jobId}...", 10);
                                switch (jobId)
                                {
                                    case 1:
                                        _logger.LogInformation("Starting Monthly Reporting Job");
                                        ProcessMonthlyReportingJobsAsync(
                                           billingDatabaseId,
                                           objectManager,
                                           dataHandler,
                                           instanceSettingManager,
                                           relativityLogger)
                                           .GetAwaiter()
                                           .GetResult();
                                        UpdateJobExecutionTime(eddsDbContext, jobId);
                                        _logger.LogInformation("Completed Monthly Reporting Job");
                                        break;

                                    case 2:
                                        _logger.LogInformation("Starting Daily Operations Job");
                                        ProcessDailyOperationsAsync(
                                            billingDatabaseId,
                                            objectManager,
                                            dataHandler,
                                            instanceSettingManager,
                                            relativityLogger)
                                            .GetAwaiter()
                                            .GetResult();
                                        UpdateJobExecutionTime(eddsDbContext, jobId);
                                        _logger.LogInformation("Completed Daily Operations Job");
                                        break;

                                    case 3:
                                        _logger.LogInformation("Starting Billing Hourly Job");
                                        ProcessBillingHourlyJobsAsync(
                                            billingDatabaseId,
                                            objectManager,
                                            dataHandler,
                                            instanceSettingManager,
                                            relativityLogger)
                                            .GetAwaiter()
                                            .GetResult();
                                        UpdateJobExecutionTime(eddsDbContext, jobId);
                                        _logger.LogInformation("Completed Billing Hourly Job");
                                        break;

                                    case 4:
                                        _logger.LogInformation("Starting Billing Metrics Job");
                                        ProcessBillingMetricsAsync(
                                            billingDatabaseId,
                                            objectManager,
                                            dataHandler,
                                            instanceSettingManager,
                                            relativityLogger)
                                            .GetAwaiter()
                                            .GetResult();
                                        UpdateJobExecutionTime(eddsDbContext, jobId);
                                        _logger.LogInformation("Completed Billing Metrics Job");
                                        break;
                                }

                                _logger.LogInformation("Job {JobId} completed successfully", jobId);
                                RaiseMessage($"Job {jobId} completed successfully", 10);
                            }
                            else 
                            {
                                _logger.LogDebug("Job {JobId} is not scheduled to run at this time", jobId);
                            }
                        }
                        catch (Exception jobEx)
                        {
                            // Log error but continue with other jobs
                            var errorMessage = FormatErrorMessage(jobEx);
                            relativityLogger.LogError(jobEx, "Error executing job {JobId}: {ErrorMessage}", jobId, errorMessage);
                            _logger.LogError(jobEx, "Error executing job {JobId}: {ErrorMessage}", jobId, errorMessage);
                            RaiseMessage($"Error in job {jobId}: {errorMessage}", 1);
                        }
                    }
                }
                _logger.LogInformation("LTAS Billing Management Agent completed successfully");
            }
            catch (Exception ex)
            {
                var errorMessage = FormatErrorMessage(ex);
                _logger.LogError(ex, "Critical error in agent execution: {ErrorMessage}", errorMessage);
                relativityLogger.ForContext(typeof(LTASBillingWorker))
                      .LogError(ex, "Critical error in agent execution: {ErrorMessage}", errorMessage);
                RaiseMessage($"Critical error: {errorMessage}", 1);
                throw;
            }
        }

        private async Task ProcessBillingHourlyJobsAsync(
            int billingDatabaseId,
            IObjectManager objectManager,
            DataHandler dataHandler,
            IInstanceSettingsBundle instanceSettingManager,
            IAPILog logger)
        {
            _logger.LogInformation("Starting billing hourly jobs processing");
            _logger.LogDebug("Initializing ClientManager");

            var clientManager = new ClientManager(
                logger,
                Helper,
                objectManager,
                dataHandler,
                instanceSettingManager,
                billingDatabaseId);

            _logger.LogInformation("Processing client routines");
            await Task.Run(async () =>
            {
                await clientManager.ProcessClientRoutinesAsync();
            }).ConfigureAwait(false);

            _logger.LogDebug("Initializing MatterManager");
            var matterRoutine = new MatterManager(
                logger,
                Helper,
                objectManager,
                dataHandler,
                instanceSettingManager,
                billingDatabaseId);

            _logger.LogInformation("Processing matter routines");
            await Task.Run(async () =>
            {
                await matterRoutine.ProcessMatterRoutinesAsync();
            }).ConfigureAwait(false);

            _logger.LogDebug("Initializing WorkspaceManager");
            var workspaceRoutine = new WorkspaceManager(
                logger,
                Helper,
                objectManager,
                dataHandler,
                instanceSettingManager,
                billingDatabaseId);

            _logger.LogInformation("Processing workspace routines");
            await Task.Run(async () =>
            {
                await workspaceRoutine.ProcessWorkspaceRoutinesAsync();
            }).ConfigureAwait(false);

            _logger.LogDebug("Initializing DataSyncManager");
            var DataSyncRoutine = new DataSyncManager(
                logger,
                Helper,
                objectManager,
                instanceSettingManager,
                dataHandler,
                billingDatabaseId);

            _logger.LogInformation("Processing data sync routines");
            await Task.Run(async () =>
            {
                await DataSyncRoutine.ProcessDataSyncRoutinesAsync();
            }).ConfigureAwait(false);

            _logger.LogInformation("Completed billing hourly jobs processing");

            //var (clientId, clientSecret, instanceUrl, instanceId) = await GetCredentialsAsync(instanceSettingManager, logger);
            //var accessToken = await GetAccessTokenAsync(clientId, clientSecret, instanceUrl, logger);


            //var BillingRoutine = new BillingManager(
            //   logger,
            //   Helper,
            //   objectManager,
            //   dataHandler,
            //   instanceSettingManager,
            //   billingDatabaseId,
            //   instanceId,
            //   accessToken,
            //   instanceUrl);

            //await Task.Run(async () =>
            //{
            //    await BillingRoutine.ProcessBillingMetricsAsync();
            //}).ConfigureAwait(false);


            //var reportingManager = new ReportingManager(
            //    logger,
            //    Helper,
            //    objectManager,
            //    dataHandler,
            //    instanceSettingManager,
            //    billingDatabaseId,
            //    instanceId,
            //    accessToken,
            //    instanceUrl);

            //await Task.Run(async () =>
            //{
            //    await reportingManager.SendInvoiceEmail();
            //}).ConfigureAwait(false);

        }

        private async Task ProcessMonthlyReportingJobsAsync(
            int billingDatabaseId,
            IObjectManager objectManager,
            DataHandler dataHandler,
            IInstanceSettingsBundle instanceSettingManager,
            IAPILog logger)
        {
            _logger.LogInformation("Starting monthly reporting jobs");
            
            _logger.LogDebug("Retrieving API credentials");
            var (clientId, clientSecret, instanceUrl, instanceId) = await GetCredentialsAsync(instanceSettingManager, logger);

            _logger.LogDebug("Getting access token");
            var accessToken = await GetAccessTokenAsync(clientId, clientSecret, instanceUrl, logger);

            var reportingManager = new ReportingManager(
                logger,
                Helper,
                objectManager,
                dataHandler,
                instanceSettingManager,
                billingDatabaseId,
                instanceId,
                accessToken,
                instanceUrl);

            _logger.LogInformation("Processing monthly report");
            await Task.Run(async () =>
            {
                await reportingManager.MonthlyProcessingOnlyReport();
            }).ConfigureAwait(false);
            
            _logger.LogInformation("Completed monthly reporting jobs");
        }

        private async Task ProcessDailyOperationsAsync(
            int billingDatabaseId,
            IObjectManager objectManager,
            DataHandler dataHandler,
            IInstanceSettingsBundle instanceSettingManager,
            IAPILog logger)        
        {
            _logger.LogInformation("Starting daily operations processing");

            _logger.LogDebug("Initializing WorkspaceManager");
            var workspaceRoutine = new WorkspaceManager(
               logger,
               Helper,
               objectManager,
               dataHandler,
               instanceSettingManager,
               billingDatabaseId);

            _logger.LogInformation("Processing daily operations");
            await Task.Run(async () =>
            {
                await workspaceRoutine.ProcessDailyOperationsAsnyc();
            }).ConfigureAwait(false);

            _logger.LogInformation("Completed daily operations processing");
        }

        private async Task ProcessBillingMetricsAsync(
             int billingDatabaseId,
            IObjectManager objectManager,
            DataHandler dataHandler,
            IInstanceSettingsBundle instanceSettingManager,
            IAPILog logger)
        {
            _logger.LogInformation("Starting billing metrics processing");

            _logger.LogDebug("Retrieving API credentials");
            var (clientId, clientSecret, instanceUrl, instanceId) = await GetCredentialsAsync(instanceSettingManager, logger);
            
            _logger.LogDebug("Getting access token");
            var accessToken = await GetAccessTokenAsync(clientId, clientSecret, instanceUrl, logger);

            _logger.LogDebug("Initializing BillingManager");
            var BillingRoutine = new BillingManager(
                logger,
                Helper,
                objectManager,
                dataHandler,
                instanceSettingManager,
                billingDatabaseId,
                instanceId,
                accessToken,
                instanceUrl);

            _logger.LogInformation("Processing billing metrics");
            await Task.Run(async () =>
            {
                await BillingRoutine.ProcessBillingMetricsAsync();
            }).ConfigureAwait(false);

            _logger.LogInformation("Completed billing metrics processing");
        }

        private async Task<string> GetAccessTokenAsync(string clientId, string clientSecret, string instanceUrl, IAPILog logger)
        {
            _logger.LogDebug("Attempting to get access token");

            try
            {                
                var tokenHandler = new TokenHandler(logger);
                var token = await tokenHandler.GetAccessTokenAsync(clientId, clientSecret, instanceUrl);

                if (string.IsNullOrEmpty(token))
                {
                    logger.LogError("Failed to obtain access token.");
                    _logger.LogError("Failed to obtain access token - received empty token");
                    return string.Empty;
                }
                
                _logger.LogInformation("Successfully obtained access token");
                return token;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting access token");
                _logger.LogError(ex, "Error getting access token: {ErrorMessage}", ex.Message);
                return string.Empty;
            }
        }

        private async Task<(string clientId, string clientSecret, string instanceUrl, string instanceId)> GetCredentialsAsync(IInstanceSettingsBundle instanceSettings, IAPILog logger)
        {
            _logger.LogDebug("Retrieving credentials from instance settings");
            try
            {
                var clientId = await instanceSettings.GetStringAsync("LTAS Billing Management", "SecurityClientId");
                var clientSecret = await instanceSettings.GetStringAsync("LTAS Billing Management", "SecurityClientSecret");
                var instanceUrl = await instanceSettings.GetStringAsync("Relativity.Core", "RelativityInstanceURL");
                var instanceId = await instanceSettings.GetStringAsync("Relativity.Core", "InstanceIdentifier");

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(instanceUrl) || string.IsNullOrEmpty(instanceId))
                {
                    logger.LogError("One or more credentials are empty");
                    _logger.LogError("One or more required credentials are empty: " +
                        "ClientId={HasClientId}, ClientSecret={HasSecret}, InstanceUrl={HasUrl}, InstanceId={HasId}",
                        !string.IsNullOrEmpty(clientId),
                        !string.IsNullOrEmpty(clientSecret),
                        !string.IsNullOrEmpty(instanceUrl),
                        !string.IsNullOrEmpty(instanceId));
                    return (string.Empty, string.Empty, string.Empty, string.Empty);
                }

                _logger.LogInformation("Successfully retrieved all required credentials");
                return (clientId, clientSecret, instanceUrl, instanceId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving credentials from instance settings");
                _logger.LogError(ex, "Error retrieving credentials from instance settings");
                return (string.Empty, string.Empty, string.Empty, string.Empty);
            }
        }
        
        //TODO: Remove
        //private bool ShouldExecuteJob(IDBContext eddsDbContext, int jobId)
        //{
        //    string jobSQL = @"
        //                SELECT 
        //                    JobExecute_Time_Day,
        //                    JobExecute_Time_Hour,
        //                    JobExecute_Interval,
        //                    JobLastExecute_DateTime,
        //                    JobLastCheck_DateTime
        //                FROM EDDS.QE.AutomationControl 
        //                WHERE JobId = @jobId";

        //    var jobInfo = eddsDbContext.ExecuteSqlStatementAsDataTable(
        //        jobSQL,
        //        new[] { new SqlParameter("@jobId", jobId)}).Rows[0];

        //    eddsDbContext.ExecuteNonQuerySQLStatement(@"
        //        UPDATE qac 
        //        SET qac.JobLastCheck_DateTime = GETDATE()
        //        FROM EDDS.QE.AutomationControl qac 
        //        WHERE qac.JobId = @jobId",
        //        new[] { new SqlParameter("@jobId", jobId) });

        //    var now = DateTime.Now;
        //    int executeDay = 0;
        //    int executeHour = 0;
        //    bool hasExecuteDay = false;
        //    bool hasExecuteHour = false;

        //    if (jobInfo["JobExecute_Time_Day"] != DBNull.Value)
        //    {
        //        executeDay = Convert.ToInt32(jobInfo["JobExecute_Time_Day"]);
        //        hasExecuteDay = true;
        //    }

        //    if (jobInfo["JobExecute_Time_Hour"] != DBNull.Value)
        //    {
        //        executeHour = Convert.ToInt32(jobInfo["JobExecute_Time_Hour"]);
        //        hasExecuteHour = true;
        //    }

        //    int intervalHours = Convert.ToInt32(jobInfo["JobExecute_Interval"]);
        //    DateTime lastExecuteTime = jobInfo["JobLastExecute_DateTime"] != DBNull.Value
        //        ? Convert.ToDateTime(jobInfo["JobLastExecute_DateTime"])
        //        : DateTime.MinValue;

        //    //Monthly Job (interval = 720)
        //    if (intervalHours == 720)
        //    {
        //        if (!hasExecuteDay || !hasExecuteHour) return false;

        //        bool notRunThisMonth = lastExecuteTime.Month != now.Month || lastExecuteTime.Year != now.Year;
        //        bool isMonday = now.DayOfWeek == DayOfWeek.Monday;

        //        if (now.Day == executeDay)
        //        {
        //            // On the exact day, run at the specified hour
        //            return now.Hour >= executeHour && notRunThisMonth;
        //        }
        //        else if (now.Day > executeDay)
        //        {
        //            // After the specified day, run if we haven't run this month
        //            return notRunThisMonth && isMonday;
        //        }

        //        return false;
        //    }
        //    // Daily Job (interval = 24)
        //    if (intervalHours == 24)
        //    {
        //        if (!hasExecuteHour)
        //        {                 
        //            return false;
        //        }

        //        bool isWeekday = now.DayOfWeek != DayOfWeek.Saturday &&
        //                         now.DayOfWeek != DayOfWeek.Sunday;
        //        var notRunToday = lastExecuteTime.Date < now.Date;
        //        var isAfterExecutionHour = now.Hour >= executeHour;

        //        if (jobId == 4)
        //        {
        //            return notRunToday && isAfterExecutionHour;
        //        }

        //        var shouldRun = notRunToday && isAfterExecutionHour && isWeekday;
                
        //        return shouldRun;
        //    }

        //    // Hourly Job (interval = 1)

        //    if (intervalHours == 1)
        //    {
        //        var shouldRun = now >= lastExecuteTime.AddHours(intervalHours);
        //        RaiseMessage($"Hourly Job {jobId} Status - LastRun: {lastExecuteTime}, ShouldRun: {shouldRun}", 10);                
        //        return shouldRun;
        //    }

        //    return false;
        //}

        private bool ShouldExecuteJob(IDBContext eddsDbContext, int jobId)
        {
            _logger.LogDebug("Checking execution schedule for Job {JobId}", jobId);

            string jobSQL = @"
            SELECT 
                JobExecute_Time_Day,
                JobExecute_Time_Hour,
                JobExecute_Interval,
                JobLastExecute_DateTime,
                JobLastCheck_DateTime
            FROM EDDS.QE.AutomationControl 
            WHERE JobId = @jobId";

            var jobInfo = eddsDbContext.ExecuteSqlStatementAsDataTable(
                jobSQL,
                new[] { new SqlParameter("@jobId", jobId) }).Rows[0];

            eddsDbContext.ExecuteNonQuerySQLStatement(@"
            UPDATE qac 
            SET qac.JobLastCheck_DateTime = GETDATE()
            FROM EDDS.QE.AutomationControl qac 
            WHERE qac.JobId = @jobId",
                new[] { new SqlParameter("@jobId", jobId) });

            var now = DateTime.Now;
            int executeDay = 0;
            int executeHour = 0;
            bool hasExecuteDay = false;
            bool hasExecuteHour = false;

            if (jobInfo["JobExecute_Time_Day"] != DBNull.Value)
            {
                executeDay = Convert.ToInt32(jobInfo["JobExecute_Time_Day"]);
                hasExecuteDay = true;
                _logger.LogDebug("Job {JobId} has execution day set to {ExecuteDay}", jobId, executeDay);
            }

            if (jobInfo["JobExecute_Time_Hour"] != DBNull.Value)
            {
                executeHour = Convert.ToInt32(jobInfo["JobExecute_Time_Hour"]);
                hasExecuteHour = true;
                _logger.LogDebug("Job {JobId} has execution hour set to {ExecuteHour}", jobId, executeHour);
            }

            int intervalHours = Convert.ToInt32(jobInfo["JobExecute_Interval"]);
            DateTime lastExecuteTime = jobInfo["JobLastExecute_DateTime"] != DBNull.Value
                ? Convert.ToDateTime(jobInfo["JobLastExecute_DateTime"])
                : DateTime.MinValue;

            _logger.LogDebug("Job {JobId} settings - Interval: {IntervalHours}h, Last Execute: {LastExecuteTime}",
                jobId, intervalHours, lastExecuteTime);

            bool shouldRun = false;

            //Monthly Job (interval = 720)
            if (intervalHours == 720)
            {
                if (!hasExecuteDay || !hasExecuteHour)
                {
                    _logger.LogWarning("Monthly job {JobId} is missing required execution day or hour settings", jobId);
                    return false;
                }

                bool notRunThisMonth = lastExecuteTime.Month != now.Month || lastExecuteTime.Year != now.Year;
                bool isMonday = now.DayOfWeek == DayOfWeek.Monday;

                if (now.Day == executeDay)
                {
                    shouldRun = now.Hour >= executeHour && notRunThisMonth;
                }
                else if (now.Day > executeDay)
                {
                    shouldRun = notRunThisMonth && isMonday;
                }
            }
            // Daily Job (interval = 24)
            else if (intervalHours == 24)
            {
                if (!hasExecuteHour)
                {
                    _logger.LogWarning("Daily job {JobId} is missing required execution hour setting", jobId);
                    return false;
                }

                bool isWeekday = now.DayOfWeek != DayOfWeek.Saturday && now.DayOfWeek != DayOfWeek.Sunday;
                var notRunToday = lastExecuteTime.Date < now.Date;
                var isAfterExecutionHour = now.Hour >= executeHour;

                if (jobId == 4)
                {
                    shouldRun = notRunToday && isAfterExecutionHour;
                }
                else
                {
                    shouldRun = notRunToday && isAfterExecutionHour && isWeekday;
                }
            }
            // Hourly Job (interval = 1)
            else if (intervalHours == 1)
            {
                shouldRun = now >= lastExecuteTime.AddHours(intervalHours);
                _logger.LogDebug("Hourly Job {JobId} Status - LastRun: {LastRun}, ShouldRun: {ShouldRun}",
                    jobId, lastExecuteTime, shouldRun);
            }

            _logger.LogInformation("Job {JobId} execution check result: {ShouldRun}", jobId, shouldRun);
            return shouldRun;
        }

        //TODO: Remove
        //private void UpdateJobExecutionTime(IDBContext eddsDbContext, int jobId)
        //{
        //    eddsDbContext.ExecuteNonQuerySQLStatement(
        //            @"UPDATE qac 
        //                SET  qac.[JobLastExecute_DateTime] = GETDATE()
        //                FROM EDDS.QE.AutomationControl qac 
        //                WHERE qac.JobId = @jobId", new[] { new SqlParameter("@jobId", jobId) });
        //}

        private void UpdateJobExecutionTime(IDBContext eddsDbContext, int jobId)
        {
            _logger.LogDebug("Updating last execution time for Job {JobId}", jobId);
            try
            {
                eddsDbContext.ExecuteNonQuerySQLStatement(
                    @"UPDATE qac 
                    SET qac.[JobLastExecute_DateTime] = GETDATE()
                    FROM EDDS.QE.AutomationControl qac 
                    WHERE qac.JobId = @jobId",
                    new[] { new SqlParameter("@jobId", jobId) });
                _logger.LogDebug("Successfully updated last execution time for Job {JobId}", jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update last execution time for Job {JobId}", jobId);
                throw;
            }
        }
        //TODO: Remove
        //private string FormatErrorMessage(Exception ex)
        //{
        //    return ex.InnerException != null
        //        ? string.Concat("---", ex.InnerException, "---", ex.StackTrace)
        //        : string.Concat("---", ex.Message, "---", ex.StackTrace);
        //}

        private string FormatErrorMessage(Exception ex)
        {
            var message = ex.InnerException != null
                ? string.Concat("---", ex.InnerException, "---", ex.StackTrace)
                : string.Concat("---", ex.Message, "---", ex.StackTrace);
            return message;
        }
    }
}
