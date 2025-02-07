using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Relativity.API;
using Relativity.Services.Objects;
using Relativity.Services.Objects.DataContracts;
using LTASBM.Agent.Utilites;
using kCura.Vendor.Castle.Core.Logging;

namespace LTASBM.Agent.Handlers
{
    public class ObjectHandler
    {
        public static async Task<CreateResult>CreateNewClientAsync(
            IObjectManager objectManager, 
            int workspaceArtifactId, 
            string clientNumberValue, 
            string clientNameValue, 
            int clientEddsArtifactIdValue, 
            IAPILog logger, 
            IHelper helper)
        {
            var ltasHelper = new LTASBMHelper(helper, logger);
                        
            try
            {
                var createRequest = new CreateRequest
                {
                    ObjectType = new ObjectTypeRef
                    {
                        Guid = ltasHelper.ClientObjectType
                    }
                    ,
                    FieldValues = new List<FieldRefValuePair>
                    {
                        new FieldRefValuePair
                        {
                            Field = new FieldRef
                            {
                                Guid = ltasHelper.ClientNumberField
                            },
                            Value = clientNumberValue
                        },
                        new FieldRefValuePair
                        {
                            Field = new FieldRef
                            {
                                Guid= ltasHelper.ClientNameField
                            },
                            Value = clientNameValue
                        },
                        new FieldRefValuePair
                        {
                            Field = new FieldRef
                            {
                                Guid= ltasHelper.ClientEDDSArtifactIdField
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

        public static async Task<CreateResult>CreateNewMatterAsync(
            IObjectManager objectManager, 
            int workspaceArtifactId, 
            string matterNumberValue, 
            string matterNameValue, 
            int matterEddsArtifactIdValue, 
            int matterClientObjectArtifactIdValue, 
            IAPILog logger, 
            IHelper helper)
        {
            var ltasHelper = new LTASBMHelper(helper, logger);

            try
            {
                var createRequest = new CreateRequest
                {
                    ObjectType = new ObjectTypeRef { Guid = ltasHelper.MatterObjectType },
                    FieldValues = new List<FieldRefValuePair>
                    {
                        new FieldRefValuePair
                        {
                            Field = new FieldRef 
                            { 
                                Guid = ltasHelper.MatterNumberField
                            }, 
                            Value = matterNumberValue
                        },
                        new FieldRefValuePair
                        {
                            Field = new FieldRef 
                            { 
                                Guid = ltasHelper.MatterNameField
                            }, 
                            Value= matterNameValue
                        },
                        new FieldRefValuePair
                        {
                            Field = new FieldRef 
                            { 
                                Guid = ltasHelper.MatterEddsArtifactIdField
                            }, 
                            Value = matterEddsArtifactIdValue
                        },
                        new FieldRefValuePair
                        {
                            Field = new FieldRef 
                            { 
                                Guid = ltasHelper.MatterGUIDField
                            }, 
                            Value= Guid.NewGuid()
                        },
                        new FieldRefValuePair
                        {
                            Field = new FieldRef
                            {
                                Guid = ltasHelper.MatterClientObjectField
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

        public static async Task<CreateResult> CreateNewWorkspaceAsync(
            IObjectManager objectManager,
            int workspaceArtifactId,
            int workspaceArtifactIdValue,
            string workspaceCreatedByValue,
            DateTime workspaceCreatedOnValue,
            string workspaceNameValue,
            int workspaceEddsMatterArtifactIdValue,
            string workspaceCaseTeamValue,
            string workspaceLtasAnalystValue,
            string workspaceStatusValue,
            IAPILog logger,
            IHelper helper)
        {
            var ltasHelper = new LTASBMHelper(helper, logger);

            try
            {
                var createRequest = new CreateRequest
                {
                    ObjectType = new ObjectTypeRef
                    {
                        Guid = ltasHelper.WorkspaceObjectType
                    },
                    FieldValues = new List<FieldRefValuePair>
            {
                new FieldRefValuePair
                {
                    Field = new FieldRef
                    {
                        Guid = ltasHelper.WorkspaceEDDSArtifactIDField
                    },
                    Value = workspaceArtifactIdValue
                },
                new FieldRefValuePair
                {
                    Field = new FieldRef
                    {
                        Guid = ltasHelper.WorkspaceCreatedByField
                    },
                    Value = workspaceCreatedByValue
                },
                new FieldRefValuePair
                {
                    Field = new FieldRef
                    {
                        Guid = ltasHelper.WorkspaceCreatedOnField
                    },
                    Value = workspaceCreatedOnValue
                },
                new FieldRefValuePair
                {
                    Field = new FieldRef
                    {
                        Guid = ltasHelper.WorkspaceNameField
                    },
                    Value = workspaceNameValue
                },
                new FieldRefValuePair
                {
                    Field = new FieldRef
                    {
                        Guid = ltasHelper.WorkspaceLtasAnalystField
                    },
                    Value = workspaceLtasAnalystValue
                },
                new FieldRefValuePair
                {
                    Field = new FieldRef
                    {
                        Guid = ltasHelper.WorkspaceCaseTeamField
                    },
                    Value = workspaceCaseTeamValue
                },
                new FieldRefValuePair
                {
                    Field = new FieldRef
                    {
                        Guid = ltasHelper.WorkspaceMatterNumberField
                    },
                    Value = new RelativityObjectRef
                    {
                        ArtifactID = await ltasHelper.LookupMatterArtifactID(
                            objectManager,
                            workspaceArtifactId,
                            workspaceEddsMatterArtifactIdValue.ToString())
                    }
                },
                new FieldRefValuePair
                {
                    Field = new FieldRef
                    {
                        Guid = ltasHelper.WorkspaceStatusField
                    },
                    Value = new ChoiceRef
                    {
                        ArtifactID = ltasHelper.GetCaseStatusArtifactID(
                            helper.GetDBContext(workspaceArtifactId),
                            workspaceStatusValue)
                    }
                }
            }
                };

                return await objectManager.CreateAsync(workspaceArtifactId, createRequest);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.InnerException != null
                    ? String.Concat(ex.InnerException.Message, "---", ex.StackTrace)
                    : String.Concat(ex.Message, "---", ex.StackTrace);
                logger.ForContext<ObjectHandler>().LogError($"{errorMessage}");
                return null;
            }
        }

        public static async Task<CreateResult> CreateNewUserAsync(
            IObjectManager objectManager,
            int workspaceArtifactId,
            string userFirstNameValue,
            string userLastNameValue,
            string userEmailAddress,
            int userEddsArtifactId,
            IAPILog logger,
            IHelper helper)
        {
            var ltasHelper = new LTASBMHelper(helper, logger);

            try
            {
                var createRequest = new CreateRequest
                {
                    ObjectType = new ObjectTypeRef { Guid = ltasHelper.UserObjectType },
                    FieldValues = new List<FieldRefValuePair>
                    {
                        new FieldRefValuePair
                        {
                            Field = new FieldRef
                            {
                                Guid = ltasHelper.UserFirstNameField
                            },
                            Value = userFirstNameValue
                        },
                        new FieldRefValuePair
                        {
                            Field = new FieldRef
                            {
                                Guid = ltasHelper.UserLastNameField
                            },
                            Value= userLastNameValue
                        },
                        new FieldRefValuePair
                        {
                            Field = new FieldRef
                            {
                                Guid = ltasHelper.UserEmailAddressField
                            },
                            Value = userEmailAddress
                        },
                        new FieldRefValuePair
                        {
                            Field = new FieldRef
                            {
                                Guid = ltasHelper.UserEddsArtifactIdField
                            },
                            Value = userEddsArtifactId
                        },
                        new FieldRefValuePair
                        {
                            Field = new FieldRef
                            {
                                Guid = ltasHelper.UserVisibleField
                            },
                            Value = 1
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

        public static async Task<UpdateResult> UpdateFieldValueAsync(
            IObjectManager objectManager, 
            int workspaceArtifactId,
            int objectArtifactId,
            Guid fieldGuid,
            object fieldValue,            
            IAPILog logger)
        {
            try 
            {
                var UpdateRequest = new UpdateRequest
                {
                    Object = new RelativityObjectRef
                    {
                        ArtifactID = objectArtifactId
                    },
                    FieldValues = new List<FieldRefValuePair>
                    {
                        new FieldRefValuePair
                        {
                            Field = new FieldRef
                            {
                                Guid = fieldGuid
                            },
                            Value = fieldValue
                        }
                    }
                };
                return await objectManager.UpdateAsync(workspaceArtifactId, UpdateRequest);
            }
            catch(Exception ex) 
            {
                string errorMessage = ex.InnerException != null ? 
                    String.Concat(ex.InnerException.Message, "---", ex.StackTrace) : 
                    String.Concat(ex.Message, "---", ex.StackTrace);
                logger.ForContext<ObjectHandler>().LogError($"{errorMessage}");
                return null;
            }
        }

        public static async Task<UpdateResult> UpdateFieldValueAsync(
            IObjectManager objectManager,
            int workspaceArtifactId,
            int objectArtifactId,
            Guid fieldGuid,
            RelativityObjectRef relatedObjectArtifactId,            
            IAPILog logger)
        {           
            try
            {
                var UpdateRequest = new UpdateRequest
                {
                    Object = new RelativityObjectRef
                    {
                        ArtifactID = objectArtifactId
                    },
                    FieldValues = new List<FieldRefValuePair>
                    {
                        new FieldRefValuePair
                        {
                            Field = new FieldRef
                            {
                                Guid = fieldGuid
                            },
                            Value = relatedObjectArtifactId
                        }
                    }
                };
                return await objectManager.UpdateAsync(workspaceArtifactId, UpdateRequest);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.InnerException != null ?
                    String.Concat(ex.InnerException.Message, "---", ex.StackTrace) :
                    String.Concat(ex.Message, "---", ex.StackTrace);
                logger.ForContext<ObjectHandler>().LogError($"{errorMessage}");
                return null;
            }
        }

        public static async Task<UpdateResult> UpdateFieldValueAsync(
            IObjectManager objectManager,
            int workspaceArtifactId,
            int objectArtifactId,
            Guid fieldGuid,
            ChoiceRef choiceArtifactId,            
            IAPILog logger)
        {           
            try
            {
                var UpdateRequest = new UpdateRequest
                {
                    Object = new RelativityObjectRef
                    {
                        ArtifactID = objectArtifactId
                    },
                    FieldValues = new List<FieldRefValuePair>
                    {
                        new FieldRefValuePair
                        {
                            Field = new FieldRef
                            {
                                Guid = fieldGuid
                            },
                            Value = choiceArtifactId
                        }
                    }
                };
                return await objectManager.UpdateAsync(workspaceArtifactId, UpdateRequest);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.InnerException != null ?
                    String.Concat(ex.InnerException.Message, "---", ex.StackTrace) :
                    String.Concat(ex.Message, "---", ex.StackTrace);
                logger.ForContext<ObjectHandler>().LogError($"{errorMessage}");
                return null;
            }
        }

        public static async Task<QueryResult> WorkspacesDeletedNoDeletedDate(
            IObjectManager objectManager, 
            int workspaceArtifactId, 
            Guid workspaceObjectType, 
            Guid workspaceEddsArtifactIdField,
            Guid workspaceNameField,
            Guid workspaceStatusField,
            Guid workspaceCreatedByField,
            Guid workspaceCreatedOnField,
            IAPILog logger
            )
        {
            try
            {
                var QueryRequest = new QueryRequest
                {
                    ObjectType = new ObjectTypeRef
                    {
                        Guid = workspaceObjectType
                    },
                    Fields = new FieldRef[]
                    {
                        new FieldRef { Name = "ArtifactId"},
                        new FieldRef { Guid = workspaceEddsArtifactIdField },
                        new FieldRef { Guid = workspaceNameField },
                        new FieldRef { Guid = workspaceStatusField },
                        new FieldRef { Guid = workspaceCreatedByField },
                        new FieldRef { Guid = workspaceCreatedOnField }
                    },
                    Condition = $"(('Case Status' == CHOICE 1068674) AND (NOT 'Status: Deleted Date' ISSET))"
                };
                return await objectManager.QueryAsync(workspaceArtifactId, QueryRequest, 0, 10000);
            }
            catch (Exception ex)
            {
                logger.ForContext<ObjectHandler>()
                       .LogError(ex, $"Error checking for workspaces missing deleted date");
                throw;
            }
        }

        public static async Task<QueryResult> WorkspacesForBilling(
            IObjectManager objectManager,
            int workspaceArtifactId,
            Guid workspaceObjectType,
            Guid workspaceEddsArtifactIdField,      
            Guid workspaceMatterArtifactID,
            IAPILog logger
            )
        {
            try
            {
                var QueryRequest = new QueryRequest
                {
                    ObjectType = new ObjectTypeRef
                    {
                        Guid = workspaceObjectType
                    },
                    Fields = new FieldRef[]
                    {
                        new FieldRef { Name = "ArtifactId" },
                        new FieldRef { Guid = workspaceEddsArtifactIdField },
                        new FieldRef { Guid = workspaceMatterArtifactID }
                    },
                    Condition = $"((NOT 'Status: Deleted Date' ISSET))"
                };                               
                return await objectManager.QueryAsync(workspaceArtifactId, QueryRequest, 0, 10000);
            }
            catch (Exception ex)
            {
                logger.ForContext<ObjectHandler>()
                       .LogError(ex, $"Error getting Billing Manager worksapce list");
                throw;
            }
        }

        public static async Task<CreateResult> CreateBillingDetails(
            IObjectManager objectManager,
            int workspaceArtifactId,
            CreateRequest createRequest,
            IAPILog logger)
        {            
            try
            {
                return await objectManager.CreateAsync(workspaceArtifactId, createRequest);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.InnerException != null ? String.Concat(ex.InnerException.Message, "---", ex.StackTrace) : String.Concat(ex.Message, "---", ex.StackTrace);
                logger.ForContext<ObjectHandler>().LogError($"{errorMessage}");
                return null;
            }
        }

        public static async Task<UpdateResult> UpdateBillingDetails(
            IObjectManager objectManager,
            int workspaceArtifactId,
            UpdateRequest updateRequest,
            IAPILog logger)
        {
            try 
            { 
                return await objectManager.UpdateAsync(workspaceArtifactId, updateRequest);
            }
            catch (Exception ex) 
            {
                string errorMessage = ex.InnerException != null ? String.Concat(ex.InnerException.Message, "---", ex.StackTrace) : String.Concat(ex.Message, "---", ex.StackTrace);
                logger.ForContext<ObjectHandler>().LogError(
                $"Failed to update billing details for artifact {updateRequest.Object.ArtifactID}: {errorMessage}");
                return null;
            }
        }

        public static async Task<UpdateResult> UpdateBillingDetailsWUpdateOptions(
            IObjectManager objectManager,
            int workspaceArtifactId,
            UpdateRequest updateRequest,
            IAPILog logger)
        {
            try
            {
                var updateOptions = new UpdateOptions
                {
                    UpdateBehavior = FieldUpdateBehavior.Replace
                };

                return await objectManager.UpdateAsync(workspaceArtifactId, updateRequest, updateOptions);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.InnerException != null ? String.Concat(ex.InnerException.Message, "---", ex.StackTrace) : String.Concat(ex.Message, "---", ex.StackTrace);
                logger.ForContext<ObjectHandler>().LogError(
                $"Failed to update billing details for artifact {updateRequest.Object.ArtifactID}: {errorMessage}");
                return null;
            }
        }

        public static async Task <QueryResult> MatterBillingStatus (
            IObjectManager objectManager, 
            int workspaceArtifactId, 
            IAPILog logger, 
            IHelper helper)
        {
            var ltasHelper = new LTASBMHelper(helper, logger);

            try
            {
                var queryRequest = new QueryRequest
                {
                    ObjectType = new ObjectTypeRef
                    {
                        Guid = ltasHelper.MatterObjectType
                    },
                    Fields = new FieldRef[]
                    {
                    new FieldRef{ Name = "ArtifactID" },
                    new FieldRef{ Name = "Active Billing"}
                    }                  
                };

                return await objectManager.QueryAsync(workspaceArtifactId, queryRequest, 0, 100000);
                
            }
            catch (Exception ex)
            {
                string errorMessage = ex.InnerException != null ? String.Concat(ex.InnerException.Message, "---", ex.StackTrace) : String.Concat(ex.Message, "---", ex.StackTrace);
                logger.ForContext<ObjectHandler>().LogError(
                $"Matter Active Billing Status query failed: {errorMessage}");
                return null;
            }
        }

        public static async Task<QueryResult> BillingDetailsDataForReporting(
            IObjectManager objectManager,
            int workspaceArtifactId,
            IAPILog logger,
            IHelper helper,
            string dateKey)
        {
            var ltasHelper = new LTASBMHelper(helper, logger);

            try 
            {
                var queryRequest = new QueryRequest
                {
                    ObjectType = new ObjectTypeRef 
                    {
                        Guid = ltasHelper.DetailsObjectType
                    },
                    Fields = new FieldRef[] 
                    {
                        new FieldRef { Name = "ArtifactID" },
                        new FieldRef { Guid = ltasHelper.DetailsRVWH3230 },
                        new FieldRef { Guid = ltasHelper.DetailsRVWH3231 },
                        new FieldRef { Guid = ltasHelper.DetailsRVWH3232 },
                        new FieldRef { Guid = ltasHelper.DetailsRPYH3220 },
                        new FieldRef { Guid = ltasHelper.DetailsRPYH3221 },
                        new FieldRef { Guid = ltasHelper.DetailsRPYH3222 },
                        new FieldRef { Guid = ltasHelper.DetailsRVWP3210 },
                        new FieldRef { Guid = ltasHelper.DetailsRVWP3211 },
                        new FieldRef { Guid = ltasHelper.DetailsRVWP3212 },
                        new FieldRef { Guid = ltasHelper.DetailsRPYP3240 },
                        new FieldRef { Guid = ltasHelper.DetailsRPYP3241 },
                        new FieldRef { Guid = ltasHelper.DetailsRPYP3242 },
                        new FieldRef { Guid = ltasHelper.DetailsTU3270 },
                        new FieldRef { Guid = ltasHelper.DetailsPU3203 },
                        new FieldRef { Guid = ltasHelper.DetailsCS3205 },
                        new FieldRef { Guid = ltasHelper.DetailsUU3200 },
                        new FieldRef { Guid = ltasHelper.DetailsARU3201 },
                        new FieldRef { Guid = ltasHelper.DetailsAPU3202 },
                        new FieldRef { Guid = ltasHelper.DetailsUsers }, 
                        new FieldRef { Guid = ltasHelper.DetailsWorkspaceCount}
                    },
                    Condition = $"(('YearMonth' IN['{dateKey}']))"
                };

                return await objectManager.QueryAsync(workspaceArtifactId, queryRequest, 0, 1000000);
            }
            catch (Exception ex) 
            {
                string errorMessage = ex.InnerException != null ? String.Concat(ex.InnerException.Message, "---", ex.StackTrace) : String.Concat(ex.Message, "---", ex.StackTrace);
                logger.ForContext<ObjectHandler>().LogError(
                $"Obtaning Billing Details failed: {errorMessage}");
                return null;
            }
        }

        public static async Task<QueryResult> MatterDetailsData(
            IObjectManager objectManager,
            int workspaceArtifactId,
            IAPILog logger,
            IHelper helper,
            int matterArtifactId)
        {
            var ltasHelper = new LTASBMHelper(helper, logger);

            try
            {
                var queryRequest = new QueryRequest
                {
                    ObjectType = new ObjectTypeRef
                    {
                        Guid = ltasHelper.MatterObjectType
                    },
                    Fields = new FieldRef[]
                    {
                        /*
                         * Matter Number
                         * Matter Name
                         * Matter Guid
                         * Main Contact
                         * Email_To
                         * Email_CC
                         */
                        new FieldRef { Guid = ltasHelper.MatterNameField },
                        new FieldRef { Guid = ltasHelper.MatterNumberField },
                        new FieldRef { Guid = ltasHelper.MatterGUIDField },                        
                        new FieldRef { Guid = ltasHelper.O_ReviewHostingField},
                        new FieldRef { Guid = ltasHelper.O_RepositoryHostingField},
                        new FieldRef { Guid = ltasHelper.O_ReviewProcessingField},
                        new FieldRef { Guid = ltasHelper.O_RepositoryProcessingField},
                        new FieldRef { Guid = ltasHelper.O_AirForReviewField},
                        new FieldRef { Guid = ltasHelper.O_AirForPrivilegeField},
                        new FieldRef { Guid = ltasHelper.O_ColdStorageField},
                        new FieldRef { Guid = ltasHelper.O_ImageField},
                        new FieldRef { Guid = ltasHelper.O_TranslationsField},
                        new FieldRef { Guid = ltasHelper.O_UsersField},
                        new FieldRef { Guid = ltasHelper.MatterMainContact},
                        new FieldRef { Guid = ltasHelper.MatterEmailTo},
                        new FieldRef { Guid = ltasHelper.MatterEmailCC}
                    },
                    Condition = $"('Artifact ID' == {matterArtifactId})"
                };

                return await objectManager.QueryAsync(workspaceArtifactId, queryRequest, 0, 1);
            }
            catch (Exception ex) 
            { 
                string errorMessage = ex.InnerException != null ? String.Concat(ex.InnerException.Message, "---", ex.StackTrace) : String.Concat(ex.Message, "---", ex.StackTrace);
                logger.ForContext<ObjectHandler>().LogError(
                $"Obtaning Matter Details failed: {errorMessage}");
                return null;
            }

        }

        public static async Task<QueryResult> BillingUserLookup(
            IObjectManager objectManager,
            int workspaceArtifactId,
            int userArtifactId,
            IAPILog logger,
            IHelper helper)
        {
            var ltasHelper = new LTASBMHelper(helper, logger);
            
            try 
            {
                var queryRequest = new QueryRequest
                {
                    ObjectType = new ObjectTypeRef
                    {
                        Guid = ltasHelper.UserObjectType
                    },
                    Fields = new FieldRef[]
                    {
                        new FieldRef { Name = "ArtifactID" },
                        new FieldRef { Guid = ltasHelper.UserFirstNameField },
                        new FieldRef { Guid = ltasHelper.UserLastNameField },
                        new FieldRef { Guid = ltasHelper.UserEmailAddressField }
                    },
                    Condition = $"('Artifact ID' == {userArtifactId})"
                };

                return await objectManager.QueryAsync(workspaceArtifactId, queryRequest, 0, 1);
            }
            catch (Exception ex) 
            {
                string errorMessage = ex.InnerException != null ? String.Concat(ex.InnerException.Message, "---", ex.StackTrace) : String.Concat(ex.Message, "---", ex.StackTrace);
                logger.ForContext<ObjectHandler>().LogError(
                $"Obtaning User Information from Billing Details failed: {errorMessage}");
                return null;
            }
        }
    }
}
