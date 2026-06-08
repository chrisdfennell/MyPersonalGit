using System.Linq;
using MyPersonalGit.Data;

namespace MyPersonalGit.Tests;

/// <summary>Tests for the manifest parsers behind the repository Dependencies feature.</summary>
public class DependencyParserTests
{
    [Fact]
    public void PackageJson_ParsesDeps_DevDeps_AndPeer()
    {
        var json = """
        { "name": "app",
          "dependencies": { "react": "^18.2.0", "express": "4.18.2" },
          "devDependencies": { "jest": "^29.0.0" },
          "peerDependencies": { "react-dom": "^18.0.0" } }
        """;
        var deps = DependencyService.ParsePackageJson(json, "package.json").ToList();

        Assert.Equal(4, deps.Count);
        Assert.All(deps, d => Assert.Equal("npm", d.Ecosystem));
        Assert.Equal("^18.2.0", deps.Single(d => d.Name == "react").Version);
        Assert.True(deps.Single(d => d.Name == "jest").IsDev);
        Assert.False(deps.Single(d => d.Name == "react").IsDev);
    }

    [Fact]
    public void PackageJson_MalformedJson_ReturnsEmpty()
        => Assert.Empty(DependencyService.ParsePackageJson("{ not json", "package.json"));

    [Fact]
    public void Csproj_ParsesPackageReference_AttributeAndElementVersion()
    {
        var xml = """
        <Project Sdk="Microsoft.NET.Sdk">
          <ItemGroup>
            <PackageReference Include="Serilog" Version="3.1.1" />
            <PackageReference Include="Newtonsoft.Json"><Version>13.0.3</Version></PackageReference>
          </ItemGroup>
        </Project>
        """;
        var deps = DependencyService.ParseCsproj(xml, "App.csproj").ToList();

        Assert.Equal(2, deps.Count);
        Assert.All(deps, d => Assert.Equal("NuGet", d.Ecosystem));
        Assert.Equal("3.1.1", deps.Single(d => d.Name == "Serilog").Version);
        Assert.Equal("13.0.3", deps.Single(d => d.Name == "Newtonsoft.Json").Version);
    }

    [Fact]
    public void Requirements_ParsesVersionsAndSkipsCommentsAndFlags()
    {
        var txt = "# comment\nflask==2.3.2\nrequests>=2.28\nrich  # pretty output\n-r other.txt\n";
        var deps = DependencyService.ParseRequirements(txt, "requirements.txt").ToList();

        Assert.Equal(3, deps.Count);
        Assert.All(deps, d => Assert.Equal("pip", d.Ecosystem));
        Assert.Equal("==2.3.2", deps.Single(d => d.Name == "flask").Version);
        Assert.Equal(">=2.28", deps.Single(d => d.Name == "requests").Version);
        Assert.Equal("", deps.Single(d => d.Name == "rich").Version);
    }

    [Fact]
    public void GoMod_ParsesRequireBlockAndStripsIndirect()
    {
        var txt = "module example.com/m\n\ngo 1.21\n\nrequire (\n\tgithub.com/gin-gonic/gin v1.9.1\n\tgolang.org/x/sync v0.3.0 // indirect\n)\n";
        var deps = DependencyService.ParseGoMod(txt, "go.mod").ToList();

        Assert.Equal(2, deps.Count);
        Assert.All(deps, d => Assert.Equal("Go", d.Ecosystem));
        Assert.Equal("v1.9.1", deps.Single(d => d.Name == "github.com/gin-gonic/gin").Version);
    }

    [Fact]
    public void Cargo_ParsesSimpleAndTableDeps()
    {
        var toml = "[package]\nname = \"x\"\n\n[dependencies]\nserde = \"1.0\"\ntokio = { version = \"1.35\", features = [\"full\"] }\n\n[dev-dependencies]\ncriterion = \"0.5\"\n";
        var deps = DependencyService.ParseCargo(toml, "Cargo.toml").ToList();

        Assert.Equal(3, deps.Count);
        Assert.All(deps, d => Assert.Equal("Cargo", d.Ecosystem));
        Assert.Equal("1.0", deps.Single(d => d.Name == "serde").Version);
        Assert.Equal("1.35", deps.Single(d => d.Name == "tokio").Version);
        Assert.True(deps.Single(d => d.Name == "criterion").IsDev);
    }

    [Fact]
    public void Composer_SkipsPlatformRequirements()
    {
        var json = """
        { "require": { "php": ">=8.1", "ext-json": "*", "monolog/monolog": "^3.0" },
          "require-dev": { "phpunit/phpunit": "^10.0" } }
        """;
        var deps = DependencyService.ParseComposer(json, "composer.json").ToList();

        Assert.Equal(2, deps.Count);
        Assert.Contains(deps, d => d.Name == "monolog/monolog" && !d.IsDev);
        Assert.Contains(deps, d => d.Name == "phpunit/phpunit" && d.IsDev);
        Assert.DoesNotContain(deps, d => d.Name == "php" || d.Name.StartsWith("ext-"));
    }

    [Fact]
    public void Gemfile_ParsesGemNameAndVersion()
    {
        var txt = "source 'https://rubygems.org'\ngem 'rails', '~> 7.0'\ngem 'puma'\n";
        var deps = DependencyService.ParseGemfile(txt, "Gemfile").ToList();

        Assert.Equal(2, deps.Count);
        Assert.All(deps, d => Assert.Equal("RubyGems", d.Ecosystem));
        Assert.Equal("~> 7.0", deps.Single(d => d.Name == "rails").Version);
        Assert.Equal("", deps.Single(d => d.Name == "puma").Version);
    }

    [Fact]
    public void PomXml_ParsesGroupAndArtifact()
    {
        var xml = """
        <project>
          <dependencies>
            <dependency>
              <groupId>com.google.guava</groupId>
              <artifactId>guava</artifactId>
              <version>32.1.2-jre</version>
            </dependency>
          </dependencies>
        </project>
        """;
        var deps = DependencyService.ParsePomXml(xml, "pom.xml").ToList();

        var d = Assert.Single(deps);
        Assert.Equal("Maven", d.Ecosystem);
        Assert.Equal("com.google.guava:guava", d.Name);
        Assert.Equal("32.1.2-jre", d.Version);
    }

    [Theory]
    [InlineData("package.json")]
    [InlineData("App.csproj")]
    [InlineData("requirements.txt")]
    [InlineData("go.mod")]
    [InlineData("Cargo.toml")]
    [InlineData("composer.json")]
    [InlineData("Gemfile")]
    [InlineData("pom.xml")]
    public void MatchParser_RecognizesKnownManifests(string fileName)
        => Assert.NotNull(DependencyService.MatchParser(fileName));

    [Theory]
    [InlineData("README.md")]
    [InlineData("main.go")]
    [InlineData("yarn.lock")]
    public void MatchParser_IgnoresOtherFiles(string fileName)
        => Assert.Null(DependencyService.MatchParser(fileName));
}
