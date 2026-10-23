using System.Runtime.CompilerServices;

namespace Lyo.Exceptions;

public static partial class ValueWhenExtensions
{
    extension<T>(T value)
        where T : IComparable<T>
    {
        /// <summary>
        /// Chooses <paramref name="result" /> when the subject is inside the inclusive range [<paramref name="min" />, <paramref name="max" />]. <paramref name="min" /> greater than
        /// <paramref name="max" /> throws <see cref="ArgumentException" />.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfInRange<TResult>(T min, T max, TResult result) => ValueOrChain<T, TResult>.From(value, ValueWhenChecks.InRange(value, min, max), result);

        /// <summary>Factory form of IfInRange. The factory runs only when the check passes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfInRange<TResult>(T min, T max, Func<TResult> result)
            => ValueOrChain<T, TResult>.From(value, ValueWhenChecks.InRange(value, min, max), result);

        /// <summary>
        /// Chooses <paramref name="result" /> when the subject is outside the inclusive range [<paramref name="min" />, <paramref name="max" />]. <paramref name="min" /> greater
        /// than <paramref name="max" /> throws <see cref="ArgumentException" />.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfNotInRange<TResult>(T min, T max, TResult result)
            => ValueOrChain<T, TResult>.From(value, !ValueWhenChecks.InRange(value, min, max), result);

        /// <summary>Factory form of IfNotInRange. The factory runs only when the check passes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfNotInRange<TResult>(T min, T max, Func<TResult> result)
            => ValueOrChain<T, TResult>.From(value, !ValueWhenChecks.InRange(value, min, max), result);

        /// <summary>Chooses <paramref name="result" /> when the subject is greater than <paramref name="threshold" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfGreaterThan<TResult>(T threshold, TResult result)
            => ValueOrChain<T, TResult>.From(value, ValueWhenChecks.GreaterThan(value, threshold), result);

        /// <summary>Factory form of IfGreaterThan. The factory runs only when the check passes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfGreaterThan<TResult>(T threshold, Func<TResult> result)
            => ValueOrChain<T, TResult>.From(value, ValueWhenChecks.GreaterThan(value, threshold), result);

        /// <summary>Chooses <paramref name="result" /> when the subject is greater than or equal to <paramref name="threshold" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfGreaterThanOrEqual<TResult>(T threshold, TResult result)
            => ValueOrChain<T, TResult>.From(value, ValueWhenChecks.GreaterThanOrEqual(value, threshold), result);

        /// <summary>Factory form of IfGreaterThanOrEqual. The factory runs only when the check passes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfGreaterThanOrEqual<TResult>(T threshold, Func<TResult> result)
            => ValueOrChain<T, TResult>.From(value, ValueWhenChecks.GreaterThanOrEqual(value, threshold), result);

        /// <summary>Chooses <paramref name="result" /> when the subject is less than <paramref name="threshold" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfLessThan<TResult>(T threshold, TResult result)
            => ValueOrChain<T, TResult>.From(value, ValueWhenChecks.LessThan(value, threshold), result);

        /// <summary>Factory form of IfLessThan. The factory runs only when the check passes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfLessThan<TResult>(T threshold, Func<TResult> result)
            => ValueOrChain<T, TResult>.From(value, ValueWhenChecks.LessThan(value, threshold), result);

        /// <summary>Chooses <paramref name="result" /> when the subject is less than or equal to <paramref name="threshold" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfLessThanOrEqual<TResult>(T threshold, TResult result)
            => ValueOrChain<T, TResult>.From(value, ValueWhenChecks.LessThanOrEqual(value, threshold), result);

        /// <summary>Factory form of IfLessThanOrEqual. The factory runs only when the check passes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfLessThanOrEqual<TResult>(T threshold, Func<TResult> result)
            => ValueOrChain<T, TResult>.From(value, ValueWhenChecks.LessThanOrEqual(value, threshold), result);
    }

