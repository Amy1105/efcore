// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Transactions;

namespace Microsoft.EntityFrameworkCore.Storage;

/// <summary>
///     The base class for <see cref="IExecutionStrategy" /> implementations.
/// </summary>
/// <remarks>
///     <para>
///     ʹ������Ϊ<see cref="ServiceLifetime.Scoped"/>������ζ��ÿ��"DbContext"/>ʵ������ʹ���Լ��ĸ÷���ʵ����
///     ��ʵ�ֿ������������κ�������ע�����������ʵ�ֲ���Ҫ���̰߳�ȫ�ġ�
///         The service lifetime is <see cref="ServiceLifetime.Scoped" />. This means that each
///         <see cref="DbContext" /> instance will use its own instance of this service.
///         The implementation may depend on other services registered with any lifetime.
///         The implementation does not need to be thread-safe.
///     </para>
///     <para>
///         See <see href="https://aka.ms/efcore-docs-connection-resiliency">Connection resiliency and database retries</see>
///         for more information and examples.
///     </para>
/// </remarks>
public abstract class ExecutionStrategy : IExecutionStrategy
{
    /// <summary>
    ///     The default number of retry attempts.
    ///     Ĭ�����Դ�����
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-connection-resiliency">Connection resiliency and database retries</see>
    ///     for more information and examples.
    /// </remarks>
    protected static readonly int DefaultMaxRetryCount = 6;

    /// <summary>
    ///     The default maximum time delay between retries, must be nonnegative.
    ///     ����֮���Ĭ�����ʱ���ӳٱ���Ϊ�Ǹ���
    /// </summary>
    protected static readonly TimeSpan DefaultMaxDelay = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     The default maximum random factor, must not be lesser than 1.
    ///     Ĭ�ϵ����������Ӳ���С��1��
    /// </summary>
    private const double DefaultRandomFactor = 1.1;

    /// <summary>
    /// ���ڼ�������֮���ӳٵ�ָ��������Ĭ�ϻ�������Ϊ����
    /// The default base for the exponential function used to compute the delay between retries, must be positive.
    /// </summary>
    private const double DefaultExponentialBase = 2;

    /// <summary>
    /// ���ڼ�������֮���ӳٵ�ָ��������Ĭ��ϵ�������ǷǸ��ġ�
    /// The default coefficient for the exponential function used to compute the delay between retries, must be nonnegative.
    /// </summary>
    private static readonly TimeSpan DefaultCoefficient = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     Creates a new instance of <see cref="ExecutionStrategy" />.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-connection-resiliency">Connection resiliency and database retries</see>
    ///     for more information and examples.
    /// </remarks>
    /// <param name="context">The context on which the operations will be invoked.</param>
    /// <param name="maxRetryCount">The maximum number of retry attempts.</param>
    /// <param name="maxRetryDelay">The maximum delay between retries.</param>
    protected ExecutionStrategy(
        DbContext context,
        int maxRetryCount,
        TimeSpan maxRetryDelay)
        : this(
            context.GetService<ExecutionStrategyDependencies>(),
            maxRetryCount,
            maxRetryDelay)
    {
    }

