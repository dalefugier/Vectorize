using Grasshopper2.Framework;
using Grasshopper2.UI.Icon;
using System;

namespace VectorizeGh2
{
  /// <summary>
  /// Grasshopper 2 plug-in entry point. Exactly one Plugin-derived class is
  /// required; Grasshopper 2 discovers the component types (marked with [IoId])
  /// in this assembly automatically. The plug-in identity (id, name, description
  /// and version) is harvested from the assembly attributes.
  /// </summary>
  public sealed class VectorizeGh2Plugin : Plugin
  {
    /// <summary>
    /// Gets the plug-in logo. Resolved on demand rather than in the constructor:
    /// Grasshopper 2 constructs this type while harvesting the assembly, and an
    /// exception raised there costs the entire plug-in.
    /// </summary>
    public override IIcon Icon => IconArt.Leaf;
    public override string Author => "Robert McNeel & Associates";
    public override sealed string Copyright => $"Copyright © 2020-{DateTime.UtcNow.Year}, Robert McNeel & Associates";
    public override string Contact => "dale@mcneel.com";
    public override string Website => "https://www.food4rhino.com/en/app/vectorize";
    public override sealed string LicenceDescription => "GNU General Public License, version 2";
    public override sealed string LicenceAgreement => "GPL-2.0-or-later";
  }
}
