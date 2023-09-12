using SqlKata.Compilers;
using SqlKata.Tests.Infrastructure;
using Xunit;

namespace SqlKata.Tests.SqlServer
{
    public class SqlServerTests : TestSupport
    {
        private readonly SqlServerCompiler compiler;

        public SqlServerTests()
        {
            compiler = Compilers.Get<SqlServerCompiler>(EngineCodes.SqlServer);
        }


        [Fact]
        public void SqlServerTop()
        {
            var query = new Query("table").Limit(1);
            var result = compiler.Compile(query);
            Assert.Equal("SELECT TOP (@p0) * FROM [table]", result.Sql);
        }

        [Fact]
        public void SqlServerTopWithDistinct()
        {
            var query = new Query("table").Limit(1).Distinct();
            var result = compiler.Compile(query);
            Assert.Equal("SELECT DISTINCT TOP (@p0) * FROM [table]", result.Sql);
        }


        [Theory()]
        [InlineData(-100)]
        [InlineData(0)]
        public void OffsetSqlServer_Should_Be_Ignored_If_Zero_Or_Negative(int offset)
        {
            var q = new Query().From("users").Offset(offset);
            var c = Compilers.CompileFor(EngineCodes.SqlServer, q);

            Assert.Equal("SELECT * FROM [users]", c.ToString());
        }


        [Theory()]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(100)]
        [InlineData(1000000)]
        public void OffsetSqlServer_Should_Be_Incremented_By_One(int offset)
        {
            var q = new Query().From("users").Offset(offset);
            var c = Compilers.CompileFor(EngineCodes.SqlServer, q);
            Assert.Equal(
                "SELECT * FROM (SELECT *, ROW_NUMBER() OVER (ORDER BY (SELECT 0)) AS [row_num] FROM [users]) AS [results_wrapper] WHERE [row_num] >= " +
                (offset + 1), c.ToString());
        }

        [Fact()]
        public void TestNested_Should_Compile()
        {
            var q = new Query().Select(@"Test.{
                 Code as Test,
                 Test1 as FromValue
            }").From("Test");

            var q2 = new Query().Select(@"Test.{ Code, Test1 }").From("Test");

            var c = Compilers.CompileFor(EngineCodes.SqlServer, q);
            var c2 = Compilers.CompileFor(EngineCodes.SqlServer, q2);

            Assert.Equal("SELECT [Test].[Code] AS [Test], [Test].[Test1] AS [FromValue] FROM [Test]", c.ToString());
            Assert.Equal("SELECT [Test].[Code], [Test].[Test1] FROM [Test]", c2.ToString());
        }

        [Fact()]
        public void VerbatimString_Should_Not_Be_Escaped()
        {
            var q = new Query().From("Test")
                .Select(@"Test.{
                    AvailDate as Date,
                    Avail as Value
                }",
                "Test.FromValue as FromValue",
                @"Test.{
                    ShouldCompile AS ShouldCompile
                }");

            var c = Compilers.CompileFor(EngineCodes.SqlServer, q);

            Assert.Equal("SELECT [Test].[AvailDate] AS [Date], [Test].[Avail] AS [Value], [Test].[FromValue] AS [FromValue], [Test].[ShouldCompile] AS [ShouldCompile] FROM [Test]", c.ToString());
        }
    }
}
