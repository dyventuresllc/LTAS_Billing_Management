using kCura.EventHandler;
using System;
using System.Collections.Generic;
using LTASBM.Agent.Utilites;


namespace LTASBM.Agent.Event_Handlers
{
    public class EH_PS_BillingReceipient : PreSaveEventHandler
    {
        public override FieldCollection RequiredFields
        {
            get
            {
                var retVal = new FieldCollection();
                var helper = new LTASBMHelper(this.Helper, Helper.GetLoggerFactory().GetLogger());
                retVal.Add(new Field(0,                           // artifactID
                                    "EDDSUserArtifactID",         // name
                                    "EDDSUserArtifactID",         // columnName
                                    0,                            // fieldTypeID
                                    0,                            // codeTypeID
                                    0,                            // fieldCategoryID
                                    false,                        // isReflected
                                    false,                        // isInLayout
                                    null,                         // value
                                    new List<Guid> { helper.UserEddsArtifactIdField }  // guids
                                    ));
                
                retVal.Add(new Field(0,                         // artifactID
                                   "Visible",                   // name
                                   "Visible",                   // columnName
                                   0,                           // fieldTypeID
                                   0,                           // codeTypeID
                                   0,                           // fieldCategoryID
                                   false,                       // isReflected
                                   false,                       // isInLayout
                                   null,                        // value
                                   new List<Guid> { helper.UserVisibleField }  // guids
                                   ));

                return retVal;
            }
        }

        public override Response Execute()
        {
            var retVal = new Response
            {
                Success = true,
                Message = string.Empty
            };

            try
            {
                var helper = new LTASBMHelper(this.Helper, Helper.GetLoggerFactory().GetLogger());
                var eddsUserArtifactIdField = this.ActiveArtifact.Fields[helper.UserEddsArtifactIdField.ToString()];
                var IsUserVisible = this.ActiveArtifact.Fields[helper.UserVisibleField.ToString()];

                if (eddsUserArtifactIdField != null &&
                    (eddsUserArtifactIdField.Value == null ||
                     eddsUserArtifactIdField.Value.Value == null ||
                     eddsUserArtifactIdField.Value.Value.ToString().Trim() == ""))
                {
                    eddsUserArtifactIdField.Value.Value = 9999999;
                    IsUserVisible.Value.Value = true;                    
                }
            }
            catch (Exception ex)
            {
                retVal.Success = false;
                retVal.Message = ex.ToString();
            }

            return retVal;
        }
    }
}
