using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Relativity.API;
using Relativity.Services.Objects;
using Relativity.Services.Objects.DataContracts;

namespace LTASBM.Agent.Handlers
{
    public class ObjectHandler
    {    
        private readonly IServicesMgr ServicesMgr;
        private readonly IAPILog Logger;

        public ObjectHandler(IServicesMgr servicesMgr, IAPILog logger)
        {
            ServicesMgr = servicesMgr;
            Logger = logger;
        }

        public async Task<CreateResult>CreateNewClient(int workspaceArtifactID, string clientNumberValue, string clientNameValue, int clientEddsArtifactIdValue)
        {            
            Guid clientNumberField = new Guid("C3F4236F-B59B-48B5-99C8-3678AA5EEA72");
            Guid clientNameField = new Guid("E704BF08-C187-4EAB-9A25-51C17AA98FB9");
            Guid clientEDDSArtifactIDField = new Guid("1A30F07F-1E5C-4177-BB43-257EF7588660");

            try
            {
                IObjectManager objectManager  = ServicesMgr.CreateProxy<IObjectManager>(ExecutionIdentity.System);

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
                            Guid= clientEDDSArtifactIDField
                            },
                            Value = clientEddsArtifactIdValue
                        }
                    }
                };

                return await objectManager.CreateAsync(workspaceArtifactID, createRequest); 
            }
            catch (Exception ex)
            {
                string errorMessage = ex.InnerException != null ? String.Concat(ex.InnerException.Message, "---", ex.StackTrace) : String.Concat(ex.Message, "---", ex.StackTrace);
                Logger.ForContext<ObjectHandler>().LogError($"{errorMessage}");
                throw;          
            }           
        }

        public static async Task CreateNewMatter(IObjectManager objectManager, int workspaceId, string matterId, string matterName)
        {
            Guid matterIdField = new Guid("3A8B7AC8-0393-4C48-9F58-C60980AE8107");
            Guid matterNameField = new Guid("C375AA14-D5CD-484C-91D5-35B21826AD14");
            Guid MatterGuidField = new Guid("4E41FF7F-9D1C-4502-96D9-DFBB9252B3E6");

            try
            {
                using (objectManager)
                {
                    var createRequest = new CreateRequest
                    {
                        ObjectType = new ObjectTypeRef { Guid = new Guid("18DA4321-AAFB-4B24-99E9-13F90090BF1B") },
                        FieldValues = new List<FieldRefValuePair>
                        {
                            new FieldRefValuePair
                            {
                                Field = new FieldRef { Guid = matterIdField }, Value = matterId
                            },
                            new FieldRefValuePair
                            {
                                Field = new FieldRef { Guid = matterNameField }, Value= matterName
                            },
                            new FieldRefValuePair
                            {
                                Field = new FieldRef { Guid = MatterGuidField}, Value= Guid.NewGuid()
                            }
                        }
                    };
                    using (objectManager)
                    {
                        var result = await objectManager.CreateAsync(workspaceId, createRequest);
                    }
                }
            }
            catch (Exception ex)
            {
               
                Console.WriteLine($"Error creating matter: {ex.Message}");
                Console.WriteLine($"Detail: {ex}");
            }
        }
    }
}
