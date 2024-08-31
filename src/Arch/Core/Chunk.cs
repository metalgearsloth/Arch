using System.Diagnostics.Contracts;
using System.Drawing;
using Arch.Core.Events;
using Arch.Core.Extensions;
using Arch.Core.Extensions.Internal;
using Arch.Core.Utils;
using CommunityToolkit.HighPerformance;

namespace Arch.Core;

/// <summary>
///     The <see cref="Chunk"/> struct represents a contiguous block of memory in which various components are stored in Structure of Arrays.
///     Chunks are internally allocated and filled by <see cref="Archetype"/>'s.
///     Through them it is possible to efficiently provide or trim memory for additional entities.
/// </summary>
[SkipLocalsInit]  // Really a speed improvements? The benchmark only showed a slight improvement
public partial struct Chunk
{

    /// <summary>
    ///     Initializes a new instance of the <see cref="Chunk"/> struct.
    ///     Automatically creates a lookup array for quick access to internal components.
    /// </summary>
    /// <param name="capacity">How many entities of the respective component structure fit into this <see cref="Chunk"/>.</param>
    /// <param name="types">The respective component structure of all entities in this <see cref="Chunk"/>.</param>
    internal Chunk(int capacity, Span<ComponentType> types)
        : this(capacity, types.ToLookupArray(), types) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Chunk"/> struct
    /// </summary>
    /// <param name="capacity">How many entities of the respective component structure fit into this <see cref="Chunk"/>.</param>
    /// <param name="componentIdToArrayIndex">A lookup array which maps the component id to the array index of the component array.</param>
    /// <param name="types">The respective component structure of all entities in this <see cref="Chunk"/>.</param>
    internal Chunk(int capacity, int[] componentIdToArrayIndex, Span<ComponentType> types)
    {
        // Calculate capacity and init arrays.
        Size = 0;
        Capacity = capacity;

        EntityReferences = new EntityReference[Capacity];
        Components = new Array[types.Length];

        // Init mapping.
        ComponentIdToArrayIndex = componentIdToArrayIndex;
        for (var index = 0; index < types.Length; index++)
        {
            var type = types[index];
            Components[index] = ArrayRegistry.GetArray(type, Capacity);
        }
    }

    public readonly EntityReference[] EntityReferences { [Pure] get; }

    /// <summary>
    ///     The component arrays in which the components of the <see cref="Arch.Core.Entity"/>'s are stored.
    ///     Represent the component structure.
    ///     They can be accessed quickly using the <see cref="ComponentIdToArrayIndex"/> or one of the chunk methods.
    /// </summary>
    public readonly Array[] Components { [Pure]  get; }

    /// <summary>
    ///     The lookup array that maps component ids to component array indexes to quickly access them.
    /// </summary>
    public readonly int[] ComponentIdToArrayIndex { [Pure]  get; }

    /// <summary>
    ///     The number of occupied <see cref="Arch.Core.Entity"/> slots in this <see cref="Chunk"/>.
    /// </summary>
    public int Size { [Pure]  get;  internal set; }

    /// <summary>
    ///     The number of possible <see cref="Arch.Core.Entity"/>'s in this <see cref="Chunk"/>.
    /// </summary>
    public int Capacity { [Pure]  get; }

    /// <summary>
    ///     Inserts an entity into the <see cref="Chunk"/>.
    ///     This won't fire an event for <see cref="EntityCreatedHandler"/>.
    /// </summary>
    /// <param name="entity">The <see cref="Arch.Core.EntityReference"/> that will be inserted.</param>
    /// <returns>The index occupied by the <see cref="Arch.Core.Entity"/> in the chunk.</returns>
    internal int Add(EntityReference entity)
    {
        // Stack variable faster than accessing 3 times the Size field.
        var size = Size;
        EntityReference(size) = entity;
        Size = size + 1;

        return size;
    }

