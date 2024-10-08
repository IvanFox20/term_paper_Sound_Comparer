using System;
using System.Drawing;
using System.Windows.Forms;

namespace SoundComparer.WaveUtils
{
    /// <summary>
    /// ����� ��� ���������� ������������� WAV-������.
    /// </summary>
    public class WaveControl : System.Windows.Forms.UserControl
    {
        #region Members

        /// <summary> 
        /// ����������� ���������� ��� ��������� ���������.
        /// </summary>
        private System.ComponentModel.Container components = null;

        /// <summary>
        /// ���������� ������ WaveFile, ����������� ���������� ��������� .WAV.
        /// </summary>
        private WaveSound m_Wavefile;

        /// <summary>
        /// ���������� ��������, �����������, ����� �� �������� .WAV. ������� �� ������ .WAV �� ��� ���, ���� �� �� ����� ��������.
        /// </summary>
        private bool m_DrawWave = false;

        /// <summary>
        /// ������ � ������ �����.
        /// </summary>
        //private string			m_Filename;

        /// <summary>
        /// ������ �������� ������� (�� ��� X) ������������ ��� ���������� ������� � WAV-�����.
        /// ��������� �������� �������� �� ������ ����������, ����� .WAV ��������� ��� ������.
        /// </summary>
        private float m_SamplesPerPixel = 0f;

        /// <summary>
        /// ��������, ������������, ��������� ������������� ��� ����������� m_SamplesPerPixel. ��� ������� ������ "����������".
        /// ��������� �������� - m_SamplesPerPixel / 25, ����� ��� ���� ��������������� ������� .WAV.
        /// </summary>
        private float m_ZoomFactor;

        /// <summary>
        /// ��������� �������� �� ��� X ��� �������������� �����.
        /// </summary>
        private int m_StartX = 0;

        /// <summary>
        /// �������� �������� �� ��� X ��� �������������� �����.
        /// </summary>
        private int m_EndX = 0;

        /// <summary>
        /// �������� �� ������ �����, ��� ���������� ���������.
        /// </summary>
        private int m_OffsetInSamples = 0;

        #endregion // Members

        #region Properties

        public bool DrawWave
        {
            set { m_DrawWave = value; }
        }

        public WaveSound Sound
        {
            get { return m_Wavefile; }
        }

        private float SamplesPerPixel
        {
            set
            {
                m_SamplesPerPixel = value;
                m_ZoomFactor = m_SamplesPerPixel / 25;
            }
        }

        #endregion // Properties

        #region Constructor

        public WaveControl()
        {
            // ���� ����� ��������� ��� ������������ ���� Windows.
            InitializeComponent();
        }

        #endregion // Constructor

        #region Component Designer generated code

