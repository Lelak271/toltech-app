using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Toltech.App.Z_Test
{
    public partial class MatrixRain : Window
    {
        private readonly Random random = new Random();
        private readonly List<StackPanel> codeColumns = new List<StackPanel>();
        private readonly DispatcherTimer timer;
        private readonly Dictionary<StackPanel, double> stackSpeeds = new Dictionary<StackPanel, double>();
        private MediaPlayer _mediaPlayer = new MediaPlayer();

        public MatrixRain()
        {
            InitializeComponent();
            Loaded += MatrixRain_Loaded;

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            timer.Tick += Timer_Tick;
        }

        private void MatrixRain_Loaded(object sender, RoutedEventArgs e)
        {
            int columnCount = (int)(this.ActualWidth / 10); // 20 px par colonne
            for (int i = 0; i < columnCount; i++)
            {
                var stack = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Width = 10
                };

                Canvas.SetLeft(stack, i * 10);
                Canvas.SetTop(stack, random.NextDouble() * this.ActualHeight);

                codeColumns.Add(stack);
                MainCanvas.Children.Add(stack);

                // Initialiser la vitesse pour chaque colonne
                stackSpeeds[stack] = random.NextDouble() * 7 + 12; // Génère un nombre entre 7 et 10
            }

            PlayBackgroundMusic("Asset/Matrix_Music.mp3");
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            foreach (var stack in codeColumns)
            {
                // Créer un caractère lumineux
                var brightChar = new TextBlock
                {
                    Text = GetAsciiCharacter().ToString(),
                    FontSize = 13,
                    FontFamily = new FontFamily("Consolas"),
                    Foreground = Brushes.LimeGreen,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                };

                stack.Children.Add(brightChar);

                // Appliquer un dégradé de couleur aux caractères précédents
                for (int i = stack.Children.Count - 1; i >= 0; i--)
                {
                    if (stack.Children[i] is TextBlock tb)
                    {
                        int distanceFromEnd = stack.Children.Count - 1 - i;

                        // Appliquer un dégradé progressif sur les 20 derniers caractères
                        if (distanceFromEnd == 0)
                        {
                            tb.Foreground = Brushes.LimeGreen;
                        }
                        else if (distanceFromEnd <= 3)
                        {
                            tb.Foreground = new SolidColorBrush(Color.FromRgb(0, 200, 0));
                        }
                        else if (distanceFromEnd <= 6)
                        {
                            tb.Foreground = new SolidColorBrush(Color.FromRgb(0, 150, 0));
                        }
                        else if (distanceFromEnd <= 9)
                        {
                            tb.Foreground = new SolidColorBrush(Color.FromRgb(0, 100, 0));
                        }
                        else if (distanceFromEnd <= 12)
                        {
                            tb.Foreground = new SolidColorBrush(Color.FromRgb(0, 60, 0));
                        }
                        else if (distanceFromEnd <= 15)
                        {
                            tb.Foreground = new SolidColorBrush(Color.FromRgb(0, 30, 0));
                        }
                        else
                        {
                            tb.Foreground = Brushes.Black;
                        }
                    }

                }

                // Limiter la hauteur
                if (stack.Children.Count > 30)
                    stack.Children.RemoveAt(0);

                // Déplacement vers le bas
                double top = Canvas.GetTop(stack);
                top += stackSpeeds[stack];

                if (top > this.ActualHeight)
                {
                    top = -stack.ActualHeight;
                    stack.Children.Clear(); // Réinitialisation
                }

                Canvas.SetTop(stack, top);
            }
        }


        private char GetAsciiCharacter()
        {
            int t = random.Next(10);
            if (t <= 2)
                return (char)('0' + random.Next(10));        // Chiffres
            else if (t <= 4)
                return (char)('a' + random.Next(26));        // Minuscules
            else if (t <= 6)
                return (char)('A' + random.Next(26));        // Majuscules
            else
                return (char)(random.Next(32, 255));         // Caractères ASCII imprimables
        }

        private char GetRandomChar()
        {
            int rangeSelector = random.Next(0, 4);

            return rangeSelector switch
            {
                0 => (char)random.Next(33, 127),         // ASCII imprimable
                1 => (char)random.Next(0x30A0, 0x30FF),  // Katakana japonais
                2 => (char)random.Next(0x3040, 0x309F),  // Hiragana japonais
                3 => (char)random.Next(0x4E00, 0x4EFF),  // Idéogrammes chinois (CJK)
                _ => '?'                                 // fallback
            };
        }





        // Joue la musique en boucle depuis un fichier local
        private void PlayBackgroundMusic(string resourcePath)
        {
            try
            {
                // Construire le chemin absolu depuis le répertoire de l'application
                var fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, resourcePath);

                if (!System.IO.File.Exists(fullPath))
                {
                    MessageBox.Show($"Le fichier audio est introuvable : {fullPath}",
                                     "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _mediaPlayer.Open(new Uri(fullPath, UriKind.Absolute));

                _mediaPlayer.Volume = 0.5; // Volume 50%
                _mediaPlayer.MediaEnded += (s, e) =>
                {
                    // Loop de la musique
                    _mediaPlayer.Position = TimeSpan.Zero;
                    _mediaPlayer.Play();
                };
                _mediaPlayer.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la lecture de la musique : {ex.Message}",
                                 "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // Arrêter lorsque la fenêtre est fermée
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _mediaPlayer.Stop();
            _mediaPlayer.Close();
        }




    }
}