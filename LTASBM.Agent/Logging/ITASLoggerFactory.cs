using Relativity.API;

namespace LTASBM.Agent.Logging
{
    public static class LoggerFactory
    {
        public static ILTASLogger CreateLogger<T>(
            IDBContext eddsDbContext,
            IHelper helper,
            IAPILog logger,
            string applicationName = "LTAS Billing Management")
        {
            return new LTASLogger(
                eddsDbContext,
                helper,
                logger,
                applicationName,
                typeof(T).Name
            );
        }
    }

}
