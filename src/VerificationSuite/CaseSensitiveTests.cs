using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace VerificationSuite
{
    public class CaseSensitiveTests
    {
        private const string HostUrl = "http://localhost:5000";

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Server_does_not_serve_cached_responses_if_path_casing_differ_and_UseCaseSensitivePaths_true(string method)
        {
            var queryString = Guid.NewGuid().ToString();
            await VerifyResponseNotEqual(
                await SendRequest(method, $"{queryString.ToUpperInvariant()}?Cache-Control=public"),
                await SendRequest(method, $"{queryString.ToLowerInvariant()}?Cache-Control=public"));
        }

        private async Task VerifyResponseNotEqual(WebResponse resp1, WebResponse resp2)
        {
            if (((HttpWebResponse)resp1).Method != "HEAD" && ((HttpWebResponse)resp2).Method != "HEAD")
            {
                Assert.NotEqual(await GetContent(resp1), await GetContent(resp2));
            }

            Assert.NotEqual(resp1.Headers["X-my-time"], resp2.Headers["X-my-time"]);
        }

        private async Task VerifyResponseEqual(WebResponse resp1, WebResponse resp2)
        {
            if (((HttpWebResponse)resp1).Method != "HEAD" && ((HttpWebResponse)resp2).Method != "HEAD")
            {
                Assert.Equal(await GetContent(resp1), await GetContent(resp2));
            }

            Assert.Equal(resp1.Headers["X-my-time"], resp2.Headers["X-my-time"]);
        }

        private async Task VerifyNoResponse(WebResponse response)
        {
            Assert.False(response.Headers.AllKeys.Contains("X-my-time"));
            Assert.Empty(await GetContent(response));
        }

        private async Task<WebResponse> SendRequest(string method, string queryString, IDictionary<string, string> headers = null)
        {
            var httpRequest = WebRequest.CreateHttp(HostUrl + "/" + queryString);
            httpRequest.Method = method;

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    httpRequest.Headers[header.Key] = header.Value;
                }
            }

            return await httpRequest.GetResponseAsync();
        }

        private async Task<string> GetContent(WebResponse webResponse)
        {
            using (var sr = new StreamReader(webResponse.GetResponseStream()))
            {
                return await sr.ReadToEndAsync();
            }
        }

    }
}
