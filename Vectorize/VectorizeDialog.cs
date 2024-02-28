using Eto.Drawing;
using Eto.Forms;
using Rhino;
using Rhino.UI;
using Rhino.UI.Controls;
using Rhino.UI.Forms;
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
    private bool m_update_and_redraw = true;

    /// <summary>
    /// Public constructor
    /// </summary>
    public VectorizeDialog(RhinoDoc doc, VectorizeConduit conduit)
    {
      m_doc = doc;
      m_conduit = conduit;

      Resizable = false;
      ShowHelpButton = true;
      HelpButtonClick += (sender, e) => ShowHelpUrl();

      Title = VectorizeCommand.Instance.EnglishName;
      Content = CreateTableLayout();
      m_update_and_redraw = false;
      Shown += (sender, e) => UpdateAndRedraw();
    }

    /// <summary>
    /// Creates the content of the dialog
    /// </summary>
    private RhinoDialogTableLayout CreateTableLayout()
    {
      var parameters = m_conduit.Parameters;

      // Threshold (0.0, 100.0)
      var sldThreshold = new Rhino.UI.Controls.Slider(this, true)
      {
        Decimals = 2,
        DrawTextLabels = true,
        ToolTip = PotraceStrings.ThresholdTooltip(false),
        Value1 = parameters.ThresholdUi,
        Width = 200
      };
      sldThreshold.SetMinMax(0.0, 100.0);
      sldThreshold.MouseUp += (sender, args) =>
      {
        var value = sldThreshold.Value1.Value;
        if (parameters.ThresholdUi != value)
        {
          parameters.ThresholdUi = value;
          UpdateAndRedraw();
        }
      };

      // TurdSize (0 to 100)
      var sldTurdSize = new Rhino.UI.Controls.Slider(this, false)
      {
        Decimals = 0,
        DrawTextLabels = true,
        ToolTip = PotraceStrings.TurdSizeTooltip(false),
        Value1 = parameters.TurdSize
      };
      sldTurdSize.SetMinMax(0.0, 100.0);
      sldTurdSize.MouseUp += (sender, args) =>
      {
        var value = (int)sldTurdSize.Value1.Value;
        if (parameters.TurdSize != value)
        {
          parameters.TurdSize = value;
          UpdateAndRedraw();
        }
      };

      // AlphaMax (0.0, 1.34)
      var sldAlphaMax = new Rhino.UI.Controls.Slider(this, false)
      {
        Decimals = 2,
        DrawTextLabels = true,
        ToolTip = PotraceStrings.AlphaMaxTooltip(false),
        Value1 = parameters.AlphaMax
      };
      sldAlphaMax.SetMinMax(0.0, 1.34);
      sldAlphaMax.MouseUp += (sender, args) =>
      {
        var value = sldAlphaMax.Value1.Value;
        if (parameters.AlphaMax != value)
        {
          parameters.AlphaMax = value;
          UpdateAndRedraw();
        }
      };

      // OptimizeTolerance (0.0, 1.0)
      var sldOptimizeTolerance = new Rhino.UI.Controls.Slider(this, false)
      {
        Decimals = 2,
        DrawTextLabels = true,
        ToolTip = PotraceStrings.OptimizeToleranceTooltip(false),
        Value1 = parameters.OptimizeTolerance
      };
      sldOptimizeTolerance.SetMinMax(0.0, 1.0);
      sldOptimizeTolerance.MouseUp += (sender, args) =>
      {
        var value = sldOptimizeTolerance.Value1.Value;
        if (parameters.OptimizeTolerance != value)
        {
          parameters.OptimizeTolerance = value;
          UpdateAndRedraw();
        }
      };

      // IncludeBorder
      var chkIncludeBorder = new CheckBox
      {
        Checked = parameters.IncludeBorder,
        ThreeState = false,
        ToolTip = PotraceStrings.IncludeBorderTooltip
      };
      chkIncludeBorder.CheckedChanged += (sender, args) =>
      {
        var value = chkIncludeBorder.Checked.Value;
        if (parameters.IncludeBorder != value)
        {
          parameters.IncludeBorder = value;
          UpdateAndRedraw();
        }
      };

      // RestoreDefaults
      var btnRestoreDefaults = new Button { Text = "Restore Defaults" };
      btnRestoreDefaults.Click += (sender, args) =>
      {
        m_update_and_redraw = true;
        parameters.SetDefaults();
        sldThreshold.Value1 = parameters.ThresholdUi;
        sldTurdSize.Value1 = parameters.TurdSize;
        sldAlphaMax.Value1 = parameters.AlphaMax;
        sldOptimizeTolerance.Value1 = parameters.OptimizeTolerance;
        chkIncludeBorder.Checked = parameters.IncludeBorder;
        m_update_and_redraw = false;
        UpdateAndRedraw();
      };

      // Layout the controls

      var layout = new RhinoDialogTableLayout(false); // { Spacing = new Eto.Drawing.Size(10, 8) };
      //layout.Rows.Add(new TableRow(new TableCell(new LabelSeparator { Text = "Vectorization options" }, true)));

      var table = new TableLayout { Padding = new Eto.Drawing.Padding(8, 0, 0, 0), Spacing = new Size(10, 8) };
      table.Rows.Add(new TableRow(new TableCell(new Label { Text = PotraceStrings.ThresholdLabel(false) }), new TableCell(sldThreshold)));
      table.Rows.Add(new TableRow(new TableCell(new Label { Text = PotraceStrings.TurdSizeLabel(true) }), new TableCell(sldTurdSize)));
      table.Rows.Add(new TableRow(new TableCell(new Label { Text = PotraceStrings.AlphaMaxLabel(true) }), new TableCell(sldAlphaMax)));
      table.Rows.Add(new TableRow(new TableCell(new Label { Text = PotraceStrings.OptimizeToleranceLabel(false) }), new TableCell(sldOptimizeTolerance)));
      table.Rows.Add(new TableRow(new TableCell(new Label { Text = PotraceStrings.IncludeBorderLabel(true) }), new TableCell(chkIncludeBorder)));
      table.Rows.Add(new TableRow(new TableCell(new Label { Text = "" }), new TableCell(btnRestoreDefaults)));
      table.Rows.Add(null);

      layout.Rows.Add(table);
      layout.Rows.Add(null);

      return layout;
    }

    /// <summary>
    /// Trace the bitmap and redraw the results.
    /// </summary>
    private void UpdateAndRedraw()
    {
      if (m_update_and_redraw || null == m_doc || null == m_conduit)
        return;

      m_update_and_redraw = true;
      using (var cursor = new WaitCursor())
      {
        m_conduit.TraceBitmap();
        m_doc.Views.Redraw();
      }
      m_update_and_redraw = false;
    }

    /// <summary>
    /// Show the help url.
    /// </summary>
    private void ShowHelpUrl()
    {
      VectorizeCommand.ShowHelpUrl();
    }
  }
}
