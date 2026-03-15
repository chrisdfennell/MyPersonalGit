using System.Text.RegularExpressions;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Components;

namespace MyPersonalGit.Data;

public interface IMarkdownService
{
    /// <summary>Render markdown to HTML with optional relative URL rewriting for repo context.</summary>
    MarkupString RenderMarkdown(string markdown, string? repoName = null, string? currentBranch = null, string? currentPath = null, bool isFile = false);

    /// <summary>Simple render without any URL rewriting (for issues, PRs, wiki).</summary>
    string RenderToHtml(string markdown);
}

public class MarkdownService : IMarkdownService
{
    private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public MarkupString RenderMarkdown(string markdown, string? repoName = null, string? currentBranch = null, string? currentPath = null, bool isFile = false)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return new MarkupString("");

        var document = Markdown.Parse(markdown, _pipeline);

        if (!string.IsNullOrEmpty(repoName))
        {
            RewriteRelativeUrls(document, repoName, currentBranch ?? "main", currentPath, isFile);
        }

        var writer = new StringWriter();
        var renderer = new Markdig.Renderers.HtmlRenderer(writer);
        _pipeline.Setup(renderer);
        renderer.Render(document);
        writer.Flush();

        // Wrap standalone <img> tags in clickable <a> links for full-size view
        var html = Regex.Replace(writer.ToString(),
            @"(?<!<a[^>]*>)\s*<img\s+([^>]*?)src=""([^""]+)""([^>]*?)/?>",
            @"<a href=""$2"" target=""_blank"" rel=""noopener""><img src=""$2"" $1$3 /></a>");

        // Add GitHub-style IDs to all headings based on their text content
        html = Regex.Replace(html, @"<(h[1-6])(?:\s+id=""[^""]*"")?>(.*?)</\1>", m =>
        {
            var tag = m.Groups[1].Value;
            var innerHtml = m.Groups[2].Value;
            var plainText = Regex.Replace(innerHtml, @"<[^>]+>", "").Trim();
            var id = GitHubSlugify(System.Net.WebUtility.HtmlDecode(plainText));
            return $"<{tag} id=\"{id}\">{innerHtml}</{tag}>";
        });

        return new MarkupString(html);
    }

    private static string GitHubSlugify(string heading)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var c in heading.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c) || c == '-')
                sb.Append(c);
            else if (c == ' ')
                sb.Append('-');
        }
        return sb.ToString();
    }

    public string RenderToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return "";
        return Markdown.ToHtml(markdown, _pipeline);
    }

    private static void RewriteRelativeUrls(MarkdownDocument document, string repoName, string currentBranch, string? currentPath, bool isFile)
    {
        var currentDir = string.IsNullOrEmpty(currentPath)
            ? ""
            : (isFile ? GetParentPath(currentPath) : currentPath);

        foreach (var node in document.Descendants())
        {
            if (node is LinkInline link && !string.IsNullOrEmpty(link.Url))
            {
                if (IsAbsoluteUrl(link.Url)) continue;

                var url = link.Url;
                var fragment = "";
                var hashIdx = url.IndexOf('#');
                if (hashIdx >= 0) { fragment = url[hashIdx..]; url = url[..hashIdx]; }
                var queryIdx = url.IndexOf('?');
                var query = "";
                if (queryIdx >= 0) { query = url[queryIdx..]; url = url[..queryIdx]; }

                if (string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(fragment))
                {
                    // Rewrite pure anchor links to include the repo page path
                    if (!string.IsNullOrEmpty(repoName))
                        link.Url = $"/repo/{repoName}{fragment}";
                    continue;
                }

                var resolvedPath = ResolveRelativePath(currentDir, url);

                if (link.IsImage)
                {
                    link.Url = $"/raw/{repoName}/{resolvedPath}?branch={Uri.EscapeDataString(currentBranch)}";
                }
                else
                {
                    link.Url = $"/repo/{repoName}/{resolvedPath}{fragment}";
                }
            }
        }
    }

    private static bool IsAbsoluteUrl(string url) =>
        url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
        url.StartsWith("//") ||
        url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
        url.StartsWith("data:", StringComparison.OrdinalIgnoreCase);

    private static string GetParentPath(string path)
    {
        var lastSlash = path.LastIndexOf('/');
        return lastSlash > 0 ? path[..lastSlash] : "";
    }

    private static string ResolveRelativePath(string currentDir, string relativePath)
    {
        if (relativePath.StartsWith("/"))
            return relativePath.TrimStart('/');

        var combined = string.IsNullOrEmpty(currentDir)
            ? relativePath
            : currentDir.TrimEnd('/') + "/" + relativePath;

        var segments = combined.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var result = new Stack<string>();
        foreach (var seg in segments)
        {
            if (seg == ".") continue;
            if (seg == "..") { if (result.Count > 0) result.Pop(); }
            else result.Push(seg);
        }
        return string.Join("/", result.Reverse());
    }
}
