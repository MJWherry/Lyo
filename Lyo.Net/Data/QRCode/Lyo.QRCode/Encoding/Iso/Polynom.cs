using System.Buffers;
using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.QRCode.Encoding.Iso;

internal sealed partial class QRIsoEncoder
{
    /// <summary>A polynomial: the sum of its terms.</summary>
    private struct Polynom : IDisposable
    {
        private PolynomItem[] _polyItems;

        /// <summary>Builds a <see cref="Polynom" /> with room for the given number of terms.</summary>
        /// <param name="count">Initial capacity of the term list.</param>
        public Polynom(int count)
        {
            Count = 0;
            _polyItems = RentArray(count);
        }

        /// <summary>Appends a term.</summary>
        public void Add(PolynomItem item)
        {
            AssertCapacity(Count + 1);
            _polyItems[Count++] = item;
        }

        /// <summary>Removes the term at the given index.</summary>
        public void RemoveAt(int index)
        {
            ArgumentHelpers.ThrowIfGreaterThanOrEqual(index, Count);
            if (index < Count - 1)
                Array.Copy(_polyItems, index + 1, _polyItems, index, Count - index - 1);

            Count--;
        }

        /// <summary>Term at the given index.</summary>
        public PolynomItem this[int index] {
            get {
                ArgumentHelpers.ThrowIfGreaterThanOrEqual(index, Count);
                return _polyItems[index];
            }
            set {
                ArgumentHelpers.ThrowIfGreaterThanOrEqual(index, Count);
                _polyItems[index] = value;
            }
        }

        /// <summary>Number of terms.</summary>
        public int Count { get; private set; }

        /// <summary>Drops every term.</summary>
        public void Clear() => Count = 0;

        /// <summary>Builds a copy with the same terms.</summary>
        public Polynom Clone()
        {
            var newPolynom = new Polynom(Count);
            Array.Copy(_polyItems, newPolynom._polyItems, Count);
            newPolynom.Count = Count;
            return newPolynom;
        }

        /// <summary>Sorts the <see cref="PolynomItem" /> terms with a custom comparer.</summary>
        /// <param name="comparer">
        /// Compares two <see cref="PolynomItem" /> values. Negative if the first is less than the second, zero if equal, positive if the first is greater.
        /// </param>
        public void Sort(Func<PolynomItem, PolynomItem, int> comparer)
        {
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));

            var items = _polyItems ?? throw new ObjectDisposedException(nameof(Polynom));
            if (Count <= 1)
                return; // empty or single-term: already ordered

            void QuickSort(int left, int right)
            {
                var i = left;
                var j = right;
                var pivot = items[(left + right) / 2];
                while (i <= j) {
                    while (comparer(items[i], pivot) < 0)
                        i++;

                    while (comparer(items[j], pivot) > 0)
                        j--;

                    if (i > j)
                        continue;

                    // Exchange items[i] and items[j]
                    (items[i], items[j]) = (items[j], items[i]);
                    i++;
                    j--;
                }

                // Sort each partition
                if (left < j)
                    QuickSort(left, j);

                if (i < right)
                    QuickSort(i, right);
            }

            QuickSort(0, Count - 1);
        }

        /// <summary>
        /// Algebraic form of the polynomial. Example: "a^2*x^3 + a^5*x^1 + a^3*x^0" for 2x³ + 5x + 3.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            for (var i = 0; i < Count; i++) {
                var polyItem = _polyItems[i];
                sb.Append("a^" + polyItem.Coefficient + "*x^" + polyItem.Exponent + " + ");
            }

            // Trim the trailing " + " after the last term
            if (sb.Length > 0)
                sb.Length -= 3;

            return sb.ToString();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            ReturnArray(_polyItems);
            _polyItems = null!;
        }

        /// <summary>Requires enough capacity for the given number of terms.</summary>
        private void AssertCapacity(int min)
        {
            if (_polyItems.Length < min) {
                // QRCoder math uses fixed polynomials; capacity is not grown.
                ThrowNotSupportedException();

                // Example grow path (unused):
                //var newArray = RentArray(Math.Max(min - 1, 8) * 2); // double, minimum +8
                //Array.Copy(_polyItems, newArray, _length);
                //ReturnArray(_polyItems);
                //_polyItems = newArray;
            }

#if NET6_0_OR_GREATER
            [StackTraceHidden]
#endif
            void ThrowNotSupportedException() => throw new NotSupportedException("The polynomial capacity is fixed and cannot be increased.");
        }

#if HAS_SPAN
        /// <summary>Rents term storage from the shared pool.</summary>
        private static PolynomItem[] RentArray(int count) => ArrayPool<PolynomItem>.Shared.Rent(count);

        /// <summary>Returns term storage to the shared pool.</summary>
        private static void ReturnArray(PolynomItem[] array) => ArrayPool<PolynomItem>.Shared.Return(array);
#else
        // Minimal array pool for .NET Framework
        [ThreadStatic]
        private static List<PolynomItem[]>? _arrayPool;

        /// <summary>
        /// Rents term storage from a shared pool.
        /// </summary>
        private static PolynomItem[] RentArray(int count)
        {
            if (count <= 0)
                ThrowArgumentOutOfRangeException();

            // Look for a large-enough array in the thread-local pool, when it exists
            if (_arrayPool != null)
            {
                for (int i = 0; i < _arrayPool.Count; i++)
                {
                    var array = _arrayPool[i];
                    if (array.Length >= count)
                    {
                        _arrayPool.RemoveAt(i);
                        return array;
                    }
                }
            }

            // Allocate when the pool has no fit
            return new PolynomItem[count];

            void ThrowArgumentOutOfRangeException() => throw new ArgumentOutOfRangeException(nameof(count), "The count must be a positive number.");
        }

        /// <summary>
        /// Returns term storage to a shared pool.
        /// </summary>
        private static void ReturnArray(PolynomItem[] array)
        {
            if (array == null)
                return;

            // Create the thread-local pool on first return
            _arrayPool ??= new List<PolynomItem[]>(8);

            // Put the buffer back in the pool
            _arrayPool.Add(array);
        }
#endif

        /// <summary>Enumerator over the terms.</summary>
        public PolynumEnumerator GetEnumerator() => new(this);

        /// <summary>Value-type enumerator for <see cref="Polynom" />.</summary>
        public struct PolynumEnumerator
        {
            private Polynom _polynom;
            private int _index;

            public PolynumEnumerator(Polynom polynom)
            {
                _polynom = polynom;
                _index = -1;
            }

            public PolynomItem Current => _polynom[_index];

            public bool MoveNext() => ++_index < _polynom.Count;
        }
    }
}