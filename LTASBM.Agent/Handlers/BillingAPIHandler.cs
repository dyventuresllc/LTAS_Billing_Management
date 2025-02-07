using LTASBM.Agent.Models;
using Newtonsoft.Json;
using Relativity.API;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace LTASBM.Agent.Handlers
{
    public class BillingAPIHandler
    {
        private readonly IAPILog _logger;
        private readonly HttpClient _billingClient;
        private readonly HttpClient _usageClient;
        private readonly string _instanceId;
        private readonly string _token;
        private readonly string _instanceUrl;

        // API URLs
        private const string BILLING_API_BASE = "https://billing-insights.api.relativity.one";
        private const string MONTHLY_METRICS_ENDPOINT = "/billing-drilldown-api/api/v2/billing-insights/month-details";        
        private const string USAGE_REPORTS_PATH = "api/usage-reports/v1/reports";        
        private const string METADATA_ENDPOINT = "api/usage-reports/v1/Metadata";
        private const string DOWNLOAD_ENDPOINT = "api/usage-reports/v1/reports/download/{0}";

        public BillingAPIHandler(IAPILog logger, string instanceId, string token, string instanceUrl)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _instanceId = instanceId ?? throw new ArgumentNullException(nameof(instanceId));
            _token = token ?? throw new ArgumentNullException(nameof(token));
            _instanceUrl = instanceUrl;

            // Set up billing client
            _billingClient = new HttpClient { BaseAddress = new Uri(BILLING_API_BASE) };
            _billingClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            _billingClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);

            // Set up usage client
            var baseUrl = instanceUrl.Replace("/Relativity", "");
            _usageClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            _usageClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            _usageClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
        }

        public string GetInstanceGeoCode()
        {
            try
            {
                if (string.IsNullOrEmpty(_instanceUrl))
                    return string.Empty;
                var uri = new Uri(_instanceUrl);
                var fullRegion = uri.Host.Split('.')[0];  // Gets "qe-us", "qe-eu", or "qe-au"
                return fullRegion.Split('-')[1].ToUpper(); // Gets "US", "EU", or "AU"
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting geo code from instance URL");
                return string.Empty;
            }
        }

        public async Task<string> GetMonthlyMetricsAsync(int workspaceId)
        {
            try
            {
                var request = new
                {
                    dateKey = DateTime.Now.ToString("yyyyMM"),
                    metricKeys = new[]
                    {
                        "Relativity_Storage_Review",
                        "Relativity_Storage_Repository",
                        "Relativity_Storage_ColdStorage",
                        "Relativity_Translate",
                        "aiR_for_Review",
                        "aiR_for_Privilege"
                    },
                    selectorLevel = "Workspace",
                    itemSelectors = new[]
                    {
                        new { instanceID = _instanceId, WorkspaceArtifactID = workspaceId }
                    },
                    level = "Workspace"
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(request),
                    Encoding.UTF8,
                    "application/json");

                var response = await _billingClient.PostAsync(MONTHLY_METRICS_ENDPOINT, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Metrics call failed. Status: {response.StatusCode}, Error: {errorContent}");
                    return string.Empty;
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monthly metrics");
                return string.Empty;
            }
        }

        public async Task<UsageReportResponse> GetUsageReportAsync(string reportName, DateTime dateFrom, DateTime dateTo, params string[] fieldIds)
        {
            try
            {
                _logger.LogInformation($"Getting usage report {reportName} with {fieldIds.Length} fields");

                var request = new
                {
                    name = reportName,
                    dateRange = new
                    {
                        from = dateFrom.ToString("yyyy-MM-dd"),
                        to = dateTo.ToString("yyyy-MM-dd")
                    },
                    fields = fieldIds
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(request),
                    Encoding.UTF8,
                    "application/json");

                var response = await _usageClient.PostAsync(USAGE_REPORTS_PATH, content);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Usage report call failed. Status: {response.StatusCode}, Error: {errorContent}");
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Successfully retrieved usage report {reportName}");
                return JsonConvert.DeserializeObject<UsageReportResponse>(responseContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting usage report");
                return null;
            }
        }

        public async Task<string> GetReportMetadataAsync()
        {           
            try
            {
                var response = await _usageClient.GetAsync(METADATA_ENDPOINT);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Metadata call failed. Status: {response.StatusCode}, Error: {errorContent}");
                    return string.Empty;
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting report metadata");
                return string.Empty;
            }
        }

        public async Task<bool> WaitForReportCompletion(string reportId, int maxAttempts = 6)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                // Wait 10 seconds between checks
                await Task.Delay(10000);

                try
                {                    
                    var checkUrl = $"{USAGE_REPORTS_PATH}/{reportId}";                    
                    var response = await _usageClient.GetAsync(checkUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var reportStatus = JsonConvert.DeserializeObject<UsageReportResponse>(content);

                        if (reportStatus.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                        else
                        {
                            _logger.LogError($"Report not yet complete, status:{reportStatus.Status}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error checking report status on attempt {i + 1}");
                }
            }

            return false;
        }

        //TODO: Remove
        //public async Task<List<SecondaryData>> DownloadReportAsync(string reportId, IInstanceSettingsBundle instanceSettings)
        //{
        //    try
        //    {
        //        _logger.LogInformation($"Starting download of report {reportId}");
        //        var response = await _usageClient.GetAsync(string.Format(DOWNLOAD_ENDPOINT, reportId));

        //        if (!response.IsSuccessStatusCode)
        //        {
        //            var errorContent = await response.Content.ReadAsStringAsync();
        //            _logger.LogError($"Download report failed. Status: {response.StatusCode}, Error: {errorContent}");
        //            return null;
        //        }

        //        var content = Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync());
        //        var result = new List<SecondaryData>();

        //        using (var reader = new StringReader(content))
        //        {
        //            var headerLine = reader.ReadLine();
        //            if (string.IsNullOrEmpty(headerLine))
        //            {
        //                _logger.LogError("CSV file is empty or missing header");
        //                return null;
        //            }

        //            var columnMap = SplitCsvLine(headerLine)
        //                .Select((value, index) => new { Value = value.Trim('"'), Index = index })
        //                .ToDictionary(x => x.Value, x => x.Index, StringComparer.OrdinalIgnoreCase);

        //            int processedLines = 0, addedRecords = 0, errorLines = 0;
        //            string line;

        //            while ((line = reader.ReadLine()) != null)
        //            {
        //                processedLines++;
        //                try
        //                {
        //                    var values = SplitCsvLine(line);
        //                    var data = new SecondaryData
        //                    {
        //                        InstanceName = GetValue(values, columnMap, "Instance Name"),
        //                        WorkspaceName = GetValue(values, columnMap, "Workspace Name"),
        //                        ClientName = GetValue(values, columnMap, "Client Name"),
        //                        MatterName = GetValue(values, columnMap, "Matter Name"),
        //                        WorkspaceArtifactId = int.Parse(GetValue(values, columnMap, "Workspace ArtifactID")),
        //                        PublishedDocumentSizeGB = ParseDecimal(GetValue(values, columnMap, "Published Document Size [GB]")),
        //                        LinkedTotalFileSizeGB = ParseDecimal(GetValue(values, columnMap, "Linked Total File Size [GB]")),
        //                        PeakWorkspaceHostedSizeGB = ParseDecimal(GetValue(values, columnMap, "Peak Workspace Hosted Size [GB]")),
        //                        TranslateDocumentUnits = (int)ParseDecimal(GetValue(values, columnMap, "Translate Document Units")),
        //                        WorkspaceUtilizationCollectedAt = ParseDateTime(GetValue(values, columnMap, "Workspace Utilization Capture Date-Time")),
        //                        ProcessingStatisticsCollectedAt = ParseDateTime(GetValue(values, columnMap, "Processing Metrics Capture Date-Time")),
        //                        WorkspaceType = GetValue(values, columnMap, "Workspace Type", false),
        //                        AirForReviewDocuments = (int)ParseDecimal(GetValue(values, columnMap, "aIR for Review Documents")),
        //                        AirForPrivilegeDocuments = (int)ParseDecimal(GetValue(values, columnMap, "aIR for Privilege Documents"))
        //                    };
        //                    result.Add(data);
        //                    addedRecords++;
        //                }
        //                catch (Exception ex)
        //                {
        //                    errorLines++;
        //                    _logger.LogError($"Error parsing line {processedLines}: {ex.Message}");
        //                    await LogParsingError(line, ex, instanceSettings);
        //                }
        //            }

        //            _logger.LogInformation($"Processing complete - Total: {processedLines}, Success: {addedRecords}, Errors: {errorLines}");
        //        }

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Error downloading report {reportId}: {ex.Message}");
        //        return null;
        //    }
        //}

        public async Task<List<SecondaryData>> DownloadReportAsync(string reportId, IInstanceSettingsBundle instanceSettings)
        {
            try
            {
                _logger.LogInformation($"Starting download of report {reportId}");
                var response = await _usageClient.GetAsync(string.Format(DOWNLOAD_ENDPOINT, reportId));

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Download report failed. Status: {response.StatusCode}, Error: {errorContent}");
                    return null;
                }

                var bytes = await response.Content.ReadAsByteArrayAsync();
                _logger.LogInformation($"Downloaded content size: {bytes.Length:N0} bytes");

                var content = Encoding.UTF8.GetString(bytes);
                var result = new List<SecondaryData>();

                using (var reader = new StringReader(content))
                {
                    var headerLine = reader.ReadLine();
                    if (string.IsNullOrEmpty(headerLine))
                    {
                        _logger.LogError("CSV file is empty or missing header");
                        return null;
                    }

                    _logger.LogInformation($"Processing CSV header: {headerLine}");
                    var columnMap = SplitCsvLine(headerLine)
                        .Select((value, index) => new { Value = value.Trim('"'), Index = index })
                        .ToDictionary(x => x.Value, x => x.Index, StringComparer.OrdinalIgnoreCase);

                    // Validate required columns are present
                    var requiredColumns = new[]
                    {
                        "Instance Name", "Workspace Name", "Client Name", "Matter Name",
                        "Workspace ArtifactID", "Published Document Size [GB]",
                        "Linked Total File Size [GB]", "Peak Workspace Hosted Size [GB]",
                        "Translate Document Units", "Workspace Type"
                    };

                    var missingColumns = requiredColumns.Where(col => !columnMap.ContainsKey(col)).ToList();
                    if (missingColumns.Any())
                    {
                        _logger.LogError($"Missing required columns: {string.Join(", ", missingColumns)}");
                        return null;
                    }

                    _logger.LogInformation($"Found {columnMap.Count} columns in CSV");

                    int processedLines = 0, addedRecords = 0, errorLines = 0;
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        processedLines++;
                        try
                        {
                            var values = SplitCsvLine(line);
                            if (values.Length < columnMap.Count)
                            {
                                _logger.LogWarning($"Line {processedLines} has fewer columns than expected: {values.Length} vs {columnMap.Count}");
                                continue;
                            }

                            var data = new SecondaryData
                            {
                                InstanceName = GetValue(values, columnMap, "Instance Name"),
                                WorkspaceName = GetValue(values, columnMap, "Workspace Name"),
                                ClientName = GetValue(values, columnMap, "Client Name"),
                                MatterName = GetValue(values, columnMap, "Matter Name"),
                                WorkspaceArtifactId = int.Parse(GetValue(values, columnMap, "Workspace ArtifactID")),
                                PublishedDocumentSizeGB = ParseDecimal(GetValue(values, columnMap, "Published Document Size [GB]")),
                                LinkedTotalFileSizeGB = ParseDecimal(GetValue(values, columnMap, "Linked Total File Size [GB]")),
                                PeakWorkspaceHostedSizeGB = ParseDecimal(GetValue(values, columnMap, "Peak Workspace Hosted Size [GB]")),
                                TranslateDocumentUnits = (int)ParseDecimal(GetValue(values, columnMap, "Translate Document Units")),
                                WorkspaceUtilizationCollectedAt = ParseDateTime(GetValue(values, columnMap, "Workspace Utilization Capture Date-Time")),
                                ProcessingStatisticsCollectedAt = ParseDateTime(GetValue(values, columnMap, "Processing Metrics Capture Date-Time")),
                                WorkspaceType = GetValue(values, columnMap, "Workspace Type"),
                                AirForReviewDocuments = (int)ParseDecimal(GetValue(values, columnMap, "aiR for Review Documents")),
                                AirForPrivilegeDocuments = (int)ParseDecimal(GetValue(values, columnMap, "aiR for Privilege Documents"))
                            };

                            result.Add(data);
                            addedRecords++;

                            if (processedLines % 1000 == 0)
                            {
                                _logger.LogInformation($"Processed {processedLines:N0} lines...");
                            }
                        }
                        catch (Exception ex)
                        {
                            errorLines++;
                            _logger.LogError($"Error parsing line {processedLines}: {ex.Message}");
                            await LogParsingError(line, ex, instanceSettings);
                        }
                    }

                    _logger.LogInformation($"Report processing complete:" +
                        $"\nTotal lines processed: {processedLines:N0}" +
                        $"\nSuccessfully added records: {addedRecords:N0}" +
                        $"\nError lines: {errorLines:N0}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error downloading report {reportId}: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                return null;
            }
        }

        private string GetValue(string[] values, Dictionary<string, int> columnMap, string columnName, bool required = true)
        {
            if (columnMap.TryGetValue(columnName, out int index) && index < values.Length)
                return values[index].Trim('"');

            if (required)
                throw new Exception($"Required column {columnName} not found or value missing");

            return null;
        }

        private DateTime? ParseDateTime(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Equals("N/A", StringComparison.OrdinalIgnoreCase))
                return null;

            return DateTime.ParseExact(value, "MM/dd/yyyy hh:mm tt", CultureInfo.InvariantCulture);
        }

        private decimal ParseDecimal(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Equals("N/A", StringComparison.OrdinalIgnoreCase))
                return 0;

            return decimal.Parse(value);
        }

        private async Task LogParsingError(string line, Exception ex, IInstanceSettingsBundle instanceSettings)
        {
            var values = line.Split(',').Select(v => v.Trim()).ToArray();
            var sb = new StringBuilder()
                .AppendLine("<table border='1' style='border-collapse: collapse; width: 100%;'>")
                .AppendLine("<tr style='background-color: #f2f2f2;'>")
                .AppendLine("<th>Error Type</th><th>Message</th></tr>")
                .AppendLine($"<tr><td>Error</td><td>{ex.Message}</td></tr>");

            if (ex.InnerException != null)
                sb.AppendLine($"<tr><td>Inner Error</td><td>{ex.InnerException.Message}</td></tr>");

            sb.AppendLine("</table>")
              .AppendLine("<br/><strong>Raw Line:</strong>")
              .AppendLine($"<pre>{line}</pre>");

            await MessageHandler.Email.SendInternalNotificationAsync(
                instanceSettings,
                sb,
                "Error Processing Report Line"
            );
        }           

        private string[] SplitCsvLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            StringBuilder field = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (line[i] == ',' && !inQuotes)
                {
                    result.Add(field.ToString());
                    field.Clear();
                }
                else
                {
                    field.Append(line[i]);
                }
            }
            result.Add(field.ToString());

            return result.ToArray();
        }
    }
}