using StackExchange.Exceptional;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Jil;

namespace StackExchange.Opserver.Data.Jira
{
    public class JiraClient
    {
        private readonly JiraSettings _jiraSettings;
        private static readonly HashSet<string> HiddenHttpKeys = new HashSet<string>
            {
                "ALL_HTTP",
                "ALL_RAW",
                "HTTP_CONTENT_LENGTH",
                "HTTP_CONTENT_TYPE",
                "HTTP_COOKIE",
                "QUERY_STRING"
            };

        private static readonly HashSet<string> DefaultHttpKeys = new HashSet<string>
        {
            "APPL_MD_PATH",
            "APPL_PHYSICAL_PATH",
            "GATEWAY_INTERFACE",
            "HTTP_ACCEPT",
            "HTTP_ACCEPT_CHARSET",
            "HTTP_ACCEPT_ENCODING",
            "HTTP_ACCEPT_LANGUAGE",
            "HTTP_CONNECTION",
            "HTTP_KEEP_ALIVE",
            "HTTPS",
            "INSTANCE_ID",
            "INSTANCE_META_PATH",
            "PATH_INFO",
            "PATH_TRANSLATED",
            "REMOTE_PORT",
            "SCRIPT_NAME",
            "SERVER_NAME",
            "SERVER_PORT",
            "SERVER_PORT_SECURE",
            "SERVER_PROTOCOL",
            "SERVER_SOFTWARE"
        };

        public JiraClient(JiraSettings jiraSettings)
        {
            _jiraSettings = jiraSettings;
        }

        public async Task<JiraCreateIssueResponse> CreateIssue(JiraAction action, Error error, string accountName)
        {

            var url = GetUrl(action);
            var userName = GetUsername(action);
            var password = GetPassword(action);
            var projectKey = GetProjectKey(action);

            var client = new JsonRestClient(url)
            {
                Username = userName,
                Password = password
            };

            var fields = new Dictionary<string, object>
            {
                {"project", new {key = projectKey}},
                {"issuetype", new {name = action.Name}},
                {"summary", error.Message.CleanCRLF().TruncateWithEllipsis(255)},
                {"description", RenderDescription(error, accountName)}
            };
            var components = action.GetComponentsForApplication(error.ApplicationName);

            if (components != null && components.Count > 0)
                fields.Add("components", components);

            var labels = action.Labels.IsNullOrEmpty()
                ? null
                : action.Labels.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (labels != null && labels.Length > 0)
                fields.Add("labels", labels);

            var payload = new { fields };

            var result = await client.PostAsync<JiraCreateIssueResponse, object>("issue", payload).ConfigureAwait(false);

            var commentBody = RenderVariableTable("Server Variables", error.ServerVariables)
                + RenderVariableTable("QueryString", error.QueryString)
                + RenderVariableTable("Form", error.Form)
                + RenderVariableTable("Cookies", error.Cookies)
                + RenderVariableTable("RequestHeaders", error.RequestHeaders);

            if (commentBody.HasValue())
            {
                await Comment(action, result, commentBody).ConfigureAwait(false);
            }
            
            result.Host = GetHost(action);
            return result;
        }

        public async Task<string> Comment(JiraAction actions, JiraCreateIssueResponse createResponse, string comment)
        {
            var url = GetUrl(actions);
            var userName = GetUsername(actions);
            var password = GetPassword(actions);

            var client = new JsonRestClient(url)
            {
                Username = userName,
                Password = password
            };

            var payload = new
            {
                body = comment
            };

            var resource = $"issue/{createResponse.Key}/comment";

            var response = await client.PostAsync<string, object>(resource, payload).ConfigureAwait(false);
            return response;
        }


        private string GetPassword(JiraAction action)
        {
            return action.Password.IsNullOrEmptyReturn(_jiraSettings.DefaultPassword);
        }

        private string GetUsername(JiraAction action)
        {
            return action.Username.IsNullOrEmptyReturn(_jiraSettings.DefaultUsername);
        }

        private string GetProjectKey(JiraAction action)
        {
            return action.ProjectKey.IsNullOrEmptyReturn(_jiraSettings.DefaultProjectKey);
        }

        private string GetUrl(JiraAction action)
        {
            return action.Url.IsNullOrEmptyReturn(_jiraSettings.DefaultUrl);
        }

        private string GetHost(JiraAction action)
        {
            return action.Host.IsNullOrEmptyReturn(_jiraSettings.DefaultHost);
        }

