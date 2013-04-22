using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using Inflectra.Global;
using Inflectra.SpiraTest.IDEIntegration.VisualStudio2012.Business;
using Inflectra.SpiraTest.IDEIntegration.VisualStudio2012.Business.SpiraTeam_Client;

namespace Inflectra.SpiraTest.IDEIntegration.VisualStudio2012.Forms
{
	/// <summary>Holds the saving functions for frmDetailsIncident</summary>
	public partial class frmDetailsIncident : UserControl
	{
		//Are we currently saving our data?
		private bool _isSavingInformation = false;
		private int _clientNumSaving;
		private RemoteIncident _IncidentConcurrent;

		/// <summary>Hit when the user wants to save the task.</summary>
		/// <param name="sender">The save button.</param>
		/// <param name="e">RoutedEventArgs</param>
		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			if (e != null)
			{
				e.Handled = true;
			}

			try
			{
				this.barSavingIncident.Value = -5;
				this.barSavingIncident.Maximum = 0;
				this.barSavingIncident.Minimum = -5;

				if (this._isFieldChanged || this._isWkfChanged || this._isResChanged || this._isDescChanged)
				{
					//Set working flag.
					this.IsSaving = true;

					//Get the new values from the form..
					RemoteIncident newIncident = this.save_GetFromFields();

					if (newIncident != null && this.workflow_CheckRequiredFields())
					{
						//Create a client, and save task and resolution..
						ImportExportClient clientSave = StaticFuncs.CreateClient(((SpiraProject)this._ArtifactDetails.ArtifactParentProject.ArtifactTag).ServerURL.ToString());
						clientSave.Connection_Authenticate2Completed += new EventHandler<Connection_Authenticate2CompletedEventArgs>(clientSave_Connection_Authenticate2Completed);
						clientSave.Connection_ConnectToProjectCompleted += new EventHandler<Connection_ConnectToProjectCompletedEventArgs>(clientSave_Connection_ConnectToProjectCompleted);
						clientSave.Incident_UpdateCompleted += new EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(clientSave_Incident_UpdateCompleted);
						clientSave.Incident_AddCommentsCompleted += clientSave_Incident_AddCommentsCompleted;
						clientSave.Connection_DisconnectCompleted += new EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(clientSave_Connection_DisconnectCompleted);

						//Fire off the connection.
						this._clientNumSaving = 1;
						clientSave.Connection_Authenticate2Async(((SpiraProject)this._ArtifactDetails.ArtifactParentProject.ArtifactTag).UserName, ((SpiraProject)this._ArtifactDetails.ArtifactParentProject.ArtifactTag).UserPass, StaticFuncs.getCultureResource.GetString("app_ReportName"), this._clientNum++);
					}
					else
					{
						//Display message saying that some required fields aren't filled out.
						MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_RequiredFieldsMessage"), StaticFuncs.getCultureResource.GetString("app_General_RequiredFields"), MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "btnSave_Click()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}

			if (this._clientNumSaving == 0)
			{
				this.IsSaving = false;
			}
		}

		#region Client Events
		/// <summary>Hit when we're finished connecting.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">AsyncCompletedEventArgs</param>
		private void clientSave_Connection_DisconnectCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "clientSave_Connection_DisconnectCompleted()";
				Logger.LogTrace(CLASS + METHOD + " Enter: " + this._clientNumSaving.ToString() + " running.");

				this._clientNumSaving--;
				this.barSavingIncident.Value++;

				//See if it's okay to reload.
				this.save_CheckIfOkayToLoad();
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "clientSave_Connection_DisconnectCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when we're finished adding a resolution.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">Incident_AddResolutionsCompletedEventArgs</param>
		private void clientSave_Incident_AddCommentsCompleted(object sender, Incident_AddCommentsCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "clientSave_Incident_AddResolutionsCompleted()";
				Logger.LogTrace(CLASS + METHOD + " Enter: " + this._clientNumSaving.ToString() + " running.");

				ImportExportClient client = (sender as ImportExportClient);
				this._clientNumSaving--;
				this.barSavingIncident.Value++;

				if (!e.Cancelled)
				{
					if (e.Error != null)
					{
						//Log message.
						Logger.LogMessage(e.Error, "Adding Comment to Incident");
						//Display error that the item saved, but adding the new resolution didn't.
						MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_AddCommentErrorMessage"), StaticFuncs.getCultureResource.GetString("app_Incident_UpdateError"), MessageBoxButton.OK, MessageBoxImage.Error);
					}

					//Regardless of what happens, we're disconnecting here.
					this._clientNumSaving++;
					client.Connection_DisconnectAsync(this._clientNum++);
				}

				//See if it's okay to reload.
				this.save_CheckIfOkayToLoad();

				Logger.LogTrace(CLASS + METHOD + " Exit: " + this._clientNumSaving.ToString() + " left.");
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "clientSave_Incident_AddResolutionsCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when we're finished updating the main information.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">AsyncCompletedEventArgs</param>
		private void clientSave_Incident_UpdateCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "clientSave_Incident_UpdateCompleted()";
				Logger.LogTrace(CLASS + METHOD + " Enter: " + this._clientNumSaving.ToString() + " running.");

				ImportExportClient client = (sender as ImportExportClient);
				this._clientNumSaving--;
				this.barSavingIncident.Value++;

				if (!e.Cancelled)
				{
					if (e.Error == null)
					{
						//See if we need to add a resolution.
						if (this._isResChanged)
						{
							//We need to save a resolution.
							RemoteComment newRes = new RemoteComment();
							newRes.CreationDate = DateTime.Now;
							newRes.UserId = ((SpiraProject)this._ArtifactDetails.ArtifactParentProject.ArtifactTag).UserID;
							newRes.ArtifactId = this._ArtifactDetails.ArtifactId;
							newRes.Text = this.cntrlResolution.HTMLText;

							this._clientNumSaving++;
							client.Incident_AddCommentsAsync(new List<RemoteComment>() { newRes }, this._clientNum++);
						}
						else
						{
							//We're finished.
							this.barSavingIncident.Value++;
							this._clientNumSaving++;
							client.Connection_DisconnectAsync(this._clientNum++);
						}
					}
					else
					{
						//Log error.
						Logger.LogMessage(e.Error, "Saving Incident Changes to Database");

						//If we get a concurrency error, get the current data.
						if (e.Error is FaultException<ServiceFaultMessage> && ((FaultException<ServiceFaultMessage>)e.Error).Detail.Type == "DataAccessConcurrencyException")
						{
							client.Incident_RetrieveByIdCompleted += new EventHandler<Incident_RetrieveByIdCompletedEventArgs>(clientSave_Incident_RetrieveByIdCompleted);

							//Fire it off.
							this._clientNumSaving++;
							client.Incident_RetrieveByIdAsync(this._ArtifactDetails.ArtifactId, this._clientNum++);
						}
						else
						{
							//Display the error screen here.

							//Cancel calls.
							this._clientNumSaving++;
							client.Connection_DisconnectAsync(this._clientNum++);
						}
					}
				}

				//See if it's okay to reload.
				this.save_CheckIfOkayToLoad();

				Logger.LogTrace(CLASS + METHOD + " Exit: " + this._clientNumSaving.ToString() + " left.");
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "clientSave_Incident_UpdateCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit if we hit a concurrency issue, and have to comapre values.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">Incident_RetrieveByIdCompletedEventArgs</param>
		private void clientSave_Incident_RetrieveByIdCompleted(object sender, Incident_RetrieveByIdCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "clientSave_Incident_RetrieveByIdCompleted()";
				Logger.LogTrace(CLASS + METHOD + " Enter: " + this._clientNumSaving.ToString() + " running.");

				ImportExportClient client = (sender as ImportExportClient);
				this._clientNumSaving--;
				this.barSavingIncident.Value++;


				if (!e.Cancelled)
				{
					if (e.Error == null)
					{
						//We got new information here. Let's see if it can be merged.
						bool canBeMerged = this.save_CheckIfConcurrencyCanBeMerged(e.Result);
						this._IncidentConcurrent = e.Result;

						if (canBeMerged)
						{
							this.gridLoadingError.Visibility = System.Windows.Visibility.Collapsed;
							this.gridSavingConcurrencyMerge.Visibility = System.Windows.Visibility.Visible;
							this.gridSavingConcurrencyNoMerge.Visibility = System.Windows.Visibility.Collapsed;
							this.display_SetOverlayWindow(this.panelSaving, System.Windows.Visibility.Hidden);
							this.display_SetOverlayWindow(this.panelError, System.Windows.Visibility.Visible);

							//Save the client to the 'Merge' button.
							this.btnConcurrencyMergeYes.Tag = sender;
						}
						else
						{
							//TODO: Display error message here, tell users they must refresh their data.
							this.gridLoadingError.Visibility = System.Windows.Visibility.Collapsed;
							this.gridSavingConcurrencyMerge.Visibility = System.Windows.Visibility.Collapsed;
							this.gridSavingConcurrencyNoMerge.Visibility = System.Windows.Visibility.Visible;
							this.display_SetOverlayWindow(this.panelSaving, System.Windows.Visibility.Hidden);
							this.display_SetOverlayWindow(this.panelError, System.Windows.Visibility.Visible);
						}
					}
					else
					{
						//We even errored on retrieving information. Somethin's really wrong here.
						//Display error.
						Logger.LogMessage(e.Error, "Getting updated Concurrency Incident");
					}
				}

				Logger.LogTrace(CLASS + METHOD + " Exit");
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "clientSave_Incident_RetrieveByIdCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when we're finished connecting to the project.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">Connection_ConnectToProjectCompletedEventArgs</param>
		private void clientSave_Connection_ConnectToProjectCompleted(object sender, Connection_ConnectToProjectCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "clientSave_Connection_ConnectToProjectCompleted()";
				Logger.LogTrace(CLASS + METHOD + " Enter: " + this._clientNumSaving.ToString() + " running.");

				ImportExportClient client = (sender as ImportExportClient);
				this._clientNumSaving--;
				this.barSavingIncident.Value++;

				if (!e.Cancelled)
				{
					if (e.Error == null)
					{
						if (e.Result)
						{
							//Get the new RemoteIncident
							RemoteIncident newIncident = this.save_GetFromFields();

							if (newIncident != null)
							{
								//Fire off our update calls.
								this._clientNumSaving++;
								client.Incident_UpdateAsync(newIncident, this._clientNum++);
							}
							else
							{
								//TODO: Show Error.
								//Cancel calls.
								this._clientNumSaving++;
								client.Connection_DisconnectAsync(this._clientNum++);
							}
						}
						else
						{
							//TODO: Show Error.
							//Cancel calls.
							this._clientNumSaving++;
							client.Connection_DisconnectAsync(this._clientNum++);
						}
					}
					else
					{
						//TODO: Show Error.
						//Cancel calls.
						this._clientNumSaving++;
						client.Connection_DisconnectAsync(this._clientNum++);
					}
				}

				//See if it's okay to reload.
				this.save_CheckIfOkayToLoad();

				Logger.LogTrace(CLASS + METHOD + " Exit: " + this._clientNumSaving.ToString() + " left.");
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "clientSave_Connection_ConnectToProjectCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when we're authenticated to the server.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">Connection_Authenticate2CompletedEventArgs</param>
		private void clientSave_Connection_Authenticate2Completed(object sender, Connection_Authenticate2CompletedEventArgs e)
		{
			try
			{
				const string METHOD = "clientSave_Connection_Authenticate2Completed()";
				Logger.LogTrace(CLASS + METHOD + " Enter: " + this._clientNumSaving.ToString() + " running.");

				ImportExportClient client = (sender as ImportExportClient);
				this._clientNumSaving--;
				this.barSavingIncident.Value++;

				if (!e.Cancelled)
				{
					if (e.Error == null)
					{
						if (e.Result)
						{
							//Connect to the progect ID.
							this._clientNumSaving++;
							client.Connection_ConnectToProjectAsync(((SpiraProject)this._ArtifactDetails.ArtifactParentProject.ArtifactTag).ProjectID, this._clientNum++);
						}
						else
						{
							//TODO: Show Error.
							//Cancel calls.
							this._clientNumSaving++;
							client.Connection_DisconnectAsync(this._clientNum++);
						}
					}
					else
					{
						//TODO: Show Error.
						//Cancel calls.
						this._clientNumSaving++;
						client.Connection_DisconnectAsync(this._clientNum++);
					}
				}

				//See if it's okay to reload.
				this.save_CheckIfOkayToLoad();

				Logger.LogTrace(CLASS + METHOD + " Exit: " + this._clientNumSaving.ToString() + " left.");
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "clientSave_Connection_Authenticate2Completed()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		#endregion

		/// <summary>Checks if it's okay to refresh the data details.</summary>
		private void save_CheckIfOkayToLoad()
		{
			try
			{
				const string METHOD = "save_CheckIfOkayToLoad()";
				Logger.LogTrace(CLASS + METHOD + " Enter: " + this._clientNumSaving.ToString() + " running.");

				//If we're down to 0, we have to reload our information.
				if (this._clientNumSaving == 0)
				{
					this.IsSaving = false;
					this.lblLoadingIncident.Text = StaticFuncs.getCultureResource.GetString("app_Incident_Refreshing");
					this.load_LoadItem();
				}

				Logger.LogTrace(CLASS + METHOD + " Exit");
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "save_CheckIfOkayToLoad()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Returns whether the given Concurrent Incident can be safely merged with the user's values.</summary>
		/// <param name="moddedTask">The concurrent task.</param>
		private bool save_CheckIfConcurrencyCanBeMerged(RemoteIncident moddedIncident)
		{
			try
			{
				bool retValue = false;

				//Get current values..
				RemoteIncident userIncident = this.save_GetFromFields();

				if (userIncident != null && moddedIncident != null)
				{
					//Okay, check all fields. We want to see if a user-changed field (userTask) was also
					//   changed by someone else. If it was, we return false (they cannot be merged). Otherwise,
					//   we return true (they can be merged).
					//So we check the user-entered field against the original field. If they are different,
					//   check the original field against the concurrent field. If they are different, return
					//   false. Otherwise to both if's, return true.
					//We just loop through all available fields. The fielNum here has no reference to workflow
					//   field ID, _WorkflowFields is just used to get the count of fields to check against.
					int fieldNum = 1;
					bool fieldCheck = true;
					while (fieldNum < 39 && fieldCheck == true)
					{
						switch (fieldNum)
						{
							case 1:
								if (userIncident.ActualEffort != this._Incident.ActualEffort) fieldCheck = (this._Incident.ActualEffort == moddedIncident.ActualEffort);
								break;
							case 2:
								if (userIncident.ClosedDate != this._Incident.ClosedDate) fieldCheck = (this._Incident.ClosedDate == moddedIncident.ClosedDate);
								break;
							case 3:
								if (userIncident.CreationDate != this._Incident.CreationDate) fieldCheck = (this._Incident.CreationDate == moddedIncident.CreationDate);
								break;
							case 4:
								if (StaticFuncs.StripTagsCharArray(userIncident.Description).ToLowerInvariant().Trim() != StaticFuncs.StripTagsCharArray(this._Incident.Description).ToLowerInvariant().Trim()) fieldCheck = (StaticFuncs.StripTagsCharArray(this._Incident.Description).ToLowerInvariant().Trim() == StaticFuncs.StripTagsCharArray(moddedIncident.Description).ToLowerInvariant().Trim());
								break;
							case 5:
								if (userIncident.DetectedReleaseId != this._Incident.DetectedReleaseId) fieldCheck = (this._Incident.DetectedReleaseId == moddedIncident.DetectedReleaseId);
								break;
							case 6:
								if (userIncident.EstimatedEffort != this._Incident.EstimatedEffort) fieldCheck = (this._Incident.EstimatedEffort == moddedIncident.EstimatedEffort);
								break;
							case 7:
								if (userIncident.IncidentStatusId != this._Incident.IncidentStatusId) fieldCheck = (this._Incident.IncidentStatusId == moddedIncident.IncidentStatusId);
								break;
							case 8:
								if (userIncident.IncidentTypeId != this._Incident.IncidentTypeId) fieldCheck = (this._Incident.IncidentTypeId == moddedIncident.IncidentTypeId);
								break;
							case 9:
								if (userIncident.List01 != this._Incident.List01) fieldCheck = (this._Incident.List01 == moddedIncident.List01);
								break;
							case 10:
								if (userIncident.List02 != this._Incident.List02) fieldCheck = (this._Incident.List02 == moddedIncident.List02);
								break;
							case 11:
								if (userIncident.List03 != this._Incident.List03) fieldCheck = (this._Incident.List03 == moddedIncident.List03);
								break;
							case 12:
								if (userIncident.List04 != this._Incident.List04) fieldCheck = (this._Incident.List04 == moddedIncident.List04);
								break;
							case 13:
								if (userIncident.List05 != this._Incident.List05) fieldCheck = (this._Incident.List05 == moddedIncident.List05);
								break;
							case 14:
								if (userIncident.List06 != this._Incident.List06) fieldCheck = (this._Incident.List06 == moddedIncident.List06);
								break;
							case 15:
								if (userIncident.List07 != this._Incident.List07) fieldCheck = (this._Incident.List07 == moddedIncident.List07);
								break;
							case 16:
								if (userIncident.List08 != this._Incident.List08) fieldCheck = (this._Incident.List08 == moddedIncident.List08);
								break;
							case 17:
								if (userIncident.List09 != this._Incident.List09) fieldCheck = (this._Incident.List09 == moddedIncident.List09);
								break;
							case 18:
								if (userIncident.List10 != this._Incident.List10) fieldCheck = (this._Incident.List10 == moddedIncident.List10);
								break;
							case 19:
								if (userIncident.Name.TrimEquals(this._Incident.Name)) fieldCheck = (this._Incident.Name.TrimEquals(moddedIncident.Name));
								break;
							case 20:
								if (userIncident.OpenerId != this._Incident.OpenerId) fieldCheck = (this._Incident.OpenerId == moddedIncident.OpenerId);
								break;
							case 21:
								if (userIncident.OwnerId != this._Incident.OwnerId) fieldCheck = (this._Incident.OwnerId == moddedIncident.OwnerId);
								break;
							case 22:
								if (userIncident.PriorityId != this._Incident.PriorityId) fieldCheck = (this._Incident.PriorityId == moddedIncident.PriorityId);
								break;
							case 23:
								if (userIncident.RemainingEffort != this._Incident.RemainingEffort) fieldCheck = (this._Incident.RemainingEffort == moddedIncident.RemainingEffort);
								break;
							case 24:
								if (userIncident.ResolvedReleaseId != this._Incident.ResolvedReleaseId) fieldCheck = (this._Incident.ResolvedReleaseId == moddedIncident.ResolvedReleaseId);
								break;
							case 25:
								if (userIncident.SeverityId != this._Incident.SeverityId) fieldCheck = (this._Incident.SeverityId == moddedIncident.SeverityId);
								break;
							case 26:
								if (userIncident.StartDate != this._Incident.StartDate) fieldCheck = (this._Incident.StartDate == moddedIncident.StartDate);
								break;
							case 27:
								if (userIncident.TestRunStepId != this._Incident.TestRunStepId) fieldCheck = (this._Incident.TestRunStepId == moddedIncident.TestRunStepId);
								break;
							case 28:
								if (userIncident.Text01.TrimEquals(this._Incident.Text01)) fieldCheck = (this._Incident.Text01.TrimEquals(moddedIncident.Text01));
								break;
							case 29:
								if (userIncident.Text02.TrimEquals(this._Incident.Text02)) fieldCheck = (this._Incident.Text02.TrimEquals(moddedIncident.Text02));
								break;
							case 30:
								if (userIncident.Text03.TrimEquals(this._Incident.Text03)) fieldCheck = (this._Incident.Text03.TrimEquals(moddedIncident.Text03));
								break;
							case 31:
								if (userIncident.Text04.TrimEquals(this._Incident.Text04)) fieldCheck = (this._Incident.Text04.TrimEquals(moddedIncident.Text04));
								break;
							case 32:
								if (userIncident.Text05.TrimEquals(this._Incident.Text05)) fieldCheck = (this._Incident.Text05.TrimEquals(moddedIncident.Text05));
								break;
							case 33:
								if (userIncident.Text06.TrimEquals(this._Incident.Text06)) fieldCheck = (this._Incident.Text06.TrimEquals(moddedIncident.Text06));
								break;
							case 34:
								if (userIncident.Text07.TrimEquals(this._Incident.Text07)) fieldCheck = (this._Incident.Text07.TrimEquals(moddedIncident.Text07));
								break;
							case 35:
								if (userIncident.Text08.TrimEquals(this._Incident.Text08)) fieldCheck = (this._Incident.Text08.TrimEquals(moddedIncident.Text08));
								break;
							case 36:
								if (userIncident.Text09.TrimEquals(this._Incident.Text09)) fieldCheck = (this._Incident.Text09.TrimEquals(moddedIncident.Text09));
								break;
							case 37:
								if (userIncident.Text10.TrimEquals(this._Incident.Text10)) fieldCheck = (this._Incident.Text10.TrimEquals(moddedIncident.Text10));
								break;
							case 38:
								if (userIncident.VerifiedReleaseId != this._Incident.VerifiedReleaseId) fieldCheck = (this._Incident.VerifiedReleaseId == moddedIncident.VerifiedReleaseId);
								break;
						}
						fieldNum++;
					}
					retValue = fieldCheck;
				}
				return retValue;
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "save_CheckIfConcurrencyCanBeMerged()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
		}

		/// <summary>Copies over our values from the form into an Incident object.</summary>
		/// <returns>A new RemoteIncident, or Null if error.</returns>
		private RemoteIncident save_GetFromFields()
		{
			const string METHOD = "save_GetFromFields()";

			RemoteIncident retIncident = null;
			try
			{
				retIncident = new RemoteIncident();

				//*Fixed fields..
				retIncident.IncidentId = this._Incident.IncidentId;
				retIncident.ProjectId = this._Incident.ProjectId;
				retIncident.CreationDate = this._Incident.CreationDate;
				retIncident.LastUpdateDate = this._Incident.LastUpdateDate;

				//*Standard fields..
				retIncident.Name = this.cntrlIncidentName.Text.Trim();
				retIncident.IncidentTypeId = ((RemoteIncidentType)this.cntrlType.SelectedItem).IncidentTypeId;
				retIncident.IncidentStatusId = ((this._IncSelectedStatus.HasValue) ? this._IncSelectedStatus.Value : this._IncCurrentStatus.Value);
				retIncident.OpenerId = ((RemoteUser)this.cntrlDetectedBy.SelectedItem).UserId;
				retIncident.OwnerId = ((RemoteUser)this.cntrlOwnedBy.SelectedItem).UserId;
				retIncident.PriorityId = ((RemoteIncidentPriority)this.cntrlPriority.SelectedItem).PriorityId;
				retIncident.SeverityId = ((RemoteIncidentSeverity)this.cntrlSeverity.SelectedItem).SeverityId;
				retIncident.DetectedReleaseId = ((RemoteRelease)this.cntrlDetectedIn.SelectedItem).ReleaseId;
				retIncident.ResolvedReleaseId = ((RemoteRelease)this.cntrlResolvedIn.SelectedItem).ReleaseId;
				retIncident.VerifiedReleaseId = ((RemoteRelease)this.cntrlVerifiedIn.SelectedItem).ReleaseId;
				if (this._isDescChanged)
					retIncident.Description = this.cntrlDescription.HTMLText;
				else
					retIncident.Description = this._Incident.Description;

				//*Schedule fields..
				retIncident.StartDate = this.cntrlStartDate.SelectedDate;
				retIncident.ClosedDate = this.cntrlEndDate.SelectedDate;
				retIncident.EstimatedEffort = StaticFuncs.GetMinutesFromValues(this.cntrlEstEffortH.Text, this.cntrlEstEffortM.Text);
				retIncident.ActualEffort = StaticFuncs.GetMinutesFromValues(this.cntrlActEffortH.Text, this.cntrlActEffortM.Text);
				retIncident.RemainingEffort = StaticFuncs.GetMinutesFromValues(this.cntrlRemEffortH.Text, this.cntrlRemEffortM.Text);

				//Custom fields..
				foreach (UIElement eleItem in this.gridCustomProperties.Children)
				{
					//Check to see if the item is a control..
					if (eleItem is Control)
					{
						dynamic custControl = eleItem as dynamic;
						RemoteCustomProperty prop = ((eleItem as Control).Tag) as RemoteCustomProperty;

						if (prop != null)
						{
							int? intSelectedList = null;
							string strSelectedText = null;
							if (prop.CustomPropertyTypeId == 1)
								strSelectedText = custControl.Text;
							else if (prop.CustomPropertyTypeId == 2)
								intSelectedList = custControl.SelectedValue;

							switch (prop.CustomPropertyName)
							{
								case "TEXT_01":
									retIncident.Text01 = strSelectedText;
									break;
								case "TEXT_02":
									retIncident.Text02 = strSelectedText;
									break;
								case "TEXT_03":
									retIncident.Text03 = strSelectedText;
									break;
								case "TEXT_04":
									retIncident.Text04 = strSelectedText;
									break;
								case "TEXT_05":
									retIncident.Text05 = strSelectedText;
									break;
								case "TEXT_06":
									retIncident.Text06 = strSelectedText;
									break;
								case "TEXT_07":
									retIncident.Text07 = strSelectedText;
									break;
								case "TEXT_08":
									retIncident.Text08 = strSelectedText;
									break;
								case "TEXT_09":
									retIncident.Text09 = strSelectedText;
									break;
								case "TEXT_10":
									retIncident.Text10 = strSelectedText;
									break;
								case "LIST_01":
									retIncident.List01 = intSelectedList;
									break;
								case "LIST_02":
									retIncident.List02 = intSelectedList;
									break;
								case "LIST_03":
									retIncident.List03 = intSelectedList;
									break;
								case "LIST_04":
									retIncident.List04 = intSelectedList;
									break;
								case "LIST_05":
									retIncident.List05 = intSelectedList;
									break;
								case "LIST_06":
									retIncident.List06 = intSelectedList;
									break;
								case "LIST_07":
									retIncident.List07 = intSelectedList;
									break;
								case "LIST_08":
									retIncident.List08 = intSelectedList;
									break;
								case "LIST_09":
									retIncident.List09 = intSelectedList;
									break;
								case "LIST_10":
									retIncident.List10 = intSelectedList;
									break;
							}
						}
					}
				}

			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, METHOD);
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
				retIncident = null;
			}

			//Return
			return retIncident;
		}

		#region Concurrency Button Events
		/// <summary>Hit when the user does not want to save, and is forced to refresh the loaded data.</summary>
		/// <param name="sender">btnConcurrencyMergeNo, btnConcurrencyRefresh</param>
		/// <param name="e">RoutedEventArgs</param>
		private void btnConcurrencyRefresh_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				//Hide the error panel, jump to loading..
				this.display_SetOverlayWindow(this.panelError, System.Windows.Visibility.Collapsed);
				this.display_SetOverlayWindow(this.panelStatus, System.Windows.Visibility.Visible);
				this.lblLoadingIncident.Text = StaticFuncs.getCultureResource.GetString("app_Incident_Refreshing");

				this.load_LoadItem();
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "btnConcurrencyRefresh_Click()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when the user wants to merge their changes with the concurrent task.</summary>
		/// <param name="sender">btnConcurrencyMergeYes</param>
		/// <param name="e">RoutedEventArgs</param>
		private void btnConcurrencyMergeYes_Click(object sender, RoutedEventArgs e)
		{
			try
			{

				e.Handled = true;
				//Get the client.
				ImportExportClient client = ((dynamic)sender).Tag as ImportExportClient;
				if (client != null)
				{
					//Switch screens again...
					this.display_SetOverlayWindow(this.panelSaving, System.Windows.Visibility.Visible);
					this.display_SetOverlayWindow(this.panelError, System.Windows.Visibility.Hidden);
					this.barSavingIncident.Value--;

					//Re-launch the saving..
					RemoteIncident incMerged = this.save_MergeConcurrency(this.save_GetFromFields(), this._IncidentConcurrent, this._Incident);

					this._clientNumSaving++;
					client.Incident_UpdateAsync(incMerged, this._clientNum++);
				}
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "btnConcurrencyMergeYes_Click()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		#endregion

		/// <summary>Merges two RemoteIncidents into one for re-saving.</summary>
		/// <param name="tskUserSaved">The user-saved task to merge with the Concurrent task.</param>
		/// <param name="tskConcurrent">The concurrent task to merge with the User task.</param>
		/// <param name="tskOriginal">The original unchanged task used for reference.</param>
		/// <returns>A new RemoteIncident suitable for saving.</returns>
		/// <remarks>This should only be called when it is known that there are no conflicting values between the User-Saved task and the Concurrent task.</remarks>
		private RemoteIncident save_MergeConcurrency(RemoteIncident incUserSaved, RemoteIncident incConcurrent, RemoteIncident incOriginal)
		{
			//If the field was not changed by the user (tskUserSaved == tskOriginal), then use the tskConcurrent. (Assuming that the
			// tskConcurrent has a possible updated value.
			//Otherwise, use the tskUserSaved value.
			try
			{
				RemoteIncident retIncident = new RemoteIncident();

				retIncident.ActualEffort = ((incUserSaved.ActualEffort == incOriginal.ActualEffort) ? incConcurrent.ActualEffort : incUserSaved.ActualEffort);
				retIncident.ClosedDate = ((incUserSaved.ClosedDate == incOriginal.ClosedDate) ? incConcurrent.ClosedDate : incUserSaved.ClosedDate);
				retIncident.CreationDate = incOriginal.CreationDate;
				string strDescUser = StaticFuncs.StripTagsCharArray(incUserSaved.Description);
				string strDescOrig = StaticFuncs.StripTagsCharArray(incOriginal.Description);
				retIncident.Description = ((strDescOrig.TrimEquals(strDescOrig)) ? incConcurrent.Description : incUserSaved.Description);
				retIncident.DetectedReleaseId = ((incUserSaved.DetectedReleaseId == incOriginal.DetectedReleaseId) ? incConcurrent.DetectedReleaseId : incUserSaved.DetectedReleaseId);
				retIncident.EstimatedEffort = ((incUserSaved.EstimatedEffort == incOriginal.EstimatedEffort) ? incConcurrent.EstimatedEffort : incUserSaved.EstimatedEffort);
				retIncident.IncidentId = incOriginal.IncidentId;
				retIncident.IncidentStatusId = ((incUserSaved.IncidentStatusId == incOriginal.IncidentStatusId) ? incConcurrent.IncidentStatusId : incUserSaved.IncidentStatusId);
				retIncident.IncidentTypeId = ((incUserSaved.IncidentTypeId == incOriginal.IncidentTypeId) ? incConcurrent.IncidentTypeId : incUserSaved.IncidentTypeId);
				retIncident.LastUpdateDate = incConcurrent.LastUpdateDate;
				retIncident.List01 = ((incUserSaved.List01 == incOriginal.List01) ? incConcurrent.List01 : incUserSaved.List01);
				retIncident.List02 = ((incUserSaved.List02 == incOriginal.List02) ? incConcurrent.List02 : incUserSaved.List02);
				retIncident.List03 = ((incUserSaved.List03 == incOriginal.List03) ? incConcurrent.List03 : incUserSaved.List03);
				retIncident.List04 = ((incUserSaved.List04 == incOriginal.List04) ? incConcurrent.List04 : incUserSaved.List04);
				retIncident.List05 = ((incUserSaved.List05 == incOriginal.List05) ? incConcurrent.List05 : incUserSaved.List05);
				retIncident.List06 = ((incUserSaved.List06 == incOriginal.List06) ? incConcurrent.List06 : incUserSaved.List06);
				retIncident.List07 = ((incUserSaved.List07 == incOriginal.List07) ? incConcurrent.List07 : incUserSaved.List07);
				retIncident.List08 = ((incUserSaved.List08 == incOriginal.List08) ? incConcurrent.List08 : incUserSaved.List08);
				retIncident.List09 = ((incUserSaved.List09 == incOriginal.List09) ? incConcurrent.List09 : incUserSaved.List09);
				retIncident.List10 = ((incUserSaved.List10 == incOriginal.List10) ? incConcurrent.List10 : incUserSaved.List10);
				retIncident.Name = ((incUserSaved.Name.TrimEquals(incOriginal.Name)) ? incConcurrent.Name : incUserSaved.Name);
				retIncident.OpenerId = ((incUserSaved.OpenerId == incOriginal.OpenerId) ? incConcurrent.OpenerId : incUserSaved.OpenerId);
				retIncident.OwnerId = ((incUserSaved.OwnerId == incOriginal.OwnerId) ? incConcurrent.OwnerId : incUserSaved.OwnerId);
				retIncident.PriorityId = ((incUserSaved.PriorityId == incOriginal.PriorityId) ? incConcurrent.PriorityId : incUserSaved.PriorityId);
				retIncident.ProjectId = incOriginal.ProjectId;
				retIncident.RemainingEffort = ((incUserSaved.RemainingEffort == incOriginal.RemainingEffort) ? incConcurrent.RemainingEffort : incUserSaved.RemainingEffort);
				retIncident.ResolvedReleaseId = ((incUserSaved.ResolvedReleaseId == incOriginal.ResolvedReleaseId) ? incConcurrent.ResolvedReleaseId : incUserSaved.ResolvedReleaseId);
				retIncident.SeverityId = ((incUserSaved.SeverityId == incOriginal.SeverityId) ? incConcurrent.SeverityId : incUserSaved.SeverityId);
				retIncident.StartDate = ((incUserSaved.StartDate == incOriginal.StartDate) ? incConcurrent.StartDate : incUserSaved.StartDate);
				retIncident.TestRunStepId = ((incUserSaved.TestRunStepId == incOriginal.TestRunStepId) ? incConcurrent.TestRunStepId : incUserSaved.TestRunStepId);
				retIncident.Text01 = ((retIncident.Text01.TrimEquals(incOriginal.Text01)) ? incConcurrent.Text01 : incUserSaved.Text01);
				retIncident.Text02 = ((retIncident.Text02.TrimEquals(incOriginal.Text02)) ? incConcurrent.Text02 : incUserSaved.Text02);
				retIncident.Text03 = ((retIncident.Text03.TrimEquals(incOriginal.Text03)) ? incConcurrent.Text03 : incUserSaved.Text03);
				retIncident.Text04 = ((retIncident.Text04.TrimEquals(incOriginal.Text04)) ? incConcurrent.Text04 : incUserSaved.Text04);
				retIncident.Text05 = ((retIncident.Text05.TrimEquals(incOriginal.Text05)) ? incConcurrent.Text05 : incUserSaved.Text05);
				retIncident.Text06 = ((retIncident.Text06.TrimEquals(incOriginal.Text06)) ? incConcurrent.Text06 : incUserSaved.Text06);
				retIncident.Text07 = ((retIncident.Text07.TrimEquals(incOriginal.Text07)) ? incConcurrent.Text07 : incUserSaved.Text07);
				retIncident.Text08 = ((retIncident.Text08.TrimEquals(incOriginal.Text01)) ? incConcurrent.Text08 : incUserSaved.Text08);
				retIncident.Text09 = ((retIncident.Text09.TrimEquals(incOriginal.Text09)) ? incConcurrent.Text09 : incUserSaved.Text09);
				retIncident.Text10 = ((retIncident.Text10.TrimEquals(incOriginal.Text10)) ? incConcurrent.Text10 : incUserSaved.Text10);
				retIncident.VerifiedReleaseId = ((incUserSaved.VerifiedReleaseId == incOriginal.VerifiedReleaseId) ? incConcurrent.VerifiedReleaseId : incUserSaved.VerifiedReleaseId);

				//Return our new task.
				return retIncident;
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "clientSave_Connection_ConnectToProjectCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
				return null;
			}
		}
	}
}
