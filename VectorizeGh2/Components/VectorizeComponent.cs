using Grasshopper2.Components;
using Grasshopper2.UI;
using Grasshopper2.UI.Icon;
using GrasshopperIO;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using VectorizeCommon;

namespace VectorizeGh2.Components
{
  [IoId("1109cdf1-f754-489a-8bde-e63a824a4fe5"), Grasshopper2.Author("Robert McNeel & Associates")]
  public sealed class VectorizeComponent : Component
  {
    /// <summary>
    /// Initializes a new instance of the VectorizeComponent class.
    /// </summary>
    public VectorizeComponent()
      : base(new Nomen("Vectorize", "Vectorize, or trace, a bitmap.", "Curve", "Util", 0, Rank.Normal))
    {
    }

    /// <summary>
    /// Deserialization constructor.
    /// </summary>
    public VectorizeComponent(IReader reader) : base(reader)
    {
    }

    protected override void AddInputs(InputAdder inputs)
    {
      PotraceParameters args = new PotraceParameters();

      inputs.AddText(PotraceStrings.PathLabel, "P", PotraceStrings.PathTooltip).Set(string.Empty);
      inputs.AddNumber(PotraceStrings.ThresholdLabel(false), "T", PotraceStrings.ThresholdTooltip(true)).Set(args.Threshold);
      inputs.AddInteger(PotraceStrings.TurdSizeLabel(false), "S", PotraceStrings.TurdSizeTooltip(true)).Set(args.TurdSize);
      inputs.AddNumber(PotraceStrings.AlphaMaxLabel(false), "C", PotraceStrings.AlphaMaxTooltip(true)).Set(args.AlphaMax);
      inputs.AddNumber(PotraceStrings.OptimizeToleranceLabel(false), "O", PotraceStrings.OptimizeToleranceTooltip(true)).Set(args.OptimizeTolerance);
      inputs.AddBoolean(PotraceStrings.IncludeBorderLabel(false), "B", PotraceStrings.IncludeBorderTooltip).Set(args.IncludeBorder);
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
      outputs.AddCurve("Curves", "Crvs", "Output curves", Grasshopper2.Parameters.Access.Twig);
    }