    extension<T>(T value)
        where T : struct, IComparable<T>
    {
        /// <summary>Chooses <paramref name="result" /> when the subject compares equal to <c>default</c>. For <see cref="DateTime" /> that is year 1.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfZero<TResult>(TResult result) => ValueOrChain<T, TResult>.From(value, ValueWhenChecks.IsZero(value), result);

        /// <summary>Factory form of IfZero. The factory runs only when the check passes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfZero<TResult>(Func<TResult> result) => ValueOrChain<T, TResult>.From(value, ValueWhenChecks.IsZero(value), result);

        /// <summary>
        /// Chooses <paramref name="result" /> when the subject is less than <c>default</c>. <see cref="DateTime" /> has no earlier value; <see cref="TimeSpan" /> compares with
        /// <see cref="TimeSpan.Zero" />.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfNegative<TResult>(TResult result) => ValueOrChain<T, TResult>.From(value, ValueWhenChecks.IsNegative(value), result);

        /// <summary>Factory form of IfNegative. The factory runs only when the check passes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfNegative<TResult>(Func<TResult> result) => ValueOrChain<T, TResult>.From(value, ValueWhenChecks.IsNegative(value), result);

        /// <summary>Chooses <paramref name="result" /> when the subject is less than or equal to <c>default</c>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfNegativeOrZero<TResult>(TResult result) => ValueOrChain<T, TResult>.From(value, ValueWhenChecks.IsNegativeOrZero(value), result);

        /// <summary>Factory form of IfNegativeOrZero. The factory runs only when the check passes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfNegativeOrZero<TResult>(Func<TResult> result) => ValueOrChain<T, TResult>.From(value, ValueWhenChecks.IsNegativeOrZero(value), result);

        /// <summary>Chooses <paramref name="result" /> when the subject is greater than <c>default</c>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfPositive<TResult>(TResult result) => ValueOrChain<T, TResult>.From(value, ValueWhenChecks.IsPositive(value), result);

        /// <summary>Factory form of IfPositive. The factory runs only when the check passes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfPositive<TResult>(Func<TResult> result) => ValueOrChain<T, TResult>.From(value, ValueWhenChecks.IsPositive(value), result);

        /// <summary>Chooses <paramref name="result" /> when the subject is greater than or equal to <c>default</c>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfPositiveOrZero<TResult>(TResult result) => ValueOrChain<T, TResult>.From(value, ValueWhenChecks.IsPositiveOrZero(value), result);

        /// <summary>Factory form of IfPositiveOrZero. The factory runs only when the check passes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfPositiveOrZero<TResult>(Func<TResult> result) => ValueOrChain<T, TResult>.From(value, ValueWhenChecks.IsPositiveOrZero(value), result);
    }

