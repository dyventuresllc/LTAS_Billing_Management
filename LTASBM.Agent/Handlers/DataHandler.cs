using LTASBM.Agent.Models;
using Relativity.API;
using System;
using System.Collections.Generic;
using System.Data;

namespace LTASBM.Agent.Handlers
{
    public class DataHandler
    {
        private readonly IDBContext EddsDbContext, BillingDbContext;

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
                           WHERE  ec.[Number] NOT IN ('Relativity','Relativity Template','Vendor','Review Vendor','Co-Counsel','Software','QE Template')";
            var dt = EddsDbContext.ExecuteSqlStatementAsDataTable(sql);

            foreach (DataRow row in dt.Rows)
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
            string sql = @"SELECT ArtifactID, EDDSClientArtifactID, ClientNumber, ClientName FROM eddsdbo.Client WITH (NOLOCK)";
            var dt = BillingDbContext.ExecuteSqlStatementAsDataTable(sql);

            foreach (DataRow row in dt.Rows)
            {
                clients.Add(new BillingClients
                {
                    BillingClientArtifactID = row.Field<int>("ArtifactID"),
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

            string sql = @"SELECT 
                                em.ArtifactID, em.[Name], em.Number,u.FirstName 'CreatedByFirstName', u.EmailAddress 'CreatedByEmailAddress', em.ClientArtifactID
                           FROM EDDS.eddsdbo.ExtendedMatter em  
                           JOIN EDDS.eddsdbo.[User] u WItH (NOLOCK)
                                ON u.ArtifactID = em.CreatedBy
                           WHERE em.Number NOT IN ('Relativity Template', 'Billing','Relativity', 'QE Template');";
            var dt = EddsDbContext.ExecuteSqlStatementAsDataTable(sql);

            foreach (DataRow row in dt.Rows)
            {
                matters.Add(new EddsMatters
                {
                    EddsMatterArtifactId = row.Field<int>("ArtifactID"),
                    EddsMatterName = row["Name"]?.ToString(),
                    EddsMatterNumber = row["Number"]?.ToString(),
                    EddsMatterCreatedByFirstName = row["CreatedByFirstName"].ToString(),
                    EddsMatterCreatedByEmail = row["CreatedByEmailAddress"].ToString(),
                    EddsMatterClientEDDSArtifactID = row.Field<int>("ClientArtifactID")
                });
            }
            return matters;
        }

        public List<BillingMatters> BillingMatters()
        {
            var matters = new List<BillingMatters>();
            string sql = @"
                            SELECT 
                                m.ArtifactID, m.EDDSMatterArtifactID, m.MatterNumber, m.MatterName, m.ClientID, c.EDDSClientArtifactID 
                            FROM eddsdbo.Matter m WITH (NOLOCK)
                            LEFT JOIN eddsdbo.Client c WITH (NOLOCK)
                                ON c.ArtifactID = m.ClientID;";
            var dt = BillingDbContext.ExecuteSqlStatementAsDataTable(sql);

            foreach (DataRow row in dt.Rows)
            {
                matters.Add(new BillingMatters
                {
                    BillingMatterArtficatId = row.Field<int>("ArtifactID"),
                    BillingEddsMatterArtifactId = row.Field<int>("EDDSMatterArtifactID"),
                    BillingEddsMatterName = row["MatterName"]?.ToString(),
                    BillingEddsMatterNumber = row["MatterNumber"]?.ToString(),
                    BillingClientId = row.Field<int>("ClientID"),
                    BillingMatterEDDSClientArtifactID = row.Field<int>("EDDSClientArtifactID")
                });
            }
            return matters;
        }

        public List<EddsWorkspaces> EddsWorkspaces()
        {
            var workspaces = new List<EddsWorkspaces>();

            string sql = @"SELECT 
	                            ec.ArtifactID, ec.CreatedByName, ec.CreatedOn, ec.Name, ec.MatterName, ec.MatterNumber, ec.MatterArtifactID, ec.ClientName,  ec.ClientNumber, ec.ClientArtifactID, ec.Notes, ec.Keywords, ec.StatusName
                            FROM EDDS.eddsdbo.ExtendedCase ec
                            JOIN EDDS.eddsdbo.[User] u WITH (NOLOCK)
                                ON u.ArtifactID = ec.CreatedBy
                            WHERE ec.ClientName NOT IN('Relativity Template','Quinn Emanuel Template')
	                        AND ec.MatterNumber NOT IN('Relativity')";	                

            var dt = EddsDbContext.ExecuteSqlStatementAsDataTable(sql);

            foreach (DataRow row in dt.Rows)
            {
                workspaces.Add(new EddsWorkspaces
                {
                    EddsWorkspaceArtifactId = row.Field<int>("ArtifactID"),
                    EddsWorkspaceCreatedBy = row["CreatedByName"]?.ToString(),
                    EddsWorkspaceCreatedOn = row.Field<DateTime>("CreatedOn"),
                    EddsWorkspaceName = row["Name"]?.ToString(),
                    EddsWorkspaceMatterArtifactId = row.Field<int>("MatterArtifactID"),
                    EddsWorkspaceClientArtifactId = row.Field<int>("ClientArtifactID"),
                    EddsWorkspaceAnalyst = row["Notes"]?.ToString(),
                    EddsWorkspaceCaseTeam = row["Keywords"]?.ToString(),
                    EddsWorkspaceStatusName = row["StatusName"]?.ToString(),
                    EddsMatterName = row["MatterName"]?.ToString()
                });
            }
            return workspaces;
        }

        public List<BillingWorkspaces> BillingWorkspaces()
        {
            string sql = @"SELECT TOP 1 CodeTypeID FROM eddsdbo.CodeType WHERE DisplayName LIKE 'Case%Status';";
            var codeTypeId = BillingDbContext.ExecuteSqlStatementAsScalar(sql);

            var workspaces = new List<BillingWorkspaces>();

            sql = $@"SELECT 
                        w.ArtifactID, w.EDDSWorkspaceArtifactID, w.WorkspaceCreatedBy, w.WorkspaceCreatedOn, w.WorkspaceName, w.WorkspaceMatterObject, w.LTASAnalyst, w.CaseTeam, c.[Name] 'Case Status', m.EDDSMatterArtifactID
	                FROM eddsdbo.workspaces w
	                LEFT JOIN eddsdbo.Matter m
		                ON w.WorkspaceMatterObject = m.ArtifactID
	                LEFT JOIN eddsdbo.ZCodeArtifact_{codeTypeId} ca
		                ON ca.AssociatedArtifactID = w.ArtifactID
	                JOIN eddsdbo.Code c
		            ON c.ArtifactID = ca.CodeArtifactID;";

            var dt = BillingDbContext.ExecuteSqlStatementAsDataTable(sql);

            foreach (DataRow row in dt.Rows)
            {
                workspaces.Add(new BillingWorkspaces
                {
                    BillingWorkspaceArtifactId = row["ArtifactID"] != DBNull.Value ? Convert.ToInt32(row["ArtifactID"]) : 0,
                    BillingWorkspaceEddsArtifactId = row["EDDSWorkspaceArtifactID"] != DBNull.Value ? Convert.ToInt32(row["EDDSWorkspaceArtifactID"]) : 0,
                    BillingWorkspaceCreatedBy = Convert.ToString(row["WorkspaceCreatedBy"]),
                    BillingWorkspaceCreatedOn = row["WorkspaceCreatedOn"] != DBNull.Value ? Convert.ToDateTime(row["WorkspaceCreatedOn"]) : DateTime.MinValue,
                    BillingWorkspaceName = Convert.ToString(row["WorkspaceName"]),
                    BillingWorkspaceMatterArtifactId = row["WorkspaceMatterObject"] != DBNull.Value ? Convert.ToInt32(row["WorkspaceMatterObject"]) : 0,
                    BillingWorkspaceAnalyst = Convert.ToString(row["LTASAnalyst"]),
                    BillingWorkspaceCaseTeam = Convert.ToString(row["CaseTeam"]),
                    BillingStatusName = Convert.ToString(row["Case Status"]),
                    BillingWorkspaceMatterEddsArtifactId = row["EDDSMatterArtifactID"] != DBNull.Value ? Convert.ToInt32(row["EDDSMatterArtifactID"]) : 0,
                });
            }
            return workspaces;
        }

        public List<EddsUsers> EDDSUsers()
        {
            var users = new List<EddsUsers>();

            string sql = @"
                        SELECT 
                            ArtifactID, 
                            FirstName, 
                            LastName, 
                            EmailAddress,
	                        RelativityAccess
                        FROM EDDS.eddsdbo.ExtendedUser
                        WHERE EmailAddress LIKE '%@quinnemanuel.com'";
            
            var dt = EddsDbContext.ExecuteSqlStatementAsDataTable(sql);

            foreach (DataRow row in dt.Rows)
            {
                users.Add(new EddsUsers 
                {
                    EddsUserArtifactId = row["ArtifactID"] != DBNull.Value 
                        ? Convert.ToInt32(row["ArtifactId"])
                        : 0,
                    EddsUserFirstName = row["FirstName"].ToString() ?? string.Empty,
                    EddsUserLastName = row["LastName"].ToString() ?? string.Empty,
                    EddsUserEmailAddress = row["EmailAddress"].ToString() ?? string.Empty,
                    EddsUserRelativityAccess = row["RelativityAccess"] != DBNull.Value
                        && Convert.ToBoolean(row["RelativityAccess"])
                });
            }
            return users;
        }

        public List<BillingUsers> BillingUsers()
        {
            var users = new List<BillingUsers>();

            string sql = @"
                        SELECT 
	                        ArtifactID, 
                            EDDSUserArtifactId, 
                            FirstName, 
                            LastName, 
                            EmailAddress 
                        FROM eddsdbo.BillingRecipients";

            var dt = BillingDbContext.ExecuteSqlStatementAsDataTable(sql);

            foreach (DataRow row in dt.Rows) 
            {
                users.Add(new BillingUsers
                {
                    BillingUserArtifactId = row.Field<int>("ArtifactID"),
                    BillingUserEddsArtifactId = row.Field<int>("EDDSUserArtifactId"),
                    BillingUserFirstName = row["FirstName"].ToString(),
                    BillingUserLastName = row["LastName"].ToString(),
                    BillingUserEmailAddress = row["EmailAddress"].ToString()
                });
            }
            return users;
        }

    }
}