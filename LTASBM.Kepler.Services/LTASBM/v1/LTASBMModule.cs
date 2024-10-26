using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using LTASBM.Kepler.Interfaces.LTASBM.v1;
using Relativity.API;

namespace LTASBM.Kepler.Services.LTASBM.v1
{
    public class LTASBMModule : ILTASClient
    {
        private readonly IDBContext _eddsdBContext;

        public LTASBMModule(IDBContext eddsdBContext)
        {
            _eddsdBContext = eddsdBContext;
        }

        public void Dispose()
        {
        }    
            
        //GOAL:
        //  To return a list of clients that are in the environment but not yet captured in our reporting tool  will be doing the same idiologty for matters and workspaces
        public async Task<List<LTASClient>> GetClients( string dB, string serverName)
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
            return await Task.FromResult(clients);
        }        
    }
}