// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Infrastructure;

/// <summary>
///     <para>
///     公开生成时和运行时注释的类。注释允许在对象上存储任意元数据。
///         A class that exposes build-time and run-time annotations. Annotations allow for arbitrary metadata to be stored on an object.
///     </para>
///     <para>
///     此接口通常由数据库提供程序（和其他扩展）使用。一般来说未在应用程序代码中使用。
///         This interface is typically used by database providers (and other extensions). It is generally
///         not used in application code.
///     </para>
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-providers">Implementation of database providers and extensions</see>
///     for more information and examples.
/// </remarks>
public interface IAnnotatable : IReadOnlyAnnotatable
{
    /// <summary>
    ///     Gets the runtime annotation with the given name, returning <see langword="null" /> if it does not exist.
    /// </summary>
    /// <param name="name">The name of the annotation to find.</param>
    /// <returns>
    ///     The existing runtime annotation if an annotation with the specified name already exists. Otherwise, <see langword="null" />.
    /// </returns>
    IAnnotation? FindRuntimeAnnotation(string name);

    /// <summary>
    ///     Gets the value of the runtime annotation with the given name, returning <see langword="null" /> if it does not exist.
    /// </summary>
    /// <param name="name">The name of the annotation to find.</param>
    /// <returns>
    ///     The value of the existing runtime annotation if an annotation with the specified name already exists.
    ///     Otherwise, <see langword="null" />.
    /// </returns>
    object? FindRuntimeAnnotationValue(string name)
        => FindRuntimeAnnotation(name)?.Value;

    /// <summary>
    ///     Gets all the runtime annotations on the current object.
    /// </summary>
    IEnumerable<IAnnotation> GetRuntimeAnnotations();

    /// <summary>
    ///     Adds a runtime annotation to this object. Throws if an annotation with the specified name already exists.
    /// </summary>
    /// <param name="name">The name of the annotation to be added.</param>
    /// <param name="value">The value to be stored in the annotation.</param>
    /// <returns>The newly added annotation.</returns>
    IAnnotation AddRuntimeAnnotation(string name, object? value);

    /// <summary>
    ///     Sets the runtime annotation stored under the given key. Overwrites the existing annotation if an
    ///     annotation with the specified name already exists.
    /// </summary>
    /// <param name="name">The name of the annotation to be added.</param>
    /// <param name="value">The value to be stored in the annotation.</param>
    /// <returns>The newly added annotation.</returns>
    IAnnotation SetRuntimeAnnotation(string name, object? value);

    /// <summary>
    ///     Removes the given runtime annotation from this object.
    /// </summary>
    /// <param name="name">The name of the annotation to remove.</param>
    /// <returns>The annotation that was removed.</returns>
    IAnnotation? RemoveRuntimeAnnotation(string name);

    /// <summary>
    ///     Gets the value of the runtime annotation with the given name, adding it if one does not exist.
    /// </summary>
    /// <param name="name">The name of the annotation.</param>
    /// <param name="valueFactory">The factory used to create the value if the annotation doesn't exist.</param>
    /// <param name="factoryArgument">An argument for the factory method.</param>
    /// <returns>
    ///     The value of the existing runtime annotation if an annotation with the specified name already exists.
    ///     Otherwise a newly created value.
    /// </returns>
    TValue GetOrAddRuntimeAnnotationValue<TValue, TArg>(
        string name,
        Func<TArg?, TValue> valueFactory,
        TArg? factoryArgument);
}
