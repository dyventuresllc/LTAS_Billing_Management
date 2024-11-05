using LTASBM.Agent.Models;
using Relativity.API;
using System.Collections.Generic;
using System.Data;

namespace LTASBM.Agent.Handlers
{
    public class DataHandler
    {
        readonly IDBContext _eddsDbContext,_billingDbContext;
        public DataHandler(IDBContext eddsDbContext, IDBContext billingDbContext)
        {
            _eddsDbContext = eddsDbContext;
            _billingDbContext = billingDbContext;
        }
            
        public List<EddsClients> EDDSClients()
        {
            var clients = new List<EddsClients>();

            string sql = @"SELECT ec.ArtifactID, ec.Name, ec.Number, u.FirstName 'CreatedByFirstName', u.EmailAddress 'CreatedByEmailAddress'
                           FROM EDDS.eddsdbo.ExtendedClient ec
                           JOIN EDDS.eddsdbo.[User] u WITH (NOLOCK)
                                ON u.ArtifactID = ec.CreatedBy
                           WHERE  ec.[Number] NOT IN ('Relativity','Relativity Template','Vendor','Review Vendor','Co-Counsel','Software','QE Template','QE')";
            var dt = _eddsDbContext.ExecuteSqlStatementAsDataTable(sql);

            foreach(DataRow row in dt.Rows) 
            {
                clients.Add(new EddsClients
                {
                    EddsClientArtifactId = row.Field<int>("ArtifactID"),
                    EddsClientName = row["Name"]?.ToString(),
                    EddsClientNumber = row["Number"]?.ToString(),
                    EddsClientCreatedByFirstName = row["CreatedByFirstName"].ToString(),
                    EddsClientCreatedByEmail = row["CreatedByEmailAddress"].ToString() 
                });
            }
            return clients;
        }

        public List<BillingClients> BillingClients()
        { 
            var clients = new List<BillingClients>();
            string sql = @"SELECT EDDSClientArtifactID, ClientNumber, ClientName FROM eddsdbo.Client WITH (NOLOCK)";
            var dt = _billingDbContext.ExecuteSqlStatementAsDataTable(sql);

            foreach (DataRow row in dt.Rows)
            {
                clients.Add(new BillingClients
                {
                    BillingEddsClientArtifactId = row.Field<int>("EDDSClientArtifactID"),
                    BillingEddsClientName = row["ClientName"]?.ToString(),
                    BillingEddsClientNumber = row["ClientNumber"]?.ToString()
                });
            }
            return clients;
        }
    }
}
