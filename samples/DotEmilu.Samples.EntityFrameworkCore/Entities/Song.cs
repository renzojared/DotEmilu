using System.ComponentModel;
using DotEmilu.Abstractions;

namespace DotEmilu.Samples.EntityFrameworkCore.Entities;

/// <summary>
/// Represents a song entity with a typed enum property.
/// Demonstrates <see cref="BaseEntity{TKey}"/> and enum-based columns
/// for use with <c>HasFormattedComment()</c>.
/// </summary>
public class Song : BaseEntity<Guid>
{
    public required string Name { get; set; }
    public SongType Type { get; set; }
}

/// <summary>
/// Song genre classification.
/// </summary>
[Description("Song genres")]
public enum SongType
{
    Salsa,
    [Description("Rock & Roll")] Rock,
    Jazz
}
