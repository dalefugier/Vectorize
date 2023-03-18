using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;
using System.Globalization;
using System.IO;
using VectorizeCommon;

namespace Vectorize
{
  /// <summary>
  /// Vectorize command
  /// </summary>
  public class VectorizeCommand : Command
  {
    // Constructor
    public VectorizeCommand()
    {
      Instance = this;
    }

    /// <summary>
    /// Get the one and only instance of the Vectorize command.
    /// </summary>
    public static VectorizeCommand Instance { get; private set; }

    /// <summary>
    /// Command.EnglishName override
    /// </summary>
    public override string EnglishName => "Vectorize";

    /// <summary>
    /// Command.RunCommand override
    /// </summary>
    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
      // Prompt the user for the name of the image file
      string path = GetImageFileName(mode);
      if (string.IsNullOrEmpty(path))
        return Result.Cancel;

      // Try readng image file
      System.Drawing.Bitmap systemBitmap;
      try
      {
        systemBitmap = System.Drawing.Image.FromFile(path) as System.Drawing.Bitmap;
        if (null == systemBitmap)
        {
          RhinoApp.WriteLine("The specified file cannot be identifed as a supported type.");
          return Result.Failure;
        }

        if (0 == systemBitmap.Width || 0 == systemBitmap.Height)
        {
          RhinoApp.WriteLine("Error reading the specified file.");
          return Result.Failure;
        }
      }
      catch
      {
        return Result.Failure;
      }

      // Calculate scale factor so curves of a reasonable size are added to Rhino
      var unitScale = (doc.ModelUnitSystem != UnitSystem.Inches)
        ? RhinoMath.UnitScale(UnitSystem.Inches, doc.ModelUnitSystem)
        : 1.0;

      var scale_x = (double)(1.0 / systemBitmap.HorizontalResolution * unitScale);
      var scale_y = (double)(1.0 / systemBitmap.VerticalResolution * unitScale);

      // I'm not convinced this is useful...
      if (true)
      {
        var format = $"F{doc.DistanceDisplayPrecision}";

        // Print image size in pixels
        RhinoApp.WriteLine("Image size in pixels: {0} x {1}", systemBitmap.Width, systemBitmap.Height);

        if (doc.ModelUnitSystem == UnitSystem.Inches)
        {
          // Print image size in inches
          var width = (double)(systemBitmap.Width / systemBitmap.HorizontalResolution);
          var height = (double)(systemBitmap.Height / systemBitmap.VerticalResolution);
          RhinoApp.WriteLine("Image size in inches: {0} x {1}",
            width.ToString(format, CultureInfo.InvariantCulture),
            height.ToString(format, CultureInfo.InvariantCulture)
          );
        }
        else
        {
          var width = (double)(systemBitmap.Width / systemBitmap.HorizontalResolution * unitScale);
          var height = (double)(systemBitmap.Height / systemBitmap.VerticalResolution * unitScale);
          RhinoApp.WriteLine("Image size in {0}: {1} x {2}",
            doc.ModelUnitSystem.ToString().ToLower(),
            width.ToString(format, CultureInfo.InvariantCulture),
            height.ToString(format, CultureInfo.InvariantCulture)
            );
        }
      }

      // Convert the bitmap to an Eto bitmap
      var etoBitmap = BitmapHelpers.ConvertBitmapToEto(systemBitmap);
      if (null == etoBitmap)
      {
        RhinoApp.WriteLine("Unable to convert image to Eto bitmap.");
        return Result.Failure;
      }

      if (!BitmapHelpers.IsCompatibleBitmap(etoBitmap))
      {
        var tempBitmap = BitmapHelpers.MakeCompatibleBitmap(etoBitmap);
        if (null == tempBitmap)
        {
          RhinoApp.WriteLine("The image has an incompatible pixel format. Please select an image with 24 or 32 bits per pixel, or 8 bit indexed.");
          return Result.Failure;
        }
        else
        {
          etoBitmap = tempBitmap;
        }
      }

      // This bitmap is not needed anymore, so dispose of it
      systemBitmap.Dispose();
      systemBitmap = null;

      // Get persistent settings
      var parameters = new PotraceParameters();
      parameters.GetSettings(Settings);

      // Create the conduit, which does most of the work
      var conduit = new VectorizeConduit(etoBitmap, parameters, scale_x, scale_y) { Enabled = true };

