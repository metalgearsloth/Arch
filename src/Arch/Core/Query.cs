using Arch.Core.Extensions.Internal;
using Arch.Core.Utils;
using Collections.Pooled;
using CommunityToolkit.HighPerformance;

namespace Arch.Core;
using Arch.Core;

/// <summary>
///     The <see cref="Signature"/> struct
///     describes a combination of different <see cref="ComponentType"/>s and caches their hash. Its basically just a list of <see cref="ComponentType"/>s.
///     This is then used for describing an <see cref="Entity"/> aswell as identification to find the correct <see cref="Query"/> or a suitable <see cref="Archetype"/>.
/// </summary>
[SkipLocalsInit]
public struct Signature : IEquatable<Signature>
{
    /// <summary>
    ///     A null reference, basically an empty <see cref="Signature"/>.
    /// </summary>
    public static readonly Signature Null = new();

    /// <summary>
    ///     Its cached hashcode, because its incredible expensive to calculate a new hashcode everytime.
    /// </summary>
    private int _hashCode;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Signature"/> struct.
    /// </summary>
    public Signature()
    {
        ComponentsArray = Array.Empty<ComponentType>();
        _hashCode = -1;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Signature"/> struct.
    /// </summary>
    /// <param name="components">An array of <see cref="ComponentType"/>s.</param>
    public Signature(params ComponentType[] components)
    {
        ComponentsArray = components;
        _hashCode = -1;
        _hashCode = GetHashCode();
    }

    /// <summary>
    ///     An array of <see cref="ComponentType"/>s.
    /// </summary>
    internal ComponentType[] ComponentsArray
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        set;
    } = Array.Empty<ComponentType>();

    /// <summary>
    ///     An array of <see cref="ComponentType"/>s.
    /// </summary>
    public Span<ComponentType> Components
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ComponentsArray.AsSpan();
    }

    /// <summary>
    ///     The amount of <see cref="ComponentType"/>s in this instance.
    /// </summary>
    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ComponentsArray.Length;
    }

    /// <summary>
    ///     Checks for indifference, if the internal arrays have equal elements true is returned. Otherwise false.
    /// </summary>
    /// <param name="other">The other <see cref="Signature"/> to compare with.</param>
    /// <returns>True if elements of the arrays are equal, otherwhise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Signature other)
    {
        return GetHashCode() == other.GetHashCode();
    }

    /// <summary>
    ///     Checks for indifference, if the internal arrays have equal elements true is returned. Otherwise false.
    /// </summary>
    /// <param name="obj">The other <see cref="object"/> to compare with.</param>
    /// <returns>True if elements of the arrays are equal, otherwhise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
    {
        return obj is Signature other && Equals(other);
    }

    /// <summary>
    ///     Calculates the hash.
    /// </summary>
    /// <returns>The hash.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        // Cache hashcode since the calculation is expensive.
        if (_hashCode != -1)
        {
            return _hashCode;
        }

        unchecked
        {
            _hashCode = Component.GetHashCode(Components);
            return _hashCode;
        }
    }

    /// <summary>
    ///     Creates an <see cref="Enumerator{T}"/> which iterates over all <see cref="Components"/> in this <see cref="Signature"/>.
    /// </summary>
    /// <returns>An <see cref="Enumerator{T}"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator<ComponentType> GetEnumerator()
    {
        return new Enumerator<ComponentType>(Components);
    }

    /// <summary>
    ///     Checks for indifference, if the internal arrays have equal elements true is returned. Otherwise false.
    /// </summary>
    /// <param name="left">The left <see cref="Signature"/>.</param>
    /// <param name="right">The right <see cref="Signature"/>.</param>
    /// <returns>True if their internal arrays are equal, otherwhise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Signature left, Signature right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     Checks for difference, if the internal arrays have equal elements false is returned. Otherwise true.
    /// </summary>
    /// <param name="left">The left <see cref="Signature"/>.</param>
    /// <param name="right">The right <see cref="Signature"/>.</param>
    /// <returns>True if their internal arrays are unequal, otherwhise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Signature left, Signature right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    ///     Converts a <see cref="ComponentType"/> array into a <see cref="Signature"/>.
    /// </summary>
    /// <param name="components">The passed <see cref="ComponentType"/>s.</param>
    /// <returns>A new <see cref="Signature"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Signature(ComponentType[] components)
    {
        return new Signature(components);
    }

    /// <summary>
    ///     Converts a <see cref="Signature"/> into a <see cref="ComponentType"/>s array.
    /// </summary>
    /// <param name="signature">The passed <see cref="Signature"/>.</param>
    /// <returns>The <see cref="ComponentType"/>s array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ComponentType[](Signature signature)
    {
        return signature.ComponentsArray;
    }

    /// <summary>
    ///     Converts a <see cref="Signature"/> into a <see cref="ComponentType"/>s array.
    /// </summary>
    /// <param name="signature">The passed <see cref="Signature"/>.</param>
    /// <returns>The <see cref="ComponentType"/>s array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Span<ComponentType>(Signature signature)
    {
        return signature.Components;
    }

    /// <summary>
    ///     Converts a <see cref="Signature"/> into a <see cref="BitSet"/>.
    /// </summary>
    /// <param name="signature">The passed <see cref="Signature"/>.</param>
    /// <returns>A new <see cref="BitSet"/>s.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator BitSet(Signature signature)
    {
        if (signature.Count == 0)
        {
            return new BitSet();
        }

        var bitSet = new BitSet();
        bitSet.SetBits(signature.Components);

        return bitSet;
    }
}


