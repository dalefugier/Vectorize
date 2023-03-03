using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;
using System.Drawing;
using System.Drawing.Imaging;
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
    /// <summary>
    /// Command.EnglishName override
    /// </summary>
    public override string EnglishName => LOC.COMMANDNAME("Vectorize");

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
      Bitmap bitmap;
      try
      {
        bitmap = Image.FromFile(path) as Bitmap;
        if (null == bitmap)
        {
          RhinoApp.WriteLine("The specified file cannot be identifed as a supported type.");
          return Result.Failure;
        }

        if (0 == bitmap.Width || 0 == bitmap.Height)
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

      var scale_x = (double)(1.0 / bitmap.HorizontalResolution * unitScale);
      var scale_y= (double)(1.0 / bitmap.VerticalResolution * unitScale);

      // I'm not convinced this is useful...
      if (true)
      {
        var format = $"F{doc.DistanceDisplayPrecision}";

        // Print image size in pixels
        RhinoApp.WriteLine("Image size in pixels: {0} x {1}", bitmap.Width, bitmap.Height);

        if (doc.ModelUnitSystem == UnitSystem.Inches)
        {
          // Print image size in inches
          var width = (double)(bitmap.Width / bitmap.HorizontalResolution);
          var height = (double)(bitmap.Height / bitmap.VerticalResolution);
          RhinoApp.WriteLine("Image size in inches: {0} x {1}",
            width.ToString(format, CultureInfo.InvariantCulture),
            height.ToString(format, CultureInfo.InvariantCulture)
          );
        }
        else
        {
          var width = (double)(bitmap.Width / bitmap.HorizontalResolution * unitScale);
          var height = (double)(bitmap.Height / bitmap.VerticalResolution * unitScale);
          RhinoApp.WriteLine("Image size in {0}: {1} x {2}",
            doc.ModelUnitSystem.ToString().ToLower(),
            width.ToString(format, CultureInfo.InvariantCulture),
            height.ToString(format, CultureInfo.InvariantCulture)
            );
        }
      }

      // Convert the bitmap to an Eto bitmap
      var eto_bitmap = ConvertBitmapToEto(bitmap);
      if (null == eto_bitmap)
      {
        RhinoApp.WriteLine("Unable to convert image to Eto bitmap.");
        return Result.Failure;
      }

      // This should prevent Eto.Drawing.BitmapData.GetPixels() from throwing an exception
      if (!IsCompatibleBitmap(eto_bitmap))
      {
        var temp_bitmap = MakeCompatibleBitmap(eto_bitmap);
        if (null == temp_bitmap)
        {
          RhinoApp.WriteLine("The image has an incompatible pixel format. Please select an image with 24 or 32 bits per pixel, or 8 bit indexed.");
          return Result.Failure;
        }
        else
        {
          eto_bitmap = temp_bitmap;
        }
      }

      // This bitmap is not needed anymore, so dispose of it
      bitmap.Dispose();

      // Get persistent settings
      var parameters = new PotraceParameters();
      parameters.GetSettings(Settings);

      // Create the conduit, which does most of the work
      var conduit = new VectorizeConduit(eto_bitmap, parameters, scale_x, scale_y) { Enabled = true };

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

          // TurnPolicy
          //var idxTurnPolicy = go.AddOptionEnumList("TurnPolicy"), parameters.TurnPolicy);

          // Turdsize
          var optTurdSize = new OptionInteger(parameters.TurdSize, 0, 100);
          var idxTurdSize = go.AddOptionInteger("Speckles", ref optTurdSize, "Image despeckle threshold");

          // AlphaMax
          var optAlphaMax = new OptionDouble(parameters.AlphaMax, 0.0, 1.34);
          var idxAlphaMax = go.AddOptionDouble("Corners", ref optAlphaMax, "Corner detection threshold");

          // OptimizeCurve
          //var optOptimizeCurve = new OptionToggle(parameters.OptimizeCurve, "No", "Yes");
          //var idxOptimizeCurve = go.AddOptionToggle("Optimizing", ref optOptimizeCurve);

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

              // TurnPolicy
              //if (idxTurnPolicy == option.Index)
              //{
              //  var list = Enum.GetValues(typeof(PotraceTurnPolicy)).Cast<PotraceTurnPolicy>().ToList();
              //  parameters.TurnPolicy = list[option.CurrentListOptionIndex];
              //  continue;
              //}

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

              // OptimizeCurve
              //if (idxOptimizeCurve == option.Index)
              //{
              //  parameters.OptimizeCurve = optOptimizeCurve.CurrentValue;
              //  continue;
              //}

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
    /// Determines if the bitmap an Eto BitmapData.GetPixel-compatible bitmap
    /// </summary>
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
        graphics.DrawImage(bitmap, 0, 0);

      return bmp;
    }

  }
}
