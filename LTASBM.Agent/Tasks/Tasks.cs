using LTASBM.Agent.Utilities;
using LTASBM.Kepler.Interfaces.LTASBM.v1;
using Relativity.API;
using Relativity.Identity.V1.Services;
using Relativity.Identity.V1.UserModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relativity.Services.Objects;
using Relativity.Services.Objects.DataContracts;

namespace LTASBM.Agent.Tasks
{
    public static class Tasks
    {
        public static async void ClientIncorrectFormat(IAPILog logger, IInstanceSettingsBundle instanceSettingsBundle, IUserManager userManager, List<LTASClient> lTASClients)
        {
            var wrongClientFormat = lTASClients.Where(client => client.Number.Length > 5).ToList();

            if (wrongClientFormat.Any())
            {
                logger.LogInformation($"There are {wrongClientFormat} client's that need to have the client number fixed.");
                foreach (var client in wrongClientFormat)
                {
                    //TODO: loop this into a try and log user info
                    UserResponse response = await userManager.ReadAsync(client.CreatedBy);
                    Emails.FixClientEmail(instanceSettingsBundle, response.EmailAddress, response.FirstName);
                }
            }
        }

        static async Task CreateNewClient(IObjectManager objectManager, int workspaceArtifactID, string clientIdValue, string clientNameValue)
        {
            Guid clientIdField = new Guid("C3F4236F-B59B-48B5-99C8-3678AA5EEA72");
            Guid clientNameField = new Guid("C3F4236F-B59B-48B5-99C8-3678AA5EEA72");

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
                            Guid = clientIdField
                           },
                           Value = clientIdValue
                       },
                       new FieldRefValuePair
                       {
                           Field = new FieldRef
                           {
                            Guid= clientNameField
                           },
                           Value = clientNameValue
                       }
                    }
                };

                using (objectManager)
                {
                    var result = await objectManager.CreateAsync(workspaceArtifactID, createRequest);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating client: {ex.Message}");
            }
        }

        static async Task CreateNewMatter(IObjectManager objectManager, int workspaceId, string matterId, string matterName)
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
                        ObjectType = new ObjectTypeRef { ArtifactTypeID = 1000116 },
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
