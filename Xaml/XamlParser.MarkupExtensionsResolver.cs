using System;

namespace Xaml
{
  public partial class XamlParser
  {
    private class MarkupExtensionsResolver : IMarkupExtensionsResolver
    {
      private readonly XamlParser _self;

      public MarkupExtensionsResolver(XamlParser self)
      {
        this._self = self;
      }

      public Type Resolve(string name)
      {
        return _self.ResolveMarkupExtensionType(name);
      }
    }
  }
}
