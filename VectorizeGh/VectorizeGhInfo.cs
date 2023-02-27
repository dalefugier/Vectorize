using System;
using System.Drawing;
using System.Reflection;
using Grasshopper.Kernel;

namespace VectorizeGh
{
  public class VectorizeGhInfo : GH_AssemblyInfo
  {
    // Return the plug-in name
    public override string Name => "VectorizeGh";

    // Return a 24x24 pixel bitmap to represent this GHA library.
    public override Bitmap Icon
    {
      get
      {
        const string resource = "VectorizeGh.Resources.VectorizeGh.ico";
        var size = new Size(24, 24);
        var assembly = Assembly.GetExecutingAssembly();
        var icon = Rhino.UI.DrawingUtilities.IconFromResource(resource, size, assembly);
        return icon.ToBitmap();
      }
    }

    // Return a short string describing the purpose of this GHA library.
    public override string Description => "Vectorize plug-in for Grasshopper";

    public override Guid Id => new Guid("d6178c4c-21b8-4421-9487-9065a631d9c2");

    // Return a string identifying you or your company.
    public override string AuthorName => "Robert McNeel & Associates";

    // Return a string representing your preferred contact details.
    public override string AuthorContact => "tech@mcneel.com";
  }
}