// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;

namespace MessagePack
{
    /// <summary>
    ///     Internal utilities and extension methods for various external types.
    /// </summary>
    internal static class Utilities
    {
        /// <summary>
        ///     A value indicating whether we're running on mono.
        /// </summary>
        internal static readonly bool IsMono = Type.GetType("Mono.RuntimeStructs") is Type;

        internal static byte[] GetWriterBytes<TArg>(TArg arg, GetWriterBytesAction<TArg> action, SequencePool pool)
        {
            using (var sequenceRental = pool.Rent())
            {
                var writer = new MessagePackWriter(sequenceRental.Value);
                action(ref writer, arg);
                writer.Flush();
                return sequenceRental.Value.AsReadOnlySequence.ToArray();
            }
        }

        internal static Memory<byte> GetMemoryCheckResult(this IBufferWriter<byte> bufferWriter, int size = 0)
        {
            var memory = bufferWriter.GetMemory(size);
            if (memory.IsEmpty)
                throw new InvalidOperationException(
                    "The underlying IBufferWriter<byte>.GetMemory(int) method returned an empty memory block, which is not allowed. This is a bug in " +
                    bufferWriter.GetType().FullName);

            return memory;
        }

        /// <summary>
        ///     Gets an <see cref="IDictionary" /> enumerator that does not allocate for each entry,
        ///     and that doesn't produce the nullable ref annotation warning about unboxing a possibly null value.
        /// </summary>
        internal static NonGenericDictionaryEnumerable GetEntryEnumerator(this IDictionary dictionary)
        {
            return new NonGenericDictionaryEnumerable(dictionary);
        }

        internal delegate void GetWriterBytesAction<TArg>(ref MessagePackWriter writer, TArg argument);

        internal struct NonGenericDictionaryEnumerable
        {
            private readonly IDictionary dictionary;

            internal NonGenericDictionaryEnumerable(IDictionary dictionary)
            {
                this.dictionary = dictionary;
            }

            public NonGenericDictionaryEnumerator GetEnumerator()
            {
                return new NonGenericDictionaryEnumerator(dictionary);
            }
        }

        internal struct NonGenericDictionaryEnumerator : IEnumerator<DictionaryEntry>
        {
            private readonly IDictionaryEnumerator enumerator;

            internal NonGenericDictionaryEnumerator(IDictionary dictionary)
            {
                enumerator = dictionary.GetEnumerator();
            }

            public DictionaryEntry Current => enumerator.Entry;

            object IEnumerator.Current => enumerator.Entry;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                return enumerator.MoveNext();
            }

            public void Reset()
            {
                enumerator.Reset();
            }
        }
    }
}