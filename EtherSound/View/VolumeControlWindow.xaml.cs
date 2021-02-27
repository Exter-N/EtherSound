using EtherSound.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EtherSound.View
{
    /// <summary>
    /// Logique d'interaction pour VolumeControlWindow.xaml
    /// </summary>
    public partial class VolumeControlWindow : Window
    {
        readonly RootModel model;

        internal VolumeControlWindow(RootModel model)
        {
            this.model = model;
            InitializeComponent();
            CompositionHelper.UpdateResources(Resources);
            DataContext = model;
            Height = model.VolumeControlHeight;
        }

        void TryClose()
        {
            try
            {
                Close();
            }
            catch (InvalidOperationException)
            {
                // Already closing, ignore
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            CompositionHelper.ExtendGlassFrame(this, null);
            CompositionHelper.EnableBlurBehind(this);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                TryClose();
            }
            base.OnPreviewKeyDown(e);
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            TryClose();
        }

        static T GetDataContext<T>(object sender)
        {
            return (T)((FrameworkElement)sender).DataContext;
        }

        void Swap_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Mute_MouseUp(sender, e);
            LocalSound.ToggleMute();
        }

        void Mute_MouseUp(object sender, MouseButtonEventArgs e)
        {
            SessionModel model = GetDataContext<SessionModel>(sender);
            model.Muted = !model.Muted;
        }

        #region Slider Event Handlers
        static void SetPositionByControlPoint(Slider slider, Point point)
        {
            double percent = point.X / slider.ActualWidth;
            double newValue = (slider.Maximum - slider.Minimum) * percent;
            slider.Value = Math.Max(slider.Minimum, Math.Min(slider.Maximum, newValue));
        }

        static void ChangePositionByAmount(Slider slider, double amount)
        {
            slider.Value = Math.Max(slider.Minimum, Math.Min(slider.Maximum, slider.Value + amount));
        }

        void Slider_TouchDown(object sender, TouchEventArgs e)
        {
            VisualStateManager.GoToState((FrameworkElement)sender, "Pressed", true);

            Slider slider = (Slider)sender;
            SetPositionByControlPoint(slider, e.GetTouchPoint(slider).Position);
            slider.CaptureTouch(e.TouchDevice);

            GetDataContext<SessionModel>(sender).Muted = false;

            e.Handled = true;
        }

        void Slider_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                VisualStateManager.GoToState((FrameworkElement)sender, "Pressed", true);

                Slider slider = (Slider)sender;
                SetPositionByControlPoint(slider, e.GetPosition(slider));
                slider.CaptureMouse();

                GetDataContext<SessionModel>(sender).Muted = false;

                e.Handled = true;
            }
        }

        void Slider_TouchUp(object sender, TouchEventArgs e)
        {
            if (GetDataContext<SessionModel>(sender).CanSwap)
            {
                SystemSounds.Beep.Play();
            }

            VisualStateManager.GoToState((FrameworkElement)sender, "Normal", true);

            ((Slider)sender).ReleaseTouchCapture(e.TouchDevice);

            e.Handled = true;
        }

        void Slider_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (GetDataContext<SessionModel>(sender).CanSwap)
            {
                SystemSounds.Beep.Play();
            }

            Slider slider = (Slider)sender;
            if (slider.IsMouseCaptured)
            {
                // If the point is outside of the control, clear the hover state.
                Rect rcSlider = new Rect(0, 0, slider.ActualWidth, slider.ActualHeight);
                if (!rcSlider.Contains(e.GetPosition(slider)))
                {
                    VisualStateManager.GoToState((FrameworkElement)sender, "Normal", true);
                }

                ((Slider)sender).ReleaseMouseCapture();

                e.Handled = true;
            }
        }

        void Slider_TouchMove(object sender, TouchEventArgs e)
        {
            Slider slider = (Slider)sender;
            if (slider.AreAnyTouchesCaptured)
            {
                SetPositionByControlPoint(slider, e.GetTouchPoint(slider).Position);
                e.Handled = true;
            }
        }

        void Slider_MouseMove(object sender, MouseEventArgs e)
        {
            Slider slider = (Slider)sender;
            if (slider.IsMouseCaptured)
            {
                SetPositionByControlPoint(slider, e.GetPosition(slider));
            }
        }

        void Slider_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Slider slider = (Slider)sender;
            double amount = Math.Sign(e.Delta) * 0.02;
            ChangePositionByAmount(slider, amount);

            GetDataContext<SessionModel>(sender).Muted = false;

            e.Handled = true;
        }
        #endregion
    }
}
