using Jellyfin.Plugin.SeasonalPicks.Web;
using Xunit;

namespace Jellyfin.Plugin.SeasonalPicks.Tests;

public class WebInjectionTests
{
    private const string Page = "<html><head><title>Jellyfin</title></head><body><div id=\"reactRoot\"></div></body></html>";

    private static string Transform(string contents) => IndexTransformer.Transform(new TransformPayload { Contents = contents });

    [Fact]
    public void Adds_the_script_before_the_end_of_the_page()
    {
        var html = Transform(Page);

        Assert.Contains("<script defer src=\"../SeasonalPicks/ClientScript\"></script></body>", html);
    }

    [Fact]
    public void Adds_the_script_only_once()
    {
        var once = Transform(Page);

        Assert.Equal(once, Transform(once));
    }

    // "index.html" is a regex to File Transformation, so the shared group also runs on JS chunks like this one.
    [Theory]
    [InlineData("\"use strict\";(self.webpackChunk=self.webpackChunk||[]).push([[123],{456:e=>{e.exports=\"<div></div>\"}}]);")]
    [InlineData("")]
    public void Leaves_anything_that_is_not_a_whole_page_alone(string contents)
    {
        Assert.Equal(contents, Transform(contents));
    }

    [Fact]
    public void Treats_missing_contents_as_empty()
    {
        Assert.Equal(string.Empty, IndexTransformer.Transform(new TransformPayload()));
    }
}
