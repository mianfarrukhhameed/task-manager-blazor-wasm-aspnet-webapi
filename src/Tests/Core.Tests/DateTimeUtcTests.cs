using System;
using Fistix.TaskManager.Core;
using Xunit;

namespace Fistix.TaskManager.Core.Tests;

public class DateTimeUtcTests
{
  [Fact]
  public void EnsureUtc_MarksUnspecifiedAsUtc()
  {
    var input = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Unspecified);
    var result = DateTimeUtc.EnsureUtc(input);

    Assert.Equal(DateTimeKind.Utc, result.Kind);
    Assert.Equal(input.Ticks, result.Ticks);
  }

  [Fact]
  public void EnsureUtc_PreservesUtc()
  {
    var input = DateTime.UtcNow;
    var result = DateTimeUtc.EnsureUtc(input);

    Assert.Equal(DateTimeKind.Utc, result.Kind);
    Assert.Equal(input, result);
  }
}
