﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Inflectra.SpiraTest.IDEIntegration.VisualStudio2012.Business.SpiraTeam_Client;
using Inflectra.SpiraTest.IDEIntegration.VisualStudio2012.Controls;
using Inflectra.SpiraTest.IDEIntegration.VisualStudio2012.Forms;
using Inflectra.SpiraTest.IDEIntegration.VisualStudio2012.Forms.ToolKit;

namespace Inflectra.SpiraTest.IDEIntegration.VisualStudio2012.Controls
{
	/// <summary>
	/// Interaction logic for cntlCustomProps.xaml
	/// </summary>
	public partial class cntlCustomProps : UserControl
	{
		/// <summary>Our remote artifact, holds the custom property values.</summary>
		private RemoteArtifact item;
		/// <summary>The defined custom properties.</summary>
		private List<RemoteCustomProperty> propertyDefinitions;
		/// <summary>All the custom lists we have defined.</summary>
		private List<RemoteCustomList> listDefinitions;
		/// <summary>List of all project users that we can select from.</summary>
		private List<RemoteProjectUser> listUsers;

		/// <summary>The number of control columns.</summary>
		private int numCols = 2;

		/// <summary>Holds any user-specified column widths.</summary>
		Dictionary<int, GridLength> colWidths = new Dictionary<int, GridLength>();

		/// <summary>Create an instance of the class.</summary>
		public cntlCustomProps()
		{
			InitializeComponent();

			//Set initial defaults.
			this.LabelHorizontalAlignment = System.Windows.HorizontalAlignment.Left;
			this.LabelVerticalAlignment = System.Windows.VerticalAlignment.Center;
			this.ControlHorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
			this.ControlVerticalAlignment = System.Windows.VerticalAlignment.Center;
		}

