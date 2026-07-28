using NetArchTest.Rules;
using FluentAssertions;

namespace Kst.ArchitectureTests;

/// <summary>
/// Enforces project dependency rules across the solution.
/// </summary>
public sealed class DependencyRuleTests
{
    private static readonly string[] InfrastructureNamespaces =
    [
        "Kst.Infrastructure",
        "Kst.Integrations.Qad",
        "Kst.Integrations.Shortages",
        "Kst.Exports"
    ];

    [Fact]
    public void Domain_Does_Not_Reference_Infrastructure()
    {
        var result = Types.InAssembly(typeof(Kst.Domain.Common.IClock).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespaces)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Kst.Domain must not depend on infrastructure or integration projects. " +
                     "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_Does_Not_Reference_AspNetCore()
    {
        var result = Types.InAssembly(typeof(Kst.Domain.Common.IClock).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Kst.Domain must not depend on ASP.NET Core.");
    }

    [Fact]
    public void Application_Does_Not_Reference_AspNetCore()
    {
        var result = Types.InAssembly(typeof(Kst.Application.SystemStatus.GetSystemStatusQuery).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Kst.Application must not depend on ASP.NET Core.");
    }

    [Fact]
    public void Application_Does_Not_Reference_SqlServer()
    {
        var result = Types.InAssembly(typeof(Kst.Application.SystemStatus.GetSystemStatusQuery).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.Data.SqlClient", "Dapper")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Kst.Application must not depend on SQL Server implementation packages.");
    }

    [Fact]
    public void Domain_Does_Not_Reference_Api()
    {
        var result = Types.InAssembly(typeof(Kst.Domain.Common.IClock).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Kst.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Kst.Domain must not depend on Kst.Api.");
    }

    [Fact]
    public void Integration_Projects_Do_Not_Reference_Api()
    {
        var qadAssembly = typeof(Kst.Integrations.Qad.Connectivity.IQadConnectivityCheck).Assembly;
        var shortagesAssembly = typeof(Kst.Integrations.Shortages.Connectivity.IShortagesConnectivityCheck).Assembly;

        foreach (var assembly in new[] { qadAssembly, shortagesAssembly })
        {
            var result = Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOn("Kst.Api")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                because: $"{assembly.GetName().Name} must not depend on Kst.Api.");
        }
    }
}
