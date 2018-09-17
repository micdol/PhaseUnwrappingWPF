using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PhaseUnwrapping
{
    public class MainWindowViewModel : BaseViewModel
    {
        #region Commands

        public ICommand BrowseForImageCommand { get; protected set; }
        public ICommand LoadImageCommand { get; protected set; }
        public ICommand SaveImageCommand { get; protected set; }
        public ICommand UnwrapCommand { get; protected set; }
        public ICommand ResiduesCommand { get; protected set; }
        public ICommand BalanceDipolesCommand { get; protected set; }
        public ICommand BranchCutsCommand { get; protected set; }

        #endregion

        #region Properties

        public string InputImagePath
        {
            get => mInputImagePath;
            set
            {
                if (mInputImagePath != value)
                {
                    mInputImagePath = value;
                    FirePropertyChanged(nameof(InputImagePath));
                }
            }
        }
        private string mInputImagePath = null;

        public string OutputImagePath
        {
            get => mOutputImagePath;
            set
            {
                if (mOutputImagePath != value)
                {
                    mOutputImagePath = value;
                    FirePropertyChanged(nameof(OutputImagePath));
                }
            }
        }
        private string mOutputImagePath = null;

        public BitmapSource InputImage
        {
            get => mInputImage;
            private set
            {
                if (mInputImage != value)
                {
                    mInputImage = value;
                    FirePropertyChanged(nameof(InputImage));
                }
            }
        }
        private BitmapSource mInputImage = null;

        public ImageSource OutputImage
        {
            get => mOutputImage;
            set
            {
                if (mOutputImage != value)
                {
                    mOutputImage = value;
                    FirePropertyChanged(nameof(OutputImage));
                }
            }
        }
        private ImageSource mOutputImage = null;

        #endregion

        public MainWindowViewModel()
        {
            SetupCommands();
        }

        #region Init  

        private void SetupCommands()
        {
            BrowseForImageCommand = new RelayCommand((object param) =>
            {
                var dialog = new OpenFileDialog();

                dialog.Multiselect = false;
                dialog.Filter = "Image files (*.png;*.jpeg;*.bmp)|*.png;*.jpeg;*.bmp";
                dialog.InitialDirectory = @"C:\Users\dolinskm\Desktop\pu";

                bool? result = dialog.ShowDialog();
                if (result == true)
                {
                    if ((param as string) == "Input")
                    {
                        InputImagePath = dialog.FileName;
                        LoadImageCommand.Execute(null);
                    }
                    else if ((param as string) == "Output")
                    {
                        OutputImagePath = dialog.FileName;
                    }
                }
            });

            LoadImageCommand = new RelayCommand(() =>
            {
                try
                {
                    // Load as grayscale
                    FormatConvertedBitmap fcb = new FormatConvertedBitmap();
                    fcb.BeginInit();
                    fcb.Source = new BitmapImage(new Uri(InputImagePath, UriKind.RelativeOrAbsolute));
                    fcb.DestinationFormat = PixelFormats.Gray8;
                    fcb.EndInit();

                    InputImage = fcb;
                    OutputImage = new WriteableBitmap(fcb);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Exception while loading input image: {0}", e);
                }
            });

            SaveImageCommand = new RelayCommand(() =>
            {
                try
                {
                    //OutputImage.Save(OutputImagePath);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Exception while saving output image: {0}", e);
                }
            });

            UnwrapCommand = new RelayCommand(() =>
            {
                double[,] wrapped = InputImage.ToDouble2D();
                var unwrapper = new Itoh(wrapped);

                unwrapper.Unwrap();

                OutputImage = InputImage.FromDouble2D(unwrapper.Unwrapped);
            });

            ResiduesCommand = new RelayCommand(() =>
            {
                double[,] wrapped = InputImage.ToDouble2D();
                var unwrapper = new Goldstein(wrapped);

                unwrapper.ComputeResidues();

                OutputImage = (OutputImage as WriteableBitmap).SetPoints(unwrapper.Residues.Select(x => (x.row, x.col, (byte)(x.charge < 0 ? 0 : 255))).ToList());
            });

            BalanceDipolesCommand = new RelayCommand(() =>
            {
                double[,] wrapped = InputImage.ToDouble2D();
                var unwrapper = new Goldstein(wrapped);

                unwrapper.ComputeResidues();
                unwrapper.BalanceDipoles();

                OutputImage = (OutputImage as WriteableBitmap).SetPoints(unwrapper.Residues.Select(x => (x.row, x.col, (byte)(x.charge < 0 ? 0 : 255))).ToList());
            });

            BranchCutsCommand = new RelayCommand(() =>
            {
                double[,] wrapped = InputImage.ToDouble2D();
                var unwrapper = new Goldstein(wrapped);

                unwrapper.ComputeResidues();
                unwrapper.BalanceDipoles();
                unwrapper.ComputeBranchCuts();

                OutputImage = (OutputImage as WriteableBitmap).SetPoints(unwrapper.BranchCuts.Select(x => (row: x.row, col: x.col, value: (byte)255)).ToList());
            });
        }

        #endregion
    }
}