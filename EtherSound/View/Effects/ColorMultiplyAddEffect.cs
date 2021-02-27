using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace EtherSound.View.Effects
{
    public class ColorMultiplyAddEffect : ShaderEffect
    {
        public static readonly DependencyProperty InputProperty =
            RegisterPixelShaderSamplerProperty("Input", typeof(ColorMultiplyAddEffect), 0);

        public static readonly DependencyProperty FactorProperty =
            DependencyProperty.Register("Factor", typeof(Color), typeof(ColorMultiplyAddEffect),
                    new UIPropertyMetadata(Colors.White, PixelShaderConstantCallback(0)));

        public static readonly DependencyProperty AddendProperty =
            DependencyProperty.Register("Addend", typeof(Color), typeof(ColorMultiplyAddEffect),
                    new UIPropertyMetadata(Color.FromArgb(0, 0, 0, 0), PixelShaderConstantCallback(1)));

        static readonly PixelShader shader = new PixelShader() { UriSource = Helpers.MakePackUri("View/Effects/ColorMultiplyAdd.fx.ps") };

        [Browsable(false)]
        public Brush Input
        {
            get => (Brush)GetValue(InputProperty);
            set => SetValue(InputProperty, value);
        }

        public Color Factor
        {
            get => (Color)GetValue(FactorProperty);
            set => SetValue(FactorProperty, value);
        }

        public Color Addend
        {
            get => (Color)GetValue(AddendProperty);
            set => SetValue(AddendProperty, value);
        }

        public ColorMultiplyAddEffect()
        {
            PixelShader = shader;

            UpdateShaderValue(InputProperty);
            UpdateShaderValue(FactorProperty);
            UpdateShaderValue(AddendProperty);
        }
    }
}
