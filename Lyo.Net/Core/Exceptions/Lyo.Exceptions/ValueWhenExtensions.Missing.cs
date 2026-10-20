using System.Runtime.CompilerServices;

namespace Lyo.Exceptions;

public static partial class ValueWhenExtensions
{
    extension<T>(T? value) where T : class
    {
        /// <summary>Chooses <paramref name="result" /> when <paramref name="value" /> is null.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfNull<TResult>(TResult result) => ValueOrChain<T?, TResult>.From(value, value is null, result);

        /// <summary>Factory form of IfNull. The factory runs only when <paramref name="value" /> is null.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfNull<TResult>(Func<TResult> result) => ValueOrChain<T?, TResult>.From(value, value is null, result);

        /// <summary>Chooses <paramref name="result" /> when <paramref name="value" /> is not null.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfNotNull<TResult>(TResult result) => ValueOrChain<T?, TResult>.From(value, value is not null, result);

        /// <summary>Factory form of IfNotNull. The factory runs only when <paramref name="value" /> is not null.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfNotNull<TResult>(Func<TResult> result) => ValueOrChain<T?, TResult>.From(value, value is not null, result);
    }

    extension<TSubject, TResult>(ValueOrChain<TSubject?, TResult> chain) where TSubject : class
    {
        /// <summary>When the chain is still open and the subject is null, chooses <paramref name="result" />. This result type is not the subject type.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<TSubject?, TResult> OrIfNull(TResult result) => chain.IsChosen ? chain : chain.Choose(chain.Subject is null, result);

        /// <summary>When the chain is still open and the subject is null, invokes <paramref name="result" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<TSubject?, TResult> OrIfNull(Func<TResult> result) => chain.IsChosen ? chain : chain.Choose(chain.Subject is null, result);

        /// <summary>When the chain is still open and the subject is not null, chooses <paramref name="result" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<TSubject?, TResult> OrIfNotNull(TResult result) => chain.IsChosen ? chain : chain.Choose(chain.Subject is not null, result);

        /// <summary>When the chain is still open and the subject is not null, invokes <paramref name="result" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<TSubject?, TResult> OrIfNotNull(Func<TResult> result) => chain.IsChosen ? chain : chain.Choose(chain.Subject is not null, result);
    }

    extension<T>(T? value) where T : struct
    {
        /// <summary>Chooses <paramref name="result" /> when <paramref name="value" /> is null.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfNull<TResult>(TResult result) => ValueOrChain<T?, TResult>.From(value, !value.HasValue, result);

        /// <summary>Factory form of IfNull. The factory runs only when <paramref name="value" /> is null.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfNull<TResult>(Func<TResult> result) => ValueOrChain<T?, TResult>.From(value, !value.HasValue, result);

        /// <summary>Chooses <paramref name="result" /> when <paramref name="value" /> has a value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfNotNull<TResult>(TResult result) => ValueOrChain<T?, TResult>.From(value, value.HasValue, result);

        /// <summary>Factory form of IfNotNull. The factory runs only when <paramref name="value" /> has a value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> IfNotNull<TResult>(Func<TResult> result) => ValueOrChain<T?, TResult>.From(value, value.HasValue, result);
    }

    extension<T, TResult>(ValueOrChain<T?, TResult> chain) where T : struct
    {
        /// <summary>When the chain is still open and the subject is null, chooses <paramref name="result" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfNull(TResult result) => chain.IsChosen ? chain : chain.Choose(!chain.Subject.HasValue, result);

        /// <summary>When the chain is still open and the subject is null, invokes <paramref name="result" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfNull(Func<TResult> result) => chain.IsChosen ? chain : chain.Choose(!chain.Subject.HasValue, result);

        /// <summary>When the chain is still open and the subject has a value, chooses <paramref name="result" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfNotNull(TResult result) => chain.IsChosen ? chain : chain.Choose(chain.Subject.HasValue, result);