		#region Private Functions
		/// <summary>Load the data into the control.</summary>
		private void DataBindFields()
		{
			#region Control Creation
			//Create our columns, first.
			for (int i = 1; i <= this.numCols * 2; i++)
			{
				//Get the col width..
				GridLength width;
				if (this.colWidths.ContainsKey(i))
				{
					width = this.colWidths[i];
				}
				else
				{
					width = ((i % 2 == 1) ? GridLength.Auto : new GridLength(.5, GridUnitType.Star));
				}
				this.grdContent.ColumnDefinitions.Add(new ColumnDefinition() { Width = width });
			}

			//Go through the field definitions, and create the fields.
			int current_rowNum = -1;
			int current_colNum = this.numCols * 2;

			//Create the fields, set initial (default) values..
			foreach (RemoteCustomProperty prop in this.propertyDefinitions)
			{
				if (current_colNum >= (this.numCols * 2))
				{
					//Advance/Reset counters..
					current_rowNum++;
					current_colNum = 0;

					//Add the new row.
					this.grdContent.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
				}

				//Create the field..
				Control propControl = null;
				TextBlock propLabel = new TextBlock();

				bool twoCols = false;
				switch (prop.CustomPropertyTypeId)
				{
					#region Text & URL
					case 1: //Text field.
					case 9: //URL field.
						//Check for richtext, first..
						bool isRich = false;
						if (prop.CustomPropertyTypeId == 1 &&
							prop.Options != null &&
							prop.Options.Where(op => op.CustomPropertyOptionId == 4).Count() == 1)
						{
							isRich = this.getBoolFromValue(prop.Options.Where(op => op.CustomPropertyOptionId == 4).Single());
						}

						if (isRich)
						{
							propControl = new cntrlRichTextEditor();
							((cntrlRichTextEditor)propControl).MinHeight = 150;
							twoCols = true;
						}
						else
							propControl = new TextBox();

						//Get other options..
						if (prop.CustomPropertyTypeId == 1 && prop.Options != null)
						{
							foreach (RemoteCustomPropertyOption opt in prop.Options)
							{
								switch (opt.CustomPropertyOptionId)
								{
									case 2: // Max Length
										int? maxLen = this.getIntFromValue(opt);
										if (!isRich && maxLen.HasValue && maxLen.Value > 0)
										{
											((TextBox)propControl).MaxLength = maxLen.Value;
										}
										break;

									case 3: // Min Length
										//Nothing done here.
										break;

									case 4: // RichText
										break;

									case 5: // Default
										if (isRich)
											((cntrlRichTextEditor)propControl).HTMLText = opt.Value;
										else
											((TextBox)propControl).Text = opt.Value;
										break;
								}
							}
						}

						//Get the artifact's custom prop, if there is one..
						if (this.item != null)
						{
							RemoteArtifactCustomProperty custProp = this.getItemsCustomProp(prop);
							string value = "";
							if (custProp != null)
								value = custProp.StringValue;

							if (isRich)
								((cntrlRichTextEditor)propControl).HTMLText = value;
							else
								((TextBox)propControl).Text = value;

						}
						break;
					#endregion

					#region Integer
					case 2: //Integer field.
						propControl = new IntegerUpDown();
						foreach (RemoteCustomPropertyOption opt in prop.Options)
						{
							switch (opt.CustomPropertyOptionId)
							{
								case 5: // Default
									((IntegerUpDown)propControl).Value = this.getIntFromValue(opt);
									break;

								case 6: // Maximum Allowed
									((IntegerUpDown)propControl).Maximum = this.getIntFromValue(opt);
									break;

								case 7: // Minimum Allowed
									((IntegerUpDown)propControl).Minimum = this.getIntFromValue(opt);
									break;
							}
						}

						//Get the artifact's custom prop, if there is one..
						if (this.item != null)
						{
							RemoteArtifactCustomProperty custProp = this.getItemsCustomProp(prop);
							if (custProp != null)
								((IntegerUpDown)propControl).Value = custProp.IntegerValue;
							else
								((IntegerUpDown)propControl).Value = null;
						}

						break;
					#endregion

					#region Decimal
					case 3: //Decimal field.
						propControl = new DecimalUpDown();

						foreach (RemoteCustomPropertyOption opt in prop.Options)
						{
							switch (opt.CustomPropertyOptionId)
							{
								case 5: // Default
									((DecimalUpDown)propControl).Value = this.getDecimalFromValue(opt);
									break;

								case 6: // Maximum Allowed
									((DecimalUpDown)propControl).Maximum = this.getDecimalFromValue(opt);
									break;

								case 7: // Minimum Allowed
									((DecimalUpDown)propControl).Minimum = this.getDecimalFromValue(opt);
									break;

								case 8: // Precision. (# digits after decimal.)
									int? digitNum = this.getIntFromValue(opt);
									if (!digitNum.HasValue) digitNum = 0;
									((DecimalUpDown)propControl).FormatString = "F" + digitNum.Value.ToString();
									decimal incNum = ((digitNum.Value < 2) ? 1 : (decimal)Math.Pow((double)10, (double)((digitNum - 2) * -1)));
									((DecimalUpDown)propControl).Increment = incNum;
									break;
							}
						}

						//Get the artifact's custom prop, if there is one..
						if (this.item != null)
						{
							RemoteArtifactCustomProperty custProp = this.getItemsCustomProp(prop);
							if (custProp != null)
								((DecimalUpDown)propControl).Value = custProp.DecimalValue;
							else
								((DecimalUpDown)propControl).Value = null;

						}
						break;
					#endregion

					#region Boolean
					case 4: //Boolean (Checkbox) field.
						{
							//The control..
							propControl = new CheckBox();
							if (prop.Options.Where(op => op.CustomPropertyOptionId == 5).Count() == 1)
							{
								((CheckBox)propControl).IsChecked = this.getBoolFromValue(prop.Options.Where(op => op.CustomPropertyOptionId == 5).Single());
							}

							if (this.item != null)
							{
								RemoteArtifactCustomProperty custProp = this.getItemsCustomProp(prop);
								if (custProp != null)
									((CheckBox)propControl).IsChecked = custProp.BooleanValue;
								else
									((CheckBox)propControl).IsChecked = false;
							}
						}
						break;
					#endregion

					#region Date
					case 5: //Date field.
						{
							//The control..
							propControl = new DatePicker();
							foreach (RemoteCustomPropertyOption opt in prop.Options)
							{
								switch (opt.CustomPropertyOptionId)
								{
									case 5: //Default
										{
											DateTime? optVal = this.getDateFromValue(opt);
											if (optVal.HasValue)
											{
												((DatePicker)propControl).SelectedDate = optVal.Value.ToLocalTime();
											}
										}
										break;
								}
							}

							if (this.item != null)
							{
								RemoteArtifactCustomProperty custProp = this.getItemsCustomProp(prop);
								if (custProp != null)
									if (custProp.DateTimeValue.HasValue)
									{
										((DatePicker)propControl).SelectedDate = custProp.DateTimeValue.Value.ToLocalTime();
									}
									else
									{
										((DatePicker)propControl).SelectedDate = null;
									}
								else
								{
									((DatePicker)propControl).SelectedDate = null;
								}
							}
						}
						break;
					#endregion

					#region List, User
					case 6: //List field.
					case 8: //User field.
						{
							propControl = new ComboBox();
							if (prop.CustomPropertyTypeId == 6 && prop.CustomList.Values.Count() > 0)
							{
								((ComboBox)propControl).ItemsSource = prop.CustomList.Values;
							}
							else if (prop.CustomPropertyTypeId == 8)
								((ComboBox)propControl).ItemsSource = this.listUsers;
							else
								propControl = null;

							//If we're allowing empty, add a 'none' item..
							if (prop.Options.Count(op => op.CustomPropertyOptionId == 1) == 1)
							{
								bool allowEmpty = this.getBoolFromValue(prop.Options.SingleOrDefault(op => op.CustomPropertyOptionId == 1));
								if (allowEmpty)
								{
									if (prop.CustomPropertyTypeId==6)
									{
										((List<RemoteCustomListValue>)((ComboBox)propControl).ItemsSource).Insert(0, new RemoteCustomListValue() { CustomPropertyListId = -1, Name = "-- None --" });
									}
									else if (prop.CustomPropertyTypeId==8)
									{
										((List<RemoteProjectUser>)((ComboBox)propControl).ItemsSource).Insert(0,new RemoteProjectUser() { FullName = "-- None --" });
									}
								}
							}

							if (this.item != null)
							{
								RemoteArtifactCustomProperty custProp = this.getItemsCustomProp(prop);
								if (custProp != null)
								{
									if (prop.CustomPropertyTypeId == 6 && prop.CustomList.Values.Count() > 0)
									{
										((ComboBox)propControl).SelectedItem = prop.CustomList.Values.Where(clv => clv.CustomPropertyValueId == custProp.IntegerValue).SingleOrDefault();
									}
									else if (prop.CustomPropertyTypeId == 8)
									{
										((ComboBox)propControl).SelectedItem = this.listUsers.Where(luv => luv.UserId == custProp.IntegerValue).SingleOrDefault();
									}
								}
								else
								{
									if (prop.CustomPropertyTypeId == 6)
									{
										((ComboBox)propControl).SelectedItem = null;
									}
									else if (prop.CustomPropertyTypeId == 8)
									{
										((ComboBox)propControl).SelectedItem = null;
									}
								}

							}
						}
						break;
					#endregion

					#region Multilist
					case 7: //Multilist field.
						{
							if (prop.CustomList.Values.Count() > 0)
							{
								propControl = new ListBox();
								((ListBox)propControl).SelectionMode = SelectionMode.Multiple;
								((ListBox)propControl).ItemsSource = prop.CustomList.Values;

								//TODO: Set default on MultiList items.
							}

							if (this.item != null)
							{
								((ListBox)propControl).SelectedItems.Clear();
								RemoteArtifactCustomProperty custProp = this.getItemsCustomProp(prop);
								if (custProp != null && custProp.IntegerListValue != null)
								{
									foreach (int selItem in custProp.IntegerListValue)
									{
										RemoteCustomListValue lst = prop.CustomList.Values.Where(clv => clv.CustomPropertyValueId == selItem).SingleOrDefault();
										if (lst != null)
										{
											((ListBox)propControl).SelectedItems.Add(lst);
										}
									}
								}
							}
						}
						break;
					#endregion
				}

				//Now add the control to our grid.
				if (propControl != null)
				{
					//Save the custom property definition.
					propControl.Tag = prop;

					//The label properties..
					propLabel.Text = prop.Name + ":";
					propLabel.Margin = this.LabelMargin;
					propLabel.VerticalAlignment = this.LabelVerticalAlignment;
					propLabel.HorizontalAlignment = this.LabelHorizontalAlignment;
					if (this.LabelStyle != null) propLabel.Style = this.LabelStyle;

					//The other control properties..
					propControl.Margin = this.ControlMargin;
					propControl.Padding = this.ControlPadding;
					propControl.VerticalAlignment = this.ControlVerticalAlignment;
					propControl.HorizontalAlignment = this.ControlHorizontalAlignment;
					if (this.ControlStyle != null) propControl.Style = this.ControlStyle;

					// Get a new row, if necessary.
					int useCols = (2 * ((twoCols) ? 2 : 1));
					if ((useCols + current_colNum) > (this.numCols * 2))
					{
						//need to create a new row.
						current_rowNum++;
						current_colNum = 0;
						this.grdContent.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
					}

					//Add the label.
					this.grdContent.Children.Add(propLabel);
					Grid.SetColumn(propLabel, current_colNum);
					Grid.SetRow(propLabel, current_rowNum);

					//Add the control..
					this.grdContent.Children.Add(propControl);
					Grid.SetColumn(propControl, current_colNum + 1);
					Grid.SetRow(propControl, current_rowNum);
					if (twoCols) Grid.SetColumnSpan(propControl, 3);

					//Advance the column count..
					current_colNum += ((twoCols) ? 4 : 2);

					//Loop and start the next property..
				}
			}
			#endregion //Control Creation

		}

