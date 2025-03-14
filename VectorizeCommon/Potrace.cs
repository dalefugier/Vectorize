﻿using Rhino;
using Rhino.Geometry;
using Rhino.UI;
using System;
using System.Collections.Generic;

namespace VectorizeCommon
{
  public class BitmapHelpers
  {
    /// <summary>
    /// Converts a System.Drawing.Bitmap to a Eto.Drawing.Bitmap.
    /// </summary>
    public static Eto.Drawing.Bitmap ConvertBitmapToEto(System.Drawing.Bitmap bitmap)
    {
      if (null == bitmap)
        return null;

      return bitmap.ToEto();
    }

    /// <summary>
    /// Determines if the bitmap an Eto BitmapData.GetPixel-compatible bitmap.
    /// </summary>
    public static bool IsCompatibleBitmap(Eto.Drawing.Bitmap bitmap)
    {
      if (null == bitmap)
        return false;

      using (Eto.Drawing.BitmapData bitmapData = bitmap.Lock())
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
    /// Makes an Eto BitmapData.GetPixel-compatible bitmap.
    /// </summary>
    public static Eto.Drawing.Bitmap MakeCompatibleBitmap(Eto.Drawing.Bitmap bitmap)
    {
      if (null == bitmap)
        return null;

      Eto.Drawing.Size size = new Eto.Drawing.Size(bitmap.Width, bitmap.Height);
      Eto.Drawing.Bitmap etoBitmap = new Eto.Drawing.Bitmap(size, Eto.Drawing.PixelFormat.Format24bppRgb);
      using (Eto.Drawing.Graphics graphics = new Eto.Drawing.Graphics(etoBitmap))
        graphics.DrawImage(bitmap, 0, 0);

      return etoBitmap;
    }
  }

  /// <summary>
  /// Specifies how to resolve ambiguities during decomposition of bitmaps into paths. 
  /// </summary>
  public enum PotraceTurnPolicy
  {
    /// <summary>
    /// Prefers to connect black (foreground) components.
    /// </summary>
    Black = 0,
    /// <summary>
    /// Prefers to connect white (background) components.
    /// </summary>
    White = 1,
    /// <summary>
    /// Always take a left turn.
    /// </summary>
    Left = 2,
    /// <summary>
    /// Always take a right turn.
    /// </summary>
    Right = 3,
    /// <summary>
    /// Default. Prefers to connect the color (black or white) that occurs least
    /// frequently in a local neighborhood of the current position.
    /// </summary>
    Minority = 4,
    /// <summary>
    /// Prefers to connect the color (black or white) that occurs most
    /// frequently in a local neighborhood of the current position.
    /// </summary>
    Majority = 5,
    /// <summary>
    /// Makes a (more or less) random choice.
    /// </summary>
    Random = 6
  }

  /// <summary>
  /// Structure to hold tracing parameters
  /// </summary>
  public class PotraceParameters : IDisposable
  {
    #region Housekeeping

    // potrace_param_t*
    private IntPtr m_ptr = IntPtr.Zero;

    /// <summary>
    /// Gets the constant (immutable) pointer.
    /// </summary>
    /// <returns>The constant pointer.</returns>
    public IntPtr ConstPointer() => m_ptr;

    /// <summary>
    /// Gets the non-constant pointer (for modification).
    /// </summary>
    /// <returns>The non-constant pointer.</returns>
    public IntPtr NonConstPointer() => m_ptr;

    /// <summary>
    /// Actively releases the unmanaged object.
    /// </summary>
    public void Dispose()
    {
      InternalDispose();
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Passively releases the unmanaged object.
    /// </summary>
    ~PotraceParameters()
    {
      InternalDispose();
    }

    /// <summary>
    /// Releases the unmanaged object.
    /// </summary>
    private void InternalDispose()
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.potrace_param_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }

    #endregion // Housekeeping

    #region Construction

    /// <summary>
    /// Constructs a new PotraceParameters object.
    /// </summary>
    internal PotraceParameters(IntPtr ptr)
    {
      m_ptr = ptr;
    }

