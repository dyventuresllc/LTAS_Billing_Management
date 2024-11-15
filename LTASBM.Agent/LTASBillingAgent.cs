using kCura.Agent;
using System;
using System.Runtime.InteropServices;
using Relativity.API;
using LTASBM.Agent.Handlers;
using LTASBM.Agent.Routines;
using Relativity.Services.Objects;
using System.Threading.Tasks;


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
            RaiseMessage("starting...", 10);

            
            try
            {
                var eddsDbContext = Helper.GetDBContext(-1);
                var instanceSettingManager = Helper.GetInstanceSettingBundle();
                var billingDatabaseId = instanceSettingManager.GetInt("LTAS Billing Management", "Management Database").Value;
                var billingDbContext = Helper.GetDBContext(billingDatabaseId);
                var jobId = 2;
                using (var objectManager = Helper.GetServicesManager().CreateProxy<IObjectManager>(ExecutionIdentity.System))
                {
                    var dataHandler = new DataHandler(eddsDbContext, billingDbContext);
                    
                    if (ShouldExecuteJob(eddsDbContext, jobId))
                    {
                        ProcessBillingJobsAsync(billingDatabaseId, objectManager, dataHandler, instanceSettingManager, logger).GetAwaiter().GetResult();                       
                        UpdateJobExecutionTime(eddsDbContext, jobId);
                    }
                   
                    
                }
            }
            catch (Exception ex)
            {
                var errorMessage = FormatErrorMessage(ex);
                logger.ForContext(typeof(ClientRoutine))
                      .LogError($"Error Client Rountine: {errorMessage}");
                throw;
            }
        }

        private async Task ProcessBillingJobsAsync(int billingDatabaseId, IObjectManager objectManager, DataHandler dataHandler, IInstanceSettingsBundle instanceSettingManager, IAPILog logger)
        {
            var clientRoutine = new ClientRoutine(logger, Helper);

            await Task.Run(async () =>
            {
                await clientRoutine.ProcessClientRoutines(
                    billingDatabaseId,
                    objectManager,
                    dataHandler,
                    instanceSettingManager);
            }).ConfigureAwait(false);

            var matterRoutine = new MatterRoutine(logger, Helper);

            await Task.Run(async () =>
            {
                await matterRoutine.ProcessMatterRoutines(
                    billingDatabaseId,
                    objectManager,
                    dataHandler,
                    instanceSettingManager);
            }).ConfigureAwait(false);

            var workspaceRoutine = new WorkspaceRoutine(logger, Helper);

            await Task.Run(async () =>
            {
                  await workspaceRoutine.ProcessWorkspaceRoutines(
                      billingDatabaseId, 
                      objectManager, 
                      dataHandler, 
                      instanceSettingManager);
            }).ConfigureAwait(false);
        }        

        private bool ShouldExecuteJob(IDBContext eddsDbContext, int jobId)
        {
            int intervalHours = (int)eddsDbContext.ExecuteSqlStatementAsScalar(
                $"SELECT JobExecute_Interval FROM EDDS.QE.AutomationControl WHERE JobId = {jobId};");

            DateTime lastExecuteTime = (DateTime)eddsDbContext.ExecuteSqlStatementAsScalar(
                $"SELECT JobLastExecute_DateTime FROM EDDS.QE.AutomationControl WHERE JobId = {jobId};");

            return DateTime.Now >= lastExecuteTime.AddHours(intervalHours);
        }
        private void UpdateJobExecutionTime(IDBContext eddsDbContext, int jobId)
        {
            eddsDbContext.ExecuteNonQuerySQLStatement(
                             "UPDATE qac SET  qac.[JobLastExecute_DateTime] = GETDATE() " +
                            $"FROM EDDS.QE.AutomationControl qac WHERE qac.JobId = {jobId};");
        }
        private string FormatErrorMessage(Exception ex)
        {
            return ex.InnerException != null
                ? string.Concat("---", ex.InnerException, "---", ex.StackTrace)
                : string.Concat("---", ex.Message, "---", ex.StackTrace);
        }
    }
}
