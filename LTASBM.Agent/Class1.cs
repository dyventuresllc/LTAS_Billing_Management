using kCura.Agent;
using System;
using System.Runtime.InteropServices;
using Relativity.API;
using LTASBM.Agent.Handlers;
using LTASBM.Agent.Routines;
using Relativity.Services.Objects;

namespace LTASBM.Agent
{
    [kCura.Agent.CustomAttributes.Name("LTAS Billing Manangement")]
    [Guid("fe5d6dca-598c-483f-942b-64af159f0982")]

    public class LTASBillingWorker : AgentBase
    {
        private IAPILog logger;
        public override string Name => "LTAS Billing Managment Worker";
        
        public override void Execute()
        {
            try 
            {
                RaiseMessage("starting...",10);
                //InitializeServices(out DataHandler dataHandler);

                IDBContext eddsDbContext,billingDbContext;
                eddsDbContext = Helper.GetDBContext(-1);
                var objectManager = Helper.GetServicesManager().CreateProxy<IObjectManager>(Relativity.API.ExecutionIdentity.System);
                logger = Helper.GetLoggerFactory().GetLogger();
                var servicesManager = Helper.GetServicesManager();
                var instanceSettingManager = Helper.GetInstanceSettingBundle();
                var billingDatabaseId = instanceSettingManager.GetInt("LTAS Billing Management", "Management Database").Value;
                billingDbContext = Helper.GetDBContext(billingDatabaseId);
                var dataHandler = new DataHandler(eddsDbContext, billingDbContext);

                //LTAS Billing Hourly Jobs 
                int intervalHours = (int)eddsDbContext.ExecuteSqlStatementAsScalar("SELECT JobExecute_Interval FROM EDDS.QE.AutomationControl WHERE JobId = 2;");
                DateTime lastExecuteTime = (DateTime)(eddsDbContext.ExecuteSqlStatementAsScalar("SELECT JobLastExecute_DateTime FROM EDDS.QE.AutomationControl WHERE JobId = 2;"));

                if (DateTime.Now >= lastExecuteTime.AddHours(intervalHours))
                {
                    var clientRoutines = new ClientRoutine(
                        logger,
                        dataHandler,
                        Helper.GetInstanceSettingBundle(),
                        Helper.GetServicesManager()
                    );
                    clientRoutines.ProcessClientRoutines(objectManager, billingDatabaseId);
                    eddsDbContext.ExecuteNonQuerySQLStatement("UPDATE qac SET  qac.[JobLastExecute_DateTime] = GETDATE() FROM EDDS.QE.AutomationControl qac WHERE qac.JobId = 2;");
                }                                                          
            }
            catch (Exception ex)
            {
                Exception(ex, $"Agent failure:\n");               
            }                  
        }

        private void InitializeServices(out DataHandler dataHandler)
        {
            logger = Helper.GetLoggerFactory().GetLogger();
            var instanceSettingManager = Helper.GetInstanceSettingBundle();
            var eddsDbContext = Helper.GetDBContext(-1);
            int billingDatabaseId = instanceSettingManager.GetInt("LTAS Billing Management", "Management Database").Value;
            var billingDbContext = Helper.GetDBContext(billingDatabaseId);


            dataHandler = new DataHandler(eddsDbContext, billingDbContext);
        }
        public void Exception(Exception ex, string errorMessage)
        {
            errorMessage += ex.InnerException != null ? string.Concat("---", ex.InnerException) : string.Concat("---", ex.Message);
            logger.LogError(errorMessage);
            RaiseError(errorMessage, ex.ToString());
            return;
        }       
    }
}