    /// <summary>
    /// Constructs a new PotraceParameters object.
    /// </summary>
    public PotraceParameters()
    {
      m_ptr = UnsafeNativeMethods.potrace_param_New();
    }

    #endregion // Construction

    #region Properties

    /// <summary>
    /// Used to de-speckle the bitmap to be traced, by removing all curves
    /// whose enclosed area is below the given threshold.
    /// The default for the parameter is 2, and the range is from 0 to 100.
    /// </summary>
    public int TurdSize
    {
      get => GetInt(idx_turdsize);
      set => SetInt(idx_turdsize, value);
    }

    /// <summary>
    /// Specifies how to resolve ambiguities during decomposition of bitmaps into paths.
    /// The default policy is TurnPolicy.Minority, which tends to keep visual lines connected.
    /// </summary>
    public PotraceTurnPolicy TurnPolicy
    {
      get => (PotraceTurnPolicy)GetInt(idx_turnpolicy);
      set => SetInt(idx_turnpolicy, (int)value);
    }

    /// <summary>
    /// The threshold for the detection of corners. 
    /// It controls the smoothness of the traced curve. 
    /// The default is 1.0, and the range is from 0.0 (polygon) to 1.34 (no corners).
    /// </summary>
    public double AlphaMax
    {
      get => GetDouble(idx_alphamax);
      set => SetDouble(idx_alphamax, value);
    }

    /// <summary>
    /// Enables curve optimizing, which optimizes paths by replacing
    /// sequences of Bézier segments with a single segment when possible.
    /// </summary>
    public bool OptimizeCurve
    {
      get => Convert.ToBoolean(GetInt(idx_opticurve));
      set => SetInt(idx_opticurve, Convert.ToInt32(value));
    }

    /// <summary>
    /// Defines the amount of error allowed in this simplification. 
    /// The default is 0.2, and the range is from 0.0 to 1.0. 
    /// </summary>
    /// <remarks>
    /// Larger values tend to decrease the number of segments, at the expense of less accuracy.
    /// For most purposes, the default value is a good tradeoff between space and accuracy.
    /// </remarks>
    public double OptimizeTolerance
    {
      get => GetDouble(idx_opttolerance);
      set => SetDouble(idx_opttolerance, value);
    }

    // The following parameters are not stored in potrace_param_t

    /// <summary>
    /// Bitmap thresholding parameter, used to weight colors to either black or white.
    /// The default value is 0.45. The range is from 0.0 to 1.0.
    /// </summary>
    public double Threshold
    {
      get => m_threshold;
      set => m_threshold = RhinoMath.Clamp(value, 0.0, 1.0);
    }
    private double m_threshold = 0.45;

    /// <summary>
    /// Bitmap thresholding parameter, used to weight colors to either black or white.
    /// The default value is 45. The range is from 0 to 100.
    /// </summary>
    public double ThresholdUi
    {
      get => Threshold * 100.0;
      set => Threshold = value / 100.0;
    }

    /// <summary>
    /// Inverts the bitmap.
    /// </summary>
    public bool Invert
    {
      get => m_invert;
      set => m_invert = value;
    }
    private bool m_invert = false;

    /// <summary>
    /// Include a rectangle curve that bounds the extends of the bitmap.
    /// </summary>
    public bool IncludeBorder { get; set; } = true;

    #endregion // Properties

    #region Methods

    /// <summary>
    /// Sets class properties to their factory defaults.
    /// </summary>
    public void SetDefaults()
    {
      UnsafeNativeMethods.potrace_param_SetDefault(m_ptr);
      Threshold = 0.45;
      Invert = false;
      IncludeBorder = true;
    }

