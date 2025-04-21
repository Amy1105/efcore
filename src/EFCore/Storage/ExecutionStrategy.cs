// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Transactions;

namespace Microsoft.EntityFrameworkCore.Storage;

/// <summary>
///     The base class for <see cref="IExecutionStrategy" /> implementations.
/// </summary>
/// <remarks>
///     <para>
///     使用寿命为<see cref="ServiceLifetime.Scoped"/>。这意味着每个"DbContext"/>实例都将使用自己的该服务实例。
///     该实现可能依赖于以任何生存期注册的其他服务。实现不需要是线程安全的。
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
    ///     默认重试次数。
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-connection-resiliency">Connection resiliency and database retries</see>
    ///     for more information and examples.
    /// </remarks>
    protected static readonly int DefaultMaxRetryCount = 6;

    /// <summary>
    ///     The default maximum time delay between retries, must be nonnegative.
    ///     重试之间的默认最大时间延迟必须为非负。
    /// </summary>
    protected static readonly TimeSpan DefaultMaxDelay = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     The default maximum random factor, must not be lesser than 1.
    ///     默认的最大随机因子不得小于1。
    /// </summary>
    private const double DefaultRandomFactor = 1.1;

    /// <summary>
    /// 用于计算重试之间延迟的指数函数的默认基数必须为正。
    /// The default base for the exponential function used to compute the delay between retries, must be positive.
    /// </summary>
    private const double DefaultExponentialBase = 2;

    /// <summary>
    /// 用于计算重试之间延迟的指数函数的默认系数必须是非负的。
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
    /// 到目前为止导致重试操作的异常列表。
    ///     The list of exceptions that caused the operation to be retried so far.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-connection-resiliency">Connection resiliency and database retries</see>
    ///     for more information and examples.
    /// </remarks>
    protected virtual List<Exception> ExceptionsEncountered { get; } = [];

    /// <summary>
    /// 一种伪随机数生成器，可用于改变重试之间的延迟。
    ///     A pseudo-random number generator that can be used to vary the delay between retries.
    /// </summary>
    protected virtual Random Random { get; } = new();

    /// <summary>
    ///     The maximum number of retry attempts.
    ///     重试尝试的最大次数。
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-connection-resiliency">Connection resiliency and database retries</see>
    ///     for more information and examples.
    /// </remarks>
    public virtual int MaxRetryCount { get; }

    /// <summary>
    ///     The maximum delay between retries.
    ///     重试之间的最大延迟。
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-connection-resiliency">Connection resiliency and database retries</see>
    ///     for more information and examples.
    /// </remarks>
    public virtual TimeSpan MaxRetryDelay { get; }

    /// <summary>
    ///     Dependencies for this service.
    ///     此服务的依赖项。
    /// </summary>
    protected virtual ExecutionStrategyDependencies Dependencies { get; }

    private static readonly AsyncLocal<ExecutionStrategy?> CurrentExecutionStrategy = new();

    /// <summary>
    /// 获取或设置当前正在执行的策略。所有嵌套调用都将由最外层的策略处理。
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
    /// 指示此<see cref="IExecutionStrategy"/>是否可能在失败后重试执行。
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
    /// 执行指定的操作并返回结果。
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
        //为了避免无限递归泛型，请使用ExecutionResult包装操作
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
    /// 执行指定的异步操作并返回结果。
    ///     Executes the specified asynchronous operation and returns the result.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-connection-resiliency">Connection resiliency and database retries</see>
    ///     for more information and examples.
    /// </remarks>
    /// <param name="state">The state that will be passed to the operation.将传递给操作的状态。</param>
    /// <param name="operation">
    ///     A function that returns a started task of type <typeparamref name="TResult" />.
    ///     一个函数，返回类型为<typeparamref name="TResult"/>的已启动任务。
    /// </param>
    /// <param name="verifySucceeded">
    /// A delegate that tests whether the operation succeeded even though an exception was thrown.
    ///测试操作是否成功（即使引发异常）的委托
    /// </param>
    /// <param name="cancellationToken">
    ///  A cancellation token used to cancel the retry operation, but not operations that are already in flight or that already completed successfully.
    ///   用于取消重试操作的取消令牌，但不包括已在运行或已成功完成的操作。  
    /// </param>
    /// <typeparam name="TState">状态的类型 The type of the state.</typeparam>
    /// <typeparam name="TResult">The result type of the <see cref="Task{T}" /> returned by <paramref name="operation" />.</typeparam>
    /// <returns>
    /// 如果原始任务成功完成（第一次或重试短暂故障后），则将运行到完成的任务。如果任务因非暂时性错误而失败，
    /// 或者达到重试限制，则返回的任务将出现故障，并且必须观察异常。
    ///     A task that will run to completion if the original task completes successfully (either the
    ///     first time or after retrying transient failures). If the task fails with a non-transient error or
    ///     the retry limit is reached, the returned task will become faulted and the exception must be observed.
    /// </returns>
    /// <exception cref="RetryLimitExceededException">
    ///     The operation has not succeeded after the configured number of retries.
    ///     超过配置的重试次数后，操作未成功。
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
    /// 在执行第一个操作之前调用的方法
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
    ///     在重试操作执行之前调用的方法
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
    ///     确定是否应重试该操作以及在下一次尝试之前的延迟。
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
    /// 确定在成功执行后是否可以引发指定的异常。
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
    /// 确定指定的异常是否表示可以通过重试进行补偿的瞬态故障。
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
    ///     递归地从＜paramref name=“exception”/＞中获取InnerException，只要它是实体框架创建的异常，
    ///     并在最里面的一个调用＜paramref name＝“exceptionHandler”/＞。
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
