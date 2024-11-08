
namespace LTASBM.Agent.Models
{
    public class EddsMatters
    {
        public int EddsMatterArtifactId {get; set; }
        public string EddsMatterName { get; set; }
        public string EddsMatterNumber { get; set; }
        public string EddsMatterCreatedByFirstName { get;set; }
        public string EddsMatterCreatedByEmail { get; set; }
    }

    public class BillingMatters
    {
        public int BillingEddsMatterArtifactId { get; set; }
        public string BillingEddsMatterName { get;set; }
        public string BillingEddsMatterNumber { get;set; }
    }
}