    /// <summary>
    /// Gets parameters from persistent settings.
    /// </summary>
    /// <param name="settings">Persistent settings.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void GetSettings(PersistentSettings settings)
    {
      if (null == settings)
        throw new ArgumentNullException(nameof(settings));
      // TurdSize
      if (settings.TryGetInteger(nameof(TurdSize), out int turdSize))
        TurdSize = turdSize;
      // TurnPolicy
      if (settings.TryGetInteger(nameof(TurnPolicy), out int turnPolicy))
        TurnPolicy = (PotraceTurnPolicy)turnPolicy;
      // AlphaMax
      if (settings.TryGetDouble(nameof(AlphaMax), out double alphaMax))
        AlphaMax = alphaMax;
      // OptimizeCurve
      if (settings.TryGetBool(nameof(OptimizeCurve), out bool optimizeCurve))
        OptimizeCurve = optimizeCurve;
      // OptimizeTolerance
      if (settings.TryGetDouble(nameof(OptimizeTolerance), out double optimizeTolerance))
        OptimizeTolerance = optimizeTolerance;
      // Threshold
      if (settings.TryGetDouble(nameof(Threshold), out double threshold))
        Threshold = threshold;
      // IncludeBorder
      if (settings.TryGetBool(nameof(IncludeBorder), out bool includeBorder))
        IncludeBorder = includeBorder;
    }

    /// <summary>
    /// Sets, or saves, parameters to persistent settings.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public void SetSettings(PersistentSettings settings)
    {
      if (null == settings)
        throw new ArgumentNullException(nameof(settings));
      // TurdSize
      settings.SetInteger(nameof(TurdSize), TurdSize);
      // TurnPolicy
      settings.SetInteger(nameof(TurnPolicy), (int)TurnPolicy);
      // AlphaMax
      settings.SetDouble(nameof(AlphaMax), AlphaMax);
      // OptimizeCurve
      settings.SetBool(nameof(OptimizeCurve), OptimizeCurve);
      // OptimizeTolerance
      settings.SetDouble(nameof(OptimizeTolerance), OptimizeTolerance);
      // Threshold
      settings.SetDouble(nameof(Threshold), Threshold);
      // IncludeBorder
      settings.SetBool(nameof(IncludeBorder), IncludeBorder);
    }

    #endregion // Methods

    #region Private methods

    private const int idx_alphamax = 0;
    private const int idx_opttolerance = 1;

    private double GetDouble(int which)
    {
      return UnsafeNativeMethods.potrace_param_GetSetDouble(m_ptr, which, false, 0);
    }

    private void SetDouble(int which, double setValue)
    {
      UnsafeNativeMethods.potrace_param_GetSetDouble(m_ptr, which, true, setValue);
    }

    private const int idx_turdsize = 0;
    private const int idx_turnpolicy = 1;
    private const int idx_opticurve = 2;

    private int GetInt(int which)
    {
      return UnsafeNativeMethods.potrace_param_GetSetInt(m_ptr, which, false, 0);
    }

    private void SetInt(int which, int setValue)
    {
      UnsafeNativeMethods.potrace_param_GetSetInt(m_ptr, which, true, setValue);
    }

    #endregion // Private methods
  }

  /// <summary>
  /// Represents Potrace's internal bitmap format.
  /// The bitamp uses the cartesian coordinate system, where each pixel takes up the space of one unit square.
  /// The origin of the coordinate system is at the lower left corner of the bitmap.
  /// </summary>
  public class PotraceBitmap : IDisposable
  {
    #region Housekeeping

    // potrace_bitmap_t*
    private IntPtr m_ptr = IntPtr.Zero;

    /// <summary>
    /// Gets the constant (immutable) pointer.
    /// </summary>
    /// <returns>The constant pointer.</returns>
    public IntPtr ConstPointer() => m_ptr;

    /// <summary>
    /// Gets the non-constant pointer (for modification).
    /// </summary>
    /// <returns>The non-constant pointer.</returns>
    public IntPtr NonConstPointer() => m_ptr;

    /// <summary>
    /// Actively releases the unmanaged object.
    /// </summary>
    public void Dispose()
    {
      InternalDispose();
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Passively releases the unmanaged object.
    /// </summary>
    ~PotraceBitmap()
    {
      InternalDispose();
    }

    /// <summary>
    /// Releases the unmanaged object.
    /// </summary>
    private void InternalDispose()
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.potrace_bitmap_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }

    #endregion // Housekeeping

    #region Construction

