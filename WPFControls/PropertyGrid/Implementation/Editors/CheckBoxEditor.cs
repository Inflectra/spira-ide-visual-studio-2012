















using System.Windows;
using System.Windows.Controls;

namespace Inflectra.SpiraTest.IDEIntegration.VisualStudio2012.Forms.ToolKit.PropertyGrid.Editors
{
	public class CheckBoxEditor : TypeEditor<CheckBox>
	{
		protected override void SetControlProperties()
		{
			Editor.Margin = new Thickness(5, 0, 0, 0);
		}

		protected override void SetValueDependencyProperty()
		{
			ValueProperty = CheckBox.IsCheckedProperty;
		}
	}
}
