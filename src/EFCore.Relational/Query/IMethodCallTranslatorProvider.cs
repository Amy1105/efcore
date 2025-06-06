// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Microsoft.EntityFrameworkCore.Query;

/// <summary>
///    为表示标量方法的LINQ表达式提供翻译。
///     Provides translations for LINQ <see cref="MethodCallExpression" /> expressions which represents scalar methods.
/// </summary>
/// <remarks>
///     The service lifetime is <see cref="ServiceLifetime.Scoped" /> and multiple registrations
///     are allowed. This means that each <see cref="DbContext" /> instance will use its own
///     set of instances of this service.
///     The implementations may depend on other services registered with any lifetime.
///     The implementations do not need to be thread-safe.
/// </remarks>
public interface IMethodCallTranslatorProvider
{
    /// <summary>
    ///     Translates a LINQ <see cref="MethodCallExpression" /> to a SQL equivalent.
    /// </summary>
    /// <param name="model">A model to use for translation.</param>
    /// <param name="instance">A SQL representation of <see cref="MethodCallExpression.Object" />.</param>
    /// <param name="method">The method info from <see cref="MethodCallExpression.Method" />.</param>
    /// <param name="arguments">SQL representations of <see cref="MethodCallExpression.Arguments" />.</param>
    /// <param name="logger">The query logger to use.</param>
    /// <returns>A SQL translation of the <see cref="MethodCallExpression" />.</returns>
    SqlExpression? Translate(
        IModel model,
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger);
}
