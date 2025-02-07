using LTASBM.Agent.Logging;
using Relativity.API;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LTASBM.Agent.Models.Metadata
{
    public class MetadataValidator
    {
        private readonly ILTASLogger _logger;
        private readonly MetadataFields _metadataFields;

        public MetadataValidator(MetadataFields metadataFields, IAPILog relativityLogger, IHelper helper)
        {
            _metadataFields = metadataFields;
            _logger = LoggerFactory.CreateLogger<MetadataValidator>(helper.GetDBContext(-1), helper, relativityLogger);
        }

        public bool PopulateMetadataFields(MetadataResponse metadata)
        {
            try
            {
                // Log all available origins and metrics
                foreach (var origin in metadata.Origins)
                {
                    _logger.LogInformation($"Origin: {origin.Name} (Metrics: {origin.Metrics?.Count ?? 0})");
                    if (origin.Metrics != null)
                    {
                        foreach (var metric in origin.Metrics)
                        {
                            _logger.LogInformation($"  - {metric.Name} (ID: {metric.Id})");
                            if (!Guid.TryParse(metric.Id, out _))
                            {
                                _logger.LogWarning($"Invalid GUID format for metric {metric.Name}: {metric.Id}");
                            }
                        }
                    }
                }

                var processingOrigin = metadata.Origins.FirstOrDefault(o => o.Name == "Processing Statistics");
                var workspaceOrigin = metadata.Origins.FirstOrDefault(o => o.Name == "Workspace Utilization");
                var productOrigin = metadata.Origins.FirstOrDefault(o => o.Name == "Product Utilization");

                _logger.LogInformation($"Origin status - Processing: {processingOrigin != null}, " +
                                     $"Workspace: {workspaceOrigin != null}, " +
                                     $"Product: {productOrigin != null}");

                if (processingOrigin == null || workspaceOrigin == null || productOrigin == null)
                {
                    _logger.LogError("Required origins missing");
                    return false;
                }

                var previousFields = new Dictionary<string, string>
                {
                    { "WorkspaceArtifactId", _metadataFields.WorkspaceArtifactId },
                    { "PublishedDocumentSizeId", _metadataFields.PublishedDocumentSizeId },
                    { "PeakWorkspaceHostedSizeId", _metadataFields.PeakWorkspaceHostedSizeId },
                    { "LinkedTotalFileSizeId", _metadataFields.LinkedTotalFileSizeId },
                    { "TranslateDocumentUnitsId", _metadataFields.TranslateDocumentUnitsId },
                    { "AirForReviewDocumentsId", _metadataFields.AirForReviewDocumentsId },
                    { "AirForPrivilegeDocumentsId", _metadataFields.AirForPrivilegeDocumentsId },
                    { "WorkspaceTypeId", _metadataFields.WorkspaceTypeId }
                };

                _metadataFields.WorkspaceArtifactId = workspaceOrigin.Metrics
                    ?.FirstOrDefault(m => m.Name == "Workspace ArtifactID")?.Id;
                _metadataFields.PublishedDocumentSizeId = processingOrigin.Metrics
                    ?.FirstOrDefault(m => m.Name == "Published Document Size [GB]")?.Id;
                _metadataFields.PeakWorkspaceHostedSizeId = workspaceOrigin.Metrics
                    ?.FirstOrDefault(m => m.Name == "Peak Workspace Hosted Size [GB]")?.Id;                
                _metadataFields.LinkedTotalFileSizeId = workspaceOrigin.Metrics
                    ?.FirstOrDefault(m => m.Name == "Linked Total File Size [GB]")?.Id;
                _metadataFields.TranslateDocumentUnitsId = productOrigin?.Metrics
                    ?.FirstOrDefault(m => m.Name == "Translate Document Units")?.Id;
                _metadataFields.AirForReviewDocumentsId = productOrigin.Metrics
                    ?.FirstOrDefault(m => m.Name == "aiR for Review Documents")?.Id;                                                   
                _metadataFields.AirForPrivilegeDocumentsId = productOrigin.Metrics
                    ?.FirstOrDefault(m => m.Name == "aiR for Privilege Documents")?.Id;
                _metadataFields.WorkspaceTypeId = workspaceOrigin.Metrics
                    ?.FirstOrDefault(m => m.Name == "Workspace Type")?.Id;

                // Log field changes
                foreach (var field in previousFields)
                {
                    var currentValue = typeof(MetadataFields).GetProperty(field.Key).GetValue(_metadataFields) as string;
                    _logger.LogInformation($"{field.Key} changed: {field.Value} -> {currentValue}");
                }

                var allFieldsFound = _metadataFields.ValidateRequiredFields();
                if (!allFieldsFound)
                {
                    var requiredFields = new Dictionary<string, string>
                    {
                        { "Workspace ArtifactID", _metadataFields.WorkspaceArtifactId },
                        { "Published Document Size [GB]", _metadataFields.PublishedDocumentSizeId },
                        { "Peak Workspace Hosted Size [GB]", _metadataFields.PeakWorkspaceHostedSizeId },
                        { "Linked Total File Size [GB]", _metadataFields.LinkedTotalFileSizeId },
                        { "Translate Document Units", _metadataFields.TranslateDocumentUnitsId },
                        { "aIR for Review Documents", _metadataFields.AirForReviewDocumentsId },
                        { "aIR for Privilege Documents", _metadataFields.AirForPrivilegeDocumentsId },
                        { "Workspace Type", _metadataFields.WorkspaceTypeId }
                    };

                    var missingFields = requiredFields
                        .Where(f => string.IsNullOrEmpty(f.Value) || !Guid.TryParse(f.Value, out _))
                        .Select(f => $"{f.Key}: {(string.IsNullOrEmpty(f.Value) ? "Missing" : $"Invalid GUID: {f.Value}")}");

                    _logger.LogError($"Missing or invalid fields: {string.Join(", ", missingFields)}");
                    return false;
                }

                _logger.LogInformation("All required fields found and validated");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error populating metadata fields: {ex.Message}");
                return false;
            }
        }
    }
}