		/// <summary>Gets the boolean from the value field of the option.</summary>
		/// <param name="opt">The custom propert option.</param>
		/// <returns>Boolean</returns>
		private bool getBoolFromValue(RemoteCustomPropertyOption opt)
		{
			bool retValue = false;
			try
			{
				if (!bool.TryParse(opt.Value, out retValue))
				{
					//Check for 'Y'.
					retValue = (opt.Value.ToUpperInvariant().Trim().Equals("Y"));
				}
			}
			catch { }
			return retValue;
		}

		/// <summary>Gets the boolean from the value field of the option.</summary>
		/// <param name="opt">The custom propert option.</param>
		/// <returns>Boolean</returns>
		private int? getIntFromValue(RemoteCustomPropertyOption opt)
		{
			int? retValue = null;
			try
			{
				int test = 0;
				if (int.TryParse(opt.Value, out test))
				{
					retValue = test;
				}
			}
			catch { }
			return retValue;
		}

		/// <summary>Convert the string value to a datetime. Returns null if unparseable.</summary>
		/// <param name="opt">The option to get the value from.</param>
		/// <returns>A DateTime</returns>
		private DateTime? getDateFromValue(RemoteCustomPropertyOption opt)
		{
			DateTime? retValue = null;
			try
			{
				DateTime test = DateTime.Now;
				if (DateTime.TryParse(opt.Value, out test))
				{
					retValue = test;
				}
			}
			catch { }
			return retValue;
		}

