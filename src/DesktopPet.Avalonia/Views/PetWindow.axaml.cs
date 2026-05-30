using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Input;
using global::Avalonia.Media.Imaging;
using global::Avalonia.Threading;
using DesktopPet.Avalonia.Models;
using DesktopPet.Avalonia.Services;
using System;
using System.Collections.Generic;
using AvaloniaScaleTransform = global::Avalonia.Media.ScaleTransform;
using AvaloniaRelativePoint = global::Avalonia.RelativePoint;
using AvaloniaRelativeUnit = global::Avalonia.RelativeUnit;
using AvaloniaPoint = global::Avalonia.Point;
using AvaloniaPixelPoint = global::Avalonia.PixelPoint;
using AvaloniaPixelRect = global::Avalonia.PixelRect;

namespace DesktopPet.Avalonia.Views;

public partial class PetWindow : Window
{
    private int _animationStep;
    private TAnimation _currentAnimation;
    private bool _isDragging = false;
    private bool _isMovingLeft = true;
    private Point _dragStartPoint;
    private PixelPoint _windowStartPosition;
    
    private double _positionX = 0.0;
    private double _positionY = 0.0;
    private double _offsetY = 0.0;
    
    private readonly Animations _animations;
    private readonly XmlParser _xml;
    private readonly DispatcherTimer _timer;
    private readonly List<Bitmap> _images = new List<Bitmap>();
    
    private int _displayIndex = 0;
    private PixelRect _screenBounds;

    public PetWindow()
    {
        InitializeComponent();
        _timer = new DispatcherTimer();
        _timer.Tick += Timer_Tick;
    }

    public PetWindow(Animations animations, XmlParser xml) : this()
    {
        _animations = animations;
        _xml = xml;
        
        // Get primary screen bounds
        UpdateScreenBounds();
    }

    private void UpdateScreenBounds()
    {
        var screens = Screens;
        if (screens != null && screens.All.Count > _displayIndex)
        {
            _screenBounds = screens.All[_displayIndex].Bounds;
        }
        else if (screens != null && screens.Primary != null)
        {
            _screenBounds = screens.Primary.Bounds;
        }
        else
        {
            _screenBounds = new PixelRect(0, 0, 1920, 1080);
        }
    }

    public void Initialize(int width, int height)
    {
        Width = width;
        Height = height;
        _animationStep = 0;
    }

    public void AddImage(Bitmap image)
    {
        _images.Add(image);
    }

    public void Play(bool first = true, int forceSpawn = -1)
    {
        _timer.Stop();
        _animationStep = 0;
        
        UpdateScreenBounds();
        
        var spawn = forceSpawn < 0 
            ? _animations.GetRandomSpawn() 
            : _animations.GetSpawnByIndex(forceSpawn);
            
        _positionY = _screenBounds.Y + spawn.Start.Y.GetValue(_displayIndex, _xml);
        _positionX = _screenBounds.X + spawn.Start.X.GetValue(_displayIndex, _xml);
        
        if (!_isMovingLeft)
        {
            _positionX = _screenBounds.X - (spawn.Start.X.GetValue(_displayIndex, _xml) - _screenBounds.Width) - Width;
        }
        
        Position = new PixelPoint((int)_positionX, (int)_positionY);
        _offsetY = 0.0;
        
        SetNewAnimation(spawn.Next);
        
        Show();
        Opacity = 1.0;
        Topmost = true;
        _timer.Start();
    }

