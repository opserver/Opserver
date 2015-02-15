using Newtonsoft.Json.Linq;
using RestSharp;
using StackExchange.Exceptional;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
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
        public JiraCreateIssueResponse CreateIssue(JiraIssue issue, Error error)
        {

            var url = GetUrl(issue);
            var userName = GetUsername(issue);
            var password = GetPassword(issue);
            var projectKey = GetProjectKey(issue);

            var client = new RestClient();
            client.BaseUrl = new Uri(url);
            client.Authenticator = new HttpBasicAuthenticator(userName, password);

            Dictionary<string, object> fields = new Dictionary<string, object>();
            fields.Add("project", new { key = projectKey });
            fields.Add("issuetype", new { name = issue.Name });
            fields.Add("summary", error.Message);
            fields.Add("description", RenderDescription(error));
            var components = issue.GetComponentsForApplication(error.ApplicationName);

            if(components != null && components.Count > 0)
                fields.Add("components", components);

            var labels = String.IsNullOrWhiteSpace(issue.Labels) 
                ? null 
                : issue.Labels.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if(labels != null && labels.Length > 0)
                fields.Add("labels", labels);

             //new
             //   {
             //       project = new { key = projectKey },
             //       issuetype = new { name = issue.Name },
             //       summary = error.Message,
             //       description = RenderDescription(error),
             //       components = issue.GetComponentsForApplication(error.ApplicationName),
             //       labels = String.IsNullOrWhiteSpace(issue.Labels) ? null : issue.Labels.Split(new char[1]{','},StringSplitOptions.RemoveEmptyEntries)
             //   }


            var payload = new
            {
                fields = fields
            };
 
            
            var request = new RestRequest(Method.POST);
           
            request.Resource = "issue";
            request.RequestFormat = DataFormat.Json;
            request.AddBody(payload);

            var result = client.Post<JiraCreateIssueResponse>(request);
            
            var commentBody = RenderVariableTable("Server Variables", error.ServerVariables) 
                + RenderVariableTable("QueryString", error.QueryString)
                + RenderVariableTable("Form", error.Form)
                + RenderVariableTable("Cookies", error.Cookies)
                + RenderVariableTable("RequestHeaders", error.RequestHeaders);

            Comment(issue, result.Data, commentBody);
            result.Data.Host = GetHost(issue);
            return result.Data;
        }

        public IRestResponse Comment(JiraIssue issue, JiraCreateIssueResponse createResponse, string comment)
        {
            var url = GetUrl(issue);
            var userName = GetUsername(issue);
            var password = GetPassword(issue);
            
            var client = new RestClient();
            client.BaseUrl = new Uri(url);
            client.Authenticator = new HttpBasicAuthenticator(userName, password);

            var payload = new
            {
                body = comment
            };

            var request = new RestRequest(Method.POST);
            request.Resource = String.Format("issue/{0}/comment",createResponse.Key);
        
            request.RequestFormat = DataFormat.Json;
            request.AddBody(payload);
            var response = client.Post(request); 
            return response;
        }

        private string GetPassword(JiraIssue issue)
        {
            var password = !String.IsNullOrWhiteSpace(issue.Password)
                ? issue.Password
                : _jiraSettings.DefaultPassword;
            return password;
        }

        private string GetUsername(JiraIssue issue)
        {
            var userName = !String.IsNullOrWhiteSpace(issue.Username)
               ? issue.Username
               : _jiraSettings.DefaultUsername;
            return userName;
        }

        private string GetProjectKey(JiraIssue issue)
        {
            var userName = !String.IsNullOrWhiteSpace(issue.ProjectKey)
               ? issue.ProjectKey
               : _jiraSettings.DefaultProjectKey;
            return userName;
        }


        private string GetUrl(JiraIssue issue)
        {
            return !String.IsNullOrWhiteSpace(issue.Url)
                ? issue.Url
                : _jiraSettings.DefaultUrl;
        }

        private string GetHost(JiraIssue issue)
        {
            return !String.IsNullOrWhiteSpace(issue.Host)
                ? issue.Host
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

        private string RenderDescription(Error error)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{noformat}");
            sb.AppendFormat("App. Name: {0}\r\n", error.ApplicationName);
            sb.AppendFormat("Machine Name: {0}\r\n", error.MachineName);
            sb.AppendFormat("Host: {0}\r\n", error.Host);
            sb.AppendFormat("Created On (UTC): {0}\r\n", error.CreationDate);
            sb.AppendFormat("Url: {0}\r\n", error.Url);
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
}
