﻿using StackExchange.Exceptional;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Jil;
using StackExchange.Utils;
using Microsoft.Net.Http.Headers;

namespace Opserver.Data.Jira
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

        public async Task<JiraCreateIssueResponse> CreateIssueAsync(JiraAction action, Error error, string accountName)
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
                ["project"] = new { key = projectKey },
                ["issuetype"] = new { name = action.Name },
                ["summary"] = error.Message.CleanCRLF().TruncateWithEllipsis(255),
                ["description"] = RenderDescription(error, accountName)
            };
            var components = action.GetComponentsForApplication(error.ApplicationName);

            if (components?.Count > 0)
                fields.Add("components", components);

            var labels = action.Labels.IsNullOrEmpty()
                ? null
                : action.Labels.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (labels?.Length > 0)
                fields.Add("labels", labels);

            var payload = new { fields };

            var result = await client.PostAsync<JiraCreateIssueResponse, object>("issue", payload);

            var commentBody = RenderVariableTable("Server Variables", error.ServerVariables)
                + RenderVariableTable("QueryString", error.QueryString)
                + RenderVariableTable("Form", error.Form)
                + RenderVariableTable("Cookies", error.Cookies)
                + RenderVariableTable("RequestHeaders", error.RequestHeaders);

            if (commentBody.HasValue())
            {
                await CommentAsync(action, result, commentBody);
            }

            result.Host = GetHost(action);
            return result;
        }

        public async Task<string> CommentAsync(JiraAction actions, JiraCreateIssueResponse createResponse, string comment)
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

            var response = await client.PostAsync<string, object>(resource, payload);
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

        private static string RenderVariableTable(string title, NameValueCollection vars)
        {
            if (vars == null || vars.Count == 0)
            {
                return string.Empty;
            }
            static bool isHidden(string k) => DefaultHttpKeys.Contains(k);
            var allKeys = vars.AllKeys.Where(key => !HiddenHttpKeys.Contains(key) && vars[key].HasValue()).OrderBy(k => k);

            var sb = StringBuilderCache.Get();
            sb.Append("h3.").AppendLine(title);
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
            return sb.ToStringRecycle();
        }

        private static string RenderDescription(Error error, string accountName)
        {
            var sb = StringBuilderCache.Get();
            sb.AppendLine("{noformat}");
            if (accountName.HasValue())
            {
                sb.AppendFormat("Reporter Account Name: {0}\r\n", accountName);
            }
            sb.AppendFormat("Error Guid: {0}\r\n", error.GUID.ToString());
            sb.AppendFormat("App. Name: {0}\r\n", error.ApplicationName);
            sb.AppendFormat("Machine Name: {0}\r\n", error.MachineName);
            sb.AppendFormat("Host: {0}\r\n", error.Host);
            sb.AppendFormat("Created On (UTC): {0}\r\n", error.CreationDate.ToString(CultureInfo.CurrentCulture));
            sb.AppendFormat("Url: {0}\r\n", error.FullUrl);
            sb.AppendFormat("HTTP Method: {0}\r\n", error.HTTPMethod);
            sb.AppendFormat("IP Address: {0}\r\n", error.IPAddress);
            sb.AppendFormat("Count: {0}\r\n", error.DuplicateCount.ToString());

            sb.AppendLine("{noformat}");
            sb.AppendLine("{noformat}");
            sb.AppendLine(error.Detail);
            sb.AppendLine("{noformat}");

            return sb.ToStringRecycle();
        }

        public class JiraCreateIssueResponse
        {
            [DataMember(Name = "id")]
            public string Id { get; set; }
            [DataMember(Name = "key")]
            public string Key { get; set; }
            [DataMember(Name = "self")]
            public string Self { get; set; }

            public string Host { get; set; }

            public string BrowseUrl => Host.IsNullOrEmpty() || Key.IsNullOrEmpty()
                ? string.Empty
                : $"{Host.TrimEnd(StringSplits.ForwardSlash)}/browse/{Key}";
        }
    }

    public class JsonRestClient
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public string BaseUrl { get; set; }
        public JsonRestClient(string baseUrl)
        {
            if (baseUrl.IsNullOrEmpty())
                throw new TypeInitializationException("Opserver.Data.Jira.JsonService", new ApplicationException("BaseUrl is required"));

            BaseUrl = baseUrl.Trim().TrimEnd(StringSplits.ForwardSlash) + "/";
        }

        private string GetUriForResource(string resource)
        {
            if (BaseUrl.IsNullOrEmpty())
                throw new ApplicationException("Base url is null or empty");

            if (string.IsNullOrWhiteSpace(resource))
                return BaseUrl;

            return BaseUrl + resource.Trim().TrimStart(StringSplits.ForwardSlash);
        }

        private string GetBasicAuthzValue()
        {
            if (Username.IsNullOrEmpty())
                return null;

            var enc = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Username}:{Password}"));
            return $"Basic {enc}";
        }

        public async Task<TResponse> GetAsync<TResponse>(string resource)
        {
            var uri = GetUriForResource(resource);
            var request = Http.Request(uri)
                              .AddHeader(HeaderNames.Accept, "application/json");

            if (GetBasicAuthzValue() is string authz)
                request.AddHeader(HeaderNames.Authorization, authz);

            var result = await request.ExpectJson<TResponse>()
                                      .GetAsync();
            return result.Data;
        }

        public async Task<TResponse> PostAsync<TResponse, TData>(string resource, TData data) where TResponse : class
        {
            var uri = GetUriForResource(resource);
            var request = Http.Request(uri)
                              .AddHeader(HeaderNames.ContentType, "application/json");

            if (GetBasicAuthzValue() is string authz)
                request.AddHeader(HeaderNames.Authorization, authz);

            request.SendJson(data);

            if (typeof(TResponse) == typeof(string))
                return await request.ExpectString().PostAsync() as TResponse;

            return (await request.ExpectJson<TResponse>().PostAsync()).Data;
        }
    }
}
