// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Data.Sqlite;

namespace Microsoft.EntityFrameworkCore.Query;

#nullable disable

public class SqlQuerySqliteTest : SqlQueryTestBase<NorthwindQuerySqliteFixture<NoopModelCustomizer>>
{
    public SqlQuerySqliteTest(NorthwindQuerySqliteFixture<NoopModelCustomizer> fixture, ITestOutputHelper testOutputHelper)
        : base(fixture)
        => Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);

    public override async Task SqlQueryRaw_queryable_composed(bool async)
    {
        await base.SqlQueryRaw_queryable_composed(async);

        AssertSql(
            """
SELECT "m"."Address", "m"."City", "m"."CompanyName", "m"."ContactName", "m"."ContactTitle", "m"."Country", "m"."CustomerID", "m"."Fax", "m"."Phone", "m"."Region", "m"."PostalCode"
FROM (
    SELECT * FROM "Customers"
) AS "m"
WHERE instr("m"."ContactName", 'z') > 0
""");
    }

    public override async Task<string> SqlQueryRaw_queryable_with_parameters_and_closure(bool async)
    {
        var queryString = await base.SqlQueryRaw_queryable_with_parameters_and_closure(async);

        Assert.Equal(
            """
.param set p0 'London'
.param set @contactTitle 'Sales Representative'

SELECT "m"."Address", "m"."City", "m"."CompanyName", "m"."ContactName", "m"."ContactTitle", "m"."Country", "m"."CustomerID", "m"."Fax", "m"."Phone", "m"."Region", "m"."PostalCode"
FROM (
    SELECT * FROM "Customers" WHERE "City" = @p0
) AS "m"
WHERE "m"."ContactTitle" = @contactTitle
""",
            queryString, ignoreLineEndingDifferences: true);

        return queryString;
    }

    public override Task Bad_data_error_handling_invalid_cast_key(bool async)
        // Not supported on SQLite
        => Task.CompletedTask;

    public override Task Bad_data_error_handling_invalid_cast(bool async)
        // Not supported on SQLite
        => Task.CompletedTask;

    public override Task Bad_data_error_handling_invalid_cast_projection(bool async)
        // Not supported on SQLite
        => Task.CompletedTask;

    public override Task Bad_data_error_handling_invalid_cast_no_tracking(bool async)
        // Not supported on SQLite
        => Task.CompletedTask;

    public override async Task SqlQueryRaw_composed_with_common_table_expression(bool async)
    {
        await base.SqlQueryRaw_composed_with_common_table_expression(async);

        AssertSql(
            """
SELECT "m"."Address", "m"."City", "m"."CompanyName", "m"."ContactName", "m"."ContactTitle", "m"."Country", "m"."CustomerID", "m"."Fax", "m"."Phone", "m"."Region", "m"."PostalCode"
FROM (
    WITH "Customers2" AS (
        SELECT * FROM "Customers"
    )
    SELECT * FROM "Customers2"
) AS "m"
WHERE instr("m"."ContactName", 'z') > 0
""");
    }

    public override async Task SqlQueryRaw_then_String_Length(bool async)
    {
        await base.SqlQueryRaw_then_String_Length(async);

        AssertSql(
            """
SELECT "s"."Value"
FROM (
    SELECT 'x' AS "Value" FROM "Customers"
) AS "s"
WHERE length("s"."Value") = 0
""");
    }

    public override async Task SqlQueryRaw_then_String_ToUpper_String_Length(bool async)
    {
        await base.SqlQueryRaw_then_String_ToUpper_String_Length(async);

        AssertSql(
            """
SELECT "s"."Value"
FROM (
    SELECT 'x' AS "Value" FROM "Customers"
) AS "s"
WHERE length(upper("s"."Value")) = 0
""");
    }

    protected override DbParameter CreateDbParameter(string name, object value)
        => new SqliteParameter { ParameterName = name, Value = value };

    private void AssertSql(params string[] expected)
        => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);
}
