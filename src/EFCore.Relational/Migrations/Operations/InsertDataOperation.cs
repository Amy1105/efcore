// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;

namespace Microsoft.EntityFrameworkCore.Migrations.Operations;

/// <summary>
///     A <see cref="MigrationOperation" /> for inserting seed data into a table.
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-migrations">Database migrations</see> for more information and examples.
/// </remarks>
[DebuggerDisplay("INSERT INTO {Table}")]
public class InsertDataOperation : MigrationOperation, ITableMigrationOperation
{
    /// <summary>
    ///     The name of the table into which data will be inserted.
    /// </summary>
    public virtual string Table { get; set; } = null!;

    /// <summary>
    ///     The schema that contains the table, or <see langword="null" /> if the default schema should be used.
    /// </summary>
    public virtual string? Schema { get; set; }

    /// <summary>
    ///     A list of column names that represent the columns into which data will be inserted.
    /// </summary>
    public virtual string[] Columns { get; set; } = null!;

    /// <summary>
    ///     A list of store types for the columns into which data will be inserted.
    /// </summary>
    public virtual string[]? ColumnTypes { get; set; }

    /// <summary>
    /// 二维数组，【行，每行数据的各个字段值】要插入的数据，表示为值数组列表，其中每个数组中的值对应于"Columns" 属性中的列。
    ///     The data to be inserted, represented as a list of value arrays where each
    ///     value in the array corresponds to a column in the <see cref="Columns" /> property.
    /// </summary>
    public virtual object?[,] Values { get; set; } = null!;
}
