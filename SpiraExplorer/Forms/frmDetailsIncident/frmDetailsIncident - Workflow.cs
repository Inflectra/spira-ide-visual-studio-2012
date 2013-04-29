using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Inflectra.Global;
using Inflectra.SpiraTest.IDEIntegration.VisualStudio2012.Business;
using Inflectra.SpiraTest.IDEIntegration.VisualStudio2012.Business.SpiraTeam_Client;
using Inflectra.SpiraTest.IDEIntegration.VisualStudio2012.Controls;

namespace Inflectra.SpiraTest.IDEIntegration.VisualStudio2012.Forms
{
	/// <summary>Holds the saving functions for frmDetailsIncident</summary>
	public partial class frmDetailsIncident : UserControl
	{
		#region Private Workflow Fields
		//Holds the definitions for the fields.
		private Dictionary<int, WorkflowField> _WorkflowFields;
		private Dictionary<int, WorkflowField> _WorkflowCustom;
		//Holds current status information for fields.
		private Dictionary<int, WorkflowField.WorkflowStatusEnum> _WorkflowFields_Current;
		private Dictionary<int, WorkflowField.WorkflowStatusEnum> _WorkflowCustom_Current;
		//Holds updated status information for fields.
		private Dictionary<int, WorkflowField.WorkflowStatusEnum> _WorkflowFields_Updated;
		private Dictionary<int, WorkflowField.WorkflowStatusEnum> _WorkflowCustom_Updated;

		//Holds current Status & Type information
		private int? _IncCurrentType;
		private int? _IncCurrentStatus;
		//Holds updated Status & Type information
		private int? _IncSelectedType;
		private int? _IncSelectedStatus;

		//Holds available transitions for the loaded Incident.
		private List<RemoteWorkflowIncidentTransition> _WorkflowTransitions;
		#endregion

		/// <summary>Generates a dictionary of IDs and WorkflowFields available for Incidents. Optionally will assign settings given.</summary>
		/// <returns>The dictionary of IDs and WorkflowFields</returns>
		private Dictionary<int, WorkflowField> workflow_GenerateStandardFields()
		{
			try
			{
				Dictionary<int, WorkflowField> retDict = new Dictionary<int, WorkflowField>();

				//TODO: Get the Projected and Remaining effort fields.
				//Load up each control..
				retDict.Add(1, new WorkflowField(1, "Severity", this.cntrlSeverity, false, false, this.lblSeverity));
				retDict.Add(2, new WorkflowField(2, "Priority", this.cntrlPriority, false, false, this.lblPriority));
				retDict.Add(3, new WorkflowField(3, "Status", null, true, false, this.lblStatus));
				retDict.Add(4, new WorkflowField(4, "Type", this.cntrlType, false, false, this.lblType));
				retDict.Add(5, new WorkflowField(5, "Opener", this.cntrlDetectedBy, false, false, this.lblDetectedBy));
				retDict.Add(6, new WorkflowField(6, "Owner", this.cntrlOwnedBy, false, false, this.lblOwnedBy));
				retDict.Add(7, new WorkflowField(7, "Detected Release", this.cntrlDetectedIn, false, false, this.lblDetectedIn));
				retDict.Add(8, new WorkflowField(8, "Resolved Release", this.cntrlResolvedIn, false, false, this.lblResolvedIn));
				retDict.Add(9, new WorkflowField(9, "Verified Release", this.cntrlVerifiedIn, false, false, this.lblVerifiedIn));
				retDict.Add(10, new WorkflowField(10, "Name", this.cntrlIncidentName, true, false));
				retDict.Add(11, new WorkflowField(11, "Description", this.cntrlDescription, false, false, this.lblDescription));
				retDict.Add(12, new WorkflowField(12, "Resolution", this.cntrlResolution, false, false));
				retDict.Add(13, new WorkflowField(13, "Creation Date", null, true, true));
				retDict.Add(14, new WorkflowField(14, "End Date", this.cntrlEndDate, false, false, this.lblEndDate));
				retDict.Add(15, new WorkflowField(15, "Last Modified Date", null, true, true));
				retDict.Add(45, new WorkflowField(45, "Start Date", this.cntrlStartDate, false, false, this.lblStartDate));
				retDict.Add(46, new WorkflowField(46, "Completion %", null, true, false));
				retDict.Add(47, new WorkflowField(47, "Estimated Effort", this.cntrlEstEffortH, false, false, this.lblEstEffort));
				retDict.Add(48, new WorkflowField(48, "Actual Effort", this.cntrlActEffortH, false, false, this.lblActEffort));
				retDict.Add(94, new WorkflowField(94, "Incident ID", null, true, false));
				retDict.Add(126, new WorkflowField(126, "Projected Effort", null, false, false, null));
				retDict.Add(127, new WorkflowField(127, "Remaining Effort", null, false, false, null));
				retDict.Add(136, new WorkflowField(136, "Fixed Build", null, false, false, null));
				retDict.Add(138, new WorkflowField(138, "Progress", null, false, false, null));

				//Return it.
				return retDict;
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "workflow_GenerateStandardFields()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
				return new Dictionary<int, WorkflowField>();
			}
		}

