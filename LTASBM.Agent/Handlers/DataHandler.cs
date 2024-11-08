using LTASBM.Agent.Models;
using Relativity.API;
using System.Collections.Generic;
using System.Data;

namespace LTASBM.Agent.Handlers
{
    public class DataHandler
    {
        private readonly IDBContext EddsDbContext,BillingDbContext;

        public DataHandler(IDBContext eddsDbContext, IDBContext billingDbContext)
        {
            EddsDbContext = eddsDbContext;
            BillingDbContext = billingDbContext;
        }
            
        public List<EddsClients> EDDSClients()
        {
            var clients = new List<EddsClients>();

            string sql = @"SELECT ec.ArtifactID, ec.Name, ec.Number, u.FirstName 'CreatedByFirstName', u.EmailAddress 'CreatedByEmailAddress'
                           FROM EDDS.eddsdbo.ExtendedClient ec
                           JOIN EDDS.eddsdbo.[User] u WITH (NOLOCK)
                                ON u.ArtifactID = ec.CreatedBy
                           WHERE  ec.[Number] NOT IN ('Relativity','Relativity Template','Vendor','Review Vendor','Co-Counsel','Software','QE Template','QE')";
            var dt = EddsDbContext.ExecuteSqlStatementAsDataTable(sql);

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
            var dt = BillingDbContext.ExecuteSqlStatementAsDataTable(sql);

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

        public List<EddsMatters> EddsMatters() 
        {
            var matters = new List<EddsMatters>();

            string sql = @"SELECT em.ArtifactID, em.[Name], em.Number,u.FirstName 'CreatedByFirstName', u.EmailAddress 'CreatedByEmailAddress'
                           FROM EDDS.eddsdbo.ExtendedMatter em  
                           JOIN EDDS.eddsdbo.[User] u WItH (NOLOCK)
                                ON u.ArtifactID = em.CreatedBy
                           WHERE em.Number NOT IN ('Relativity Template', 'Billing','QE Internal','Relativity', 'QE Template')";
            var dt = EddsDbContext.ExecuteSqlStatementAsDataTable(sql);

            foreach (DataRow row in dt.Rows)
            {
                matters.Add(new EddsMatters
                {
                    EddsMatterArtifactId = row.Field<int>("ArtifactID"),
                    EddsMatterName = row["Name"]?.ToString(),
                    EddsMatterNumber = row["Number"]?.ToString(),
                    EddsMatterCreatedByFirstName = row["CreatedByFirstName"].ToString(),
                    EddsMatterCreatedByEmail = row["CreatedByEmailAddress"].ToString()
                }) ;
            }
            return matters;
        }

        public List<BillingMatters> BillingMatters()
        {
            var matters = new List<BillingMatters>();
            string sql = @"SELECT EDDSMatterArtifactID, MatterNumber, MatterName FROM eddsdbo.Matter WITH (NOLOCK)";
            var dt = BillingDbContext.ExecuteSqlStatementAsDataTable(sql);

            foreach (DataRow row in dt.Rows)
            {
                matters.Add(new BillingMatters
                {
                    BillingEddsMatterArtifactId = row.Field<int>("EDDSMatterArtifactID"),
                    BillingEddsMatterName = row["MatterName"]?.ToString(),
                    BillingEddsMatterNumber = row["MatterNumber"]?.ToString()
                });
            }
            return matters;
        }
    }
}
