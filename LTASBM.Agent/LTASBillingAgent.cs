using kCura.Agent;
using System;
using System.Runtime.InteropServices;
using Relativity.API;
using LTASBM.Agent.Handlers;
using LTASBM.Agent.Routines;

namespace LTASBM.Agent
{
    [kCura.Agent.CustomAttributes.Name("LTAS Billing Manangement")]
    [Guid("fe5d6dca-598c-483f-942b-64af159f0982")]

    public class LTASBillingWorker : AgentBase
    {
        public override string Name => "LTAS Billing Managment Worker";
        
        public override void Execute()
        {
            IAPILog logger = Helper.GetLoggerFactory().GetLogger().ForContext<LTASBillingWorker>();

            try 
            {
                RaiseMessage("starting...",10);
               
                IDBContext eddsDbContext,billingDbContext;
                eddsDbContext = Helper.GetDBContext(-1);
                var instanceSettingManager = Helper.GetInstanceSettingBundle();
                var billingDatabaseId = instanceSettingManager.GetInt("LTAS Billing Management", "Management Database").Value;
                billingDbContext = Helper.GetDBContext(billingDatabaseId);
                
                var servicesManager = Helper.GetServicesManager();
                var dataHandler = new DataHandler(eddsDbContext, billingDbContext);
                
                //LTAS Billing Hourly Jobs 
                int intervalHours = (int)eddsDbContext.ExecuteSqlStatementAsScalar("SELECT JobExecute_Interval FROM EDDS.QE.AutomationControl WHERE JobId = 2;");
                DateTime lastExecuteTime = (DateTime)(eddsDbContext.ExecuteSqlStatementAsScalar("SELECT JobLastExecute_DateTime FROM EDDS.QE.AutomationControl WHERE JobId = 2;"));

                if (DateTime.Now >= lastExecuteTime.AddHours(intervalHours))
                {                   
                    var clientRoutine = new ClientRoutine();
                    clientRoutine.ProcessClientRoutines(billingDatabaseId, servicesManager, dataHandler, instanceSettingManager, logger);

                    eddsDbContext.ExecuteNonQuerySQLStatement("UPDATE qac SET  qac.[JobLastExecute_DateTime] = GETDATE() FROM EDDS.QE.AutomationControl qac WHERE qac.JobId = 2;");
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