    /// <summary>
    /// Constructs a new PotraceBitmap object.
    /// </summary>
    internal PotraceBitmap(IntPtr ptr)
    {
      m_ptr = ptr;
    }

    /// <summary>
    /// Constructs a new PotraceBitmap object.
    /// </summary>
    public PotraceBitmap(int width, int height)
    {
      m_ptr = UnsafeNativeMethods.potrace_bitmap_New(width, height);
      if (m_ptr != IntPtr.Zero)
      {
        Width = width;
        Height = height;
      }
    }

    /// <summary>
    /// Constructs a PotraceBitmap from an Eto.Drawing.Bitmap.
    /// </summary>
    /// <param name="bitmap">The Eto Bitmap.</param>
    /// <param name="brightnessThreshold">
    /// Thresholding parameter, used to weight colors to either black or white.
    /// The range is from 0.0 to 1.0.
    /// </param>
    /// <exception cref="ArgumentNullException"></exception>
    public PotraceBitmap(Eto.Drawing.Bitmap bitmap, double brightnessThreshold)
    {
      if (null == bitmap)
        throw new ArgumentNullException(nameof(bitmap));

      if (0 == bitmap.Width || 0 == bitmap.Height)
        return;

      brightnessThreshold = RhinoMath.Clamp(brightnessThreshold, 0.0, 1.0);

      // Do brightness thresholding
      const double brightnessFloor = 0.0;
      double floor = 3.0 * brightnessFloor * 256.0;
      double cutoff = 3.0 * brightnessThreshold * 256.0;
      bool[] values = new bool[bitmap.Width * bitmap.Height];

      using (Eto.Drawing.BitmapData bitmapData = bitmap.Lock())
      {
        int i = 0;
        foreach (Eto.Drawing.Color argbData in bitmapData.GetPixels())
        {
          int alpha = argbData.Ab;
          int white = 3 * (255 - alpha);
          int sample = argbData.Rb + argbData.Gb + argbData.Bb;
          int brightness = sample * alpha / 256 + white;
          bool black = brightness >= floor && brightness < cutoff;
          values[i++] = black;
        };
      }

      // Rather than calling potrace_bitmap_New and then potrace_bitmap_PutPixel
      // width * height times, do it all at once (genius).

      m_ptr = UnsafeNativeMethods.potrace_bitmap_New2(bitmap.Width, bitmap.Height, values.Length, values);
      if (m_ptr != IntPtr.Zero)
      {
        // Potrace bitmaps use a Cartesian coordinate system
        Flip();

        // Save some values for later
        Width = bitmap.Width;
        Height = bitmap.Height;
        Threshold = brightnessThreshold;
      }
    }

    #endregion // Construction

    #region Properties

    /// <summary>
    /// The width of the bitmap.
    /// </summary>
    public int Width { get; private set; } = 0;

    /// <summary>
    /// The height of the bitmap.
    /// </summary>
    public int Height { get; private set; } = 0;

    /// <summary>
    /// The threshold used to create the bitmap.
    /// </summary>
    public double Threshold { get; private set; } = 0.5;

    #endregion // Properties

    #region Methods

    /// <summary>
    /// Clear the bitmap, sets all pixels to white.
    /// </summary>
    public void Clear()
    {
      UnsafeNativeMethods.potrace_bitmap_Clear(m_ptr);
    }

    /// <summary>
    /// Duplcates the bitmap.
    /// </summary>
    /// <returns>A duplicate copy of this bitmap.</returns>
    public PotraceBitmap Duplicate()
    {
      IntPtr ptr = UnsafeNativeMethods.potrace_bitmap_Duplicate(m_ptr);
      return (IntPtr.Zero != ptr) ? new PotraceBitmap(ptr) : null;
    }

    /// <summary>
    /// Inverts the bitmap.
    /// </summary>
    public void Invert()
    {
      UnsafeNativeMethods.potrace_bitmap_Invert(m_ptr);
    }

    /// <summary>
    /// Turns the bitmap upside down.
    /// </summary>
    public void Flip()
    {
      UnsafeNativeMethods.potrace_bitmap_Flip(m_ptr);
    }

