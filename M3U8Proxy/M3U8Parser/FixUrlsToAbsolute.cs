﻿using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using RestSharp;

namespace M3U8Proxy.M3U8Parser;

public partial class M3U8Paser
{
    private readonly Regex _getParamsRegex;

    [GeneratedRegexAttribute(@"\?.+", RegexOptions.Compiled)]
    private static partial Regex GetParamsRegex();

    public static string FixUrls(IRestResponse response, string url)
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();
        try
        {
            var absoluteUrl = new StringBuilder();
            var lines = response.Content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var parameters = GetParamsRegex().Match(url).Value;
            var uri = new Uri(url);
            var baseUrl = $"{uri.Scheme}://{uri.Authority}";
            var index = url.LastIndexOf('/');

            for (var i = 0; i < lines.Length; i++)
                if (!lines[i].StartsWith("http") && !lines[i].StartsWith("#") && !string.IsNullOrWhiteSpace(lines[i]))
                {
                    if (lines[i].StartsWith("/"))
                    {
                        absoluteUrl.Clear();
                        absoluteUrl.Append(baseUrl);
                        absoluteUrl.Append(lines[i]);
                        absoluteUrl.Append(parameters);
                    }
                    else
                    {
                        absoluteUrl.Clear();
                        absoluteUrl.Append(url[..(index + 1)]);
                        absoluteUrl.Append(lines[i]);
                        absoluteUrl.Append(parameters);
                    }

                    lines[i] = absoluteUrl.ToString();
                }

            return string.Join(Environment.NewLine, lines);
        }
        finally
        {
            stopwatch.Stop();
            Console.WriteLine($"FixUrls: {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}