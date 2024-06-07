// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.EntityFrameworkCore.Infrastructure;

/// <summary>
///     <para>
///     一个支持注释的类。注释允许在对象上存储任意元数据。
///         A class that supports annotations. Annotations allow for arbitrary metadata to be stored on an object.
///     </para>
///     <para>
///         This interface is typically used by database providers (and other extensions). It is generally
///         not used in application code.
///     </para>
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-providers">Implementation of database providers and extensions</see>
///     for more information and examples.
/// </remarks>
public interface IReadOnlyAnnotatable
{
    /// <summary>
    /// 获取具有给定名称的注释的值，如果不存在，则返回null。
    ///     Gets the value of the annotation with the given name, returning <see langword="null" /> if it does not exist.
    /// </summary>
    /// <param name="name">The name of the annotation to find.</param>
    /// <returns>
    ///     The value of the existing annotation if an annotation with the specified name already exists. Otherwise, <see langword="null" />.
    /// </returns>
    object? this[string name] { get; }

    /// <summary>
    /// 获取具有给定名称的注释，如果不存在，则返回null。
    ///     Gets the annotation with the given name, returning <see langword="null" /> if it does not exist.
    /// </summary>
    /// <param name="name">The name of the annotation to find.</param>
    /// <returns>
    ///     The existing annotation if an annotation with the specified name already exists. Otherwise, <see langword="null" />.
    /// </returns>
    IAnnotation? FindAnnotation(string name);

    /// <summary>
    /// 获取当前对象的所有批注。
    ///     Gets all annotations on the current object.
    /// </summary>
    IEnumerable<IAnnotation> GetAnnotations();

    /// <summary>
    /// 获取具有给定名称的批注，如果该批注不存在则抛出。
    ///     Gets the annotation with the given name, throwing if it does not exist.
    /// </summary>
    /// <param name="annotationName">The key of the annotation to find.</param>
    /// <returns>The annotation with the specified name.</returns>
    IAnnotation GetAnnotation(string annotationName)
        => AnnotatableBase.GetAnnotation(this, annotationName);

    /// <summary>
    /// 获取对象上声明的所有批注的调试字符串。
    ///     Gets the debug string for all annotations declared on the object.
    /// </summary>
    /// <param name="indent">The number of indent spaces to use before each new line.</param>
    /// <returns>Debug string representation of all annotations.</returns>
    string AnnotationsToDebugString(int indent = 0)
    {
        var annotations = GetAnnotations().ToList();
        if (annotations.Count == 0)
        {
            return "";
        }

        var builder = new StringBuilder();
        var indentString = new string(' ', indent);

        builder.AppendLine().Append(indentString).Append("Annotations: ");
        foreach (var annotation in annotations)
        {
            builder
                .AppendLine()
                .Append(indentString)
                .Append("  ")
                .Append(annotation.Name)
                .Append(": ")
                .Append(annotation.Value);
        }

        return builder.ToString();
    }
}
