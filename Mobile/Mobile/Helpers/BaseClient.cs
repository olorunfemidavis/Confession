﻿using Microsoft.AppCenter.Crashes;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Mobile.Helpers
{
    public class BaseClient
    {
        internal static async Task<string> PostEntities(string urlLink, string content)
        {
            HttpClient httpClient = HttpConnection.CreateClient(await Logic.GetToken());
            HttpResponseMessage httpResponse = await httpClient.PostAsync(urlLink, new StringContent(content, Encoding.UTF8, Constants.ContentTypeHeaderJson)).ConfigureAwait(false);
            return await httpResponse.Content.ReadAsStringAsync();
        }
        internal static async Task<string> GetEntities(string urlLink)
        {
            HttpClient client = HttpConnection.CreateClient(await Logic.GetToken());
            Task<HttpResponseMessage> response = client.GetAsync(urlLink);
            return await response.Result.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
        internal static async Task<string> GetEntities(string urlLink, string secretKey)
        {
            HttpClient client = HttpConnection.CreateClient(secretKey);
            Task<HttpResponseMessage> response = client.GetAsync(urlLink);
            return await response.Result.Content.ReadAsStringAsync();
        }
        internal static async Task<string> DeleteEntities(string urlLink)
        {
            HttpClient client = HttpConnection.CreateClient(await Logic.GetToken());
            Task<HttpResponseMessage> response = client.DeleteAsync(urlLink);
            return await response.Result.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
        internal static async Task<string> PutEntities(string urlLink, string content)
        {
            HttpClient client = HttpConnection.CreateClient(await Logic.GetToken());
            HttpResponseMessage response = await client.PutAsync(urlLink, new StringContent(content, Encoding.UTF8, Constants.ContentTypeHeaderJson)).ConfigureAwait(false);
            return await response.Content.ReadAsStringAsync();
        }
        internal static async Task<string> PostImageStream(byte[] input)
        {
            try
            {
                Stream stream = new MemoryStream(input);
                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri(Constants.BaseURL);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Constants.AuthorizationHeaderType, await Logic.GetToken());
                    StreamContent inputData = new StreamContent(stream);
                    string urlLink = "chat/postimage";
                    HttpResponseMessage response = await client.PostAsync(urlLink, inputData).ConfigureAwait(false);
                    var result =  await response.Content.ReadAsStringAsync();
                    return result;
                }
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex, Logic.GetErrorProperties(ex));
                return string.Empty;
            }
        }
    }
}