    /// <summary>
    ///     Sets or replaces a component for an index in the chunk.
    ///     This won't fire an event for <see cref="ComponentSetHandler{T}"/>.
    /// </summary>
    /// <typeparam name="T">The generic type.</typeparam>
    /// <param name="index">The index in the array.</param>
    /// <param name="cmp">The component value.</param>
    public void Set<T>(int index, in T cmp)
    {
        ref var item = ref GetFirst<T>();
        Unsafe.Add(ref item, index) = cmp;
    }

    /// <summary>
    ///     Checks if a component is included in this <see cref="Chunk"/>.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <returns>True if included, false otherwise.</returns>
    [Pure]
    public bool Has<T>()
    {
        var id = Component<T>.ComponentType.Id;
        var idToArrayIndex = ComponentIdToArrayIndex;
        return id < idToArrayIndex.Length && idToArrayIndex.DangerousGetReferenceAt(id) != -1;
    }

    /// <summary>
    ///     Returns a component from an index within the <see cref="Chunk"/>.
    /// </summary>
    /// <typeparam name="T">The generic type.</typeparam>
    /// <param name="index">The index.</param>
    /// <returns>A reference to the component.</returns>
    [Pure]
    public ref T Get<T>(int index)
    {
        ref var item = ref GetFirst<T>();
        return ref Unsafe.Add(ref item, index);
    }

    /// <summary>
    ///     Returns a component at the index of the passed array.
    /// </summary>
    /// <typeparam name="T">The generic type.</typeparam>
    /// <param name="first">The first element of the array.</param>
    /// <param name="index">The index.</param>
    /// <returns>A reference to the component.</returns>
    [Pure]
    public ref T Get<T>(ref T first, int index)
    {
        return ref Unsafe.Add(ref first, index);
    }

    /// <summary>
    ///     Returns a component and <see cref="Arch.Core.Entity"/> from an index within the <see cref="Chunk"/>.
    /// </summary>
    /// <typeparam name="T">The generic type.</typeparam>
    /// <param name="index">The index.</param>
    /// <returns>A reference to the component.</returns>
    [Pure]
    public EntityComponents<T> GetRow<T>(int index)
    {
        var array = GetSpan<T>();
        return new EntityComponents<T>(ref EntityReferences[index], ref array[index]);
    }

    [Pure]
    public ref EntityReference EntityReference(int index)
    {
        return ref EntityReferences.DangerousGetReferenceAt(index);
    }

    /// <summary>
    ///     Removes the <see cref="Arch.Core.Entity"/> at an index with all its components.
    ///     Copies the last <see cref="Arch.Core.Entity"/> in its place to ensure a uniform array.
    ///     This won't fire an event for <see cref="ComponentRemovedHandler"/>.
    /// </summary>
    /// <param name="index">Its index.</param>

    internal void Remove(int index)
    {
        // Last entity in archetype.
        var lastIndex = Size - 1;

        // Copy last entity to replace the removed one.
        ref var entities = ref EntityReferences.DangerousGetReference();
        Unsafe.Add(ref entities, index) = Unsafe.Add(ref entities, lastIndex);  // entities[index] = entities[lastIndex]; but without bound checks

        // Copy components of last entity to replace the removed one
        var components = Components;
        for (var i = 0; i < components.Length; i++)
        {
            var array = components[i];
            Array.Copy(array, lastIndex, array, index, 1);
        }

        // Update the mapping.
        Size = lastIndex;
    }

    /// <summary>
    ///     Creates and returns a new <see cref="EntityEnumerator"/> instance to iterate over all used rows representing <see cref="Arch.Core.Entity"/>'s.
    /// </summary>
    /// <returns>A new <see cref="EntityEnumerator"/> instance.</returns>

    public EntityEnumerator GetEnumerator()
    {
        return new EntityEnumerator(Size);
    }

    /// <summary>
    ///     Cleares this <see cref="Chunk"/>, an efficient method to delete all <see cref="Arch.Core.Entity"/>s.
    ///     Does not dispose any resources nor modifies its <see cref="Capacity"/>.
    /// </summary>

