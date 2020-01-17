﻿using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SoundMap.Settings;
using SoundMap.Windows;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;

namespace SoundMap
{
	public class MainWindowModel: Observable
	{
		private RelayCommand FOpenProjectCommand = null;
		private RelayCommand FSaveProjectCommand = null;
		private RelayCommand FSaveProjectAsCommand = null;

		private RelayCommand FExitCommand = null;
		private RelayCommand FNewProjectCommand = null;

		private RelayCommand FIsPauseCommand = null;
		private RelayCommand FSetNewPointKindCommand = null;
		private RelayCommand FSaveSampleCommand = null;
		private RelayCommand FPreferencesCommand = null;
		private RelayCommand FProjectPropertiesCommand = null;

		private SoundProject FProject = new SoundProject();
		private bool FIsPause = false;
		private IWavePlayer FOutput = null;
		private readonly MainWindow FMainWindow;
		private readonly DispatcherTimer FStatusTimer;

		public MainWindowModel(MainWindow AMainWindow)
		{
			FMainWindow = AMainWindow;

			try
			{
				if (App.Args.Length == 1)
					Project = SoundProject.CreateFromFile(App.Args[0]);
			}
			catch (Exception ex)
			{
				App.ShowError(ex.Message);
			}

			FStatusTimer = new DispatcherTimer();
			FStatusTimer.Interval = TimeSpan.FromMilliseconds(100);
			FStatusTimer.Tick += StatusTimer_Tick;
			
		}

		public void WindowLoaded()
		{
			StartPlay();
			FStatusTimer.Start();
		}

		public SoundProject Project
		{
			get => FProject;
			set
			{
				if (!IsPause)
					StopPlay();

				Interlocked.Exchange(ref FProject, value);

				if (!IsPause)
					StartPlay();

				NotifyPropertyChanged(nameof(Project));
			}
		}

		private void StartPlay()
		{
			if (FOutput != null)
				return;

			try
			{
				FOutput = App.Settings.Preferences.CreateOutput();
				if ((FProject != null) && (FOutput != null))
				{
					FProject.NotePanic();
					FProject.ConfigureGenerator(App.Settings.Preferences.SampleRate, App.Settings.Preferences.Channels);
					FOutput.Init(FProject);
					//var sg = new SignalGenerator(App.Settings.Preferences.SampleRate, App.Settings.Preferences.Channels);
					//sg.Frequency = 220;
					//sg.Type = SignalGeneratorType.Sin;
					//FOutput.Init(sg);
					FOutput.Play();
				}
			}
			catch (Exception ex)
			{
				FOutput = null;
				App.ShowError(ex.Message);
			}
		}

		private void StopPlay()
		{
			if (FOutput != null)
			{
				FProject?.NotePanic();
				FOutput.Stop();
				FOutput.Dispose();
				FOutput = null;
			}
		}

		public bool IsPause
		{
			get => FIsPause;
			set
			{
				if (FIsPause != value)
				{
					FIsPause = value;
					if (FIsPause)
						StopPlay();
					else
						StartPlay();
					NotifyPropertyChanged(nameof(IsPause));
				}
			}
		}

		public void WindowClose()
		{
			StopPlay();
		}

		public RelayCommand ExitCommand
		{
			get
			{
				if (FExitCommand == null)
					FExitCommand = new RelayCommand((obj) => System.Windows.Application.Current.MainWindow.Close());
				return FExitCommand;
			}
		}

		public RelayCommand OpenProjectCommand
		{
			get
			{
				if (FOpenProjectCommand == null)
					FOpenProjectCommand = new RelayCommand((obj) =>
					{
						try
						{
							OpenFileDialog dlg = new OpenFileDialog();
							dlg.Filter = SoundProject.FileFilter;
							if (dlg.ShowDialog() == DialogResult.OK)
								Project = SoundProject.CreateFromFile(dlg.FileName);
						}
						catch (Exception ex)
						{
							App.ShowError(ex.Message);
						}
					});
				return FOpenProjectCommand;
			}
		}

		public RelayCommand SaveProjectCommand
		{
			get
			{
				if (FSaveProjectCommand == null)
					FSaveProjectCommand = new RelayCommand((obj) =>
					{
						if (string.IsNullOrEmpty(Project.FileName))
							SaveProjectAsCommand.Execute(null);
						else
							Project.SaveToFile(Project.FileName);
					});
				return FSaveProjectCommand;
			}
		}

		public RelayCommand SaveProjectAsCommand
		{
			get
			{
				if (FSaveProjectAsCommand == null)
					FSaveProjectAsCommand = new RelayCommand((obj) =>
					{
						using (SaveFileDialog dlg = new SaveFileDialog())
						{
							if (!string.IsNullOrEmpty(Project.FileName))
							{
								dlg.InitialDirectory = Path.GetFullPath(Project.FileName);
								dlg.FileName = Path.GetFileName(Project.FileName);
							}
							dlg.Filter = SoundProject.FileFilter;
							dlg.FilterIndex = 1;
							if (dlg.ShowDialog() == DialogResult.OK)
								Project.SaveToFile(dlg.FileName);
						}
					});
				return FSaveProjectAsCommand;
			}
		}

