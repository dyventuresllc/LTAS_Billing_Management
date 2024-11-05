using Relativity.Kepler.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LTASBM.Kepler.Interfaces.LTASBM.v1
{
    [WebService("LTASClientService")]
    [ServiceAudience(Audience.Public)]
    [RoutePrefix("ltasclients")]
    
    public interface ILTASClient: IDisposable
    {

        [HttpGet]
        [Route("")]
        Task<List<LTASClient>> GetClientsAsync();
    }

    public class LTASClient
    {
        public int ArtifactID { get; set; } 
        public string Number { get; set; }
        public string Name { get; set; }
        public int CreatedBy { get; set; } 
    }
}
