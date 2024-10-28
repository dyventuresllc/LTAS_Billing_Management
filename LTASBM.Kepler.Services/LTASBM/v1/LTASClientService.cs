using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using LTASBM.Kepler.Interfaces.LTASBM.v1;
using Relativity.API;

namespace LTASBM.Kepler.Services.LTASBM.v1
{
    public class LTASClientService : ILTASClient
    {
        private IDBContext _eddsdBContext;
        private IAPILog _logger;

        public LTASClientService(IDBContext eddsdBContext, IAPILog logger)
        {
            _eddsdBContext = eddsdBContext;
            _logger = logger.ForContext<LTASClientService>();
        }

        public void Dispose()
        {
        }

        //GOAL:
        //  To return a list of clients that are in the environment but not yet captured in our reporting tool  will be doing the same idiologty for matters and workspaces
        public async Task<List<LTASClient>> GetClientsAsync(string dB, string serverName)
        {            
            var clients = new List<LTASClient>();
            string sql;

            sql = @"SELECT DISTINCT
                        ec.ClientNumber, ec.ClientName, ec.CreatedBy
                    FROM 
                    EDDS.Eddsdbo.ExtendedCase ec
                    WHERE ec.ClientNumber NOT IN 
                    (   SELECT 
                            DISTINCT ClientNumber
                        FROM OPENQUERY([" + serverName + @"], 
                        'SELECT c.ClientNumber, c.ClientName FROM [" + dB + @"].eddsdbo.client c WITH (NOLOCK)') 
                    )";
            try
            {
                DataTable dt = _eddsdBContext.ExecuteSqlStatementAsDataTable(sql);
                foreach (DataRow row in dt.Rows)
                {
                    var client = new LTASClient
                    {
                        Number = row["ClientNumber"].ToString(),
                        Name = row["ClientName"].ToString(),
                        CreatedBy = Convert.ToInt32(row["CreatedBy"])
                    };
                    clients.Add(client);
                }
                return await Task.Run((() => clients)).ConfigureAwait(false);
            }
            catch (Exception ex) 
            {                
                _logger.LogError(ex.InnerException != null ? string.Concat("---", ex.InnerException) : string.Concat("---", ex.Message));
                throw;
            }          
        }       
    }
}