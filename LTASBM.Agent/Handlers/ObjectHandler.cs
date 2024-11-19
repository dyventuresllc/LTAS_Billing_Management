using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Relativity.API;
using Relativity.Services.Objects;
using Relativity.Services.Objects.DataContracts;
using LTASBM.Agent.Utilites;

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
            int relatedObjectArtifactId,            
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
            bool isChoice,
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
    }
}