		/// <summary>Convert the string value to a datetime. Returns null if unparseable.</summary>
		/// <param name="opt">The option to get the value from.</param>
		/// <returns>A DateTime</returns>
		private decimal? getDecimalFromValue(RemoteCustomPropertyOption opt)
		{
			decimal? retValue = null;
			try
			{
				decimal test = 0;
				if (decimal.TryParse(opt.Value, out test))
				{
					retValue = test;
				}
			}
			catch { }
			return retValue;
		}

		/// <summary>Pulls a matching Artifact Custom Prop from the artifact for the given RemoteCustomProp given.</summary>
		/// <param name="custProp">The custom property to pull from the artifact.</param>
		/// <returns>The ArtifactCustomProperty.</returns>
		private RemoteArtifactCustomProperty getItemsCustomProp(RemoteCustomProperty custProp)
		{
			RemoteArtifactCustomProperty retValue = null;

			if (this.item != null && this.item.CustomProperties.Where(cp => cp.PropertyNumber == custProp.PropertyNumber).Count() == 1)
			{
				retValue = this.item.CustomProperties.Where(cp => cp.PropertyNumber == custProp.PropertyNumber).SingleOrDefault();
			}

			return retValue;
		}
		#endregion #Private Functions

		#region Properties
		/// <summary>The number of columns to display the controls in.</summary>
		public int NumberControlColumns
		{
			get
			{
				return this.numCols;
			}
			set
			{
				if (value == null || value < 1)
					throw new ArgumentException("Number of columns must be at least 1.");

				this.numCols = value;
			}
		}

