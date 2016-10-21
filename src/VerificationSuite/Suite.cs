using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace VerificationSuite
{
    public class Suite
    {
        private const string HostUrl = "http://localhost:5000";

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Response_not_cached_if_no_CacheControl_public_in_response(string method)
        {
            var queryString = Guid.NewGuid().ToString();
            await VerifyResponseNotEqual(
                await SendRequest(method, queryString),
                await SendRequest(method, queryString));
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Response_cached_if_CacheControl_public_in_response(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public";
            await VerifyResponseEqual(
                await SendRequest(method, queryString),
                await SendRequest(method, queryString));
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Cached_response_served_if_path_casing_differ(string method)
        {
            var queryString = Guid.NewGuid().ToString();
            await VerifyResponseEqual(
                await SendRequest(method, $"{queryString.ToUpperInvariant()}?Cache-Control=public"),
                await SendRequest(method, $"{queryString.ToLowerInvariant()}?Cache-Control=public"));
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Cached_response_returned_if_queryString_differ(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public";
            await VerifyResponseEqual(
                await SendRequest(method, queryString + "&abc"),
                await SendRequest(method, queryString + "&xyz"));
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Not_cached_response_returned_if_paths_differ(string method)
        {
            var pathPrefix = Guid.NewGuid().ToString();
            await VerifyResponseNotEqual(
                await SendRequest(method, pathPrefix + "?Cache-Control=public"),
                await SendRequest(method, pathPrefix + "/path?Cache-Control=public"));
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Cached_response_contains_MaxAge_header(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public";
            await SendRequest(method, queryString);
            var cachedResponse = await SendRequest(method, queryString);
            Assert.True(cachedResponse.Headers["Age"].All(char.IsDigit));
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Response_not_cached_response_if_CacheControl_no_cache_in_request(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public";
            await VerifyResponseNotEqual(
                await SendRequest(method, queryString, new Dictionary<string, string> { { "Cache-Control", "no-cache" } }),
                await SendRequest(method, queryString));
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Response_not_response_if_Pragma_no_cache_in_request(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public";
            await VerifyResponseNotEqual(
                await SendRequest(method, queryString, new Dictionary<string, string> { { "Pragma", "no-cache" } }),
                await SendRequest(method, queryString));

        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Response_not_cached_if_request_contains_Authorization_header(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public";
            await VerifyResponseNotEqual(
                await SendRequest(method, queryString, new Dictionary<string, string> { { "Authorization", "c" } }),
                await SendRequest(method, queryString));
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Non_cached_response_if_CacheControl_no_cache_in_request(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public";
            await VerifyResponseNotEqual(
                await SendRequest(method, queryString),
                await SendRequest(method, queryString, new Dictionary<string, string> { { "Cache-Control", "no-cache" } }));
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Non_cached_response_if_Pragma_no_cache_in_request(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public";
            await VerifyResponseNotEqual(
                await SendRequest(method, queryString),
                await SendRequest(method, queryString, new Dictionary<string, string> { { "Pragma", "no-cache" } }));
        }

        [Fact]
        public async Task GET_HEAD_cached_responses_not_mixed()
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public";
            await VerifyResponseNotEqual(
                await SendRequest("GET", queryString),
                await SendRequest("HEAD", queryString));
        }

        [Fact]
        public async Task HEAD_GET_cached_responses_not_mixed()
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public";
            await VerifyResponseNotEqual(
                await SendRequest("HEAD", queryString),
                await SendRequest("GET", queryString));
        }


        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Cache_respects_max_age(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public,%20max-age=2";

            var resp1 = await SendRequest(method, queryString);
            await Task.Delay(1000);
            var resp2 = await SendRequest(method, queryString);
            await VerifyResponseEqual(resp1, resp2);
            await Task.Delay(2000);

            var resp3 = await SendRequest(method, queryString);
            // Can't re-read the resp2 stream
            Assert.NotEqual(resp2.Headers["X-my-time"], resp3.Headers["X-my-time"]);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Cache_respects_Expires(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public&Expires={DateTime.Now.AddSeconds(2).ToUniversalTime().ToString("r")}";

            var resp1 = await SendRequest(method, queryString);
            await Task.Delay(1000);
            var resp2 = await SendRequest(method, queryString);
            await VerifyResponseEqual(resp1, resp2);
            await Task.Delay(2000);

            var resp3 = await SendRequest(method, queryString);
            // Can't re-read the resp2 stream
            Assert.NotEqual(resp2.Headers["X-my-time"], resp3.Headers["X-my-time"]);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task StatusCode_304_if_IfNoneMatch_any(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public";

            var resp1 = await SendRequest(method, queryString);
            var exception =
                await Assert.ThrowsAsync<WebException>(
                    async () => await SendRequest(method, queryString,
                        new Dictionary<string, string> { { "If-None-Match", "*" } }));

            Assert.Equal(HttpStatusCode.NotModified, ((HttpWebResponse)exception.Response).StatusCode);
            await VerifyNoResponse(exception.Response);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Cached_response_returned_if_IfNoneMatch_not_any(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public";

            await VerifyResponseEqual(
                await SendRequest(method, queryString),
                await SendRequest(method, queryString, new Dictionary<string, string> { { "If-None-Match", "\"test\"" } }));
        }


        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task StatusCode_304_if_IfNoneMatch_contains_known_etag(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public&ETag=\"42\"";

            var resp1 = await SendRequest(method, queryString);
            var exception =
                await Assert.ThrowsAsync<WebException>(
                    async () => await SendRequest(method, queryString,
                        new Dictionary<string, string> { { "If-None-Match", "\"41\", \"42\", \"43\"" } }));

            Assert.Equal(((HttpWebResponse)exception.Response).StatusCode, HttpStatusCode.NotModified);
            await VerifyNoResponse(exception.Response);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Cached_response_returned_if_IfNoneMatch_does_not_contain_known_headers(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public&ETag=\"42\"";

            await VerifyResponseEqual(
                await SendRequest(method, queryString),
                await SendRequest(method, queryString, new Dictionary<string, string> { { "If-None-Match", "\"41\", \"4_2\", \"43\"" } }));
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task StatusCode_304_if_IfUnmodifiedSince_after_response_was_cached(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public";

            var resp1 = await SendRequest(method, queryString);
            var exception =
                await Assert.ThrowsAsync<WebException>(
                    async () => await SendRequest(method, queryString,
                        new Dictionary<string, string>
                        {
                            { "If-Unmodified-Since", DateTime.Now.ToUniversalTime().ToString("r") }
                        }));

            Assert.Equal(((HttpWebResponse)exception.Response).StatusCode, HttpStatusCode.NotModified);
            await VerifyNoResponse(exception.Response);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Cached_response_returned_if_IfUnmodifiedSince_before_response_was_cached(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public&ETag=\"42\"";

            await VerifyResponseEqual(
                await SendRequest(method, queryString),
                await SendRequest(method, queryString,
                    new Dictionary<string, string>
                    {
                        { "If-Unmodified-Since", DateTime.Now.AddSeconds(-10).ToUniversalTime().ToString("r") }
                    }));
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Cache_respects_Vary_header(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public";

            var resp1a = await SendRequest(method, queryString + "&Vary=\"X-RespCaching-Verification\"",
                new Dictionary<string, string> { { "X-RespCaching-Verification", "1" } });

            var resp2a = await SendRequest(method, queryString + "&Vary=\"X-RespCaching-Verification\"",
                    new Dictionary<string, string> { { "X-RespCaching-Verification", "2" } });

            var resp1b = await SendRequest(method, queryString,
                    new Dictionary<string, string>
                    {
                        { "X-RespCaching-Verification", "1" }
                    });

            var resp2b = await SendRequest(method, queryString,
                    new Dictionary<string, string>
                    {
                        { "X-RespCaching-Verification", "2" }
                    });

            await VerifyResponseEqual(resp1a, resp1b);
            await VerifyResponseEqual(resp2a, resp2b);
            // cannot re-read streams
            Assert.NotEqual(resp1a.Headers["X-my-time"], resp2a.Headers["X-my-time"]);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Cache_respects_vary_by_query_keys(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public%2C+max-age=100&VaryByQueryKeys=X-RespCaching-Verification";

            var resp1a = await SendRequest(method, queryString + "&X-RespCaching-Verification=1");
            var resp2a = await SendRequest(method, queryString + "&X-RespCaching-Verification=2");

            var resp1b = await SendRequest(method, queryString + "&X-RespCaching-Verification=1");
            var resp2b = await SendRequest(method, queryString + "&X-RespCaching-Verification=2");

            await VerifyResponseEqual(resp1a, resp1b);
            await VerifyResponseEqual(resp2a, resp2b);
            // cannot re-read streams
            Assert.NotEqual(resp1a.Headers["X-my-time"], resp2a.Headers["X-my-time"]);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Server_returns_504_if_OnlyIfCached_requested_and_response_not_cached(string method)
        {
            var exception = await Assert.ThrowsAsync<WebException>(
                async () => await SendRequest(method, $"{Guid.NewGuid().ToString()}",
                    new Dictionary<string, string> { { "Cache-Control", "only-if-cached" } }));

            Assert.Equal(HttpStatusCode.GatewayTimeout, ((HttpWebResponse)exception.Response).StatusCode);
        }


        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Cached_response_returned_if_OnlyIfCached_requested_and_response_cached(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public";
            await VerifyResponseEqual(
                await SendRequest(method, queryString),
                await SendRequest(method, queryString, new Dictionary<string, string> { { "Cache-Control", "only-if-cached" } }));
        }

        [Fact]
        public async Task Response_not_cached_if_method_POST()
        {
            const string method = "POST";
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public";
            await VerifyResponseNotEqual(
                await SendRequest(method, queryString),
                await SendRequest(method, queryString));
        }


        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task SharedMaxAge_overrides_MaxAge(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public&Cache-Control=s-maxage=1&Cache-Control=max-age=100";
            var resp1 = await SendRequest(method, queryString);
            await Task.Delay(2000);
            await VerifyResponseNotEqual(
                resp1,
                await SendRequest(method, queryString));
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task MustRevalidate_removes_cache_item(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public&Cache-Control=max-age=100";
            var resp1 = await SendRequest(method, queryString + "&Cache-Control=must-revalidate");
            var resp2 = await SendRequest(method, queryString, new Dictionary<string, string> { { "Cache-Control", "max-age=0" } });
            await VerifyResponseNotEqual(resp1, resp2);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Cache_respects_request_MaxAge(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public&Cache-Control=max-age=100";
            var resp1 = await SendRequest(method, queryString);
            var resp2 = await SendRequest(method, queryString, new Dictionary<string, string> { { "Cache-Control", "max-age=0" } });
            await VerifyResponseNotEqual(resp1, resp2);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Cache_respects_request_MinFresh(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public&Cache-Control=max-age=10";
            var resp1 = await SendRequest(method, queryString);
            var resp2 = await SendRequest(method, queryString, new Dictionary<string, string> { { "Cache-Control", "min-fresh=15" } });
            await VerifyResponseNotEqual(resp1, resp2);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Cache_respects_request_MaxStale(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public&Cache-Control=max-age=100";
            var resp1 = await SendRequest(method, queryString);
            await Task.Delay(1000);
            var resp2 = await SendRequest(method, queryString, new Dictionary<string, string> { { "Cache-Control", "max-stale=5, max-age=0" } });
            await VerifyResponseEqual(resp1, resp2);
        }

        [Fact]
        public async Task Responses_bigger_than_limit_not_cached()
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public&append=1025";
            await VerifyResponseNotEqual(
                await SendRequest("GET", queryString),
                await SendRequest("GET", queryString));
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
