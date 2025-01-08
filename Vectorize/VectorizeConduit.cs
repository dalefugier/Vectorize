using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using VectorizeCommon;

namespace Vectorize
{
  /// <summary>
  /// Vectorize display conduit.
  /// </summary>
  public class VectorizeConduit : DisplayConduit
  {
    private readonly Eto.Drawing.Bitmap m_etoBitmap;
    private readonly PotraceParameters m_parameters;
    private readonly double m_scaleX = 1.0;
    private readonly double m_scaleY = 1.0;

    private readonly Color m_color = Rhino.ApplicationSettings.AppearanceSettings.SelectedObjectColor;
    private readonly List<Curve> m_curves = new List<Curve>();
    private PotraceBitmap m_potraceBitmap;
    private BoundingBox m_bbox = BoundingBox.Unset;

    /// <summary>
    /// Public constructor.
    /// </summary>
    public VectorizeConduit(Eto.Drawing.Bitmap bitmap, PotraceParameters parameters, double scaleX, double scaleY)
    {
      m_etoBitmap = bitmap;
      m_parameters = parameters;
      m_scaleX = scaleX;
      m_scaleY = scaleY;
    }

    /// <summary>
    /// Potrace parameters.
    /// </summary>
    public PotraceParameters Parameters => m_parameters;

    /// <summary>
    /// The list of outline curves created from the path curves.
    /// These curve may end up in the Rhino document.
    /// </summary>
    public List<Curve> Curves
    {
      get => m_curves;
    }

    public void ClearCurves()
    {
      if (null != m_curves && m_curves.Count > 0)
      {
        foreach (Curve curve in m_curves)
          curve.Dispose();
        m_curves.Clear();
      }
      GC.KeepAlive(this);
    }

    /// <summary>
    /// DisplayConduit.CalculateBoundingBox override.
    /// </summary>
    protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
    {
      if (m_bbox.IsValid)
        e.IncludeBoundingBox(m_bbox);
      GC.KeepAlive(this);
    }

    /// <summary>
    /// DisplayConduit.CalculateBoundingBoxZoomExtents override.
    /// </summary>
    protected override void CalculateBoundingBoxZoomExtents(CalculateBoundingBoxEventArgs e)
    {
      CalculateBoundingBox(e);
      GC.KeepAlive(this);
    }

    /// <summary>
    /// DisplayConduit.DrawOverlay override.
    /// </summary>
    protected override void DrawOverlay(DrawEventArgs e)
    {
      for (int i = 0; i < m_curves.Count; i++)
      {
        if (i == 0 && !m_parameters.IncludeBorder)
          continue;
        e.Display.DrawCurve(m_curves[i], m_color);
      }
      GC.KeepAlive(this);
    }

    /// <summary>
    /// Trace the bitmap using Potrace.
    /// </summary>
    public void TraceBitmap()
    {
      // Clear curve list and reset conduit bounding box
      ClearCurves();
      m_bbox = BoundingBox.Unset;

      // Create Potrace bitmap if needed
      if (null == m_potraceBitmap)
      {
        m_potraceBitmap = new PotraceBitmap(m_etoBitmap, m_parameters.Threshold);
      }
      else if (m_potraceBitmap.Threshold != m_parameters.Threshold)
      {
        m_potraceBitmap.Dispose();
        m_potraceBitmap = null;
        m_potraceBitmap = new PotraceBitmap(m_etoBitmap, m_parameters.Threshold);
      }

      // Trace the bitmap
      Potrace potrace = Potrace.Trace(m_potraceBitmap, m_parameters);
      if (null != potrace)
      {

        // The first curve is always the border curve no matter what
        Point3d[] corners = new Point3d[] {
          Point3d.Origin,
          new Point3d(m_etoBitmap.Width, 0.0, 0.0),
          new Point3d(m_etoBitmap.Width, m_etoBitmap.Height, 0.0),
          new Point3d(0.0, m_etoBitmap.Height, 0.0),
          Point3d.Origin
          };

        PolylineCurve border = new PolylineCurve(corners);
        m_curves.Add(border);

        // Harvest the Potrace path curves
        PotracePath potracePath = potrace.Path;
        while (null != potracePath)
        {
          Curve curve = potracePath.Curve;
          if (null != curve)
            m_curves.Add(curve);
          potracePath = potracePath.Next;
        }

        if (m_curves.Count > 0)
        {
          // Scale the output, per the calculation made in the command.
          if (m_scaleX != 1.0 || m_scaleY != 1.0)
          {
            Transform xform = Transform.Scale(Plane.WorldXY, m_scaleX, m_scaleY, 1.0);
            for (int i = 0; i < m_curves.Count; i++)
              m_curves[i].Transform(xform);
          }
          m_bbox = m_curves[0].GetBoundingBox(true);
        }

        potrace.Dispose();
      }

      GC.KeepAlive(this);
    }
  }
}
