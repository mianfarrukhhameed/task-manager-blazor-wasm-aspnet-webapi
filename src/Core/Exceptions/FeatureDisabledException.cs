using System;

namespace Fistix.TaskManager.Core.Exceptions
{
  public class FeatureDisabledException : Exception
  {
    public FeatureDisabledException(string featureName)
      : base($"{featureName} is currently disabled.") { }
  }
}