    /// <summary>
    /// Gets a pixel.
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <returns>True if the pixel is set (black), false if the pixel is not set (white).</returns>
    public bool GetPixel(int x, int y)
    {
      return UnsafeNativeMethods.potrace_bitmap_GetPixel(m_ptr, x, y);
    }

    /// <summary>
    /// Sets a pixel (black).
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    public void SetPixel(int x, int y)
    {
      UnsafeNativeMethods.potrace_bitmap_SetPixel(m_ptr, x, y);
    }

    /// <summary>
    /// Clears a pixel (white).
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    public void ClearPixel(int x, int y)
    {
      UnsafeNativeMethods.potrace_bitmap_ClearPixel(m_ptr, x, y);
    }

    /// <summary>
    /// Inverts a pixel.
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    public void InvertPixel(int x, int y)
    {
      UnsafeNativeMethods.potrace_bitmap_InvertPixel(m_ptr, x, y);
    }

    /// <summary>
    /// Puts a pixel
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <param name="set">Set true (black) or false (white).</param>
    public void PutPixel(int x, int y, bool set)
    {
      UnsafeNativeMethods.potrace_bitmap_PutPixel(m_ptr, x, y, set);
    }

    #endregion // Methods
  }

  /// <summary>
  /// Represents a Potrace traced bitmap.
  /// </summary>
  public class PotracePath
  {
    #region Housekeeping

    // potrace_path_t*
    private readonly IntPtr m_ptr = IntPtr.Zero;

    #endregion // Housekeeping

    #region Construction

    /// <summary>
    /// Constructs a new Potrace object.
    /// </summary>
    internal PotracePath(IntPtr ptr)
    {
      m_ptr = ptr;
    }

    #endregion // Construction

    #region Properties

    private const int POINT_STRIDE = 2; // A point has two doubles
    private const int CURVE_STRIDE = 4; // A curve segment has four points

    /// <summary>
    /// Gets the next PotracePath.
    /// </summary>
    public PotracePath Next
    {
      get
      {
        IntPtr ptr = UnsafeNativeMethods.potrace_path_Next(m_ptr);
        PotracePath rc = (IntPtr.Zero != ptr) ? new PotracePath(ptr) : null;
        GC.KeepAlive(this);
        return rc;
      }
    }

    /// <summary>
    /// Gets the path curve.
    /// </summary>
    public Curve Curve
    {
      get
      {
        Curve rc = null;
        int count = SegmentCount;
        if (count > 0)
        {
          int size = count * POINT_STRIDE * CURVE_STRIDE;
          double[] values = new double[size];
          if (UnsafeNativeMethods.potrace_path_SegmentPoints(m_ptr, values.Length, values))
          {
            PolyCurve polyCurve = new PolyCurve();

            int i = 0;
            for (int c = 0; c < count; c++)
            {
              Point3d[] points = new Point3d[CURVE_STRIDE];
              for (int p = 0; p < CURVE_STRIDE; p++)
                points[p] = new Point3d(values[i++], values[i++], 0.0);

              if (points[1].IsValid)
              {
                BezierCurve bezier = new Rhino.Geometry.BezierCurve(points);
                polyCurve.AppendSegment(bezier.ToNurbsCurve());
              }
              else
              {
                PolylineCurve polyline = new PolylineCurve(new Point3d[] { points[0], points[2], points[3] });
                polyCurve.AppendSegment(polyline);
              }
            }

            if (polyCurve.IsValid)
            {
              Curve curve = polyCurve.CleanUp();
              rc = curve ?? polyCurve;
            }
          }
        }
        GC.KeepAlive(this);
        return rc;
      }
    }

    /// <summary>
    /// The approximate magnitude of the area enclosed by the curve.
    /// </summary>
    public int Area
    {
      get
      {
        int rc = UnsafeNativeMethods.potrace_path_Area(m_ptr);
        GC.KeepAlive(this);
        return rc;
      }
    }

