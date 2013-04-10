﻿using System;
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
	public partial class frmDetailsIncident : UserControl
	{
		#region Client Values
		private ImportExportClient _client;
		private int _clientNumRunning; //Holds the number current executing.
		private int _clientNum; //Holds the total amount. Needed to multiple ASYNC() calls.
		#endregion

		#region Private Data Storage Variables

		//The Project and the Incident
		private SpiraProject _Project = null;
		private RemoteIncident _Incident;

		//Other project-specific items.
		private List<RemoteProjectUser> _ProjUsers;
		private List<RemoteRelease> _ProjReleases;
		private List<RemoteIncidentSeverity> _IncSeverity;
		private List<RemoteIncidentPriority> _IncPriority;
		private List<RemoteIncidentType> _IncType;
		private List<RemoteIncidentStatus> _IncStatus;
		private List<RemoteDocument> _IncDocuments;
		private string _IncDocumentsUrl;
		private string _IncidentUrl;
		private int? _tempHoursWorked;
		private int? _tempMinutedWorked;

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
					this._isWkfChanged = false;
					this.btnSave.IsEnabled = false;

					//Set flag, reset vars..
					this.IsLoading = true;
					this.barLoadingIncident.Value = 0;
					this._IncDocumentsUrl = null;
					this._IncidentUrl = null;

					//Reset any highlights.
					this.workflow_ClearAllRequiredHighlights();

					//Create a client.
					this._client = null;
					this._client = StaticFuncs.CreateClient(((SpiraProject)this.ArtifactDetail.ArtifactParentProject.ArtifactTag).ServerURL.ToString());

					//Set client events.
					this._client.Connection_Authenticate2Completed += new EventHandler<Connection_Authenticate2CompletedEventArgs>(_client_Connection_Authenticate2Completed);
					this._client.Connection_ConnectToProjectCompleted += new EventHandler<Connection_ConnectToProjectCompletedEventArgs>(_client_Connection_ConnectToProjectCompleted);
					this._client.Connection_DisconnectCompleted += new EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(_client_Connection_DisconnectCompleted);
					this._client.Incident_RetrieveByIdCompleted += new EventHandler<Incident_RetrieveByIdCompletedEventArgs>(_client_Incident_RetrieveByIdCompleted);
					this._client.Incident_RetrieveCommentsCompleted+= _client_Incident_RetrieveResolutionsCompleted;
					this._client.Incident_RetrieveSeveritiesCompleted += new EventHandler<Incident_RetrieveSeveritiesCompletedEventArgs>(_client_Incident_RetrieveSeveritiesCompleted);
					this._client.Incident_RetrievePrioritiesCompleted += new EventHandler<Incident_RetrievePrioritiesCompletedEventArgs>(_client_Incident_RetrievePrioritiesCompleted);
					this._client.Incident_RetrieveStatusesCompleted += new EventHandler<Incident_RetrieveStatusesCompletedEventArgs>(_client_Incident_RetrieveStatusesCompleted);
					this._client.Incident_RetrieveTypesCompleted += new EventHandler<Incident_RetrieveTypesCompletedEventArgs>(_client_Incident_RetrieveTypesCompleted);
					this._client.Incident_RetrieveWorkflowCustomPropertiesCompleted += new EventHandler<Incident_RetrieveWorkflowCustomPropertiesCompletedEventArgs>(_client_Incident_RetrieveWorkflowCustomPropertiesCompleted);
					this._client.Incident_RetrieveWorkflowFieldsCompleted += new EventHandler<Incident_RetrieveWorkflowFieldsCompletedEventArgs>(_client_Incident_RetrieveWorkflowFieldsCompleted);
					this._client.Incident_RetrieveWorkflowTransitionsCompleted += new EventHandler<Incident_RetrieveWorkflowTransitionsCompletedEventArgs>(_client_Incident_RetrieveWorkflowTransitionsCompleted);
					this._client.Document_RetrieveForArtifactCompleted += new EventHandler<Document_RetrieveForArtifactCompletedEventArgs>(_client_Document_RetrieveForArtifactCompleted);
					this._client.Release_RetrieveCompleted += new EventHandler<Release_RetrieveCompletedEventArgs>(_client_Release_RetrieveCompleted);
					this._client.Project_RetrieveUserMembershipCompleted += new EventHandler<Project_RetrieveUserMembershipCompletedEventArgs>(_client_Project_RetrieveUserMembershipCompleted);
					this._client.System_GetArtifactUrlCompleted += new EventHandler<System_GetArtifactUrlCompletedEventArgs>(_client_System_GetArtifactUrlCompleted);
					this._client.CustomProperty_RetrieveForArtifactTypeCompleted += new EventHandler<CustomProperty_RetrieveForArtifactTypeCompletedEventArgs>(_client_CustomProperty_RetrieveForArtifactTypeCompleted);

					//Fire the connection off here.
					this._clientNumRunning++;
					this.barLoadingIncident.Maximum = 18;
					this._client.Connection_Authenticate2Async(this._Project.UserName, this._Project.UserPass, StaticFuncs.getCultureResource.GetString("app_ReportName"));

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
						this._clientNumRunning += 9;
						//Here we need to fire off all data retrieval functions:
						// - Project users.
						this._client.Project_RetrieveUserMembershipAsync(this._clientNum++);
						// - Incident Statuses, Types, Priorities, Severities
						this._client.Incident_RetrievePrioritiesAsync(this._clientNum++);
						this._client.Incident_RetrieveSeveritiesAsync(this._clientNum++);
						this._client.Incident_RetrieveStatusesAsync(this._clientNum++);
						this._client.Incident_RetrieveTypesAsync(this._clientNum++);
						this._client.CustomProperty_RetrieveForArtifactTypeAsync(3, false,  this._clientNum++);
						// - Available Releases
						this._client.Release_RetrieveAsync(true, this._clientNum++);
						// - Resolutions / Comments
						this._client.Incident_RetrieveCommentsAsync(this.ArtifactDetail.ArtifactId, this._clientNum++);
						// - System URL
						this._client.System_GetArtifactUrlAsync(-14, this._Project.ProjectID, -2, null, this._clientNum++);
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
								StaticFuncs.getCultureResource.GetString("app_General_InvalidProject"));
						}
						this._client.Connection_DisconnectAsync();
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
			try
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
						this.load_IsReadyToGetMainData();
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
				Logger.LogMessage(ex, "_client_Project_RetrieveUserMembershipCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when we're finished getting our project releases.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">Release_RetrieveCompletedEventArgs</param>
		private void _client_Release_RetrieveCompleted(object sender, Release_RetrieveCompletedEventArgs e)
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
						this._ProjReleases = e.Result;
						//See if we're ready to get the actual data.
						this.load_IsReadyToGetMainData();
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
				Logger.LogMessage(ex, "_client_Release_RetrieveCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when we're finished getting our task types.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">Incident_RetrieveTypesCompletedEventArgs</param>
		private void _client_Incident_RetrieveTypesCompleted(object sender, Incident_RetrieveTypesCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "_client_Incident_RetrieveTypesCompleted()";
				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " ENTER. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());

				this._clientNumRunning--;
				this.barLoadingIncident.Value++;

				if (!e.Cancelled)
				{
					if (e.Error == null)
					{
						this._IncType = e.Result;
						//See if we're ready to get the actual data.
						this.load_IsReadyToGetMainData();
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
				Logger.LogMessage(ex, "_client_Incident_RetrieveTypesCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when we're finished getting our task statuses.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">Incident_RetrieveStatusesCompletedEventArgs</param>
		private void _client_Incident_RetrieveStatusesCompleted(object sender, Incident_RetrieveStatusesCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "_client_Incident_RetrieveStatusesCompleted()";
				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " ENTER. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());

				this._clientNumRunning--;
				this.barLoadingIncident.Value++;

				if (!e.Cancelled)
				{
					if (e.Error == null)
					{
						this._IncStatus = e.Result;
						//See if we're ready to get the actual data.
						this.load_IsReadyToGetMainData();
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
				Logger.LogMessage(ex, "_client_Incident_RetrieveStatusesCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when we're finished getting our task priorities.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">Incident_RetrievePrioritiesCompletedEventArgs</param>
		private void _client_Incident_RetrievePrioritiesCompleted(object sender, Incident_RetrievePrioritiesCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "_client_Incident_RetrievePrioritiesCompleted()";
				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " ENTER. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());

				this._clientNumRunning--;
				this.barLoadingIncident.Value++;

				if (e.Error == null)
				{
					this._IncPriority = e.Result;
					//See if we're ready to get the actual data.
					this.load_IsReadyToGetMainData();
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

				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " EXIT. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "_client_Incident_RetrievePrioritiesCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when we're finished getting our task severities.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">Incident_RetrieveSeveritiesCompletedEventArgs</param>
		private void _client_Incident_RetrieveSeveritiesCompleted(object sender, Incident_RetrieveSeveritiesCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "_client_Incident_RetrieveSeveritiesCompleted()";
				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " ENTER. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());

				this._clientNumRunning--;
				this.barLoadingIncident.Value++;

				if (!e.Cancelled)
				{
					if (e.Error == null)
					{
						this._IncSeverity = e.Result;
						//See if we're ready to get the actual data.
						this.load_IsReadyToGetMainData();
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
				Logger.LogMessage(ex, "_client_Incident_RetrieveSeveritiesCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when we're finished getting task resolutions.</summary>
		/// <param name="sender">IMportExportClient</param>
		/// <param name="e">Incident_RetrieveResolutionsCompletedEventArgs</param>
		private void _client_Incident_RetrieveResolutionsCompleted(object sender, Incident_RetrieveCommentsCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "_client_Incident_RetrieveResolutionsCompleted()";
				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " ENTER. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());

				this._clientNumRunning--;
				this.barLoadingIncident.Value++;

				if (!e.Cancelled)
				{
					if (e.Error == null)
					{
						this.loadItem_PopulateDiscussion(e.Result);

						//See if we're ready to get the actual data.
						this.load_IsReadyToGetMainData();
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
				Logger.LogMessage(ex, "_client_Incident_RetrieveResolutionsCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when we're finished getting the main task details.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">Incident_RetrieveByIdCompletedEventArgs</param>
		private void _client_Incident_RetrieveByIdCompleted(object sender, Incident_RetrieveByIdCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "_client_Incident_RetrieveByIdCompleted()";
				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " ENTER. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());

				this._clientNumRunning--;
				this.barLoadingIncident.Value++;

				if (!e.Cancelled)
				{
					if (e.Error == null)
					{
						//Load recorded data..
						this._IncCurrentStatus = e.Result.IncidentStatusId;
						this._IncCurrentType = e.Result.IncidentTypeId;
						this._Incident = e.Result;

						//Get workflow steps and fields, and URL.
						this._clientNumRunning += 5;
						this._client.Incident_RetrieveWorkflowFieldsAsync(this._Incident.IncidentTypeId.Value, this._Incident.IncidentStatusId.Value, this._clientNum++);
						this._client.Incident_RetrieveWorkflowTransitionsAsync(this._Incident.IncidentTypeId.Value, this._Incident.IncidentStatusId.Value, (this._Incident.OpenerId == this._Project.UserID), (this._Incident.OwnerId == this._Project.UserID), this._clientNum++);
						this._client.Incident_RetrieveWorkflowCustomPropertiesAsync(this._Incident.IncidentTypeId.Value, this._Incident.IncidentStatusId.Value, this._clientNum++);
						this._client.Document_RetrieveForArtifactAsync(3, this._Incident.IncidentId.Value, new List<RemoteFilter>(), new RemoteSort(), this._clientNum++);
						this._client.System_GetArtifactUrlAsync(3, this._Project.ProjectID, this._Incident.IncidentId.Value, null, this._clientNum++);

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
				Logger.LogMessage(ex, "_client_Incident_RetrieveByIdCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when the client is finished getting available workflow transitions.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">Incident_RetrieveWorkflowTransitionsCompletedEventArgs</param>
		private void _client_Incident_RetrieveWorkflowTransitionsCompleted(object sender, Incident_RetrieveWorkflowTransitionsCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "_client_Incident_RetrieveWorkflowTransitionsCompleted()";
				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " ENTER. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());

				this._clientNumRunning--;
				this.barLoadingIncident.Value++;

				if (!e.Cancelled)
				{
					if (e.Error == null)
					{
						this._WorkflowTransitions = e.Result;

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
				Logger.LogMessage(ex, "_client_Incident_RetrieveWorkflowTransitionsCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when the client is finished pulling all the workflow fields and their status.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">Incident_RetrieveWorkflowFieldsCompletedEventArgs</param>
		private void _client_Incident_RetrieveWorkflowFieldsCompleted(object sender, Incident_RetrieveWorkflowFieldsCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "_client_Incident_RetrieveWorkflowFieldsCompleted()";
				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " ENTER. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());

				this._clientNumRunning--;
				this.barLoadingIncident.Value++;

				if (!e.Cancelled)
				{
					if (e.Error == null)
					{
						this._WorkflowFields_Current = this.workflow_LoadFieldStatus(e.Result);

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
				Logger.LogMessage(ex, "_client_Incident_RetrieveWorkflowFieldsCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when the client is finished getting custom workflow property fields.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">Incident_RetrieveWorkflowCustomPropertiesCompletedEventArgs</param>
		private void _client_Incident_RetrieveWorkflowCustomPropertiesCompleted(object sender, Incident_RetrieveWorkflowCustomPropertiesCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "_client_Incident_RetrieveWorkflowCustomPropertiesCompleted()";
				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " ENTER. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());

				this._clientNumRunning--;
				this.barLoadingIncident.Value++;

				if (!e.Cancelled)
				{
					if (e.Error == null)
					{
						this._WorkflowCustom_Current = this.workflow_LoadFieldStatus(e.Result);

						//See if we're ready to get the actual data.
						this.load_IsReadyToDisplayData();
					}
					else
					{
						Logger.LogMessage(e.Error);
						this._client.Connection_DisconnectAsync();

						//Display the error panel.
						this.display_ShowErrorPanel();
					}
				}

				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " EXIT. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "_client_Incident_RetrieveWorkflowCustomPropertiesCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when the client finishes getting the custom field values.</summary>
		/// <param name="sender">ImportExaoprtClient</param>
		/// <param name="e">CustomProperty_RetrieveForArtifactTypeCompletedEventArgs</param>
		private void _client_CustomProperty_RetrieveForArtifactTypeCompleted(object sender, CustomProperty_RetrieveForArtifactTypeCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "_client_CustomProperty_RetrieveForArtifactTypeCompleted()";
				System.Diagnostics.Debug.WriteLine(CLASS + METHOD + " ENTER. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());

				this._clientNumRunning--;
				this.barLoadingIncident.Value++;

				if (!e.Cancelled)
				{
					if (e.Error == null)
					{
						//Clear our WorkflowCustom field..
						this._WorkflowCustom = new Dictionary<int, WorkflowField>();

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

							//** Create our record for the field.
							WorkflowField wkfCustom = new WorkflowField(e.Result[j].CustomPropertyId, e.Result[j].Alias, custControl, false, false, lblCustProp);
							this._WorkflowCustom.Add(wkfCustom.FieldID, wkfCustom);

							//Flip the IsOnFirst..
							IsOnFirst = !IsOnFirst;
						}

						//See if we're ready to get the actual data.
						this.load_IsReadyToGetMainData();
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
				Logger.LogMessage(ex, "_client_CustomProperty_RetrieveForArtifactTypeCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
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

						this.load_IsReadyToGetMainData();
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
						if (string.IsNullOrWhiteSpace(this._IncDocumentsUrl))
						{
							this._IncDocumentsUrl = e.Result;
							this.load_IsReadyToGetMainData();
						}
						else
						{
							this._IncidentUrl = e.Result.Replace("~", this._Project.ServerURL.ToString());
							this.load_IsReadyToDisplayData();
						}

					}
					else
					{
						Logger.LogMessage(e.Error);
						this._IncDocumentsUrl = "--none--";
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

		#endregion

		/// <summary>Checks to make sure that it is okay to go get the main Incident data.
		/// Called after all Async calls except workflow items.
		/// </summary>
		private void load_IsReadyToGetMainData()
		{
			try
			{
				Logger.LogTrace("load_IsReadyToGetMainData: Clients Running " + this._clientNumRunning.ToString());

				if (this._clientNumRunning == 0)
				{
					//No clients are currently running, we can get the main data now.
					this._client.Incident_RetrieveByIdAsync(this.ArtifactDetail.ArtifactId, this._clientNum++);
					this._clientNumRunning++;
				}
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "load_IsReadyToGetMainData()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

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
					this.loadItem_DisplayInformation(this._Incident);

					//Set Workflow Data. (To disable Fields)
					this.workflow_SetEnabledFields(this._WorkflowFields, this._WorkflowFields_Current);
					this.workflow_SetEnabledFields(this._WorkflowCustom, this._WorkflowCustom_Current);

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
				Logger.LogMessage(ex, "loadItem_PopulateUser()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
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

		/// <summary>Loads the list of available severities into the specified ComboBox.</summary>
		/// <param name="box">The ComboBox to load severities into.</param>
		/// <param name="SelectedUserID">The SeverityId to select.</param>
		private void loadItem_PopulateSeverity(ComboBox box, int? SelectedItem)
		{
			try
			{
				//Clear and add our 'none'.
				box.Items.Clear();
				box.SelectedIndex = box.Items.Add(new RemoteIncidentSeverity() { Name = StaticFuncs.getCultureResource.GetString("app_General_None") });

				if (!SelectedItem.HasValue)
					SelectedItem = -1;

				foreach (RemoteIncidentSeverity Severity in this._IncSeverity)
				{
					int nunAdded = box.Items.Add(Severity);
					if (Severity.SeverityId == SelectedItem)
					{
						box.SelectedIndex = nunAdded;
					}
				}
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "loadItem_PopulateSeverity()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Loads the list of available priorities into the specified ComboBox.</summary>
		/// <param name="box">The ComboBox to load priorities into.</param>
		/// <param name="SelectedUserID">The PriorityId to select.</param>
		private void loadItem_PopulatePriority(ComboBox box, int? SelectedItem)
		{
			try
			{
				//Clear and add our 'none'.
				box.Items.Clear();
				box.SelectedIndex = box.Items.Add(new RemoteIncidentPriority() { Name = StaticFuncs.getCultureResource.GetString("app_General_None") });

				if (!SelectedItem.HasValue)
					SelectedItem = -1;

				foreach (RemoteIncidentPriority Priority in this._IncPriority)
				{
					int nunAdded = box.Items.Add(Priority);
					if (Priority.PriorityId == SelectedItem)
						box.SelectedIndex = nunAdded;
				}
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "loadItem_PopulatePriority()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Loads the list of available types into the specified ComboBox.</summary>
		/// <param name="box">The ComboBox to load types into.</param>
		/// <param name="SelectedUserID">The TypeId to select.</param>
		private void loadItem_PopulateType(ComboBox box, int? SelectedItem)
		{
			try
			{
				box.Items.Clear();

				if (!SelectedItem.HasValue)
					SelectedItem = -1;

				foreach (Business.SpiraTeam_Client.RemoteIncidentType Type in this._IncType)
				{
					int numAdded = box.Items.Add(Type);
					if (SelectedItem == Type.IncidentTypeId)
						box.SelectedIndex = numAdded;
				}
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "loadItem_PopulateType()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Loads the list of available statuses and workflow transitions into the specified ComboBox.</summary>
		/// <param name="box">The ComboBox to load statuses into.</param>
		/// <param name="SelectedUserID">The StatusId to select.</param>
		private void loadItem_PopulateStatus(MenuItem menu, int? SelectedItem)
		{
			//Loop through all the available ones. We only add the ones that are in the 
			//  workflow transition, or the current status, making sure the current
			//  one is selected.
			try
			{
				//Clear items already there, add the null item.
				menu.Items.Clear();

				//Load ones that are available.
				foreach (Business.SpiraTeam_Client.RemoteIncidentStatus Status in this._IncStatus)
				{
					if (Status.IncidentStatusId == SelectedItem)
					{
						//Display the current status in the label..
						this.cntrlIncidentStatus.Text = Status.Name;
					}
					else
					{
						//Loop through available transitions. If this status is available, add it.
						foreach (Business.SpiraTeam_Client.RemoteWorkflowIncidentTransition Transition in this._WorkflowTransitions)
						{
							if (Transition.IncidentStatusId_Output == Status.IncidentStatusId)
							{
								if (!Transition.Name.Trim().StartsWith("»")) Transition.Name = "» " + Transition.Name;
								menu.Items.Add(Transition);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "loadItem_PopulateStatus()");
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

				//Add new Comments..
				if (Discussions.Count < 1)
					this.cntrlDiscussion.Children.Add(new cntlDiscussionFrame("No comments for this item.", ""));
				else
				{
					foreach (Business.SpiraTeam_Client.RemoteComment Resolution in Discussions)
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
		#endregion











		/// <summary>Load the specified task into the data fields.</summary>
		/// <param name="task">The task details to load into fields.</param>
		private void loadItem_DisplayInformation(Business.SpiraTeam_Client.RemoteIncident incident)
		{
			try
			{
				const string METHOD = "loadItem_DisplayInformation()";

				try
				{
					#region Base Fields
					// - Name & ID
					this.cntrlIncidentName.Text = incident.Name;
					this.lblToken.Text = this._ArtifactDetails.ArtifactIDDisplay;
					this.cntrlResolution.HTMLText = "";

					// - Users
					this.loadItem_PopulateUser(this.cntrlDetectedBy, incident.OpenerId);
					this.loadItem_PopulateUser(this.cntrlOwnedBy, incident.OwnerId);
					this.cntrlDetectedBy.Items.RemoveAt(0);

					// - Releases
					this.loadItem_PopulateReleaseControl(this.cntrlDetectedIn, incident.DetectedReleaseId);
					this.loadItem_PopulateReleaseControl(this.cntrlResolvedIn, incident.ResolvedReleaseId);
					this.loadItem_PopulateReleaseControl(this.cntrlVerifiedIn, incident.VerifiedReleaseId);

					// - Priority & Severity
					this.loadItem_PopulatePriority(this.cntrlPriority, incident.PriorityId);
					this.loadItem_PopulateSeverity(this.cntrlSeverity, incident.SeverityId);

					// - Type & Status
					this.loadItem_PopulateType(this.cntrlType, incident.IncidentTypeId);
					this.loadItem_PopulateStatus(this.mnuActions, incident.IncidentStatusId);

					// - Description
					this.cntrlDescription.HTMLText = incident.Description;
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
									atchUri = new Uri(this._IncDocumentsUrl.Replace("~", this._Project.ServerURL.ToString()).Replace("{art}", incidentAttachment.AttachmentId.ToString()));
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

					#region Schedule
					this.cntrlStartDate.SelectedDate = incident.StartDate;
					this.cntrlEndDate.SelectedDate = incident.ClosedDate;
					this.barPercentComplete.Value = incident.CompletionPercent;
					//Get estimated effort..
					this.cntrlEstEffortH.Text = ((incident.EstimatedEffort.HasValue) ? Math.Floor(((double)incident.EstimatedEffort / (double)60)).ToString() : null);
					this.cntrlEstEffortM.Text = ((incident.EstimatedEffort.HasValue) ? ((double)incident.EstimatedEffort % (double)60).ToString() : null);
					//Get actual effort..
					int existingH = 0;
					int existingM = 0;
					if (incident.ActualEffort.HasValue)
					{
						existingH += (int)Math.Floor((double)incident.ActualEffort / 60D);
						existingM += (int)(incident.ActualEffort % 60D);
					}
					this.cntrlActEffortH.Text = ((existingH == 0 && existingM == 0 && !incident.ActualEffort.HasValue) ? null : existingH.ToString());
					this.cntrlActEffortM.Text = ((existingH == 0 && existingM == 0 && !incident.ActualEffort.HasValue) ? null : existingM.ToString());
					//Get projected effort..
					this.cntrlProjEffortH.Text = ((incident.ProjectedEffort.HasValue) ? Math.Floor(((double)incident.ProjectedEffort / (double)60)).ToString() : "0");
					this.cntrlProjEffortM.Text = ((incident.ProjectedEffort.HasValue) ? ((double)incident.ProjectedEffort % (double)60).ToString() : "0");
					//Get remaining effort..
					this.cntrlRemEffortH.Text = ((incident.RemainingEffort.HasValue) ? Math.Floor(((double)incident.RemainingEffort / (double)60)).ToString() : null);
					this.cntrlRemEffortM.Text = ((incident.RemainingEffort.HasValue) ? ((double)incident.RemainingEffort % (double)60).ToString() : null);
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
										dynControl.Text = incident.Text01;
										break;
									case "TEXT_02":
										dynControl.Text = incident.Text02;
										break;
									case "TEXT_03":
										dynControl.Text = incident.Text03;
										break;
									case "TEXT_04":
										dynControl.Text = incident.Text04;
										break;
									case "TEXT_05":
										dynControl.Text = incident.Text05;
										break;
									case "TEXT_06":
										dynControl.Text = incident.Text06;
										break;
									case "TEXT_07":
										dynControl.Text = incident.Text07;
										break;
									case "TEXT_08":
										dynControl.Text = incident.Text08;
										break;
									case "TEXT_09":
										dynControl.Text = incident.Text09;
										break;
									case "TEXT_10":
										dynControl.Text = incident.Text10;
										break;
									case "LIST_01":
										dynControl.SelectedValue = incident.List01;
										break;
									case "LIST_02":
										dynControl.SelectedValue = incident.List02;
										break;
									case "LIST_03":
										dynControl.SelectedValue = incident.List03;
										break;
									case "LIST_04":
										dynControl.SelectedValue = incident.List04;
										break;
									case "LIST_05":
										dynControl.SelectedValue = incident.List05;
										break;
									case "LIST_06":
										dynControl.SelectedValue = incident.List06;
										break;
									case "LIST_07":
										dynControl.SelectedValue = incident.List07;
										break;
									case "LIST_08":
										dynControl.SelectedValue = incident.List08;
										break;
									case "LIST_09":
										dynControl.SelectedValue = incident.List09;
										break;
									case "LIST_10":
										dynControl.SelectedValue = incident.List10;
										break;
								}
							}
						}
					}
					#endregion

					//Set the tab title.
					this.ParentWindowPane.Caption = this.TabTitle;
					this.display_SetWindowChanged(false || this._isWkfChanged || this._isDescChanged || this._isResChanged || this._isFieldChanged);
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
				this.lblLoadingIncident.Text = StaticFuncs.getCultureResource.GetString("app_Incident_Loading");

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