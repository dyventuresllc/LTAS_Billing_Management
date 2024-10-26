using Relativity.Kepler.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LTASBM.Kepler.Interfaces.LTASBM.v1
{
    [ServiceModule("LTASBM Module")]
    [RoutePrefix("LTASBM", VersioningStrategy.Namespace)]
    public interface ILTASClient
    {
        [Route("GetClients")]
        [HttpGet]
        Task<List<LTASClient>> GetClients(string dB, string serverName);
    }

    public class LTASClient
    {
        public string Number { get; set; }
        public string Name { get; set; }
        public int CreatedBy { get; set; } 
    }
}