    /// <summary>
    /// True ('+') or false ('-') depending on orientation.
    /// </summary>
    public bool Sign
    {
      get
      {
        bool rc = UnsafeNativeMethods.potrace_path_Sign(m_ptr);
        GC.KeepAlive(this);
        return rc;
      }
    }

    /// <summary>
    /// Get the number of curve segments.
    /// </summary>
    private int SegmentCount
    {
      get
      {
        int rc = UnsafeNativeMethods.potrace_path_SegmentCount(m_ptr);
        GC.KeepAlive(this);
        return rc;
      }
    }

    /// <summary>
    /// Potrace curve segment type
    /// </summary>
    private enum SegmentType
    {
      /// <summary>
      /// None
      /// </summary>
      None = 0,
      /// <summary>
      /// POTRACE_CURVETO, or Bezier
      /// </summary>
      Curve = 1,
      /// <summary>
      /// POTRACE_CORNER, or Polyline
      /// </summary>
      Corner = 2,
    }

    /// <summary>
    /// Returns the type of curve segment.
    /// </summary>
    /// <param name="index">The segment index.</param>
    /// <returns>The tag.</returns>
    private SegmentType SegmentTag(int index)
    {
      SegmentType rc = (SegmentType)UnsafeNativeMethods.potrace_path_SegmentTag(m_ptr, index);
      GC.KeepAlive(this);
      return rc;
    }

    #endregion // Properties

    #region Methods

    /// <summary>
    /// Get the 3 points for a POTRACE_CORNER.
    /// </summary>
    /// <param name="index">The segment index.</param>
    /// <returns>The points.</returns>
    private Point3d[] SegmentCornerPoints(int index)
    {
      Point3d[] rc = new Point3d[0];
      double[] vertices = new double[6]; // 3- 2d points
      if (UnsafeNativeMethods.potrace_path_SegmentCornerPoints(m_ptr, index, vertices.Length, vertices))
      {
        List<Point3d> points = new List<Point3d>(3);
        for (int vi = 0; vi < vertices.Length; vi += 2)
          points.Add(new Point3d(vertices[vi], vertices[vi + 1], 0.0));
        rc = points.ToArray();
      }
      GC.KeepAlive(this);
      return rc;
    }

    /// <summary>
    /// Get the 4 points for a POTRACE_CURVETO.
    /// </summary>
    /// <param name="index"></param>
    /// <returns>The points.</returns>
    private Point3d[] SegmentCurvePoints(int index)
    {
      Point3d[] rc = new Point3d[0];
      double[] vertices = new double[8]; // 4- 2d points
      if (UnsafeNativeMethods.potrace_path_SegmentCurvePoints(m_ptr, index, vertices.Length, vertices))
      {
        List<Point3d> points = new List<Point3d>(3);
        for (int vi = 0; vi < vertices.Length; vi += 2)
          points.Add(new Point3d(vertices[vi], vertices[vi + 1], 0.0));
        rc = points.ToArray();
      }
      GC.KeepAlive(this);
      return rc;
    }

    #endregion // Methods
  }

  /// <summary>
  /// Represents the results of a Portace traced bitmap.
  /// </summary>
  public class Potrace : IDisposable
  {
    #region Housekeeping

    // potrace_state_t*
    private IntPtr m_ptr = IntPtr.Zero;

    /// <summary>
    /// Gets the constant (immutable) pointer.
    /// </summary>
    /// <returns>The constant pointer.</returns>
    public IntPtr ConstPointer() => m_ptr;

    /// <summary>
    /// Gets the non-constant pointer (for modification).
    /// </summary>
    /// <returns>The non-constant pointer.</returns>
    public IntPtr NonConstPointer() => m_ptr;

    /// <summary>
    /// Actively releases the unmanaged object.
    /// </summary>
    public void Dispose()
    {
      InternalDispose();
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Passively releases the unmanaged object.
    /// </summary>
    ~Potrace()
    {
      InternalDispose();
    }

    /// <summary>
    /// Releases the unmanaged object.
    /// </summary>
    private void InternalDispose()
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.potrace_state_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }

    #endregion // Housekeeping

    #region Construction

