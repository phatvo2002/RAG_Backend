using Nexora.Infrastructure.Services.Common;

namespace Nexora.Tests.Infrastructure;

public sealed class DotEnvConfigurationTests
{
    [Fact]
    public void ReadsDotNetKeysAndQuotedSecretsFromRepositoryRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"nexora-dotenv-{Guid.NewGuid():N}");
        var child = Path.Combine(root, "src", "Nexora.WebApi");
        Directory.CreateDirectory(child);

        try
        {
            File.WriteAllText(Path.Combine(root, ".env"),
                "# Local configuration\n" +
                "ConnectionStrings__Postgres='Host=postgres;Password=abc#;xyz'\n" +
                "Storage__SecretKey=example # comment\n" +
                "Qdrant__ApiKey=key#part\n");

            var values = DotEnvConfiguration.ReadFromAncestors(child);

            Assert.Equal("Host=postgres;Password=abc#;xyz", values["ConnectionStrings:Postgres"]);
            Assert.Equal("example", values["Storage:SecretKey"]);
            Assert.Equal("key#part", values["Qdrant:ApiKey"]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
