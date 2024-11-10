using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using kCura.Vendor.Castle.Core.Logging;
using Relativity.API;
using Relativity.Services.Objects;
using Relativity.Services.Objects.DataContracts;
using System.Runtime.CompilerServices;

namespace LTASBM.Agent.Handlers
{
    public class ObjectHandler
    {
        public static async Task<int> LookupClientArtifactID(IObjectManager objectManager, int workspaceArtifactId, string clientNumberValue, IAPILog logger)
        {            
            Guid objectType = new Guid("628EC03F-E789-40AF-AA13-351F92FFA44D");

            try
            {
                var queryRequest = new QueryRequest
                {
                    ObjectType = new ObjectTypeRef
                    {
                        Guid = objectType
                    },
                    Fields = new FieldRef[]
                    {
                        new FieldRef{ Name = "ArtifactID" }
                    },
                    Condition = $"'Client Number' == '{clientNumberValue.Trim()}'"
                };
                var result = await objectManager.QueryAsync(workspaceArtifactId, queryRequest, 0, 1);
                return result.Objects[0].ArtifactID;
            }
            catch (Exception ex)
            {
                string methodName = "LookupClientArtifactID";
                string errorMessage = ex.InnerException != null
                    ? String.Concat($"Method: {methodName} ---Value:{clientNumberValue} ", ex.InnerException.Message, "---", ex.StackTrace)
                    : String.Concat($"Method: {methodName} ---Value:{clientNumberValue} ", ex.Message, "---", ex.StackTrace);

                logger.ForContext<ObjectHandler>()
                      .LogError($"Error in {nameof(LookupClientArtifactID)}: {errorMessage}");
                return 0;
            }
        }
        public static async Task<CreateResult>CreateNewClient(IObjectManager objectManager, int workspaceArtifactId, string clientNumberValue, string clientNameValue, int clientEddsArtifactIdValue, IAPILog logger)
        {            
            Guid clientNumberField = new Guid("C3F4236F-B59B-48B5-99C8-3678AA5EEA72");
            Guid clientNameField = new Guid("E704BF08-C187-4EAB-9A25-51C17AA98FB9");
            Guid clientEDDSArtifactIdField = new Guid("1A30F07F-1E5C-4177-BB43-257EF7588660");

            try
            {
                var createRequest = new CreateRequest
                {
                    ObjectType = new ObjectTypeRef
                    {
                        Guid = new Guid("628EC03F-E789-40AF-AA13-351F92FFA44D")
                    }
                    ,
                    FieldValues = new List<FieldRefValuePair>
                    {
                        new FieldRefValuePair
                        {
                            Field = new FieldRef
                            {
                            Guid = clientNumberField
                            },
                            Value = clientNumberValue
                        },
                        new FieldRefValuePair
                        {
                            Field = new FieldRef
                            {
                            Guid= clientNameField
                            },
                            Value = clientNameValue
                        },
                        new FieldRefValuePair
                        {
                            Field = new FieldRef
                            {
                            Guid= clientEDDSArtifactIdField
                            },
                            Value = clientEddsArtifactIdValue
                        }
                    }
                };
                return await objectManager.CreateAsync(workspaceArtifactId, createRequest);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.InnerException != null ? String.Concat(ex.InnerException.Message, "---", ex.StackTrace) : String.Concat(ex.Message, "---", ex.StackTrace);
                logger.ForContext<ObjectHandler>().LogError($"{errorMessage}");
                return null;
            }           
        }

        public static async Task<CreateResult>CreateNewMatter(IObjectManager objectManager, int workspaceArtifactId, string matterNumberValue, string matterNameValue, int matterEddsArtifactIdValue, int matterClientObjectArtifactIdValue, IAPILog logger)
        {
            Guid matterNumberField = new Guid("3A8B7AC8-0393-4C48-9F58-C60980AE8107");
            Guid matterNameField = new Guid("C375AA14-D5CD-484C-91D5-35B21826AD14");
            Guid matterEddsArtifactIdField = new Guid("8F134CD2-4DB1-48E4-8479-2F7E7B18CF9F");
            Guid matterGUIDField = new Guid("4E41FF7F-9D1C-4502-96D9-DFBB9252B3E6");
            Guid matterClientObjectField = new Guid("0DD5C18A-35F8-4CF1-A00B-7814FA3A5788");
            try
            {
                var createRequest = new CreateRequest
                {
                    ObjectType = new ObjectTypeRef { Guid = new Guid("18DA4321-AAFB-4B24-99E9-13F90090BF1B") },
                    FieldValues = new List<FieldRefValuePair>
                    {
                        new FieldRefValuePair
                        {
                            Field = new FieldRef 
                            { 
                                Guid = matterNumberField 
                            }, 
                            Value = matterNumberValue
                        },
                        new FieldRefValuePair
                        {
                            Field = new FieldRef 
                            { 
                                Guid = matterNameField 
                            }, 
                            Value= matterNameValue
                        },
                        new FieldRefValuePair
                        {
                            Field = new FieldRef 
                            { 
                                Guid = matterEddsArtifactIdField 
                            }, 
                            Value = matterEddsArtifactIdValue
                        },
                        new FieldRefValuePair
                        {
                            Field = new FieldRef 
                            { 
                                Guid = matterGUIDField 
                            }, 
                            Value= Guid.NewGuid()
                        },
                        new FieldRefValuePair
                        {
                            Field = new FieldRef
                            {
                                Guid = matterClientObjectField
                            },
                            Value = new RelativityObjectRef
                            {
                                ArtifactID = matterClientObjectArtifactIdValue
                            }
                        }
                    }
                };                        
                return await objectManager.CreateAsync(workspaceArtifactId, createRequest);                                    
            }
            catch (Exception ex)
            {
                string errorMessage = ex.InnerException != null ? String.Concat(ex.InnerException.Message, "---", ex.StackTrace) : String.Concat(ex.Message, "---", ex.StackTrace);
                logger.ForContext<ObjectHandler>().LogError($"{errorMessage}");
                return null;
            }
        }
    }
}
