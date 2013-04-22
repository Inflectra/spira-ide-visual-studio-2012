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
	public partial class frmDetailsTask : UserControl
	{
		//Are we currently saving our data?
		private bool _isSavingInformation = false;
		private int _clientNumSaving;
		private RemoteTask _TaskConcurrent;

		/// <summary>Hit when the user wants to save the requirement.</summary>
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

				this.barSavingTask.Value = -5;
				this.barSavingTask.Maximum = 0;
				this.barSavingTask.Minimum = -5;

				if (this._isFieldChanged || this._isResChanged || this._isDescChanged)
				{
					//Set working flag.
					this.IsSaving = true;

					//Get the new values from the form..
					RemoteTask newTask = this.save_GetFromFields();

					if (newTask != null)
					{
						//Create a client, and save requirement and resolution..
						ImportExportClient clientSave = StaticFuncs.CreateClient(((SpiraProject)this._ArtifactDetails.ArtifactParentProject.ArtifactTag).ServerURL.ToString());
						clientSave.Connection_Authenticate2Completed += new EventHandler<Connection_Authenticate2CompletedEventArgs>(clientSave_Connection_Authenticate2Completed);
						clientSave.Connection_ConnectToProjectCompleted += new EventHandler<Connection_ConnectToProjectCompletedEventArgs>(clientSave_Connection_ConnectToProjectCompleted);
						clientSave.Task_UpdateCompleted += new EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(clientSave_Task_UpdateCompleted);
						clientSave.Task_CreateCommentCompleted += new EventHandler<Task_CreateCommentCompletedEventArgs>(clientSave_Task_CreateCommentCompleted);
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
				this.barSavingTask.Value++;

				//See if it's okay to reload.
				this.save_CheckIfOkayToLoad();
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "clientSave_Connection_DisconnectCompleted()");
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
				this.barSavingTask.Value++;

				if (!e.Cancelled)
				{
					if (e.Error == null)
					{
						if (e.Result)
						{
							//Get the new RemoteIncident
							RemoteTask newTask = this.save_GetFromFields();

							if (newTask != null)
							{
								//Fire off our update calls.
								this._clientNumSaving++;
								client.Task_UpdateAsync(newTask, this._clientNum++);
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
				this.barSavingTask.Value++;

				if (!e.Cancelled)
				{
					if (e.Error == null)
					{
						if (e.Result)
						{
							//Connect to the project ID.
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

		/// <summary>Hit when the client is finished updating the task.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">AsyncCompletedEventArgs</param>
		private void clientSave_Task_UpdateCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "clientSave_Incident_UpdateCompleted()";
				Logger.LogTrace(CLASS + METHOD + " Enter: " + this._clientNumSaving.ToString() + " running.");

				ImportExportClient client = (sender as ImportExportClient);
				this._clientNumSaving--;
				this.barSavingTask.Value++;

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
							client.Task_CreateCommentAsync(newRes, this._clientNum++);
						}
						else
						{
							//We're finished.
							this.barSavingTask.Value++;
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
							client.Task_RetrieveByIdCompleted += new EventHandler<Task_RetrieveByIdCompletedEventArgs>(clientSave_Task_RetrieveByIdCompleted);

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

		/// <summary>Hit when we had a concurrency issue, and had to reload the task.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">Task_RetrieveByIdCompletedEventArgs</param>
		private void clientSave_Task_RetrieveByIdCompleted(object sender, Task_RetrieveByIdCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "clientSave_Incident_RetrieveByIdCompleted()";
				Logger.LogTrace(CLASS + METHOD + " Enter: " + this._clientNumSaving.ToString() + " running.");

				ImportExportClient client = (sender as ImportExportClient);
				this._clientNumSaving--;
				this.barSavingTask.Value++;


				if (!e.Cancelled)
				{
					if (e.Error == null)
					{
						//We got new information here. Let's see if it can be merged.
						bool canBeMerged = this.save_CheckIfConcurrencyCanBeMerged(e.Result);
						this._TaskConcurrent = e.Result;

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
				Logger.LogMessage(ex, "clientSave_Task_RetrieveByIdCompleted()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when the client is finished adding a new comment.</summary>
		/// <param name="sender">ImportExportClient</param>
		/// <param name="e">Task_CreateCommentCompletedEventArgs</param>
		private void clientSave_Task_CreateCommentCompleted(object sender, Task_CreateCommentCompletedEventArgs e)
		{
			try
			{
				const string METHOD = "clientSave_Incident_AddResolutionsCompleted()";
				Logger.LogTrace(CLASS + METHOD + " Enter: " + this._clientNumSaving.ToString() + " running.");

				ImportExportClient client = (sender as ImportExportClient);
				this._clientNumSaving--;
				this.barSavingTask.Value++;

				if (!e.Cancelled)
				{
					if (e.Error != null)
					{
						//Log message.
						Logger.LogMessage(e.Error, "Adding Comment to Incident");
						//Display error that the item saved, but adding the new resolution didn't.
						MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_Task_AddCommentErrorMessage"), StaticFuncs.getCultureResource.GetString("app_Incident_UpdateError"), MessageBoxButton.OK, MessageBoxImage.Error);
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
				Logger.LogMessage(ex, "clientSave_Task_CreateCommentCompleted()");
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
					this.lblLoadingTask.Text = StaticFuncs.getCultureResource.GetString("app_Task_Refreshing");
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
		/// <param name="moddedTask">The concurrent requirement.</param>
		private bool save_CheckIfConcurrencyCanBeMerged(RemoteTask moddedTask)
		{
			try
			{
				bool retValue = false;

				//Get current values..
				RemoteTask userTask = this.save_GetFromFields();

				if (userTask != null && moddedTask != null)
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
					while (fieldNum <= 34 && fieldCheck == true)
					{
						switch (fieldNum)
						{
							case 1:
								if (userTask.ActualEffort != this._Task.ActualEffort) fieldCheck = (this._Task.ActualEffort == moddedTask.ActualEffort);
								break;
							case 2:
								if (userTask.CreationDate != this._Task.CreationDate) fieldCheck = (this._Task.CreationDate == moddedTask.CreationDate);
								break;
							case 3:
								if (userTask.CreatorId != this._Task.CreatorId) fieldCheck = (this._Task.CreatorId == moddedTask.CreatorId);
								break;
							case 4:
								if (StaticFuncs.StripTagsCharArray(userTask.Description).ToLowerInvariant().Trim() != StaticFuncs.StripTagsCharArray(this._Task.Description).ToLowerInvariant().Trim()) fieldCheck = (StaticFuncs.StripTagsCharArray(this._Task.Description).ToLowerInvariant().Trim() == StaticFuncs.StripTagsCharArray(moddedTask.Description).ToLowerInvariant().Trim());
								break;
							case 5:
								if (userTask.EndDate != this._Task.EndDate) fieldCheck = (this._Task.EndDate == moddedTask.EndDate);
								break;
							case 6:
								if (userTask.EstimatedEffort != this._Task.EstimatedEffort) fieldCheck = (this._Task.EstimatedEffort == moddedTask.EstimatedEffort);
								break;
							case 7:
								if (userTask.List01 != this._Task.List01) fieldCheck = (this._Task.List01 == moddedTask.List01);
								break;
							case 8:
								if (userTask.List02 != this._Task.List02) fieldCheck = (this._Task.List02 == moddedTask.List02);
								break;
							case 9:
								if (userTask.List03 != this._Task.List03) fieldCheck = (this._Task.List03 == moddedTask.List03);
								break;
							case 10:
								if (userTask.List04 != this._Task.List04) fieldCheck = (this._Task.List04 == moddedTask.List04);
								break;
							case 11:
								if (userTask.List05 != this._Task.List05) fieldCheck = (this._Task.List05 == moddedTask.List05);
								break;
							case 12:
								if (userTask.List06 != this._Task.List06) fieldCheck = (this._Task.List06 == moddedTask.List06);
								break;
							case 13:
								if (userTask.List07 != this._Task.List07) fieldCheck = (this._Task.List07 == moddedTask.List07);
								break;
							case 14:
								if (userTask.List08 != this._Task.List08) fieldCheck = (this._Task.List08 == moddedTask.List08);
								break;
							case 15:
								if (userTask.List09 != this._Task.List09) fieldCheck = (this._Task.List09 == moddedTask.List09);
								break;
							case 16:
								if (userTask.List10 != this._Task.List10) fieldCheck = (this._Task.List10 == moddedTask.List10);
								break;
							case 17:
								if (userTask.Name.TrimEquals(this._Task.Name)) fieldCheck = (this._Task.Name.TrimEquals(moddedTask.Name));
								break;
							case 18:
								if (userTask.OwnerId != this._Task.OwnerId) fieldCheck = (this._Task.OwnerId == moddedTask.OwnerId);
								break;
							case 19:
								if (userTask.ReleaseId != this._Task.ReleaseId) fieldCheck = (this._Task.ReleaseId == moddedTask.ReleaseId);
								break;
							case 20:
								if (userTask.RemainingEffort != this._Task.RemainingEffort) fieldCheck = (this._Task.RemainingEffort == moddedTask.RemainingEffort);
								break;
							case 21:
								if (userTask.RequirementId != this._Task.RequirementId) fieldCheck = (this._Task.RequirementId == moddedTask.RequirementId);
								break;
							case 22:
								if (userTask.StartDate != this._Task.StartDate) fieldCheck = (this._Task.StartDate == moddedTask.StartDate);
								break;
							case 23:
								if (userTask.TaskPriorityId != this._Task.TaskPriorityId) fieldCheck = (this._Task.TaskPriorityId == moddedTask.TaskPriorityId);
								break;
							case 24:
								if (userTask.TaskStatusId != this._Task.TaskStatusId) fieldCheck = (this._Task.TaskStatusId == moddedTask.TaskStatusId);
								break;
							case 25:
								if (userTask.Text01.TrimEquals(this._Task.Text01)) fieldCheck = (this._Task.Text01.TrimEquals(moddedTask.Text01));
								break;
							case 26:
								if (userTask.Text02.TrimEquals(this._Task.Text02)) fieldCheck = (this._Task.Text02.TrimEquals(moddedTask.Text02));
								break;
							case 27:
								if (userTask.Text03.TrimEquals(this._Task.Text03)) fieldCheck = (this._Task.Text03.TrimEquals(moddedTask.Text03));
								break;
							case 28:
								if (userTask.Text04.TrimEquals(this._Task.Text04)) fieldCheck = (this._Task.Text04.TrimEquals(moddedTask.Text04));
								break;
							case 29:
								if (userTask.Text05.TrimEquals(this._Task.Text05)) fieldCheck = (this._Task.Text05.TrimEquals(moddedTask.Text05));
								break;
							case 30:
								if (userTask.Text06.TrimEquals(this._Task.Text06)) fieldCheck = (this._Task.Text06.TrimEquals(moddedTask.Text06));
								break;
							case 31:
								if (userTask.Text07.TrimEquals(this._Task.Text07)) fieldCheck = (this._Task.Text07.TrimEquals(moddedTask.Text07));
								break;
							case 32:
								if (userTask.Text08.TrimEquals(this._Task.Text08)) fieldCheck = (this._Task.Text08.TrimEquals(moddedTask.Text08));
								break;
							case 33:
								if (userTask.Text09.TrimEquals(this._Task.Text09)) fieldCheck = (this._Task.Text09.TrimEquals(moddedTask.Text09));
								break;
							case 34:
								if (userTask.Text10.TrimEquals(this._Task.Text10)) fieldCheck = (this._Task.Text10.TrimEquals(moddedTask.Text10));
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
		private RemoteTask save_GetFromFields()
		{
			try
			{
				RemoteTask retTask = null;
				try
				{
					retTask = new RemoteTask();

					//*Fixed fields..
					retTask.TaskId = this._Task.TaskId;
					retTask.ProjectId = this._Task.ProjectId;
					retTask.CreationDate = this._Task.CreationDate;
					retTask.LastUpdateDate = this._Task.LastUpdateDate;
					retTask.RequirementId = this._Task.RequirementId;

					//*Standard fields..
					retTask.Name = this.cntrlTaskName.Text.Trim();
					retTask.TaskPriorityId = ((TaskPriority)this.cntrlPriority.SelectedValue).PriorityId;
					retTask.CreatorId = ((RemoteUser)this.cntrlDetectedBy.SelectedItem).UserId;
					retTask.OwnerId = ((RemoteUser)this.cntrlOwnedBy.SelectedItem).UserId;
					retTask.ReleaseId = ((RemoteRelease)this.cntrlDetectedIn.SelectedItem).ReleaseId;
					if (this._isDescChanged)
						retTask.Description = this.cntrlDescription.HTMLText;
					else
						retTask.Description = this._Task.Description;

					//*Schedule fields..
					retTask.StartDate = this.cntrlStartDate.SelectedDate;
					retTask.EndDate = this.cntrlEndDate.SelectedDate;
					retTask.TaskStatusId = ((TaskStatus)this.cntrlStatus.SelectedValue).StatusId.Value;
					retTask.EstimatedEffort = StaticFuncs.GetMinutesFromValues(this.cntrlEstEffortH.Text, this.cntrlEstEffortM.Text);
					retTask.ActualEffort = StaticFuncs.GetMinutesFromValues(this.cntrlActEffortH.Text, this.cntrlActEffortM.Text);
					retTask.RemainingEffort = StaticFuncs.GetMinutesFromValues(this.cntrlRemEffortH.Text, this.cntrlRemEffortM.Text);

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
										retTask.Text01 = strSelectedText;
										break;
									case "TEXT_02":
										retTask.Text02 = strSelectedText;
										break;
									case "TEXT_03":
										retTask.Text03 = strSelectedText;
										break;
									case "TEXT_04":
										retTask.Text04 = strSelectedText;
										break;
									case "TEXT_05":
										retTask.Text05 = strSelectedText;
										break;
									case "TEXT_06":
										retTask.Text06 = strSelectedText;
										break;
									case "TEXT_07":
										retTask.Text07 = strSelectedText;
										break;
									case "TEXT_08":
										retTask.Text08 = strSelectedText;
										break;
									case "TEXT_09":
										retTask.Text09 = strSelectedText;
										break;
									case "TEXT_10":
										retTask.Text10 = strSelectedText;
										break;
									case "LIST_01":
										retTask.List01 = intSelectedList;
										break;
									case "LIST_02":
										retTask.List02 = intSelectedList;
										break;
									case "LIST_03":
										retTask.List03 = intSelectedList;
										break;
									case "LIST_04":
										retTask.List04 = intSelectedList;
										break;
									case "LIST_05":
										retTask.List05 = intSelectedList;
										break;
									case "LIST_06":
										retTask.List06 = intSelectedList;
										break;
									case "LIST_07":
										retTask.List07 = intSelectedList;
										break;
									case "LIST_08":
										retTask.List08 = intSelectedList;
										break;
									case "LIST_09":
										retTask.List09 = intSelectedList;
										break;
									case "LIST_10":
										retTask.List10 = intSelectedList;
										break;
								}
							}
						}
					}

				}
				catch (Exception ex)
				{
					Logger.LogMessage(ex, "Creating new Task to Update");

					retTask = null;
				}

				//Return
				return retTask;
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "save_GetFromFields()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
				return null;
			}
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
			this.lblLoadingTask.Text = StaticFuncs.getCultureResource.GetString("app_Task_Refreshing");

			this.load_LoadItem();
			}
			catch (Exception ex)
			{
				Logger.LogMessage(ex, "btnConcurrencyRefresh_Click()");
				MessageBox.Show(StaticFuncs.getCultureResource.GetString("app_General_UnexpectedError"), StaticFuncs.getCultureResource.GetString("app_General_ApplicationShortName"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>Hit when the user wants to merge their changes with the concurrent requirement.</summary>
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
					this.barSavingTask.Value--;

					//Re-launch the saving..
					RemoteTask incMerged = this.save_MergeConcurrency(this.save_GetFromFields(), this._TaskConcurrent, this._Task);

					this._clientNumSaving++;
					client.Task_UpdateAsync(incMerged, this._clientNum++);
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
		/// <param name="tskUserSaved">The user-saved requirement to merge with the Concurrent requirement.</param>
		/// <param name="tskConcurrent">The concurrent requirement to merge with the User requirement.</param>
		/// <param name="tskOriginal">The original unchanged requirement used for reference.</param>
		/// <returns>A new RemoteIncident suitable for saving.</returns>
		/// <remarks>This should only be called when it is known that there are no conflicting values between the User-Saved requirement and the Concurrent requirement.</remarks>
		private RemoteTask save_MergeConcurrency(RemoteTask tskUserSaved, RemoteTask tskConcurrent, RemoteTask tskOriginal)
		{
			//If the field was not changed by the user (tskUserSaved == tskOriginal), then use the tskConcurrent. (Assuming that the
			// tskConcurrent has a possible updated value.
			//Otherwise, use the tskUserSaved value.
			try
			{
				RemoteTask retTask = new RemoteTask();

				retTask.ActualEffort = ((tskUserSaved.ActualEffort == tskOriginal.ActualEffort) ? tskConcurrent.ActualEffort : tskUserSaved.ActualEffort);
				retTask.CreationDate = tskOriginal.CreationDate;
				retTask.CreatorId = ((tskUserSaved.CreatorId == tskOriginal.CreatorId) ? tskConcurrent.CreatorId : tskUserSaved.CreatorId);
				string strDescUser = StaticFuncs.StripTagsCharArray(tskUserSaved.Description);
				string strDescOrig = StaticFuncs.StripTagsCharArray(tskOriginal.Description);
				retTask.Description = ((strDescOrig.TrimEquals(strDescOrig)) ? tskConcurrent.Description : tskUserSaved.Description);
				retTask.EndDate = ((tskUserSaved.EndDate == tskOriginal.EndDate) ? tskConcurrent.EndDate : tskUserSaved.EndDate);
				retTask.EstimatedEffort = ((tskUserSaved.EstimatedEffort == tskOriginal.EstimatedEffort) ? tskConcurrent.EstimatedEffort : tskUserSaved.EstimatedEffort);
				retTask.LastUpdateDate = tskConcurrent.LastUpdateDate;
				retTask.List01 = ((tskUserSaved.List01 == tskOriginal.List01) ? tskConcurrent.List01 : tskUserSaved.List01);
				retTask.List02 = ((tskUserSaved.List02 == tskOriginal.List02) ? tskConcurrent.List02 : tskUserSaved.List02);
				retTask.List03 = ((tskUserSaved.List03 == tskOriginal.List03) ? tskConcurrent.List03 : tskUserSaved.List03);
				retTask.List04 = ((tskUserSaved.List04 == tskOriginal.List04) ? tskConcurrent.List04 : tskUserSaved.List04);
				retTask.List05 = ((tskUserSaved.List05 == tskOriginal.List05) ? tskConcurrent.List05 : tskUserSaved.List05);
				retTask.List06 = ((tskUserSaved.List06 == tskOriginal.List06) ? tskConcurrent.List06 : tskUserSaved.List06);
				retTask.List07 = ((tskUserSaved.List07 == tskOriginal.List07) ? tskConcurrent.List07 : tskUserSaved.List07);
				retTask.List08 = ((tskUserSaved.List08 == tskOriginal.List08) ? tskConcurrent.List08 : tskUserSaved.List08);
				retTask.List09 = ((tskUserSaved.List09 == tskOriginal.List09) ? tskConcurrent.List09 : tskUserSaved.List09);
				retTask.List10 = ((tskUserSaved.List10 == tskOriginal.List10) ? tskConcurrent.List10 : tskUserSaved.List10);
				retTask.Name = ((tskUserSaved.Name.TrimEquals(tskOriginal.Name)) ? tskConcurrent.Name : tskUserSaved.Name);
				retTask.OwnerId = ((tskUserSaved.OwnerId == tskOriginal.OwnerId) ? tskConcurrent.OwnerId : tskUserSaved.OwnerId);
				retTask.ProjectId = tskOriginal.ProjectId;
				retTask.ReleaseId = ((tskUserSaved.ReleaseId == tskOriginal.ReleaseId) ? tskConcurrent.ReleaseId : tskUserSaved.ReleaseId);
				retTask.RemainingEffort = ((tskUserSaved.RemainingEffort == tskOriginal.RemainingEffort) ? tskConcurrent.RemainingEffort : tskUserSaved.RemainingEffort);
				retTask.RequirementId = ((tskUserSaved.RequirementId == tskOriginal.RequirementId) ? tskConcurrent.RequirementId : tskUserSaved.RequirementId);
				retTask.StartDate = ((tskUserSaved.StartDate == tskOriginal.StartDate) ? tskConcurrent.StartDate : tskUserSaved.StartDate);
				retTask.TaskId = tskOriginal.TaskId;
				retTask.TaskPriorityId = ((tskUserSaved.TaskPriorityId == tskOriginal.TaskPriorityId) ? tskConcurrent.TaskPriorityId : tskUserSaved.TaskPriorityId);
				retTask.TaskStatusId = ((tskUserSaved.TaskStatusId == tskOriginal.TaskStatusId) ? tskConcurrent.TaskStatusId : tskUserSaved.TaskStatusId);
				retTask.Text01 = ((retTask.Text01.TrimEquals(tskOriginal.Text01)) ? tskConcurrent.Text01 : tskUserSaved.Text01);
				retTask.Text02 = ((retTask.Text02.TrimEquals(tskOriginal.Text02)) ? tskConcurrent.Text02 : tskUserSaved.Text02);
				retTask.Text03 = ((retTask.Text03.TrimEquals(tskOriginal.Text03)) ? tskConcurrent.Text03 : tskUserSaved.Text03);
				retTask.Text04 = ((retTask.Text04.TrimEquals(tskOriginal.Text04)) ? tskConcurrent.Text04 : tskUserSaved.Text04);
				retTask.Text05 = ((retTask.Text05.TrimEquals(tskOriginal.Text05)) ? tskConcurrent.Text05 : tskUserSaved.Text05);
				retTask.Text06 = ((retTask.Text06.TrimEquals(tskOriginal.Text06)) ? tskConcurrent.Text06 : tskUserSaved.Text06);
				retTask.Text07 = ((retTask.Text07.TrimEquals(tskOriginal.Text07)) ? tskConcurrent.Text07 : tskUserSaved.Text07);
				retTask.Text08 = ((retTask.Text08.TrimEquals(tskOriginal.Text01)) ? tskConcurrent.Text08 : tskUserSaved.Text08);
				retTask.Text09 = ((retTask.Text09.TrimEquals(tskOriginal.Text09)) ? tskConcurrent.Text09 : tskUserSaved.Text09);
				retTask.Text10 = ((retTask.Text10.TrimEquals(tskOriginal.Text10)) ? tskConcurrent.Text10 : tskUserSaved.Text10);

				//Return our new requirement.
				return retTask;
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
