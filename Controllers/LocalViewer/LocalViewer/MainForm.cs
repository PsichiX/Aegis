using Aegis.DotNetControllerAPI;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LocalViewer
{
    public partial class MainForm : Form
    {
        private CommunicationService m_service;
        //private volatile bool m_workingCameraListReceiver = false;

        public MainForm()
        {
            m_service = new CommunicationService();

            InitializeComponent();

            FormClosed += MainForm_FormClosed;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            var dialog = new ConnectDialog();
            while (true)
            {
                dialog.ShowDialog();
                if (dialog.DialogResult != System.Windows.Forms.DialogResult.OK)
                {
                    Close();
                    return;
                }
                if (m_service.Connect("127.0.0.1", dialog.Port, dialog.Token, CommunicationService.ControllerPermissions.ReceiveEyeImages))
                    break;
            }
            m_service.OnObtainCamerasList += m_service_OnObtainCamerasList;
            m_service.OnReceiveImage += m_service_OnReceiveImage;
            camerasListComboBox.SelectedIndexChanged += camerasListComboBox_SelectedIndexChanged;
            m_service.ObtainCamerasList();
            //Task.Run(() =>
            //{
            //    m_workingCameraListReceiver = true;
            //    while (m_workingCameraListReceiver)
            //    {
            //        m_service.ObtainCamerasList();
            //        Thread.Sleep(1000);
            //    }
            //});
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            //m_workingCameraListReceiver = false;
            m_service.Disconnect();
        }

        private void m_service_OnObtainCamerasList(object sender, EventArgs e)
        {
            this.DoOnUiThread(() =>
            {
                if (m_service == null)
                    return;
                camerasListComboBox.Items.Clear();
                var cameras = m_service.CamerasList;
                foreach (var camera in cameras)
                    camerasListComboBox.Items.Add(camera);
            });
        }

        private void m_service_OnReceiveImage(object sender, EventArgs e)
        {
            this.DoOnUiThread(() =>
            {
                if (m_service == null)
                    return;
                var data = m_service.Image;
                var img = cameraViewPictureBox.Image as Bitmap;
                if (img == null || img.Width != data.Width || img.Height != data.Height)
                    img = new Bitmap(data.Width, data.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                var bmp = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, img.PixelFormat);
                var ptr = bmp.Scan0;
                var numBytes = bmp.Stride * img.Height;
                var bytes = new byte[numBytes];
                Marshal.Copy(ptr, bytes, 0, numBytes);
                byte v = 0;
                for (int i = 0, c = data.Data.Length, p = 0; i < c; ++i)
                {
                    v = data.Data[i];
                    bytes[p++] = v;
                    bytes[p++] = v;
                    bytes[p++] = v;
                }
                Marshal.Copy(bytes, 0, ptr, numBytes);
                img.UnlockBits(bmp);
                cameraViewPictureBox.Image = img;
            });
        }

        private void camerasListComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            m_service.ChangeSourceCamera(camerasListComboBox.SelectedIndex < 0 ? "" : camerasListComboBox.Items[camerasListComboBox.SelectedIndex] as string);
        }
    }
}
