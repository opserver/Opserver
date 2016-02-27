using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.ModelBinding;
using Jil;
using StackExchange.Exceptional;
using StackExchange.Opserver.Data.Jira;

namespace StackExchange.Opserver.Data.TFS
{
    public class TfsClient 
    {
        private readonly TfsSettings tfsSettings;

        public TfsClient(TfsSettings tfsSettings)
        {
            this.tfsSettings = tfsSettings;
        }
        
        public async Task<TfsWorkItemResponse> CreateIssue(TfsAction tfsAction, Error error, string accountName)
        {

            var url = $"{tfsSettings.DefaultCollection}/{tfsSettings.DefaultProjectKey}/_apis/wit/workitems/${tfsAction.Name}?api-version={tfsSettings.ApiVersion}";

            var restClient = new JsonRestClient(tfsSettings.InstanceUrl)
            {
                Username = tfsSettings.DefaultUsername,
                Password = tfsSettings.DefaultPassword
            };
            var title = error.Message.CleanCRLF().TruncateWithEllipsis(250);
            var errorDescription = RenderDescription(error, accountName);
            var titlePayLoad = new Dictionary<string, string>
            {
                {"op", "add"},
                {"path", "/fields/System.Title"},
                { "value", title}

            };

            var descrPayLoad = new Dictionary<string, string>
            {
                {"op", "add"},
                {"path", "/fields/System.Description"},
                { "value", errorDescription }

            };

            var tagsPayLoadItem = new Dictionary<string, string>
            {
                {"op", "add"},
                {"path", "/fields/System.Tags"},
                {"value", tfsAction.Labels}
            };
                
            var payLoadArray = new object[] { titlePayLoad, descrPayLoad , tagsPayLoadItem };
            var result = await restClient.PostAsync<TfsWorkItemResponse, object>(url, payLoadArray, "application/json-patch+json").ConfigureAwait(false);
            return result;
        }

        private string RenderDescription(Error error, string accountName)
        {
            var sb = new StringBuilder();
          
            if (accountName.HasValue())
            {
                sb.AppendFormat("<div>Reporter Account Name: {0}</div>", accountName);
            }
            sb.AppendFormat("<div>Error Guid: {0}</div>", error.GUID);
            sb.AppendFormat("<div>App. Name: {0}</div>", error.ApplicationName);
            sb.AppendFormat("<div>Machine Name: {0}</div>", error.MachineName);
            sb.AppendFormat("<div>Host: {0}</div>", error.Host);
            sb.AppendFormat("<div>Created On (UTC): {0}</div>", error.CreationDate);
            sb.AppendFormat("<div>Url: {0}</div>", error.Url);
            sb.AppendFormat("<div>HTTP Method: {0}</div>", error.HTTPMethod);
            sb.AppendFormat("<div>IP Address: {0}</div>", error.IPAddress);

            sb.AppendLine("<h5>Details</h5>");
            sb.AppendLine(error.Detail);

            if (error.CustomData.Any())
            {
                sb.AppendLine("<h5>Custom data</h5>");
                foreach (var customErrorData in error.CustomData)
                {
                    sb.AppendLine("<div>" + customErrorData.Key + ": " + customErrorData.Value+"</div>");
                }
            }
            return sb.ToString();
        }
    }
    public class TfsWorkItemResponse
    {
        [JilDirective(Name = "id")]
        public int Id { get; set; }

        [JilDirective(Name = "_links")]
        public Links Links { get; set; }

        [JilDirective(Name = "url")]
        public string Url { get; set; }
    }
    public class Links
    {
        [JilDirective(Name = "html")]
        public LinkItem Html { get; set; }


    }
    public class LinkItem
    {
        [JilDirective(Name = "href")]
        public string Href { get; set; }
    }

}