/// <summary>
///     The <see cref="QueryDescription"/> struct
///     represents a description of the <see cref="Entity"/>'s or components we want to address by means of a query.
/// </summary>
[SkipLocalsInit]
public partial struct QueryDescription : IEquatable<QueryDescription>
{
    /// <summary>
    ///     A null reference, basically an empty <see cref="QueryDescription"/> that queries for all <see cref="Entity"/>s.
    /// </summary>
    public static readonly QueryDescription Null = new();

    /// <summary>
    ///     A cached hash code that is used to find the matching <see cref="Query"/> for this instance.
    /// </summary>
    private int _hashCode;

    /// <summary>
    ///     An <see cref="Signature"/> of all components that an <see cref="Entity"/> should have mandatory.
    /// <remarks>If the content of the array is subsequently changed, a <see cref="Rebuild"/> should be carried out.</remarks>
    /// </summary>
    public Signature All { get; set; } = new();

    /// <summary>
    ///     An array of all components of which an <see cref="Entity"/> should have at least one.
    /// <remarks>If the content of the array is subsequently changed, a <see cref="Rebuild"/> should be carried out.</remarks>
    /// </summary>
    public Signature Any { get; set; } = new();

    /// <summary>
    ///     An array of all components of which an <see cref="Entity"/> should not have any.
    /// <remarks>If the content of the array is subsequently changed, a <see cref="Rebuild"/> should be carried out.</remarks>
    /// </summary>
    public Signature None { get; set; } = new();