    extension<T, TResult>(ValueOrChain<T, TResult> chain)
        where T : IComparable<T>
    {
        /// <summary>
        /// When the chain is still open, chooses <paramref name="result" /> when the subject is inside the inclusive range [<paramref name="min" />, <paramref name="max" />].
        /// <paramref name="min" /> greater than <paramref name="max" /> throws <see cref="ArgumentException" />. A chosen chain is left unchanged.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfInRange(T min, T max, TResult result)
            => chain.IsChosen ? chain : chain.Choose(ValueWhenChecks.InRange(chain.Subject, min, max), result);

        /// <summary>
        /// When the chain is still open, invokes <paramref name="result" /> when the subject is inside the inclusive range [<paramref name="min" />, <paramref name="max" />].
        /// <paramref name="min" /> greater than <paramref name="max" /> throws <see cref="ArgumentException" />. A chosen chain does not invoke the factory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfInRange(T min, T max, Func<TResult> result)
            => chain.IsChosen ? chain : chain.Choose(ValueWhenChecks.InRange(chain.Subject, min, max), result);

        /// <summary>
        /// When the chain is still open, chooses <paramref name="result" /> when the subject is outside the inclusive range [<paramref name="min" />, <paramref name="max" />].
        /// <paramref name="min" /> greater than <paramref name="max" /> throws <see cref="ArgumentException" />. A chosen chain is left unchanged.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfNotInRange(T min, T max, TResult result)
            => chain.IsChosen ? chain : chain.Choose(!ValueWhenChecks.InRange(chain.Subject, min, max), result);

        /// <summary>
        /// When the chain is still open, invokes <paramref name="result" /> when the subject is outside the inclusive range [<paramref name="min" />, <paramref name="max" />].
        /// <paramref name="min" /> greater than <paramref name="max" /> throws <see cref="ArgumentException" />. A chosen chain does not invoke the factory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfNotInRange(T min, T max, Func<TResult> result)
            => chain.IsChosen ? chain : chain.Choose(!ValueWhenChecks.InRange(chain.Subject, min, max), result);

        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject is greater than <paramref name="threshold" />. A chosen chain is left unchanged.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfGreaterThan(T threshold, TResult result)
            => chain.IsChosen ? chain : chain.Choose(ValueWhenChecks.GreaterThan(chain.Subject, threshold), result);

        /// <summary>
        /// When the chain is still open, invokes <paramref name="result" /> when the subject is greater than <paramref name="threshold" />. A chosen chain does not invoke the
        /// factory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfGreaterThan(T threshold, Func<TResult> result)
            => chain.IsChosen ? chain : chain.Choose(ValueWhenChecks.GreaterThan(chain.Subject, threshold), result);

        /// <summary>
        /// When the chain is still open, chooses <paramref name="result" /> when the subject is greater than or equal to <paramref name="threshold" />. A chosen chain is left
        /// unchanged.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfGreaterThanOrEqual(T threshold, TResult result)
            => chain.IsChosen ? chain : chain.Choose(ValueWhenChecks.GreaterThanOrEqual(chain.Subject, threshold), result);

        /// <summary>
        /// When the chain is still open, invokes <paramref name="result" /> when the subject is greater than or equal to <paramref name="threshold" />. A chosen chain does not
        /// invoke the factory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfGreaterThanOrEqual(T threshold, Func<TResult> result)
            => chain.IsChosen ? chain : chain.Choose(ValueWhenChecks.GreaterThanOrEqual(chain.Subject, threshold), result);

        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject is less than <paramref name="threshold" />. A chosen chain is left unchanged.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfLessThan(T threshold, TResult result)
            => chain.IsChosen ? chain : chain.Choose(ValueWhenChecks.LessThan(chain.Subject, threshold), result);

        /// <summary>When the chain is still open, invokes <paramref name="result" /> when the subject is less than <paramref name="threshold" />. A chosen chain does not invoke the factory.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfLessThan(T threshold, Func<TResult> result)
            => chain.IsChosen ? chain : chain.Choose(ValueWhenChecks.LessThan(chain.Subject, threshold), result);

        /// <summary>
        /// When the chain is still open, chooses <paramref name="result" /> when the subject is less than or equal to <paramref name="threshold" />. A chosen chain is left
        /// unchanged.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfLessThanOrEqual(T threshold, TResult result)
            => chain.IsChosen ? chain : chain.Choose(ValueWhenChecks.LessThanOrEqual(chain.Subject, threshold), result);

        /// <summary>
        /// When the chain is still open, invokes <paramref name="result" /> when the subject is less than or equal to <paramref name="threshold" />. A chosen chain does not invoke
        /// the factory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfLessThanOrEqual(T threshold, Func<TResult> result)
            => chain.IsChosen ? chain : chain.Choose(ValueWhenChecks.LessThanOrEqual(chain.Subject, threshold), result);
    }

    extension<T, TResult>(ValueOrChain<T, TResult> chain)
        where T : struct, IComparable<T>
    {
        /// <summary>
        /// When the chain is still open, chooses <paramref name="result" /> when the subject compares equal to <c>default</c>. For <see cref="DateTime" /> that is year 1. A chosen
        /// chain is left unchanged.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfZero(TResult result) => chain.IsChosen ? chain : chain.Choose(ValueWhenChecks.IsZero(chain.Subject), result);

        /// <summary>
        /// When the chain is still open, invokes <paramref name="result" /> when the subject compares equal to <c>default</c>. For <see cref="DateTime" /> that is year 1. A chosen
        /// chain does not invoke the factory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfZero(Func<TResult> result) => chain.IsChosen ? chain : chain.Choose(ValueWhenChecks.IsZero(chain.Subject), result);

        /// <summary>
        /// When the chain is still open, chooses <paramref name="result" /> when the subject is less than <c>default</c>. <see cref="DateTime" /> has no earlier value;
        /// <see cref="TimeSpan" /> compares with <see cref="TimeSpan.Zero" />. A chosen chain is left unchanged.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfNegative(TResult result) => chain.IsChosen ? chain : chain.Choose(ValueWhenChecks.IsNegative(chain.Subject), result);

        /// <summary>
        /// When the chain is still open, invokes <paramref name="result" /> when the subject is less than <c>default</c>. <see cref="DateTime" /> has no earlier value;
        /// <see cref="TimeSpan" /> compares with <see cref="TimeSpan.Zero" />. A chosen chain does not invoke the factory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfNegative(Func<TResult> result) => chain.IsChosen ? chain : chain.Choose(ValueWhenChecks.IsNegative(chain.Subject), result);

        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject is less than or equal to <c>default</c>. A chosen chain is left unchanged.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfNegativeOrZero(TResult result) => chain.IsChosen ? chain : chain.Choose(ValueWhenChecks.IsNegativeOrZero(chain.Subject), result);

        /// <summary>When the chain is still open, invokes <paramref name="result" /> when the subject is less than or equal to <c>default</c>. A chosen chain does not invoke the factory.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfNegativeOrZero(Func<TResult> result) => chain.IsChosen ? chain : chain.Choose(ValueWhenChecks.IsNegativeOrZero(chain.Subject), result);

        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject is greater than <c>default</c>. A chosen chain is left unchanged.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfPositive(TResult result) => chain.IsChosen ? chain : chain.Choose(ValueWhenChecks.IsPositive(chain.Subject), result);

        /// <summary>When the chain is still open, invokes <paramref name="result" /> when the subject is greater than <c>default</c>. A chosen chain does not invoke the factory.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfPositive(Func<TResult> result) => chain.IsChosen ? chain : chain.Choose(ValueWhenChecks.IsPositive(chain.Subject), result);

        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject is greater than or equal to <c>default</c>. A chosen chain is left unchanged.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfPositiveOrZero(TResult result) => chain.IsChosen ? chain : chain.Choose(ValueWhenChecks.IsPositiveOrZero(chain.Subject), result);

        /// <summary>When the chain is still open, invokes <paramref name="result" /> when the subject is greater than or equal to <c>default</c>. A chosen chain does not invoke the factory.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfPositiveOrZero(Func<TResult> result) => chain.IsChosen ? chain : chain.Choose(ValueWhenChecks.IsPositiveOrZero(chain.Subject), result);
    }