		/// <summary>Margin for the lable control.</summary>
		public Thickness LabelMargin
		{ get; set; }

		/// <summary>Horizontal Alignmnet for Labels</summary>
		public HorizontalAlignment LabelHorizontalAlignment
		{ get; set; }

		/// <summary>Vertical alignment for labels.</summary>
		public VerticalAlignment LabelVerticalAlignment
		{ get; set; }

		/// <summary>Padding for the data control.</summary>
		public Thickness ControlPadding
		{ get; set; }

		/// <summary>Margin for the data control.</summary>
		public Thickness ControlMargin
		{ get; set; }

		/// <summary>Horizontal Alignment for controls.</summary>
		public HorizontalAlignment ControlHorizontalAlignment
		{ get; set; }

		/// <summary>Vertical Alignment for controls.</summary>
		public VerticalAlignment ControlVerticalAlignment
		{ get; set; }

		/// <summary>The Style to apply to Controls.</summary>
		public Style ControlStyle
		{ get; set; }

		/// <summary>The style to apply to labels.</summary>
		public Style LabelStyle
		{ get; set; }

		#endregion #Properties

		#region Public Functions
		/// <summary>Sets the data that we're databinding against.</summary>
		/// <param name="fieldDataSource">The remote artifact like the incident or requirement.</param>
		/// <param name="fieldDataDefinition">The full definition of custom properties.</param>
		/// <param name="fieldCustomLists">Any defined custom lists.</param>
		/// <param name="delayDisplay">Whether or not to display data now, or wait until .DataBindFields() is called.</param>
		/// <param name="projectUsers">List of project users that can be selected.</param>
		public void SetItemsSource(RemoteArtifact fieldDataSource, List<RemoteCustomProperty> fieldDataDefinition, List<RemoteCustomList> fieldCustomLists, List<RemoteProjectUser> projectUsers, bool delayDisplay = false)
		{
			if (fieldDataDefinition == null)
			{ throw new ArgumentNullException("fieldDataDefinition"); }
			else if (fieldDataSource == null)
			{ throw new ArgumentNullException("fieldDataSource"); }
			else if (fieldCustomLists == null)
			{ throw new ArgumentNullException("fieldCustomLists"); }
			else if (projectUsers == null)
			{ throw new ArgumentNullException("projectUsers"); }


			this.item = fieldDataSource;
			this.propertyDefinitions = fieldDataDefinition;
			this.listDefinitions = fieldCustomLists;
			this.listUsers = projectUsers;

			if (!delayDisplay)
				this.DataBindFields();

		}