    /// <summary>
    /// Constructs a new Potrace object.
    /// </summary>
    internal Potrace(IntPtr ptr)
    {
      m_ptr = ptr;
    }

    #endregion // Construction

    #region Properties

    /// <summary>
    /// Gets the vector data in linked list form.
    /// </summary>
    public PotracePath Path
    {
      get
      {
        IntPtr ptr = UnsafeNativeMethods.potrace_state_PathList(m_ptr);
        GC.KeepAlive(this);
        return (IntPtr.Zero != ptr) ? new PotracePath(ptr) : null;
      }
    }

    #endregion // Properties

    #region Static methods

    /// <summary>
    /// Traces a bitmap.
    /// </summary>
    /// <param name="bitmap">The bitmap to trace.</param>
    /// <returns>The results of the trace.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static Potrace Trace(PotraceBitmap bitmap)
    {
      if (null == bitmap)
        throw new ArgumentNullException(nameof(bitmap));
      PotraceParameters parameters = new PotraceParameters();
      return Trace(bitmap, parameters);
    }

    /// <summary>
    /// Traces a bitmap.
    /// </summary>
    /// <param name="bitmap">The bitmap to trace.</param>
    /// <param name="parameters">The tracing parameters.</param>
    /// <returns>The results of the trace.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static Potrace Trace(PotraceBitmap bitmap, PotraceParameters parameters)
    {
      if (null == bitmap)
        throw new ArgumentNullException(nameof(bitmap));
      if (null == parameters)
        throw new ArgumentNullException(nameof(parameters));

      IntPtr ptr_const_bitmap = bitmap.ConstPointer();
      IntPtr ptr_const_param = parameters.ConstPointer();
      IntPtr ptr = UnsafeNativeMethods.potrace_state_New(ptr_const_bitmap, ptr_const_param);

      GC.KeepAlive(bitmap);
      GC.KeepAlive(parameters);

      return (IntPtr.Zero != ptr) ? new Potrace(ptr) : null;
    }

    #endregion // Static methods
  }

  /// <summary>
  /// Some re-usable tooltips.
  /// </summary>
  public class PotraceStrings
  {
    public static string PathLabel => "Path";
    public static string PathTooltip => "Image file path.";

    public static string ThresholdLabel(bool verbose)
    {
      return verbose ? "Brightness threshold" : "Threshold";
    }

    public static string ThresholdTooltip(bool verbose)
    {
      string str = "Image brightness threshold.";
      if (verbose)
        str += " Range is from 0.0 (black) to 1.0 (white).";
      return str;
    }

    public static string TurnPolicyLabel => "Turn policy";
    public static string TurnPolicyTooltip => "Algorithm used to resolve ambiguities in path decomposition.";

    public static string TurdSizeLabel(bool verbose)
    {
      return verbose ? "Speckles" : "Speckles";
    }

    public static string TurdSizeTooltip(bool verbose)
    {
      string str = "Image despeckle threshold.";
      if (verbose)
        str += " Range is from from 0 to 100.";
      return str;
    }

    public static string AlphaMaxLabel(bool verbose)
    {
      return verbose ? "Smooth corners" : "Corners";
    }

    public static string AlphaMaxTooltip(bool verbose)
    {
      string str = "Corner detection threshold.";
      if (verbose)
        str += " Range is from 0.0 (polygons) to 1.34 (no corner).";
      return str;
    }

    public static string OptimizeCurveTooltip => "Optimize Bézier segments.";
    public static string OptimizeCurveLabel => "Optimize";


    public static string OptimizeToleranceLabel(bool verbose)
    {
      return verbose ? "Optimize tolerance" : "Tolerance";
    }

    public static string OptimizeToleranceTooltip(bool verbose)
    {
      string str = "Bézier segment optimization tolerance.";
      if (verbose)
        str += " Range is from 0.0 to 1.0.";
      return str;
    }

    public static string IncludeBorderLabel(bool verbose)
    {
      return verbose ? "Include border" : "Border";
    }

    public static string IncludeBorderTooltip => "Include border curve.";
  }
}
