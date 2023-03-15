using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using VectorizeCommon;

namespace VectorizeGh
{
  public class VectorizeGhComponent : GH_Component
  {
    // Input parameters
    private int idxPath = -1;
    private int idxThreshold = -1;
    private int idxTurdSize = -1;
    private int idxAlphaMax = -1;
    private int idxOptimizeTolerance = -1;
    private int idxIncludeBorder = -1;
    // Output parameters
    private int idxCurves = -1;

    /// <summary>
    /// Constructor
    /// </summary>
    public VectorizeGhComponent()
      : base("Vectorize", "Vectorize", "Vectorize, or trace, a bitmap.", "Curve", "Util")
    {
    }

    /// <summary>
    /// Register inputs
    /// </summary>
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      var args = new PotraceParameters();

      idxPath = pManager.AddTextParameter("Path", "P", PotraceTooltips.Path, GH_ParamAccess.item);
      idxThreshold = pManager.AddNumberParameter("Threshold", "T", PotraceTooltips.Threshold, GH_ParamAccess.item, args.Threshold);
      idxTurdSize = pManager.AddIntegerParameter("Speckles", "S", PotraceTooltips.TurdSize, GH_ParamAccess.item, args.TurdSize);
      idxAlphaMax = pManager.AddNumberParameter("Corners", "C", PotraceTooltips.AlphaMax, GH_ParamAccess.item, args.AlphaMax);
      idxOptimizeTolerance = pManager.AddNumberParameter("Optimize", "O", PotraceTooltips.OptimizeTolerance, GH_ParamAccess.item, args.OptimizeTolerance);
      idxIncludeBorder = pManager.AddBooleanParameter("Border", "B", PotraceTooltips.IncludeBorder, GH_ParamAccess.item, args.IncludeBorder);

      // All but the path parameter are optional
      pManager[idxThreshold].Optional = true;
      pManager[idxTurdSize].Optional = true;
      pManager[idxAlphaMax].Optional = true;
      pManager[idxOptimizeTolerance].Optional = true;
      pManager[idxIncludeBorder].Optional = true;
    }

    /// <summary>
    /// Register outputs
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      idxCurves = pManager.AddCurveParameter("Curves", "Crvs", "Output curves", GH_ParamAccess.list);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      //////////////////////////////////////////////////////////
      // Get bitmap

      string path = null;
      if (!DA.GetData(idxPath, ref path))
        return;

      // Validate path string
      if (!string.IsNullOrEmpty(path))
        path = path.Trim();

      if (string.IsNullOrEmpty(path))
        return;

