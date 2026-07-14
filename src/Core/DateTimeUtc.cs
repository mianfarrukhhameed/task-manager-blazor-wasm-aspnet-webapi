using System;

namespace Fistix.TaskManager.Core;

/// <summary>
/// Helpers for Npgsql timestamptz, which only accepts UTC DateTime values.
/// </summary>
public static class DateTimeUtc
{
  /// <summary>
  /// Converts a value to UTC. Unspecified Kind (e.g. Blazor InputDate) is treated as UTC.
  /// </summary>
  public static DateTime EnsureUtc(DateTime value) =>
    value.Kind switch
    {
      DateTimeKind.Utc => value,
      DateTimeKind.Local => value.ToUniversalTime(),
      _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

  public static DateTime? EnsureUtc(DateTime? value) =>
    value.HasValue ? EnsureUtc(value.Value) : null;
}
