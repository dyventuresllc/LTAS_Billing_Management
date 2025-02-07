using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace LTASBM.Agent.Models
{
    public class TempBilingDetailsWorkspace
    {
        public int ArtifactIdWorkspaceEDDS { get; set; }    
        public int ArtifactIdMatter { get; set; }
        public string DateKey { get; set; }
        public decimal HostingReview {  get; set; }
        public decimal HostingRepository { get; set; }
        public decimal ProcessingReview { get; set; }
        public decimal ProcessingRepository { get; set;}
        public int TranslationUnits { get; set; }
        public int PageCountUnits { get; set; }
        public int AirPriviilegeUnits { get; set; }
        public int AirReviewUnits { get; set; }
        public decimal ColdStorage { get; set; }
    }

    public class BillingResponse
    {
        public List<BillingResult> Results { get; set; }
    }

    public class BillingResult
    {
        public WorkspaceInfo Workspace { get; set; }
        public PricedMetrics PricedMetrics { get; set; }
    }

    public class WorkspaceInfo
    {
        public string WorkspaceStatus { get; set; }
        public string BillableStatus { get; set; }
    }

    public class PricedMetrics
    {
        [JsonProperty("Relativity_Storage_Review")]
        public MetricDetail ReviewHosting { get; set; }

        [JsonProperty("Relativity_Storage_Repository")]
        public MetricDetail RepositoryHosting { get; set; }

        [JsonProperty("Relativity_Storage_ColdStorage")]
        public MetricDetail StorageCold { get; set; }

        [JsonProperty("Relativity_Translate")]
        public MetricDetail TranslateUnits { get; set; }

        [JsonProperty("aiR_for_Review")]
        public MetricDetail AirReviewUnits { get; set; }

        [JsonProperty("aiR_for_Privilege")]
        public MetricDetail AirPrivilegeUnits { get; set; }
    }

    public class MetricDetail
    {
        public decimal? BillableValue { get; set; }
    }

    public class MetadataResponse
    {
        public List<Origin> Origins { get; set; }
    }

    public class Origin
    {
        public string Name { get; set; }
        public List<Metric> Metrics { get; set; }
    }

    public class Metric
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class ReportRequest
    {
        public string RequestId { get; } = Guid.NewGuid().ToString();
        public DateTime RequestDate { get; set; }
    }

    public class UsageReportResponse
    {
        public string Id { get; set; }
        public string TenantId { get; set; }
        public string Name { get; set; }
        public DateRange DateRange { get; set; }
        public List<string> Fields { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Workspaces { get; set; }
        public bool IsAdmin { get; set; }
        public int Version { get; set; }
    }

    public class DateRange
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }

    public class SecondaryData
    {
        public string InstanceName { get; set; }
        public string WorkspaceName { get; set; }
        public string ClientName { get; set; }
        public string MatterName { get; set; }
        public DateTime? WorkspaceUtilizationCollectedAt { get; set; }
        public DateTime? ProcessingStatisticsCollectedAt { get; set; }
        public int WorkspaceArtifactId { get; set; }
        public decimal PublishedDocumentSizeGB { get; set; }        
        public decimal LinkedTotalFileSizeGB { get; set; }
        public decimal PeakWorkspaceHostedSizeGB { get; set; }
        public int TranslateDocumentUnits { get; set; }        
        public string WorkspaceType { get; set; }

        public int AirForReviewDocuments { get; set; }
        public int AirForPrivilegeDocuments { get; set; }
    }
    
    public class BillingSummaryMetrics
    {        
        public int MatterArtifactId { get; set; }
        public string DateKey { get; set; }
        public decimal SumHostingReview { get; set; }
        public decimal SumHostingRepository { get; set; }
        public decimal SumProcessingReview { get; set; }
        public decimal SumProcessingRepository { get; set; }
        public int SumTranslationUnits { get; set; }
        public int SumPageCountUnits { get; set; }
        public int SumAirPriviilegeUnits { get; set; }
        public int SumAirReviewUnits { get; set; }
        public decimal SumColdStorage { get; set; }   
        
    }

    public class BillingSummaryWorkspaces
    {
        public int MatterArtifactId { get; set; }
        public string DateKey { get; set; }
        public int WorkspaceEddsArtifactId { get; set; }
    }

    public class BillingSummaryUsers
    {
        public int MatterArtifactId { get; set; }
        public string DateKey { get; set; } 
        public int UserArtifactId { get; set; }
    }

    public class BillingDetails
    {
        public int MatterArtifactId { get; set; }
        public string DateKey { get; set; }
        public int BillingDetailsArtifactId { get; set; }
    }

    public class BillingOverrides
    {
        public int MatterArtifactsId { get; set; }
        public decimal? RVWH_O { get; set; }
        public decimal? RPYH_O { get; set; }
        public decimal? RVWP_O { get; set; }
        public decimal? RPYP_O { get; set; }
        public decimal? TU_O { get; set; }
        public decimal? PU_O { get; set; }
        public decimal? U_O { get; set; }
        public decimal? CS_O { get; set; }
        public decimal? ARU_O { get; set; }
        public decimal? APU_O { get; set; }
    }    
}

