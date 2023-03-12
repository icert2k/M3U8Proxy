﻿using System.Net;
using System.Text;
using M3U8Proxy.M3U8Parser;
using M3U8Proxy.RequestHandler;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Newtonsoft.Json;

namespace M3U8Proxy.Controllers;

public partial class Proxy
{
    private readonly string _baseUrl;
    private readonly List<string> _listOfKeywords = new() { "#EXT-X-STREAM-INF", "#EXT-X-I-FRAME-STREAM-INF" };

    public Proxy(IConfiguration configuration)
    {
        _baseUrl = configuration["ProxyUrl"]!;
    }

    [OutputCache(PolicyName = "m3u8")]
    [HttpGet("m3u8/{url}/{headers?}/{type?}")]
    public IActionResult GetM3U8(string url, string? headers = "{}")
    {
        Console.WriteLine("no cache");
        var proxyUrl = _baseUrl + "proxy/";
        var m3U8Url = _baseUrl + "proxy/m3u8/";

        try
        {
            url = Uri.UnescapeDataString(url);

            headers = Uri.UnescapeDataString(headers!);

            if (string.IsNullOrEmpty(url))
                return BadRequest("URL missing or malformed.");

            var headersDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(headers);
            var response = _reqHandler.MakeRequest(url, headersDictionary!);
            HttpContext.Response.StatusCode = (int)response.StatusCode;

            if (response.StatusCode != HttpStatusCode.OK)
                return BadRequest(JsonConvert.SerializeObject(response));

            ReqHandler.RemoveBlockedHeaders(response);
            ReqHandler.AddResponseHeaders(response);

            var content = M3U8Paser.FixUrls(response, url);
            var isPlaylistM3U8 = content.IndexOf(_listOfKeywords[0], StringComparison.OrdinalIgnoreCase) >= 0
                                 || content.IndexOf(_listOfKeywords[1], StringComparison.OrdinalIgnoreCase) >= 0;

            var modifiedContent = _paser.ModifyContent(content, isPlaylistM3U8 ? m3U8Url : proxyUrl, headers);

            return File(Encoding.UTF8.GetBytes(modifiedContent), "application/vnd.apple.mpegurl",
                $"{GenerateRandomId(10)}.m3u8");
        }
        catch (Exception e)
        {
            return BadRequest(JsonConvert.SerializeObject(e));
        }
    }

    public static string GenerateRandomId(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}