    extension<T>(T? value)
        where T : struct, IComparable<T>
    {
        /// <summary>
        /// Chooses <paramref name="result" /> when the subject is inside the inclusive range. A null subject leaves the chain open. <paramref name="min" /> greater than
        /// <paramref name="max" /> throws <see cref="ArgumentException" />.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfInRange<TResult>(T min, T max, TResult result)
        {
            ValueWhenChecks.ThrowIfInverted(min, max);
            return value is T subject
                ? ValueOrChain<T?, TResult>.From(value, subject.CompareTo(min) >= 0 && subject.CompareTo(max) <= 0, result)
                : ValueOrChain<T?, TResult>.Miss(value);
        }

        /// <summary>Factory form of IfInRange. A null subject leaves the chain open and does not invoke the factory.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfInRange<TResult>(T min, T max, Func<TResult> result)
        {
            ValueWhenChecks.ThrowIfInverted(min, max);
            return value is T subject
                ? ValueOrChain<T?, TResult>.From(value, subject.CompareTo(min) >= 0 && subject.CompareTo(max) <= 0, result)
                : ValueOrChain<T?, TResult>.Miss(value);
        }

        /// <summary>
        /// Chooses <paramref name="result" /> when the subject is outside the inclusive range. A null subject leaves the chain open. <paramref name="min" /> greater than
        /// <paramref name="max" /> throws <see cref="ArgumentException" />.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfNotInRange<TResult>(T min, T max, TResult result)
        {
            ValueWhenChecks.ThrowIfInverted(min, max);
            return value is T subject
                ? ValueOrChain<T?, TResult>.From(value, subject.CompareTo(min) < 0 || subject.CompareTo(max) > 0, result)
                : ValueOrChain<T?, TResult>.Miss(value);
        }

        /// <summary>Factory form of IfNotInRange. A null subject leaves the chain open and does not invoke the factory.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfNotInRange<TResult>(T min, T max, Func<TResult> result)
        {
            ValueWhenChecks.ThrowIfInverted(min, max);
            return value is T subject
                ? ValueOrChain<T?, TResult>.From(value, subject.CompareTo(min) < 0 || subject.CompareTo(max) > 0, result)
                : ValueOrChain<T?, TResult>.Miss(value);
        }

        /// <summary>Chooses <paramref name="result" /> when the subject is greater than <paramref name="threshold" />. A null subject leaves the chain open.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfGreaterThan<TResult>(T threshold, TResult result)
            => value is T subject ? ValueOrChain<T?, TResult>.From(value, ValueWhenChecks.GreaterThan(subject, threshold), result) : ValueOrChain<T?, TResult>.Miss(value);

        /// <summary>Factory form of IfGreaterThan. A null subject leaves the chain open and does not invoke the factory.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfGreaterThan<TResult>(T threshold, Func<TResult> result)
            => value is T subject ? ValueOrChain<T?, TResult>.From(value, ValueWhenChecks.GreaterThan(subject, threshold), result) : ValueOrChain<T?, TResult>.Miss(value);

        /// <summary>Chooses <paramref name="result" /> when the subject is greater than or equal to <paramref name="threshold" />. A null subject leaves the chain open.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfGreaterThanOrEqual<TResult>(T threshold, TResult result)
            => value is T subject ? ValueOrChain<T?, TResult>.From(value, ValueWhenChecks.GreaterThanOrEqual(subject, threshold), result) : ValueOrChain<T?, TResult>.Miss(value);

        /// <summary>Factory form of IfGreaterThanOrEqual. A null subject leaves the chain open and does not invoke the factory.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfGreaterThanOrEqual<TResult>(T threshold, Func<TResult> result)
            => value is T subject ? ValueOrChain<T?, TResult>.From(value, ValueWhenChecks.GreaterThanOrEqual(subject, threshold), result) : ValueOrChain<T?, TResult>.Miss(value);

        /// <summary>Chooses <paramref name="result" /> when the subject is less than <paramref name="threshold" />. A null subject leaves the chain open.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfLessThan<TResult>(T threshold, TResult result)
            => value is T subject ? ValueOrChain<T?, TResult>.From(value, ValueWhenChecks.LessThan(subject, threshold), result) : ValueOrChain<T?, TResult>.Miss(value);

        /// <summary>Factory form of IfLessThan. A null subject leaves the chain open and does not invoke the factory.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfLessThan<TResult>(T threshold, Func<TResult> result)
            => value is T subject ? ValueOrChain<T?, TResult>.From(value, ValueWhenChecks.LessThan(subject, threshold), result) : ValueOrChain<T?, TResult>.Miss(value);

        /// <summary>Chooses <paramref name="result" /> when the subject is less than or equal to <paramref name="threshold" />. A null subject leaves the chain open.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfLessThanOrEqual<TResult>(T threshold, TResult result)
            => value is T subject ? ValueOrChain<T?, TResult>.From(value, ValueWhenChecks.LessThanOrEqual(subject, threshold), result) : ValueOrChain<T?, TResult>.Miss(value);

        /// <summary>Factory form of IfLessThanOrEqual. A null subject leaves the chain open and does not invoke the factory.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfLessThanOrEqual<TResult>(T threshold, Func<TResult> result)
            => value is T subject ? ValueOrChain<T?, TResult>.From(value, ValueWhenChecks.LessThanOrEqual(subject, threshold), result) : ValueOrChain<T?, TResult>.Miss(value);

        /// <summary>Chooses <paramref name="result" /> when the subject compares equal to default. A null subject leaves the chain open.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfZero<TResult>(TResult result)
            => value is T subject ? ValueOrChain<T?, TResult>.From(value, ValueWhenChecks.IsZero(subject), result) : ValueOrChain<T?, TResult>.Miss(value);

        /// <summary>Factory form of IfZero. A null subject leaves the chain open and does not invoke the factory.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfZero<TResult>(Func<TResult> result)
            => value is T subject ? ValueOrChain<T?, TResult>.From(value, ValueWhenChecks.IsZero(subject), result) : ValueOrChain<T?, TResult>.Miss(value);

        /// <summary>Chooses <paramref name="result" /> when the subject is less than default. A null subject leaves the chain open.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfNegative<TResult>(TResult result)
            => value is T subject ? ValueOrChain<T?, TResult>.From(value, ValueWhenChecks.IsNegative(subject), result) : ValueOrChain<T?, TResult>.Miss(value);

        /// <summary>Factory form of IfNegative. A null subject leaves the chain open and does not invoke the factory.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfNegative<TResult>(Func<TResult> result)
            => value is T subject ? ValueOrChain<T?, TResult>.From(value, ValueWhenChecks.IsNegative(subject), result) : ValueOrChain<T?, TResult>.Miss(value);

        /// <summary>Chooses <paramref name="result" /> when the subject is less than or equal to default. A null subject leaves the chain open.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfNegativeOrZero<TResult>(TResult result)
            => value is T subject ? ValueOrChain<T?, TResult>.From(value, ValueWhenChecks.IsNegativeOrZero(subject), result) : ValueOrChain<T?, TResult>.Miss(value);

        /// <summary>Factory form of IfNegativeOrZero. A null subject leaves the chain open and does not invoke the factory.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfNegativeOrZero<TResult>(Func<TResult> result)
            => value is T subject ? ValueOrChain<T?, TResult>.From(value, ValueWhenChecks.IsNegativeOrZero(subject), result) : ValueOrChain<T?, TResult>.Miss(value);

        /// <summary>Chooses <paramref name="result" /> when the subject is greater than default. A null subject leaves the chain open.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfPositive<TResult>(TResult result)
            => value is T subject ? ValueOrChain<T?, TResult>.From(value, ValueWhenChecks.IsPositive(subject), result) : ValueOrChain<T?, TResult>.Miss(value);

        /// <summary>Factory form of IfPositive. A null subject leaves the chain open and does not invoke the factory.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfPositive<TResult>(Func<TResult> result)
            => value is T subject ? ValueOrChain<T?, TResult>.From(value, ValueWhenChecks.IsPositive(subject), result) : ValueOrChain<T?, TResult>.Miss(value);

        /// <summary>Chooses <paramref name="result" /> when the subject is greater than or equal to default. A null subject leaves the chain open.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfPositiveOrZero<TResult>(TResult result)
            => value is T subject ? ValueOrChain<T?, TResult>.From(value, ValueWhenChecks.IsPositiveOrZero(subject), result) : ValueOrChain<T?, TResult>.Miss(value);

        /// <summary>Factory form of IfPositiveOrZero. A null subject leaves the chain open and does not invoke the factory.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfPositiveOrZero<TResult>(Func<TResult> result)
            => value is T subject ? ValueOrChain<T?, TResult>.From(value, ValueWhenChecks.IsPositiveOrZero(subject), result) : ValueOrChain<T?, TResult>.Miss(value);
    }