        /// <summary> 
        /// ����������� ����� ��� ��������� ������������ - �� ��������� 
        /// ���������� ����� ������ � ������� ��������� ����.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // WaveControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.BackColor = System.Drawing.Color.Lavender;
            this.ForeColor = System.Drawing.Color.Lime;
            this.Name = "WaveControl";
            this.Size = new System.Drawing.Size(530, 101);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.WaveControl_Paint);
            this.ResumeLayout(false);
        }

        /// <summary> 
        /// ����������� ����� ������������ �������.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #endregion // Component Designer generated code

        #region Wave Drawing

        /// <summary>
        /// ������ WAV-���� � ������������� ���� ��� ��������� �����.
        /// </summary>
        /// <param name="wc">�������, � ������� ����� �������� WAV-����.</param>
        /// <param name="filename">��� ����� ��� ������.</param>
        /// <param name="progress">������ ��������� ��� ����������� ��������� ��������.</param>
        public void Read(WaveControl wc, string filename, ProgressBar progress)
        {
            m_Wavefile = new WaveSound(filename, wc, progress);
            m_Wavefile.ReadWavFile();
            m_DrawWave = true;
        }

        /// <summary>
        /// �������������� ��� ���������, ����� �������� �������������� ��������.
        /// </summary>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // ��������� ������, ����� �������� �������������� ��������
        }

        /// <summary>
        /// ������ ����� �� ����������� ���������.
        /// </summary>
        /// <param name="pea">��������� ���������.</param>
        /// <param name="pen">�������� ��� ���������.</param>
        private void Draw(PaintEventArgs pea, Pen pen)
        {
            Graphics grfx = pea.Graphics;
            Rectangle visBounds = ClientRectangle;

            if (m_SamplesPerPixel == 0.0)
            {
                this.SamplesPerPixel = (m_Wavefile.Samples.Length / visBounds.Width);
            }

            grfx.DrawLine(pen, 0, (int)visBounds.Height / 2, (int)visBounds.Width, (int)visBounds.Height / 2);
            Draw16Bit(grfx, pen, visBounds);
        }

        /// <summary>
        /// ������ 16-������ ������� WAV-�����.
        /// </summary>
        /// <param name="grfx">����������� ��������.</param>
        /// <param name="pen">�������� ��� ���������.</param>
        /// <param name="visBounds">������� ���������.</param>
        private void Draw16Bit(Graphics grfx, Pen pen, RectangleF visBounds)
        {
            int prevX = 0;
            int prevY = 0;

            int i = 0;
            int index = m_OffsetInSamples; // ������ ��� �������� � ������� ������
            int maxSampleToShow = (int)((m_SamplesPerPixel * visBounds.Width) + m_OffsetInSamples);

            maxSampleToShow = Math.Min(maxSampleToShow, m_Wavefile.Samples.Length);
            while (index < maxSampleToShow)
            {
                short maxVal = -32767;
                short minVal = 32767;

                // ������� ������������ � ����������� ���� ��� ����� ������� 
                for (int x = 0; x < m_SamplesPerPixel; x++)
                {
                    maxVal = Math.Max(maxVal, m_Wavefile.Samples[x + index]);
                    minVal = Math.Min(minVal, m_Wavefile.Samples[x + index]);
                }

                // ��������������� �� ������ ����
                int scaledMinVal = (int)(((minVal + 32768) * visBounds.Height) / 65536);
                int scaledMaxVal = (int)(((maxVal + 32768) * visBounds.Height) / 65536);

                // ���� ���������� �������� �� ������� ��������� ��� ������ ����, �� ������� �� ������� ��������� ����������, ������� ������ �� ����������
                if (m_SamplesPerPixel > 0.0000000001)
                {
                    // ���� max/min ���������, ��������� ����� �� ���������� �������,
                    // � ��������� ������ �� ������ �� ������
                    if (scaledMinVal == scaledMaxVal)
                    {
                        if (prevY != 0)
                            grfx.DrawLine(pen, prevX, prevY, i, scaledMaxVal);
                    }
                    else
                    {
                        grfx.DrawLine(pen, i, scaledMinVal, i, scaledMaxVal);
                    }
                }
                else
                    return;

                prevX = i;
                prevY = scaledMaxVal;

                i++;
                index = (int)(i * m_SamplesPerPixel) + m_OffsetInSamples;
            }
        }

        /// <summary>
        /// ���������� ������� ��������� ��� �������� ����.
        /// </summary>
        /// <param name="sender">������, ��������� �������.</param>
        /// <param name="e">��������� ������� ���������.</param>
        private void WaveControl_Paint(object sender, PaintEventArgs e)
        {
            Pen pen = new Pen(Color.Black);
            SolidBrush brush = new SolidBrush(this.BackColor);
            e.Graphics.FillRectangle(brush, 0, 0, (int)e.Graphics.ClipBounds.Width, (int)e.Graphics.ClipBounds.Height);
            if (m_DrawWave)
            {
                Draw(e, pen);
            }

            int regionStartX = Math.Min(m_StartX, m_EndX);
            int regionEndX = Math.Max(m_StartX, m_EndX);

            brush = new SolidBrush(Color.Violet);
            e.Graphics.FillRectangle(brush, regionStartX, 0, regionEndX - regionStartX, (int)e.Graphics.ClipBounds.Height);
        }

        #endregion // Wave Drawing
    }
}