      if (mode == RunMode.Interactive)
      {
        // Show the interactive dialog box
        var dialog = new VectorizeDialog(doc, conduit);
        dialog.RestorePosition();
        var result = dialog.ShowSemiModal(doc, RhinoEtoApp.MainWindow);
        dialog.SavePosition();
        if (result != Result.Success)
        {
          conduit.Enabled = false;
          doc.Views.Redraw();
          return Result.Cancel;
        }
      }
      else
      {
        // Show the command line options
        var go = new GetOption();
        go.SetCommandPrompt("Vectorization options. Press Enter when done");
        go.AcceptNothing(true);
        while (true)
        {
          conduit.TraceBitmap();
          doc.Views.Redraw();

          go.ClearCommandOptions();

          // Threshold
          var optThreshold = new OptionDouble(parameters.Threshold, 0.0, 100.0);
          var idxThreshold = go.AddOptionDouble("Threshold", ref optThreshold, "Image brightness threshold");

          // Turdsize
          var optTurdSize = new OptionInteger(parameters.TurdSize, 0, 100);
          var idxTurdSize = go.AddOptionInteger("Speckles", ref optTurdSize, "Image despeckle threshold");

          // AlphaMax
          var optAlphaMax = new OptionDouble(parameters.AlphaMax, 0.0, 1.34);
          var idxAlphaMax = go.AddOptionDouble("Corners", ref optAlphaMax, "Corner detection threshold");

          // OptimizeTolerance
          var optOptimizeTolerance = new OptionDouble(parameters.OptimizeTolerance, 0.0, 1.0);
          var idxOptimizeTolerance = go.AddOptionDouble("Optimize", ref optOptimizeTolerance, "Curve simplification tolerance");

          // IncludeBorder
          var optIncludeBorder = new OptionToggle(parameters.IncludeBorder, "No", "Yes");
          var idxIncludeBorder = go.AddOptionToggle("IncludeBorder", ref optIncludeBorder);

          // RestoreDefaults
          var idxRestoreDefaults = go.AddOption("RestoreDefaults");

          var res = go.Get();

          if (res == GetResult.Option)
          {
            var option = go.Option();
            if (null != option)
            {
              // Threshold
              if (idxThreshold == option.Index)
              {
                parameters.Threshold = optThreshold.CurrentValue;
                continue;
              }

              // Turdsize
              if (idxTurdSize == option.Index)
              {
                parameters.TurdSize = optTurdSize.CurrentValue;
                continue;
              }

              // AlphaMax
              if (idxAlphaMax == option.Index)
              {
                parameters.AlphaMax = optAlphaMax.CurrentValue;
                continue;
              }

              // IncludeBorder
              if (idxIncludeBorder == option.Index)
              {
                parameters.IncludeBorder = optIncludeBorder.CurrentValue;
                continue;
              }

              // OptimizeTolerance
              if (idxOptimizeTolerance == option.Index)
              {
                parameters.OptimizeTolerance = optOptimizeTolerance.CurrentValue;
                continue;
              }

              // RestoreDefaults
              if (idxRestoreDefaults == option.Index)
              {
                parameters.SetDefaults();
                continue;
              }
            }
            continue;
          }

          if (res != GetResult.Nothing)
          {
            conduit.Enabled = false;
            doc.Views.Redraw();
            return Result.Cancel;
          }

          break;
        }
      }

      // Group curves
      var attributes = doc.CreateDefaultAttributes();
      attributes.AddToGroup(doc.Groups.Add());

      // Add curves to document
      for (var i = 0; i < conduit.Curves.Count; i++)
      {
        if (i == 0 && !parameters.IncludeBorder) // skip border
          continue;

        var objectId = doc.Objects.AddCurve(conduit.Curves[i], attributes);
        var rhinoObj = doc.Objects.Find(objectId);
        rhinoObj?.Select(true);
      }

      conduit.Enabled = false;
      doc.Views.Redraw();

      // Set persistent settings
      parameters.SetSettings(Settings);

      // Done!
      return Result.Success;
    }

    /// <summary>
    /// Get the name of an image file to trace.
    /// </summary>
    private string GetImageFileName(RunMode mode)
    {
      return (mode == RunMode.Interactive)
        ? GetImageFileNameInteractive()
        : GetImageFileNameScripted();
    }

    /// <summary>
    /// Get the name of an image file interactively.
    /// </summary>
    private string GetImageFileNameInteractive()
    {
      var dialog = new Eto.Forms.OpenFileDialog();

      string[] all = { ".bmp", ".gif", ".jpg", ".jpeg", ".png", ".tif", ".tiff" };
      dialog.Filters.Add(new Eto.Forms.FileFilter("All image files", all));

      dialog.Filters.Add(new Eto.Forms.FileFilter("Bitmap", ".bmp"));
      dialog.Filters.Add(new Eto.Forms.FileFilter("GIF", ".gif"));

      string[] jpeg = { ".jpg", ".jpe", ".jpeg" };
      dialog.Filters.Add(new Eto.Forms.FileFilter("JPEG", jpeg));
      dialog.Filters.Add(new Eto.Forms.FileFilter("PNG", ".png"));

      string[] tiff = { ".tif", ".tiff" };
      dialog.Filters.Add(new Eto.Forms.FileFilter("TIFF", tiff));

      var res = dialog.ShowDialog(RhinoEtoApp.MainWindow);
      if (res != Eto.Forms.DialogResult.Ok)
        return null;

      return dialog.FileName;
    }

    /// <summary>
    /// Get the name of an image file scripted.
    /// </summary>
    private string GetImageFileNameScripted()
    {
      var gs = new GetString();
      gs.SetCommandPrompt("Name of image file to open");
      gs.AddOption("Browse");
      var result = gs.Get();

      if (result == GetResult.Option)
        return GetImageFileNameInteractive();

      if (result != GetResult.String)
        return null;

      var path = gs.StringResult();
      if (!string.IsNullOrEmpty(path))
        path = path.Trim();

      if (string.IsNullOrEmpty(path))
        return null;

      if (!File.Exists(path))
      {
        RhinoApp.WriteLine("The specified file cannot be found.");
        return null;
      }

      return path;
    }
  }
}
