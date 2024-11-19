using kCura.Agent;
using System;
using System.Runtime.InteropServices;
using Relativity.API;
using LTASBM.Agent.Handlers;
using Relativity.Services.Objects;
using System.Threading.Tasks;
using LTASBM.Agent.Managers;
using System.Data.SqlClient;
using kCura.Vendor.Castle.Core.Logging;

namespace LTASBM.Agent
{
    [kCura.Agent.CustomAttributes.Name("LTAS Billing Management")]
    [Guid("fe5d6dca-598c-483f-942b-64af159f0982")]

    public class LTASBillingWorker : AgentBase
    {
        public override string Name => "LTAS Billing Management Worker";

        public override void Execute()
        {
            IAPILog logger = Helper.GetLoggerFactory().GetLogger().ForContext<LTASBillingWorker>();
            RaiseMessage("Starting LTAS Billing Management...", 10);

            try
            {
                var eddsDbContext = Helper.GetDBContext(-1);
                var instanceSettingManager = Helper.GetInstanceSettingBundle();
                var billingDatabaseId = instanceSettingManager.GetInt("LTAS Billing Management", "Management Database").Value;
                var billingDbContext = Helper.GetDBContext(billingDatabaseId);
                using (var objectManager = Helper.GetServicesManager().CreateProxy<IObjectManager>(ExecutionIdentity.System))
                {
                    var dataHandler = new DataHandler(eddsDbContext, billingDbContext);

                    int[] jobIds = { 1, 2, 3 };

                    foreach (int jobId in jobIds)
                    {
                        try
                        {
                            RaiseMessage($"Checking job {jobId}...", 10);

                            if (ShouldExecuteJob(eddsDbContext, jobId))
                            {
                                RaiseMessage($"Executing job {jobId}...", 10);
                                switch (jobId)
                                {
                                    case 1:
                                        ProcessMonthlyReportingJobsAsync(
                                           billingDatabaseId,
                                           objectManager,
                                           dataHandler,
                                           instanceSettingManager,
                                           logger)
                                           .GetAwaiter()
                                           .GetResult();
                                        break;
                                    case 2:
                                        ProcessDailyOperationsAsync(
                                            billingDatabaseId,
                                            objectManager,
                                            dataHandler,
                                            instanceSettingManager,
                                            logger)
                                            .GetAwaiter()
                                            .GetResult();
                                        break;
                                    case 3:
                                        ProcessBillingHourlyJobsAsync(
                                            billingDatabaseId,
                                            objectManager,
                                            dataHandler,
                                            instanceSettingManager,
                                            logger)
                                            .GetAwaiter()
                                            .GetResult();
                                        break;
                                }
                                UpdateJobExecutionTime(eddsDbContext, jobId);
                                RaiseMessage($"Job {jobId} completed successfully", 10);
                            }
                        }
                        catch (Exception jobEx)
                        {
                            // Log error but continue with other jobs
                            var errorMessage = FormatErrorMessage(jobEx);
                            logger.LogError(jobEx, "Error executing job {JobId}: {ErrorMessage}", jobId, errorMessage);
                            RaiseMessage($"Error in job {jobId}: {errorMessage}", 1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = FormatErrorMessage(ex);
                logger.ForContext(typeof(LTASBillingWorker))
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
            var clientManager = new ClientManager(
                logger,
                Helper,
                objectManager,
                dataHandler,
                instanceSettingManager,
                billingDatabaseId);

            await Task.Run(async () =>
            {
                await clientManager.ProcessClientRoutinesAsync();
            }).ConfigureAwait(false);

            var matterRoutine = new MatterManager(
                logger,
                Helper,
                objectManager,
                dataHandler,
                instanceSettingManager,
                billingDatabaseId);

            await Task.Run(async () =>
            {
                await matterRoutine.ProcessMatterRoutinesAsync();
            }).ConfigureAwait(false);

            var workspaceRoutine = new WorkspaceManager(
                logger,
                Helper,
                objectManager,
                dataHandler,
                instanceSettingManager,
                billingDatabaseId);

            await Task.Run(async () =>
            {
                await workspaceRoutine.ProcessWorkspaceRoutinesAsync();
            }).ConfigureAwait(false);

            var DataSyncRoutine = new DataSyncManager(
                logger,
                Helper,
                objectManager,
                instanceSettingManager,
                dataHandler,
                billingDatabaseId);

            await Task.Run(async () =>
            {
                await DataSyncRoutine.ProcessDataSyncRoutinesAsync();
            }).ConfigureAwait(false);
        }

        private async Task ProcessMonthlyReportingJobsAsync(
            int billingDatabaseId,
            IObjectManager objectManager,
            DataHandler dataHandler,
            IInstanceSettingsBundle instanceSettingManager,
            IAPILog logger)
        {
            var workspaceRoutine = new WorkspaceManager(
                logger,
                Helper,
                objectManager,
                dataHandler,
                instanceSettingManager,
                billingDatabaseId);

            await Task.Run(async () =>
            {
                await workspaceRoutine.ProcessMonthlyReportingJobs();
            }).ConfigureAwait(false);
        }

        private async Task ProcessDailyOperationsAsync(
            int billingDatabaseId,
            IObjectManager objectManager,
            DataHandler dataHandler,
            IInstanceSettingsBundle instanceSettingManager,
            IAPILog logger)        
        {
            var workspaceRoutine = new WorkspaceManager(
               logger,
               Helper,
               objectManager,
               dataHandler,
               instanceSettingManager,
               billingDatabaseId);

            await Task.Run(async () =>
            {
                await workspaceRoutine.ProcessDailyOperationsAsnyc();
            }).ConfigureAwait(false);
        }

        private bool ShouldExecuteJob(IDBContext eddsDbContext, int jobId)
        {
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
                new[] { new SqlParameter("@jobId", jobId)}).Rows[0];

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
            }

            if (jobInfo["JobExecute_Time_Hour"] != DBNull.Value)
            {
                executeHour = Convert.ToInt32(jobInfo["JobExecute_Time_Hour"]);
                hasExecuteHour = true;
            }

            int intervalHours = Convert.ToInt32(jobInfo["JobExecute_Interval"]);
            DateTime lastExecuteTime = jobInfo["JobLastExecute_DateTime"] != DBNull.Value
                ? Convert.ToDateTime(jobInfo["JobLastExecute_DateTime"])
                : DateTime.MinValue;

            //Monthly Job (interval = 720)
            if (intervalHours == 720)
            {
                if (!hasExecuteDay || !hasExecuteHour) return false;

                bool notRunThisMonth = lastExecuteTime.Month != now.Month || lastExecuteTime.Year != now.Year;
                bool isMonday = now.DayOfWeek == DayOfWeek.Monday;

                if (now.Day == executeDay)
                {
                    // On the exact day, run at the specified hour
                    return now.Hour >= executeHour && notRunThisMonth;
                }
                else if (now.Day > executeDay)
                {
                    // After the specified day, run if we haven't run this month
                    return notRunThisMonth && isMonday;
                }

                return false;
            }
            // Daily Job (interval = 24)
            if (intervalHours == 24)
            {
                if (!hasExecuteHour)
                {                 
                    return false;
                }

                var notRunToday = lastExecuteTime.Date < now.Date;
                var isAfterExecutionHour = now.Hour >= executeHour;
                var shouldRun = notRunToday && isAfterExecutionHour;

                return shouldRun;
            }

            // Hourly Job (interval = 1)

            if (intervalHours == 1)
            {
                var shouldRun = now >= lastExecuteTime.AddHours(intervalHours);
                RaiseMessage($"Hourly Job {jobId} Status - LastRun: {lastExecuteTime}, ShouldRun: {shouldRun}", 10);                
                return shouldRun;
            }

            return false;
        }
        private void UpdateJobExecutionTime(IDBContext eddsDbContext, int jobId)
        {
            eddsDbContext.ExecuteNonQuerySQLStatement(
                    @"UPDATE qac 
                        SET  qac.[JobLastExecute_DateTime] = GETDATE()
                        FROM EDDS.QE.AutomationControl qac 
                        WHERE qac.JobId = @jobId", new[] { new SqlParameter("@jobId", jobId) });
        }
        private string FormatErrorMessage(Exception ex)
        {
            return ex.InnerException != null
                ? string.Concat("---", ex.InnerException, "---", ex.StackTrace)
                : string.Concat("---", ex.Message, "---", ex.StackTrace);
        }
    }
}
