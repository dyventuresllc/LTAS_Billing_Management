using LTASBM.Agent.Utilities;
using LTASBM.Kepler.Interfaces.LTASBM.v1;
using Relativity.API;
using Relativity.Identity.V1.Services;
using Relativity.Identity.V1.UserModels;
using System.Collections.Generic;
using System.Linq;

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
    }
}