    /// <summary>
    ///     Creates a new instance of <see cref="ExecutionStrategy" />.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-connection-resiliency">Connection resiliency and database retries</see>
    ///     for more information and examples.
    /// </remarks>
    /// <param name="dependencies">Parameter object containing service dependencies.</param>
    /// <param name="maxRetryCount">The maximum number of retry attempts.</param>
    /// <param name="maxRetryDelay">The maximum delay between retries.</param>
    protected ExecutionStrategy(
        ExecutionStrategyDependencies dependencies,
        int maxRetryCount,
        TimeSpan maxRetryDelay)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetryCount);

        if (maxRetryDelay.TotalMilliseconds < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetryDelay));
        }

        Dependencies = dependencies;
        MaxRetryCount = maxRetryCount;
        MaxRetryDelay = maxRetryDelay;
    }

    /// <summary>
    /// ��ĿǰΪֹ�������Բ������쳣�б�
    ///     The list of exceptions that caused the operation to be retried so far.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-connection-resiliency">Connection resiliency and database retries</see>
    ///     for more information and examples.
    /// </remarks>
    protected virtual List<Exception> ExceptionsEncountered { get; } = [];

    /// <summary>
    /// һ��α������������������ڸı�����֮����ӳ١�
    ///     A pseudo-random number generator that can be used to vary the delay between retries.
    /// </summary>
    protected virtual Random Random { get; } = new();

    /// <summary>
    ///     The maximum number of retry attempts.
    ///     ���Գ��Ե���������
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-connection-resiliency">Connection resiliency and database retries</see>
    ///     for more information and examples.
    /// </remarks>
    public virtual int MaxRetryCount { get; }

    /// <summary>
    ///     The maximum delay between retries.
    ///     ����֮�������ӳ١�
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-connection-resiliency">Connection resiliency and database retries</see>
    ///     for more information and examples.
    /// </remarks>
    public virtual TimeSpan MaxRetryDelay { get; }

    /// <summary>
    ///     Dependencies for this service.
    ///     �˷���������
    /// </summary>
    protected virtual ExecutionStrategyDependencies Dependencies { get; }

    private static readonly AsyncLocal<ExecutionStrategy?> CurrentExecutionStrategy = new();

    /// <summary>
    /// ��ȡ�����õ�ǰ����ִ�еĲ��ԡ�����Ƕ�׵��ö����������Ĳ��Դ���
    ///     Gets or sets the currently executing strategy. All nested calls will be handled by the outermost strategy.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-connection-resiliency">Connection resiliency and database retries</see>
    ///     for more information and examples.
    /// </remarks>
    public static ExecutionStrategy? Current
    {
        get => CurrentExecutionStrategy.Value;
        protected set => CurrentExecutionStrategy.Value = value;
    }

    /// <summary>
    /// ָʾ��<see cref="IExecutionStrategy"/>�Ƿ������ʧ�ܺ�����ִ�С�
    ///     Indicates whether this <see cref="IExecutionStrategy" /> might retry the execution after a failure.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-connection-resiliency">Connection resiliency and database retries</see>
    ///     for more information and examples.
    /// </remarks>
    public virtual bool RetriesOnFailure
    {
        get
        {
            var current = Current;
            return (current == null || current == this) && MaxRetryCount > 0;
        }
    }

    /// <summary>
    /// ִ��ָ���Ĳ��������ؽ����
    ///     Executes the specified operation and returns the result.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-connection-resiliency">Connection resiliency and database retries</see>
    ///     for more information and examples.
    /// </remarks>
    /// <param name="state">The state that will be passed to the operation.</param>
    /// <param name="operation">
    ///     A delegate representing an executable operation that returns the result of type <typeparamref name="TResult" />.
    /// </param>
    /// <param name="verifySucceeded">A delegate that tests whether the operation succeeded even though an exception was thrown.</param>
    /// <typeparam name="TState">The type of the state.</typeparam>
    /// <typeparam name="TResult">The return type of <paramref name="operation" />.</typeparam>
    /// <returns>The result from the operation.</returns>
    /// <exception cref="RetryLimitExceededException">
    ///     The operation has not succeeded after the configured number of retries.
    /// </exception>
    public virtual TResult Execute<TState, TResult>(
        TState state,
        Func<DbContext, TState, TResult> operation,
        Func<DbContext, TState, ExecutionResult<TResult>>? verifySucceeded)
    {
        Check.NotNull(operation, nameof(operation));

        if (Current != null)
        {
            return operation(Dependencies.CurrentContext.Context, state);
        }

        OnFirstExecution();

        // In order to avoid infinite recursive generics, wrap operation with ExecutionResult
        //Ϊ�˱������޵ݹ鷺�ͣ���ʹ��ExecutionResult��װ����
        return ExecuteImplementation(
            (context, state) => new ExecutionResult<TResult>(true, operation(context, state)),
            verifySucceeded,
            state).Result;
    }

    private ExecutionResult<TResult> ExecuteImplementation<TState, TResult>(
        Func<DbContext, TState, ExecutionResult<TResult>> operation,
        Func<DbContext, TState, ExecutionResult<TResult>>? verifySucceeded,
        TState state)
    {
        while (true)
        {
            try
            {
                Check.DebugAssert(Current == null, "Current != null");

                Current = this;
                var result = operation(Dependencies.CurrentContext.Context, state);
                Current = null;
                return result;
            }
            catch (Exception ex)
            {
                Current = null;

                EntityFrameworkMetricsData.ReportExecutionStrategyOperationFailure();

                if (verifySucceeded != null
                    && CallOnWrappedException(ex, ShouldVerifySuccessOn))
                {
                    var result = ExecuteImplementation(verifySucceeded, null, state);
                    if (result.IsSuccessful)
                    {
                        return result;
                    }
                }

                if (!CallOnWrappedException(ex, ShouldRetryOn))
                {
                    throw;
                }

                ExceptionsEncountered.Add(ex);

                var delay = GetNextDelay(ex);
                if (delay == null)
                {
                    throw new RetryLimitExceededException(CoreStrings.RetryLimitExceeded(MaxRetryCount, GetType().Name), ex);
                }

                Dependencies.Logger.ExecutionStrategyRetrying(ExceptionsEncountered, delay.Value, async: true);

                OnRetry();

                Thread.Sleep(delay.Value);
            }
        }
    }

    /// <summary>
    /// ִ��ָ�����첽���������ؽ����
    ///     Executes the specified asynchronous operation and returns the result.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-connection-resiliency">Connection resiliency and database retries</see>
    ///     for more information and examples.
    /// </remarks>
    /// <param name="state">The state that will be passed to the operation.�����ݸ�������״̬��</param>
    /// <param name="operation">
    ///     A function that returns a started task of type <typeparamref name="TResult" />.
    ///     һ����������������Ϊ<typeparamref name="TResult"/>������������
    /// </param>
    /// <param name="verifySucceeded">
    /// A delegate that tests whether the operation succeeded even though an exception was thrown.
    ///���Բ����Ƿ�ɹ�����ʹ�����쳣����ί��
    /// </param>
    /// <param name="cancellationToken">
    ///  A cancellation token used to cancel the retry operation, but not operations that are already in flight or that already completed successfully.
    ///   ����ȡ�����Բ�����ȡ�����ƣ����������������л��ѳɹ���ɵĲ�����  
    /// </param>
    /// <typeparam name="TState">״̬������ The type of the state.</typeparam>
    /// <typeparam name="TResult">The result type of the <see cref="Task{T}" /> returned by <paramref name="operation" />.</typeparam>
    /// <returns>
    /// ���ԭʼ����ɹ���ɣ���һ�λ����Զ��ݹ��Ϻ󣩣������е���ɵ�����������������ʱ�Դ����ʧ�ܣ�
    /// ���ߴﵽ�������ƣ��򷵻ص����񽫳��ֹ��ϣ����ұ���۲��쳣��
    ///     A task that will run to completion if the original task completes successfully (either the
    ///     first time or after retrying transient failures). If the task fails with a non-transient error or
    ///     the retry limit is reached, the returned task will become faulted and the exception must be observed.
    /// </returns>
    /// <exception cref="RetryLimitExceededException">
    ///     The operation has not succeeded after the configured number of retries.
    ///     �������õ����Դ����󣬲���δ�ɹ���
    /// </exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public virtual async Task<TResult> ExecuteAsync<TState, TResult>(
        TState state,
        Func<DbContext, TState, CancellationToken, Task<TResult>> operation,
        Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded,
        CancellationToken cancellationToken = default)
    {
        Check.NotNull(operation, nameof(operation));

        if (Current != null)
        {
            return await operation(Dependencies.CurrentContext.Context, state, cancellationToken).ConfigureAwait(false);
        }

        OnFirstExecution();

        // In order to avoid infinite recursive generics, wrap operation with ExecutionResult
        var result = await ExecuteImplementationAsync(
            async (context, state, cancellationToken) => new ExecutionResult<TResult>(
                true, await operation(context, state, cancellationToken).ConfigureAwait(false)),
            verifySucceeded,
            state,
            cancellationToken).ConfigureAwait(false);
        return result.Result;
    }

    private async Task<ExecutionResult<TResult>> ExecuteImplementationAsync<TState, TResult>(
        Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>> operation,
        Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded,
        TState state,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                Check.DebugAssert(Current == null, "Current != null");

                Current = this;
                var result = await operation(Dependencies.CurrentContext.Context, state, cancellationToken)
                    .ConfigureAwait(true);
                Current = null;
                return result;
            }
            catch (Exception ex)
            {
                Current = null;

                EntityFrameworkMetricsData.ReportExecutionStrategyOperationFailure();

                if (verifySucceeded != null
                    && CallOnWrappedException(ex, ShouldVerifySuccessOn))
                {
                    var result = await ExecuteImplementationAsync(verifySucceeded, null, state, cancellationToken)
                        .ConfigureAwait(true);
                    if (result.IsSuccessful)
                    {
                        return result;
                    }
                }

                if (!CallOnWrappedException(ex, ShouldRetryOn))
                {
                    throw;
                }

                ExceptionsEncountered.Add(ex);

                var delay = GetNextDelay(ex);
                if (delay == null)
                {
                    throw new RetryLimitExceededException(CoreStrings.RetryLimitExceeded(MaxRetryCount, GetType().Name), ex);
                }

                Dependencies.Logger.ExecutionStrategyRetrying(ExceptionsEncountered, delay.Value, async: true);

                OnRetry();

                await Task.Delay(delay.Value, cancellationToken).ConfigureAwait(true);
            }
        }
    }

    /// <summary>
    /// ��ִ�е�һ������֮ǰ���õķ���
    ///     Method called before the first operation execution
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-connection-resiliency">Connection resiliency and database retries</see>
    ///     for more information and examples.
    /// </remarks>
    protected virtual void OnFirstExecution()
    {
        if (RetriesOnFailure
            && (Dependencies.CurrentContext.Context.Database.CurrentTransaction is not null
                || Dependencies.CurrentContext.Context.Database.GetEnlistedTransaction() is not null
                || (((IDatabaseFacadeDependenciesAccessor)Dependencies.CurrentContext.Context.Database).Dependencies
                    .TransactionManager as
                    ITransactionEnlistmentManager)?.CurrentAmbientTransaction is not null))
        {
            throw new InvalidOperationException(
                CoreStrings.ExecutionStrategyExistingTransaction(
                    GetType().Name,
                    nameof(DbContext)
                    + "."
                    + nameof(DbContext.Database)
                    + "."
                    + nameof(DatabaseFacade.CreateExecutionStrategy)
                    + "()"));
        }

        ExceptionsEncountered.Clear();
    }

    /// <summary>
    ///     Method called before retrying the operation execution
    ///     �����Բ���ִ��֮ǰ���õķ���
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-connection-resiliency">Connection resiliency and database retries</see>
    ///     for more information and examples.
    /// </remarks>
    protected virtual void OnRetry()
    {
    }

    /// <summary>
    ///     Determines whether the operation should be retried and the delay before the next attempt.
    ///     ȷ���Ƿ�Ӧ���Ըò����Լ�����һ�γ���֮ǰ���ӳ١�
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-connection-resiliency">Connection resiliency and database retries</see>
    ///     for more information and examples.
    /// </remarks>
    /// <param name="lastException">The exception thrown during the last execution attempt.</param>
    /// <returns>
    ///     Returns the delay indicating how long to wait for before the next execution attempt if the operation should be retried;
    ///     <see langword="null" /> otherwise
    /// </returns>
    protected virtual TimeSpan? GetNextDelay(Exception lastException)
    {
        var currentRetryCount = ExceptionsEncountered.Count - 1;
        if (currentRetryCount < MaxRetryCount)
        {
            var delta = (Math.Pow(DefaultExponentialBase, currentRetryCount) - 1.0)
                * (1.0 + Random.NextDouble() * (DefaultRandomFactor - 1.0));

            var delay = Math.Min(
                DefaultCoefficient.TotalMilliseconds * delta,
                MaxRetryDelay.TotalMilliseconds);

            return TimeSpan.FromMilliseconds(delay);
        }

        return null;
    }

    /// <summary>
    /// ȷ���ڳɹ�ִ�к��Ƿ��������ָ�����쳣��
    ///     Determines whether the specified exception could be thrown after a successful execution.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-connection-resiliency">Connection resiliency and database retries</see>
    ///     for more information and examples.
    /// </remarks>
    /// <param name="exception">The exception object to be verified.</param>
    /// <returns>
    ///     <see langword="true" /> if the specified exception could be thrown after a successful execution, otherwise <see langword="false" />.
    /// </returns>
    protected internal virtual bool ShouldVerifySuccessOn(Exception exception)
        => ShouldRetryOn(exception);

    /// <summary>
    /// ȷ��ָ�����쳣�Ƿ��ʾ����ͨ�����Խ��в�����˲̬���ϡ�
    ///     Determines whether the specified exception represents a transient failure that can be compensated by a retry.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-connection-resiliency">Connection resiliency and database retries</see>
    ///     for more information and examples.
    /// </remarks>
    /// <param name="exception">The exception object to be verified.</param>
    /// <returns>
    ///     <see langword="true" /> if the specified exception is considered as transient, otherwise <see langword="false" />.
    /// </returns>
    protected internal abstract bool ShouldRetryOn(Exception exception);

    /// <summary>
    ///     Recursively gets InnerException from <paramref name="exception" /> as long as it is an
    ///     exception created by Entity Framework and calls <paramref name="exceptionHandler" /> on the innermost one.
    ///     �ݹ�شӣ�paramref name=��exception��/���л�ȡInnerException��ֻҪ����ʵ���ܴ������쳣��
    ///     �����������һ�����ã�paramref name����exceptionHandler��/����
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-connection-resiliency">Connection resiliency and database retries</see>
    ///     for more information and examples.
    /// </remarks>
    /// <param name="exception">The exception to be unwrapped.</param>
    /// <param name="exceptionHandler">A delegate that will be called with the unwrapped exception.</param>
    /// <typeparam name="TResult">The return type of <paramref name="exceptionHandler" />.</typeparam>
    /// <returns>
    ///     The result from <paramref name="exceptionHandler" />.
    /// </returns>
    public static TResult CallOnWrappedException<TResult>(
        Exception exception,
        Func<Exception, TResult> exceptionHandler)
    {
        while (true)
        {
            if (exception is DbUpdateException { InnerException: Exception innerException })
            {
                exception = innerException;
                continue;
            }

            return exceptionHandler(exception);
        }
    }
}
