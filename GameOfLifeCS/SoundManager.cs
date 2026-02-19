using System;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace GameOfLifeCS
{
    public class SoundManager : IDisposable
    {
        private bool soundEnabled = true;
        private float volume = 0.5f;
        private WaveOutEvent? outputDevice;
        private MixingSampleProvider? mixer;
        private CancellationTokenSource? musicCancellation;
        private readonly Random random = new();

        // Musical note frequencies (Equal temperament)
        private static readonly Dictionary<string, float> Notes = new()
        {
            {"C3", 130.81f}, {"D3", 146.83f}, {"E3", 164.81f}, {"F3", 174.61f}, {"G3", 196.00f}, {"A3", 220.00f}, {"B3", 246.94f},
            {"C4", 261.63f}, {"D4", 293.66f}, {"E4", 329.63f}, {"F4", 349.23f}, {"G4", 392.00f}, {"A4", 440.00f}, {"B4", 493.88f},
            {"C5", 523.25f}, {"D5", 587.33f}, {"E5", 659.25f}, {"F5", 698.46f}, {"G5", 783.99f}, {"A5", 880.00f}, {"B5", 987.77f},
            {"C6", 1046.50f}, {"D6", 1174.66f}, {"E6", 1318.51f}
        };

        // 8-bit style melody (30 seconds worth of notes)
        private static readonly (string note, float duration)[] Chiptunemelody = new[]
        {
            // Main theme - energetic 8-bit melody
            ("E5", 0.25f), ("E5", 0.25f), ("E5", 0.25f), ("C5", 0.125f), ("E5", 0.375f),
            ("G5", 0.5f), ("G4", 0.5f),
            
            ("C5", 0.375f), ("G4", 0.25f), ("E4", 0.375f),
            ("A4", 0.25f), ("B4", 0.25f), ("A4", 0.25f), ("G4", 0.333f),
            
            ("E5", 0.333f), ("G5", 0.333f), ("A5", 0.25f),
            ("F5", 0.25f), ("G5", 0.25f), ("E5", 0.25f), ("C5", 0.25f), ("D5", 0.25f), ("B4", 0.25f),
            
            // Variation
            ("C5", 0.375f), ("G4", 0.25f), ("E4", 0.375f),
            ("A4", 0.25f), ("B4", 0.25f), ("A4", 0.25f), ("G4", 0.333f),
            
            ("E5", 0.333f), ("G5", 0.333f), ("A5", 0.25f),
            ("F5", 0.25f), ("G5", 0.25f), ("E5", 0.25f), ("C5", 0.25f), ("D5", 0.25f), ("B4", 0.25f),
            
            // Bridge
            ("G5", 0.25f), ("F5", 0.125f), ("E5", 0.125f), ("D5", 0.25f),
            ("E5", 0.25f), ("G4", 0.25f), ("A4", 0.25f), ("C5", 0.25f),
            ("A4", 0.25f), ("C5", 0.25f), ("D5", 0.25f),
            
            // Repeat main theme
            ("E5", 0.25f), ("E5", 0.25f), ("E5", 0.25f), ("C5", 0.125f), ("E5", 0.375f),
            ("G5", 0.5f), ("G4", 0.5f),
            
            ("C5", 0.375f), ("G4", 0.25f), ("E4", 0.5f),
            ("A4", 0.25f), ("B4", 0.25f), ("A4", 0.25f), ("G4", 0.5f),
        };

        // 8-bit bass line (simpler, lower notes)
        private static readonly (string note, float duration)[] ChiptuneBass = new[]
        {
            ("C3", 0.5f), ("C3", 0.5f), ("C3", 0.5f), ("C3", 0.5f),
            ("G3", 0.5f), ("G3", 0.5f), ("E3", 0.5f), ("E3", 0.5f),
            ("A3", 0.5f), ("A3", 0.5f), ("F3", 0.5f), ("F3", 0.5f),
            ("G3", 0.5f), ("G3", 0.5f), ("C3", 0.5f), ("C3", 0.5f),
            
            ("C3", 0.5f), ("C3", 0.5f), ("G3", 0.5f), ("G3", 0.5f),
            ("A3", 0.5f), ("A3", 0.5f), ("F3", 0.5f), ("F3", 0.5f),
            ("C3", 0.5f), ("C3", 0.5f), ("G3", 0.5f), ("G3", 0.5f),
            ("C3", 0.5f), ("C3", 0.5f), ("C3", 0.5f), ("C3", 0.5f),
        };

        public SoundManager()
        {
            InitializeAudio();
        }

        public bool SoundEnabled
        {
            get => soundEnabled;
            set
            {
                soundEnabled = value;
                if (!value)
                {
                    StopBackgroundMusic();
                }
                else if (value)
                {
                    PlayBackgroundMusic();
                }
            }
        }

        public float Volume
        {
            get => volume;
            set
            {
                volume = Math.Clamp(value, 0f, 1f);
                if (outputDevice != null)
                {
                    outputDevice.Volume = volume;
                }
            }
        }

        private void InitializeAudio()
        {
            try
            {
                outputDevice = new WaveOutEvent();
                mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
                {
                    ReadFully = true
                };
                outputDevice.Init(mixer);
                outputDevice.Volume = volume;
                outputDevice.Play();
            }
            catch
            {
                // Audio initialization failed
                soundEnabled = false;
            }
        }

        public void PlayBackgroundMusic()
        {
            if (!soundEnabled || mixer == null) return;

            StopBackgroundMusic();
            musicCancellation = new CancellationTokenSource();

            Task.Run(() => Play8BitMusicLoop(musicCancellation.Token));
        }

        public void StopBackgroundMusic()
        {
            musicCancellation?.Cancel();
            musicCancellation?.Dispose();
            musicCancellation = null;
        }

        public void PlayCellBirth()
        {
            if (!soundEnabled) return;

            var birthNotes = new[] { "C5", "E5", "G5" };
            Play8BitNote(birthNotes[random.Next(birthNotes.Length)], 0.05f, 0.08f);
        }

        public void PlayCellDeath()
        {
            if (!soundEnabled) return;

            var deathNotes = new[] { "G3", "E3", "C3" };
            Play8BitNote(deathNotes[random.Next(deathNotes.Length)], 0.05f, 0.08f);
        }

        public void PlayButtonClick()
        {
            if (!soundEnabled) return;
            Play8BitNote("C5", 0.1f, 0.1f);
        }

        public void PlayPatternLoad()
        {
            if (!soundEnabled) return;

            Task.Run(() =>
            {
                Play8BitNote("C5", 0.1f, 0.15f);
                Thread.Sleep(80);
                Play8BitNote("E5", 0.1f, 0.15f);
                Thread.Sleep(80);
                Play8BitNote("G5", 0.1f, 0.15f);
                Thread.Sleep(80);
                Play8BitNote("C6", 0.15f, 0.2f);
            });
        }

        public void PlayGameStart()
        {
            if (!soundEnabled) return;

            Task.Run(() =>
            {
                Play8BitChord(new[] { "C4", "E4", "G4" }, 0.2f);
                Thread.Sleep(150);
                Play8BitChord(new[] { "C5", "E5", "G5" }, 0.3f);
            });
        }

        public void PlayGameStop()
        {
            if (!soundEnabled) return;

            Task.Run(() =>
            {
                Play8BitChord(new[] { "G5", "E5", "C5" }, 0.2f);
                Thread.Sleep(150);
                Play8BitChord(new[] { "G4", "E4", "C4" }, 0.3f);
            });
        }

        public void PlayClear()
        {
            if (!soundEnabled) return;

            Task.Run(() =>
            {
                string[] scale = { "C6", "B5", "A5", "G5", "F5", "E5", "D5", "C5" };
                foreach (var note in scale)
                {
                    Play8BitNote(note, 0.06f, 0.1f);
                    Thread.Sleep(30);
                }
            });
        }

        private async void Play8BitMusicLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && soundEnabled)
                {
                    // Play melody and bass simultaneously
                    var melodyTask = PlayMelodyTrack(Chiptunemelody, token);
                    var bassTask = PlayBassTrack(ChiptuneBass, token);

                    await Task.WhenAll(melodyTask, bassTask);

                    // Small pause before loop repeats
                    if (!token.IsCancellationRequested)
                    {
                        await Task.Delay(500, token);
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task PlayMelodyTrack((string note, float duration)[] melody, CancellationToken token)
        {
            foreach (var (note, duration) in melody)
            {
                if (token.IsCancellationRequested) break;

                Play8BitNote(note, duration, 0.12f);
                await Task.Delay((int)(duration * 1000), token);
            }
        }

        private async Task PlayBassTrack((string note, float duration)[] bass, CancellationToken token)
        {
            foreach (var (note, duration) in bass)
            {
                if (token.IsCancellationRequested) break;

                Play8BitNote(note, duration, 0.08f, SignalGeneratorType.Square); // Square wave for bass
                await Task.Delay((int)(duration * 1000), token);
            }
        }

        private void Play8BitNote(string noteName, float duration, float noteVolume = 0.15f, SignalGeneratorType waveType = SignalGeneratorType.Square)
        {
            if (!Notes.TryGetValue(noteName, out float frequency)) return;

            var note = Create8BitTone(frequency, duration, noteVolume, waveType);
            mixer?.AddMixerInput(note);
        }

        private void Play8BitChord(string[] noteNames, float duration, float chordVolume = 0.12f)
        {
            foreach (var noteName in noteNames)
            {
                if (Notes.TryGetValue(noteName, out float frequency))
                {
                    var note = Create8BitTone(frequency, duration, chordVolume / noteNames.Length, SignalGeneratorType.Square);
                    mixer?.AddMixerInput(note);
                }
            }
        }

        private ISampleProvider Create8BitTone(float frequency, float duration, float amplitude = 0.15f, SignalGeneratorType waveType = SignalGeneratorType.Square)
        {
            var signal = new SignalGenerator(44100, 2)
            {
                Gain = amplitude * volume,
                Frequency = frequency,
                Type = waveType // Square wave for authentic 8-bit sound
            };

            // Simple envelope for 8-bit style (quick attack, sustain, quick release)
            var envelope = new EnvelopeGenerator
            {
                AttackSeconds = 0.001f,
                DecaySeconds = 0.01f,
                SustainLevel = 0.8f,
                ReleaseSeconds = 0.05f
            };

            return signal
                .Take(TimeSpan.FromSeconds(duration))
                .ApplyEnvelope(envelope);
        }

        public void Dispose()
        {
            soundEnabled = false;
            StopBackgroundMusic();
            outputDevice?.Stop();
            outputDevice?.Dispose();
        }
    }

    // Helper extension for envelope
    public static class SampleProviderExtensions
    {
        public static ISampleProvider ApplyEnvelope(this ISampleProvider source, EnvelopeGenerator envelope)
        {
            return new EnvelopedSampleProvider(source, envelope);
        }
    }

    public class EnvelopedSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly EnvelopeGenerator envelope;
        private int sampleCount = 0;

        public EnvelopedSampleProvider(ISampleProvider source, EnvelopeGenerator envelope)
        {
            this.source = source;
            this.envelope = envelope;
        }

        public WaveFormat WaveFormat => source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            int read = source.Read(buffer, offset, count);
            
            for (int i = 0; i < read; i++)
            {
                float envelopeValue = envelope.GetAmplitude(sampleCount / (float)WaveFormat.SampleRate);
                buffer[offset + i] *= envelopeValue;
                sampleCount++;
            }

            return read;
        }
    }

    public class EnvelopeGenerator
    {
        public float AttackSeconds { get; set; } = 0.01f;
        public float DecaySeconds { get; set; } = 0.05f;
        public float SustainLevel { get; set; } = 0.7f;
        public float ReleaseSeconds { get; set; } = 0.1f;

        public float GetAmplitude(float timeSeconds)
        {
            if (timeSeconds < AttackSeconds)
            {
                return timeSeconds / AttackSeconds;
            }
            else if (timeSeconds < AttackSeconds + DecaySeconds)
            {
                float decayTime = timeSeconds - AttackSeconds;
                return 1.0f - (1.0f - SustainLevel) * (decayTime / DecaySeconds);
            }
            else
            {
                return SustainLevel * Math.Max(0, 1.0f - (timeSeconds - AttackSeconds - DecaySeconds) / ReleaseSeconds);
            }
        }
    }
}
