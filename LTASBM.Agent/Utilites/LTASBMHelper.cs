using kCura.Agent.CustomAttributes;
using kCura.Vendor.Castle.Core.Internal;
using Relativity.API;
using Relativity.Services.Objects;
using Relativity.Services.Objects.DataContracts;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using static kCura.Vendor.Castle.MicroKernel.ModelBuilder.Descriptors.InterceptorDescriptor;


namespace LTASBM.Agent.Utilites
{
    internal class LTASBMHelper
    {
        private readonly IHelper _helper;
        private readonly IAPILog _logger;

        //Client object Type and Field GUIDs
        public Guid ClientObjectType { get; } = new Guid("628EC03F-E789-40AF-AA13-351F92FFA44D");
        public Guid ClientNumberField { get; } = new Guid("C3F4236F-B59B-48B5-99C8-3678AA5EEA72");
        public Guid ClientNameField { get; } = new Guid("E704BF08-C187-4EAB-9A25-51C17AA98FB9");
        public Guid ClientEDDSArtifactIdField { get; } = new Guid("1A30F07F-1E5C-4177-BB43-257EF7588660");

        //Matter object Type and Field GUIDs
        public Guid MatterObjectType { get; } = new Guid("18DA4321-AAFB-4B24-99E9-13F90090BF1B");
        public Guid MatterNumberField { get; } = new Guid("3A8B7AC8-0393-4C48-9F58-C60980AE8107");
        public Guid MatterNameField { get; } = new Guid("C375AA14-D5CD-484C-91D5-35B21826AD14");
        public Guid MatterEddsArtifactIdField { get; } = new Guid("8F134CD2-4DB1-48E4-8479-2F7E7B18CF9F");
        public Guid MatterGUIDField { get; } = new Guid("4E41FF7F-9D1C-4502-96D9-DFBB9252B3E6");        
        public Guid MatterClientObjectField { get; } = new Guid("0DD5C18A-35F8-4CF1-A00B-7814FA3A5788");

        //Workspace Object Type and Field GUIDs

        public Guid WorkspaceObjectType { get; } = new Guid("27AE803F-590D-4C97-9CFD-F1B9E21690EF");
        public Guid WorkspaceArtifactIDField { get; } = new Guid("0315B4C2-82B1-4AFE-B1DB-05D1BA348B6D");
        public Guid WorkspaceCreatedByField { get; } = new Guid("69B6424F-0589-4080-875F-85193D3064D0");
        public Guid WorkspaceCreatedOnField { get; } = new Guid("305A1D9E-A8D0-4EEC-A76D-FDF6839C712B");
        public Guid WorkspaceNameField { get; } = new Guid("8CECDA63-0D55-45E4-8965-0F5F6B6A5C73");
        public Guid WorkspaceMatterNumberField { get; } = new Guid("FC3B7349-C34E-444F-A94B-B6D518E508BD");
        public Guid WorkspaceClientNumberField { get; } = new Guid("32452C8D-2AE1-4276-A1D6-BF7B8458506D");
        public Guid WorkspaceCaseTeamField { get; } = new Guid("0E3A3210-7083-4677-B851-B5FFB96BC618");
        public Guid WorkspaceLtasAnalystField { get; } = new Guid("B5EFD6A5-010B-41CE-B233-6CC517AA86EE");
        public Guid WorkspaceStatusField { get; } = new Guid("4506039A-78A1-49DB-9B09-976D407E14F7");

        public IHelper Helper => _helper; //public property to access helper when needed
        public LTASBMHelper(IHelper helper, IAPILog logger)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public async Task<int> LookupClientArtifactID(IObjectManager objectManager, int workspaceArtifactId, string clientNumberValue) 
        {
            try
            {
                var queryRequest = new QueryRequest
                {
                    ObjectType = new ObjectTypeRef
                    {
                        Guid = ClientObjectType  
                    },
                    Fields = new FieldRef[]
                    {
                    new FieldRef{ Name = "ArtifactID" }
                    },
                    Condition = $"'Client Number' == '{clientNumberValue?.Trim()}'"
                };

                var result = await objectManager.QueryAsync(workspaceArtifactId, queryRequest, 0, 1);
                return result.Objects[0].ArtifactID;
            }
            catch (Exception ex)
            {
                string methodName = nameof(LookupClientArtifactID);
                string errorMessage = ex.InnerException != null
                    ? $"Method: {methodName} ---Value:{clientNumberValue} {ex.InnerException.Message}---{ex.StackTrace}"
                    : $"Method: {methodName} ---Value:{clientNumberValue} {ex.Message}---{ex.StackTrace}";

                _logger.ForContext<LTASBMHelper>()
                       .LogError($"Error in {methodName}: {errorMessage}");
                return 0;
            }
        }

