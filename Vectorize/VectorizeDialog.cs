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

      var ns_threshold = new NumericUpDownWithUnitParsing
      {
        ValueUpdateMode = NumericUpDownWithUnitParsingUpdateMode.WhenDoneChanging,
        MinValue = 0.0,
        MaxValue = 100.0,
        DecimalPlaces = 0,
        Increment = 1.0,
        ToolTip = "Weighted RGB color evaluation threshold",
        Value = (int)(parameters.Threshold * 100.0),
        Width = 45
      };

      var sld_threshold = new Eto.Forms.Slider
      {
        MinValue = 0,
        MaxValue = 100,
        TickFrequency = 25,
        Value = (int)(parameters.Threshold * 100.0),
        Width = 220
      };

      ns_threshold.ValueChanged += (sender, args) =>
      {
        if (m_allow_update_and_redraw)
        {
          m_allow_update_and_redraw = false;
          parameters.Threshold = ns_threshold.Value / 100.0;
          sld_threshold.Value = (int)(parameters.Threshold * 100.0);
          m_allow_update_and_redraw = true;
          UpdateAndRedraw();
        }
      };

      sld_threshold.ValueChanged += (sender, args) =>
      {
        if (m_allow_update_and_redraw)
        {
          m_allow_update_and_redraw = false;
          parameters.Threshold = sld_threshold.Value / 100.0;
          ns_threshold.Value = (int)(parameters.Threshold * 100.0);
          m_allow_update_and_redraw = true;
          UpdateAndRedraw();
        }
      };

      var dd_turnpolicy = new DropDown
      {
        ToolTip = "Algorithm used to resolve ambiguities in path decomposition"
      };
      foreach (var str in Enum.GetNames(typeof(PotraceTurnPolicy)))
        dd_turnpolicy.Items.Add(str);
      dd_turnpolicy.SelectedIndex = (int)parameters.TurnPolicy;
      dd_turnpolicy.SelectedIndexChanged += (sender, args) =>
      {
        if (dd_turnpolicy.SelectedIndex != 0)
        {
          parameters.TurnPolicy = (PotraceTurnPolicy)dd_turnpolicy.SelectedIndex;
          UpdateAndRedraw();
        }
      };

      var ns_turdsize = new NumericUpDownWithUnitParsing
      {
        ValueUpdateMode = NumericUpDownWithUnitParsingUpdateMode.WhenDoneChanging,
        MinValue = 0.0,
        MaxValue = 100.0,
        DecimalPlaces = 0,
        Increment = 1.0,
        ToolTip = "Filter speckles of up to this size in pixels",
        Value = parameters.TurdSize
      };
      ns_turdsize.ValueChanged += (sender, args) =>
      {
        parameters.TurdSize = (int)ns_turdsize.Value;
        UpdateAndRedraw();
      };

      var ns_alphamax = new NumericUpDownWithUnitParsing
      {
        ValueUpdateMode = NumericUpDownWithUnitParsingUpdateMode.WhenDoneChanging,
        MinValue = 0.0,
        MaxValue = 1.34,
        DecimalPlaces = 1,
        Increment = 0.1,
        ToolTip = "Corner rounding threshold",
        Value = parameters.AlphaMax
      };
      ns_alphamax.ValueChanged += (sender, args) =>
      {
        parameters.AlphaMax = ns_alphamax.Value;
        UpdateAndRedraw();
      };

      var chk_includeborder = new CheckBox
      {
        ThreeState = false,
        ToolTip = "Include border rectangle",
        Checked = parameters.IncludeBorder
      };
      chk_includeborder.CheckedChanged += (sender, args) =>
      {
        parameters.IncludeBorder = chk_includeborder.Checked.Value;
        UpdateAndRedraw();
      };

      var chk_curveoptimizing = new CheckBox
      {
        ThreeState = false,
        ToolTip = "Optimize of Bézier segments by a single segment when possible",
        Checked = parameters.OptimizeCurve
      };

      var ns_opttolerance = new NumericUpDownWithUnitParsing
      {
        ValueUpdateMode = NumericUpDownWithUnitParsingUpdateMode.WhenDoneChanging,
        MinValue = 0.0,
        MaxValue = 1.0,
        DecimalPlaces = m_doc.ModelDistanceDisplayPrecision,
        Increment = 0.1,
        Enabled = parameters.OptimizeCurve,
        ToolTip = "Tolerance used to optimize Bézier segments",
        Value = parameters.OptimizeTolerance
      };

      chk_curveoptimizing.CheckedChanged += (sender, args) =>
      {
        parameters.OptimizeCurve = chk_curveoptimizing.Checked.Value;
        ns_opttolerance.Enabled = parameters.OptimizeCurve;
        UpdateAndRedraw();
      };

      ns_opttolerance.ValueChanged += (sender, args) =>
      {
        parameters.OptimizeTolerance = ns_opttolerance.Value;
        UpdateAndRedraw();
      };

      var btn_reset = new Button
      {
        Text = "Restore Defaults"
      };
      btn_reset.Click += (sender, args) =>
      {
        m_allow_update_and_redraw = false;
        parameters.SetDefaults();

        sld_threshold.Value = (int)(parameters.Threshold * 100.0);
        ns_threshold.Value = sld_threshold.Value;
        dd_turnpolicy.SelectedIndex = (int)parameters.TurnPolicy;
        ns_turdsize.Value = parameters.TurdSize;
        ns_alphamax.Value = parameters.AlphaMax;
        chk_includeborder.Checked = parameters.IncludeBorder;
        chk_curveoptimizing.Checked = parameters.OptimizeCurve;
        ns_opttolerance.Value = parameters.OptimizeTolerance;

        m_allow_update_and_redraw = true;
        UpdateAndRedraw();
      };

      // Layout the controls

      var minimum_size = new Eto.Drawing.Size(150, 0);

      var layout = new RhinoDialogTableLayout(false) { Spacing = new Eto.Drawing.Size(10, 8) };
      layout.Rows.Add(new TableRow(new TableCell(new LabelSeparator { Text = "Vectorization options" }, true)));

      var panel0 = new Panel { MinimumSize = minimum_size, Content = new Label() { Text = "Threshold" } };
      var table0 = new TableLayout { Padding = new Eto.Drawing.Padding(8, 0, 0, 0) };
      table0.Rows.Add(new TableRow(new TableCell(panel0), new TableCell(sld_threshold, true), new TableCell(ns_threshold)));
      layout.Rows.Add(table0);

      var panel1 = new Panel { MinimumSize = minimum_size, Content = new Label() { Text = "Turn policy" } };
      var panel2 = new Panel { MinimumSize = minimum_size, Content = new Label() { Text = "Include border" } };

      var table1 = new TableLayout { Padding = new Eto.Drawing.Padding(8, 0, 0, 0), Spacing = new Size(10, 8) };
      table1.Rows.Add(new TableRow(new TableCell(panel1), new TableCell(dd_turnpolicy)));
      table1.Rows.Add(new TableRow(new TableCell(new Label() { Text = "Filter size" }), new TableCell(ns_turdsize)));
      table1.Rows.Add(new TableRow(new TableCell(new Label() { Text = "Corner rounding" }), new TableCell(ns_alphamax)));
      table1.Rows.Add(new TableRow(new TableCell(panel2), new TableCell(chk_includeborder)));

      layout.Rows.Add(table1);

      layout.Rows.Add(new TableRow(new TableCell(new LabelSeparator { Text = "Curve optimization" }, true)));

      var panel3 = new Panel { MinimumSize = minimum_size, Content = new Label() { Text = "Optimizing" } };
      var table2 = new TableLayout { Padding = new Eto.Drawing.Padding(8, 0, 0, 0), Spacing = new Size(10, 8) };
      table2.Rows.Add(new TableRow(new TableCell(panel3), new TableCell(chk_curveoptimizing)));
      table2.Rows.Add(new TableRow(new TableCell(new Label() { Text = "Tolerance" }), new TableCell(ns_opttolerance)));
      table2.Rows.Add(null);
      table2.Rows.Add(new TableRow(new TableCell(new Label() { Text = "" }), new TableCell(btn_reset)));
      layout.Rows.Add(table2);

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
