using LTASBM.Agent.Models;
using Relativity.API;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace LTASBM.Agent.Handlers
{
    public class DataHandler
    {
        private readonly IDBContext EddsDbContext, BillingDbContext;
        private readonly IHelper _helper;

        public DataHandler(IDBContext eddsDbContext, IDBContext billingDbContext, IHelper helper)
        {
            EddsDbContext = eddsDbContext;
            BillingDbContext = billingDbContext;
            _helper = helper;
        }

        public List<EddsClients> EDDSClients()
        {
            var clients = new List<EddsClients>();

            string sql = @"SELECT ec.ArtifactID, ec.Name, ec.Number, u.FirstName 'CreatedByFirstName', u.EmailAddress 'CreatedByEmailAddress'
                           FROM EDDS.eddsdbo.ExtendedClient ec
                           JOIN EDDS.eddsdbo.[User] u WITH (NOLOCK)
                                ON u.ArtifactID = ec.CreatedBy
                           WHERE  ec.[Number] NOT IN ('Relativity','Relativity Template','Vendor','Review Vendor','Co-Counsel','Software','QE Template','Client','QE FileShare')";
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
                            WHERE ec.ClientName NOT IN('Relativity Template','Quinn Emanuel Template','QE FileShare')
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
                        WHERE EmailAddress NOT LIKE '%@PreviewUser.com' AND EmailAddress NOT LIKE '@Relativity.com'";

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

        public List<TempBilingDetailsWorkspace> TempBillingDetailsWorkspace()
        {
            var tempBillingDetailsWorkspace = new List<TempBilingDetailsWorkspace>();

            string sql = @"
                        SELECT
	                        WID, MID, DateKey, RVWH, RPYH, RVWP, RPYP, TU, PU, APU, ARU, CS
                        FROM eddsdbo.BillingDetailsTempWorkspace";

            var dt = BillingDbContext.ExecuteSqlStatementAsDataTable(sql);

            foreach (DataRow row in dt.Rows)
            {
                tempBillingDetailsWorkspace.Add(new TempBilingDetailsWorkspace
                {
                    ArtifactIdWorkspaceEDDS = row.Field<int>("WID"),
                    ArtifactIdMatter = row.Field<int>("MID"),
                    DateKey = row["DateKey"].ToString(),
                    HostingReview = row.Field<decimal>("RVWH"),
                    HostingRepository = row.Field<decimal>("RPYH"),
                    ProcessingReview = row.Field<decimal>("RVWP"),
                    ProcessingRepository = row.Field<decimal>("RPYP"),
                    TranslationUnits = row.Field<int>("TU"),
                    PageCountUnits = row.Field<int>("PU"),
                    AirPriviilegeUnits = row.Field<int>("APU"),
                    AirReviewUnits = row.Field<int>("ARU"),
                    ColdStorage = row.Field<decimal>("CS")
                });
            }
            return tempBillingDetailsWorkspace;
        }

        public Task InsertIntoBillingDetailsWorkspace(string sql)
        {
            return Task.Run(() => BillingDbContext.ExecuteNonQuerySQLStatement(sql));
        }

        public List<BillingReportUsers> GetUsersForCase(int caseArtifactId, int MatterArtifactId)
        {
            string sql = $@"
                SELECT DISTINCT
                    gcg.[CaseArtifactID],
                    u.ArtifactID AS UserID,                    
                    g.ArtifactID AS GroupID
                FROM EDDS.eddsdbo.[GroupCaseGroup] gcg    
                JOIN EDDS.eddsdbo.[Group] g WITH (NOLOCK) ON gcg.GroupArtifactID = g.ArtifactID
                JOIN EDDS.eddsdbo.[GroupUser] gu WITH (NOLOCK) ON gu.GroupArtifactID = g.ArtifactID
                JOIN EDDS.eddsdbo.[User] u WITH (NOLOCK) ON u.ArtifactID = gu.UserArtifactID    
                JOIN EDDS.eddsdbo.Artifact a WITH (NOLOCK) ON a.ArtifactID = u.ArtifactID
                WHERE 
                    a.Keywords NOT IN ('Do Not Bill')
                    AND u.EmailAddress NOT LIKE '%previewUser%' 
                    AND u.EmailAddress NOT LIKE '%relativity.com%' 
                    AND u.EmailAddress NOT LIKE '%kcura.com%'                    
                    AND gcg.CaseArtifactID = {caseArtifactId}";

            var users = new List<BillingReportUsers>();

            using (var reader = EddsDbContext.ExecuteSQLStatementAsReader(sql))
            {
                while (reader.Read())
                {
                    users.Add(new BillingReportUsers
                    {
                        WID = caseArtifactId,
                        MID = MatterArtifactId,
                        UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                        DateKey = DateTime.Now.ToString("yyyyMM"),
                        GroupID = reader.GetInt32(reader.GetOrdinal("GroupID"))
                    });
                }
            }

            return users;
        }

        public Task InsertIntoBillingUserInfo(List<BillingReportUsers> users)
        {
            if (!users.Any())
            {
                return Task.CompletedTask;
            }

            try
            {
                var insertValues = string.Join(",", users.Select(u =>
                    $"({u.WID}, {u.MID}, {u.UserID}, '{u.DateKey.Replace("'", "''")}', {u.GroupID})"));

                string insertSql = @"
                INSERT INTO [EDDSDBO].[BillingDetailsTempUsers]
                ([WID], [MID], [UserID], [DateKey], [GroupID])
                VALUES " + insertValues;

                return Task.Run(() => BillingDbContext.ExecuteNonQuerySQLStatement(insertSql));
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to insert billing user info", ex);
            }
        }
          
        public async Task<int> DeleteBillingDetailsCurrentDateKey(string dateKey)
        {
            string sql = @"
               DELETE FROM [EDDSDBO].[BillingDetails] 
               WHERE YearMonth = @dateKey;
               SELECT @@ROWCOUNT;";

            var parameter = new SqlParameter("@dateKey", dateKey);
            return await Task.Run(() => BillingDbContext.ExecuteSqlStatementAsScalar<int>(sql, parameter));
        }

        public Task TruncateBillingDetailsTempWorkspace()
        {
            string sql = "TRUNCATE TABLE [EDDSDBO].[BillingDetailsTempWorkspace]";
            return Task.Run(() => BillingDbContext.ExecuteNonQuerySQLStatement(sql));
        }

        public Task TruncateBillingDetailsTempUsers()
        {
            string sql = "TRUNCATE TABLE [EDDSDBO].[BillingDetailsTempUsers]";
            return Task.Run(() => BillingDbContext.ExecuteNonQuerySQLStatement(sql));
        }

        public int GetWorkspaceImageCount(DateTime startDate, DateTime endDate, int workspaceArtifactId)
        {
            var dbContext = _helper.GetDBContext(workspaceArtifactId);

            string columnCheckSql = "SELECT CASE WHEN COL_LENGTH('eddsdbo.document', 'ProcessingFileId') IS NOT NULL THEN 1 ELSE 0 END";
            bool columnExists = dbContext.ExecuteSqlStatementAsScalar<int>(columnCheckSql) == 1;

            if (!columnExists)
            {
                return 0;
            }
            else
            {
                string sql = $@"
                IF COL_LENGTH('eddsdbo.document', 'ProcessingFileId') IS NULL
                    SELECT 0
                ELSE
                    SELECT
                        ISNULL((
                            SELECT
                                SUM(PageCount.RelativityImageCount)
                            FROM
                            (
                                SELECT
                                    d.ArtifactID, d.RelativityImageCount    
                                FROM eddsdbo.document d WITH (NOLOCK)
                                JOIN
                                (
                                    SELECT DISTINCT ar.ArtifactID
                                    FROM eddsdbo.AuditRecord ar WITH (NOLOCK)
                                    WHERE
                                        ar.[Action] = 13
                                    AND ar.[TimeStamp] BETWEEN CONVERT(DATETIME,'{startDate:MM/dd/yyyy}') AND CONVERT(DATETIME,'{endDate:MM/dd/yyyy}')
                                ) ar
                                ON ar.ArtifactID = d.ArtifactID
                                WHERE 
                                    d.RelativityImageCount IS NOT NULL
                                AND d.ProcessingFileId IS NOT NULL
                            ) PageCount
                        ), 0)";

                return dbContext.ExecuteSqlStatementAsScalar<int>(sql);
            }
        }

        public List<BillingSummaryMetrics> BillingSummaryMetrics()
        {
            var billingSummaryMetrics = new List<BillingSummaryMetrics>();
            string sql = $@"SELECT
                                MID, DateKey, SUM(RVWH) 'Matter_RVWH', SUM(RPYH) 'Matter_RPYH', SUM(RVWP) 'Matter_RVWP', SUM(RPYP) 'Matter_RPYP', SUM(TU) 'Matter_TU', SUM(PU) 'Matter_PU', SUM(APU) 'Matter_APU', SUM(ARU) 'Matter_ARU', SUM(CS) 'Matter_CS'
                            FROM eddsdbo.BillingDetailsTempWorkspace
                            GROUP BY MID, DateKey";

            var dt = BillingDbContext.ExecuteSqlStatementAsDataTable(sql);

            foreach (DataRow row in dt.Rows)
            {
                billingSummaryMetrics.Add(new Models.BillingSummaryMetrics
                {
                    MatterArtifactId = row.Field<int>("MID"),
                    DateKey = row["DateKey"]?.ToString(),
                    SumHostingReview = row.Field<decimal>("Matter_RVWH"),
                    SumHostingRepository = row.Field<decimal>("Matter_RPYH"),
                    SumProcessingReview = row.Field<decimal>("Matter_RVWP"),
                    SumProcessingRepository = row.Field<decimal>("Matter_RPYP"),
                    SumColdStorage = row.Field<decimal>("Matter_CS"),
                    SumTranslationUnits = row.Field<int>("Matter_TU"),
                    SumPageCountUnits = row.Field<int>("Matter_PU"),
                    SumAirPriviilegeUnits = row.Field<int>("Matter_APU"),
                    SumAirReviewUnits = row.Field<int>("Matter_ARU")
                });
            }
            return billingSummaryMetrics;
        }

        public List<BillingSummaryWorkspaces> BillingSummaryWorkspaces()
        {
            var billingSummaryWorkspacs = new List<BillingSummaryWorkspaces>();
            string sql = "SELECT MID, DateKey, WID FROM eddsdbo.BillingDetailsTempWorkspace";

            var dt = BillingDbContext.ExecuteSqlStatementAsDataTable(sql);

            foreach (DataRow row in dt.Rows)
            {
                billingSummaryWorkspacs.Add(new Models.BillingSummaryWorkspaces
                {
                    MatterArtifactId = row.Field<int>("MID"),
                    DateKey = row["DateKey"]?.ToString(),
                    WorkspaceEddsArtifactId = row.Field<int>("WID")
                });
            }
            return billingSummaryWorkspacs;
        }

        public List<BillingSummaryUsers> BillingSummaryUsers()
        {
            var billingSummaryUsers = new List<BillingSummaryUsers>();
            string sql = "SELECT DISTINCT bdtu.MID, bdtu.DateKey, br.ArtifactID FROM eddsdbo.BillingDetailsTempUsers bdtu JOIN eddsdbo.BillingRecipients br ON br.EDDSUserArtifactId = bdtu.UserID GROUP BY bdtu.MID, bdtu.DateKey, br.ArtifactID";

            var dt = BillingDbContext.ExecuteSqlStatementAsDataTable(sql);

            foreach (DataRow row in dt.Rows)
            {
                billingSummaryUsers.Add(new Models.BillingSummaryUsers 
                {
                    MatterArtifactId = row.Field<int>("MID"),
                    DateKey = row["DateKey"]?.ToString(),
                    UserArtifactId = row.Field<int>("ArtifactID")
                });
            }
            return billingSummaryUsers;
        }

        public List<BillingDetails> BillingDetails() 
        {
            var billingDetails = new List<BillingDetails>();
            string sql = "SELECT a.ParentArtifactID 'MatterArtifactID', bd.ArtifactID, bd.YearMonth FROM eddsdbo.billingdetails bd JOIN eddsdbo.artifact a ON bd.ArtifactID = a.ArtifactID";

            var dt = BillingDbContext.ExecuteSqlStatementAsDataTable(sql);

            foreach (DataRow row in dt.Rows)
            {
                billingDetails.Add(new Models.BillingDetails 
                {
                    MatterArtifactId = row.Field<int>("MatterArtifactID"),
                    DateKey = row["YearMonth"]?.ToString(),
                    BillingDetailsArtifactId = row.Field<int>("ArtifactID")
                });
            }
            return billingDetails;
        }

        public List<BillingOverrides> BillingOverrides() 
        {
            var billingOverrides = new List<BillingOverrides>();
            string sql = "SELECT ArtifactID, RP_ActiveHosting, RP_RepositoryHosting, RP_ReviewProcessing, RP_RepositoryProcessing, RP_Imaging, RP_Translations, RP_Users, RP_ColdStorage, RP_AirForReview, RP_AirForPrivilege " +
                         "FROM eddsdbo.Matter " +
                         "WHERE " +
                          "     RP_ActiveHosting IS NOT NULL " +
                          "OR   RP_RepositoryHosting IS NOT NULL " +
                          "OR   RP_ReviewProcessing IS NOT NULL " +
                          "OR   RP_RepositoryProcessing IS NOT NULL " +
                          "OR   RP_Imaging IS NOT NULL " +
                          "OR   RP_Translations IS NOT NULL " +
                          "OR   RP_Users IS NOT NULL " +
                          "OR   RP_ColdStorage IS NOT NULL " +
                          "OR   RP_AirForReview IS NOT NULL " +
                          "OR   RP_AirForPrivilege IS NOT NULL ";


            var dt = BillingDbContext.ExecuteSqlStatementAsDataTable(sql);

            foreach (DataRow row in dt.Rows)
            {
                billingOverrides.Add(new Models.BillingOverrides
                {
                    MatterArtifactsId = row.Field<int>("ArtifactId"),
                    RVWH_O = row.Field<decimal?>("RP_ActiveHosting"),
                    RPYH_O = row.Field<decimal?>("RP_RepositoryHosting"),
                    RVWP_O = row.Field<decimal?>("RP_ReviewProcessing"),
                    RPYP_O = row.Field<decimal?>("RP_RepositoryProcessing"),
                    TU_O = row.Field<decimal?>("RP_Translations"),
                    PU_O = row.Field<decimal?>("RP_Imaging"),
                    U_O = row.Field<decimal?>("RP_Users"),
                    CS_O = row.Field<decimal?>("RP_ColdStorage"),
                    ARU_O = row.Field<decimal?>("RP_AirForReview"),
                    APU_O = row.Field<decimal?>("RP_AirForPrivilege")
                });
            }
            return billingOverrides;
        }

        public async Task<int> GetRemainingRecordsCount(string dateKey)
        {
            string sql = @"
        SELECT COUNT(*) 
        FROM [EDDSDBO].[BillingDetails]
        WHERE YearMonth = @dateKey";

            var parameter = new SqlParameter("@dateKey", dateKey);
            return await Task.Run(() => BillingDbContext.ExecuteSqlStatementAsScalar<int>(sql, parameter));
        }
    }
}