    /// <summary>
    ///     An array of all components that exactly match the structure of an <see cref="Entity"/>.
    ///     <see cref="Entity"/>'s with more or less components than those defined in the array are not addressed.
    /// <remarks>If the content of the array is subsequently changed, a <see cref="Rebuild"/> should be carried out.</remarks>
    /// </summary>
    public Signature Exclusive { get; set; } = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="QueryDescription"/> struct.
    /// </summary>
    public QueryDescription()
    {
        _hashCode = -1;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="QueryDescription"/> struct.
    /// </summary>
    /// <param name="all">An array of all components that an <see cref="Entity"/> should have mandatory.</param>
    /// <param name="any">An array of all components of which an <see cref="Entity"/> should have at least one.</param>
    /// <param name="none">An array of all components of which an <see cref="Entity"/> should not have any.</param>
    /// <param name="exclusive">All components that an <see cref="Entity"/> should have mandatory.</param>
    public QueryDescription(ComponentType[]? all = null, ComponentType[]? any = null, ComponentType[]? none = null, ComponentType[]? exclusive = null)
    {
        All = all ?? All;
        Any = any ?? Any;
        None = none ?? None;
        Exclusive = exclusive ?? Exclusive;

        _hashCode = -1;
        _hashCode = GetHashCode();
    }

    /// <summary>
    ///     Recreates this instance by calculating a new <see cref="_hashCode"/>.
    ///     Is actually only needed if the passed arrays are changed afterwards.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Rebuild()
    {
        _hashCode = -1;
        _hashCode = GetHashCode();
    }

    /// <summary>
    ///     All components that an <see cref="Entity"/> should have mandatory.
    /// </summary>
    /// <typeparam name="T">The generic type.</typeparam>
    /// <returns>The same <see cref="QueryDescription"/> instance for chained operations.</returns>
    [UnscopedRef]
    public ref QueryDescription WithAll<T>()
    {
        All = Component<T>.Signature;
        _hashCode = -1;
        return ref this;
    }

    /// <summary>
    ///     All components of which an <see cref="Entity"/> should have at least one.
    /// </summary>
    /// <typeparam name="T">The generic type.</typeparam>
    /// <returns>The same <see cref="QueryDescription"/> instance for chained operations.</returns>
    [UnscopedRef]
    public ref QueryDescription WithAny<T>()
    {
        Any = Component<T>.Signature;
        _hashCode = -1;
        return ref this;
    }

    /// <summary>
    ///     All components of which an <see cref="Entity"/> should have none.
    /// </summary>
    /// <typeparam name="T">The generic type.</typeparam>
    /// <returns>The same <see cref="QueryDescription"/> instance for chained operations.</returns>
    [UnscopedRef]
    public ref QueryDescription WithNone<T>()
    {
        None = Component<T>.Signature;
        _hashCode = -1;
        return ref this;
    }

    /// <summary>
    ///     An array of all components that exactly match the structure of an <see cref="Entity"/>.
    ///     <see cref="Entity"/>'s with more or less components than those defined in the array are not addressed.
    /// </summary>
    /// <typeparam name="T">The generic type.</typeparam>
    /// <returns>The same <see cref="QueryDescription"/> instance for chained operations.</returns>
    [UnscopedRef]
    public ref QueryDescription WithExclusive<T>()
    {
        Exclusive = Component<T>.Signature;
        _hashCode = -1;
        return ref this;
    }

    /// <summary>
    ///     Checks for indifference, if the internal arrays have equal elements true is returned. Otherwise false.
    /// </summary>
    /// <param name="other">The other <see cref="QueryDescription"/> to compare with.</param>
    /// <returns>True if elements of the arrays are equal, otherwhise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(QueryDescription other)
    {
        return GetHashCode() == other.GetHashCode();
    }

    /// <summary>
    ///     Checks for indifference, if the internal arrays have equal elements true is returned. Otherwise false.
    /// </summary>
    /// <param name="obj">The other <see cref="object"/> to compare with.</param>
    /// <returns>True if elements of the arrays are equal, otherwhise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
    {
        return obj is QueryDescription other && Equals(other);
    }

    /// NOTE: Probably we should use Component.GetHashCode(...) ?
    /// <summary>
    ///     Calculates the hash.
    /// </summary>
    /// <returns>The hash.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        // Cache hashcode since the calculation is expensive.
        if (_hashCode != -1)
        {
            return _hashCode;
        }

        unchecked
        {
            // Overflow is fine, just wrap{
            var hash = 17;
            hash = (hash * 23) + All.GetHashCode();
            hash = (hash * 23) + Any.GetHashCode();
            hash = (hash * 23) + None.GetHashCode();
            hash = (hash * 23) + Exclusive.GetHashCode();
            _hashCode = hash;
            return hash;
        }
    }

    /// <summary>
    ///     Checks for indifference, if the internal arrays have equal elements true is returned. Otherwise false.
    /// </summary>
    /// <param name="left">The left <see cref="QueryDescription"/>.</param>
    /// <param name="right">The right <see cref="QueryDescription"/>.</param>
    /// <returns>True if their internal arrays are equal, otherwhise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(QueryDescription left, QueryDescription right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     Checks for difference, if the internal arrays have equal elements false is returned. Otherwise true.
    /// </summary>
    /// <param name="left">The left <see cref="QueryDescription"/>.</param>
    /// <param name="right">The right <see cref="QueryDescription"/>.</param>
    /// <returns>True if their internal arrays are unequal, otherwhise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(QueryDescription left, QueryDescription right)
    {
        return !left.Equals(right);
    }
}