        /// <summary>When the chain is still open and the subject has a value, invokes <paramref name="result" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T?, TResult> OrIfNotNull(Func<TResult> result) => chain.IsChosen ? chain : chain.Choose(chain.Subject.HasValue, result);
    }

    extension<T>(T value) where T : struct
    {
        /// <summary>Chooses <paramref name="result" /> when <paramref name="value" /> equals <c>default</c>. For <see cref="DateTime" /> that is an unset timestamp.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfDefault<TResult>(TResult result)
            => ValueOrChain<T, TResult>.From(value, EqualityComparer<T>.Default.Equals(value, default), result);

        /// <summary>Factory form of IfDefault. The factory runs only when <paramref name="value" /> equals <c>default</c>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfDefault<TResult>(Func<TResult> result)
            => ValueOrChain<T, TResult>.From(value, EqualityComparer<T>.Default.Equals(value, default), result);

        /// <summary>Chooses <paramref name="result" /> when <paramref name="value" /> is not <c>default</c>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfNotDefault<TResult>(TResult result)
            => ValueOrChain<T, TResult>.From(value, !EqualityComparer<T>.Default.Equals(value, default), result);

        /// <summary>Factory form of IfNotDefault. The factory runs only when <paramref name="value" /> is not <c>default</c>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> IfNotDefault<TResult>(Func<TResult> result)
            => ValueOrChain<T, TResult>.From(value, !EqualityComparer<T>.Default.Equals(value, default), result);
    }

    extension<T, TResult>(ValueOrChain<T, TResult> chain) where T : struct
    {
        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject equals <c>default</c>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfDefault(TResult result)
            => chain.IsChosen ? chain : chain.Choose(EqualityComparer<T>.Default.Equals(chain.Subject, default), result);

        /// <summary>When the chain is still open, invokes <paramref name="result" /> when the subject equals <c>default</c>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfDefault(Func<TResult> result)
            => chain.IsChosen ? chain : chain.Choose(EqualityComparer<T>.Default.Equals(chain.Subject, default), result);

        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject is not <c>default</c>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfNotDefault(TResult result)
            => chain.IsChosen ? chain : chain.Choose(!EqualityComparer<T>.Default.Equals(chain.Subject, default), result);

        /// <summary>When the chain is still open, invokes <paramref name="result" /> when the subject is not <c>default</c>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<T, TResult> OrIfNotDefault(Func<TResult> result)
            => chain.IsChosen ? chain : chain.Choose(!EqualityComparer<T>.Default.Equals(chain.Subject, default), result);
    }

    extension(Guid value)
    {
        /// <summary>Chooses <paramref name="result" /> when <paramref name="value" /> is <see cref="Guid.Empty" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<Guid, TResult> IfEmpty<TResult>(TResult result) => ValueOrChain<Guid, TResult>.From(value, value == Guid.Empty, result);

        /// <summary>Factory form of IfEmpty. The factory runs only when <paramref name="value" /> is <see cref="Guid.Empty" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<Guid, TResult> IfEmpty<TResult>(Func<TResult> result) => ValueOrChain<Guid, TResult>.From(value, value == Guid.Empty, result);

        /// <summary>Chooses <paramref name="result" /> when <paramref name="value" /> is not <see cref="Guid.Empty" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<Guid, TResult> IfNotEmpty<TResult>(TResult result) => ValueOrChain<Guid, TResult>.From(value, value != Guid.Empty, result);

        /// <summary>Factory form of IfNotEmpty. The factory runs only when <paramref name="value" /> is not <see cref="Guid.Empty" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<Guid, TResult> IfNotEmpty<TResult>(Func<TResult> result) => ValueOrChain<Guid, TResult>.From(value, value != Guid.Empty, result);
    }

    extension<TResult>(ValueOrChain<Guid, TResult> chain)
    {
        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject is <see cref="Guid.Empty" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<Guid, TResult> OrIfEmpty(TResult result) => chain.IsChosen ? chain : chain.Choose(chain.Subject == Guid.Empty, result);

