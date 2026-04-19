using NAudio.Wave;

namespace CaptchaAlarm.Services
{
    /// <summary>
    /// Plays alarm sounds using NAudio.
    /// </summary>
    public class AlarmService : IDisposable
    {
        private IWavePlayer? _wavePlayer;
        private AudioFileReader? _audioReader;
        private string? _currentSoundPath;
        private int _loopsRemaining;
        private bool _disposed;

        /// <summary>Raised when playback finishes all loops.</summary>
        public event EventHandler? PlaybackFinished;

        /// <summary>
        /// Plays the specified sound file <paramref name="loopCount"/> times.
        /// If the file is not found, falls back to the system beep.
        /// </summary>
        public void Play(string soundPath, bool loop, int loopCount)
        {
            Stop(); // Stop any previous playback

            if (!File.Exists(soundPath))
            {
                AppLogger.Log($"[Alarm] Sound file not found: {soundPath} – using system beep.");
                SystemSounds.Beep(loopCount);
                return;
            }

            try
            {
                _currentSoundPath = soundPath;
                _loopsRemaining = loop ? Math.Max(1, loopCount) : 1;

                StartPlayback();
            }
            catch (Exception ex)
            {
                AppLogger.Log($"[Alarm] Playback error: {ex.Message}");
                SystemSounds.Beep(1);
            }
        }

        /// <summary>Stops playback immediately.</summary>
        public void Stop()
        {
            try
            {
                _loopsRemaining = 0;
                _wavePlayer?.Stop();
                _wavePlayer?.Dispose();
                _audioReader?.Dispose();
                _wavePlayer = null;
                _audioReader = null;
            }
            catch (Exception ex)
            {
                AppLogger.Log($"[Alarm] Stop error: {ex.Message}");
            }
        }

        private void StartPlayback()
        {
            if (_currentSoundPath == null || _loopsRemaining <= 0) return;

            _audioReader = new AudioFileReader(_currentSoundPath);
            _wavePlayer = new WaveOutEvent();
            _wavePlayer.Init(_audioReader);
            _wavePlayer.PlaybackStopped += OnPlaybackStopped;
            _wavePlayer.Play();
        }

        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            _loopsRemaining--;
            if (_loopsRemaining > 0 && _currentSoundPath != null)
            {
                // Restart for next loop
                _audioReader?.Dispose();
                _wavePlayer?.Dispose();
                _audioReader = null;
                _wavePlayer = null;
                StartPlayback();
            }
            else
            {
                PlaybackFinished?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Tests a sound by playing it once.</summary>
        public void TestSound(string soundPath)
        {
            Play(soundPath, false, 1);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
            GC.SuppressFinalize(this);
        }

        /// <summary>Simple fallback using Console.Beep.</summary>
        private static class SystemSounds
        {
            public static void Beep(int count)
            {
                Task.Run(() =>
                {
                    for (int i = 0; i < count; i++)
                    {
                        try { Console.Beep(880, 400); }
                        catch { /* Ignore on headless systems */ }
                        if (i < count - 1)
                            Thread.Sleep(200);
                    }
                });
            }
        }
    }
}
