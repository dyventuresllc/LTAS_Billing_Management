using kCura.Agent;
using System;
using System.Runtime.InteropServices;
using LTASBM.Kepler.Interfaces.LTASBM.v1;
using System.Collections.Generic;

namespace LTASBM.Agent
{
    [kCura.Agent.CustomAttributes.Name("LTAS Billing Manangement")]
    [Guid("fe5d6dca-598c-483f-942b-64af159f0982")]

    public class LTASBillingWorker : AgentBase
    {
        public override string Name => "LTAS Billing Managment Worker";

        public override void Execute()
        {
            using (var servicesManager = Helper.GetServicesManager())
            {
                string dB = "EDDS1623625"; 
                string serverName = @"esus02512841W05.sql-Y012.relativity.one\esus02512841W05"; 

                var keplerServiceProxy = servicesManager.CreateProxy<ILTASClient>();
                List<LTASClient> clients = keplerServiceProxy.GetClients(dB, serverName).Result;


            }

        }
    }
}
