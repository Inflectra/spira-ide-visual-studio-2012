using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Inflectra.Global;
using Inflectra.SpiraTest.IDEIntegration.VisualStudio2010.Business;

namespace Inflectra.SpiraTest.IDEIntegration.VisualStudio2010.Controls
{

	class NumericTextBox : TextBox
	{
		/// <summary>Hit when the control has keyboard focus.</summary>
		/// <param name="e">KeyboardFocusChangedEventArgs</param>
		protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
		{
			try
			{
				//Check to make sure that no non-numeric values were entered.
				if (this.Text.Length > 0)
				{
					if (!this.Text.All<char>(char.IsNumber))
					{
						this.Text = "";
					}
					else
					{
						//Highlight the full number..
						this.CaretIndex = 0;
						this.SelectAll();
					}
				}

				base.OnGotKeyboardFocus(e);
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "OnGotKeyboardFocus()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}

		}

		/// <summary>Hit when the user enters a digit.</summary>
		/// <param name="e">KeyEventArgs</param>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			try
			{
				if ((e.Key >= Key.D0 && e.Key <= Key.D9) ||									//Numeric Keys
					(e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) ||						//Number Pad Keys
					(e.Key == Key.Up || e.Key == Key.Left) ||								//Left/Right keys.
					(e.Key == Key.Delete || e.Key == Key.Insert || e.Key == Key.Back) ||	//Delete, Inisert, Backspace
					(e.Key == Key.Home || e.Key == Key.End) ||								// Home / End
					(e.Key == Key.Tab || e.Key == Key.Enter))								// Tab / Enter
				{
					//Valid key, let them enter it.
					base.OnKeyDown(e);
				}
				else
				{
					//Invalid key, ignore it.
					e.Handled = true;
				}
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "OnKeyDown()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when the text changes.</summary>
		/// <param name="e">TextCompositionEventArgs</param>
		protected override void OnPreviewTextInput(TextCompositionEventArgs e)
		{
			try
			{
				//Verify the new text is still a number.
				string origText = ((NumericTextBox)e.OriginalSource).Text;

				if (!e.Text.All<char>(char.IsNumber))
				{
					this.Text = origText;
					e.Handled = true;
				}
				else
				{
					base.OnPreviewTextInput(e);
				}
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "OnPreviewTextInput()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}

		}

		/// <summary>Hit when the user leaves the testbox.</summary>
		/// <param name="e">RoutedEventArgs</param>
		protected override void OnLostFocus(RoutedEventArgs e)
		{
			try
			{
				//Transform he entered number into a real number..
				if (!string.IsNullOrWhiteSpace(this.Text) && this.Text.All<char>(char.IsNumber))
				{
					this.Text = int.Parse(this.Text).ToString();
				}
				else
				{
					this.Text = "";
				}

				base.OnLostFocus(e);
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "OnLostFocus()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}

		}
	}
}