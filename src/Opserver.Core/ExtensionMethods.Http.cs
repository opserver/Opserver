using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using StackExchange.Utils;

namespace Opserver
{
    public static partial class ExtensionMethods
    {
        /// <summary>
        /// Adds a <see cref="NameValueCollection"/> as the body for this request, with a content type of
        /// <c>application/x-www-form-urlencoded</c>.
        /// </summary>
        /// <remarks>
        /// This extension is required because <see cref="ExtensionsForHttp.SendForm(IRequestBuilder, NameValueCollection)"/>
        /// received a breaking change in version 0.3.41 that changed the behaviour to send a <c>multipart/form-data</c>
        /// request instead of a <c>application/x-www-form-urlencoded</c> request.
        /// </remarks>
        /// <param name="builder">The builder we're working on.</param>
        /// <param name="form">The <see cref="NameValueCollection"/> (e.g. FormCollection) to use.</param>
        /// <returns>The request builder for chaining.</returns>
        public static IRequestBuilder SendFormUrlEncoded(this IRequestBuilder builder, NameValueCollection form) =>
            builder.SendContent(new FormUrlEncodedContent(form.AllKeys.ToDictionary(k => k, v => form[v])));
    }
}