		/// <summary>Creates a useable dictionary of Field ID and Status from the given list of Workflow Fields.</summary>
		/// <param name="workflowFields">A list of Workflow Fields</param>
		/// <returns>A dictionary of FieldId and current Status.</returns>
		private Dictionary<int, WorkflowField.WorkflowStatusEnum> workflow_LoadFieldStatus(List<RemoteWorkflowIncidentFields> workflowFields)
		{
			Dictionary<int, WorkflowField.WorkflowStatusEnum> retList = new Dictionary<int, WorkflowField.WorkflowStatusEnum>();

			try
			{
				//Go through each known field, and see if it exists in the data we got from the server.
				foreach (KeyValuePair<int, WorkflowField> kvpField in this._WorkflowFields)
				{
					WorkflowField.WorkflowStatusEnum status = WorkflowField.WorkflowStatusEnum.Normal;
					RemoteWorkflowIncidentFields wkfField = workflowFields.SingleOrDefault(wkf => wkf.FieldId == kvpField.Key);
					if (wkfField != null)
					{
						status = (WorkflowField.WorkflowStatusEnum)wkfField.FieldStateId;
					}
					retList.Add(kvpField.Key, status);
				}

				return retList;
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "workflow_LoadFieldStatus()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
				retList = new Dictionary<int, WorkflowField.WorkflowStatusEnum>();
			}
			return retList;
		}

		/// <summary>Creates a useable dictionary of Field ID and Status from the given list of Workflow Custom Fields.</summary>
		/// <param name="workflowFields">A list of Workflow Fields</param>
		/// <returns>A dictionary of FieldId and current Status.</returns>
		private Dictionary<int, WorkflowField.WorkflowStatusEnum> workflow_LoadFieldStatus(List<RemoteWorkflowIncidentCustomProperties> workflowFields)
		{
			Dictionary<int, WorkflowField.WorkflowStatusEnum> retList = new Dictionary<int, WorkflowField.WorkflowStatusEnum>();

			try
			{
				//Go through each field..
				foreach (KeyValuePair<int, WorkflowField> kvpField in this._WorkflowCustom)
				{
					WorkflowField.WorkflowStatusEnum status = WorkflowField.WorkflowStatusEnum.Normal;
					RemoteWorkflowIncidentCustomProperties wkfField = workflowFields.SingleOrDefault(wkf => wkf.CustomPropertyId == kvpField.Key);
					if (wkfField != null)
					{
						status = (WorkflowField.WorkflowStatusEnum)wkfField.FieldStateId;
					}

					retList.Add(kvpField.Key, status);
				}
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "workflow_LoadFieldStatus()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
				retList = new Dictionary<int, WorkflowField.WorkflowStatusEnum>();
			}
			return retList;
		}

