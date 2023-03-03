using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using VectorizeCommon;

namespace VectorizeGh
{
  public class VectorizeGhComponent : GH_Component
  {
    private int idxPath = -1;
    private int idxThreshold = -1;
    //private int idxTurnPolicy = -1;
    private int idxTurdSize = -1;
    private int idxAlphaMax = -1;
    //private int idxOptimizeCurve = -1;
    private int idxOptimizeTolerance = -1;
    private int idxIncludeBorder = -1;
    private int idxCurves = -1;

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

      idxPath = pManager.AddTextParameter("Path", "P", "Image file path", GH_ParamAccess.item);
      idxThreshold = pManager.AddNumberParameter("Threshold", "T", "Image brightness threshold, from 0.0 (black) to 1.0 (white).", GH_ParamAccess.item, args.Threshold);
      //idxTurnPolicy = pManager.AddIntegerParameter("TurnPolicy", "Turn", "Algorithm use to resolve ambiguities during decomposition of bitmaps. Range is from 0 to 6.", GH_ParamAccess.item, (int)args.TurnPolicy);
      idxTurdSize = pManager.AddIntegerParameter("Speckles", "S", "Image despeckle threshold, from 0 to 100.", GH_ParamAccess.item, args.TurdSize);
      idxAlphaMax = pManager.AddNumberParameter("Corners", "C", "Corner detection threshold, from 0.0 (polygons) to 1.34 (no corner).", GH_ParamAccess.item, args.AlphaMax);
      //idxOptimizeCurve = pManager.AddBooleanParameter("OptimizeCurve", "Optimize", "Enables curve optimizing", GH_ParamAccess.item, args.OptimizeCurve);
      idxOptimizeTolerance = pManager.AddNumberParameter("Optimize", "O", "Optimize paths by replacing sequences of Bézier segments with single segments. Range is from 0.0 to 1.0.", GH_ParamAccess.item, args.OptimizeTolerance);
      idxIncludeBorder = pManager.AddBooleanParameter("Border", "B", "Include border curve.", GH_ParamAccess.item, args.IncludeBorder);

      pManager[idxThreshold].Optional = true;
      //pManager[idxTurnPolicy].Optional = true;
      pManager[idxTurdSize].Optional = true;
      pManager[idxAlphaMax].Optional = true;
      //pManager[idxOptimizeCurve].Optional = true;
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

      // Validate path
      if (!string.IsNullOrEmpty(path))
        path = path.Trim();

      if (string.IsNullOrEmpty(path))
        return;

      if (!File.Exists(path))
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The specified file cannot be found.");
        return;
      }