		/// <summary>Set the width for the specified column. Defaults to Auto and * for Label, Control columns.</summary>
		/// <param name="colNum">The column number. 1, 3, 5 are label columns, while 2, 4, 6 are control columns.</param>
		/// <param name="width">The width. null to remove the width.</param>
		public void SetColNumWidth(int colNum, GridLength width)
		{
			if (colNum > (this.numCols * 2))
			{
				throw new ArgumentException("Column number specified (" +
					colNum.ToString() +
					") cannot be higher than the number of columns in the control (" +
					(this.numCols * 2).ToString() +
					" (" +
					this.numCols.ToString() +
					" specified.)).");
			}
			else
			{
				if (width != null && this.colWidths.ContainsKey(colNum))
				{
					this.colWidths.Remove(colNum);
				}
				else if (this.colWidths.ContainsKey(colNum))
				{
					this.colWidths[colNum] = width;
				}
				else
				{
					this.colWidths.Add(colNum, width);
				}
			}
		}

		/// <summary>Clears the loaded data.</summary>
		public void ClearData()
		{
			//Clear out our records..
			this.item = null;
			this.propertyDefinitions = new List<RemoteCustomProperty>();
			this.listDefinitions = new List<RemoteCustomList>();
			this.listUsers = new List<RemoteProjectUser>();
			this.numCols = 2;

			//Remove the controls..
			for (int i = 0; i < this.grdContent.Children.Count; )
			{
				this.grdContent.Children.Remove(this.grdContent.Children[i]);
			}

			//Remove rows and columns.
			this.grdContent.RowDefinitions.Clear();
			this.grdContent.ColumnDefinitions.Clear();
		}

