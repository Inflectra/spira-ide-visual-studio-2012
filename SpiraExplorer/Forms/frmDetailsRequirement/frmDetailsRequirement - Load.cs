using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Shapes;
using Inflectra.Global;
using Inflectra.SpiraTest.IDEIntegration.VisualStudio2010.Business;
using Inflectra.SpiraTest.IDEIntegration.VisualStudio2010.Business.SpiraTeam_Client;
using Inflectra.SpiraTest.IDEIntegration.VisualStudio2010.Controls;

namespace Inflectra.SpiraTest.IDEIntegration.VisualStudio2010.Forms
{
	public partial class frmDetailsRequirement : UserControl
	{
		#region Client Values
		private ImportExportClient _client;
		private int _clientNumRunning; //Holds the number current executing.
		private int _clientNum; //Holds the total amount. Needed to multiple ASYNC() calls.
		#endregion

		#region Private Data Storage Variables

		//The Project and the Incident
		private SpiraProject _Project = null;
		private RemoteRequirement _Requirement;

		//Other project-specific items.
		private List<RemoteProjectUser> _ProjUsers;
		private List<RemoteRelease> _ProjReleases;
		private List<RemoteDocument> _IncDocuments;
		private string _ReqDocumentsUrl;
		private string _RequirementUrl;

		#endregion

