using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using NetArchTest.Rules;

namespace Deskair.Tests.Architecture;

/// <summary>The dependency rules from docs/engineering/architecture.md section 3.</summary>
public class DependencyRulesTests
{
    private static readonly Assembly domain = typeof(Deskair.Domain.Readings.Reading).Assembly;
    private static readonly Assembly services = typeof(Deskair.Services.ServiceCollectionExtensions).Assembly;
    private static readonly Assembly sensors = typeof(Deskair.Sensors.Simulated.SimulatedProvider).Assembly;
    private static readonly Assembly storage = typeof(Deskair.Storage.ServiceCollectionExtensions).Assembly;
    private static readonly Assembly app = typeof(Deskair.App.Program).Assembly;

    private static IEnumerable<Assembly> LowerLayers => [domain, services, sensors, storage];

    private static void AssertPasses(NetArchTest.Rules.TestResult result, string rule)
    {
        var offenders = result.FailingTypeNames ?? [];

        Assert.True(result.IsSuccessful, $"{rule}. Offending types: {string.Join(", ", offenders)}");
    }

    [Theory]
    [InlineData("Deskair.App.Ui")]
    [InlineData("Deskair.App.ViewModels")]
    [InlineData("Deskair.App.Localization")]
    [InlineData("Deskair.App.Platform")]
    public void Rule1_only_Composition_uses_Sensors(string layer)
    {
        var result = Types.InAssembly(app)
            .That().ResideInNamespaceStartingWith(layer)
            .ShouldNot().HaveDependencyOn("Deskair.Sensors")
            .GetResult();

        AssertPasses(result, $"{layer} must not use Deskair.Sensors");
    }

    [Theory]
    [InlineData("Deskair.App.Ui")]
    [InlineData("Deskair.App.ViewModels")]
    public void Rule2_Ui_and_ViewModels_do_not_use_Storage_or_Platform(string layer)
    {
        var result = Types.InAssembly(app)
            .That().ResideInNamespaceStartingWith(layer)
            .ShouldNot().HaveDependencyOnAny("Deskair.Storage", "Deskair.App.Platform")
            .GetResult();

        AssertPasses(result, $"{layer} must not use Storage or Platform");
    }

    [Fact]
    public void Rule3_ViewModels_do_not_use_WPF()
    {
        var result = Types.InAssembly(app)
            .That().ResideInNamespaceStartingWith("Deskair.App.ViewModels")
            .ShouldNot().HaveDependencyOn("System.Windows")
            .GetResult();

        AssertPasses(result, "View models must not use System.Windows");
    }

    [Fact]
    public void Rule4_lower_layers_do_not_use_WPF_or_Windows_APIs()
    {
        foreach (var assembly in LowerLayers)
        {
            var result = Types.InAssembly(assembly)
                .ShouldNot().HaveDependencyOnAny("System.Windows", "Windows", "Microsoft.Win32")
                .GetResult();

            AssertPasses(result, $"{assembly.GetName().Name} must not use WPF or Windows APIs");
        }
    }

    [Fact]
    public void Rule5_Domain_does_not_use_IO_or_TimeProvider()
    {
        var result = Types.InAssembly(domain)
            .ShouldNot().HaveDependencyOnAny("System.IO", "System.TimeProvider")
            .GetResult();

        AssertPasses(result, "Domain must not use System.IO or TimeProvider");
    }

    [Fact]
    public void Rule5_nothing_reads_the_current_time_directly()
    {
        var forbidden = new HashSet<string> { "get_Now", "get_UtcNow", "get_Today" };

        foreach (var assembly in new[] { domain, services, sensors, storage, app })
        {
            var offenders = FindMemberReferences(assembly, new[] { "DateTime", "DateTimeOffset" }, forbidden);

            Assert.True(
                offenders.Count == 0,
                $"{assembly.GetName().Name} reads the current time directly ({string.Join(", ", offenders)}). Inject TimeProvider instead.");
        }
    }

    [Fact]
    public void Rule6_lower_layers_do_not_look_up_localized_text()
    {
        foreach (var assembly in LowerLayers)
        {
            var result = Types.InAssembly(assembly)
                .ShouldNot().HaveDependencyOnAny("System.Resources", "Deskair.App")
                .GetResult();

            AssertPasses(result, $"{assembly.GetName().Name} must not produce user-visible text");
        }
    }

    [Fact]
    public void No_vendor_name_outside_Sensors_and_Composition()
    {
        var typeNames = new List<string>();

        foreach (var assembly in new[] { domain, services, storage })
        {
            typeNames.AddRange(assembly.GetTypes().Select(type => type.FullName ?? type.Name));
        }

        typeNames.AddRange(app.GetTypes()
            .Where(type => !(type.Namespace ?? string.Empty).StartsWith("Deskair.App.Composition", StringComparison.Ordinal))
            .Select(type => type.FullName ?? type.Name));

        var offenders = typeNames.Where(name => name.Contains("Aranet", StringComparison.OrdinalIgnoreCase)).ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Project_references_follow_the_documented_direction()
    {
        Assert.Empty(ReferencedProjects(domain));
        Assert.Equal(new[] { "Deskair.Domain" }, ReferencedProjects(services));
        Assert.Equal(new[] { "Deskair.Domain", "Deskair.Services" }, ReferencedProjects(sensors));
        Assert.Equal(new[] { "Deskair.Domain", "Deskair.Services" }, ReferencedProjects(storage));
        Assert.Equal(
            new[] { "Deskair.Domain", "Deskair.Sensors", "Deskair.Services", "Deskair.Storage" },
            ReferencedProjects(app));
    }

    private static string[] ReferencedProjects(Assembly assembly)
    {
        return assembly.GetReferencedAssemblies()
            .Select(name => name.Name ?? string.Empty)
            .Where(name => name.StartsWith("Deskair.", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }

    private static List<string> FindMemberReferences(Assembly assembly, string[] typeNames, HashSet<string> memberNames)
    {
        using var stream = File.OpenRead(assembly.Location);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        var offenders = new List<string>();

        foreach (var handle in reader.MemberReferences)
        {
            var member = reader.GetMemberReference(handle);
            var name = reader.GetString(member.Name);

            if (!memberNames.Contains(name) || member.Parent.Kind != HandleKind.TypeReference)
            {
                continue;
            }

            var parent = reader.GetTypeReference((TypeReferenceHandle)member.Parent);
            var parentName = reader.GetString(parent.Name);

            if (typeNames.Contains(parentName))
            {
                offenders.Add($"{parentName}.{name}");
            }
        }

        return offenders;
    }
}

