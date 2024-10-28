using Relativity.Kepler.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LTASBM.Kepler.Interfaces.LTASBM.v1
{
    [WebService("LTASClient Service")]
    [ServiceAudience(Audience.Public)]
    [RoutePrefix("ltasclients")]
    
    public interface ILTASClient: IDisposable
    {

        [HttpGet]
        [Route("GetClients")]
        Task<List<LTASClient>> GetClientsAsync(string dB, string serverName);
    }

    public class LTASClient
    {
        public string Number { get; set; }
        public string Name { get; set; }
        public int CreatedBy { get; set; } 
    }
}