        /// <summary>When the chain is still open, invokes <paramref name="result" /> when the subject is <see cref="Guid.Empty" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<Guid, TResult> OrIfEmpty(Func<TResult> result) => chain.IsChosen ? chain : chain.Choose(chain.Subject == Guid.Empty, result);

        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject is not <see cref="Guid.Empty" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<Guid, TResult> OrIfNotEmpty(TResult result) => chain.IsChosen ? chain : chain.Choose(chain.Subject != Guid.Empty, result);

        /// <summary>When the chain is still open, invokes <paramref name="result" /> when the subject is not <see cref="Guid.Empty" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<Guid, TResult> OrIfNotEmpty(Func<TResult> result) => chain.IsChosen ? chain : chain.Choose(chain.Subject != Guid.Empty, result);
    }

    extension<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        /// <summary>Chooses <paramref name="result" /> when <paramref name="value" /> is a defined member of <typeparamref name="TEnum" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<TEnum, TResult> IfDefined<TResult>(TResult result) => ValueOrChain<TEnum, TResult>.From(value, ValueWhenChecks.IsDefined(value), result);

        /// <summary>Factory form of IfDefined. The factory runs only when <paramref name="value" /> is a defined member.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<TEnum, TResult> IfDefined<TResult>(Func<TResult> result) => ValueOrChain<TEnum, TResult>.From(value, ValueWhenChecks.IsDefined(value), result);

        /// <summary>Chooses <paramref name="result" /> when <paramref name="value" /> is not a defined member of <typeparamref name="TEnum" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<TEnum, TResult> IfNotDefined<TResult>(TResult result) => ValueOrChain<TEnum, TResult>.From(value, !ValueWhenChecks.IsDefined(value), result);

