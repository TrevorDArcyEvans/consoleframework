using System;

namespace Xaml
{
  public interface IMarkupExtensionsResolver
  {
    Type Resolve(String name);
  }
}
