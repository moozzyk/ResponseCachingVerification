using Xunit;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System;

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
                method,
                await SendRequest(method, queryString, new Dictionary<string, string>()),
                await SendRequest(method, queryString, new Dictionary<string, string>()));
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Response_cached_if_CacheControl_public_in_response(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public";
            await VerifyResponseEqual(
                method,
                await SendRequest(method, queryString),
                await SendRequest(method, queryString));
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Response_not_cached_response_if_CacheControl_no_cache_in_request(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public";
            await VerifyResponseNotEqual(
                method,
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
                method,
                await SendRequest("GET", queryString, new Dictionary<string, string> { { "Pragma", "no-cache" } }),
                await SendRequest("GET", queryString));
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Non_cached_response_if_CacheControl_no_cache_in_request(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public";
            await VerifyResponseNotEqual(
                method,
                await SendRequest("GET", queryString),
                await SendRequest("GET", queryString, new Dictionary<string, string> { { "Cache-Control", "no-cache" } }));
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Non_cached_response_if_Pragma_no_cache_in_request(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public";
            await VerifyResponseNotEqual(
                method,
                await SendRequest("GET", queryString),
                await SendRequest("GET", queryString, new Dictionary<string, string> { { "Pragma", "no-cache" } }));
        }

        [Fact]
        public async Task GET_HEAD_cached_responses_not_mixed()
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public";
            await VerifyResponseNotEqual(
                "HEAD",
                await SendRequest("GET", queryString),
                await SendRequest("HEAD", queryString));
        }

        [Fact]
        public async Task HEAD_GET_cached_responses_not_mixed()
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public";
            await VerifyResponseNotEqual(
                "HEAD",
                await SendRequest("HEAD", queryString),
                await SendRequest("GET", queryString));
        }


        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async Task Cache_respects_max_age(string method)
        {
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public,%20max-age=1";

            var resp1 = await SendRequest("GET", queryString, new Dictionary<string, string>());
            var resp2 = await SendRequest("GET", queryString, new Dictionary<string, string>());
            await VerifyResponseEqual("GET", resp1, resp2);

            await Task.Delay(2000);

            var resp3 = await SendRequest("GET", queryString, new Dictionary<string, string>());
            await VerifyResponseEqual("HEAD", resp2, resp3);
        }

        [Fact]
        public async Task Response_not_cached_if_method_POST()
        {
            const string method = "POST";
            var queryString = $"{Guid.NewGuid().ToString()}?Cache-Control=public";
            await VerifyResponseNotEqual(
                method,
                await SendRequest(method, queryString),
                await SendRequest(method, queryString));
        }

        private async Task VerifyResponseNotEqual(string method, WebResponse resp1, WebResponse resp2)
        {
            if (method != "HEAD")
            {
                Assert.NotEqual(await GetContent(resp1), await GetContent(resp2));
            }

            Assert.NotEqual(resp1.Headers["X-my-time"], resp2.Headers["X-my-time"]);
        }

        private async Task VerifyResponseEqual(string method, WebResponse resp1, WebResponse resp2)
        {
            if (method != "HEAD")
            {
                Assert.Equal(await GetContent(resp1), await GetContent(resp2));
            }

            Assert.Equal(resp1.Headers["X-my-time"], resp2.Headers["X-my-time"]);
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
