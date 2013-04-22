using System;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using Inflectra.Global;
using Inflectra.SpiraTest.IDEIntegration.VisualStudio2012.Business;
using Inflectra.SpiraTest.IDEIntegration.VisualStudio2012.Business.SpiraTeam_Client;

namespace Inflectra.SpiraTest.IDEIntegration.VisualStudio2012.Forms
{
	/// <summary>Holds the saving functions for frmDetailsIncident</summary>
	public partial class frmDetailsRequirement : UserControl
	{
		//Are we currently saving our data?
		private bool _isSavingInformation = false;
		private int _clientNumSaving;
		private RemoteRequirement _RequirementConcurrent;

		/// <summary>Hit when the user wants to save the task.</summary>
		/// <param name="sender">The save button.</param>
		/// <param name="e">RoutedEventArgs</param>
		private void btnSave_Click(object sender, RoutedEventArgs e)
		{

			try
			{
				e.Handled = true;
			}
			catch { }

			try
			{
 
				this.barSavingIncident.Value = -5;
				this.barSavingIncident.Maximum = 0;
				this.barSavingIncident.Minimum = -5;

				if (this._isFieldChanged || this._isResChanged || this._isDescChanged)
				{
					//Set working flag.
					this.IsSaving = true;

					//Get the new values from the form..
					RemoteRequirement newRequirement = this.save_GetFromFields();

					if (newRequirement != null)
					{
						//Create a client, and save task and resolution..
						ImportExportClient clientSave = StaticFuncs.CreateClient(((SpiraProject)this._ArtifactDetails.ArtifactParentProject.ArtifactTag).ServerURL.ToString());
						clientSave.Connection_Authenticate2Completed += new EventHandler<Connection_Authenticate2CompletedEventArgs>(clientSave_Connection_Authenticate2Completed);
						clientSave.Connection_ConnectToProjectCompleted += new EventHandler<Connection_ConnectToProjectCompletedEventArgs>(clientSave_Connection_ConnectToProjectCompleted);
						clientSave.Requirement_UpdateCompleted += new EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(clientSave_Requirement_UpdateCompleted);
						clientSave.Requirement_CreateCommentCompleted += new EventHandler<Requirement_CreateCommentCompletedEventArgs>(clientSave_Requirement_CreateCommentCompleted);
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
		private void clientSave_Requirement_CreateCommentCompleted(object sender, Requirement_CreateCommentCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "clientSave_Requirement_CreateCommentCompleted()";
				Logger.LogTrace(CLASS + METHOD + " Enter: " + this._clientNumSaving.ToString() + " running.");

				ImportExportClient client = (sender as ImportExportClient);
				this._clientNumSaving--;
				this.barSavingIncident.Value++;

				if (!e.Cancelled)
				{
					if (e.Error != null)
					{
						//Log message.
						Logger.LogMessage(e.Error, "Adding Comment to Requirement");
						//Display error that the item saved, but adding the new resolution didn't.
						MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_AddCommentErrorMessage"), StaticFuncs.getCultureResource.GetString("app_General_UpdateError"), MessageBoxButton.OK, MessageBoxImage.Error);
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
				Logger.LogMessage(ex, "clientSave_Requirement_CreateCommentCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when we're finished updating the main information.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">AsyncCompletedEventArgs</param>
		private void clientSave_Requirement_UpdateCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "clientSave_Requirement_UpdateCompleted()";
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
							client.Requirement_CreateCommentAsync(newRes, this._clientNum++);
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
							client.Requirement_RetrieveByIdCompleted += new EventHandler<Requirement_RetrieveByIdCompletedEventArgs>(client_Requirement_RetrieveByIdCompleted);

							//Fire it off.
							this._clientNumSaving++;
							client.Requirement_RetrieveByIdAsync(this._ArtifactDetails.ArtifactId, this._clientNum++);
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
				Logger.LogMessage(ex, "clientSave_Requirement_UpdateCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit if we hit a concurrency issue, and have to compare values.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">Incident_RetrieveByIdCompletedEventArgs</param>
		private void client_Requirement_RetrieveByIdCompleted(object sender, Requirement_RetrieveByIdCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "client_Requirement_RetrieveByIdCompleted()";
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
						this._RequirementConcurrent = e.Result;

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
				Logger.LogMessage(ex, "client_Requirement_RetrieveByIdCompleted()");
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
							RemoteRequirement newRequirement = this.save_GetFromFields();

							if (newRequirement != null)
							{
								//Fire off our update calls.
								this._clientNumSaving++;
								client.Requirement_UpdateAsync(newRequirement, this._clientNum++);
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
					this.lblLoadingIncident.Text = StaticFuncs.getCultureResource.GetString("app_Requirement_Refreshing");
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
		private bool save_CheckIfConcurrencyCanBeMerged(RemoteRequirement moddedRequirement)
		{
			try
			{
				bool retValue = false;

				//Get current values..
				RemoteRequirement userRequirement = this.save_GetFromFields();

				if (userRequirement != null && moddedRequirement != null)
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
					while (fieldNum < 29 && fieldCheck == true)
					{
						switch (fieldNum)
						{
							case 1:
								if (userRequirement.AuthorId != this._Requirement.AuthorId) fieldCheck = (this._Requirement.AuthorId == moddedRequirement.AuthorId);
								break;
							case 2:
								if (StaticFuncs.StripTagsCharArray(userRequirement.Description).ToLowerInvariant().Trim() != StaticFuncs.StripTagsCharArray(this._Requirement.Description).ToLowerInvariant().Trim()) fieldCheck = (StaticFuncs.StripTagsCharArray(this._Requirement.Description).ToLowerInvariant().Trim() == StaticFuncs.StripTagsCharArray(moddedRequirement.Description).ToLowerInvariant().Trim());
								break;
							case 3:
								if (userRequirement.ImportanceId != this._Requirement.ImportanceId) fieldCheck = (this._Requirement.ImportanceId == moddedRequirement.ImportanceId);
								break;
							case 4:
								if (userRequirement.List01 != this._Requirement.List01) fieldCheck = (this._Requirement.List01 == moddedRequirement.List01);
								break;
							case 5:
								if (userRequirement.List02 != this._Requirement.List02) fieldCheck = (this._Requirement.List02 == moddedRequirement.List02);
								break;
							case 6:
								if (userRequirement.List03 != this._Requirement.List03) fieldCheck = (this._Requirement.List03 == moddedRequirement.List03);
								break;
							case 7:
								if (userRequirement.List04 != this._Requirement.List04) fieldCheck = (this._Requirement.List04 == moddedRequirement.List04);
								break;
							case 8:
								if (userRequirement.List05 != this._Requirement.List05) fieldCheck = (this._Requirement.List05 == moddedRequirement.List05);
								break;
							case 9:
								if (userRequirement.List06 != this._Requirement.List06) fieldCheck = (this._Requirement.List06 == moddedRequirement.List06);
								break;
							case 10:
								if (userRequirement.List07 != this._Requirement.List07) fieldCheck = (this._Requirement.List07 == moddedRequirement.List07);
								break;
							case 11:
								if (userRequirement.List08 != this._Requirement.List08) fieldCheck = (this._Requirement.List08 == moddedRequirement.List08);
								break;
							case 12:
								if (userRequirement.List09 != this._Requirement.List09) fieldCheck = (this._Requirement.List09 == moddedRequirement.List09);
								break;
							case 13:
								if (userRequirement.List10 != this._Requirement.List10) fieldCheck = (this._Requirement.List10 == moddedRequirement.List10);
								break;
							case 14:
								if (userRequirement.Name.TrimEquals(this._Requirement.Name)) fieldCheck = (this._Requirement.Name.TrimEquals(moddedRequirement.Name));
								break;
							case 15:
								if (userRequirement.OwnerId != this._Requirement.OwnerId) fieldCheck = (this._Requirement.OwnerId == moddedRequirement.OwnerId);
								break;
							case 16:
								if (userRequirement.PlannedEffort != this._Requirement.PlannedEffort) fieldCheck = (this._Requirement.PlannedEffort == moddedRequirement.PlannedEffort);
								break;
							case 17:
								if (userRequirement.ReleaseId != this._Requirement.ReleaseId) fieldCheck = (this._Requirement.ReleaseId == moddedRequirement.ReleaseId);
								break;
							case 18:
								if (userRequirement.StatusId != this._Requirement.StatusId) fieldCheck = (this._Requirement.StatusId == moddedRequirement.StatusId);
								break;
							case 19:
								if (userRequirement.Text01.TrimEquals(this._Requirement.Text01)) fieldCheck = (this._Requirement.Text01.TrimEquals(moddedRequirement.Text01));
								break;
							case 20:
								if (userRequirement.Text02.TrimEquals(this._Requirement.Text02)) fieldCheck = (this._Requirement.Text02.TrimEquals(moddedRequirement.Text02));
								break;
							case 21:
								if (userRequirement.Text03.TrimEquals(this._Requirement.Text03)) fieldCheck = (this._Requirement.Text03.TrimEquals(moddedRequirement.Text03));
								break;
							case 22:
								if (userRequirement.Text04.TrimEquals(this._Requirement.Text04)) fieldCheck = (this._Requirement.Text04.TrimEquals(moddedRequirement.Text04));
								break;
							case 23:
								if (userRequirement.Text05.TrimEquals(this._Requirement.Text05)) fieldCheck = (this._Requirement.Text05.TrimEquals(moddedRequirement.Text05));
								break;
							case 24:
								if (userRequirement.Text06.TrimEquals(this._Requirement.Text06)) fieldCheck = (this._Requirement.Text06.TrimEquals(moddedRequirement.Text06));
								break;
							case 25:
								if (userRequirement.Text07.TrimEquals(this._Requirement.Text07)) fieldCheck = (this._Requirement.Text07.TrimEquals(moddedRequirement.Text07));
								break;
							case 26:
								if (userRequirement.Text08.TrimEquals(this._Requirement.Text08)) fieldCheck = (this._Requirement.Text08.TrimEquals(moddedRequirement.Text08));
								break;
							case 27:
								if (userRequirement.Text09.TrimEquals(this._Requirement.Text09)) fieldCheck = (this._Requirement.Text09.TrimEquals(moddedRequirement.Text09));
								break;
							case 28:
								if (userRequirement.Text10.TrimEquals(this._Requirement.Text10)) fieldCheck = (this._Requirement.Text10.TrimEquals(moddedRequirement.Text10));
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
		private RemoteRequirement save_GetFromFields()
		{
			const string METHOD = "save_GetFromFields()";

			RemoteRequirement retRequirement = null;
			try
			{
				retRequirement = new RemoteRequirement();

				//*Fixed fields..
				retRequirement.CoverageCountBlocked = this._Requirement.CoverageCountBlocked;
				retRequirement.CoverageCountCaution = this._Requirement.CoverageCountCaution;
				retRequirement.CoverageCountFailed = this._Requirement.CoverageCountFailed;
				retRequirement.CoverageCountPassed = this._Requirement.CoverageCountPassed;
				retRequirement.CoverageCountTotal = this._Requirement.CoverageCountTotal;
				retRequirement.CreationDate = this._Requirement.CreationDate;
				retRequirement.IndentLevel = this._Requirement.IndentLevel;
				retRequirement.LastUpdateDate = this._Requirement.LastUpdateDate;
				retRequirement.ProjectId = this._Requirement.ProjectId;
				retRequirement.RequirementId = this._Requirement.RequirementId;
				retRequirement.Summary = this._Requirement.Summary;
				retRequirement.TaskActualEffort = this._Requirement.TaskActualEffort;
				retRequirement.TaskCount = this._Requirement.TaskCount;
				retRequirement.TaskEstimatedEffort = this._Requirement.TaskEstimatedEffort;

				//*Standard fields..
				retRequirement.AuthorId = ((RemoteUser)this.cntrlCreatedBy.SelectedItem).UserId;
				if (this._isDescChanged)
					retRequirement.Description = this.cntrlDescription.HTMLText;
				else
					retRequirement.Description = this._Requirement.Description;
				retRequirement.ImportanceId = ((RequirementPriority)this.cntrlImportance.SelectedItem).PriorityId;
				retRequirement.Name = this.cntrlName.Text.Trim();
				retRequirement.OwnerId = ((RemoteUser)this.cntrlOwnedBy.SelectedItem).UserId;
				retRequirement.PlannedEffort = StaticFuncs.GetMinutesFromValues(this.cntrlPlnEffortH.Text, this.cntrlPlnEffortM.Text);
				retRequirement.ReleaseId = ((RemoteRelease)this.cntrlRelease.SelectedItem).ReleaseId;
				retRequirement.StatusId = ((RequirementStatus)this.cntrlStatus.SelectedItem).StatusId;

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
									retRequirement.Text01 = strSelectedText;
									break;
								case "TEXT_02":
									retRequirement.Text02 = strSelectedText;
									break;
								case "TEXT_03":
									retRequirement.Text03 = strSelectedText;
									break;
								case "TEXT_04":
									retRequirement.Text04 = strSelectedText;
									break;
								case "TEXT_05":
									retRequirement.Text05 = strSelectedText;
									break;
								case "TEXT_06":
									retRequirement.Text06 = strSelectedText;
									break;
								case "TEXT_07":
									retRequirement.Text07 = strSelectedText;
									break;
								case "TEXT_08":
									retRequirement.Text08 = strSelectedText;
									break;
								case "TEXT_09":
									retRequirement.Text09 = strSelectedText;
									break;
								case "TEXT_10":
									retRequirement.Text10 = strSelectedText;
									break;
								case "LIST_01":
									retRequirement.List01 = intSelectedList;
									break;
								case "LIST_02":
									retRequirement.List02 = intSelectedList;
									break;
								case "LIST_03":
									retRequirement.List03 = intSelectedList;
									break;
								case "LIST_04":
									retRequirement.List04 = intSelectedList;
									break;
								case "LIST_05":
									retRequirement.List05 = intSelectedList;
									break;
								case "LIST_06":
									retRequirement.List06 = intSelectedList;
									break;
								case "LIST_07":
									retRequirement.List07 = intSelectedList;
									break;
								case "LIST_08":
									retRequirement.List08 = intSelectedList;
									break;
								case "LIST_09":
									retRequirement.List09 = intSelectedList;
									break;
								case "LIST_10":
									retRequirement.List10 = intSelectedList;
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
				retRequirement = null;
			}

			//Return
			return retRequirement;
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
				this.lblLoadingIncident.Text = StaticFuncs.getCultureResource.GetString("app_Requirement_Refreshing");

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
					RemoteRequirement reqMerged = this.save_MergeConcurrency(this.save_GetFromFields(), this._RequirementConcurrent, this._Requirement);

					this._clientNumSaving++;
					client.Requirement_UpdateAsync(reqMerged, this._clientNum++);
				}
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "btnConcurrencyMergeYes_Click()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		#endregion

		/// <summary>Merges two RemoteRequirements into one for re-saving.</summary>
		/// <param name="reqUserSaved">The user-saved requirement to merge with the Concurrent requirement.</param>
		/// <param name="reqConcurrent">The concurrent requirement to merge with the User requirement.</param>
		/// <param name="reqOriginal">The original unchanged requirement used for reference.</param>
		/// <returns>A new RemoteRequirement suitable for saving.</returns>
		/// <remarks>This should only be called when it is known that there are no conflicting values between the User-Saved requirement and the Concurrent requirement.</remarks>
		private RemoteRequirement save_MergeConcurrency(RemoteRequirement reqUserSaved, RemoteRequirement reqConcurrent, RemoteRequirement reqOriginal)
		{
			//If the field was not changed by the user (reqUserSaved == reqOriginal), then use the reqConcurrent. (Assuming that the
			// reqConcurrent has a possible updated value.
			//Otherwise, use the reqUserSaved value.
			try
			{
				RemoteRequirement retRequirement = new RemoteRequirement();

				//Let do fixed fields first.
				retRequirement.CoverageCountBlocked = reqConcurrent.CoverageCountBlocked;
				retRequirement.CoverageCountCaution = reqConcurrent.CoverageCountCaution;
				retRequirement.CoverageCountFailed = reqConcurrent.CoverageCountFailed;
				retRequirement.CoverageCountPassed = reqConcurrent.CoverageCountPassed;
				retRequirement.CoverageCountTotal = reqConcurrent.CoverageCountTotal;
				retRequirement.CreationDate = reqConcurrent.CreationDate;
				retRequirement.IndentLevel = reqConcurrent.IndentLevel;
				retRequirement.LastUpdateDate = reqConcurrent.LastUpdateDate;
				retRequirement.ProjectId = reqConcurrent.ProjectId;
				retRequirement.RequirementId = reqConcurrent.RequirementId;
				retRequirement.Summary = reqConcurrent.Summary;
				retRequirement.TaskActualEffort = reqConcurrent.TaskActualEffort;
				retRequirement.TaskCount = reqConcurrent.TaskCount;
				retRequirement.TaskEstimatedEffort = reqConcurrent.TaskEstimatedEffort;

				//Now the user fields..
				retRequirement.AuthorId = ((reqUserSaved.AuthorId == reqOriginal.AuthorId) ? reqConcurrent.AuthorId : reqUserSaved.AuthorId);
				string strDescUser = StaticFuncs.StripTagsCharArray(reqUserSaved.Description);
				string strDescOrig = StaticFuncs.StripTagsCharArray(reqOriginal.Description);
				retRequirement.Description = ((strDescOrig.TrimEquals(strDescOrig)) ? reqConcurrent.Description : reqUserSaved.Description);
				retRequirement.ImportanceId = ((reqUserSaved.ImportanceId == reqOriginal.ImportanceId) ? reqConcurrent.ImportanceId : reqUserSaved.ImportanceId);
				retRequirement.List01 = ((reqUserSaved.List01 == reqOriginal.List01) ? reqConcurrent.List01 : reqUserSaved.List01);
				retRequirement.List02 = ((reqUserSaved.List02 == reqOriginal.List02) ? reqConcurrent.List02 : reqUserSaved.List02);
				retRequirement.List03 = ((reqUserSaved.List03 == reqOriginal.List03) ? reqConcurrent.List03 : reqUserSaved.List03);
				retRequirement.List04 = ((reqUserSaved.List04 == reqOriginal.List04) ? reqConcurrent.List04 : reqUserSaved.List04);
				retRequirement.List05 = ((reqUserSaved.List05 == reqOriginal.List05) ? reqConcurrent.List05 : reqUserSaved.List05);
				retRequirement.List06 = ((reqUserSaved.List06 == reqOriginal.List06) ? reqConcurrent.List06 : reqUserSaved.List06);
				retRequirement.List07 = ((reqUserSaved.List07 == reqOriginal.List07) ? reqConcurrent.List07 : reqUserSaved.List07);
				retRequirement.List08 = ((reqUserSaved.List08 == reqOriginal.List08) ? reqConcurrent.List08 : reqUserSaved.List08);
				retRequirement.List09 = ((reqUserSaved.List09 == reqOriginal.List09) ? reqConcurrent.List09 : reqUserSaved.List09);
				retRequirement.List10 = ((reqUserSaved.List10 == reqOriginal.List10) ? reqConcurrent.List10 : reqUserSaved.List10);
				retRequirement.Name = ((reqUserSaved.Name == reqOriginal.Name) ? reqConcurrent.Name : reqUserSaved.Name);
				retRequirement.OwnerId = ((reqUserSaved.OwnerId == reqOriginal.OwnerId) ? reqConcurrent.OwnerId : reqUserSaved.OwnerId);
				retRequirement.PlannedEffort = ((reqUserSaved.PlannedEffort == reqOriginal.PlannedEffort) ? reqConcurrent.PlannedEffort : reqUserSaved.PlannedEffort);
				retRequirement.ReleaseId = ((reqUserSaved.ReleaseId == reqOriginal.ReleaseId) ? reqConcurrent.ReleaseId : reqUserSaved.ReleaseId);
				retRequirement.StatusId = ((reqUserSaved.StatusId == reqOriginal.StatusId) ? reqConcurrent.StatusId : reqUserSaved.StatusId);
				retRequirement.Text01 = ((retRequirement.Text01.TrimEquals(reqOriginal.Text01)) ? reqConcurrent.Text01 : reqUserSaved.Text01);
				retRequirement.Text02 = ((retRequirement.Text02.TrimEquals(reqOriginal.Text02)) ? reqConcurrent.Text02 : reqUserSaved.Text02);
				retRequirement.Text03 = ((retRequirement.Text03.TrimEquals(reqOriginal.Text03)) ? reqConcurrent.Text03 : reqUserSaved.Text03);
				retRequirement.Text04 = ((retRequirement.Text04.TrimEquals(reqOriginal.Text04)) ? reqConcurrent.Text04 : reqUserSaved.Text04);
				retRequirement.Text05 = ((retRequirement.Text05.TrimEquals(reqOriginal.Text05)) ? reqConcurrent.Text05 : reqUserSaved.Text05);
				retRequirement.Text06 = ((retRequirement.Text06.TrimEquals(reqOriginal.Text06)) ? reqConcurrent.Text06 : reqUserSaved.Text06);
				retRequirement.Text07 = ((retRequirement.Text07.TrimEquals(reqOriginal.Text07)) ? reqConcurrent.Text07 : reqUserSaved.Text07);
				retRequirement.Text08 = ((retRequirement.Text08.TrimEquals(reqOriginal.Text01)) ? reqConcurrent.Text08 : reqUserSaved.Text08);
				retRequirement.Text09 = ((retRequirement.Text09.TrimEquals(reqOriginal.Text09)) ? reqConcurrent.Text09 : reqUserSaved.Text09);
				retRequirement.Text10 = ((retRequirement.Text10.TrimEquals(reqOriginal.Text10)) ? reqConcurrent.Text10 : reqUserSaved.Text10);

				//Return our new task.
				return retRequirement;
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "save_MergeConcurrency()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
				return null;
			}
		}
	}
}
