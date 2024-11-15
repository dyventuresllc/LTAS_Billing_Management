
using System;

namespace LTASBM.Agent.Models
{
    public class EddsWorkspaces
    {
        public int EddsWorkspaceArtifactId {get; set; }
        public string EddsWorkspaceName { get; set; }
        public string EddsWorkspaceCreatedBy { get; set; }
        public DateTime EddsWorkspaceCreatedOn { get;set; }
        public int EddsWorkspaceMatterArtifactId { get; set; }
        public int EddsWorkspaceClientArtifactId { get; set; }
        public string EddsWorkspaceAnalyst { get; set; } = null;
        public string EddsWorkspaceCaseTeam { get; set; } = null;
        public string EddsWorkspaceStatusName { get; set; }
    }

    public class BillingWorkspaces
    {
        public int BillingWorkspaceArtifactId { get; set; }
        public int BillingWorkspaceEddsArtifactId { get; set; } 
        public string BillingWorkspaceName { get; set; } = null;
        public string BillingWorkspaceCreatedBy { get; set; } = null;
        public DateTime BillingWorkspaceCreatedOn { get; set; } = DateTime.MinValue;
        public int BillingWorkspaceMatterArtifactId { get; set; } = 0;      
        public string BillingWorkspaceAnalyst { get; set; } = null;
        public string BillingWorkspaceCaseTeam { get; set; } = null;
        public string BillingStatusName { get; set; } = null;
    }
}