    extension<T, TResult>(ValueOrChain<T?, TResult> chain)
        where T : struct, IComparable<T>
    {
        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject is inside the inclusive range. A null subject leaves the chain open.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfInRange(T min, T max, TResult result)
        {
            if (chain.IsChosen)
                return chain;

            ValueWhenChecks.ThrowIfInverted(min, max);
            return chain.Subject is T subject ? chain.Choose(subject.CompareTo(min) >= 0 && subject.CompareTo(max) <= 0, result) : chain;
        }

        /// <summary>
        /// When the chain is still open, invokes <paramref name="result" /> when the subject is inside the inclusive range. A null or already chosen chain does not invoke the
        /// factory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfInRange(T min, T max, Func<TResult> result)
        {
            if (chain.IsChosen)
                return chain;

            ValueWhenChecks.ThrowIfInverted(min, max);
            return chain.Subject is T subject ? chain.Choose(subject.CompareTo(min) >= 0 && subject.CompareTo(max) <= 0, result) : chain;
        }

        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject is outside the inclusive range. A null subject leaves the chain open.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfNotInRange(T min, T max, TResult result)
        {
            if (chain.IsChosen)
                return chain;

            ValueWhenChecks.ThrowIfInverted(min, max);
            return chain.Subject is T subject ? chain.Choose(subject.CompareTo(min) < 0 || subject.CompareTo(max) > 0, result) : chain;
        }

        /// <summary>
        /// When the chain is still open, invokes <paramref name="result" /> when the subject is outside the inclusive range. A null or already chosen chain does not invoke the
        /// factory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfNotInRange(T min, T max, Func<TResult> result)
        {
            if (chain.IsChosen)
                return chain;

            ValueWhenChecks.ThrowIfInverted(min, max);
            return chain.Subject is T subject ? chain.Choose(subject.CompareTo(min) < 0 || subject.CompareTo(max) > 0, result) : chain;
        }

        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject is greater than <paramref name="threshold" />. A null subject leaves the chain open.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfGreaterThan(T threshold, TResult result)
            => chain.IsChosen || chain.Subject is not T subject ? chain : chain.Choose(ValueWhenChecks.GreaterThan(subject, threshold), result);

        /// <summary>
        /// When the chain is still open, invokes <paramref name="result" /> when the subject is greater than <paramref name="threshold" />. A null or already chosen chain does not
        /// invoke the factory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfGreaterThan(T threshold, Func<TResult> result)
            => chain.IsChosen || chain.Subject is not T subject ? chain : chain.Choose(ValueWhenChecks.GreaterThan(subject, threshold), result);

        /// <summary>
        /// When the chain is still open, chooses <paramref name="result" /> when the subject is greater than or equal to <paramref name="threshold" />. A null subject leaves the
        /// chain open.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfGreaterThanOrEqual(T threshold, TResult result)
            => chain.IsChosen || chain.Subject is not T subject ? chain : chain.Choose(ValueWhenChecks.GreaterThanOrEqual(subject, threshold), result);

        /// <summary>
        /// When the chain is still open, invokes <paramref name="result" /> when the subject is greater than or equal to <paramref name="threshold" />. A null or already chosen
        /// chain does not invoke the factory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfGreaterThanOrEqual(T threshold, Func<TResult> result)
            => chain.IsChosen || chain.Subject is not T subject ? chain : chain.Choose(ValueWhenChecks.GreaterThanOrEqual(subject, threshold), result);

        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject is less than <paramref name="threshold" />. A null subject leaves the chain open.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfLessThan(T threshold, TResult result)
            => chain.IsChosen || chain.Subject is not T subject ? chain : chain.Choose(ValueWhenChecks.LessThan(subject, threshold), result);

        /// <summary>
        /// When the chain is still open, invokes <paramref name="result" /> when the subject is less than <paramref name="threshold" />. A null or already chosen chain does not
        /// invoke the factory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfLessThan(T threshold, Func<TResult> result)
            => chain.IsChosen || chain.Subject is not T subject ? chain : chain.Choose(ValueWhenChecks.LessThan(subject, threshold), result);

        /// <summary>
        /// When the chain is still open, chooses <paramref name="result" /> when the subject is less than or equal to <paramref name="threshold" />. A null subject leaves the chain
        /// open.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfLessThanOrEqual(T threshold, TResult result)
            => chain.IsChosen || chain.Subject is not T subject ? chain : chain.Choose(ValueWhenChecks.LessThanOrEqual(subject, threshold), result);

        /// <summary>
        /// When the chain is still open, invokes <paramref name="result" /> when the subject is less than or equal to <paramref name="threshold" />. A null or already chosen chain
        /// does not invoke the factory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfLessThanOrEqual(T threshold, Func<TResult> result)
            => chain.IsChosen || chain.Subject is not T subject ? chain : chain.Choose(ValueWhenChecks.LessThanOrEqual(subject, threshold), result);

        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject compares equal to default. A null subject leaves the chain open.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfZero(TResult result)
            => chain.IsChosen || chain.Subject is not T subject ? chain : chain.Choose(ValueWhenChecks.IsZero(subject), result);

        /// <summary>When the chain is still open, invokes <paramref name="result" /> when the subject compares equal to default. A null or already chosen chain does not invoke the factory.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfZero(Func<TResult> result)
            => chain.IsChosen || chain.Subject is not T subject ? chain : chain.Choose(ValueWhenChecks.IsZero(subject), result);

        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject is less than default. A null subject leaves the chain open.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfNegative(TResult result)
            => chain.IsChosen || chain.Subject is not T subject ? chain : chain.Choose(ValueWhenChecks.IsNegative(subject), result);

        /// <summary>When the chain is still open, invokes <paramref name="result" /> when the subject is less than default. A null or already chosen chain does not invoke the factory.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfNegative(Func<TResult> result)
            => chain.IsChosen || chain.Subject is not T subject ? chain : chain.Choose(ValueWhenChecks.IsNegative(subject), result);

        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject is less than or equal to default. A null subject leaves the chain open.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfNegativeOrZero(TResult result)
            => chain.IsChosen || chain.Subject is not T subject ? chain : chain.Choose(ValueWhenChecks.IsNegativeOrZero(subject), result);

        /// <summary>
        /// When the chain is still open, invokes <paramref name="result" /> when the subject is less than or equal to default. A null or already chosen chain does not invoke the
        /// factory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfNegativeOrZero(Func<TResult> result)
            => chain.IsChosen || chain.Subject is not T subject ? chain : chain.Choose(ValueWhenChecks.IsNegativeOrZero(subject), result);

        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject is greater than default. A null subject leaves the chain open.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfPositive(TResult result)
            => chain.IsChosen || chain.Subject is not T subject ? chain : chain.Choose(ValueWhenChecks.IsPositive(subject), result);

        /// <summary>When the chain is still open, invokes <paramref name="result" /> when the subject is greater than default. A null or already chosen chain does not invoke the factory.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfPositive(Func<TResult> result)
            => chain.IsChosen || chain.Subject is not T subject ? chain : chain.Choose(ValueWhenChecks.IsPositive(subject), result);

        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject is greater than or equal to default. A null subject leaves the chain open.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfPositiveOrZero(TResult result)
            => chain.IsChosen || chain.Subject is not T subject ? chain : chain.Choose(ValueWhenChecks.IsPositiveOrZero(subject), result);

        /// <summary>
        /// When the chain is still open, invokes <paramref name="result" /> when the subject is greater than or equal to default. A null or already chosen chain does not invoke the
        /// factory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfPositiveOrZero(Func<TResult> result)
            => chain.IsChosen || chain.Subject is not T subject ? chain : chain.Choose(ValueWhenChecks.IsPositiveOrZero(subject), result);
    }

