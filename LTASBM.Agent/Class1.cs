using kCura.Agent;
using System;
using System.Runtime.InteropServices;
using LTASBM.Kepler.Interfaces.LTASBM.v1;
using System.Collections.Generic;
using Relativity.API;
using System.Data.SqlClient;
using Relativity.Identity.V1.Services;

namespace LTASBM.Agent
{
    [kCura.Agent.CustomAttributes.Name("LTAS Billing Manangement")]
    [Guid("fe5d6dca-598c-483f-942b-64af159f0982")]

    public class LTASBillingWorker : AgentBase
    {
        private IAPILog logger;
        private string managementDb = null;
        private string serverName = null;
        public override string Name => "LTAS Billing Managment Worker";

        public override void Execute()
        {
            logger = Helper.GetLoggerFactory().GetLogger();
            var servicesManager = Helper.GetServicesManager();
            var instanceSettingManager = Helper.GetInstanceSettingBundle();
            IDBContext eddsDbContext;
            eddsDbContext = Helper.GetDBContext(-1);
            IUserManager userManager = servicesManager.CreateProxy<IUserManager>(ExecutionIdentity.System);
            InitializeDatabaseSettings(instanceSettingManager, eddsDbContext);
                      
            var keplerServiceProxy = servicesManager.CreateProxy<ILTASClient>(ExecutionIdentity.System);
            try
            {
                List<LTASClient> clients = keplerServiceProxy.GetClientsAsync(managementDb, serverName).Result;
                Tasks.Tasks.ClientIncorrectFormat(logger, instanceSettingManager, userManager, clients);
            }
            catch (Exception ex)
            {
                Exception(ex, "Failure obtaining LTAS client list.");
            }                  
        }

        public void Exception(Exception ex, string errorMessage)
        {
            errorMessage += ex.InnerException != null ? string.Concat("---", ex.InnerException) : string.Concat("---", ex.Message);
            logger.LogError(errorMessage);
            RaiseError(errorMessage, ex.ToString());
            return;
        }

        public void InitializeDatabaseSettings(IInstanceSettingsBundle instanceSettingManager, IDBContext eddsDbContext)
        {
            logger.LogInformation("Starting to retrieve management database variables...");
            try
            {
                managementDb = "EDDS" + Convert.ToInt32(instanceSettingManager.GetUInt("LTAS Billing Management", "Management Database"));
            }
            catch (Exception ex)
            {
                Exception(ex, "Failure obtaining Management Database instance setting.");
            }

            try
            {
                string sql = "SELECT DbLocation FROM EDDS.eddsdbo.ExtendedCase WHERE ArtifactID @WorkspaceArtifactId";
                SqlParameter workspaceArtifactIdParam = new SqlParameter("@WorkspaceArtifactId", System.Data.SqlDbType.Int);

                serverName = (eddsDbContext.ExecuteSqlStatementAsScalar<string>(sql, workspaceArtifactIdParam));
            }
            catch (Exception ex)
            {
                Exception(ex, "Failure obtaining Management Database server location.");
            }

            if (managementDb != null && serverName != null)
            {
                logger.LogInformation("ManagementDb '{managementDb}' and Server '{serverName}' identified successfully", managementDb, serverName);
            }
        }
    }
}
