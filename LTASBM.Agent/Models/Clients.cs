namespace LTASBM.Agent.Models
{
    public class EddsClients
    {
        public int EddsClientArtifactId {get; set; }
        public string EddsClientName { get; set; }
        public string EddsClientNumber { get; set; }
        public string EddsClientCreatedByFirstName { get;set; }
        public string EddsClientCreatedByEmail { get; set; }
    }

    public class BillingClients
    {
        public int BillingClientArtifactID { get; set; }
        public int BillingEddsClientArtifactId { get; set; }
        public string BillingEddsClientName { get;set; }
        public string BillingEddsClientNumber { get;set; }
    }
}
