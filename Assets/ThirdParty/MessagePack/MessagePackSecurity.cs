// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using MessagePack.Formatters;
using MessagePack.Internal;

namespace MessagePack
{
    /// <summary>
    ///     Settings related to security, particularly relevant when deserializing data from untrusted sources.
    /// </summary>
    public class MessagePackSecurity
    {
        /// <summary>
        ///     Gets an instance preconfigured with settings that omit all protections. Useful for deserializing fully-trusted and
        ///     valid msgpack sequences.
        /// </summary>
        public static readonly MessagePackSecurity TrustedData = new();

        /// <summary>
        ///     Gets an instance preconfigured with protections applied with reasonable settings for deserializing untrusted
        ///     msgpack sequences.
        /// </summary>
        public static readonly MessagePackSecurity UntrustedData = new()
        {
            HashCollisionResistant = true,
            MaximumObjectGraphDepth = 500
        };

        private static readonly SipHash Hash = new();

        private readonly ObjectFallbackEqualityComparer objectFallbackEqualityComparer;

        private MessagePackSecurity()
        {
            objectFallbackEqualityComparer = new ObjectFallbackEqualityComparer(this);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessagePackSecurity" /> class
        ///     with properties copied from a provided template.
        /// </summary>
        /// <param name="copyFrom">The template to copy from.</param>
        protected MessagePackSecurity(MessagePackSecurity copyFrom)
            : this()
        {
            if (copyFrom is null) throw new ArgumentNullException(nameof(copyFrom));

            HashCollisionResistant = copyFrom.HashCollisionResistant;
            MaximumObjectGraphDepth = copyFrom.MaximumObjectGraphDepth;
        }

        /// <summary>
        ///     Gets a value indicating whether data to be deserialized is untrusted and thus should not be allowed to create
        ///     dictionaries or other hash-based collections unless the hashed type has a hash collision resistant implementation
        ///     available.
        ///     This can mitigate some denial of service attacks when deserializing untrusted code.
        /// </summary>
        /// <value>
        ///     The value is <see langword="false" /> for <see cref="TrustedData" /> and <see langword="true" /> for
        ///     <see cref="UntrustedData" />.
        /// </value>
        public bool HashCollisionResistant { get; private set; }

        /// <summary>
        ///     Gets the maximum depth of an object graph that may be deserialized.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This value can be reduced to avoid a stack overflow that would crash the process when deserializing a msgpack
        ///         sequence designed to cause deep recursion.
        ///         A very short callstack on a thread with 1MB of total stack space might deserialize ~2000 nested arrays before
        ///         crashing due to a stack overflow.
        ///         Since stack space occupied may vary by the kind of object deserialized, a conservative value for this property
        ///         to defend against stack overflow attacks might be 500.
        ///     </para>
        /// </remarks>
        public int MaximumObjectGraphDepth { get; private set; } = int.MaxValue;

        /// <summary>
        ///     Gets a copy of these options with the <see cref="MaximumObjectGraphDepth" /> property set to a new value.
        /// </summary>
        /// <param name="maximumObjectGraphDepth">The new value for the <see cref="MaximumObjectGraphDepth" /> property.</param>
        /// <returns>The new instance; or the original if the value is unchanged.</returns>
        public MessagePackSecurity WithMaximumObjectGraphDepth(int maximumObjectGraphDepth)
        {
            if (MaximumObjectGraphDepth == maximumObjectGraphDepth) return this;

            var clone = Clone();
            clone.MaximumObjectGraphDepth = maximumObjectGraphDepth;
            return clone;
        }

        /// <summary>
        ///     Gets a copy of these options with the <see cref="HashCollisionResistant" /> property set to a new value.
        /// </summary>
        /// <param name="hashCollisionResistant">The new value for the <see cref="HashCollisionResistant" /> property.</param>
        /// <returns>The new instance; or the original if the value is unchanged.</returns>
        public MessagePackSecurity WithHashCollisionResistant(bool hashCollisionResistant)
        {
            if (HashCollisionResistant == hashCollisionResistant) return this;

            var clone = Clone();
            clone.HashCollisionResistant = hashCollisionResistant;
            return clone;
        }

        /// <summary>
        ///     Gets an <see cref="IEqualityComparer{T}" /> that is suitable to use with a hash-based collection.
        /// </summary>
        /// <typeparam name="T">The type of key that will be hashed in the collection.</typeparam>
        /// <returns>The <see cref="IEqualityComparer{T}" /> to use.</returns>
        /// <remarks>
        ///     When <see cref="HashCollisionResistant" /> is active, this will be a collision resistant instance which may reject
        ///     certain key types.
        ///     When <see cref="HashCollisionResistant" /> is not active, this will be <see cref="EqualityComparer{T}.Default" />.
        /// </remarks>
        public IEqualityComparer<T> GetEqualityComparer<T>()
        {
            return HashCollisionResistant
                ? GetHashCollisionResistantEqualityComparer<T>()
                : EqualityComparer<T>.Default;
        }

        /// <summary>
        ///     Gets an <see cref="IEqualityComparer" /> that is suitable to use with a hash-based collection.
        /// </summary>
        /// <returns>The <see cref="IEqualityComparer" /> to use.</returns>
        /// <remarks>
        ///     When <see cref="HashCollisionResistant" /> is active, this will be a collision resistant instance which may reject
        ///     certain key types.
        ///     When <see cref="HashCollisionResistant" /> is not active, this will be <see cref="EqualityComparer{T}.Default" />.
        /// </remarks>
        public IEqualityComparer GetEqualityComparer()
        {
            return HashCollisionResistant
                ? GetHashCollisionResistantEqualityComparer()
                : EqualityComparer<object>.Default;
        }

        /// <summary>
        ///     Returns a hash collision resistant equality comparer.
        /// </summary>
        /// <typeparam name="T">The type of key that will be hashed in the collection.</typeparam>
        /// <returns>A hash collision resistant equality comparer.</returns>
        protected virtual IEqualityComparer<T> GetHashCollisionResistantEqualityComparer<T>()
        {
            if (HashResistantCache<T>.EqualityComparer is { } result) return result;

            if (typeof(T) == typeof(object)) return (IEqualityComparer<T>)objectFallbackEqualityComparer;

            // Any type we don't explicitly whitelist here shouldn't be allowed to use as the key in a hash-based collection since it isn't known to be hash resistant.
            // This method can of course be overridden to add more hash collision resistant type support, or the deserializing party can indicate that the data is Trusted
            // so that this method doesn't even get called.
            throw new TypeAccessException($"No hash-resistant equality comparer available for type: {typeof(T)}");
        }

        /// <summary>
        ///     Checks the depth of the deserializing graph and increments it by 1.
        /// </summary>
        /// <param name="reader">The reader that is involved in deserialization.</param>
        /// <remarks>
        ///     Callers should decrement <see cref="MessagePackReader.Depth" /> after exiting that edge in the graph.
        /// </remarks>
        /// <exception cref="InsufficientExecutionStackException">
        ///     Thrown if <see cref="MessagePackReader.Depth" /> is already at or
        ///     exceeds <see cref="MaximumObjectGraphDepth" />.
        /// </exception>
        /// <remarks>
        ///     Rather than wrap the body of every <see cref="IMessagePackFormatter{T}.Deserialize" /> method,
        ///     this should wrap *calls* to these methods. They need not appear in pure "thunk" methods that simply delegate the
        ///     deserialization to another formatter.
        ///     In this way, we can avoid repeatedly incrementing and decrementing the counter when deserializing each element of a
        ///     collection.
        /// </remarks>
        public void DepthStep(ref MessagePackReader reader)
        {
            if (reader.Depth >= MaximumObjectGraphDepth)
                throw new InsufficientExecutionStackException(
                    $"This msgpack sequence has an object graph that exceeds the maximum depth allowed of {MaximumObjectGraphDepth}.");

            reader.Depth++;
        }

        /// <summary>
        ///     Returns a hash collision resistant equality comparer.
        /// </summary>
        /// <returns>A hash collision resistant equality comparer.</returns>
        protected virtual IEqualityComparer GetHashCollisionResistantEqualityComparer()
        {
            return (IEqualityComparer)GetHashCollisionResistantEqualityComparer<object>();
        }

        /// <summary>
        ///     Creates a new instance that is a copy of this one.
        /// </summary>
        /// <remarks>
        ///     Derived types should override this method to instantiate their own derived type.
        /// </remarks>
        protected virtual MessagePackSecurity Clone()
        {
            return new MessagePackSecurity(this);
        }

        private static int SecureHash<T>(T value)
            where T : unmanaged
        {
            Span<T> span = stackalloc T[1];
            span[0] = value;
            return unchecked((int)Hash.Compute(MemoryMarshal.Cast<T, byte>(span)));
        }

        private static int SecureHash(ReadOnlySpan<byte> data)
        {
            return unchecked((int)Hash.Compute(data));
        }

        private class HashResistantCache<T>
        {
            internal static readonly IEqualityComparer<T>? EqualityComparer;

            static HashResistantCache()
            {
                // We have to specially handle some 32-bit types (e.g. float) where multiple in-memory representations should hash to the same value.
                // Any type supported by the PrimitiveObjectFormatter should be added here if supporting it as a key in a collection makes sense.
                EqualityComparer =
                    typeof(T) == typeof(bool)
                        ? (IEqualityComparer<T>)CollisionResistantHasherUnmanaged<bool>.Instance
                        :
                        typeof(T) == typeof(char)
                            ? (IEqualityComparer<T>)CollisionResistantHasherUnmanaged<char>.Instance
                            :
                            typeof(T) == typeof(sbyte)
                                ?
                                (IEqualityComparer<T>)CollisionResistantHasherUnmanaged<sbyte>.Instance
                                :
                                typeof(T) == typeof(byte)
                                    ? (IEqualityComparer<T>)CollisionResistantHasherUnmanaged<byte>.Instance
                                    :
                                    typeof(T) == typeof(short)
                                        ? (IEqualityComparer<T>)CollisionResistantHasherUnmanaged<short>.Instance
                                        :
                                        typeof(T) == typeof(ushort)
                                            ? (IEqualityComparer<T>)CollisionResistantHasherUnmanaged<ushort>.Instance
                                            :
                                            typeof(T) == typeof(int)
                                                ? (IEqualityComparer<T>)CollisionResistantHasherUnmanaged<int>.Instance
                                                :
                                                typeof(T) == typeof(uint)
                                                    ? (IEqualityComparer<T>)CollisionResistantHasherUnmanaged<uint>
                                                        .Instance
                                                    :
                                                    typeof(T) == typeof(long)
                                                        ? (IEqualityComparer<T>)CollisionResistantHasherUnmanaged<long>
                                                            .Instance
                                                        :
                                                        typeof(T) == typeof(ulong)
                                                            ? (IEqualityComparer<T>)CollisionResistantHasherUnmanaged<
                                                                    ulong>
                                                                .Instance
                                                            :
                                                            typeof(T) == typeof(Guid)
                                                                ? (IEqualityComparer<T>)
                                                                CollisionResistantHasherUnmanaged<Guid>
                                                                    .Instance
                                                                :

                                                                // Data types that are managed or have multiple in-memory representations for equivalent values:
                                                                typeof(T) == typeof(float)
                                                                    ? (IEqualityComparer<T>)SingleEqualityComparer
                                                                        .Instance
                                                                    :
                                                                    typeof(T) == typeof(double)
                                                                        ? (IEqualityComparer<T>)DoubleEqualityComparer
                                                                            .Instance
                                                                        :
                                                                        typeof(T) == typeof(string)
                                                                            ? (IEqualityComparer<T>)
                                                                            StringEqualityComparer
                                                                                .Instance
                                                                            :
                                                                            typeof(T) == typeof(DateTime)
                                                                                ? (IEqualityComparer<T>)
                                                                                DateTimeEqualityComparer
                                                                                    .Instance
                                                                                :
                                                                                typeof(T) == typeof(DateTimeOffset)
                                                                                    ? (IEqualityComparer<T>)
                                                                                    DateTimeOffsetEqualityComparer
                                                                                        .Instance
                                                                                    :

                                                                                    // Call out each primitive behind an enum explicitly to avoid dynamically generating code.
                                                                                    typeof(T).GetTypeInfo().IsEnum &&
                                                                                    typeof(T).GetTypeInfo()
                                                                                            .GetEnumUnderlyingType() is
                                                                                        Type
                                                                                        underlying
                                                                                        ? underlying == typeof(byte)
                                                                                            ?
                                                                                            CollisionResistantEnumHasher
                                                                                            <T,
                                                                                                byte>.Instance
                                                                                            :
                                                                                            underlying == typeof(sbyte)
                                                                                                ? CollisionResistantEnumHasher
                                                                                                <T
                                                                                                    , sbyte>.Instance
                                                                                                :
                                                                                                underlying ==
                                                                                                typeof(ushort)
                                                                                                    ? CollisionResistantEnumHasher
                                                                                                        <T, ushort>
                                                                                                    .Instance
                                                                                                    :
                                                                                                    underlying ==
                                                                                                    typeof(short)
                                                                                                        ? CollisionResistantEnumHasher
                                                                                                            <T, short>
                                                                                                        .Instance
                                                                                                        :
                                                                                                        underlying ==
                                                                                                        typeof(uint)
                                                                                                            ? CollisionResistantEnumHasher
                                                                                                                <T,
                                                                                                                    uint>
                                                                                                                .Instance
                                                                                                            :
                                                                                                            underlying ==
                                                                                                            typeof(int)
                                                                                                                ? CollisionResistantEnumHasher
                                                                                                                    <T
                                                                                                                        , int>
                                                                                                                    .Instance
                                                                                                                :
                                                                                                                underlying ==
                                                                                                                typeof(
                                                                                                                    ulong)
                                                                                                                    ? CollisionResistantEnumHasher
                                                                                                                        <T
                                                                                                                            ,
                                                                                                                            ulong>
                                                                                                                        .Instance
                                                                                                                    :
                                                                                                                    underlying ==
                                                                                                                    typeof
                                                                                                                        (long)
                                                                                                                        ? CollisionResistantEnumHasher
                                                                                                                            <T
                                                                                                                                ,
                                                                                                                                long>
                                                                                                                            .Instance
                                                                                                                        :
                                                                                                                        null
                                                                                        :

                                                                                        // Failsafe. If we don't recognize the type, don't assume we have a good, secure hash function for it.
                                                                                        null;
            }
        }

        /// <summary>
        ///     A hash collision resistant implementation of <see cref="IEqualityComparer{T}" />.
        /// </summary>
        /// <typeparam name="T">The type of key that will be hashed.</typeparam>
        private abstract class CollisionResistantHasher<T> : IEqualityComparer<T>, IEqualityComparer
        {
            bool IEqualityComparer.Equals(object? x, object? y)
            {
                return ((IEqualityComparer)EqualityComparer<T>.Default).Equals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return GetHashCode((T)obj);
            }

            public bool Equals(T? x, T? y)
            {
                return EqualityComparer<T?>.Default.Equals(x, y);
            }

            public abstract int GetHashCode(T value);
        }

        private class CollisionResistantHasherUnmanaged<T> : CollisionResistantHasher<T>
            where T : unmanaged
        {
            internal static readonly CollisionResistantHasherUnmanaged<T> Instance = new();

            public override int GetHashCode(T value)
            {
                return SecureHash(value);
            }
        }

        /// <summary>
        ///     A special hash-resistant equality comparer that defers picking the actual implementation
        ///     till it can check the runtime type of each value to be hashed.
        /// </summary>
        private class ObjectFallbackEqualityComparer : IEqualityComparer<object>, IEqualityComparer
        {
            private static readonly Lazy<MethodInfo> GetHashCollisionResistantEqualityComparerOpenGenericMethod =
                new(() => typeof(MessagePackSecurity).GetTypeInfo().DeclaredMethods.Single(m =>
                    m.Name == nameof(GetHashCollisionResistantEqualityComparer) && m.IsGenericMethod));

            private readonly ThreadsafeTypeKeyHashTable<IEqualityComparer> equalityComparerCache = new();
            private readonly MessagePackSecurity security;

            internal ObjectFallbackEqualityComparer(MessagePackSecurity security)
            {
                this.security = security ?? throw new ArgumentNullException(nameof(security));
            }

            bool IEqualityComparer.Equals(object? x, object? y)
            {
                return ((IEqualityComparer)EqualityComparer<object>.Default).Equals(x, y);
            }

            bool IEqualityComparer<object>.Equals(object? x, object? y)
            {
                return EqualityComparer<object?>.Default.Equals(x, y);
            }

            public int GetHashCode(object value)
            {
                if (value is null) return 0;

                var valueType = value.GetType();

                // Take care to avoid recursion.
                if (valueType == typeof(object))
                    // We can trust object.GetHashCode() to be collision resistant.
                    return value.GetHashCode();

                if (!equalityComparerCache.TryGetValue(valueType, out var equalityComparer))
                {
                    try
                    {
                        equalityComparer = (IEqualityComparer)GetHashCollisionResistantEqualityComparerOpenGenericMethod
                            .Value.MakeGenericMethod(valueType).Invoke(security, Array.Empty<object>())!;
                    }
                    catch (TargetInvocationException ex) when (ex.InnerException is not null)
                    {
                        ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                        throw null!; // not reachable
                    }

                    equalityComparerCache.TryAdd(valueType, equalityComparer);
                }

                return equalityComparer.GetHashCode(value);
            }
        }

        private class SingleEqualityComparer : CollisionResistantHasherUnmanaged<float>
        {
            internal new static readonly SingleEqualityComparer Instance = new();

            public override int GetHashCode(float value)
            {
                return base.GetHashCode(value switch
                {
                    0.0f => 0, // Special check for 0.0 so that the hash of 0.0 and -0.0 will equal.
                    float.NaN => float.NaN, // Standardize on the binary representation of NaN prior to hashing.
                    _ => value
                });
            }
        }

        private class DoubleEqualityComparer : CollisionResistantHasherUnmanaged<double>
        {
            internal new static readonly DoubleEqualityComparer Instance = new();

            public override int GetHashCode(double value)
            {
                return base.GetHashCode(value switch
                {
                    0.0 => 0, // Special check for 0.0 so that the hash of 0.0 and -0.0 will equal.
                    double.NaN => double.NaN, // Standardize on the binary representation of NaN prior to hashing.
                    _ => value
                });
            }
        }

        private class DateTimeEqualityComparer : CollisionResistantHasherUnmanaged<DateTime>
        {
            internal new static readonly DateTimeEqualityComparer Instance = new();

            public override int GetHashCode(DateTime value)
            {
                return SecureHash(value.Ticks);
            }
        }

        private class DateTimeOffsetEqualityComparer : CollisionResistantHasherUnmanaged<DateTimeOffset>
        {
            internal new static readonly DateTimeOffsetEqualityComparer Instance = new();

            public override int GetHashCode(DateTimeOffset value)
            {
                return SecureHash(value.UtcDateTime.Ticks);
            }
        }

        private class StringEqualityComparer : CollisionResistantHasher<string>
        {
            internal static readonly StringEqualityComparer Instance = new();

            public override int GetHashCode(string value)
            {
                // The Cast call could result in OverflowException at runtime if value is greater than 1bn chars in length.
                return SecureHash(MemoryMarshal.Cast<char, byte>(value.AsSpan()));
            }
        }

        private class CollisionResistantEnumHasher<TEnum, TUnderlying> : IEqualityComparer<TEnum>, IEqualityComparer
            where TUnderlying : unmanaged
        {
            internal static readonly CollisionResistantEnumHasher<TEnum, TUnderlying> Instance = new();

            bool IEqualityComparer.Equals(object? x, object? y)
            {
                return x is TEnum e1 && y is TEnum e2 && Equals(e1, e2);
            }

            int IEqualityComparer.GetHashCode(object obj)
            {
                return GetHashCode((TEnum)obj);
            }

            public bool Equals(TEnum? x, TEnum? y)
            {
                return EqualityComparer<TEnum?>.Default.Equals(x, y);
            }

            public int GetHashCode(TEnum obj)
            {
                return SecureHash(Unsafe.As<TEnum, TUnderlying>(ref obj));
            }
        }
    }
}