		/// <summary>Loads the item currently assigned to the ArtifactDetail property.</summary>
		/// <returns>Boolean on whether of not load was started successfully.</returns>
		private bool load_LoadItem()
		{
			try
			{
				bool retValue = false;
				if (this.ArtifactDetail != null)
				{
					//Clear the loading flag & dirty flags
					this._isDescChanged = false;
					this._isResChanged = false;
					this._isFieldChanged = false;
					this.btnSave.IsEnabled = false;

					//Set flag, reset vars..
					this.IsLoading = true;
					this.barLoadingIncident.Value = 0;
					this._ReqDocumentsUrl = null;
					this._RequirementUrl = null;

					//Create a client.
					this._client = null;
					this._client = StaticFuncs.CreateClient(((SpiraProject)this.ArtifactDetail.ArtifactParentProject.ArtifactTag).ServerURL.ToString());

					//Set client events.
					this._client.Connection_Authenticate2Completed += new EventHandler<Connection_Authenticate2CompletedEventArgs>(_client_Connection_Authenticate2Completed);
					this._client.Connection_ConnectToProjectCompleted += new EventHandler<Connection_ConnectToProjectCompletedEventArgs>(_client_Connection_ConnectToProjectCompleted);
					this._client.Connection_DisconnectCompleted += new EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(_client_Connection_DisconnectCompleted);
					this._client.Document_RetrieveForArtifactCompleted += new EventHandler<Document_RetrieveForArtifactCompletedEventArgs>(_client_Document_RetrieveForArtifactCompleted);
					this._client.Release_RetrieveCompleted += new EventHandler<Release_RetrieveCompletedEventArgs>(_client_Release_RetrieveCompleted);
					this._client.Project_RetrieveUserMembershipCompleted += new EventHandler<Project_RetrieveUserMembershipCompletedEventArgs>(_client_Project_RetrieveUserMembershipCompleted);
					this._client.System_GetArtifactUrlCompleted += new EventHandler<System_GetArtifactUrlCompletedEventArgs>(_client_System_GetArtifactUrlCompleted);
					this._client.Requirement_RetrieveByIdCompleted += new EventHandler<Requirement_RetrieveByIdCompletedEventArgs>(_client_Requirement_RetrieveByIdCompleted);
					this._client.Requirement_RetrieveCommentsCompleted += new EventHandler<Requirement_RetrieveCommentsCompletedEventArgs>(_client_Requirement_RetrieveCommentsCompleted);
					this._client.Task_RetrieveCompleted += new EventHandler<Task_RetrieveCompletedEventArgs>(_client_Task_RetrieveCompleted);
					this._client.CustomProperty_RetrieveForArtifactTypeCompleted += new EventHandler<CustomProperty_RetrieveForArtifactTypeCompletedEventArgs>(_client_CustomProperty_RetrieveForArtifactTypeCompleted);

					//Fire the connection off here.
					this._clientNumRunning++;
					this.barLoadingIncident.Maximum = 9;
					this._client.Connection_Authenticate2Async(this._Project.UserName, this._Project.UserPass, StaticFuncs.getCultureResource.GetString("app_ReportName"), this._clientNum++);

				}

				return retValue;
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "load_LoadItem()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
		}

		#region Client Events
		/// <summary>Hit once we've disconnected form the server, all work is done.</summary>
		/// <param name="sender">ImportExporClient</param>
		/// <param name="e">AsyncCompletedEventArgs</param>
		private void _client_Connection_DisconnectCompleted(object sender, AsyncCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "_client_Connection_DisconnectCompleted()";
				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " ENTER. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());

				this._clientNumRunning = 0;
				this._clientNum = 0;
				this._client = null;

				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " EXIT. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "_client_Connection_DisconnectCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when we've successfully connected to the server.</summary>
		/// <param name="sender">ImportExporClient</param>
		/// <param name="e">Connection_Authenticate2CompletedEventArgs</param>
		private void _client_Connection_Authenticate2Completed(object sender, Connection_Authenticate2CompletedEventArgs e)
		{
			try
			{
				const string METHOD = "_client_Connection_Authenticate2Completed()";
				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " ENTER. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());

				this._clientNumRunning--;
				this.barLoadingIncident.Value++;

				if (!e.Cancelled)
				{
					if (e.Error == null && e.Result)
					{
						//Connect to our project.
						this._client.Connection_ConnectToProjectAsync(((SpiraProject)this._ArtifactDetails.ArtifactParentProject.ArtifactTag).ProjectID, this._clientNum++);
						this._clientNumRunning++;
					}
					else
					{
						if (e.Error != null)
						{
							Logger.LogMessage(e.Error);

							//Display the error panel.
							this.display_ShowErrorPanel(
								StaticFuncs.getCultureResource.GetString("app_General_TalkingToServerErrorMessage") +
								Environment.NewLine +
								StaticFuncs.getCultureResource.GetString("app_General_TalkingToServerErrorMessageDetails") +
								Environment.NewLine +
								e.Error.Message.Truncate(250, Strings.TruncateOptionsEnum.AllowLastWordToGoOverMaxLength & Strings.TruncateOptionsEnum.FinishWord & Strings.TruncateOptionsEnum.IncludeEllipsis));
						}
						else
						{
							Logger.LogMessage("Could not log in!");
							//Display the error panel.
							this.display_ShowErrorPanel(
								StaticFuncs.getCultureResource.GetString("app_General_TalkingToServerErrorMessage") +
								Environment.NewLine +
								StaticFuncs.getCultureResource.GetString("app_General_TalkingToServerErrorMessageDetails") +
								Environment.NewLine +
								StaticFuncs.getCultureResource.GetString("app_General_InvalidUsernameOrPassword"));
						}
						this._client.Connection_DisconnectAsync();
					}
				}

				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " EXIT. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "_client_Connection_Authenticate2Completed()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when we've completed connecting to the project. </summary>
		/// <param name="sender">ImportExporClient</param>
		/// <param name="e">Connection_ConnectToProjectCompletedEventArgs</param>
		private void _client_Connection_ConnectToProjectCompleted(object sender, Connection_ConnectToProjectCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "_client_Connection_ConnectToProjectCompleted()";
				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " ENTER. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());

				this._clientNumRunning--;
				this.barLoadingIncident.Value++;

				if (!e.Cancelled)
				{
					if (e.Error == null && e.Result)
					{
						this._clientNumRunning += 8;
						//Here we need to fire off all data retrieval functions:
						// - Project users.
						this._client.Project_RetrieveUserMembershipAsync(this._clientNum++);
						// - Custom Properties
						this._client.CustomProperty_RetrieveForArtifactTypeAsync(1,false, this._clientNum++);
						// - Available Releases
						this._client.Release_RetrieveAsync(true, this._clientNum++);
						// - Resolutions / Comments
						this._client.Requirement_RetrieveCommentsAsync(this.ArtifactDetail.ArtifactId, this._clientNum++);
						// - System URL
						this._client.System_GetArtifactUrlAsync(-14, this._Project.ProjectID, -2, null, this._clientNum++);
						// - Get the Requirement.
						this._client.Requirement_RetrieveByIdAsync(this._ArtifactDetails.ArtifactId, this._clientNum++);
						// - Get documents for the item.
						this._client.Document_RetrieveForArtifactAsync(1, this._ArtifactDetails.ArtifactId, new List<RemoteFilter> { }, new RemoteSort(), this._clientNum++);
						// - Get the linked tasks.
						this._client.Task_RetrieveAsync(
							new List<RemoteFilter> { new RemoteFilter() { IntValue = this._ArtifactDetails.ArtifactId, PropertyName = "RequirementId" } },
							new RemoteSort() { PropertyName = "StartDate", SortAscending = true },
							1,
							999999,
							this._clientNum++);
					}
					else
					{
						if (e.Error != null)
						{
							Logger.LogMessage(e.Error);
							this._client.Connection_DisconnectAsync();

							//Display the error panel.
							this.display_ShowErrorPanel(
								StaticFuncs.getCultureResource.GetString("app_General_TalkingToServerErrorMessage") +
								Environment.NewLine +
								StaticFuncs.getCultureResource.GetString("app_General_TalkingToServerErrorMessageDetails") +
								Environment.NewLine +
								e.Error.Message.Truncate(250, Strings.TruncateOptionsEnum.AllowLastWordToGoOverMaxLength & Strings.TruncateOptionsEnum.FinishWord & Strings.TruncateOptionsEnum.IncludeEllipsis));
						}
						else
						{
							Logger.LogMessage("Could not access project!");
							//Display the error panel.
							this.display_ShowErrorPanel(
								StaticFuncs.getCultureResource.GetString("app_General_TalkingToServerErrorMessage") +
								Environment.NewLine +
								StaticFuncs.getCultureResource.GetString("app_General_TalkingToServerErrorMessageDetails") +
								Environment.NewLine +
								StaticFuncs.getCultureResource.GetString("app_General_InvalidProject"));
						}
					}
				}

				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " EXIT. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "_client_Connection_ConnectToProjectCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when we're finished getting our project users.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">Project_RetrieveUserMembershipCompletedEventArgs</param>
		private void _client_Project_RetrieveUserMembershipCompleted(object sender, Project_RetrieveUserMembershipCompletedEventArgs e)
		{
			const string METHOD = "_client_Project_RetrieveUserMembershipCompleted()";
			System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " ENTER. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());

			this._clientNumRunning--;
			this.barLoadingIncident.Value++;

			if (!e.Cancelled)
			{
				if (e.Error == null)
				{
					this._ProjUsers = e.Result;
					//See if we're ready to get the actual data.
					this.load_IsReadyToDisplayData();
				}
				else
				{
					Logger.LogMessage(e.Error);
					this._client.Connection_DisconnectAsync();

					//Display the error panel.
					this.display_ShowErrorPanel(
						StaticFuncs.getCultureResource.GetString("app_General_TalkingToServerErrorMessage") +
						Environment.NewLine +
						StaticFuncs.getCultureResource.GetString("app_General_TalkingToServerErrorMessageDetails") +
						Environment.NewLine +
						e.Error.Message.Truncate(250, Strings.TruncateOptionsEnum.AllowLastWordToGoOverMaxLength & Strings.TruncateOptionsEnum.FinishWord & Strings.TruncateOptionsEnum.IncludeEllipsis));
				}
			}

			System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " EXIT. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());
		}

		/// <summary>Hit when we're finished getting our project releases.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">Release_RetrieveCompletedEventArgs</param>
		private void _client_Release_RetrieveCompleted(object sender, Release_RetrieveCompletedEventArgs e)
		{
			const string METHOD = "_client_Release_RetrieveCompleted()";
			System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " ENTER. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());

			this._clientNumRunning--;
			this.barLoadingIncident.Value++;

			if (!e.Cancelled)
			{
				if (e.Error == null)
				{
					this._ProjReleases = e.Result;
					//See if we're ready to get the actual data.
					this.load_IsReadyToDisplayData();
				}
				else
				{
					Logger.LogMessage(e.Error);
					this._client.Connection_DisconnectAsync();

					//Display the error panel.
					this.display_ShowErrorPanel(
						StaticFuncs.getCultureResource.GetString("app_General_TalkingToServerErrorMessage") +
						Environment.NewLine +
						StaticFuncs.getCultureResource.GetString("app_General_TalkingToServerErrorMessageDetails") +
						Environment.NewLine +
						e.Error.Message.Truncate(250, Strings.TruncateOptionsEnum.AllowLastWordToGoOverMaxLength & Strings.TruncateOptionsEnum.FinishWord & Strings.TruncateOptionsEnum.IncludeEllipsis));
				}
			}

			System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " EXIT. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());
		}

		/// <summary>Hit when the client finishes getting the custom field values.</summary>
		/// <param name="sender">ImportExaoprtClient</param>
		/// <param name="e">CustomProperty_RetrieveForArtifactTypeCompletedEventArgs</param>
		private void _client_CustomProperty_RetrieveForArtifactTypeCompleted(object sender, CustomProperty_RetrieveForArtifactTypeCompletedEventArgs e)
		{
			const string METHOD = "_client_CustomProperty_RetrieveForArtifactTypeCompleted()";
			System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " ENTER. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());

			this._clientNumRunning--;
			this.barLoadingIncident.Value++;

			if (!e.Cancelled)
			{
				if (e.Error == null)
				{
					//Here create the grid to hold the data.
					this.gridCustomProperties.Children.Clear();
					this.gridCustomProperties.RowDefinitions.Clear();
					for (int i = 0; i < Math.Ceiling(e.Result.Count / 2D); i++)
						this.gridCustomProperties.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

					//Here, create the contols..
					bool IsOnFirst = true;
					for (int j = 0; j < e.Result.Count; j++)
					{
						//** The label first.
						TextBlock lblCustProp = new TextBlock();
						lblCustProp.Text = e.Result[j].Alias + ":";
						lblCustProp.Style = (Style)this.FindResource("PaddedLabel");
						lblCustProp.VerticalAlignment = System.Windows.VerticalAlignment.Top;

						//Add it to the row/column.
						Grid.SetColumn(lblCustProp, ((IsOnFirst) ? 0 : 3));
						Grid.SetRow(lblCustProp, (int)Math.Floor(j / 2D));
						//Add it to the grid.
						this.gridCustomProperties.Children.Add(lblCustProp);

						//** Now the control.
						Control custControl = null;
						if (e.Result[j].CustomPropertyTypeId == 1) //Text field.
						{
							TextBox txtControl = new TextBox();
							txtControl.AcceptsReturn = true;
							txtControl.AcceptsTab = true;
							txtControl.MaxLines = 2;
							txtControl.MinLines = 2;
							txtControl.TextChanged += new TextChangedEventHandler(_cntrl_TextChanged);
							custControl = txtControl;
						}
						else if (e.Result[j].CustomPropertyTypeId == 2) //List field.
						{
							ComboBox lsbControl = new ComboBox();
							lsbControl.SelectedValuePath = "Key";
							lsbControl.DisplayMemberPath = "Value";
							lsbControl.SelectionChanged += new SelectionChangedEventHandler(_cntrl_TextChanged);

							//Load selectable items.
							lsbControl.Items.Add(new KeyValuePair<int, string>(-1, ""));
							foreach (RemoteCustomListValue list in e.Result[j].CustomList.Values)
							{
								KeyValuePair<int, string> item = new KeyValuePair<int, string>(list.CustomPropertyValueId.Value, list.Name);
								lsbControl.Items.Add(item);
							}

							custControl = lsbControl;
						}
						custControl.Style = (Style)this.FindResource("PaddedControl");
						custControl.Tag = e.Result[j];
						custControl.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
						custControl.VerticalAlignment = System.Windows.VerticalAlignment.Top;
						//Add it to the row/column.
						Grid.SetColumn(custControl, ((IsOnFirst) ? 1 : 4));
						Grid.SetRow(custControl, (int)Math.Floor(j / 2D));
						//Add it to the grid.
						this.gridCustomProperties.Children.Add(custControl);

						//Flip the IsOnFirst..
						IsOnFirst = !IsOnFirst;
					}

					//See if we're ready to get the actual data.
					this.load_IsReadyToDisplayData();
				}
				else
				{
					Logger.LogMessage(e.Error);
					this._client.Connection_DisconnectAsync();

					//Display the error panel.
					this.display_ShowErrorPanel(
						StaticFuncs.getCultureResource.GetString("app_General_TalkingToServerErrorMessage") +
						Environment.NewLine +
						StaticFuncs.getCultureResource.GetString("app_General_TalkingToServerErrorMessageDetails") +
						Environment.NewLine +
						e.Error.Message.Truncate(250, Strings.TruncateOptionsEnum.AllowLastWordToGoOverMaxLength & Strings.TruncateOptionsEnum.FinishWord & Strings.TruncateOptionsEnum.IncludeEllipsis));
				}
			}

			System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " EXIT. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());
		}

		/// <summary>Hit when we're finished getting the attached documents for the artifact.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">Document_RetrieveForArtifactCompletedEventArgs</param>
		private void _client_Document_RetrieveForArtifactCompleted(object sender, Document_RetrieveForArtifactCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "_client_Document_RetrieveForArtifactCompleted()";
				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " ENTER. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());

				this._clientNumRunning--;
				this.barLoadingIncident.Value++;

				if (!e.Cancelled)
				{
					if (e.Error == null)
					{
						//Get the results into our variable.
						this._IncDocuments = e.Result;
						//We won't load them into display until the other information is displayed.

						this.load_IsReadyToDisplayData();
					}
					else
					{
						Logger.LogMessage(e.Error);
					}
				}
				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " EXIT. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "_client_Document_RetrieveForArtifactCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when the client returns with our needed URL</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">System_GetArtifactUrlCompletedEventArgs</param>
		private void _client_System_GetArtifactUrlCompleted(object sender, System_GetArtifactUrlCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "_client_System_GetArtifactUrlCompleted()";
				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " ENTER. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());

				this._clientNumRunning--;
				this.barLoadingIncident.Value++;

				if (!e.Cancelled)
				{
					if (e.Error == null)
					{
						if (string.IsNullOrWhiteSpace(this._ReqDocumentsUrl))
						{
							this._ReqDocumentsUrl = e.Result;
							this._clientNumRunning++;
							//Get the other link now.
							this._client.System_GetArtifactUrlAsync(1, this._ArtifactDetails.ArtifactParentProject.ArtifactId, this._ArtifactDetails.ArtifactId, null, this._clientNum++);
						}
						else
						{
							this._RequirementUrl = e.Result.Replace("~", this._Project.ServerURL.ToString());
						}
						this.load_IsReadyToDisplayData();

					}
					else
					{
						Logger.LogMessage(e.Error);
						this._ReqDocumentsUrl = "--none--";
					}

					System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " EXIT. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());
				}
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "_client_System_GetArtifactUrlCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when the client's finished getting the comments for the artifact.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">Requirement_RetrieveCommentsCompletedEventArgs</param>
		private void _client_Requirement_RetrieveCommentsCompleted(object sender, Requirement_RetrieveCommentsCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "_client_Requirement_RetrieveCommentsCompleted()";
				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " ENTER. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());

				this._clientNumRunning--;
				this.barLoadingIncident.Value++;

				if (!e.Cancelled)
				{
					if (e.Error == null)
					{
						//Load the comments.
						this.loadItem_PopulateDiscussion(e.Result);

						//See if we're ready to get the actual data.
						this.load_IsReadyToDisplayData();
					}
					else
					{
						Logger.LogMessage(e.Error);
						this._client.Connection_DisconnectAsync();

						//Display the error panel.
						this.display_ShowErrorPanel(
							StaticFuncs.getCultureResource.GetString("app_General_TalkingToServerErrorMessage") +
							Environment.NewLine +
							StaticFuncs.getCultureResource.GetString("app_General_TalkingToServerErrorMessageDetails") +
							Environment.NewLine +
							e.Error.Message.Truncate(250, Strings.TruncateOptionsEnum.AllowLastWordToGoOverMaxLength & Strings.TruncateOptionsEnum.FinishWord & Strings.TruncateOptionsEnum.IncludeEllipsis));
					}
				}

				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " EXIT. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "_client_Requirement_RetrieveCommentsCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when the client is finished getting the requirement.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">Requirement_RetrieveByIdCompletedEventArgs</param>
		private void _client_Requirement_RetrieveByIdCompleted(object sender, Requirement_RetrieveByIdCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "_client_Requirement_RetrieveByIdCompleted()";
				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " ENTER. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());

				this._clientNumRunning--;
				this.barLoadingIncident.Value++;

				if (!e.Cancelled)
				{
					if (e.Error == null)
					{
						//Load the requirement.
						this._Requirement = e.Result;

						//See if we're ready to get the actual data.
						this.load_IsReadyToDisplayData();
					}
					else
					{
						Logger.LogMessage(e.Error);
						this._client.Connection_DisconnectAsync();

						//Display the error panel.
						this.display_ShowErrorPanel(
							StaticFuncs.getCultureResource.GetString("app_General_TalkingToServerErrorMessage") +
							Environment.NewLine +
							StaticFuncs.getCultureResource.GetString("app_General_TalkingToServerErrorMessageDetails") +
							Environment.NewLine +
							e.Error.Message.Truncate(250, Strings.TruncateOptionsEnum.AllowLastWordToGoOverMaxLength & Strings.TruncateOptionsEnum.FinishWord & Strings.TruncateOptionsEnum.IncludeEllipsis));
					}
				}

				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " EXIT. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "_client_Requirement_RetrieveByIdCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when the client is finished getting a list of associated tasks.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">Task_RetrieveCompletedEventArgs</param>
		private void _client_Task_RetrieveCompleted(object sender, Task_RetrieveCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "_client_Release_RetrieveCompleted()";
				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " ENTER. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());

				this._clientNumRunning--;
				this.barLoadingIncident.Value++;

				if (!e.Cancelled)
				{
					if (e.Error == null)
					{
						//Load up the grid..
						this.loadItem_PopulateLinkedTasks(e.Result);

						//See if we're ready to get the actual data.
						this.load_IsReadyToDisplayData();
					}
					else
					{
						Logger.LogMessage(e.Error);
						this._client.Connection_DisconnectAsync();

						//Display the error panel.
						this.display_ShowErrorPanel(
							StaticFuncs.getCultureResource.GetString("app_General_TalkingToServerErrorMessage") +
							Environment.NewLine +
							StaticFuncs.getCultureResource.GetString("app_General_TalkingToServerErrorMessageDetails") +
							Environment.NewLine +
							e.Error.Message.Truncate(250, Strings.TruncateOptionsEnum.AllowLastWordToGoOverMaxLength & Strings.TruncateOptionsEnum.FinishWord & Strings.TruncateOptionsEnum.IncludeEllipsis));
					}
				}

				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " EXIT. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "_client_Task_RetrieveCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		#endregion

		/// <summary>Checks to see if it's okay to start loading form data.
		/// Called after the two Workflow data retrieval Asyncs.
		/// </summary>
		private void load_IsReadyToDisplayData()
		{
			try
			{
				Logger.LogTrace("load_IsReadyToDisplayData: Clients Running " + this._clientNumRunning.ToString());

				if (this._clientNumRunning == 0)
				{
					this.loadItem_DisplayInformation(this._Requirement);

					//Turn off loading screen..
					this.IsLoading = false;
				}
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "load_IsReadyToDisplayData()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		#region Form Load Functions
		/// <summary>Loads the list of available users.</summary>
		/// <param name="box">The ComboBox to load users from.</param>
		/// <param name="SelectedUserID">The user to select.</param>
		private void loadItem_PopulateUser(ComboBox box, int? SelectedUserID)
		{
			try
			{
				//Clear and add our 'none'.
				box.Items.Clear();
				box.SelectedIndex = box.Items.Add(new RemoteProjectUser() { FullName = StaticFuncs.getCultureResource.GetString("app_General_None") });

				if (!SelectedUserID.HasValue)
					SelectedUserID = -1;

				//Load the project users.
				foreach (Business.SpiraTeam_Client.RemoteProjectUser projUser in this._ProjUsers)
				{
					int numAdded = box.Items.Add(projUser);
					if (projUser.UserId == SelectedUserID)
					{
						box.SelectedIndex = numAdded;
					}
				}
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "Populating user.");
			}
		}

		/// <summary>Loads the list of available releases into the specified ComboBox.</summary>
		/// <param name="box">The ComboBox to load users from.</param>
		/// <param name="SelectedUserID">The releaseId to select.</param>
		private void loadItem_PopulateReleaseControl(ComboBox box, int? SelectedRelease)
		{
			try
			{
				//Clear and add our 'none'.
				box.Items.Clear();
				box.SelectedIndex = box.Items.Add(new RemoteRelease() { Name = StaticFuncs.getCultureResource.GetString("app_General_None") });

				if (!SelectedRelease.HasValue)
					SelectedRelease = -1;

				foreach (Business.SpiraTeam_Client.RemoteRelease Release in this._ProjReleases)
				{
					int numAdded = box.Items.Add(Release);
					if (Release.ReleaseId == SelectedRelease)
					{
						box.SelectedIndex = numAdded;
					}
				}
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "loadItem_PopulateReleaseControl()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Loads the listed discussions into the discussion control.</summary>
		/// <param name="Discussions">The list of COmments to add.</param>
		private void loadItem_PopulateDiscussion(List<RemoteComment> Discussions)
		{
			try
			{
				//Erase ones in there.
				this.cntrlDiscussion.Children.Clear();

				if (Discussions.Count < 1)
					this.cntrlDiscussion.Children.Add(new cntlDiscussionFrame("No comments for this item.", ""));
				else
				{
					foreach (RemoteComment Resolution in Discussions)
					{
						string header = Resolution.UserName + " [" + Resolution.CreationDate.Value.ToShortDateString() + " " + Resolution.CreationDate.Value.ToShortTimeString() + "]";
						this.cntrlDiscussion.Children.Add(new cntlDiscussionFrame(header, Resolution.Text));
					}
				}

				//Clear the entry box.
				this.cntrlResolution.HTMLText = "";
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "loadItem_PopulateDiscussion()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Selects the given ID in the Priority Control</summary>
		/// <param name="PriorityId">Integer of the selected priority.</param>
		private void loadItem_SelectPriority(int? PriorityId)
		{
			try
			{
				foreach (RequirementPriority availPri in this.cntrlImportance.Items)
				{
					if (availPri.PriorityId == PriorityId)
					{
						this.cntrlImportance.SelectedItem = availPri;
					}
				}
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "loadItem_SelectPriority()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Selects the given ID in the Status Control</summary>
		/// <param name="PriorityId">Integer of the selected status.</param>
		private void loadItem_SelectStatus(int? StatusId)
		{
			foreach (RequirementStatus availSta in this.cntrlStatus.Items)
			{
				if (availSta.StatusId == StatusId)
				{
					this.cntrlStatus.SelectedItem = availSta;
				}
			}
		}

		/// <summary>Loads the list of assigned tasks into the grid.</summary>
		/// <param name="listTasks">The list of Tasks</param>
		private void loadItem_PopulateLinkedTasks(List<RemoteTask> listTasks)
		{
			try
			{
				const string METHOD = "loadItem_PopulateLinkedTasks()";

				//Remove existing rows.
				try
				{
					int intChildTries = 0;
					while (this.gridTasks.Children.Count > 7 && intChildTries < 10000)
					{
						this.gridTasks.Children.RemoveAt(7);
						intChildTries++;
					}

					//Remove rows.
					int intRowTries = 0;
					while (this.gridTasks.RowDefinitions.Count > 1 && intRowTries < 10000)
					{
						this.gridTasks.RowDefinitions.RemoveAt(1);
						intRowTries++;
					}
				}
				catch (Exception ex)
				{
					Logger.LogMessage(ex, CLASS + METHOD + " Clearing Attachments Grid");
				}

				//Add new rows.
				if (listTasks != null)
				{
					foreach (RemoteTask reqTask in listTasks)
					{
						int numAdding = this.gridTasks.RowDefinitions.Count;

						//Create the TreeViewrtifact to put on the HyperLink.
						TreeViewArtifact artProject = new TreeViewArtifact();
						artProject.ArtifactId = this._ArtifactDetails.ArtifactParentProject.ArtifactId;
						artProject.ArtifactIsFolder = this._ArtifactDetails.ArtifactParentProject.ArtifactIsFolder;
						artProject.ArtifactIsFolderMine = this._ArtifactDetails.ArtifactParentProject.ArtifactIsFolderMine;
						artProject.ArtifactIsNo = this._ArtifactDetails.ArtifactParentProject.ArtifactIsNo;
						artProject.ArtifactName = this._ArtifactDetails.ArtifactParentProject.ArtifactName;
						artProject.ArtifactTag = this._ArtifactDetails.ArtifactParentProject.ArtifactTag;
						artProject.ArtifactType = TreeViewArtifact.ArtifactTypeEnum.Project;
						artProject.Items = new List<object>();

						TreeViewArtifact artTask = new TreeViewArtifact();
						artTask.Parent = artProject;
						artTask.ArtifactId = reqTask.TaskId.Value;
						artTask.ArtifactName = reqTask.Name;
						artTask.ArtifactTag = reqTask;
						artTask.ArtifactType = TreeViewArtifact.ArtifactTypeEnum.Task;

						artProject.Items.Add(artTask);

						//Create textblocks..
						// - Image
						Image imgTaskIcon = StaticFuncs.getImage("imgTask", new Size(16, 16));

						// - Link/Name
						TextBlock txtTaskName = new TextBlock();
						Hyperlink linkFile = new Hyperlink();
						linkFile.Inlines.Add("[TK:" + reqTask.TaskId.ToString() + "] ");
						linkFile.Inlines.Add(reqTask.Name);
						linkFile.Tag = artTask;

						linkFile.Click += new RoutedEventHandler(Hyperlink_OpenTask_Click);

						txtTaskName.Inlines.Add(linkFile);

						//Create ToolTip.
						TreeViewArtifact tskTrv = new TreeViewArtifact();
						tskTrv.ArtifactId = reqTask.TaskId.Value;
						tskTrv.ArtifactName = reqTask.Name;
						tskTrv.ArtifactTag = reqTask;
						tskTrv.ArtifactType = TreeViewArtifact.ArtifactTypeEnum.Task;
						tskTrv.Parent = this._ArtifactDetails;
						txtTaskName.ToolTip = new Business.Forms.cntlTTipTask(tskTrv);
						txtTaskName.Style = (Style)this.FindResource("PaddedLabel");
						txtTaskName.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;

						// - Task Progress
						Grid gridProgress = new Grid();
						gridProgress.MinWidth = 100;
						ProgressBar barProgress = new ProgressBar();
						barProgress.Minimum = 0;
						barProgress.Maximum = 100;
						barProgress.Value = reqTask.CompletionPercent;
						barProgress.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
						barProgress.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
						barProgress.MaxHeight = 20;
						barProgress.Margin = new Thickness(0, 2, 0, 2);
						Grid.SetColumn(barProgress, 0);
						Grid.SetRow(barProgress, 0);
						gridProgress.Children.Add(barProgress);

						TextBlock txtProgress = new TextBlock();
						txtProgress.Text = reqTask.CompletionPercent.ToString() + "%";
						txtProgress.Style = (Style)this.FindResource("PaddedLabel");
						txtProgress.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
						txtProgress.VerticalAlignment = System.Windows.VerticalAlignment.Center;
						Grid.SetColumn(txtProgress, 0);
						Grid.SetRow(txtProgress, 0);
						gridProgress.Children.Add(txtProgress);

						// - Status
						TextBlock txbStatus = new TextBlock();
						txbStatus.Text = reqTask.TaskStatusId.ToString() + " - " + reqTask.TaskStatusName;
						txbStatus.Style = (Style)this.FindResource("PaddedLabel");

						// - Importance
						TextBlock txbImportance = new TextBlock();
						txbImportance.Text = reqTask.TaskPriorityName;
						txbImportance.Style = (Style)this.FindResource("PaddedLabel");

						// - Owner
						TextBlock txbOwner = new TextBlock();
						txbOwner.Text = reqTask.OwnerName;
						txbOwner.Style = (Style)this.FindResource("PaddedLabel");

						//Create the row, and add the controls to it.
						this.gridTasks.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
						Grid.SetColumn(imgTaskIcon, 0);
						Grid.SetRow(imgTaskIcon, numAdding);
						this.gridTasks.Children.Add(imgTaskIcon);
						Grid.SetColumn(txtTaskName, 1);
						Grid.SetRow(txtTaskName, numAdding);
						this.gridTasks.Children.Add(txtTaskName);
						Grid.SetColumn(gridProgress, 2);
						Grid.SetRow(gridProgress, numAdding);
						this.gridTasks.Children.Add(gridProgress);
						Grid.SetColumn(txbStatus, 3);
						Grid.SetRow(txbStatus, numAdding);
						this.gridTasks.Children.Add(txbStatus);
						Grid.SetColumn(txbImportance, 4);
						Grid.SetRow(txbImportance, numAdding);
						this.gridTasks.Children.Add(txbImportance);
						Grid.SetColumn(txbOwner, 5);
						Grid.SetRow(txbOwner, numAdding);
						this.gridTasks.Children.Add(txbOwner);
					}

					//Now create the background rectangle..
					Rectangle rectBackg = new Rectangle();
					rectBackg.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
					rectBackg.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
					rectBackg.Margin = new Thickness(0);
					rectBackg.Fill = this.rectTitleBar.Fill;
					Grid.SetColumn(rectBackg, 0);
					Grid.SetRow(rectBackg, 1);
					Grid.SetColumnSpan(rectBackg, 7);
					Grid.SetRowSpan(rectBackg, this.gridTasks.RowDefinitions.Count);
					Panel.SetZIndex(rectBackg, -100);
					this.gridTasks.Children.Insert(6, rectBackg);
				}
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "loadItem_PopulateLinkedTasks()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		#endregion













		/// <summary>Load the specified task into the data fields.</summary>
		/// <param name="task">The task details to load into fields.</param>
		private void loadItem_DisplayInformation(RemoteRequirement requirement)
		{
			try
			{
				const string METHOD = "loadItem_DisplayInformation()";

				try
				{
					#region Base Fields
					// - Name & ID
					this.cntrlName.Text = requirement.Name;
					this.lblToken.Text = this._ArtifactDetails.ArtifactIDDisplay;
					this.cntrlResolution.HTMLText = "";

					// - Users
					this.loadItem_PopulateUser(this.cntrlCreatedBy, requirement.AuthorId);
					this.loadItem_PopulateUser(this.cntrlOwnedBy, requirement.OwnerId);
					this.cntrlCreatedBy.Items.RemoveAt(0);

					// - Releases
					this.loadItem_PopulateReleaseControl(this.cntrlRelease, requirement.ReleaseId);

					// - Priority & Severity
					this.loadItem_SelectPriority(requirement.ImportanceId);
					this.loadItem_SelectStatus(requirement.StatusId);

					// - Planned Effort
					this.cntrlPlnEffortH.Text = ((requirement.PlannedEffort.HasValue) ? Math.Floor(((double)requirement.PlannedEffort / (double)60)).ToString() : "0");
					this.cntrlPlnEffortM.Text = ((requirement.PlannedEffort.HasValue) ? ((double)requirement.PlannedEffort % (double)60).ToString() : "0");

					// - Description
					this.cntrlDescription.HTMLText = requirement.Description;
					#endregion

					#region History
					//TODO: History (need API update)
					#endregion

					#region Attachments
					//Remove existing rows.
					try
					{
						int intChildTries = 0;
						while (this.gridAttachments.Children.Count > 7 && intChildTries < 10000)
						{
							this.gridAttachments.Children.RemoveAt(7);
							intChildTries++;
						}

						//Remove rows.
						int intRowTries = 0;
						while (this.gridAttachments.RowDefinitions.Count > 1 && intRowTries < 10000)
						{
							this.gridAttachments.RowDefinitions.RemoveAt(1);
							intRowTries++;
						}
					}
					catch (Exception ex)
					{
						Logger.LogMessage(ex, CLASS + METHOD + " Clearing Attachments Grid");
					}
					//Add new rows.
					if (this._IncDocuments != null)
					{
						foreach (RemoteDocument incidentAttachment in this._IncDocuments)
						{
							int numAdding = this.gridAttachments.RowDefinitions.Count;
							//Create textblocks..
							// - Link/Name
							TextBlock txbFilename = new TextBlock();
							Hyperlink linkFile = new Hyperlink();
							linkFile.Inlines.Add(incidentAttachment.FilenameOrUrl);
							//Try to get a URL out of it..
							bool IsUrl = false;
							Uri atchUri = null;
							try
							{
								atchUri = new Uri(incidentAttachment.FilenameOrUrl);
								IsUrl = true;
							}
							catch { }

							if (!IsUrl)
							{
								try
								{
									atchUri = new Uri(this._ReqDocumentsUrl.Replace("~", this._Project.ServerURL.ToString()).Replace("{art}", incidentAttachment.AttachmentId.ToString()));
								}
								catch { }
							}
							linkFile.NavigateUri = atchUri;
							linkFile.Click += new RoutedEventHandler(Hyperlink_Click);

							//Add the link to the TextBlock.
							txbFilename.Inlines.Add(linkFile);
							//Create ToolTip.
							txbFilename.ToolTip = new cntrlRichTextEditor() { IsReadOnly = true, IsToolbarVisible = false, HTMLText = incidentAttachment.Description, Width = 200 };
							txbFilename.Style = (Style)this.FindResource("PaddedLabel");

							// - Document Version
							TextBlock txbVersion = new TextBlock();
							txbVersion.Text = incidentAttachment.CurrentVersion;
							txbVersion.Style = (Style)this.FindResource("PaddedLabel");

							// - Author
							TextBlock txbAuthor = new TextBlock();
							txbAuthor.Text = incidentAttachment.AuthorName;
							txbAuthor.Style = (Style)this.FindResource("PaddedLabel");

							// - Date Created
							TextBlock txbDateCreated = new TextBlock();
							txbDateCreated.Text = incidentAttachment.UploadDate.ToShortDateString();
							txbDateCreated.Style = (Style)this.FindResource("PaddedLabel");

							// - Size
							TextBlock txbSize = new TextBlock();
							txbSize.Text = incidentAttachment.Size.ToString() + "kb";
							txbSize.Style = (Style)this.FindResource("PaddedLabel");

							//Create the row, and add the controls to it.
							gridAttachments.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
							Grid.SetColumn(txbFilename, 0);
							Grid.SetRow(txbFilename, numAdding);
							gridAttachments.Children.Add(txbFilename);
							Grid.SetColumn(txbVersion, 1);
							Grid.SetRow(txbVersion, numAdding);
							gridAttachments.Children.Add(txbVersion);
							Grid.SetColumn(txbAuthor, 2);
							Grid.SetRow(txbAuthor, numAdding);
							gridAttachments.Children.Add(txbAuthor);
							Grid.SetColumn(txbDateCreated, 3);
							Grid.SetRow(txbDateCreated, numAdding);
							gridAttachments.Children.Add(txbDateCreated);
							Grid.SetColumn(txbSize, 4);
							Grid.SetRow(txbSize, numAdding);
							gridAttachments.Children.Add(txbSize);
						}

						//Now create the background rectangle..
						Rectangle rectBackg = new Rectangle();
						rectBackg.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
						rectBackg.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
						rectBackg.Margin = new Thickness(0);
						rectBackg.Fill = this.rectTitleBar.Fill;
						Grid.SetColumn(rectBackg, 0);
						Grid.SetRow(rectBackg, 1);
						Grid.SetColumnSpan(rectBackg, 7);
						Grid.SetRowSpan(rectBackg, this.gridAttachments.RowDefinitions.Count);
						Panel.SetZIndex(rectBackg, -100);
						this.gridAttachments.Children.Insert(6, rectBackg);
					}
					#endregion

					#region Custom Properties
					// We search backwards.
					foreach (UIElement cntCustom in this.gridCustomProperties.Children)
					{
						if ((cntCustom as Control) != null)
						{
							if ((cntCustom as Control).Tag.GetType() == typeof(RemoteCustomProperty))
							{
								dynamic dynControl = cntCustom;
								RemoteCustomProperty custProp = (RemoteCustomProperty)((Control)cntCustom).Tag;
								switch (custProp.CustomPropertyName)
								{
									case "TEXT_01":
										dynControl.Text = requirement.Text01;
										break;
									case "TEXT_02":
										dynControl.Text = requirement.Text02;
										break;
									case "TEXT_03":
										dynControl.Text = requirement.Text03;
										break;
									case "TEXT_04":
										dynControl.Text = requirement.Text04;
										break;
									case "TEXT_05":
										dynControl.Text = requirement.Text05;
										break;
									case "TEXT_06":
										dynControl.Text = requirement.Text06;
										break;
									case "TEXT_07":
										dynControl.Text = requirement.Text07;
										break;
									case "TEXT_08":
										dynControl.Text = requirement.Text08;
										break;
									case "TEXT_09":
										dynControl.Text = requirement.Text09;
										break;
									case "TEXT_10":
										dynControl.Text = requirement.Text10;
										break;
									case "LIST_01":
										dynControl.SelectedValue = requirement.List01;
										break;
									case "LIST_02":
										dynControl.SelectedValue = requirement.List02;
										break;
									case "LIST_03":
										dynControl.SelectedValue = requirement.List03;
										break;
									case "LIST_04":
										dynControl.SelectedValue = requirement.List04;
										break;
									case "LIST_05":
										dynControl.SelectedValue = requirement.List05;
										break;
									case "LIST_06":
										dynControl.SelectedValue = requirement.List06;
										break;
									case "LIST_07":
										dynControl.SelectedValue = requirement.List07;
										break;
									case "LIST_08":
										dynControl.SelectedValue = requirement.List08;
										break;
									case "LIST_09":
										dynControl.SelectedValue = requirement.List09;
										break;
									case "LIST_10":
										dynControl.SelectedValue = requirement.List10;
										break;
								}
							}
						}
					}
					#endregion

					//Set the tab title.
					this.ParentWindowPane.Caption = this.TabTitle;
					this.display_SetWindowChanged(this._isDescChanged || this._isResChanged || this._isFieldChanged);
				}
				catch (Exception ex)
				{
					Logger.LogMessage(ex, CLASS + METHOD);
				}
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "loadItem_DisplayInformation()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when the user does not want to save, and is forced to refresh the loaded data.</summary>
		/// <param name="sender">btnConcurrencyMergeNo, btnConcurrencyRefresh</param>
		/// <param name="e">RoutedEventArgs</param>
		private void btnRetryLoad_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				//Hide the error panel, jump to loading..
				this.display_SetOverlayWindow(this.panelError, System.Windows.Visibility.Collapsed);
				this.display_SetOverlayWindow(this.panelSaving, System.Windows.Visibility.Collapsed);
				this.display_SetOverlayWindow(this.panelStatus, System.Windows.Visibility.Visible);
				this.lblLoadingIncident.Text = StaticFuncs.getCultureResource.GetString("app_Requirement_Loading");

				this.load_LoadItem();
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "btnRetryLoad_Click()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
}
