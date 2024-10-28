using Relativity.Kepler.Services;

namespace LTASBM.Kepler.Interfaces.LTASBM
{
    /// <summary>
    /// LTASBM Module Interface.
    /// </summary>

    [ServiceModule("LTASBM Module")]
    [RoutePrefix("LTASBM", VersioningStrategy.Namespace)]
    public interface ILTASBMSVCModule
    {        
    }

}