      // Creates a bitmap from the specified file.
      var bitmap = Image.FromFile(path) as Bitmap;
      if (null == bitmap)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The specified file cannot be identifed as a supported type.");
        return;
      }

      // Verify bitmap size     
      if (0 == bitmap.Width || 0 == bitmap.Height)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Error reading the specified file..");
        return;
      }

      // Calculate scale factor so curves of a reasonable size are added to Rhino
      var doc = RhinoDoc.ActiveDoc;
      if (null == doc)
        return;

      var unit_scale = (doc.ModelUnitSystem != UnitSystem.Inches)
        ? RhinoMath.UnitScale(UnitSystem.Inches, doc.ModelUnitSystem)
        : 1.0;

      var scale = (double)(1.0 / bitmap.HorizontalResolution * unit_scale);

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

      // TurnPolicy
      //var turnPolicy = (int)args.TurnPolicy;
      //if (DA.GetData(idxTurnPolicy, ref turnPolicy))
      //{
      //  if (turnPolicy < 0 || turnPolicy > 6)
      //  {
      //    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "TurnPolicy range is from 0 to 6.");
      //    return;
      //  }
      //  args.TurnPolicy = (PotraceTurnPolicy)turnPolicy;
      //}

      // TurdSize
      var turdSize = args.TurdSize;
      if (DA.GetData(idxTurdSize, ref turdSize))
      {
        if (turdSize < 0 || turdSize > 100)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "FilterSize range is from 0 to 100.");
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
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "CornerRounding range is from 0.0 to 1.34.");
          return;
        }
        args.AlphaMax = alphaMax;
      }

      // OptimizeCurve
      //var optimizeCurve = args.OptimizeCurve;
      //if (DA.GetData(idxOptimizeCurve, ref optimizeCurve))
      //{
      //  args.OptimizeCurve = optimizeCurve;
      //}

      // OptimizeTolerance
      var optimizeTolerance = args.OptimizeTolerance;
      if (DA.GetData(idxOptimizeTolerance, ref optimizeTolerance))
      {
        if (optimizeTolerance < 0.0 || optimizeTolerance > 1.0)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "OptimizeTolerance range is from 0.0 to 1.0.");
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
      var eto_bitmap = ConvertBitmapToEto(bitmap);
      if (null == eto_bitmap)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unable to convert image to Eto bitmap.");
        return;
      }

      // This should prevent Eto.Drawing.BitmapData.GetPixels() from throwing an exception
      if (!IsCompatibleBitmap(eto_bitmap))
      {
        var temp_bitmap = MakeCompatibleBitmap(eto_bitmap);
        if (null == temp_bitmap)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Image has an incompatible pixel format.");
          return;
        }
        else
        {
          eto_bitmap = temp_bitmap;
        }
      }

      // This bitmap is not needed anymore, so dispose of it
      bitmap.Dispose();

      //////////////////////////////////////////////////////////
      // Create Potrace bitmap
      var potraceBitmap = new PotraceBitmap(eto_bitmap, args.Threshold);

      // Trace the bitmap
      var potrace = Potrace.Trace(potraceBitmap, args);
      if (null == potrace)
        return;

      var curves = new List<Curve>();

      if (args.IncludeBorder)
      {
        var corners = new Point3d[] {
          Point3d.Origin,
          new Point3d(eto_bitmap.Width, 0.0, 0.0),
          new Point3d(eto_bitmap.Width, eto_bitmap.Height, 0.0),
          new Point3d(0.0, eto_bitmap.Height, 0.0),
          Point3d.Origin
        };

        var border = new PolylineCurve(corners);
        curves.Add(border);
      }

      // Harvest the Potrace path curves
      var potracePath = potrace.Path;
      while (null != potracePath)
      {
        var curve = potracePath.Curve;
        if (null != curve)
          curves.Add(curve);
        potracePath = potracePath.Next;
      }

      if (curves.Count > 0)
      {
        // Scale the output, per the calculation made in the command.
        if (scale != 1.0)
        {
          var xform = Transform.Scale(Point3d.Origin, scale);
          for (var i = 0; i < curves.Count; i++)
            curves[i].Transform(xform);
        }
      }

      DA.SetDataList(idxCurves, curves);

      GC.KeepAlive(potrace);
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override System.Drawing.Bitmap Icon => Properties.Resources.Vectorize_24x24;

    public override Guid ComponentGuid => new Guid("9890d4c6-ace2-4c99-b5aa-a71bd385f451");

    /// <summary>
    /// Convert a System.Drawing.Bitmap to a Eto.Drawing.Bitmap
    /// </summary>
    private Eto.Drawing.Bitmap ConvertBitmapToEto(Bitmap bitmap)
    {
      if (null == bitmap)
        return null;

      using (var stream = new MemoryStream())
      {
        bitmap.Save(stream, ImageFormat.Png);
        stream.Seek(0, SeekOrigin.Begin);
        var eto_bitmap = new Eto.Drawing.Bitmap(stream);
        return eto_bitmap;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="bitmap"></param>
    /// <returns></returns>
    private bool IsCompatibleBitmap(Eto.Drawing.Bitmap bitmap)
    {
      if (null == bitmap)
        return false;

      using (var bitmapData = bitmap.Lock())
      {
        if (bitmapData.BytesPerPixel == 4)
          return true;

        if (bitmapData.BytesPerPixel == 3)
          return true;

        if (bitmapData.Image is Eto.Drawing.IndexedBitmap && bitmapData.BytesPerPixel == 1)
          return true;
      }

      return false;
    }

    /// <summary>
    /// Makes an Eto BitmapData.GetPixel-compatible bitmap
    /// </summary>
    private Eto.Drawing.Bitmap MakeCompatibleBitmap(Eto.Drawing.Bitmap bitmap)
    {
      if (null == bitmap)
        return null;

      var size = new Eto.Drawing.Size(bitmap.Width, bitmap.Height);
      var bmp = new Eto.Drawing.Bitmap(size, Eto.Drawing.PixelFormat.Format24bppRgb);
      using (var graphics = new Eto.Drawing.Graphics(bmp))
      {
        graphics.DrawImage(bitmap, 0, 0);
      }
      return bmp;
    }
  }
}