/// <summary>
///     The <see cref="Query"/> struct
///     Represents a query which is created based on a <see cref="World"/> and a <see cref="QueryDescription"/>.
///     It provides some methods to iterate over all <see cref="Entity"/>'s that match the aspect of the <see cref="QueryDescription"/> that was used to create this instance.
/// </summary>
[SkipLocalsInit]
public readonly partial struct Query : IEquatable<Query>
{
    private readonly QueryDescription _queryDescription;

    private readonly BitSet _any;
    private readonly BitSet _all;
    private readonly BitSet _none;
    private readonly BitSet _exclusive;

    private readonly bool _isExclusive;
    public readonly PooledList<Archetype> Matches;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Query"/> struct.
    /// </summary>
    /// <param name="archetypes">The <see cref="Archetype"/>'s this query iterates over.</param>
    /// <param name="description">The <see cref="QueryDescription"/> used to target <see cref="Entity"/>'s.</param>
    internal Query(PooledList<Archetype> archetypes, QueryDescription description) : this()
    {
        Debug.Assert(
            !((description.Any.Count != 0 ||
            description.All.Count != 0 ||
            description.None.Count != 0) &&
            description.Exclusive.Count != 0),
            "If Any, All or None have items then Exclusive may not have any items"
        );

        // Convert to `BitSet`s.
        _all = description.All;
        _any = description.Any;
        _none = description.None;
        _exclusive = description.Exclusive;

        // Handle exclusive.
        if (description.Exclusive.Count != 0)
        {
            _isExclusive = true;
        }

        _queryDescription = description;

        Matches = new PooledList<Archetype>();
        foreach (var archetype in archetypes)
        {
            if (Valid(archetype.BitSet))
            {
                Matches.Add(archetype);
                archetype.QueryMatches.Add(Matches);
            }
        }
    }

    /// <summary>
    ///     Checks whether the specified <see cref="BitSet"/> matches.
    /// </summary>
    /// <param name="bitset">The <see cref="BitSet"/> to compare with.</param>
    /// <returns>True if it matches, otherwhise false.</returns>
    public bool Valid(BitSet bitset)
    {
        return _isExclusive ? _exclusive.Exclusive(bitset) : _all.All(bitset) && _any.Any(bitset) && _none.None(bitset);
    }

    /// <summary>
    ///     Returns an iterator to iterate over all <see cref="Archetype"/>'s containing <see cref="Entity"/>'s addressed by this <see cref="Query"/>.
    /// </summary>
    /// <returns>A new instance of the <see cref="QueryArchetypeIterator"/>.</returns>
    public QueryArchetypeIterator GetArchetypeIterator()
    {
        return new QueryArchetypeIterator(this, Matches.Span);
    }

    /// <summary>
    ///     Returns an iterator to iterate over all <see cref="Chunk"/>'s containing <see cref="Entity"/>'s addressed by this <see cref="Query"/>.
    /// </summary>
    /// <returns>A new instance of the <see cref="QueryChunkIterator"/>.</returns>
    public QueryChunkIterator GetChunkIterator()
    {
        return new QueryChunkIterator(this, Matches.Span);
    }

    /// <summary>
    ///     Returns an enumerator to iterate over all <see cref="Chunk"/>'s containing <see cref="Entity"/>'s addressed by this <see cref="Query"/>.
    /// </summary>
    /// <returns>A new instance of the <see cref="QueryChunkIterator"/>.</returns>
    public QueryChunkEnumerator GetEnumerator()
    {
        return new QueryChunkEnumerator(this, Matches.Span);
    }

    /// <summary>
    ///     Checks this <see cref="Query"/> for equality with another.
    /// </summary>
    /// <param name="other">The other <see cref="Query"/>.</param>
    /// <returns>True if they are equal, false if not.</returns>
    public bool Equals(Query other)
    {
        return Equals(_any, other._any) && Equals(_all, other._all) && Equals(_none, other._none) && Equals(_exclusive, other._exclusive) && _queryDescription.Equals(other._queryDescription);
    }

    /// <summary>
    ///      Checks this <see cref="Query"/> for equality with another object.
    /// </summary>
    /// <param name="obj">The other <see cref="object"/>.</param>
    /// <returns>True if they are equal, false if not.</returns>
    public override bool Equals(object? obj)
    {
        return obj is Query other && Equals(other);
    }

    /// NOTE: Probably we should use Component.GetHashCode(...) ?
    /// <summary>
    ///     Calculates the hash.
    /// </summary>
    /// <returns>The hash.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = _any is not null ? _any.GetHashCode() : 0;
            hashCode = (hashCode * 397) ^ (_all is not null ? _all.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (_none is not null ? _none.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (_exclusive?.GetHashCode() ?? 0);
            hashCode = (hashCode * 397) ^ _queryDescription.GetHashCode();

            return hashCode;
        }
    }

    /// <summary>
    ///     Checks for indifference.
    /// </summary>
    /// <param name="left">The left <see cref="Query"/>.</param>
    /// <param name="right">The right <see cref="Query"/>.</param>
    /// <returns>True if they are equal, false if not.</returns>
    public static bool operator ==(Query left, Query right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     Checks for difference.
    /// </summary>
    /// <param name="left">The left <see cref="Query"/>.</param>
    /// <param name="right">The right <see cref="Query"/>.</param>
    /// <returns>True if they are unequal, false if not.</returns>
    public static bool operator !=(Query left, Query right)
    {
        return !left.Equals(right);
    }
}
