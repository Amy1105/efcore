// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Storage;

/// <summary>
///     <para>
///     生成要针对关系数据库执行的命令。
///         Builds a command to be executed against a relational database.
///     </para>
///     <para>
///         This type is typically used by database providers (and other extensions). It is generally
///         not used in application code.
///     </para>
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-providers">Implementation of database providers and extensions</see>
///     for more information and examples.
/// </remarks>
public interface IRelationalCommandBuilder
{
    /// <summary>
    ///     The collection of parameters.
    ///     参数的集合。
    /// </summary>
    IReadOnlyList<IRelationalParameter> Parameters { get; }

    /// <summary>
    ///     Adds the given parameter to this command.
    ///     将给定的参数添加到此命令中。
    /// </summary>
    /// <param name="parameter">The parameter.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    IRelationalCommandBuilder AddParameter(IRelationalParameter parameter);

    /// <summary>
    ///     Removes the parameter with the given index from this command.
    ///     从该命令中删除具有给定索引的参数。
    /// </summary>
    /// <param name="index">The index of the parameter to be removed.
    /// 要删除的参数的索引。
    /// </param>
    /// <returns>The same builder instance so that multiple calls can be chained.
    /// 相同的构建器实例，以便可以链接多个调用。</returns>
    IRelationalCommandBuilder RemoveParameterAt(int index);

    /// <summary>
    ///     The source for <see cref="RelationalTypeMapping" />s to use.
    /// </summary>
    [Obsolete("Code trying to add parameter should add type mapped parameter using TypeMappingSource directly.")]
    IRelationalTypeMappingSource TypeMappingSource { get; }

    /// <summary>
    ///     Creates the command.
    ///     创建命令。
    /// </summary>
    /// <returns>The newly created command.</returns>
    IRelationalCommand Build();

    /// <summary>
    ///     Appends an object to the command text.
    ///     将对象追加到命令文本中。
    /// </summary>
    /// <param name="value">The object to be written.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    IRelationalCommandBuilder Append(string value);

    /// <summary>
    ///     Appends a blank line to the command text.
    ///     在命令文本后追加一个空行。
    /// </summary>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    IRelationalCommandBuilder AppendLine();

    /// <summary>
    ///     Increments the indent of subsequent lines.
    ///     增加后续行的缩进量。
    /// </summary>
    /// <returns>The same builder instance so that multiple calls can be chained.
    /// 相同的构建器实例，以便可以链接多个调用。
    /// </returns>
    IRelationalCommandBuilder IncrementIndent();

    /// <summary>
    ///     Decrements the indent of subsequent lines.
    ///     减少后续行的缩进量。
    /// </summary>
    /// <returns>The same builder instance so that multiple calls can be chained.
    /// 相同的构建器实例，以便可以链接多个调用。
    /// </returns>
    IRelationalCommandBuilder DecrementIndent();

    /// <summary>
    ///     Gets the length of the command text.
    ///     获取命令文本的长度。
    /// </summary>
    int CommandTextLength { get; }
}
