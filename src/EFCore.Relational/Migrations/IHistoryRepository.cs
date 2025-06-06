// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Migrations;

/// <summary>
/// 用于访问跟踪元数据的“__EFMigrationsHistory”表的存储库接口关于EF核心迁移，例如应用了哪些迁移。
///     An interface for the repository used to access the '__EFMigrationsHistory' table that tracks metadata
///     about EF Core Migrations such as which migrations have been applied.
/// </summary>
/// <remarks>
///     <para>
///         Database providers typically implement this service by inheriting from <see cref="HistoryRepository" />.
///     </para>
///     <para>
///         The service lifetime is <see cref="ServiceLifetime.Scoped" />. This means that each
///         <see cref="DbContext" /> instance will use its own instance of this service.
///         The implementation may depend on other services registered with any lifetime.
///         The implementation does not need to be thread-safe.
///     </para>
///     <para>
///         See <see href="https://aka.ms/efcore-docs-migrations">Database migrations</see> for more information and examples.
///     </para>
/// </remarks>
public interface IHistoryRepository
{
    /// <summary>
    ///     Checks whether the history table exists.
    /// </summary>
    /// <returns><see langword="true" /> if the table already exists, <see langword="false" /> otherwise.</returns>
    bool Exists();

    /// <summary>
    ///     Checks whether the history table exists.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains
    ///     <see langword="true" /> if the table already exists, <see langword="false" /> otherwise.
    /// </returns>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    Task<bool> ExistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates the history table.
    /// </summary>
    void Create();

    /// <summary>
    ///     Creates the history table.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    /// </returns>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    Task CreateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates the history table if it doesn't exist.
    /// </summary>
    /// <returns><see langword="true" /> if the table was created, <see langword="false" /> otherwise.</returns>
    bool CreateIfNotExists()
    {
        if (!Exists())
        {
            Create();
            return true;
        }
        return false;
    }

    /// <summary>
    ///     Creates the history table.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains
    ///     <see langword="true" /> if the table was created, <see langword="false" /> otherwise.
    /// </returns>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    async Task<bool> CreateIfNotExistsAsync(CancellationToken cancellationToken = default)
    {
        if (!await ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            await CreateAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        return false;
    }

    /// <summary>
    ///     Queries the history table for all migrations that have been applied.
    /// </summary>
    /// <returns>The list of applied migrations, as <see cref="HistoryRow" /> entities.</returns>
    IReadOnlyList<HistoryRow> GetAppliedMigrations();

    /// <summary>
    ///     Queries the history table for all migrations that have been applied.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains
    ///     the list of applied migrations, as <see cref="HistoryRow" /> entities.
    /// </returns>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    Task<IReadOnlyList<HistoryRow>> GetAppliedMigrationsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     The condition under witch the lock is released implicitly.
    /// </summary>
    LockReleaseBehavior LockReleaseBehavior { get; }

    /// <summary>
    ///     Acquires an exclusive lock on the database.
    /// </summary>
    /// <returns>An object that can be disposed to release the lock.</returns>
    IMigrationsDatabaseLock AcquireDatabaseLock();

    /// <summary>
    ///     Acquires an exclusive lock on the database asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>An object that can be disposed to release the lock.</returns>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    Task<IMigrationsDatabaseLock> AcquireDatabaseLockAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Generates a SQL script that will create the history table.
    /// </summary>
    /// <returns>The SQL script.</returns>
    string GetCreateScript();

    /// <summary>
    ///     Generates a SQL script that will create the history table if and only if it does not already exist.
    /// </summary>
    /// <returns>The SQL script.</returns>
    string GetCreateIfNotExistsScript();

    /// <summary>
    ///     Generates a SQL script to insert a row into the history table.
    /// </summary>
    /// <param name="row">The row to insert, represented as a <see cref="HistoryRow" /> entity.</param>
    /// <returns>The generated SQL.</returns>
    string GetInsertScript(HistoryRow row);

    /// <summary>
    ///     Generates a SQL script to delete a row from the history table.
    /// </summary>
    /// <param name="migrationId">The migration identifier of the row to delete.</param>
    /// <returns>The generated SQL.</returns>
    string GetDeleteScript(string migrationId);

    /// <summary>
    ///     Generates a SQL Script that will <c>BEGIN</c> a block
    ///     of SQL if and only if the migration with the given identifier does not already exist in the history table.
    /// </summary>
    /// <param name="migrationId">The migration identifier.</param>
    /// <returns>The generated SQL.</returns>
    string GetBeginIfNotExistsScript(string migrationId);

    /// <summary>
    ///     Generates a SQL Script that will <c>BEGIN</c> a block
    ///     of SQL if and only if the migration with the given identifier already exists in the history table.
    /// </summary>
    /// <param name="migrationId">The migration identifier.</param>
    /// <returns>The generated SQL.</returns>
    string GetBeginIfExistsScript(string migrationId);

    /// <summary>
    ///     Generates a SQL script to <c>END</c> the SQL block.
    /// </summary>
    /// <returns>The generated SQL.</returns>
    string GetEndIfScript();
}