      // Validate path
      if (!File.Exists(path))
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The specified file cannot be found.");
        return;
      }

      System.Drawing.Bitmap systemBitmap;
      try
      {
        // Creates a bitmap from the specified file.
        systemBitmap = System.Drawing.Image.FromFile(path) as System.Drawing.Bitmap;
        if (null == systemBitmap)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The specified file cannot be identifed as a supported type.");
          return;
        }

        // Verify bitmap size     
        if (0 == systemBitmap.Width || 0 == systemBitmap.Height)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Error reading the specified file.");
          return;
        }
      }
      catch (Exception ex) 
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        return;
      }

      // Calculate scale factor so curves of a reasonable size are added to Rhino
      var doc = RhinoDoc.ActiveDoc;
      if (null == doc)
        return;

      var unit_scale = (doc.ModelUnitSystem != UnitSystem.Inches)
        ? RhinoMath.UnitScale(UnitSystem.Inches, doc.ModelUnitSystem)
        : 1.0;

      var scale = (double)(1.0 / systemBitmap.HorizontalResolution * unit_scale);

      //////////////////////////////////////////////////////////
      // Get properties

      var args = new PotraceParameters();

      // Threshold
      var threshold = args.Threshold;
      if (DA.GetData(idxThreshold, ref threshold))
      {
        if (threshold < 0.0 || threshold > 1.0)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Threshold range is from 0.0 to 1.0.");
          return;
        }
        args.Threshold = threshold;
      }

      // TurdSize
      var turdSize = args.TurdSize;
      if (DA.GetData(idxTurdSize, ref turdSize))
      {
        if (turdSize < 0 || turdSize > 100)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Speckles range is from 0 to 100.");
          return;
        }
        args.TurdSize = turdSize;
      }

      // AlphaMax
      var alphaMax = args.AlphaMax;
      if (DA.GetData(idxAlphaMax, ref alphaMax))
      {
        if (alphaMax < 0.0 || alphaMax > 1.34)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Corners range is from 0.0 to 1.34.");
          return;
        }
        args.AlphaMax = alphaMax;
      }

      // OptimizeTolerance
      var optimizeTolerance = args.OptimizeTolerance;
      if (DA.GetData(idxOptimizeTolerance, ref optimizeTolerance))
      {
        if (optimizeTolerance < 0.0 || optimizeTolerance > 1.0)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Optimize range is from 0.0 to 1.0.");
          return;
        }
        args.OptimizeTolerance = optimizeTolerance;
      }

      // IncludeBorder
      var includeBorder = args.IncludeBorder;
      if (DA.GetData(idxIncludeBorder, ref includeBorder))
      {
        args.IncludeBorder = includeBorder;
      }

      //////////////////////////////////////////////////////////
      // Convert the bitmap to an Eto bitmap

      var etoBitmap = BitmapHelpers.ConvertBitmapToEto(systemBitmap);
      if (null == etoBitmap)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unable to convert image to Eto bitmap.");
        return;
      }

      if (!BitmapHelpers.IsCompatibleBitmap(etoBitmap))
      {
        var tempBitmap = BitmapHelpers.MakeCompatibleBitmap(etoBitmap);
        if (null == tempBitmap)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Image has an incompatible pixel format.");
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

      var potraceBitmap = new PotraceBitmap(etoBitmap, args.Threshold);

      //////////////////////////////////////////////////////////
      // Trace the bitmap

      var potrace = Potrace.Trace(potraceBitmap, args);
      if (null == potrace)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unable to trace iamge.");
        return;
      }

      //////////////////////////////////////////////////////////
      // Get results
      var outCurves = new List<Curve>();

      // Create the border curve if needed
      if (args.IncludeBorder)
      {
        var corners = new Point3d[] {
          Point3d.Origin,
          new Point3d(etoBitmap.Width, 0.0, 0.0),
          new Point3d(etoBitmap.Width, etoBitmap.Height, 0.0),
          new Point3d(0.0, etoBitmap.Height, 0.0),
          Point3d.Origin
        };

        var border = new PolylineCurve(corners);
        outCurves.Add(border);
      }

      // Harvest the Potrace path curves
      var potracePath = potrace.Path;
      while (null != potracePath)
      {
        var curve = potracePath.Curve;
        if (null != curve)
          outCurves.Add(curve);
        potracePath = potracePath.Next;
      }

      // Scale the output, per the calculation made above
      if (outCurves.Count > 0 && scale != 1.0)
      {
        var xform = Transform.Scale(Point3d.Origin, scale);
        for (var i = 0; i < outCurves.Count; i++)
          outCurves[i].Transform(xform);
      }

      // Return curves
      DA.SetDataList(idxCurves, outCurves);

      GC.KeepAlive(potrace);

      // Done!
    }

    /// <summary>
    /// Gets the component's exposure
    /// </summary>
    public override GH_Exposure Exposure => GH_Exposure.primary;

    /// <summary>
    /// Gets the component's icon.
    /// </summary>
    protected override System.Drawing.Bitmap Icon => Properties.Resources.Vectorize_24x24;

    /// <summary>
    /// Gets the component's ID
    /// </summary>
    public override Guid ComponentGuid => new Guid("9890d4c6-ace2-4c99-b5aa-a71bd385f451");
  }
}