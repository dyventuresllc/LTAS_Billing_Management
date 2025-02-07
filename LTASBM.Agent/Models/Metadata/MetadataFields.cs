using System;
using System.Collections.Generic;
using System.Linq;

namespace LTASBM.Agent.Models.Metadata
{
    public class MetadataFields
    {
        public string WorkspaceArtifactId { get; set; }
        public string PublishedDocumentSizeId { get; set; }
        public string PeakWorkspaceHostedSizeId { get; set; }
        public string LinkedTotalFileSizeId { get; set; }
        public string TranslateDocumentUnitsId { get; set; }
        public string AirForReviewDocumentsId { get; set; }
        public string AirForPrivilegeDocumentsId { get; set; }
        public string WorkspaceTypeId { get; set; }

        public bool ValidateRequiredFields()
        {
            var requiredFields = new Dictionary<string, string>
            {
                { "Workspace ArtifactID", WorkspaceArtifactId },
                { "Published Document Size [GB]", PublishedDocumentSizeId },
                { "Peak Workspace Hosted Size [GB]", PeakWorkspaceHostedSizeId },
                { "Linked Total File Size [GB]", LinkedTotalFileSizeId },
                { "Translate Document Units", TranslateDocumentUnitsId },
                { "aIR for Review Documents", AirForReviewDocumentsId },
                { "aIR for Privilege Documents", AirForPrivilegeDocumentsId },
                { "Workspace Type", WorkspaceTypeId }
            };

            var missingFields = new List<string>();
            foreach (var field in requiredFields)
            {
                if (string.IsNullOrEmpty(field.Value))
                {
                    missingFields.Add(field.Key);
                }
                else if (!Guid.TryParse(field.Value, out _))
                {
                    missingFields.Add($"{field.Key} (Invalid GUID: {field.Value})");
                }
            }

            return !missingFields.Any();
        }
    }
}