    public void Clear()
    {
        Size = 0;
    }

    /// <summary>
    ///     Converts this <see cref="Chunk"/> to a human readable string.
    /// </summary>
    /// <returns>A string.</returns>

    public override string ToString()
    {
        return $"Chunk = {{ {nameof(Capacity)} = {Capacity}, {nameof(Size)} = {Size} }}";
    }
}

public partial struct Chunk
{

    /// <summary>
    ///     Returns the component array index of a component.
    /// </summary>
    /// <typeparam name="T">The componen type.</typeparam>
    /// <returns>The index in the <see cref="Components"/> array.</returns>
    [SkipLocalsInit]

    [Pure]
    private int Index<T>()
    {
        var id = Component<T>.ComponentType.Id;
        Debug.Assert(id != -1 && id < ComponentIdToArrayIndex.Length, $"Index is out of bounds, component {typeof(T)} with id {id} does not exist in this chunk.");
        return ComponentIdToArrayIndex.DangerousGetReferenceAt(id);
    }

    /// <summary>
    ///     Returns the component array for a given component in an unsafe manner.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <returns>The array.</returns>
    [SkipLocalsInit]

    [Pure]
    public T[] GetArray<T>()
    {
        var index = Index<T>();
        Debug.Assert(index != -1 && index < Components.Length, $"Index is out of bounds, component {typeof(T)} with id {index} does not exist in this chunk.");

        var array = Components.DangerousGetReferenceAt(index);
        return Unsafe.As<T[]>(array);
    }


    /// <summary>
    ///     Returns the component array <see cref="Span{T}"/> for a given component in an unsafe manner.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <returns>The array <see cref="Span{T}"/>.</returns>
    [SkipLocalsInit]

    [Pure]
    public Span<T> GetSpan<T>()
    {
        ref var item = ref GetFirst<T>();
        return MemoryMarshal.CreateSpan(ref item, Capacity);
    }

    /// <summary>
    ///     Returns a reference to the first element of a component from its component array in an unsafe manner.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <returns>A reference to the first element.</returns>
    [SkipLocalsInit]

    [Pure]
    public ref T GetFirst<T>()
    {
        return ref GetArray<T>().DangerousGetReference();
    }
}

public partial struct Chunk
{
    /// <summary>
    ///     Sets or replaces a component for an index in the chunk.
    ///     This won't fire an event for <see cref="ComponentSetHandler{T}"/>.
    /// </summary>
    /// <param name="index">The index in the array.</param>
    /// <param name="cmp">The component value.</param>
    public void Set(int index, object cmp)
    {
        var array = GetArray(cmp.GetType());
        array.SetValue(cmp, index);
    }

    /// <summary>
    ///     Checks if a component is included in this <see cref="Chunk"/>.
    /// </summary>
    /// <param name="t">The type.</param>
    /// <returns>True if included, false otherwise.</returns>
    [Pure]
    public bool Has(ComponentType t)
    {
        var id = t.Id;
        if (id >= ComponentIdToArrayIndex.Length)
        {
            return false;
        }

        return ComponentIdToArrayIndex.DangerousGetReferenceAt(id) != -1;
    }

    /// <summary>
    ///     Returns a component from an index within the <see cref="Chunk"/>.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="index">The index.</param>
    /// <returns>A component casted to an <see cref="object"/>.</returns>
    [Pure]
    public object? Get(int index, ComponentType type)
    {
        var array = GetArray(type);
        return array.GetValue(index);
    }

    /// <summary>
    ///     Returns the component array index of a component by its type.
    /// </summary>
    /// <param name="type">The <see cref="ComponentType"/>.</param>
    /// <returns>The index in the <see cref="Components"/> array.</returns>
    [Pure]
    private int Index(ComponentType type)
    {
        var id = type.Id;
        if (id >= ComponentIdToArrayIndex.Length)
        {
            return -1;
        }

        return ComponentIdToArrayIndex.DangerousGetReferenceAt(id);
    }

