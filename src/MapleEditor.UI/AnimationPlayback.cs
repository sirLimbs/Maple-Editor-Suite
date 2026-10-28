using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MapleEditor.UI;

public sealed class AnimationPlayback : IDisposable
{
	private readonly PictureBox _target;

	private readonly List<Bitmap> _frames;

	private readonly List<int> _delays;

	private readonly Timer _timer;

	private int _index;

	public event EventHandler FrameChanged;

	public bool IsPlaying => _timer.Enabled;

	public int FrameIndex => _index;

	public int FrameCount => _frames.Count;

	public AnimationPlayback(PictureBox target, IReadOnlyList<Bitmap> frames, IReadOnlyList<int> delays)
	{
		_target = target ?? throw new ArgumentNullException(nameof(target));
		_frames = new List<Bitmap>(frames ?? throw new ArgumentNullException(nameof(frames)));
		_delays = new List<int>(delays ?? new List<int>());
		_timer = new Timer();
		_timer.Tick += (object? _, EventArgs _) =>
		{
			Advance();
		};
	}

	public void Play()
	{
		if (_frames.Count == 0)
		{
			return;
		}
		_index = 0;
		ShowFrame();
		_timer.Interval = GetDelay(_index);
		_timer.Start();
	}

	public void Pause()
	{
		_timer.Stop();
	}

	public void Toggle()
	{
		if (_timer.Enabled)
		{
			Pause();
		}
		else
		{
			Play();
		}
	}

	public void Dispose()
	{
		_timer.Stop();
		_timer.Dispose();
		_target.Image = null;
		_target.Invalidate();
	}

	private void Advance()
	{
		if (_frames.Count == 0)
		{
			return;
		}
		_index = (_index + 1) % _frames.Count;
		ShowFrame();
		_timer.Interval = GetDelay(_index);
	}

	private void ShowFrame()
	{
		_target.Image = _frames[_index];
		_target.Invalidate();
		this.FrameChanged?.Invoke(this, EventArgs.Empty);
	}

	private int GetDelay(int index)
	{
		int delay = ((index >= 0 && index < _delays.Count) ? _delays[index] : 100);
		return Math.Max(20, delay);
	}
}