        /// <summary>Factory form of IfNotDefined. The factory runs only when <paramref name="value" /> is not a defined member.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<TEnum, TResult> IfNotDefined<TResult>(Func<TResult> result) => ValueOrChain<TEnum, TResult>.From(value, !ValueWhenChecks.IsDefined(value), result);
    }

    extension<TEnum, TResult>(ValueOrChain<TEnum, TResult> chain) where TEnum : struct, Enum
    {
        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject is a defined enum member.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<TEnum, TResult> OrIfDefined(TResult result) => chain.IsChosen ? chain : chain.Choose(ValueWhenChecks.IsDefined(chain.Subject), result);

        /// <summary>When the chain is still open, invokes <paramref name="result" /> when the subject is a defined enum member.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<TEnum, TResult> OrIfDefined(Func<TResult> result) => chain.IsChosen ? chain : chain.Choose(ValueWhenChecks.IsDefined(chain.Subject), result);

        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject is not a defined enum member.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<TEnum, TResult> OrIfNotDefined(TResult result) => chain.IsChosen ? chain : chain.Choose(!ValueWhenChecks.IsDefined(chain.Subject), result);

        /// <summary>When the chain is still open, invokes <paramref name="result" /> when the subject is not a defined enum member.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<TEnum, TResult> OrIfNotDefined(Func<TResult> result) => chain.IsChosen ? chain : chain.Choose(!ValueWhenChecks.IsDefined(chain.Subject), result);
    }

    extension(string? value)
    {
        /// <summary>Chooses <paramref name="result" /> when <paramref name="value" /> is null or empty. The result type does not have to be <see cref="string" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<string?, TResult> IfNullOrEmpty<TResult>(TResult result) => ValueOrChain<string?, TResult>.From(value, string.IsNullOrEmpty(value), result);

        /// <summary>Factory form of IfNullOrEmpty. The factory runs only when <paramref name="value" /> is null or empty.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<string?, TResult> IfNullOrEmpty<TResult>(Func<TResult> result) => ValueOrChain<string?, TResult>.From(value, string.IsNullOrEmpty(value), result);

        /// <summary>Chooses <paramref name="result" /> when <paramref name="value" /> is null, empty, or whitespace.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<string?, TResult> IfNullOrWhiteSpace<TResult>(TResult result)
            => ValueOrChain<string?, TResult>.From(value, string.IsNullOrWhiteSpace(value), result);

        /// <summary>Factory form of IfNullOrWhiteSpace. The factory runs only when <paramref name="value" /> is null, empty, or whitespace.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<string?, TResult> IfNullOrWhiteSpace<TResult>(Func<TResult> result)
            => ValueOrChain<string?, TResult>.From(value, string.IsNullOrWhiteSpace(value), result);

        /// <summary>
        /// Chooses <paramref name="result" /> when <paramref name="value" /> is non-null and its length is inside the inclusive range. A null string leaves the chain open.
        /// <paramref name="minLength" /> greater than <paramref name="maxLength" /> throws <see cref="ArgumentException" />.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<string?, TResult> IfLengthInRange<TResult>(int minLength, int maxLength, TResult result)
        {
            ArgumentHelpers.ThrowIf(minLength > maxLength, "Minimum length must be less than or equal to maximum length.");
            var selected = value is not null && value.Length >= minLength && value.Length <= maxLength;
            return ValueOrChain<string?, TResult>.From(value, selected, result);
        }

        /// <summary>Factory form of IfLengthInRange. A null string or a length outside the range does not invoke the factory.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<string?, TResult> IfLengthInRange<TResult>(int minLength, int maxLength, Func<TResult> result)
        {
            ArgumentHelpers.ThrowIf(minLength > maxLength, "Minimum length must be less than or equal to maximum length.");
            var selected = value is not null && value.Length >= minLength && value.Length <= maxLength;
            return ValueOrChain<string?, TResult>.From(value, selected, result);
        }
    }

    extension<TResult>(ValueOrChain<string?, TResult> chain)
    {
        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject is null or empty.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<string?, TResult> OrIfNullOrEmpty(TResult result) => chain.IsChosen ? chain : chain.Choose(string.IsNullOrEmpty(chain.Subject), result);

        /// <summary>When the chain is still open, invokes <paramref name="result" /> when the subject is null or empty.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<string?, TResult> OrIfNullOrEmpty(Func<TResult> result) => chain.IsChosen ? chain : chain.Choose(string.IsNullOrEmpty(chain.Subject), result);

        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject is null, empty, or whitespace.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<string?, TResult> OrIfNullOrWhiteSpace(TResult result)
            => chain.IsChosen ? chain : chain.Choose(string.IsNullOrWhiteSpace(chain.Subject), result);

        /// <summary>When the chain is still open, invokes <paramref name="result" /> when the subject is null, empty, or whitespace.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<string?, TResult> OrIfNullOrWhiteSpace(Func<TResult> result)
            => chain.IsChosen ? chain : chain.Choose(string.IsNullOrWhiteSpace(chain.Subject), result);

        /// <summary>When the chain is still open, chooses <paramref name="result" /> when the subject's length is inside the inclusive range.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<string?, TResult> OrIfLengthInRange(int minLength, int maxLength, TResult result)
        {
            if (chain.IsChosen)
                return chain;

            ArgumentHelpers.ThrowIf(minLength > maxLength, "Minimum length must be less than or equal to maximum length.");
            var selected = chain.Subject is not null && chain.Subject.Length >= minLength && chain.Subject.Length <= maxLength;
            return chain.Choose(selected, result);
        }

        /// <summary>When the chain is still open, invokes <paramref name="result" /> when the subject's length is inside the inclusive range.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<string?, TResult> OrIfLengthInRange(int minLength, int maxLength, Func<TResult> result)
        {
            if (chain.IsChosen)
                return chain;

            ArgumentHelpers.ThrowIf(minLength > maxLength, "Minimum length must be less than or equal to maximum length.");
            var selected = chain.Subject is not null && chain.Subject.Length >= minLength && chain.Subject.Length <= maxLength;
            return chain.Choose(selected, result);
        }
    }
}