		/// <summary>Checks values for validity.</summary>
		/// <returns>False if there are errors, true if there are no issues and values are valid.</returns>
		/// <param name="highlightFieldsInError">If true, checking for the fields will then highlight them as being in error.</param>
		public bool PerformValidation(bool highlightFieldsInError = false)
		{
			bool retValue = true;
			//Loop through each custom property, and make sure that fields are entered properly.

			foreach (UIElement cont in this.grdContent.Children)
			{
				if (cont is Control)
				{
					if (((Control)cont).Tag is RemoteCustomProperty)
					{
						RemoteCustomProperty prop = ((RemoteCustomProperty)((Control)cont).Tag);

						switch (prop.CustomPropertyTypeId)
						{
							case 1: // Text
							case 9: // URL
								{
									string enteredValue = "";
									if (cont is cntrlRichTextEditor)
										enteredValue = ((cntrlRichTextEditor)cont).HTMLText.Trim();
									else if (cont is TextBox)
										enteredValue = ((TextBox)cont).Text.Trim();

									//Required. (Only plain text & url)
									if (prop.Options.Count(op => op.CustomPropertyOptionId == 1) == 1)
									{
										bool required = !this.getBoolFromValue(prop.Options.Single(op => op.CustomPropertyOptionId == 1));

										if (required && string.IsNullOrWhiteSpace(enteredValue) && (cont is TextBox))
											retValue = false;
									}

									//Max Length (Only plain text & url)
									if (prop.Options.Count(op => op.CustomPropertyOptionId == 2) == 1)
									{
										int? MaxLength = this.getIntFromValue(prop.Options.Single(op => op.CustomPropertyOptionId == 2));

										if (MaxLength.HasValue && MaxLength.Value > 0)
										{
											if (enteredValue.Length > MaxLength)
												retValue = false;
										}
									}

									//Min Length (Only plain text & url)
									if (prop.Options.Count(op => op.CustomPropertyOptionId == 3) == 1)
									{
										int? MinLength = this.getIntFromValue(prop.Options.Single(op => op.CustomPropertyOptionId == 3));

										if (MinLength.HasValue && MinLength.Value > 0)
										{
											if (enteredValue.Length < MinLength)
												retValue = false;
										}
									}
								}
								break;

							case 2: // Integer
							case 6: // List
							case 8: // User
								{
									int? enteredValue = null;
									if (cont is IntegerUpDown)
										enteredValue = ((IntegerUpDown)cont).Value;
									else if (cont is ComboBox)
									{
										object selectedItem = ((ComboBox)cont).SelectedItem;
										if (selectedItem != null)
										{
											if (selectedItem is RemoteUser)
											{
												enteredValue = ((RemoteUser)selectedItem).UserId;
											}
											else if (selectedItem is RemoteCustomListValue)
											{
												enteredValue = ((RemoteCustomListValue)selectedItem).CustomPropertyValueId;
											}
										}
									}

									//Required?
									if (prop.Options.Count(op => op.CustomPropertyOptionId == 1) == 1)
									{
										bool required = !this.getBoolFromValue(prop.Options.Single(op => op.CustomPropertyOptionId == 1));

										if (required && !enteredValue.HasValue)
											retValue = false;
									}

									//Max Value (only Integer)
									if (prop.Options.Count(op => op.CustomPropertyOptionId == 6) == 1 && prop.CustomPropertyTypeId == 2)
									{
										int? MaxValue = this.getIntFromValue(prop.Options.Single(op => op.CustomPropertyOptionId == 6));

										if (MaxValue.HasValue && MaxValue.Value > 0)
										{
											if (enteredValue.HasValue && enteredValue.Value > MaxValue)
												retValue = false;
										}
									}

									//Min Value (only Integer)
									if (prop.Options.Count(op => op.CustomPropertyOptionId == 7) == 1 && prop.CustomPropertyTypeId == 2)
									{
										int? MinValue = this.getIntFromValue(prop.Options.Single(op => op.CustomPropertyOptionId == 7));

										if (MinValue.HasValue && MinValue.Value > 0)
										{
											if (enteredValue.HasValue && enteredValue.Value < MinValue)
												retValue = false;
										}
									}
								}
								break;

							case 3: // Decimal
								{
									decimal? enteredValue = null;
									if (cont is DecimalUpDown)
										enteredValue = ((DecimalUpDown)cont).Value;

									//Required?
									if (prop.Options.Count(op => op.CustomPropertyOptionId == 1) == 1)
									{
										bool required = !this.getBoolFromValue(prop.Options.Single(op => op.CustomPropertyOptionId == 1));

										if (required && !enteredValue.HasValue)
											retValue = false;
									}

									//Max Value (only Integer)
									if (prop.Options.Count(op => op.CustomPropertyOptionId == 6) == 1 && prop.CustomPropertyTypeId == 2)
									{
										decimal? MaxValue = this.getDecimalFromValue(prop.Options.Single(op => op.CustomPropertyOptionId == 6));

										if (MaxValue.HasValue && MaxValue.Value > 0)
										{
											if (enteredValue.HasValue && enteredValue.Value > MaxValue)
												retValue = false;
										}
									}

									//Min Value (only Integer)
									if (prop.Options.Count(op => op.CustomPropertyOptionId == 7) == 1 && prop.CustomPropertyTypeId == 2)
									{
										decimal? MinValue = this.getDecimalFromValue(prop.Options.Single(op => op.CustomPropertyOptionId == 7));

										if (MinValue.HasValue && MinValue.Value > 0)
										{
											if (enteredValue.HasValue && enteredValue.Value < MinValue)
												retValue = false;
										}
									}
								}
								break;

							case 4: // Boolean
								//Boolean has no checks.
								break;

							case 5: // Date
								{
									DateTime? enteredValue = null;
									if (cont is DatePicker)
										enteredValue = ((DatePicker)cont).SelectedDate;

									//Required?
									if (prop.Options.Count(op => op.CustomPropertyOptionId == 1) == 1)
									{
										bool required = !this.getBoolFromValue(prop.Options.Single(op => op.CustomPropertyOptionId == 1));

										if (required && !enteredValue.HasValue)
											retValue = false;
									}
								}
								break;

							case 7: // Multilist
								{
									List<int> enteredValue = new List<int>();
									if (cont is ComboBox)
									{
										//Required?
										if (prop.Options.Count(op => op.CustomPropertyOptionId == 1) == 1)
										{
											bool required = !this.getBoolFromValue(prop.Options.Single(op => op.CustomPropertyOptionId == 1));

											if (required && ((ListBox)cont).SelectedItems.Count < 1)
												retValue = false;
										}
									}
								}
								break;
						}
					}
				}
			}

			return retValue;
		}