		/// <summary>Set the enabled and required fields for the current stage in the workflow.</summary>
		/// <param name="WorkFlowFields">The Dictionary of Workflow Fields</param>
		private void workflow_SetEnabledFields(Dictionary<int, WorkflowField> FieldList, Dictionary<int, WorkflowField.WorkflowStatusEnum> WorkflowFields)
		{
			try
			{
				//We need to loop through each field, and set it appropriately.
				foreach (KeyValuePair<int, WorkflowField> incField in FieldList)
				{
					if (!incField.Value.IsHidden && !incField.Value.IsFixed)
					{
						//Try to set Enabled/Disabled
						if (incField.Value.FieldControl != null)
						{
							try
							{
								incField.Value.FieldControl.IsEnabled = (WorkflowFields[incField.Key] != WorkflowField.WorkflowStatusEnum.Hidden) && (WorkflowFields[incField.Key] != WorkflowField.WorkflowStatusEnum.Inactive);
							}
							catch (Exception ex)
							{
								Logger.LogMessage(ex, "Trying to set field '" + incField.Value.FieldName + "' enabled/disabled.");
							}
						}

						//Try to set Bold/Normal for label.
						if (incField.Value.FieldLabel != null)
						{
							try
							{
								((dynamic)incField.Value.FieldLabel).FontWeight = ((WorkflowFields[incField.Key] == WorkflowField.WorkflowStatusEnum.Required) ? FontWeights.Bold : FontWeights.Normal);
							}
							catch (Exception ex)
							{
								Logger.LogMessage(ex, "Trying to set field '" + incField.Value.FieldName + "' bold.");
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "workflow_SetEnabledFields()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Simple function to clear all required fields of their Required Hishlight.</summary>
		private void workflow_ClearAllRequiredHighlights()
		{
			try
			{
				if (this._WorkflowCustom == null) this._WorkflowCustom = new Dictionary<int, WorkflowField>();
				if (this._WorkflowFields == null) this._WorkflowFields = new Dictionary<int, WorkflowField>();

				//Custom fields.
				foreach (KeyValuePair<int, WorkflowField> kvpField in this._WorkflowCustom)
					if (kvpField.Value.FieldControl != null)
						kvpField.Value.FieldControl.Style = (Style)this.FindResource("PaddedControl");

				//Standard fields.
				foreach (KeyValuePair<int, WorkflowField> kvpField in this._WorkflowFields)
					if (kvpField.Value.FieldControl != null)
						kvpField.Value.FieldControl.Style = (Style)this.FindResource("PaddedControl");

			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "workflow_ClearAllRequiredHighlights()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Checks all required fields for a value.</summary>
		/// <returns>True if all is good. False if fields marked as required are left unset.</returns>
		private bool workflow_CheckRequiredFields()
		{
			try
			{
				bool retValue = true;

				//First, get the set of required fields we want to check against.
				bool useUpdate = (this._IncSelectedStatus.HasValue || this._IncSelectedType.HasValue);
				Dictionary<int, WorkflowField.WorkflowStatusEnum> workflowField = ((useUpdate) ? this._WorkflowFields_Updated : this._WorkflowFields_Current);
				Dictionary<int, WorkflowField.WorkflowStatusEnum> workflowCustom = ((useUpdate) ? this._WorkflowCustom_Updated : this._WorkflowCustom_Current);

				#region Loop through Standard Fields
				//Loop through each field..
				foreach (KeyValuePair<int, WorkflowField> kvpField in this._WorkflowFields)
				{
					//We only care about required fields.
					if (workflowField[kvpField.Key] == WorkflowField.WorkflowStatusEnum.Required && !kvpField.Value.IsHidden && !kvpField.Value.IsFixed)
					{
						//Find the field and act upon it. Based on type on control and datatype inside it.
						switch (kvpField.Key)
						{
							case 1: // Severity
								if (!((RemoteIncidentSeverity)((ComboBox)kvpField.Value.FieldControl).SelectedItem).SeverityId.HasValue)
								{
									kvpField.Value.FieldControl.Style = (Style)this.FindResource("PaddedControlRequiredHighlight");
									retValue = false;
								}
								break;

							case 2: // Priority
								if (!((RemoteIncidentPriority)((ComboBox)kvpField.Value.FieldControl).SelectedItem).PriorityId.HasValue)
								{
									kvpField.Value.FieldControl.Style = (Style)this.FindResource("PaddedControlRequiredHighlight");
									retValue = false;
								}
								break;

							case 4: // Type
								if (!((RemoteIncidentType)((ComboBox)kvpField.Value.FieldControl).SelectedItem).IncidentTypeId.HasValue)
								{
									kvpField.Value.FieldControl.Style = (Style)this.FindResource("PaddedControlRequiredHighlight");
									retValue = false;
								}
								break;

							case 5: // Detector
							case 6: // Owner
								if (!((RemoteUser)((ComboBox)kvpField.Value.FieldControl).SelectedItem).UserId.HasValue)
								{
									kvpField.Value.FieldControl.Style = (Style)this.FindResource("PaddedControlRequiredHighlight");
									retValue = false;
								}
								break;

							case 7: // Detected Release
							case 8: // Resolved Release
							case 9: // Verified Release
								if (!((RemoteRelease)((ComboBox)kvpField.Value.FieldControl).SelectedItem).ReleaseId.HasValue)
								{
									kvpField.Value.FieldControl.Style = (Style)this.FindResource("PaddedControlRequiredHighlight");
									retValue = false;
								}
								break;

							case 10: // Name
								if (string.IsNullOrWhiteSpace(((TextBox)kvpField.Value.FieldControl).Text))
								{
									kvpField.Value.FieldControl.Style = (Style)this.FindResource("PaddedControlRequiredHighlight");
									retValue = false;
								}
								break;

							case 11: // Description 
							case 12: // Resolution
								if (string.IsNullOrWhiteSpace(StaticFuncs.StripTagsCharArray(((cntrlRichTextEditor)kvpField.Value.FieldControl).HTMLText)))
								{
									kvpField.Value.FieldControl.Style = (Style)this.FindResource("PaddedControlRequiredHighlight");
									retValue = false;
								}
								break;

							case 14: // Closed Date
							case 45: // Start Date
								if (!((DatePicker)kvpField.Value.FieldControl).SelectedDate.HasValue)
								{
									kvpField.Value.FieldControl.Style = (Style)this.FindResource("PaddedControlRequiredHighlight");
									retValue = false;
								}
								break;

							case 47: // Estimated Effort
								if (string.IsNullOrWhiteSpace(this.cntrlEstEffortH.Text.Trim()) && string.IsNullOrWhiteSpace(this.cntrlEstEffortM.Text.Trim()))
								{
									this.cntrlEstEffortH.Style = (Style)this.FindResource("PaddedControlRequiredHighlight");
									this.cntrlEstEffortM.Style = (Style)this.FindResource("PaddedControlRequiredHighlight");
									retValue = false;
								}
								break;

							case 48: // Actual Effort
								if (string.IsNullOrWhiteSpace(this.cntrlActEffortH.Text.Trim()) && string.IsNullOrWhiteSpace(this.cntrlActEffortM.Text.Trim()))
								{
									this.cntrlActEffortH.Style = (Style)this.FindResource("PaddedControlRequiredHighlight");
									this.cntrlActEffortM.Style = (Style)this.FindResource("PaddedControlRequiredHighlight");
									retValue = false;
								}
								break;

							case 126: // Projected Effort
								//TODO: Get Projected Date controls.
								break;

							case 127: // Projected Effort
								//TODO: Get Projected Date controls.
								break;

							case 3: // Status
							case 13: // Creation Date
							case 15: // Last Modified Date
							case 46: // Completion %
							case 94: // Incident ID
								break;
						}
					}
				}
				#endregion

				#region Loop through Custom Fields
				foreach (KeyValuePair<int, WorkflowField> kvpField in this._WorkflowCustom)
				{
					//We only care about required fields.
					if (workflowCustom[kvpField.Key] == WorkflowField.WorkflowStatusEnum.Required  && !kvpField.Value.IsHidden && !kvpField.Value.IsFixed)
					{
						//Depends on which control type it is..
						string cntrlType = kvpField.Value.FieldControl.GetType().ToString().ToLowerInvariant();
						cntrlType = cntrlType.Substring(cntrlType.LastIndexOf(".") + 1);

						switch (cntrlType)
						{
							case "textbox":
								if (string.IsNullOrWhiteSpace(((TextBox)kvpField.Value.FieldControl).Text))
								{
									kvpField.Value.FieldControl.Style = (Style)this.FindResource("PaddedControlRequiredHighlight");
									retValue = false;
								}
								break;

							case "combobox":
								if (((ComboBox)kvpField.Value.FieldControl).SelectedItem == null)
								{
									kvpField.Value.FieldControl.Style = (Style)this.FindResource("PaddedControlRequiredHighlight");
									retValue = false;
								}
								break;
						}
					}
				}
				#endregion

				return retValue;
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "workflow_CheckRequiredFields()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
		}

		/// <summary>Called when the user changes the workflow step, pulls enabled/required fields.</summary>
		private void workflow_ChangeWorkflowStep()
		{
			try
			{
				//This is a potentially different workflow, so create the client to go out and get fields.
				ImportExportClient wkfClient = StaticFuncs.CreateClient(this._Project.ServerURL.ToString());
				wkfClient.Connection_Authenticate2Completed += new EventHandler<Connection_Authenticate2CompletedEventArgs>(wkfClient_Connection_Authenticate2Completed);
				wkfClient.Connection_ConnectToProjectCompleted += new EventHandler<Connection_ConnectToProjectCompletedEventArgs>(wkfClient_Connection_ConnectToProjectCompleted);
				wkfClient.Incident_RetrieveWorkflowFieldsCompleted += new EventHandler<Incident_RetrieveWorkflowFieldsCompletedEventArgs>(wkfClient_Incident_RetrieveWorkflowFieldsCompleted);
				wkfClient.Incident_RetrieveWorkflowCustomPropertiesCompleted += new EventHandler<Incident_RetrieveWorkflowCustomPropertiesCompletedEventArgs>(wkfClient_Incident_RetrieveWorkflowCustomPropertiesCompleted);

				//Connect.
				this._clientNumRunning = 1;
				wkfClient.Connection_Authenticate2Async(this._Project.UserName, this._Project.UserPass, StaticFuncs.getCultureResource.GetString("app_ReportName"));
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "workflow_ChangeWorkflowStep()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		#region Workflow Client Events
		/// <summary>Hit when we've successfully connected to the server.</summary>
		/// <param name="sender">ImportExporClient</param>
		/// <param name="e">Connection_Authenticate2CompletedEventArgs</param>
		private void wkfClient_Connection_Authenticate2Completed(object sender, Connection_Authenticate2CompletedEventArgs e)
		{
			try
			{
				const string METHOD = "wkfClient_Connection_Authenticate2Completed()";
				Logger.LogTrace(CLASS + METHOD + " ENTER.");

				this._clientNumRunning--;
				this.barLoadingIncident.Value++;

				if (sender is ImportExportClient)
				{
					ImportExportClient client = sender as ImportExportClient;

					if (!e.Cancelled)
					{
						if (e.Error == null && e.Result)
						{
							//Connect to our project.
							this._clientNumRunning++;
							client.Connection_ConnectToProjectAsync(((SpiraProject)this._ArtifactDetails.ArtifactParentProject.ArtifactTag).ProjectID, this._clientNum++);
						}
						else
						{
							if (e.Error != null)
							{
								Logger.LogMessage(e.Error);
							}
							else
							{
								Logger.LogMessage(METHOD, "Could not log in.", System.Diagnostics.EventLogEntryType.Error);
							}
						}
					}
				}

				Logger.LogTrace(CLASS + METHOD + " EXIT. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "wkfClient_Connection_Authenticate2Completed()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when we've completed connecting to the project. </summary>
		/// <param name="sender">ImportExporClient</param>
		/// <param name="e">Connection_ConnectToProjectCompletedEventArgs</param>
		private void wkfClient_Connection_ConnectToProjectCompleted(object sender, Connection_ConnectToProjectCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "wkfClient_Connection_ConnectToProjectCompleted()";
				Logger.LogTrace(CLASS + METHOD + " ENTER.");

				this._clientNumRunning--;
				this.barLoadingIncident.Value++;

				if (sender is ImportExportClient)
				{
					ImportExportClient client = sender as ImportExportClient;

					if (!e.Cancelled)
					{
						if (e.Error == null && e.Result)
						{
							//Get the current status/type..
							int intStatus = ((this._IncSelectedStatus.HasValue) ? this._IncSelectedStatus.Value : this._IncCurrentStatus.Value);
							int intType = ((this._IncSelectedType.HasValue) ? this._IncSelectedType.Value : this._IncCurrentType.Value);
							//Get the current workflow fields here.
							this._clientNumRunning += 2;
							client.Incident_RetrieveWorkflowCustomPropertiesAsync(intType, intStatus, this._clientNum++);
							client.Incident_RetrieveWorkflowFieldsAsync(intType, intStatus, this._clientNum++);
						}
						else
						{
							if (e.Error != null)
							{
								Logger.LogMessage(e.Error);
							}
							else
							{
								Logger.LogMessage(METHOD, "Could not log in.", System.Diagnostics.EventLogEntryType.Error);
							}
						}
					}
				}

				Logger.LogTrace(CLASS + METHOD + " EXIT. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "wkfClient_Connection_ConnectToProjectCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when the client is finished pulling all the workflow fields and their status.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">Incident_RetrieveWorkflowFieldsCompletedEventArgs</param>
		private void wkfClient_Incident_RetrieveWorkflowFieldsCompleted(object sender, Incident_RetrieveWorkflowFieldsCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "wkfClient_Incident_RetrieveWorkflowFieldsCompleted()";
				Logger.LogTrace(CLASS + METHOD + " ENTER.");

				this._clientNumRunning--;
				this.barLoadingIncident.Value++;

				if (sender is ImportExportClient)
				{
					if (!e.Cancelled)
					{
						if (e.Error == null)
						{
							this._WorkflowFields_Updated = this.workflow_LoadFieldStatus(e.Result);

							//Update main workflow fields.
							this.workflow_SetEnabledFields(this._WorkflowFields, this._WorkflowFields_Updated);

							//Hide the status if needed.
							if (this._clientNumRunning == 0)
							{
								this.display_SetOverlayWindow(this.panelStatus, Visibility.Hidden);
								this._isWorkflowChanging = false;
							}
						}
						else
						{
							Logger.LogMessage(e.Error);
						}
					}
				}
				Logger.LogTrace(CLASS + METHOD + " EXIT. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "wkfClient_Incident_RetrieveWorkflowFieldsCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when the client is finished getting custom workflow property fields.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">Incident_RetrieveWorkflowCustomPropertiesCompletedEventArgs</param>
		private void wkfClient_Incident_RetrieveWorkflowCustomPropertiesCompleted(object sender, Incident_RetrieveWorkflowCustomPropertiesCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "wkfClient_Incident_RetrieveWorkflowCustomPropertiesCompleted()";
				Logger.LogTrace(CLASS + METHOD + " ENTER.");

				this._clientNumRunning--;
				this.barLoadingIncident.Value++;

				if (sender is ImportExportClient)
				{
					if (!e.Cancelled)
					{
						if (e.Error == null)
						{
							this._WorkflowCustom_Updated = this.workflow_LoadFieldStatus(e.Result);

							//Update custom workflow fields.
							this.workflow_SetEnabledFields(this._WorkflowCustom, this._WorkflowCustom_Updated);

							//Hide the status if needed.
							if (this._clientNumRunning == 0)
							{
								this.display_SetOverlayWindow(this.panelStatus, Visibility.Hidden);
								this._isWorkflowChanging = false;
							}
						}
						else
						{
							Logger.LogMessage(e.Error);
						}
					}
				}
				Logger.LogTrace(CLASS + METHOD + " EXIT. Clients - Running: " + this._clientNumRunning.ToString() + ", Total: " + this._clientNum.ToString());
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "wkfClient_Incident_RetrieveWorkflowCustomPropertiesCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		#endregion

		/// <summary>Class that holds workflow fields and their UI Controls and statuses.</summary>
		private class WorkflowField
		{
			public WorkflowField(int Number, string Name, Control Control, bool Fixed, bool Hidden, UIElement Label = null)
			{
				this.FieldID = Number;
				this.FieldName = Name;
				this.FieldControl = Control;
				this.IsFixed = Fixed;
				this.IsHidden = Hidden;
				this.FieldLabel = Label;
			}

			/// <summary>The workflow field id.</summary>
			public int FieldID
			{
				get;
				set;
			}

			/// <summary>The plain-text name of the field.</summary>
			public string FieldName
			{
				get;
				set;
			}

			/// <summary>Whether or not the field's status is fixed and cannot be changed.</summary>
			public bool IsFixed
			{
				get;
				set;
			}

			/// <summary>Whether or not the field is hidden (not on the form).</summary>
			public bool IsHidden
			{
				get;
				set;
			}

			/// <summary>The UI Control for the field.</summary>
			public Control FieldControl
			{
				get;
				set;
			}

			public UIElement FieldLabel
			{
				get;
				set;
			}

			/// <summary>Status to refer fields to.</summary>
			public enum WorkflowStatusEnum : int
			{
				/// <summary>Field is active and enabled, not required.</summary>
				Normal = 0,
				/// <summary>Field is inactive, cannot be changed.</summary>
				Inactive = 1,
				/// <summary>Field is enabled and it must be non-empty.</summary>
				Required = 2,
				/// <summary>Field is hidden, not displayed to the user.</summary>
				Hidden = 3
			}
		}
	}
}
