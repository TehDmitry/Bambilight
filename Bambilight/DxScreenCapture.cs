/* See the file "LICENSE" for the full license governing this code. */

using System;
using System.ComponentModel;
using System.Windows.Forms;
using SlimDX;
using SlimDX.Direct3D9;

namespace Bambilight {

    public class DxScreenCapture : IDisposable {

        private const int COLORS_PER_LED = 3;
        private const int DATASTREAM_BYTES_PER_PIXEL = 4;
        private const int AVERAGE_PIXEL_GRID_STEP = 8;

        private BackgroundWorker mBackgroundWorker = new BackgroundWorker();

        private byte[] mColorBufferSpot = new byte[DATASTREAM_BYTES_PER_PIXEL];
        private long[] mColorSumSpot = new long[COLORS_PER_LED];
        private int[] mColorBuffer = new int[COLORS_PER_LED];

        public DxScreenCapture() {
            mBackgroundWorker.DoWork += mBackgroundWorker_DoWork;
            mBackgroundWorker.WorkerSupportsCancellation = true;
        }

        public void Start() {
            if (!mBackgroundWorker.IsBusy) {
                mBackgroundWorker.RunWorkerAsync();
            }
        }

        public void Stop() {
            if (mBackgroundWorker.IsBusy) {
                mBackgroundWorker.CancelAsync();
            }
        }

        private void mBackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {

            Direct3D direct3D = new Direct3D();

            PresentParameters present_params = new PresentParameters();
            present_params.Windowed = true;
            present_params.SwapEffect = SwapEffect.Discard;
            Device device = new Device(direct3D, 0, DeviceType.Hardware, IntPtr.Zero, CreateFlags.SoftwareVertexProcessing, present_params);

            int pixelCount;
            int ScreenWidth = Program.ScreenWidth;

            while (!mBackgroundWorker.CancellationPending) {

                Surface surface = Surface.CreateOffscreenPlain(device,
                    Screen.PrimaryScreen.Bounds.Width,
                    Screen.PrimaryScreen.Bounds.Height,
                    Format.A8R8G8B8,
                    Pool.Scratch);

                device.GetFrontBufferData(0, surface);

                DataRectangle dataRectangle = surface.LockRectangle(LockFlags.None);
                DataStream dataStream = dataRectangle.Data;

                lock (SpotSet.Lock) {
                    foreach (Spot spot in SpotSet.Spots) {
                        if (spot.TopLeft.DxPos >= 0 
                            && spot.TopRight.DxPos >= 0 
                            && spot.BottomLeft.DxPos >= 0 
                            && spot.BottomRight.DxPos >= 0) {

                            pixelCount = 0;
                            for (int i = 0; i < COLORS_PER_LED; i++)
                            {
                                mColorSumSpot[i] = 0;
                            }
                            
                            for (long Y = spot.Rectangle.Y; Y < spot.Rectangle.Y + spot.Rectangle.Width; Y += AVERAGE_PIXEL_GRID_STEP)
                            {
                                for (long X = spot.Rectangle.X; X < spot.Rectangle.X + spot.Rectangle.Height; X += AVERAGE_PIXEL_GRID_STEP)
                                {
                                    long Position = (Y * ScreenWidth + X) * DATASTREAM_BYTES_PER_PIXEL;
                                    dataStream.Position = Position;
                                    dataStream.Read(mColorBufferSpot, 0, 4);

                                    for (int i = 0; i < COLORS_PER_LED; i++)
                                    {
                                        mColorSumSpot[i] += mColorBufferSpot[i];
                                    }
                                    pixelCount++;
                                }
                            }

                            for (int i = 0; i < COLORS_PER_LED; i++)
                            {
                                mColorBuffer[i] = (byte)(mColorSumSpot[i]/ pixelCount); 
                            }

                            if (mColorBuffer[0] <= Settings.SaturationTreshold) { mColorBuffer[0] = 0x00; } //blue
                            if (mColorBuffer[1] <= Settings.SaturationTreshold) { mColorBuffer[1] = 0x00; } // green
                            if (mColorBuffer[2] <= Settings.SaturationTreshold) { mColorBuffer[2] = 0x00; } // red

                            spot.SetColor(mColorBuffer[2] /* red */, mColorBuffer[1] /* green */, mColorBuffer[0] /* blue */);
                        }
                    }
                }

                surface.UnlockRectangle();
                surface.Dispose();
                
            }

            device.Dispose();
            direct3D.Dispose();

            e.Cancel = true;
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                Stop();

                mBackgroundWorker.Dispose();
            }
        }
    }
}