    /// <summary>
    ///      Returns the component array for a given component type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The <see cref="Array"/>.</returns>
    [Pure]
    public Array GetArray(ComponentType type)
    {
        var index = Index(type);
        return Components.DangerousGetReferenceAt(index);
    }
}

public partial struct Chunk
{

    /// <summary>
    ///  Copies the whole <see cref="Chunk"/> (with all its entities and components) or a part from it to the another <see cref="Chunk"/>.
    /// </summary>
    /// <param name="source">The source <see cref="Chunk"/>.</param>
    /// <param name="index">The start index in the source <see cref="Chunk"/>.</param>
    /// <param name="destination">The destination <see cref="Chunk"/>.</param>
    /// <param name="destinationIndex">The start index in the destination <see cref="Chunk"/>.</param>
    /// <param name="length">The length indicating the amount of <see cref="Entity"/>s being copied.</param>
    [Pure]
    internal static void Copy(ref Chunk source, int index, ref Chunk destination, int destinationIndex, int length)
    {
        // Arrays
        var entities = source.EntityReferences;
        var sourceComponents = source.Components;

        // Copy entities array
        Array.Copy(entities, index, destination.EntityReferences, destinationIndex, length);

        // Copy component arrays
        for (var i = 0; i < sourceComponents.Length; i++)
        {
            var sourceArray = sourceComponents[i];
            var sourceType = (ComponentType) sourceArray.GetType().GetElementType()!;

            if (!destination.Has(sourceType))
            {
                continue;
            }

            var destinationArray = destination.GetArray(sourceType);
            Array.Copy(sourceArray, index, destinationArray, destinationIndex, length);
        }
    }

    /// <summary>
    ///     Copies an <see cref="Arch.Core.Entity"/> components at one index to another <see cref="Chunk"/>-index.
    /// </summary>
    /// <param name="source">The source <see cref="Chunk"/>.</param>
    /// <param name="index">The start index in the source <see cref="Chunk"/>.</param>
    /// <param name="destination">The destination <see cref="Chunk"/>.</param>
    /// <param name="destinationIndex">The start index in the destination <see cref="Chunk"/>.</param>
    /// <param name="length">The length indicating the amount of <see cref="Entity"/>s being copied.</param>
    [Pure]
    internal static void CopyComponents(ref Chunk source, int index, ref Chunk destination, int destinationIndex, int length)
    {
        // Arrays
        var sourceComponents = source.Components;

        // Copy component arrays
        for (var i = 0; i < sourceComponents.Length; i++)
        {
            var sourceArray = sourceComponents[i];
            var sourceType = sourceArray.GetType().GetElementType();
            var compType = (ComponentType) sourceType!;

            if (!destination.Has(compType))
            {
                continue;
            }

            var destinationArray = destination.GetArray(compType);
            Array.Copy(sourceArray, index, destinationArray, destinationIndex, length);
        }
    }

    /// <summary>
    ///     Transfers the last <see cref="Arch.Core.Entity"/> of the referenced <see cref="Chunk"/> into this <see cref="Chunk"/> at the given index.
    /// </summary>
    /// <param name="index">The index of the <see cref="Arch.Core.Entity"/>.</param>
    /// <param name="chunk">The <see cref="Chunk"/> we want transfer the last <see cref="Arch.Core.Entity"/> from.</param>
    /// <returns></returns>
    [Pure]
    internal int Transfer(int index, ref Chunk chunk)
    {
        // Get last entity
        var lastIndex = chunk.Size - 1;
        var lastEntity = chunk.EntityReference(lastIndex);

        // Replace index entity with the last entity from the other chunk
        EntityReferences[index] = chunk.EntityReference(lastIndex);
        for (var i = 0; i < Components.Length; i++)
        {
            var sourceArray = chunk.Components[i];
            var desArray = Components[i];
            Array.Copy(sourceArray, lastIndex, desArray, index, 1);
        }

        chunk.Size--;
        return lastEntity.Entity.Id;
    }
}
