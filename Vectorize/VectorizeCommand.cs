﻿using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using VectorizeCommon;

namespace Vectorize
{
  /// <summary>
  /// Vectorize command
  /// </summary>
  public class VectorizeCommand : Rhino.Commands.Command
  {
    /// <summary>
    /// Constructor
    /// </summary>
    public VectorizeCommand()
    {
      Instance = this;
    }

    /// <summary>
    /// Get the one and only instance of the Vectorize command.
    /// </summary>
    public static VectorizeCommand Instance { get; private set; }

    /// <summary>
    /// Get the help url.
    /// </summary>
    public static string HelpUrl => "https://github.com/dalefugier/Vectorize/wiki";

    /// <summary>
    /// Show the help url.
    /// </summary>
    public static void ShowHelpUrl()
    {
      Uri uri = new Uri(HelpUrl, UriKind.Absolute);
      try
      {
        Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
      }
      catch (Exception ex)
      {
        RhinoApp.WriteLine(ex.Message);
      }
    }

    /// <summary>
    /// Command.EnglishName override
    /// </summary>
    public override string EnglishName => "Vectorize";

    /// <summary>
    /// Command.OnHelp override
    /// </summary>
    protected override void OnHelp()
    {
      ShowHelpUrl();
    }

    /// <summary>
    /// Command.RunCommand override
    /// </summary>
    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
      // Prompt the user for the name of the image file
      string path = GetImageFileName(mode);
      if (string.IsNullOrEmpty(path))
        return Result.Cancel;

      // Try reading image file
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
      double unitScale = (doc.ModelUnitSystem != UnitSystem.Inches)
        ? RhinoMath.UnitScale(UnitSystem.Inches, doc.ModelUnitSystem)
        : 1.0;

      double scale_x = (double)(1.0 / systemBitmap.HorizontalResolution * unitScale);
      double scale_y = (double)(1.0 / systemBitmap.VerticalResolution * unitScale);

      // I'm not convinced this is useful...
      if (true)
      {
        string format = $"F{doc.DistanceDisplayPrecision}";

        // Print image size in pixels
        RhinoApp.WriteLine("Image size in pixels: {0} x {1}", systemBitmap.Width, systemBitmap.Height);

        if (doc.ModelUnitSystem == UnitSystem.Inches)
        {
          // Print image size in inches
          double width = (double)(systemBitmap.Width / systemBitmap.HorizontalResolution);
          double height = (double)(systemBitmap.Height / systemBitmap.VerticalResolution);
          RhinoApp.WriteLine("Image size in inches: {0} x {1}",
            width.ToString(format, CultureInfo.InvariantCulture),
            height.ToString(format, CultureInfo.InvariantCulture)
          );
        }
        else
        {
          double width = (double)(systemBitmap.Width / systemBitmap.HorizontalResolution * unitScale);
          double height = (double)(systemBitmap.Height / systemBitmap.VerticalResolution * unitScale);
          RhinoApp.WriteLine("Image size in {0}: {1} x {2}",
            doc.ModelUnitSystem.ToString().ToLower(),
            width.ToString(format, CultureInfo.InvariantCulture),
            height.ToString(format, CultureInfo.InvariantCulture)
            );
        }
      }

      RhinoApp.SetCommandPrompt("Processing image, please wait");

      // Convert the bitmap to an Eto bitmap
      Eto.Drawing.Bitmap etoBitmap = BitmapHelpers.ConvertBitmapToEto(systemBitmap);
      if (null == etoBitmap)
      {
        RhinoApp.WriteLine("Unable to convert image to Eto bitmap.");
        return Result.Failure;
      }

