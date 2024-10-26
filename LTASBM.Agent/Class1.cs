using kCura.Agent;
using System;
using System.Runtime.InteropServices;
using LTASBM.Kepler.Interfaces.LTASBM.v1;
using System.Collections.Generic;
using Relativity.API;

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
            var servicesManager = Helper.GetServicesManager();
            string dB = "EDDS1623625";
            string serverName = @"esus02512841W05.sql-Y012.relativity.one\esus02512841W05";

            var keplerServiceProxy = servicesManager.CreateProxy<ILTASClient>();
            try
            {
                List<LTASClient> clients = keplerServiceProxy.GetClients(dB, serverName).Result;
            }
            catch (Exception ex)
            {
                Exception(ex, "Failure obtaining LTAS client list.");
            }
            finally
            {
               
            }           
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
