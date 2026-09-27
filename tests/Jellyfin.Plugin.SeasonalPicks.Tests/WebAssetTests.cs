using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Acornima;
using Xunit;

namespace Jellyfin.Plugin.SeasonalPicks.Tests;

/// <summary>The config page and web script ship as embedded resources; a syntax error there only shows up in the browser.</summary>
public class WebAssetTests
{
    private static string Resource(string name)
    {
        var asm = typeof(Plugin).Assembly;
        using var stream = asm.GetManifestResourceStream($"{typeof(Plugin).Namespace}.{name}");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }

    private static string ConfigScript()
        => Regex.Match(Resource("Configuration.configPage.html"), "<script type=\"text/javascript\">([\\s\\S]*?)</script>").Groups[1].Value;

    [Fact]
    public void Config_page_script_parses()
    {
        new Parser(new ParserOptions { EcmaVersion = EcmaVersion.ES2020 }).ParseScript(ConfigScript());
    }

    [Fact]
    public void Web_script_parses_after_placeholders_are_filled()
    {
        var js = Resource("Web.forYou.js").Replace("__SP_ENABLED__", "true").Replace("__SP_HOME_ROW__", "false")
            .Replace("__SP_TITLE__", System.Text.Json.JsonSerializer.Serialize("Suggested \"</script>\" 'quotes'"));
        Assert.DoesNotContain("__SP_", js);
        new Parser(new ParserOptions { EcmaVersion = EcmaVersion.ES2020 }).ParseScript(js);
    }

    [Fact]
    public void Every_element_the_config_script_looks_up_exists()
    {
        var html = Resource("Configuration.configPage.html");
        var script = ConfigScript();

        var ids = Regex.Matches(script, @"\$\('#(\w+)'\)").Select(m => m.Groups[1].Value)
            .Concat(ListIn(script, @"var CHECKS = \[([^\]]*)\]"))
            .Concat(ListIn(script, @"\[('PickCount'[^\]]*)\]\.forEach"))
            .Distinct()
            .ToList();

        Assert.NotEmpty(ids);
        Assert.All(ids, id => Assert.Contains($"id=\"{id}\"", html));
    }

    [Fact]
    public void Every_checkbox_setting_exists_in_the_configuration()
    {
        var names = typeof(Configuration.PluginConfiguration).GetProperties().Select(p => p.Name).ToHashSet();
        var script = ConfigScript();
        var used = ListIn(script, @"var CHECKS = \[([^\]]*)\]").Concat(ListIn(script, @"\[('PickCount'[^\]]*)\]\.forEach"));
        Assert.All(used, n => Assert.Contains(n, names));
    }

    private static string[] ListIn(string script, string pattern)
    {
        var m = Regex.Match(script, pattern);
        Assert.True(m.Success, pattern);
        return Regex.Matches(m.Groups[1].Value, "'(\\w+)'").Select(x => x.Groups[1].Value).ToArray();
    }
}