		public RelayCommand NewProjectCommand
		{
			get
			{
				if (FNewProjectCommand == null)
					FNewProjectCommand = new RelayCommand((obj) =>
					{
						Project = new SoundProject();
					});
				return FNewProjectCommand;
			}
		}

		public RelayCommand IsPauseCommand
		{
			get
			{
				if (FIsPauseCommand == null)
					FIsPauseCommand = new RelayCommand((obj) => IsPause = !IsPause);
				return FIsPauseCommand;
			}
		}

		public RelayCommand SetNewPointKindCommand
		{
			get
			{
				if (FSetNewPointKindCommand == null)
					FSetNewPointKindCommand = new RelayCommand((obj) =>
					{
						//Project.NewPointKind = (PointKind)obj;
					});
				return FSetNewPointKindCommand;
			}
		}

		public RelayCommand SaveSampleCommand
		{
			get
			{
				if (FSaveSampleCommand == null)
					FSaveSampleCommand = new RelayCommand((obj) =>
					{
						using (SaveFileDialog dlg = new SaveFileDialog())
						{
							if (!string.IsNullOrEmpty(Project.FileName))
							{
								dlg.InitialDirectory = Path.GetFullPath(Project.FileName);
								dlg.FileName = Path.GetFileName(Project.FileName);
							}
							dlg.Filter = "Wave file (*.wav)|*.wav";
							dlg.FilterIndex = 1;
							if (dlg.ShowDialog() == DialogResult.OK)
							{
								if (!IsPause)
									StopPlay();

								Project.SaveSampleToFile(dlg.FileName);

								if (!IsPause)
									StartPlay();
							}
						}
					});
				return FSaveSampleCommand;
			}
		}

		public RelayCommand PreferencesCommand
		{
			get
			{
				if (FPreferencesCommand == null)
					FPreferencesCommand = new RelayCommand((param) =>
					{
						if (!IsPause)
							StopPlay();

						PreferencesWindow wnd = new PreferencesWindow();
						wnd.Owner = FMainWindow;
						wnd.DataContext = App.Settings.Preferences.Clone();
						if (wnd.ShowDialog() == true)
							App.Settings.Preferences = (PreferencesSettings)wnd.DataContext;

						if (!IsPause)
							StartPlay();
					});
				return FPreferencesCommand;
			}
		}

		public RelayCommand ProjectPropertiesCommand
		{
			get
			{
				if (FProjectPropertiesCommand == null)
					FProjectPropertiesCommand = new RelayCommand((param) =>
					{
						ProjectSettingsWindow wnd = new ProjectSettingsWindow();
						wnd.Owner = FMainWindow;
						wnd.DataContext = FProject.Settings.Clone();
						if (wnd.ShowDialog() == true)
							FProject.Settings = (ProjectSettings)wnd.DataContext;
					});
				return FProjectPropertiesCommand;
			}
		}

		public void KeyDown(Key AKey)
		{
			switch (AKey)
			{
				case Key.F1:
					Project.DebugMode = true;
					break;

				case Key.Z:
					Project.AddNoteByHalftone(AKey, -7);
					break;
				case Key.X:
					Project.AddNoteByHalftone(AKey, -5);
					break;
				case Key.C:
					Project.AddNoteByHalftone(AKey, -3);
					break;
				case Key.V:
					Project.AddNoteByHalftone(AKey, -2);
					break;

				case Key.B:
					Project.AddNoteByHalftone(AKey, 0);
					break;

				case Key.N:
					Project.AddNoteByHalftone(AKey, 2);
					break;
				case Key.M:
					Project.AddNoteByHalftone(AKey, 3);
					break;
				case Key.OemComma:
					Project.AddNoteByHalftone(AKey, 5);
					break;
				case Key.OemPeriod:
					Project.AddNoteByHalftone(AKey, 7);
					break;
				case Key.OemQuestion:
					Project.AddNoteByHalftone(AKey, 9);
					break;
				default:
					Debug.WriteLine(AKey);
					break;
			}
		}

		public void KeyUp(Key AKey)
		{
			switch (AKey)
			{
				case Key.F1:
					Project.DebugMode = false;
					break;
			}

			Project.DeleteNote(AKey);
		}

		public string Status
		{
			get
			{
				if (FOutput != null)
					return $"Playing; {FProject.WaveFormat.SampleRate}, {FProject.WaveFormat.Channels}; {FProject.Status}";
				return "Stopped;";
			}
		}

		private void StatusTimer_Tick(object sender, EventArgs e)
		{
			NotifyPropertyChanged(nameof(Status));
		}
	}
}