    public void SetNewAnimation(int animationId)
    {
        if (!_animations.SheepAnimations.ContainsKey(animationId))
        {
            StartUp.AddDebugInfo(StartUp.DebugType.Error, $"Animation {animationId} not found");
            return;
        }
        
        _currentAnimation = _animations.SheepAnimations[animationId];
        _animationStep = 0;
        
        if (_currentAnimation.Sequence.Interval > 0)
        {
            _timer.Interval = TimeSpan.FromMilliseconds(_currentAnimation.Sequence.Interval);
        }
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        if (_currentAnimation.Sequence == null || _currentAnimation.Sequence.Frames == null)
            return;
            
        var totalSteps = _currentAnimation.Sequence.TotalSteps;
        if (_animationStep >= totalSteps)
        {
            // Animation finished, get next
            var nextAnim = _animations.GetNextAnimation(_currentAnimation.Id, false, false, false);
            if (nextAnim >= 0)
            {
                SetNewAnimation(nextAnim);
            }
            else
            {
                // No next animation, respawn
                Play(false);
            }
            return;
        }
        
        // Get current frame
        var frameIndex = _currentAnimation.Sequence.GetFrameIndex(_animationStep);
        if (frameIndex >= 0 && frameIndex < _images.Count)
        {
            PetImage.Source = _images[frameIndex];
            
            // Apply flip if moving right
            if (!_isMovingLeft)
            {
                PetImage.RenderTransform = new AvaloniaScaleTransform(-1, 1);
                PetImage.RenderTransformOrigin = new AvaloniaRelativePoint(0.5, 0.5, AvaloniaRelativeUnit.Relative);
            }
            else
            {
                PetImage.RenderTransform = null;
            }
        }
        
        // Update position
        var movement = _currentAnimation.Sequence.GetMovement(_animationStep);
        if (!_isDragging)
        {
            _positionX += movement.X.GetValue(_displayIndex, _xml) * (_isMovingLeft ? 1 : -1);
            _positionY += movement.Y.GetValue(_displayIndex, _xml);
            _offsetY = movement.OffsetY;
            
            // Apply opacity
            if (movement.Opacity > 0)
            {
                Opacity = movement.Opacity;
            }
            
            // Check screen bounds
            CheckBorders();
            
            Position = new PixelPoint((int)_positionX, (int)(_positionY + _offsetY));
        }
        
        // Update interval if needed
        var interval = movement.Interval.GetValue(_displayIndex, _xml);
        if (interval > 0)
        {
            _timer.Interval = TimeSpan.FromMilliseconds(interval);
        }
        
        _animationStep++;
    }

    private void CheckBorders()
    {
        // Check left/right borders
        if (_positionX < _screenBounds.X)
        {
            _positionX = _screenBounds.X;
            HandleBorderHit();
        }
        else if (_positionX + Width > _screenBounds.X + _screenBounds.Width)
        {
            _positionX = _screenBounds.X + _screenBounds.Width - Width;
            HandleBorderHit();
        }
        
        // Check top/bottom borders  
        if (_positionY < _screenBounds.Y)
        {
            _positionY = _screenBounds.Y;
            HandleBorderHit();
        }
        else if (_positionY + Height > _screenBounds.Y + _screenBounds.Height)
        {
            _positionY = _screenBounds.Y + _screenBounds.Height - Height;
            HandleGravity();
        }
    }

    private void HandleBorderHit()
    {
        var nextAnim = _animations.GetNextAnimation(_currentAnimation.Id, false, true, false);
        if (nextAnim >= 0)
        {
            SetNewAnimation(nextAnim);
        }
    }

    private void HandleGravity()
    {
        var nextAnim = _animations.GetNextAnimation(_currentAnimation.Id, false, false, true);
        if (nextAnim >= 0)
        {
            SetNewAnimation(nextAnim);
        }
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        
        if (point.Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            _dragStartPoint = e.GetPosition(this);
            _windowStartPosition = Position;
            
            // Set drag animation if available
            if (_animations.AnimationDrag > 0)
            {
                SetNewAnimation(_animations.AnimationDrag);
            }
            
            e.Pointer.Capture(PetImage);
        }
        else if (point.Properties.IsRightButtonPressed)
        {
            // Show context menu - handled by tray icon service
        }
    }

    private void OnPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            e.Pointer.Capture(null);
            
            _positionX = Position.X;
            _positionY = Position.Y;
            
            // Set fall animation if available
            if (_animations.AnimationFall > 0)
            {
                SetNewAnimation(_animations.AnimationFall);
            }
        }
    }

    private void OnPointerMoved(object sender, PointerEventArgs e)
    {
        if (_isDragging)
        {
            var currentPoint = e.GetPosition(this);
            var offset = currentPoint - _dragStartPoint;
            
            Position = new PixelPoint(
                _windowStartPosition.X + (int)offset.X,
                _windowStartPosition.Y + (int)offset.Y
            );
        }
    }

    public void Kill()
    {
        _timer.Stop();
        
        if (_animations.AnimationKill > 0)
        {
            SetNewAnimation(_animations.AnimationKill);
            _timer.Start();
            
            // Close after animation completes
            var closeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            closeTimer.Tick += (s, e) =>
            {
                closeTimer.Stop();
                Close();
            };
            closeTimer.Start();
        }
        else
        {
            Close();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();
        foreach (var img in _images)
        {
            img?.Dispose();
        }
        _images.Clear();
        base.OnClosed(e);
    }
}
