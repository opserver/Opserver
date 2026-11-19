using Microsoft.Data.SqlClient;
using Opserver.Helpers;
using Xunit;

namespace Opserver.Tests
{
    /// <summary>
    /// Guards the connection-string compatibility shim used when moving from System.Data.SqlClient
    /// (Encrypt defaults to false) to Microsoft.Data.SqlClient (Encrypt defaults to true).
    /// </summary>
    public class ConnectionTests
    {
        [Theory]
        [InlineData("Data Source=.;Initial Catalog=master;Integrated Security=SSPI;",
                    "Data Source=.;Initial Catalog=master;Integrated Security=SSPI;Encrypt=False")]
        [InlineData("Data Source=.;Initial Catalog=master",
                    "Data Source=.;Initial Catalog=master;Encrypt=False")]
        [InlineData("Data Source=.;Initial Catalog=master;   ",
                    "Data Source=.;Initial Catalog=master;Encrypt=False")]
        // Explicit settings must never be overridden.
        [InlineData("Data Source=.;Encrypt=True;", "Data Source=.;Encrypt=True;")]
        [InlineData("Data Source=.;Encrypt=False;", "Data Source=.;Encrypt=False;")]
        [InlineData("Data Source=.;Encrypt=Strict;TrustServerCertificate=True;",
                    "Data Source=.;Encrypt=Strict;TrustServerCertificate=True;")]
        [InlineData("data source=.;encrypt = mandatory;", "data source=.;encrypt = mandatory;")]
        [InlineData(null, null)]
        [InlineData("", "")]
        public void LegacyEncryptionDefault(string input, string expected)
        {
            Assert.Equal(expected, Connection.WithLegacyEncryptionDefault(input));
        }

        [Theory]
        [InlineData("Data Source=.;Initial Catalog=master;Integrated Security=SSPI;", "False")]
        [InlineData("Data Source=.;Initial Catalog=master;Encrypt=True", "True")]
        [InlineData("Data Source=.;Initial Catalog=master;Encrypt=False", "False")]
        [InlineData("Data Source=.;Initial Catalog=master;Encrypt=Strict", "Strict")]
        public void DriverHonorsTheNormalizedConnectionString(string input, string expectedEncrypt)
        {
            var normalized = Connection.WithLegacyEncryptionDefault(input);
            var builder = new SqlConnectionStringBuilder(normalized);
            Assert.Equal(expectedEncrypt, builder.Encrypt.ToString());
        }
    }
}
