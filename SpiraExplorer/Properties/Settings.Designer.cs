﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Inflectra.SpiraTest.IDEIntegration.VisualStudio2010.Properties
{


	[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
	[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "10.0.0.0")]
	internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase
	{

		private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));

		public static Settings Default
		{
			get
			{
				return defaultInstance;
			}
		}

		[global::System.Configuration.UserScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		public global::Inflectra.SpiraTest.IDEIntegration.VisualStudio2010.Business.SerializableDictionary<string, string> AssignedProjects
		{
			get
			{
				return ((global::Inflectra.SpiraTest.IDEIntegration.VisualStudio2010.Business.SerializableDictionary<string, string>)(this["AssignedProjects"]));
			}
			set
			{
				this["AssignedProjects"] = value;
			}
		}

		[global::System.Configuration.UserScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		public global::Inflectra.SpiraTest.IDEIntegration.VisualStudio2010.Business.SerializableList<string> AllProjects
		{
			get
			{
				return ((global::Inflectra.SpiraTest.IDEIntegration.VisualStudio2010.Business.SerializableList<string>)(this["AllProjects"]));
			}
			set
			{
				this["AllProjects"] = value;
			}
		}

		[global::System.Configuration.UserScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("True")]
		public bool ShowUnassigned
		{
			get
			{
				return ((bool)(this["ShowUnassigned"]));
			}
			set
			{
				this["ShowUnassigned"] = value;
			}
		}

		[global::System.Configuration.UserScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("False")]
		public bool ShowCompleted
		{
			get
			{
				return ((bool)(this["ShowCompleted"]));
			}
			set
			{
				this["ShowCompleted"] = value;
			}
		}
	}
}
