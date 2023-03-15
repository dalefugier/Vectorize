using Eto.Drawing;
using Eto.Forms;
using Rhino;
using Rhino.UI;
using Rhino.UI.Controls;
using Rhino.UI.Forms;
using System;
using VectorizeCommon;

namespace Vectorize
{
  /// <summary>
  /// Vectorize command dialog
  /// </summary>
  public class VectorizeDialog : CommandDialog
  {
    private readonly RhinoDoc m_doc;
    private readonly VectorizeConduit m_conduit;
    private bool m_allow_update_and_redraw = true;

    /// <summary>
    /// Public constructor
    /// </summary>
    public VectorizeDialog(RhinoDoc doc, VectorizeConduit conduit)
    {
      m_doc = doc;
      m_conduit = conduit;

      Resizable = false;
      ShowHelpButton = false;
      Width = 350;
      Title = "Vectorize";
      Content = CreateTableLayout();
      Shown += (sender, e) => UpdateAndRedraw();
    }

    /// <summary>
    /// Creates the content of the dialog
    /// </summary>
    private RhinoDialogTableLayout CreateTableLayout()
    {
      // Create controls and define behaviors
      var parameters = m_conduit.Parameters;

      // Threshold % (0.0, 100.0)
      var sldThreshold = new Rhino.UI.Controls.Slider(this, true, null, null)
      {
        Decimals = 2,
        ToolTip = PotraceTooltips.Threshold
      };
      sldThreshold.SetMinMax(0.0, 100.0);
      sldThreshold.Value1 = parameters.Threshold * 100.0;
      sldThreshold.PropertyChanged += (sender, args) =>
      {
        if (m_allow_update_and_redraw)
        {
          m_allow_update_and_redraw = false;
          parameters.Threshold = sldThreshold.Value1.Value / 100.0;
          sldThreshold.Value1 = parameters.Threshold * 100.0;
          m_allow_update_and_redraw = true;
          UpdateAndRedraw();
        }
      };

      // TurdSize (0 to 100)
      var sldTurdSize = new Rhino.UI.Controls.Slider(this, false, null, null)
      {
        Decimals = 0,
        ToolTip = PotraceTooltips.TurdSize
      };
      sldTurdSize.SetMinMax(0.0, 100.0);
      sldTurdSize.Value1 = parameters.TurdSize;
      sldTurdSize.PropertyChanged += (sender, args) =>
      {
        if (m_allow_update_and_redraw)
        {
          m_allow_update_and_redraw = false;
          parameters.TurdSize = (int)sldTurdSize.Value1.Value;
          sldTurdSize.Value1 = parameters.TurdSize;
          m_allow_update_and_redraw = true;
          UpdateAndRedraw();
        }
      };

      // AlphaMax (0.0, 1.34)
      var sldAlphaMax = new Rhino.UI.Controls.Slider(this, false, null, null)
      {
        Decimals = 2,
        ToolTip = PotraceTooltips.AlphaMax,
      };
      sldAlphaMax.SetMinMax(0.0, 1.34);
      sldAlphaMax.Value1 = parameters.AlphaMax;
      sldAlphaMax.PropertyChanged += (sender, args) =>
      {
        if (m_allow_update_and_redraw)
        {
          m_allow_update_and_redraw = false;
          parameters.AlphaMax = sldAlphaMax.Value1.Value;
          sldAlphaMax.Value1 = parameters.AlphaMax;
          m_allow_update_and_redraw = true;
          UpdateAndRedraw();
        }
      };

      // OptimizeTolerance (0.0, 1.0)
      var sldOptimizeTolerance = new Rhino.UI.Controls.Slider(this, false, null, null)
      {
        Decimals = 2,
        ToolTip = PotraceTooltips.OptimizeTolerance
      };
      sldOptimizeTolerance.SetMinMax(0.0, 1.0);
      sldOptimizeTolerance.Value1 = parameters.OptimizeTolerance;
      sldOptimizeTolerance.PropertyChanged += (sender, args) =>
      {
        if (m_allow_update_and_redraw)
        {
          m_allow_update_and_redraw = false;
          parameters.OptimizeTolerance = sldOptimizeTolerance.Value1.Value;
          sldOptimizeTolerance.Value1 = parameters.OptimizeTolerance;
          m_allow_update_and_redraw = true;
          UpdateAndRedraw();
        }
      };

      var chkIncludeBorder = new CheckBox
      {
        Checked = parameters.IncludeBorder,
        ThreeState = false,
        ToolTip = PotraceTooltips.IncludeBorder
      };
      chkIncludeBorder.CheckedChanged += (sender, args) =>
      {
        parameters.IncludeBorder = chkIncludeBorder.Checked.Value;
        UpdateAndRedraw();
      };

      var btnReset = new Button { Text = "Restore Defaults" };
      btnReset.Click += (sender, args) =>
      {
        m_allow_update_and_redraw = false;
        parameters.SetDefaults();
        sldThreshold.Value1 = parameters.Threshold * 100.0;
        sldTurdSize.Value1 = parameters.TurdSize;
        sldAlphaMax.Value1 = parameters.AlphaMax;
        sldOptimizeTolerance.Value1 = parameters.OptimizeTolerance;
        chkIncludeBorder.Checked = parameters.IncludeBorder;
        m_allow_update_and_redraw = true;
        UpdateAndRedraw();
      };

      // Layout the controls

      var layout = new RhinoDialogTableLayout(false) { Spacing = new Eto.Drawing.Size(10, 8) };
      layout.Rows.Add(new TableRow(new TableCell(new LabelSeparator { Text = "Vectorization options" }, true)));

      var table = new TableLayout { Padding = new Eto.Drawing.Padding(8, 0, 0, 0), Spacing = new Size(10, 8) };
      table.Rows.Add(new TableRow(new TableCell(new Label() { Text = "Threshold" }), new TableCell(sldThreshold)));
      table.Rows.Add(new TableRow(new TableCell(new Label() { Text = "Speckles" }), new TableCell(sldTurdSize)));
      table.Rows.Add(new TableRow(new TableCell(new Label() { Text = "Smooth corners" }), new TableCell(sldAlphaMax)));
      table.Rows.Add(new TableRow(new TableCell(new Label() { Text = "Optimize" }), new TableCell(sldOptimizeTolerance)));
      table.Rows.Add(new TableRow(new TableCell(new Label() { Text = "Include border" }), new TableCell(chkIncludeBorder)));
      table.Rows.Add(null);
      table.Rows.Add(new TableRow(new TableCell(new Label() { Text = "" }), new TableCell(btnReset)));
      layout.Rows.Add(table);

      return layout;
    }

    private void UpdateAndRedraw()
    {
      if (m_allow_update_and_redraw && null != m_doc && null != m_conduit)
      {
        m_conduit.TraceBitmap();
        m_doc.Views.Redraw();
      }
    }
  }
}