    extension<T>(T value)
        where T : IEquatable<T>
    {
        /// <summary>Chooses <paramref name="result" /> when <paramref name="value" /> equals <paramref name="other" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfEqual<TResult>(T other, TResult result) => ValueOrChain<T, TResult>.From(value, value.Equals(other), result);

        /// <summary>Factory form of IfEqual. The factory runs only when the values are equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfEqual<TResult>(T other, Func<TResult> result) => ValueOrChain<T, TResult>.From(value, value.Equals(other), result);

        /// <summary>Chooses <paramref name="result" /> when <paramref name="value" /> does not equal <paramref name="other" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfNotEqual<TResult>(T other, TResult result) => ValueOrChain<T, TResult>.From(value, !value.Equals(other), result);

        /// <summary>Factory form of IfNotEqual. The factory runs only when the values differ.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfNotEqual<TResult>(T other, Func<TResult> result) => ValueOrChain<T, TResult>.From(value, !value.Equals(other), result);
    }

    extension<T, TResult>(ValueOrChain<T, TResult> chain)
        where T : IEquatable<T>
    {
        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject equals <paramref name="other" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfEqual(T other, TResult result) => chain.IsChosen ? chain : chain.Choose(chain.Subject.Equals(other), result);

        /// <summary>When the chain is still open, invokes <paramref name="result" /> when the subject equals <paramref name="other" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfEqual(T other, Func<TResult> result) => chain.IsChosen ? chain : chain.Choose(chain.Subject.Equals(other), result);

        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject does not equal <paramref name="other" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfNotEqual(T other, TResult result) => chain.IsChosen ? chain : chain.Choose(!chain.Subject.Equals(other), result);

        /// <summary>When the chain is still open, invokes <paramref name="result" /> when the subject does not equal <paramref name="other" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfNotEqual(T other, Func<TResult> result) => chain.IsChosen ? chain : chain.Choose(!chain.Subject.Equals(other), result);
    }

