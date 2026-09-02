using Grasshopper2.UI.Icon;
using System;
using System.IO;
using System.Reflection;

namespace VectorizeGh2
{
  /// <summary>
  /// Loads the Vectorize maple leaf artwork, the same image the Rhino plug-in and
  /// the Grasshopper 1 component use. The leaf is Potrace's canonical example
  /// image, so it doubles as the plug-in logo and the component icon.
  /// </summary>
  internal static class IconArt
  {
    private static readonly Lazy<IIcon> g_icon = new Lazy<IIcon>(CreateIcon);

    /// <summary>
    /// Gets the shared maple leaf icon, or null if it cannot be created.
    /// Grasshopper 2 asks for icons repeatedly and at several sizes, so the
    /// bitmaps are decoded once, on first access, and reused. Never call this
    /// from a Plugin or Component constructor: Eto bitmaps need an initialized
    /// Eto platform, which is not guaranteed while Grasshopper 2 is harvesting
    /// types out of the assembly.
    /// </summary>
    public static IIcon Leaf => g_icon.Value;

    /// <summary>
    /// Builds a PixelIcon from the embedded artwork.
    /// </summary>
    /// <remarks>
    /// Only the largest image is handed over, even though 24, 64 and 128 pixel
    /// versions are embedded. PixelIcon's multi-bitmap constructor validates with
    /// an inverted comparison:
    ///
    ///   if (Math.Abs(ratio[j] - ratio[0]) &lt; 0.0001)
    ///     throw new ArgumentException("All bitmaps must have the same width/height ratio.");
    ///
    /// so it throws when the aspect ratios agree, which is the only case worth
    /// passing. The single-bitmap path returns before that check, and Grasshopper 2
    /// scales the 128 pixel artwork down cleanly. Once the check is fixed upstream
    /// this can pass all three and let Grasshopper 2 choose per size.
    /// </remarks>
    private static IIcon CreateIcon()
    {
      Eto.Drawing.Bitmap[] bitmaps = null;
      try
      {
        bitmaps = new Eto.Drawing.Bitmap[] { LoadBitmap("Vectorize_128x128.png") };
        return new PixelIcon(bitmaps);
      }
      catch (Exception ex)
      {
        // An unavailable icon is not worth failing the plug-in over, so report it
        // and carry on without one. Include the decoded sizes, since PixelIcon
        // rejects mismatched aspect ratios and the sizes are what explain that.
        string sizes = (null == bitmaps)
          ? "not decoded"
          : string.Join(", ", Array.ConvertAll(bitmaps, b => null == b ? "null" : $"{b.Width}x{b.Height}"));
        Rhino.RhinoApp.WriteLine($"VectorizeGh2: unable to create icon. {ex.Message} (sizes: {sizes})");
        return null;
      }
    }

    /// <summary>
    /// Reads one embedded PNG into an Eto bitmap. The resource is copied into a
    /// byte array first: Eto may decode lazily, so handing it a stream that is
    /// then disposed can yield a zero-sized bitmap rather than an error.
    /// </summary>
    private static Eto.Drawing.Bitmap LoadBitmap(string name)
    {
      Assembly assembly = typeof(IconArt).Assembly;
      string path = $"VectorizeGh2.Resources.{name}";
      using (Stream stream = assembly.GetManifestResourceStream(path))
      {
        if (null == stream)
          throw new InvalidOperationException($"Embedded resource not found: {path}");

        using (MemoryStream buffer = new MemoryStream())
        {
          stream.CopyTo(buffer);
          return new Eto.Drawing.Bitmap(buffer.ToArray());
        }
      }
    }
  }
}