        public int GetWorkspaceArtifactID(IDBContext dBContext)
        {
            try
            {
                string sql = @"SELECT ArtifactID FROM eddsdbo.Artifact WITH (NOLOCK) WHERE ArtifactTypeID = 8";
                return (int)dBContext.ExecuteSqlStatementAsScalar(sql);
            }
            catch (Exception ex)
            {
                string methodName = nameof(GetWorkspaceArtifactID);
                string errorMessage = ex.InnerException != null
               ? $"Method: {methodName} {ex.InnerException.Message}---{ex.StackTrace}"
               : $"Method: {methodName} {ex.Message}---{ex.StackTrace}";

                _logger.ForContext<LTASBMHelper>()
                       .LogError($"Error in {methodName}: {errorMessage}");
                return 0;
            }
        }

        public DataTable GetHyperlinkValues(IDBContext dBContext, string guidStringValue) 
        {
            try
            {
                string sql = @"SELECT
                            ot.[Name], ot.DescriptorArtifactTypeID 'ArtifactTypeID', v.ArtifactID 'ViewArtifactID', t.ArtifactID 'TabArtifactID'
                        FROM eddsdbo.ArtifactGuid ag WITH (NOLOCK)
                        JOIN eddsdbo.Artifact a WITH (NOLOCK)
                            ON ag.ArtifactID = a.ArtifactID
                        JOIN eddsdbo.ObjectType ot
                            ON ot.ArtifactID = ag.ArtifactID
                        JOIN eddsdbo.[view] v WITH (NOLOCK)
                            ON v.ArtifactTypeID = ot.DescriptorArtifactTypeID
                        JOIN eddsdbo.Tab t WITH (NOLOCK)
                            ON t.ObjectArtifactTypeID = ot.DescriptorArtifactTypeID
                        WHERE ag.ArtifactGuid IN('@guidValue')
	                        AND v.AvailableInObjectTab = 1;";
                var parameter = new SqlParameter("@guidValue", guidStringValue);
                return dBContext.ExecuteSqlStatementAsDataTable(sql, new[] { parameter });
            }
            catch (Exception ex)
            {
                string methodName = nameof(GetHyperlinkValues);
                string errorMessage = ex.InnerException != null
                    ? $"Method: {methodName} ---Value:{guidStringValue} {ex.InnerException.Message}---{ex.StackTrace}"
                    : $"Method: {methodName} ---Value:{guidStringValue} {ex.Message}---{ex.StackTrace}";

                _logger.ForContext<LTASBMHelper>()
                       .LogError($"Error in {methodName}: {errorMessage}");
                return new DataTable();
            }
        }

        public int GetCaseStatusArtifactID(IDBContext dBContext, string statusValue)
        {
            try
            {
                string sql = @"SELECT TOP 1 CodeTypeID FROM eddsdbo.CodeType WHERE DisplayName LIKE 'Case%Status';";
                var codeTypeId = dBContext.ExecuteSqlStatementAsScalar(sql);

                string artifactSql = @"SELECT ArtifactID FROM eddsdbo.ExtendedCode WHERE CodeTypeID = @codeTypeId AND [Name] LIKE @statusValue;";
                var parameter = new[] 
                { 
                    new SqlParameter("@codeTypeId", codeTypeId),
                    new SqlParameter("@statusValue", statusValue)
                };
                return (int)dBContext.ExecuteSqlStatementAsScalar(artifactSql, parameter);
            }
            catch (Exception ex) 
            {
                string methodName = nameof(GetCaseStatusArtifactID);
                string errorMessage = ex.InnerException != null
                    ? $"Method: {methodName} ---Value:{statusValue} {ex.InnerException.Message}---{ex.StackTrace}"
                    : $"Method: {methodName} ---Value:{statusValue} {ex.Message}---{ex.StackTrace}";

                _logger.ForContext<LTASBMHelper>()
                       .LogError($"Error in {methodName}: {errorMessage}");
                return 0;
            }
        }
    }
}
