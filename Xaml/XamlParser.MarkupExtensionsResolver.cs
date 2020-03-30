using System;

namespace Xaml
{
  public partial class XamlParser
  {
    private class MarkupExtensionsResolver : IMarkupExtensionsResolver
    {
      private readonly XamlParser self;

      public MarkupExtensionsResolver(XamlParser self)
      {
        this.self = self;
      }

      public Type Resolve(string name)
      {
        return self.ResolveMarkupExtensionType(name);
      }
    }
  }
}
