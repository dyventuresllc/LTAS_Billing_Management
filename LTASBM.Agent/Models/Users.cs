﻿namespace LTASBM.Agent.Models
{
    public class EddsUsers
    {
        public int EddsUserArtifactId { get; set; }
        public string EddsUserFirstName { get; set; }
        public string EddsUserLastName { get; set;}
        public string EddsUserEmailAddress { get; set;}
        public bool EddsUserRelativityAccess { get; set;}
    }

    public class BillingUsers
    {
        public int BillingUserArtifactId { get; set;}
        public int BillingUserEddsArtifactId { get; set;}
        public string BillingUserFirstName { get; set;} 
        public string BillingUserLastName { get; set;}  
        public string BillingUserEmailAddress { get; set;}
    }
}