    protected override void Process(IDataAccess access)
    {
      //////////////////////////////////////////////////////////
      // Get bitmap

      access.GetItem(0, out string path);

      // Validate path string
      if (!string.IsNullOrEmpty(path))
        path = path.Trim();

      if (string.IsNullOrEmpty(path))
        return;

      // Validate path
      if (!File.Exists(path))
      {
        access.AddError("File not found", "The specified file cannot be found.");
        return;
      }

      System.Drawing.Bitmap systemBitmap;
      try
      {
        // Creates a bitmap from the specified file.
        systemBitmap = System.Drawing.Image.FromFile(path) as System.Drawing.Bitmap;
        if (null == systemBitmap)
        {
          access.AddError("Unsupported file type", "The specified file cannot be identifed as a supported type.");
          return;
        }

        // Verify bitmap size
        if (0 == systemBitmap.Width || 0 == systemBitmap.Height)
        {
          access.AddError("File read error", "Error reading the specified file.");
          return;
        }
      }
      catch (Exception ex)
      {
        access.AddError("File read error", ex.Message);
        return;
      }

      // Calculate scale factor so curves of a reasonable size are added to Rhino
      RhinoDoc doc = RhinoDoc.ActiveDoc;
      if (null == doc)
      {
        systemBitmap.Dispose();
        return;
      }

      double unit_scale = (doc.ModelUnitSystem != UnitSystem.Inches)
        ? RhinoMath.UnitScale(UnitSystem.Inches, doc.ModelUnitSystem)
        : 1.0;

      double scale = (double)(1.0 / systemBitmap.HorizontalResolution * unit_scale);

      //////////////////////////////////////////////////////////
      // Get properties

      PotraceParameters args = new PotraceParameters();

      // Threshold
      access.GetItem(1, out double threshold);
      if (threshold < 0.0 || threshold > 1.0)
      {
        access.AddError("Threshold out of range", "Threshold range is from 0.0 to 1.0.");
        systemBitmap.Dispose();
        return;
      }
      args.Threshold = threshold;

      // TurdSize
      access.GetItem(2, out int turdSize);
      if (turdSize < 0 || turdSize > 100)
      {
        access.AddError("Speckles out of range", "Speckles range is from 0 to 100.");
        systemBitmap.Dispose();
        return;
      }
      args.TurdSize = turdSize;

      // AlphaMax
      access.GetItem(3, out double alphaMax);
      if (alphaMax < 0.0 || alphaMax > 1.34)
      {
        access.AddError("Corners out of range", "Corners range is from 0.0 to 1.34.");
        systemBitmap.Dispose();
        return;
      }
      args.AlphaMax = alphaMax;

      // OptimizeTolerance
      access.GetItem(4, out double optimizeTolerance);
      if (optimizeTolerance < 0.0 || optimizeTolerance > 1.0)
      {
        access.AddError("Tolerance out of range", "Optimize range is from 0.0 to 1.0.");
        systemBitmap.Dispose();
        return;
      }
      args.OptimizeTolerance = optimizeTolerance;

      // IncludeBorder
      access.GetItem(5, out bool includeBorder);
      args.IncludeBorder = includeBorder;

      //////////////////////////////////////////////////////////
      // Convert the bitmap to an Eto bitmap

      Eto.Drawing.Bitmap etoBitmap = BitmapHelpers.ConvertBitmapToEto(systemBitmap);
      if (null == etoBitmap)
      {
        access.AddError("Bitmap conversion error", "Unable to convert image to Eto bitmap.");
        systemBitmap.Dispose();
        return;
      }

      if (!BitmapHelpers.IsCompatibleBitmap(etoBitmap))
      {
        Eto.Drawing.Bitmap tempBitmap = BitmapHelpers.MakeCompatibleBitmap(etoBitmap);
        if (null == tempBitmap)
        {
          access.AddError("Incompatible pixel format", "Image has an incompatible pixel format.");
          systemBitmap.Dispose();
          return;
        }
        else
        {
          etoBitmap = tempBitmap;
        }
      }

      // This bitmap is not needed anymore, so dispose of it
      systemBitmap.Dispose();

      //////////////////////////////////////////////////////////
      // Create Potrace bitmap

      PotraceBitmap potraceBitmap = new PotraceBitmap(etoBitmap, args.Threshold);

      //////////////////////////////////////////////////////////
      // Trace the bitmap

      Potrace potrace = Potrace.Trace(potraceBitmap, args);
      if (null == potrace)
      {
        access.AddError("Trace error", "Unable to trace image.");
        return;
      }

      //////////////////////////////////////////////////////////
      // Get results
      List<Curve> outCurves = new List<Curve>();

      // Create the border curve if needed
      if (args.IncludeBorder)
      {
        Point3d[] corners = new Point3d[] {
          Point3d.Origin,
          new Point3d(etoBitmap.Width, 0.0, 0.0),
          new Point3d(etoBitmap.Width, etoBitmap.Height, 0.0),
          new Point3d(0.0, etoBitmap.Height, 0.0),
          Point3d.Origin
        };

        PolylineCurve border = new PolylineCurve(corners);
        outCurves.Add(border);
      }

      // Harvest the Potrace path curves
      PotracePath potracePath = potrace.Path;
      while (null != potracePath)
      {
        Curve curve = potracePath.Curve;
        if (null != curve)
          outCurves.Add(curve);
        potracePath = potracePath.Next;
      }

      // Scale the output, per the calculation made above
      if (outCurves.Count > 0 && scale != 1.0)
      {
        Transform xform = Transform.Scale(Point3d.Origin, scale);
        for (int i = 0; i < outCurves.Count; i++)
          outCurves[i].Transform(xform);
      }

      // Return curves
      access.SetTwig(0, outCurves.ToArray());

      GC.KeepAlive(potrace);

      // Done!
    }

    protected override IIcon IconInternal => IconArt.Leaf;
  }
}