		/// <summary>Used to retrieve the values from the user.</summary>
		public List<RemoteArtifactCustomProperty> GetCustomProperties()
		{
			List<RemoteArtifactCustomProperty> retList = new List<RemoteArtifactCustomProperty>();

			//Load what the user entered..
			foreach (UIElement cont in this.grdContent.Children)
			{
				if (cont is Control)
				{
					if (((Control)cont).Tag is RemoteCustomProperty)
					{
						//Get the Custom Property definition.
						RemoteCustomProperty prop = ((RemoteCustomProperty)((Control)cont).Tag);
						//Now get the Artifact Custom Property defintiion.
						RemoteArtifactCustomProperty artProp = this.item.CustomProperties.Where(cp => cp.PropertyNumber == prop.PropertyNumber).SingleOrDefault();
						if (artProp == null)
						{
							artProp = new RemoteArtifactCustomProperty();
							artProp.Definition = prop;
							artProp.PropertyNumber = prop.PropertyNumber;
						}

						switch (prop.CustomPropertyTypeId)
						{
							case 1: // Text
							case 9: // URL
								if (cont is cntrlRichTextEditor)
									artProp.StringValue = ((cntrlRichTextEditor)cont).HTMLText;
								else if (cont is TextBox)
									artProp.StringValue = ((TextBox)cont).Text;
								break;

							case 2: // Integer
							case 6: // List
							case 8: // User
								int? newValue = null;
								if (cont is IntegerUpDown)
								{
									newValue = ((IntegerUpDown)cont).Value;
								}
								else if (cont is ComboBox)
								{
									if (prop.CustomPropertyTypeId == 6) // Re
									{
										if (((ComboBox)cont).SelectedItem != null)
										{
											newValue = ((RemoteCustomListValue)((ComboBox)cont).SelectedItem).CustomPropertyValueId;
										}
									}
									else if (prop.CustomPropertyTypeId == 8)
									{
										if (((ComboBox)cont).SelectedItem != null)
										{
											newValue = ((RemoteUser)((ComboBox)cont).SelectedItem).UserId;
										}
									}
								}

								artProp.IntegerValue = newValue;
								break;

							case 3: // Decimal
								artProp.DecimalValue = ((DecimalUpDown)cont).Value;
								break;

							case 4: // Boolean
								artProp.BooleanValue = ((CheckBox)cont).IsChecked;
								break;

							case 5: // Date
								if (((DatePicker)cont).SelectedDate.HasValue)
								{
									artProp.DateTimeValue = ((DatePicker)cont).SelectedDate.Value.ToUniversalTime();
								}
								break;

							case 7: // MultiList
								artProp.IntegerListValue = new List<int>();
								foreach (RemoteCustomListValue value in ((ListBox)cont).SelectedItems)
								{
									artProp.IntegerListValue.Add(value.CustomPropertyValueId.Value);
								}
								break;

						}

						retList.Add(artProp);
					}
				}
			}

			return retList;
		}
		#endregion
	}
}