        private string RenderVariableTable(string title, NameValueCollection vars)
        {
            if (vars == null || vars.Count == 0)
            {
                return string.Empty;
            }
            Func<string, bool> isHidden = k => DefaultHttpKeys.Contains(k);
            var allKeys = vars.AllKeys.Where(key => !HiddenHttpKeys.Contains(key) && vars[key].HasValue()).OrderBy(k => k);

            var sb = new StringBuilder();
            sb.AppendLine("h3." + title);
            sb.AppendLine("{noformat}");
            foreach (var k in allKeys.Where(k => !isHidden(k)))
            {
                sb.AppendFormat("{0}: {1}\r\n", k, vars[k]);
            }
            if (vars["HTTP_HOST"].HasValue() && vars["URL"].HasValue())
            {
                var ssl = vars["HTTP_X_FORWARDED_PROTO"] == "https" || vars["HTTP_X_SSL"].HasValue() || vars["HTTPS"] == "on";
                var url = string.Format("http{3}://{0}{1}{2}", vars["HTTP_HOST"], vars["URL"], vars["QUERY_STRING"].HasValue() ? "?" + vars["QUERY_STRING"] : "", ssl ? "s" : "");

                sb.AppendFormat("Request and URL: {0}\r\n", url);
            }

            sb.AppendLine("{noformat}");
            return sb.ToString();
        }

        private string RenderDescription(Error error, string accountName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{noformat}");
            if (accountName.HasValue())
            {
                sb.AppendFormat("Reporter Account Name: {0}\r\n", accountName);
            }
            sb.AppendFormat("Error Guid: {0}\r\n", error.GUID);
            sb.AppendFormat("App. Name: {0}\r\n", error.ApplicationName);
            sb.AppendFormat("Machine Name: {0}\r\n", error.MachineName);
            sb.AppendFormat("Host: {0}\r\n", error.Host);
            sb.AppendFormat("Created On (UTC): {0}\r\n", error.CreationDate);
            sb.AppendFormat("Url: {0}\r\n", error.Url);
            sb.AppendFormat("HTTP Method: {0}\r\n", error.HTTPMethod);
            sb.AppendFormat("IP Address: {0}\r\n", error.IPAddress);
            sb.AppendFormat("Count: {0}\r\n", error.DuplicateCount);

            sb.AppendLine("{noformat}");
            sb.AppendLine("{noformat}");
            sb.AppendLine(error.Detail);
            sb.AppendLine("{noformat}");

            return sb.ToString();
        }

        public class JiraCreateIssueResponse
        {
            public string Key { get; set; }
            public int Id { get; set; }

            public string Self { get; set; }

            public string Host { get; set; }

            public string BrowseUrl => Host.IsNullOrEmpty() || Key.IsNullOrEmpty()
                ? string.Empty
                : $"{Host.TrimEnd('/')}/browse/{Key}";
        }
    }

    public class JsonRestClient
    {
        private WebClient client;

        public string Username { get; set; }
        public string Password { get; set; }

        public string BaseUrl { get; set; }
        public JsonRestClient(string baseUrl)
        {
            if (baseUrl.IsNullOrEmpty())
                throw new TypeInitializationException("StackExchange.Opserver.Data.Jira.JsonService", new ApplicationException("BaseUrl is required"));

            BaseUrl = baseUrl.Trim().TrimEnd("/") + "/";
        }

        private Uri GetUriForResource(string resource)
        {
            if (BaseUrl.IsNullOrEmpty())
                throw new ApplicationException("Base url is null or empty");

            if (string.IsNullOrWhiteSpace(resource))
                return new Uri(BaseUrl);

            return new Uri(BaseUrl + resource.Trim().TrimStart('/'));
        }

        private string GetBasicAuthzValue()
        {
            if (Username.IsNullOrEmpty())
                return string.Empty;
            
            var enc = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Username}:{Password}"));
            return $"{"Basic"} {enc}";
        }

        public async Task<TResponse> GetAsync<TResponse>(string resource)
        {
            client = client ?? new WebClient();
            client.Headers.Add(HttpRequestHeader.Accept, "application/json");
            client.Encoding = Encoding.UTF8;
            var authz = GetBasicAuthzValue();
            if (authz.HasValue())
                client.Headers.Add(HttpRequestHeader.Authorization, authz);

            var uri = GetUriForResource(resource);
            var responseBytes = await client.DownloadDataTaskAsync(uri).ConfigureAwait(false);

            string response = Encoding.UTF8.GetString(responseBytes);
            return JSON.Deserialize<TResponse>(response);
        }

        public async Task<TResponse> PostAsync<TResponse, TData>(string resource, TData data) where TResponse : class
        {
            client = client ?? new WebClient();
            client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            client.Encoding = Encoding.UTF8;
            var authz = GetBasicAuthzValue();
            if (authz.HasValue())
                client.Headers.Add(HttpRequestHeader.Authorization, authz);


            var json = JSON.Serialize(data);
            byte[] dataBytes = Encoding.UTF8.GetBytes(json);
            var uri = GetUriForResource(resource);
            var responseBytes = new byte[0];
            try
            {
                responseBytes = await client.UploadDataTaskAsync(uri, "POST", dataBytes).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            string response = Encoding.UTF8.GetString(responseBytes);
            if (typeof(TResponse) == typeof(string))
                return response as TResponse;

            return JSON.Deserialize<TResponse>(response);
        }
    }
}
