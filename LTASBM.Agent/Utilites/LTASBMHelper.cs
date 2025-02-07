using LTASBM.Agent.Handlers;
using LTASBM.Agent.Models;
using Relativity.API;
using Relativity.Services.Objects;
using Relativity.Services.Objects.DataContracts;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace LTASBM.Agent.Utilites
{
    public enum CostCode
    {
        RVWH3230 = 3230,
        RVWH3231 = 3231,
        RVWH3232 = 3232,
        RPYH3220 = 3220,
        RPYH3221 = 3221,
        RPYH3222 = 3222,
        RVWP3210 = 3210,
        RVWP3211 = 3211,
        RVWP3212 = 3212,
        RPYP3240 = 3240,
        RPYP3241 = 3241,
        RPYP3242 = 3242,
        TU3270 = 3270,
        PU3203 = 3203,
        CS3205 = 3205,
        UU3200 = 3200,
        ARU3201 = 3201,
        APU3202 = 3202
    }

    public class CostCodeInfo
    {
        public CostCode Code { get; set; }
        public string Description { get; set; }        
        public Dictionary<string, decimal> StandardRateByEnv { get; set; }
        public Guid QuantityFieldGuid { get; set; }
        public Guid OverrideFieldGuid { get; set; }

        public decimal GetRateForEnvironment(string env) 
        {
            if(StandardRateByEnv.TryGetValue(env.ToUpperInvariant(), out decimal rate)) 
                return rate;

            throw new InvalidOperationException($"No rate defined for environment:{env}");
        }
    }

    internal class LTASBMCostCodeHelper
    {
        private readonly LTASBMHelper _ltasHelper;
        private readonly Dictionary<CostCode, CostCodeInfo> CostCodeDetails;

        public LTASBMCostCodeHelper(LTASBMHelper ltasHelper)
        {
            _ltasHelper = ltasHelper ?? throw new ArgumentNullException(nameof(ltasHelper));

            CostCodeDetails = new Dictionary<CostCode, CostCodeInfo>
            {
                {
                    CostCode.RVWH3230,
                    new CostCodeInfo
                    {
                        Code = CostCode.RVWH3230,
                        Description = "Review Hosting",
                        StandardRateByEnv = new Dictionary<string, decimal>
                        {
                            {"US", 12.00m },
                            {"EU", 17.12m },
                            {"AU", 10.77m }
                        }, 
                        QuantityFieldGuid = _ltasHelper.DetailsRVWH3230,
                        OverrideFieldGuid = _ltasHelper.O_ReviewHostingField            
                    }
                },
                { 
                    CostCode.RVWH3231,
                    new CostCodeInfo
                    {
                        Code = CostCode.RVWH3231,
                        Description = "Review Hosting",
                        StandardRateByEnv = new Dictionary<string, decimal>
                        {
                            {"US", 10.00m },
                            {"EU", 14.27m },
                            {"AU", 8.97m }
                        },
                        QuantityFieldGuid = _ltasHelper.DetailsRVWH3231,
                        OverrideFieldGuid = _ltasHelper.O_ReviewHostingField
                    }
                },
                {
                    CostCode.RVWH3232,
                    new CostCodeInfo
                    {
                        Code = CostCode.RVWH3232,
                        Description = "Review Hosting",
                        StandardRateByEnv = new Dictionary < string, decimal > 
                        { 
                            { "US", 8.00m }, 
                            { "EU", 11.41m }, 
                            { "AU", 7.18m } 
                        },
                        QuantityFieldGuid = _ltasHelper.DetailsRVWH3232,
                        OverrideFieldGuid = _ltasHelper.O_ReviewHostingField
                    }
                },
                {
                    CostCode.RPYH3220,
                    new CostCodeInfo
                    {
                        Code = CostCode.RPYH3220,
                        Description = "Repository Hosting",
                         StandardRateByEnv = new Dictionary<string, decimal>
                        {
                            {"US", 5.00m },
                            {"EU", 7.13m },
                            {"AU", 4.49m }
                        },
                        QuantityFieldGuid = _ltasHelper.DetailsRPYH3220,
                        OverrideFieldGuid = _ltasHelper.O_RepositoryHostingField
                    }
                },
                {
                    CostCode.RPYH3221,
                    new CostCodeInfo
                    {
                        Code = CostCode.RPYH3221,
                        Description = "Repository Hosting (Per GB)",
                         StandardRateByEnv = new Dictionary<string, decimal>
                        {
                            {"US", 4.00m },
                            {"EU", 5.71m },
                            {"AU", 3.59m }
                        },
                        QuantityFieldGuid = _ltasHelper.DetailsRPYH3221,
                        OverrideFieldGuid = _ltasHelper.O_RepositoryHostingField
                    }
                },
                {
                    CostCode.RPYH3222,
                    new CostCodeInfo
                    {
                        Code = CostCode.RPYH3222,
                        Description = "Repository Hosting (Per GB)",
                        StandardRateByEnv = new Dictionary<string, decimal>
                        {
                            {"US", 3.00m },
                            {"EU", 4.28m },
                            {"AU", 2.69m }
                        },
                        QuantityFieldGuid = _ltasHelper.DetailsRPYH3222,
                        OverrideFieldGuid = _ltasHelper.O_RepositoryHostingField
                    }
                },
                {
                    CostCode.RVWP3210,
                    new CostCodeInfo
                    {
                        Code = CostCode.RVWP3210,
                        Description = "Review Processing (Per GB)",
                        StandardRateByEnv = new Dictionary<string, decimal>
                        {
                            {"US", 95.00m },
                            {"EU", 135.52m },
                            {"AU", 85.22m }
                        },
                        QuantityFieldGuid = _ltasHelper.DetailsRVWP3210,
                        OverrideFieldGuid = _ltasHelper.O_ReviewProcessingField
                    }
                },
                {
                    CostCode.RVWP3211,
                    new CostCodeInfo
                    {
                        Code = CostCode.RVWP3211,
                        Description = "Review Processing (Per GB)",
                        StandardRateByEnv = new Dictionary<string, decimal>
                        {
                            {"US", 70.00m },
                            {"EU", 99.86m },
                            {"AU", 62.80m }
                        },
                        QuantityFieldGuid = _ltasHelper.DetailsRVWP3211,
                        OverrideFieldGuid = _ltasHelper.O_ReviewProcessingField
                    }
                },
                {
                    CostCode.RVWP3212,
                    new CostCodeInfo
                    {
                        Code = CostCode.RVWP3212,
                        Description = "Review Processing (Per GB)",
                        StandardRateByEnv = new Dictionary<string, decimal>
                        {
                            {"US", 45.00m },
                            {"EU", 64.19m },
                            {"AU", 40.37m }
                        },
                        QuantityFieldGuid = _ltasHelper.DetailsRVWP3212,
                        OverrideFieldGuid = _ltasHelper.O_ReviewProcessingField
                    }
                },
                {
                    CostCode.RPYP3240,
                    new CostCodeInfo
                    {
                        Code = CostCode.RPYP3240,
                        Description = "Repository Processing (Per GB)",
                        StandardRateByEnv = new Dictionary<string, decimal>
                        {
                            {"US", 25.00m },
                            {"EU", 35.66m },
                            {"AU", 22.43m }
                        },
                        QuantityFieldGuid = _ltasHelper.DetailsRPYP3240,
                        OverrideFieldGuid = _ltasHelper.O_RepositoryProcessingField
                    }
                },
                {
                    CostCode.RPYP3241,
                    new CostCodeInfo
                    {
                        Code = CostCode.RPYP3241,
                        Description = "Repository Processing (Per GB)",
                        StandardRateByEnv = new Dictionary<string, decimal>
                        {
                            {"US", 20.00m },
                            {"EU", 28.53m },
                            {"AU", 17.94m }
                        },
                        QuantityFieldGuid = _ltasHelper.DetailsRPYP3241,
                        OverrideFieldGuid = _ltasHelper.O_RepositoryProcessingField
                    }
                },
                {
                    CostCode.RPYP3242,
                    new CostCodeInfo
                    {
                        Code = CostCode.RPYP3242,
                        Description = "Repository Processing (Per GB)",
                        StandardRateByEnv = new Dictionary<string, decimal>
                        {
                            {"US", 15.00m },
                            {"EU", 21.40m },
                            {"AU", 13.46m }
                        },
                        QuantityFieldGuid = _ltasHelper.DetailsRPYP3242,
                        OverrideFieldGuid = _ltasHelper.O_RepositoryProcessingField
                    }
                },
                {
                    CostCode.ARU3201,
                    new CostCodeInfo
                    {
                        Code = CostCode.ARU3201,
                        Description = "Air For Review (per document unit)",
                        StandardRateByEnv = new Dictionary<string, decimal>
                        {
                            {"US", 1.00m },
                            {"EU", 1.00m },
                            {"AU", 1.00m }
                        },
                        QuantityFieldGuid = _ltasHelper.DetailsARU3201,
                        OverrideFieldGuid = _ltasHelper.O_AirForReviewField
                    }
                },
                {
                    CostCode.APU3202,
                    new CostCodeInfo
                    {
                        Code = CostCode.APU3202,
                        Description = "Air For Privilege (per document unit)",
                        StandardRateByEnv = new Dictionary<string, decimal>
                        {
                            {"US", 1.00m },
                            {"EU", 1.00m },
                            {"AU", 1.00m }
                        },
                        QuantityFieldGuid = _ltasHelper.DetailsAPU3202,
                        OverrideFieldGuid = _ltasHelper.O_AirForPrivilegeField
                    }
                },
                {
                    CostCode.CS3205,
                    new CostCodeInfo
                    {
                        Code = CostCode.CS3205,
                        Description = "Cold Storage Hosting (Per GB)",
                        StandardRateByEnv = new Dictionary<string, decimal>
                        {
                            {"US", 2.00m },
                            {"EU", 2.85m },
                            {"AU", 1.79m }
                        },
                        QuantityFieldGuid = _ltasHelper.DetailsCS3205,
                        OverrideFieldGuid = _ltasHelper.O_ColdStorageField
                    }
                },
                {
                    CostCode.PU3203,
                    new CostCodeInfo
                    {
                        Code = CostCode.PU3203,
                        Description = "TIFF (per page)",
                        StandardRateByEnv = new Dictionary<string, decimal>
                        {
                            {"US", 0.02m },
                            {"EU", 0.03m },
                            {"AU", 0.02m }
                        },
                        QuantityFieldGuid = _ltasHelper.DetailsPU3203,
                        OverrideFieldGuid = _ltasHelper.O_ImageField
                    }
                },
                {
                    CostCode.TU3270,
                    new CostCodeInfo
                    {
                        Code = CostCode.TU3270,
                        Description = "Machine Translation (per document unit)",
                        StandardRateByEnv = new Dictionary<string, decimal>
                        {
                            {"US", 0.75m },
                            {"EU", 1.09m },
                            {"AU", 0.75m }
                        },
                        QuantityFieldGuid = _ltasHelper.DetailsTU3270,
                        OverrideFieldGuid = _ltasHelper.O_TranslationsField
                    }
                },
                {
                    CostCode.UU3200,
                    new CostCodeInfo
                    {
                        Code = CostCode.UU3200,
                        Description = "RelOne User Fee",
                        StandardRateByEnv = new Dictionary<string, decimal>
                        {
                            {"US", 100.00m },
                            {"EU", 142.65m },
                            {"AU", 89.71m }
                        },
                        QuantityFieldGuid = _ltasHelper.DetailsUU3200,
                        OverrideFieldGuid = _ltasHelper.O_UsersField
                    }
                }
            };
        }

        public FieldRef[] GetQueryFields()
        {
            var fieldRefs = new List<FieldRef>
            {
                new FieldRef { Name = "ArtifactID" }
            };

            foreach (var costCode in CostCodeDetails.Values)
            {
                fieldRefs.Add(new FieldRef { Guid = costCode.QuantityFieldGuid });
            }

            return fieldRefs.ToArray();
        }

        public CostCodeInfo GetCostCodeInfo(Guid fieldGuid)
        {
            return CostCodeDetails.Values
                .FirstOrDefault(info => info.QuantityFieldGuid == fieldGuid);
        }

        public bool IsCostCodeField(Guid fieldGuid)
        {
            return CostCodeDetails.Values
                .Any(info => info.QuantityFieldGuid == fieldGuid);
        }

        public IEnumerable<CostCodeInfo> GetAllCostCodes()
        {
            return CostCodeDetails.Values;
        }
    }

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
        public Guid MatterMainContact { get; } = new Guid("3F59C301-126B-4A32-8B13-E4D6BB6D444A");
        public Guid MatterEmailTo { get; } = new Guid("5A1654F3-A3B0-49F0-A7AA-836D46C76155");
        public Guid MatterEmailCC { get; } = new Guid("FD7F7F22-3F03-4CB5-B0C8-BF2DC74D5334");
        public Guid O_ReviewHostingField { get; } = new Guid("7859D3A1-CF32-472D-98C5-82ECFF923717");
        public Guid O_RepositoryHostingField { get; } = new Guid("D1F23EAF-DB2C-45E3-82AB-441517A8E223");
        public Guid O_ReviewProcessingField { get; } = new Guid("C1CD87BF-17DF-4B2B-BE1A-9F7D35EB9858");
        public Guid O_RepositoryProcessingField { get; } = new Guid("CDDA91F8-CBD9-4B63-8104-05CBB160F3CC");
        public Guid O_AirForReviewField { get; } = new Guid("589F5968-1F84-408A-9D7C-1D5CEBC5E26C");
        public Guid O_AirForPrivilegeField { get; } = new Guid("056CA254-A9BB-4F5C-85AC-27E198F352C4");
        public Guid O_ColdStorageField { get; } = new Guid("CF4EFDC3-0306-4BD8-AE71-F61C28AC5FD8");
        public Guid O_ImageField { get; } = new Guid("D571BD0E-83EB-4757-9C7E-3CB93FF1073E");
        public Guid O_TranslationsField { get; } = new Guid("96F1F66F-13A8-4D0B-AB02-D4490D8BFB82");  
        public Guid O_UsersField { get; } = new Guid("F31CC726-7BD8-4F02-A7CF-1BD7995C4831");        


        //Workspace Object Type and Field GUIDs
        public Guid WorkspaceObjectType { get; } = new Guid("27AE803F-590D-4C97-9CFD-F1B9E21690EF");
        public Guid WorkspaceEDDSArtifactIDField { get; } = new Guid("7625A536-4262-443E-B6ED-C0E25DB2A6C4");
        public Guid WorkspaceCreatedByField { get; } = new Guid("69B6424F-0589-4080-875F-85193D3064D0");
        public Guid WorkspaceCreatedOnField { get; } = new Guid("305A1D9E-A8D0-4EEC-A76D-FDF6839C712B");
        public Guid WorkspaceNameField { get; } = new Guid("0315B4C2-82B1-4AFE-B1DB-05D1BA348B6D");
        public Guid WorkspaceMatterNumberField { get; } = new Guid("FC3B7349-C34E-444F-A94B-B6D518E508BD");
        public Guid WorkspaceCaseTeamField { get; } = new Guid("0E3A3210-7083-4677-B851-B5FFB96BC618");
        public Guid WorkspaceLtasAnalystField { get; } = new Guid("B5EFD6A5-010B-41CE-B233-6CC517AA86EE");
        public Guid WorkspaceStatusField { get; } = new Guid("4506039A-78A1-49DB-9B09-976D407E14F7");

        //Billing Recipient
        public Guid UserObjectType { get; } = new Guid("4DFCF305-41FB-4CB6-8E88-521E928F0DA7");
        public Guid UserFirstNameField { get; } = new Guid("C3CA7F99-0974-4E6C-8338-E47B57570FAC");
        public Guid UserLastNameField { get; } = new Guid("B87ECC86-3492-422C-9C13-0F83316C6DA3");
        public Guid UserEmailAddressField { get; } = new Guid("23576B7F-738F-4F55-AC2D-AD4B3C26BCB4");
        public Guid UserEddsArtifactIdField { get; } = new Guid("AE481D85-0C01-4DAF-8224-672C9E524250");
        public Guid UserVisibleField { get; } = new Guid("6DF40CDA-8B30-47ED-9AAA-0B75F194B74E");

        //Billing Details
        public Guid DetailsObjectType { get; } = new Guid("2177440E-0C3A-4FAC-910B-BD1F44382133");
        public Guid DetailsYearMonth { get; } = new Guid("305DBA79-5EA0-4459-9783-415044F1AED9");
        public Guid DetailsRVWH3230 { get; } = new Guid("9A89B4FB-0DDD-4A8A-ADFF-4E4A436734BE");
        public Guid DetailsRVWH3231 { get; } = new Guid("3EC2EF61-9DBD-4A8E-9451-B39C76E9738E");
        public Guid DetailsRVWH3232 { get; } = new Guid("6E96BBC3-A867-4207-97F5-23ACBD65B426");
        public Guid DetailsRPYH3220 { get; } = new Guid("67E4A91E-B0A4-4CD5-A131-6F3AD7C61C42");
        public Guid DetailsRPYH3221 { get; } = new Guid("0C8930B2-55C1-4032-8393-640E34806A32");
        public Guid DetailsRPYH3222 { get; } = new Guid("0B9B0283-D801-456A-9BDD-AA5D507900D2");
        public Guid DetailsRVWP3210 { get; } = new Guid("B1D02C28-4CA2-4108-A6FE-9B96B4340C28");
        public Guid DetailsRVWP3211 { get; } = new Guid("6D0AB7B4-4F6F-4F1D-8FE1-E4BEA34D8ABD");
        public Guid DetailsRVWP3212 { get; } = new Guid("DEBAFDEF-144C-415B-A338-2C27007FE645");
        public Guid DetailsRPYP3240 { get; } = new Guid("815FFCD0-FB1F-44B5-B024-5E08A46D2CE9");
        public Guid DetailsRPYP3241 { get; } = new Guid("BB30889D-C6F9-46E5-8E8F-A16787892929");
        public Guid DetailsRPYP3242 { get; } = new Guid("9FED9580-21F2-4D31-A1FB-5014BA4BE19F");
        public Guid DetailsCS3205 { get; } = new Guid("CAC7A969-CAA6-4FF0-9765-529F6CD39DF0");
        public Guid DetailsCS3205Override { get; } = new Guid("B3220204-4CD7-461E-870F-C5303695FA5A");
        public Guid DetailsTU3270 { get; } = new Guid("A9CC85D8-200E-475E-8A39-FD18A5167379");
        public Guid DetailsTU3270Override { get; } = new Guid("6BB7C5A8-A428-4CDD-80A2-56CC853A2FDF");
        public Guid DetailsPU3203 { get; } = new Guid("50E86C9C-BD65-4EF9-9968-9A9E5DC0422B");
        public Guid DetailsPU3203Override { get; } = new Guid("CD947368-9E66-4119-8D44-B9B4E2271531");
        public Guid DetailsUU3200 { get; } = new Guid("4B4C47E4-63C2-40FF-AC1D-6B0B675EBD5A");
        public Guid DetailsUU3200Override { get; } = new Guid("3FF6C1DA-DA39-4184-8356-0868E16855CD");
        public Guid DetailsUsers { get; } = new Guid("67657A8C-BC57-45EA-A6A1-FCD2E1DD10CF");
        public Guid DetailsWorkspaceCount { get; } = new Guid("BDD06A4D-7737-4970-9638-A24F6132E2F5");
        public Guid DetailsWorkspaces { get; } = new Guid("933956CC-DF78-43F7-AC29-BCD435C7AC2D");
        public Guid DetailsAPU3202 { get; } = new Guid("A846DAB2-D8CB-4CEF-9488-D88C7DF42879");
        public Guid DetailsAPU3202Override { get; } = new Guid("5C4B9545-70B1-407F-B37F-31992DC89E21");
        public Guid DetailsARU3201 { get; } = new Guid("BDBD1856-9CC9-4626-BDE8-8582FB7047CA");
        public Guid DetailsARU3201Override { get; } = new Guid("3E4ECCF6-85C1-46CD-B7A4-958A3A4A48A3");

        public IHelper Helper => _helper;
        public IAPILog Logger => _logger;

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

        public async Task<int> LookupClientArtifactID(IObjectManager objectManager, int workspaceArtifactId, int clientEddsArtifactID)
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
                    Condition = $"(('EDDS Client ArtifactID' == {clientEddsArtifactID}))"
                };

                var result = await objectManager.QueryAsync(workspaceArtifactId, queryRequest, 0, 1);
                return result.Objects[0].ArtifactID;
            }
            catch (Exception ex)
            {
                string methodName = nameof(LookupClientArtifactID);
                string errorMessage = ex.InnerException != null
                    ? $"Method: {methodName} ---Value:{clientEddsArtifactID} {ex.InnerException.Message}---{ex.StackTrace}"
                    : $"Method: {methodName} ---Value:{clientEddsArtifactID} {ex.Message}---{ex.StackTrace}";

                _logger.ForContext<LTASBMHelper>()
                       .LogError($"Error in {methodName}: {errorMessage}");
                return 0;
            }
        }

        public async Task<int> LookupMatterArtifactID(IObjectManager objectManager, int workspaceArtifactId, string EddsMatterArtifactIdValue)
        {
            try
            {
                var queryRequest = new QueryRequest
                {
                    ObjectType = new ObjectTypeRef
                    {
                        Guid = MatterObjectType
                    },
                    Fields = new FieldRef[]
                    {
                    new FieldRef{ Name = "ArtifactID" }
                    },
                    Condition = $"'EDDS Matter ArtifactID' == '{EddsMatterArtifactIdValue?.Trim()}'"
                };

                var result = await objectManager.QueryAsync(workspaceArtifactId, queryRequest, 0, 1);
                return result.Objects[0].ArtifactID;
            }
            catch (Exception ex)
            {
                string methodName = nameof(LookupMatterArtifactID);
                string errorMessage = ex.InnerException != null
                    ? $"Method: {methodName} ---Value:{EddsMatterArtifactIdValue} {ex.InnerException.Message}---{ex.StackTrace}"
                    : $"Method: {methodName} ---Value:{EddsMatterArtifactIdValue} {ex.Message}---{ex.StackTrace}";

                _logger.ForContext<LTASBMHelper>()
                       .LogError($"Error in {methodName}: {errorMessage}");
                return 0;
            }
        }

        public string GetWorkspaceNameByBillingWorkspaceArtifactID(int billingWorkspaceArtifactId, List<BillingWorkspaces> billingWorkspaces)
        {
            return billingWorkspaces
                .Where(w => w.BillingWorkspaceArtifactId == billingWorkspaceArtifactId)
                .Select(w => w.BillingWorkspaceName)
                .FirstOrDefault() ?? "Uknown";
        }

        public int GetWorkspaceArtifactIdFromEddsArtifactId(int eddsArtifactId, List<BillingWorkspaces> billingWorkspaces)
        {
            return billingWorkspaces
                .Where(w => w.BillingWorkspaceEddsArtifactId == eddsArtifactId)
                .Select(w => w.BillingWorkspaceArtifactId)
                .FirstOrDefault();
        }

        public string GetMatterNameByEDDSMatterArtifactId(int matterArtifactId, List<EddsMatters> eddsMatters)
        {
            return eddsMatters
                .Where(m => m.EddsMatterArtifactId == matterArtifactId)
                .Select(m => m.EddsMatterName)
                .FirstOrDefault() ?? "Uknown";
        }

        public int GetMatterArifactIdByEddsMatterArtifactId(int eddsMatterArtifactId, List<BillingMatters> billingMatters)
        {
            return billingMatters
                .Where(w => w.BillingEddsMatterArtifactId == eddsMatterArtifactId)
                .Select(m => m.BillingMatterArtficatId)
                .DefaultIfEmpty(0)
                .First();
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

        public int GetBillingActiveStatusArtifactID(IDBContext dBContext, string statusValue)
        {
            try
            {
                string sql = @"SELECT TOP 1 CodeTypeID FROM eddsdbo.CodeType WHERE DisplayName LIKE 'Active%Billing';";
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

        public int GetBillingDetailsParentMatterArtifactID(IDBContext dBContext, int billingDetailsArtifactID)
        {
            try
            {
                string sql = @"SELECT 
                                    a.ParentArtifactID
                                FROM eddsdbo.BillingDetails bd 
                                JOIN eddsdbo.Artifact a 
                                    ON a.ArtifactID = bd.ArtifactID
                                WHERE bd.ArtifactID = @billingDetailsArtifactID";

                var parameter = new SqlParameter("@billingDetailsArtifactID", billingDetailsArtifactID);
                var result = dBContext.ExecuteSqlStatementAsScalar(sql, new[] { parameter });


                if (result == null || result == DBNull.Value)
                {
                    return 0;
                }

                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                _logger.ForContext<LTASBMHelper>().LogError(ex, $"Error getting (parent) matter artifactid for billing details artifactid: {billingDetailsArtifactID}");
                throw;
            }
        }

        public (int ArtifactTypeID, int ViewArtifactID, int TabArtifactID) GetHyperlinkValues(IDBContext dBContext, string guidStringValue)
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
                var result = dBContext.ExecuteSqlStatementAsDataTable(sql, new[] { parameter });

                if (result.Rows.Count == 1)
                {
                    return (
                        ArtifactTypeID: (int)result.Rows[0]["ArtifactTypeID"],
                        ViewArtifactID: (int)result.Rows[0]["ViewArtifactID"],
                        TabArtifactID: (int)result.Rows[0]["TabArtifactID"]
                          );
                }
                return (0, 0, 0);
            }
            catch (Exception ex)
            {
                _logger.ForContext<LTASBMHelper>()
               .LogError(ex,
                        "Error getting hyperlink values for GUID: {GuidValue}",
                        guidStringValue);
                throw;
            }
        }

        public (string FirstName, string LastName) GetUserName(IDBContext dBContext, string emailAddress)
        {
            try
            {
                string sql = @"SELECT u.FirstName, u.LastName FROM EDDS.eddsdbo.[User] u WITH (NOLOCK) WHERE u.EmailAddress LIKE @userEmail;";
                var parameter = new SqlParameter("@userEmail", emailAddress);
                var result = dBContext.ExecuteSqlStatementAsDataTable(sql, new[] { parameter });

                if (result.Rows.Count == 1)
                {
                    return (
                        FirstName: result.Rows[0]["FirstName"].ToString(),
                        LastName: result.Rows[0]["LastName"].ToString()
                    );
                }

                return (string.Empty, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.ForContext<LTASBMHelper>()
                .LogError(ex, "Error getting user name for email {EmailAddress}", emailAddress);
                throw;
            }
        }

        public Guid DetermineHostingReviewGuid(decimal sumHostingReview, BillingOverrides matterOverride)
        {
            if (matterOverride != null && matterOverride.RVWH_O.HasValue)
            {
                return DetailsRVWH3232;
            }

            if (sumHostingReview < 499.98M)
            {
                return DetailsRVWH3230;
            }
            else if (sumHostingReview < 999.99M)
            {
                return DetailsRVWH3231;
            }
            else
            {
                return DetailsRVWH3232;
            }
        }

        public Guid DetermineHostingRepositoryGuid(decimal sumHostingRepository, BillingOverrides matterOverride)
        {
            if (matterOverride != null && matterOverride.RPYH_O.HasValue)
            {
                return DetailsRPYH3222;
            }

            if (sumHostingRepository < 499.98M)
            {
                return DetailsRPYH3220;
            }
            else if (sumHostingRepository < 999.99M)
            {
                return DetailsRPYH3221;
            }
            else
            {
                return DetailsRPYH3222;
            }
        }

        public Guid DetermineProcessingReview(decimal sumProcessingReview, BillingOverrides matterOverride)
        {
            if (matterOverride != null && matterOverride.RVWP_O.HasValue)
            {
                return DetailsRVWP3212;
            }

            if (sumProcessingReview < 499.98M)
            {
                return DetailsRVWP3210;
            }
            else if (sumProcessingReview < 999.99M)
            {
                return DetailsRVWP3211;
            }
            else
            {
                return DetailsRVWP3212;
            }
        }

        public Guid DetermineProcesingRepository(decimal sumProcessingRepository, BillingOverrides matterOverride)
        {
            if (matterOverride != null && matterOverride.RPYP_O.HasValue)
            {
                return DetailsRPYP3242;
            }

            if (sumProcessingRepository < 499.98M)
            {
                return DetailsRPYP3240;
            }
            else if (sumProcessingRepository < 999.99M)
            {
                return DetailsRPYP3241;
            }
            else
            {
                return DetailsRPYP3242;
            }
        }
              
    }
}
