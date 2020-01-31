﻿using NAudio.CoreAudioApi;
using NAudio.Wave;
using SoundMap.NoteWaveProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SoundMap.Settings
{
	[Serializable]
	public class PreferencesSettings: Observable, ICloneable
	{
		private static AudioOutput[] FAudioOutputs = null;
		private static AudioOutput FDefaultAudioOutput = null;
		private static readonly Dictionary<string, Type> FNoteProviders = new Dictionary<string, Type>();
		private MidiSettings FMidi = null;
		private OpenCLSettings FOpenCL = null;
		private string FNoteProviderName = null;

		[XmlIgnore]
		public AudioOutput[] AudioOutputs => FAudioOutputs;

		[XmlIgnore]
		public AudioOutput DefaultAudioOutput => FDefaultAudioOutput;

		public PreferencesSettings()
		{
			if (FAudioOutputs == null)
				InitAudio();
		}

		private void InitAudio()
		{
			List<AudioOutput> aos = new List<AudioOutput>();

			FDefaultAudioOutput = null;
			using (var e = new MMDeviceEnumerator())
			{
				string defaultId = e.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID;

				foreach (MMDevice d in e.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
				{
					var ao = new AudioOutput(d);
					if (d.ID == defaultId)
						FDefaultAudioOutput = ao;
					aos.Add(ao);
				}
			}

			if (AsioOut.isSupported())
				foreach (var n in AsioOut.GetDriverNames())
					aos.Add(new AudioOutput(n));

			FAudioOutputs = aos.ToArray();
			if (FDefaultAudioOutput == null)
				FDefaultAudioOutput = FAudioOutputs.First();
		}

		public string SelectedAudioOutputName { get; set; }
		private int? FLatency = null;
		private int? FSampleRate = null;
		private string FChannel = null;

		[XmlIgnore]
		public AudioOutput SelectedAudioOutput
		{
			get
			{
				var r = AudioOutputs.FirstOrDefault(ao => ao.Name == SelectedAudioOutputName);
				if (r == null)
					return DefaultAudioOutput;
				return r;
			}
			set
			{
				if (value == null)
					SelectedAudioOutputName = null;
				else
					SelectedAudioOutputName = value.Name;
				NotifyPropertyChanged(nameof(SampleRate));
				NotifyPropertyChanged(nameof(Channel));
				NotifyPropertyChanged(nameof(Latency));
				NotifyPropertyChanged(nameof(LatencyEnabled));
				NotifyPropertyChanged(nameof(SelectedAudioOutput));
			}
		}

		public IWavePlayer CreateOutput() => SelectedAudioOutput.CreateOutput(this);

		public int Latency
		{
			get
			{
				if (FLatency.HasValue)
					return FLatency.Value;
				return SelectedAudioOutput.DefaultLatency;
			}
			set
			{
				if (value == 0)
					FLatency = SelectedAudioOutput.DefaultLatency;
				else
					FLatency = value;
			}
		}

		public bool LatencyEnabled
		{
			get => SelectedAudioOutput.LatencyEnbled;
		}

		public int Channels => 2;

		public int SampleRate
		{
			get
			{
				var sao = SelectedAudioOutput;
				if (FSampleRate.HasValue)
				{ 
					if (sao.SampleRates.Contains(FSampleRate.Value))
						return FSampleRate.Value;
				}
				return sao.SampleRates.First();
			}
			set => FSampleRate = value;
		}

		public string Channel
		{
			get
			{
				var ao = SelectedAudioOutput;
				if (ao.Channels.Contains(FChannel))
					return FChannel;
				return ao.Channels.First();
			}
			set => FChannel = value;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public PreferencesSettings Clone()
		{
			var r = new PreferencesSettings();
			r.SelectedAudioOutputName = this.SelectedAudioOutputName;
			r.FLatency = this.FLatency;
			r.FSampleRate = this.FSampleRate;
			r.FChannel = this.FChannel;
			r.Midi = this.Midi.Clone();
			r.OpenCL = this.OpenCL.Clone();
			r.NoteProviderName = this.NoteProviderName;
			return r;
		}

		public MidiSettings Midi
		{
			get
			{
				if (FMidi == null)
					FMidi = new MidiSettings();
				return FMidi;
			}
			set => FMidi = value;
		}

		public OpenCLSettings OpenCL
		{
			get
			{
				if (FOpenCL == null)
					FOpenCL = new OpenCLSettings();
				return FOpenCL;
			}
			set => FOpenCL = value;
		}

		public string NoteProviderName
		{
			get
			{
				if (FNoteProviderName == null)
					FNoteProviderName = NoteProviderNames.First();
				return FNoteProviderName;
			}
			set => FNoteProviderName = value;
		}

		private static void CheckNoteProviders()
		{
			if (FNoteProviders.Count == 0)
			{
				var a = typeof(NoteWaveProvider).Assembly.GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(NoteWaveProvider)));
				foreach (var t in a)
					FNoteProviders.Add(t.Name, t);
			}
		}

		public static string[] NoteProviderNames
		{
			get
			{
				CheckNoteProviders();
				return FNoteProviders.Keys.ToArray();
			}
		}

		public NoteWaveProvider CreateNoteProvider()
		{
			CheckNoteProviders();
			var t = FNoteProviders[NoteProviderName];
			return (NoteWaveProvider)Activator.CreateInstance(t);
		}
	}
}