    extension<T>(T? value)
        where T : struct, IEquatable<T>
    {
        /// <summary>Chooses <paramref name="result" /> when <paramref name="value" /> equals <paramref name="other" />. A null subject leaves the chain open.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfEqual<TResult>(T other, TResult result)
            => value is T subject ? ValueOrChain<T?, TResult>.From(value, subject.Equals(other), result) : ValueOrChain<T?, TResult>.Miss(value);

        /// <summary>Factory form of IfEqual. A null subject leaves the chain open and does not invoke the factory.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfEqual<TResult>(T other, Func<TResult> result)
            => value is T subject ? ValueOrChain<T?, TResult>.From(value, subject.Equals(other), result) : ValueOrChain<T?, TResult>.Miss(value);

        /// <summary>Chooses <paramref name="result" /> when <paramref name="value" /> does not equal <paramref name="other" />. A null subject leaves the chain open.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfNotEqual<TResult>(T other, TResult result)
            => value is T subject ? ValueOrChain<T?, TResult>.From(value, !subject.Equals(other), result) : ValueOrChain<T?, TResult>.Miss(value);

        /// <summary>Factory form of IfNotEqual. A null subject leaves the chain open and does not invoke the factory.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfNotEqual<TResult>(T other, Func<TResult> result)
            => value is T subject ? ValueOrChain<T?, TResult>.From(value, !subject.Equals(other), result) : ValueOrChain<T?, TResult>.Miss(value);
    }

    extension<T, TResult>(ValueOrChain<T?, TResult> chain)
        where T : struct, IEquatable<T>
    {
        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject equals <paramref name="other" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfEqual(T other, TResult result)
            => chain.IsChosen || chain.Subject is not T subject ? chain : chain.Choose(subject.Equals(other), result);

        /// <summary>When the chain is still open, invokes <paramref name="result" /> when the subject equals <paramref name="other" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfEqual(T other, Func<TResult> result)
            => chain.IsChosen || chain.Subject is not T subject ? chain : chain.Choose(subject.Equals(other), result);

        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject does not equal <paramref name="other" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfNotEqual(T other, TResult result)
            => chain.IsChosen || chain.Subject is not T subject ? chain : chain.Choose(!subject.Equals(other), result);

        /// <summary>When the chain is still open, invokes <paramref name="result" /> when the subject does not equal <paramref name="other" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfNotEqual(T other, Func<TResult> result)
            => chain.IsChosen || chain.Subject is not T subject ? chain : chain.Choose(!subject.Equals(other), result);
    }
}