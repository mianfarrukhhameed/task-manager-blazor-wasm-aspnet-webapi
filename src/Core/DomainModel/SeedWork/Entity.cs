using System;
using System.Collections.Generic;
using System.Text;

namespace Fistix.TaskManager.Core.DomainModel.SeedWork
{
  public class Entity
  {
    public int Id { get; protected set; }
    public Guid ExternalId { get; protected set; }

    public void GenerateNewExternalId()
    {
      ExternalId = Guid.NewGuid();
    }
  }
}
