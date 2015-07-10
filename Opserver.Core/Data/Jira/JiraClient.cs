using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Exceptional;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Data.Jira
{
    public class JiraClient
    {
        private JiraSettings _jiraSettings;
        private static HashSet<string> hiddenHttpKeys;
        private static HashSet<string> defaultHttpKeys;

        static JiraClient()
        {
            PrepareHttpKeys();
        }

        public JiraClient(JiraSettings jiraSettings)
        {
            _jiraSettings = jiraSettings;
        }

        private static void PrepareHttpKeys()
        {
            hiddenHttpKeys = new HashSet<string>
            {
                "ALL_HTTP",
                "ALL_RAW",
                "HTTP_CONTENT_LENGTH",
                "HTTP_CONTENT_TYPE",
                "HTTP_COOKIE",
                "QUERY_STRING"
            };

            defaultHttpKeys = new HashSet<string>
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
        }

        public async Task<JiraCreateIssueResponse> CreateIssue(JiraAction action, Error error, string accountName)
        {

            var url = GetUrl(action);
            var userName = GetUsername(action);
            var password = GetPassword(action);
            var projectKey = GetProjectKey(action);

            var client = new JsonRestClient(url);
            client.Username = userName;
            client.Password = password;

            Dictionary<string, object> fields = new Dictionary<string, object>();
            fields.Add("project", new { key = projectKey });
            fields.Add("issuetype", new { name = action.Name });
            fields.Add("summary", error.Message.CleanCRLF().TruncateWithEllipsis(255));
            fields.Add("description", RenderDescription(error, accountName));
            var components = action.GetComponentsForApplication(error.ApplicationName);

            if (components != null && components.Count > 0)
                fields.Add("components", components);

            var labels = String.IsNullOrWhiteSpace(action.Labels)
                ? null
                : action.Labels.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (labels != null && labels.Length > 0)
                fields.Add("labels", labels);

            var payload = new
            {
                fields = fields
            };

            var result = await client.PostAsync<JiraCreateIssueResponse, object>("issue", payload).ConfigureAwait(false);

            var commentBody = RenderVariableTable("Server Variables", error.ServerVariables)
                + RenderVariableTable("QueryString", error.QueryString)
                + RenderVariableTable("Form", error.Form)
                + RenderVariableTable("Cookies", error.Cookies)
                + RenderVariableTable("RequestHeaders", error.RequestHeaders);

            if (!String.IsNullOrWhiteSpace(commentBody))
            {
                var commentReponse = await Comment(action, result, commentBody).ConfigureAwait(false);
            }
            
            result.Host = GetHost(action);
            return result;
        }

        public async Task<string> Comment(JiraAction actions, JiraCreateIssueResponse createResponse, string comment)
        {
            var url = GetUrl(actions);
            var userName = GetUsername(actions);
            var password = GetPassword(actions);

            var client = new JsonRestClient(url);
            client.Username = userName;
            client.Password = password;

            var payload = new
            {
                body = comment
            };

            var resource = String.Format("issue/{0}/comment", createResponse.Key);

            var response = await client.PostAsync<string, object>(resource, payload).ConfigureAwait(false);
            return response;
        }


        private string GetPassword(JiraAction action)
        {
            var password = !String.IsNullOrWhiteSpace(action.Password)
                ? action.Password
                : _jiraSettings.DefaultPassword;
            return password;
        }

        private string GetUsername(JiraAction action)
        {
            var userName = !String.IsNullOrWhiteSpace(action.Username)
               ? action.Username
               : _jiraSettings.DefaultUsername;
            return userName;
        }

        private string GetProjectKey(JiraAction action)
        {
            var userName = !String.IsNullOrWhiteSpace(action.ProjectKey)
               ? action.ProjectKey
               : _jiraSettings.DefaultProjectKey;
            return userName;
        }


        private string GetUrl(JiraAction action)
        {
            return !String.IsNullOrWhiteSpace(action.Url)
                ? action.Url
                : _jiraSettings.DefaultUrl;
        }

        private string GetHost(JiraAction action)
        {
            return !String.IsNullOrWhiteSpace(action.Host)
                ? action.Host
                : _jiraSettings.DefaultHost;
        }

        private string RenderVariableTable(string title, NameValueCollection vars)
        {
            if (vars == null || vars.Count == 0)
            {
                return String.Empty;
            }
            Func<string, bool> isHidden = k => defaultHttpKeys.Contains(k);
            var allKeys = vars.AllKeys.Where(key => !hiddenHttpKeys.Contains(key) && vars[key].HasValue()).OrderBy(k => k);

            StringBuilder sb = new StringBuilder();
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
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{noformat}");
            if (!String.IsNullOrWhiteSpace(accountName))
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

            public string BrowseUrl
            {
                get
                {
                    if (String.IsNullOrWhiteSpace(Host) || String.IsNullOrWhiteSpace(Key))
                        return String.Empty;
                    else
                        return String.Format("{0}/browse/{1}", Host.TrimEnd('/'), Key);
                }
            }
        }
    }

    public class JsonRestClient
    {
        private WebClient client = null;

        public string Username { get; set; }
        public string Password { get; set; }

        public string BaseUrl { get; set; }
        public JsonRestClient(string baseUrl)
        {
            if (String.IsNullOrWhiteSpace(baseUrl))
                throw new TypeInitializationException("StackExchange.Opserver.Data.Jira.JsonService", new ApplicationException("BaseUrl is required"));

            BaseUrl = baseUrl.Trim().TrimEnd("/") + "/";
        }

        private Uri GetUriForResource(string resource)
        {
            if (String.IsNullOrWhiteSpace(BaseUrl))
                throw new ApplicationException("Base url is null or empty");

            if (String.IsNullOrWhiteSpace(resource))
                return new Uri(BaseUrl);

            return new Uri(BaseUrl + resource.Trim().TrimStart('/'));
        }

        private string GetBasicAuthzValue()
        {
            if (String.IsNullOrWhiteSpace(Username))
                return String.Empty;

            string _auth = string.Format("{0}:{1}", Username, Password);
            string _enc = Convert.ToBase64String(Encoding.ASCII.GetBytes(_auth));
            return string.Format("{0} {1}", "Basic", _enc);
        }

        public async Task<TResponse> GetAsync<TResponse>(string resource)
        {
            if (client == null)
                client = new WebClient();

            client.Headers.Add(HttpRequestHeader.Accept, "application/json");
            client.Encoding = System.Text.Encoding.UTF8;
            var authz = GetBasicAuthzValue();
            if (!String.IsNullOrWhiteSpace(authz))
                client.Headers.Add(HttpRequestHeader.Authorization, authz);

            var uri = GetUriForResource(resource);
            var responseBytes = new byte[0];
            try
            {
                responseBytes = await client.DownloadDataTaskAsync(uri).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            string response = Encoding.UTF8.GetString(responseBytes);
            return JsonConvert.DeserializeObject<TResponse>(response);
        }

        public async Task<TResponse> PostAsync<TResponse, TData>(string resource, TData data) where TResponse : class
        {
            if (client == null)
                client = new WebClient();

            client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            client.Encoding = System.Text.Encoding.UTF8;
            var authz = GetBasicAuthzValue();
            if (!String.IsNullOrWhiteSpace(authz))
                client.Headers.Add(HttpRequestHeader.Authorization, authz);


            var json = JsonConvert.SerializeObject(data);
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
            if (typeof(TResponse) == typeof(String))
                return response as TResponse;

            return JsonConvert.DeserializeObject<TResponse>(response);
        }
    }

}
