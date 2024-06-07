// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Storage;

/// <summary>
///     <para>
///     ����Ҫ��Թ�ϵ���ݿ�ִ�е����
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
    ///     �����ļ��ϡ�
    /// </summary>
    IReadOnlyList<IRelationalParameter> Parameters { get; }

    /// <summary>
    ///     Adds the given parameter to this command.
    ///     �������Ĳ�����ӵ��������С�
    /// </summary>
    /// <param name="parameter">The parameter.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    IRelationalCommandBuilder AddParameter(IRelationalParameter parameter);

    /// <summary>
    ///     Removes the parameter with the given index from this command.
    ///     �Ӹ�������ɾ�����и��������Ĳ�����
    /// </summary>
    /// <param name="index">The index of the parameter to be removed.
    /// Ҫɾ���Ĳ�����������
    /// </param>
    /// <returns>The same builder instance so that multiple calls can be chained.
    /// ��ͬ�Ĺ�����ʵ�����Ա�������Ӷ�����á�</returns>
    IRelationalCommandBuilder RemoveParameterAt(int index);

    /// <summary>
    ///     The source for <see cref="RelationalTypeMapping" />s to use.
    /// </summary>
    [Obsolete("Code trying to add parameter should add type mapped parameter using TypeMappingSource directly.")]
    IRelationalTypeMappingSource TypeMappingSource { get; }

    /// <summary>
    ///     Creates the command.
    ///     �������
    /// </summary>
    /// <returns>The newly created command.</returns>
    IRelationalCommand Build();

    /// <summary>
    ///     Appends an object to the command text.
    ///     ������׷�ӵ������ı��С�
    /// </summary>
    /// <param name="value">The object to be written.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    IRelationalCommandBuilder Append(string value);

    /// <summary>
    ///     Appends a blank line to the command text.
    ///     �������ı���׷��һ�����С�
    /// </summary>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    IRelationalCommandBuilder AppendLine();

    /// <summary>
    ///     Increments the indent of subsequent lines.
    ///     ���Ӻ����е���������
    /// </summary>
    /// <returns>The same builder instance so that multiple calls can be chained.
    /// ��ͬ�Ĺ�����ʵ�����Ա�������Ӷ�����á�
    /// </returns>
    IRelationalCommandBuilder IncrementIndent();

    /// <summary>
    ///     Decrements the indent of subsequent lines.
    ///     ���ٺ����е���������
    /// </summary>
    /// <returns>The same builder instance so that multiple calls can be chained.
    /// ��ͬ�Ĺ�����ʵ�����Ա�������Ӷ�����á�
    /// </returns>
    IRelationalCommandBuilder DecrementIndent();

    /// <summary>
    ///     Gets the length of the command text.
    ///     ��ȡ�����ı��ĳ��ȡ�
    /// </summary>
    int CommandTextLength { get; }
}
