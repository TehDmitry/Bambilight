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

        private Device mDevice;
        private Direct3D mDirect3D;
        private BackgroundWorker mBackgroundWorker = new BackgroundWorker();

        private byte[] mColorBufferTopLeft = new byte[DATASTREAM_BYTES_PER_PIXEL];
        private byte[] mColorBufferTopRight = new byte[DATASTREAM_BYTES_PER_PIXEL];
        private byte[] mColorBufferCenter = new byte[DATASTREAM_BYTES_PER_PIXEL];
        private byte[] mColorBufferBottomLeft = new byte[DATASTREAM_BYTES_PER_PIXEL];
        private byte[] mColorBufferBottomRight = new byte[DATASTREAM_BYTES_PER_PIXEL];
        private int[] mColorBuffer = new int[COLORS_PER_LED];

        public DxScreenCapture() {
            mDirect3D = new Direct3D();

            PresentParameters present_params = new PresentParameters();
            present_params.Windowed = true;
            present_params.SwapEffect = SwapEffect.Discard;
            mDevice = new Device(mDirect3D, 0, DeviceType.Hardware, IntPtr.Zero, CreateFlags.SoftwareVertexProcessing, present_params);

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

            while (!mBackgroundWorker.CancellationPending) {

                Surface surface = Surface.CreateOffscreenPlain(mDevice,
                    Screen.PrimaryScreen.Bounds.Width,
                    Screen.PrimaryScreen.Bounds.Height,
                    Format.A8R8G8B8,
                    Pool.Scratch);

                mDevice.GetFrontBufferData(0, surface);

                DataRectangle dataRectangle = surface.LockRectangle(LockFlags.None);
                DataStream dataStream = dataRectangle.Data;

                lock (SpotSet.Lock) {
                    foreach (Spot spot in SpotSet.Spots) {
                        if (spot.TopLeft.DxPos >= 0 
                            && spot.TopRight.DxPos >= 0 
                            && spot.BottomLeft.DxPos >= 0 
                            && spot.BottomRight.DxPos >= 0) {

                            dataStream.Position = spot.TopLeft.DxPos;
                            dataStream.Read(mColorBufferTopLeft, 0, 4);

                            dataStream.Position = spot.TopRight.DxPos;
                            dataStream.Read(mColorBufferTopRight, 0, 4);

                            dataStream.Position = spot.Center.DxPos;
                            dataStream.Read(mColorBufferCenter, 0, 4);

                            dataStream.Position = spot.BottomLeft.DxPos;
                            dataStream.Read(mColorBufferBottomLeft, 0, 4);

                            dataStream.Position = spot.BottomRight.DxPos;
                            dataStream.Read(mColorBufferBottomRight, 0, 4);

                            averageValues();

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

            e.Cancel = true;
        }

        private void averageValues() {
            for (int i = 0; i < COLORS_PER_LED; i++) {
                int temp = (int)(mColorBufferTopLeft[i] + mColorBufferTopRight[i] + mColorBufferCenter[i] + mColorBufferBottomLeft[i] + mColorBufferBottomRight[i]) / 5;
                mColorBuffer[i] = (byte)temp;
            }
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                Stop();

                mBackgroundWorker.Dispose();

                mDevice.Dispose();
                mDirect3D.Dispose();
            }
        }
    }
}