      if (!BitmapHelpers.IsCompatibleBitmap(etoBitmap))
      {
        Eto.Drawing.Bitmap tempBitmap = BitmapHelpers.MakeCompatibleBitmap(etoBitmap);
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

      // Get persistent settings
      PotraceParameters parameters = new PotraceParameters();
      parameters.GetSettings(Settings);

      // Create the conduit, which does most of the work
      VectorizeConduit conduit = new VectorizeConduit(etoBitmap, parameters, scale_x, scale_y) { Enabled = true };

      if (mode == RunMode.Interactive)
      {
        RhinoApp.SetCommandPrompt("Vectorize options");

        // Show the interactive dialog box
        VectorizeDialog dialog = new VectorizeDialog(doc, conduit);
        //var dialog = new VectorizeDialogOld(doc, conduit);
        dialog.RestorePosition();
        Result result = dialog.ShowSemiModal(doc, RhinoEtoApp.MainWindow);
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
        GetOption go = new GetOption();
        go.SetCommandPrompt("Vectorize options. Press Enter when done");
        go.AcceptNothing(true);
        while (true)
        {
          RhinoApp.SetCommandPrompt("Tracing image, please wait");
          conduit.TraceBitmap();
          doc.Views.Redraw();
          go.SetCommandPrompt("Vectorize options. Press Enter when done");

          go.ClearCommandOptions();

          // Threshold
          OptionDouble optThreshold = new OptionDouble(parameters.Threshold, 0.0, 100.0);
          int idxThreshold = go.AddOptionDouble("Threshold", ref optThreshold, "Image brightness threshold");

          // Turdsize
          OptionInteger optTurdSize = new OptionInteger(parameters.TurdSize, 0, 100);
          int idxTurdSize = go.AddOptionInteger("Speckles", ref optTurdSize, "Image despeckle threshold");

          // AlphaMax
          OptionDouble optAlphaMax = new OptionDouble(parameters.AlphaMax, 0.0, 1.34);
          int idxAlphaMax = go.AddOptionDouble("Corners", ref optAlphaMax, "Corner detection threshold");

          // OptimizeTolerance
          OptionDouble optOptimizeTolerance = new OptionDouble(parameters.OptimizeTolerance, 0.0, 1.0);
          int idxOptimizeTolerance = go.AddOptionDouble("Optimize", ref optOptimizeTolerance, "Curve simplification tolerance");

          // IncludeBorder
          OptionToggle optIncludeBorder = new OptionToggle(parameters.IncludeBorder, "No", "Yes");
          int idxIncludeBorder = go.AddOptionToggle("IncludeBorder", ref optIncludeBorder);

          // RestoreDefaults
          int idxRestoreDefaults = go.AddOption("RestoreDefaults");

          GetResult res = go.Get();

          if (res == GetResult.Option)
          {
            CommandLineOption option = go.Option();
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
      Rhino.DocObjects.ObjectAttributes attributes = doc.CreateDefaultAttributes();
      attributes.AddToGroup(doc.Groups.Add());

      // Add curves to document
      for (int i = 0; i < conduit.Curves.Count; i++)
      {
        if (i == 0 && !parameters.IncludeBorder) // skip border
          continue;

        Guid objectId = doc.Objects.AddCurve(conduit.Curves[i], attributes);
        Rhino.DocObjects.RhinoObject rhinoObj = doc.Objects.Find(objectId);
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
      Eto.Forms.OpenFileDialog dialog = new Eto.Forms.OpenFileDialog();

      string[] all = { ".bmp", ".gif", ".jpg", ".jpeg", ".png", ".tif", ".tiff" };
      dialog.Filters.Add(new Eto.Forms.FileFilter("All image files", all));

      dialog.Filters.Add(new Eto.Forms.FileFilter("Bitmap", ".bmp"));
      dialog.Filters.Add(new Eto.Forms.FileFilter("GIF", ".gif"));

      string[] jpeg = { ".jpg", ".jpe", ".jpeg" };
      dialog.Filters.Add(new Eto.Forms.FileFilter("JPEG", jpeg));
      dialog.Filters.Add(new Eto.Forms.FileFilter("PNG", ".png"));

      string[] tiff = { ".tif", ".tiff" };
      dialog.Filters.Add(new Eto.Forms.FileFilter("TIFF", tiff));

      Eto.Forms.DialogResult res = dialog.ShowDialog(RhinoEtoApp.MainWindow);
      if (res != Eto.Forms.DialogResult.Ok)
        return null;

      return dialog.FileName;
    }

    /// <summary>
    /// Get the name of an image file scripted.
    /// </summary>
    private string GetImageFileNameScripted()
    {
      GetString gs = new GetString();
      gs.SetCommandPrompt("Name of image file to open");
      gs.AddOption("Browse");
      GetResult result = gs.Get();

      if (result == GetResult.Option)
        return GetImageFileNameInteractive();

      if (result != GetResult.String)
        return null;

      string path = gs.StringResult();
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
