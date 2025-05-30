// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Storage;

/// <summary>
/// 表示插件关系类型映射源。
///     Represents a plugin relational type mapping source.
/// </summary>
/// <remarks>
///     <para>
///         The service lifetime is <see cref="ServiceLifetime.Singleton" /> and multiple registrations
///         are allowed. This means a single instance of each service is used by many <see cref="DbContext" />
///         instances. The implementation must be thread-safe.
///         This service cannot depend on services registered as <see cref="ServiceLifetime.Scoped" />.
///     </para>
///     <para>
///         See <see href="https://aka.ms/efcore-docs-providers">Implementation of database providers and extensions</see>
///         for more information and examples.
///     </para>
/// </remarks>
public interface IRelationalTypeMappingSourcePlugin
{
    /// <summary>
    ///     Finds a type mapping for the given info.
    /// </summary>
    /// <param name="mappingInfo">The mapping info to use to create the mapping.</param>
    /// <returns>The type mapping, or <see langword="null" /> if none could be found.</returns>
    RelationalTypeMapping? FindMapping(in RelationalTypeMappingInfo mappingInfo);
}
